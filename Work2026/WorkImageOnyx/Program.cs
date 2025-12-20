using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace WorkImageOnyx;

public static class Program
{
    public static void Main()
    {
        const float confidenceThreshold = 0.5f;
        const float iouThreshold = 0.3f;

        // 利用可能なモデルを表示
        Console.WriteLine("=== 顔検出モデルの選択 ===");
        Console.WriteLine("1. version-RFB-320.onnx (320x240)");
        Console.WriteLine("2. version-RFB-640.onnx (640x480)");
        Console.WriteLine("3. version-slim-320.onnx (320x240)");
        Console.WriteLine("4. version-RFB-320_simplified.onnx (320x240)");
        Console.WriteLine("5. version-RFB-320_without_postprocessing.onnx (320x240)");
        Console.Write("\n使用するモデルを選択してください (1-5): ");

        var choice = Console.ReadLine();
        var modelPath = choice switch
        {
            "1" => "version-RFB-320.onnx",
            "2" => "version-RFB-640.onnx",
            "3" => "version-slim-320.onnx",
            "4" => "version-RFB-320_simplified.onnx",
            "5" => "version-RFB-320_without_postprocessing.onnx",
            _ => "version-RFB-320.onnx"
        };

        if (!File.Exists(modelPath))
        {
            Console.WriteLine($"エラー: モデルファイル '{modelPath}' が見つかりません。");
            return;
        }

        Console.WriteLine();

        using var detector = new FaceDetector(modelPath);

        var imagePath = @"D:\学習データ\people640x480.png";
        using var bitmap = SKBitmap.Decode(imagePath);
        if (bitmap == null)
        {
            Console.WriteLine("画像の読み込みに失敗しました。");
            return;
        }

        Console.WriteLine($"使用モデル: {modelPath}");
        Console.WriteLine($"モデル入力サイズ: {detector.ModelWidth}x{detector.ModelHeight}");
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

        var stopwatch = Stopwatch.StartNew();
        var faces = detector.Detect(imageBytes, bitmap.Width, bitmap.Height, confidenceThreshold, iouThreshold);
        stopwatch.Stop();

        Console.WriteLine($"\n検出処理時間: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.Elapsed.TotalSeconds:F3} 秒)");
        Console.WriteLine($"検出された顔の数: {faces.Count}");
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
    private readonly DenseTensor<float> _inputTensor;
    
    public int ModelWidth { get; }
    public int ModelHeight { get; }

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

        // ONNXモデルから入力サイズを取得
        var inputMetadata = _session.InputMetadata.First().Value;
        var dimensions = inputMetadata.Dimensions;
        
        // 入力テンソルの形状は [batch, channels, height, width] を想定
        if (dimensions.Length >= 4)
        {
            ModelHeight = dimensions[2];
            ModelWidth = dimensions[3];
        }
        else
        {
            throw new InvalidOperationException("モデルの入力テンソルの形状が想定と異なります。");
        }

        _inputTensor = new DenseTensor<float>(new[] { 1, 3, ModelHeight, ModelWidth });
    }

    public void Dispose()
    {
        _session?.Dispose();
    }

    public List<FaceBox> Detect(ReadOnlySpan<byte> image, int width, int height, float confidenceThreshold, float iouThreshold)
    {
        var mean = new[] { 127f, 127f, 127f };
        var scale = 128f;

        ResizeBilinearDirectToTensor(image, _inputTensor, width, height, ModelWidth, ModelHeight, mean, scale);

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(_session.InputMetadata.First().Key, _inputTensor)
        };

        using var results = _session.Run(inputs);
        var outputList = results.ToList();

        var scores = outputList[0].AsEnumerable<float>().ToList();
        var boxes = outputList[1].AsEnumerable<float>().ToList();

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

    //--------------------------------------------------------------------------------
    // ヘルパー
    //--------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Min(int a, int b) => a < b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Clamp(float value)
    {
        if (value < 0)
        {
            return 0;
        }
        if (value > 255)
        {
            return 255;
        }
        return (byte)value;
    }

    //--------------------------------------------------------------------------------
    // スカラー処理
    //--------------------------------------------------------------------------------

    private static void ResizeBilinearDirectToTensor(ReadOnlySpan<byte> source, DenseTensor<float> tensor, int srcWidth, int srcHeight, int dstWidth, int dstHeight, float[] mean, float scale)
    {
        var xRatio = (float)(srcWidth - 1) / dstWidth;
        var yRatio = (float)(srcHeight - 1) / dstHeight;

        for (var y = 0; y < dstHeight; y++)
        {
            var srcY = y * yRatio;
            var srcYInt = (int)srcY;
            var yDiff = srcY - srcYInt;
            var yDiffInv = 1.0f - yDiff;
            var srcY1 = Min(srcYInt + 1, srcHeight - 1);

            var srcRow0 = srcYInt * srcWidth * 3;
            var srcRow1 = srcY1 * srcWidth * 3;

            for (var x = 0; x < dstWidth; x++)
            {
                var srcX = x * xRatio;
                var srcXInt = (int)srcX;
                var xDiff = srcX - srcXInt;
                var xDiffInv = 1.0f - xDiff;
                var srcX1 = Min(srcXInt + 1, srcWidth - 1);

                var srcCol0 = srcXInt * 3;
                var srcCol1 = srcX1 * 3;

                var idx00 = srcRow0 + srcCol0;
                var idx10 = srcRow0 + srcCol1;
                var idx01 = srcRow1 + srcCol0;
                var idx11 = srcRow1 + srcCol1;

                var w00 = xDiffInv * yDiffInv;
                var w10 = xDiff * yDiffInv;
                var w01 = xDiffInv * yDiff;
                var w11 = xDiff * yDiff;

                // R channel
                var valR =
                    source[idx00] * w00 +
                    source[idx10] * w10 +
                    source[idx01] * w01 +
                    source[idx11] * w11;
                tensor[0, 0, y, x] = (valR - mean[0]) / scale;

                // G channel
                var valG =
                    source[idx00 + 1] * w00 +
                    source[idx10 + 1] * w10 +
                    source[idx01 + 1] * w01 +
                    source[idx11 + 1] * w11;
                tensor[0, 1, y, x] = (valG - mean[1]) / scale;

                // B channel
                var valB =
                    source[idx00 + 2] * w00 +
                    source[idx10 + 2] * w10 +
                    source[idx01 + 2] * w01 +
                    source[idx11 + 2] * w11;
                tensor[0, 2, y, x] = (valB - mean[2]) / scale;
            }
        }
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
