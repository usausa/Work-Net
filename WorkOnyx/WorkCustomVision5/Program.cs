using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using System.Buffers;
using System.Diagnostics;
using SkiaSharp;

var session = new InferenceSession(File.ReadAllBytes("model.onnx"));
var labels = File.ReadAllLines("labels.txt");

for (var i = 1; i <= 14; i++)
{
    var input = $"image{i:D3}.jpg";
    var output = $"output{i:D3}.jpg";

    if (!File.Exists(input))
    {
        continue;
    }

    var results = Process(labels, input);

    Debug.WriteLine($"-------- {input}");
    Debug.WriteLine(results.Count);
    var results2 = results.Where(x => x.Score >= 0.5).ToList();
    Debug.WriteLine(results2.Count);
    foreach (var result in results2)
    {
        Debug.WriteLine($"{result.Label} {result.Score}");
    }

    Draw(results2, input, output);
}

void Draw(List<DetectResult> results, string input, string output)
{
    var bitmap = SKBitmap.Decode(input);
    var width = bitmap.Width;
    var height = bitmap.Height;

    using var canvas = new SKCanvas(bitmap);
    var paint = new SKPaint
    {
        Color = SKColors.Red,
        StrokeWidth = 3,
        IsStroke = true
    };

    foreach (var result in results)
    {
        canvas.DrawRect(width * result.Left, height * result.Top, width * (result.Right - result.Left), height * (result.Bottom - result.Top), paint);
    }

    using var outputStream = File.OpenWrite(output);
    bitmap.Encode(outputStream, SKEncodedImageFormat.Jpeg, 100);
}


// ReSharper disable once VariableHidesOuterVariable
List<DetectResult> Process(string[] labels, string image)
{
    // TODO filter ?
    var bitmap = SKBitmap.Decode(image);

    var metadata = session.InputMetadata.First();
    var dimensions = metadata.Value.Dimensions;
    var height = dimensions[2];
    var width = dimensions[3];

    var size = 3 * width * height;
    var buffer = ArrayPool<float>.Shared.Rent(size);
    var inputTensor = new DenseTensor<float>(buffer.AsMemory(0, size), [1, 3, height, width]);
    PrepareTensor(bitmap, inputTensor, width, height);

    var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(metadata.Key, inputTensor) };
    using var values = session.Run(inputs);

    ArrayPool<float>.Shared.Return(buffer);

    var boxes = values.First(x => x.Name == "detected_boxes").AsTensor<float>();
    var classes = values.First(x => x.Name == "detected_classes").AsTensor<long>();
    var scores = values.First(x => x.Name == "detected_scores").AsTensor<float>();

    var results = new List<DetectResult>();
    for (var i = 0; i < scores.Length; i++)
    {
        results.Add(new DetectResult(boxes[0, i, 0], boxes[0, i, 1], boxes[0, i, 2], boxes[0, i, 3], scores[0, i], labels[classes[0, i]]));
    }
    return results;

}

void PrepareTensor(SKBitmap bitmap, DenseTensor<float> tensor, int width, int height)
{
    var resizedBitmap = bitmap.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
    for (var y = 0; y < resizedBitmap.Height; y++)
    {
        for (var x = 0; x < resizedBitmap.Width; x++)
        {
            var color = resizedBitmap.GetPixel(x, y);
            tensor[0, 0, y, x] = color.Red;
            tensor[0, 1, y, x] = color.Green;
            tensor[0, 2, y, x] = color.Blue;
        }
    }
}

public class DetectResult
{
    public float Left { get; }

    public float Top { get; }

    public float Right { get; }

    public float Bottom { get; }

    public float Score { get; }

    public string Label { get; }

    public DetectResult(float left, float top, float right, float bottom, float score, string label)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
        Score = score;
        Label = label;
    }
}
