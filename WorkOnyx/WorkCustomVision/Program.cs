using System.Diagnostics;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using SkiaSharp;

// TODO もっとシンプルなモデルで
using var session = new InferenceSession("model.onnx");
var metadata = session.InputMetadata.First();
var dimensions = metadata.Value.Dimensions;

var inputTensor = LoadImageFile("image.jpg", dimensions);

var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(metadata.Key, inputTensor) };
using var results = session.Run(inputs);

using var skBitmap = SKBitmap.Decode("image.jpg");
using var skCanvas = new SKCanvas(skBitmap);
var paint = new SKPaint
{
    Color = SKColors.Red,
    StrokeWidth = 1,
    IsStroke = true
};

foreach (var result in results)
{
    Debug.WriteLine("-----");
    Debug.WriteLine($"Output Name: {result.Name}");
    var tensor = result.AsTensor<float>();
    if (tensor is not null)
    {
        Debug.WriteLine($"Tensor length: {tensor.Length}");
        if (result.Name == "detected_boxes")
        {
            for (var i = 0; i < tensor.Dimensions[1]; i++)
            {
                var xmin = tensor[0, i, 0] * skBitmap.Width;
                var ymin = tensor[0, i, 1] * skBitmap.Height;
                var xmax = tensor[0, i, 2] * skBitmap.Width;
                var ymax = tensor[0, i, 3] * skBitmap.Height;
                Debug.WriteLine($"{xmin} {ymin} {xmax} {ymax}");
                skCanvas.DrawRect(new SKRect(xmin, ymin, xmax, ymax), paint);
            }
        }
    }
}

using var outputStream = File.OpenWrite("output.jpg");
skBitmap.Encode(outputStream, SKEncodedImageFormat.Jpeg, 100);

static DenseTensor<float> LoadImageFile(string imagePath, int[] dimensions)
{
    using var inputStream = File.OpenRead(imagePath);
    using var skBitmap = SKBitmap.Decode(inputStream);
    //var resizedBitmap = skBitmap.Resize(new SKImageInfo(dimensions[2], dimensions[3]), SKFilterQuality.High);
    var resizedBitmap = skBitmap;

    var input = new DenseTensor<float>([1, 3, dimensions[2], dimensions[3]]);
    for (var y = 0; y < resizedBitmap.Height; y++)
    {
        for (var x = 0; x < resizedBitmap.Width; x++)
        {
            var color = resizedBitmap.GetPixel(x, y);
            input[0, 0, y, x] = color.Red / 255.0f;
            input[0, 1, y, x] = color.Green / 255.0f;
            input[0, 2, y, x] = color.Blue / 255.0f;
        }
    }

    return input;
}
