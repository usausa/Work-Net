using System.Buffers;
using System.Diagnostics;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using SkiaSharp;

using var session = new InferenceSession("model.onnx");
var metadata = session.InputMetadata.First();
var dimensions = metadata.Value.Dimensions;

var size = 3 * dimensions[2] * dimensions[3];
var buffer = ArrayPool<float>.Shared.Rent(size);
var inputTensor = new DenseTensor<float>(buffer.AsMemory(0, size), [1, 3, dimensions[2], dimensions[3]]);
LoadImageFile("image.jpg", inputTensor, dimensions[2], dimensions[3]);

var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(metadata.Key, inputTensor) };
using var results = session.Run(inputs);

ArrayPool<float>.Shared.Return(buffer);

var labels = File.ReadAllLines("labels.txt");

using var skBitmap = SKBitmap.Decode("image.jpg");
using var skCanvas = new SKCanvas(skBitmap);
var paint = new SKPaint
{
    Color = SKColors.Red,
    StrokeWidth = 1,
    IsStroke = true
};

var boxes = results.First(x => x.Name == "detected_boxes").AsTensor<float>();
var classes = results.First(x => x.Name == "detected_classes").AsTensor<long>();
var scores = results.First(x => x.Name == "detected_scores").AsTensor<float>();
for (var i = 0; i < Math.Min(scores.Length, 10); i++)
{
    var xmin = boxes[0, i, 0] * skBitmap.Width;
    var ymin = boxes[0, i, 1] * skBitmap.Height;
    var xmax = boxes[0, i, 2] * skBitmap.Width;
    var ymax = boxes[0, i, 3] * skBitmap.Height;
    var classIndex = classes[0, i];
    var score = scores[0, i];
    Debug.WriteLine($"{score} : {labels[classIndex]} : {xmin} {ymin} {xmax} {ymax}");
    if (score > 0.5)
    {
        skCanvas.DrawRect(new SKRect(xmin, ymin, xmax, ymax), paint);
    }
}

using var outputStream = File.OpenWrite("output.jpg");
skBitmap.Encode(outputStream, SKEncodedImageFormat.Jpeg, 100);

static void LoadImageFile(string imagePath, DenseTensor<float> tensor, int width, int height)
{
    using var inputStream = File.OpenRead(imagePath);
    using var skBitmap = SKBitmap.Decode(inputStream);
    var resizedBitmap = skBitmap.Resize(new SKImageInfo(width, height), SKFilterQuality.High);

    for (var y = 0; y < resizedBitmap.Height; y++)
    {
        for (var x = 0; x < resizedBitmap.Width; x++)
        {
            var color = resizedBitmap.GetPixel(x, y);
            tensor[0, 0, y, x] = color.Red - 255.0f;
            tensor[0, 1, y, x] = color.Green - 255.0f;
            tensor[0, 2, y, x] = color.Blue - 255.0f;
        }
    }
}
