using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace WorkImageOnyx;

public static class Program
{
    public static void Main()
    {
        const float confidenceThreshold = 0.5f;
        const float iouThreshold = 0.3f;

        using var detector = new FaceDetector("version-RFB-320.onnx");

        var imagePath = @"D:\学習データ\people640x480.png";
        using var bitmap = SKBitmap.Decode(imagePath);
        if (bitmap == null)
        {
            Console.WriteLine("画像の読み込みに失敗しました。");
            return;
        }

        Console.WriteLine($"画像サイズ: {bitmap.Width}x{bitmap.Height}");

        var imageBytes = new byte[bitmap.Width * bitmap.Height * 3];
        var pixels = bitmap.Pixels;
        var idx = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = pixels[y * bitmap.Width + x];
                imageBytes[idx++] = pixel.Red;
                imageBytes[idx++] = pixel.Green;
                imageBytes[idx++] = pixel.Blue;
            }
        }

        var faces = detector.Detect(imageBytes, bitmap.Width, bitmap.Height, confidenceThreshold, iouThreshold);

        Console.WriteLine($"\n検出された顔の数: {faces.Count}");
        foreach (var face in faces)
        {
            Console.WriteLine(face);
        }

        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
        var canvas = surface.Canvas;
        canvas.DrawBitmap(bitmap, 0, 0);

        foreach (var face in faces)
        {
            // スコアに応じて色を黄色(0.5)～赤(1.0)に変化
            var normalizedScore = Math.Clamp((face.Confidence - 0.5f) / 0.5f, 0f, 1f);
            var red = (byte)255;
            var green = (byte)(255 * (1 - normalizedScore));
            var color = new SKColor(red, green, 0);

            using var paint = new SKPaint
            {
                Color = color,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke
            };

            var rect = new SKRect(face.X, face.Y, face.X + face.Width, face.Y + face.Height);
            canvas.DrawRect(rect, paint);

            // スコアをテキストで描画
            using var textPaint = new SKPaint
            {
                Color = color,
                TextSize = 16,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            var scoreText = $"{face.Confidence:F2}";
            var textY = face.Y > 20 ? face.Y - 5 : face.Y + face.Height + 20;
            canvas.DrawText(scoreText, face.X, textY, textPaint);
        }

        var outputPath = @"D:\学習データ\people640x480.output.png";
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);

        Console.WriteLine($"\n結果画像を保存しました: {outputPath}");
    }
}

public sealed class FaceDetector : IDisposable
{
    private readonly InferenceSession _session;
    private const int ModelWidth = 320;
    private const int ModelHeight = 240;

    public FaceDetector(string modelPath)
    {
        var options = new SessionOptions
        {
            LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR,
            EnableCpuMemArena = true,
            EnableMemoryPattern = true,
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
        };

        _session = new InferenceSession(modelPath, options);
    }

    public void Dispose()
    {
        _session?.Dispose();
    }

    public List<FaceBox> Detect(ReadOnlySpan<byte> image, int width, int height, float confidenceThreshold, float iouThreshold)
    {
        var resizedImage = new byte[ModelWidth * ModelHeight * 3];
        ImageResizer.ResizeBilinearScalar(image, resizedImage, width, height, ModelWidth, ModelHeight);

        var inputTensor = new DenseTensor<float>(new[] { 1, 3, ModelHeight, ModelWidth });
        var mean = new[] { 127f, 127f, 127f };
        var scale = 128f;

        for (var y = 0; y < ModelHeight; y++)
        {
            for (var x = 0; x < ModelWidth; x++)
            {
                var idx = (y * ModelWidth + x) * 3;
                inputTensor[0, 0, y, x] = (resizedImage[idx] - mean[0]) / scale;
                inputTensor[0, 1, y, x] = (resizedImage[idx + 1] - mean[1]) / scale;
                inputTensor[0, 2, y, x] = (resizedImage[idx + 2] - mean[2]) / scale;
            }
        }

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(_session.InputMetadata.First().Key, inputTensor)
        };

        using var results = _session.Run(inputs);
        var outputList = results.ToList();

        var scores = outputList[0].AsEnumerable<float>().ToArray();
        var boxes = outputList[1].AsEnumerable<float>().ToArray();

        var scoresDims = (outputList[0].Value as DenseTensor<float>)?.Dimensions.ToArray();
        var numBoxes = scoresDims?[1] ?? 0;

        var detections = new List<FaceBox>();

        for (var i = 0; i < numBoxes; i++)
        {
            var faceScore = scores[i * 2 + 1];

            if (faceScore > confidenceThreshold)
            {
                var x1 = boxes[i * 4] * width;
                var y1 = boxes[i * 4 + 1] * height;
                var x2 = boxes[i * 4 + 2] * width;
                var y2 = boxes[i * 4 + 3] * height;

                detections.Add(new FaceBox
                {
                    X = (int)x1,
                    Y = (int)y1,
                    Width = (int)(x2 - x1),
                    Height = (int)(y2 - y1),
                    Confidence = faceScore
                });
            }
        }

        var nmsResults = ApplyNMS(detections, iouThreshold);

        return nmsResults;
    }

    private static List<FaceBox> ApplyNMS(List<FaceBox> boxes, float iouThreshold)
    {
        var sortedBoxes = boxes.OrderByDescending(b => b.Confidence).ToList();
        var results = new List<FaceBox>();

        while (sortedBoxes.Count > 0)
        {
            var best = sortedBoxes[0];
            results.Add(best);
            sortedBoxes.RemoveAt(0);

            sortedBoxes = sortedBoxes.Where(box => CalculateIOU(best, box) < iouThreshold).ToList();
        }

        return results;
    }

    private static float CalculateIOU(FaceBox box1, FaceBox box2)
    {
        var x1 = Math.Max(box1.X, box2.X);
        var y1 = Math.Max(box1.Y, box2.Y);
        var x2 = Math.Min(box1.X + box1.Width, box2.X + box2.Width);
        var y2 = Math.Min(box1.Y + box1.Height, box2.Y + box2.Height);

        var intersectionArea = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
        var box1Area = box1.Width * box1.Height;
        var box2Area = box2.Width * box2.Height;
        var unionArea = box1Area + box2Area - intersectionArea;

        return unionArea > 0 ? (float)intersectionArea / unionArea : 0;
    }
}

public class FaceBox
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public float Confidence { get; set; }

    public override string ToString()
    {
        return $"Face at ({X}, {Y}) [{Width}x{Height}] - Confidence: {Confidence:F2}";
    }
}
