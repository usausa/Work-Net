using Microsoft.AI.MachineLearning;

using System.IO;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

var model = LearningModel.LoadFromFilePath("model.onyx");
var session = new LearningModelSession(model);
var binding = new LearningModelBinding(session);

var inputTensor = LoadImageFile("image.jpg", 320, 320);
binding.Bind("data", inputTensor);
var result = session.Evaluate(binding, "0");


//var decoder = await BitmapDecoder.CreateAsync(stream);
//var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
//var videoFrame = VideoFrame.CreateWithSoftwareBitmap(softwareBitmap);

static TensorFloat LoadImageFile(string imagePath, int width, int height)
{
    using var inputStream = File.OpenRead(imagePath);
    using var skBitmap = SKBitmap.Decode(inputStream);
    //var resizedBitmap = skBitmap.Resize(new SKImageInfo(dimensions[2], dimensions[3]), SKFilterQuality.High);
    var resizedBitmap = skBitmap;

    var input = new DenseTensor<float>([1, 3, height, width]);
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

    return TensorFloat.CreateFromArray([1, 3, height, width], input.ToArray());
}
