using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace WorkImageOnyx;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--benchmark")
        {
            BenchmarkRunner.Run<FaceDetectionBenchmark>();
        }
        else
        {
            RunInteractive();
        }
    }

    private static void RunInteractive()
    {
        const float confidenceThreshold = 0.5f;
        const float iouThreshold = 0.3f;

        // 利用可能なモデルを表示
        Console.WriteLine("=== 顔検出モデルの選択 ===");
        Console.WriteLine("1. version-RFB-320.onnx (320x240)");
        Console.WriteLine("2. version-RFB-640.onnx (640x480)");
        Console.WriteLine("3. version-slim-320.onnx (320x240)");
        Console.Write("\n使用するモデルを選択してください (1-3): ");

        var choice = Console.ReadLine();
        var modelPath = choice switch
        {
            "1" => "version-RFB-320.onnx",
            "2" => "version-RFB-640.onnx",
            "3" => "version-slim-320.onnx",
            _ => "version-RFB-320.onnx"
        };

        if (!File.Exists(modelPath))
        {
            Console.WriteLine($"エラー: モデルファイル '{modelPath}' が見つかりません。");
            return;
        }

        Console.WriteLine();

        // 実行オプションの設定
        var useGpu = false;
        var intraOpNumThreads = 0;
        var interOpNumThreads = 0;

        Console.Write("GPU (CUDA) を使用しますか？ (y/n, デフォルト: n): ");
        var gpuChoice = Console.ReadLine()?.Trim().ToLower();
        if (gpuChoice == "y" || gpuChoice == "yes")
        {
            useGpu = true;
        }

        Console.Write("IntraOpスレッド数を指定しますか？ (0=自動, デフォルト: 0): ");
        if (int.TryParse(Console.ReadLine(), out var intraThreads))
        {
            intraOpNumThreads = intraThreads;
        }

        Console.Write("InterOpスレッド数を指定しますか？ (0=自動, デフォルト: 0): ");
        if (int.TryParse(Console.ReadLine(), out var interThreads))
        {
            interOpNumThreads = interThreads;
        }

        Console.WriteLine();

        using var detector = new FaceDetector(modelPath, useGpu, intraOpNumThreads, interOpNumThreads);

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
            // 正規化座標から画像座標に変換
            var left = face.Left * bitmap.Width;
            var top = face.Top * bitmap.Height;
            var right = face.Right * bitmap.Width;
            var bottom = face.Bottom * bitmap.Height;
            var width = right - left;
            var height = bottom - top;

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

            var rect = new SKRect(left, top, right, bottom);
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
            var textY = top > 20 ? top - 5 : bottom + 20;
            canvas.DrawText(scoreText, left, textY, textPaint);
        }

        var outputPath = @"D:\学習データ\people640x480.output.png";
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);

        Console.WriteLine($"\n結果画像を保存しました: {outputPath}");
    }
}

[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class FaceDetectionBenchmark
{
    //private const int N = 30;
    private const int N = 30 * 60;

    private FaceDetector? _detector320;
    private FaceDetector? _detector640;
    private FaceDetector? _detectorSlim320;
    //private FaceDetector? _detector320Gpu;
    //private FaceDetector? _detector640Gpu;
    private byte[]? _imageBytes320;
    private byte[]? _imageBytes640;
    private const float ConfidenceThreshold = 0.5f;
    private const float IouThreshold = 0.3f;

    [GlobalSetup]
    public void Setup()
    {
        // 320x240画像データを準備
        _imageBytes320 = new byte[320 * 240 * 3];
        using (var bitmap = SKBitmap.Decode(@"D:\学習データ\people640x480.png"))
        {
            using var resized = bitmap.Resize(new SKImageInfo(320, 240), SKFilterQuality.High);
            var pixels = resized.Pixels;
            var idx = 0;
            for (var y = 0; y < 240; y++)
            {
                for (var x = 0; x < 320; x++)
                {
                    var pixel = pixels[y * 320 + x];
                    _imageBytes320[idx++] = pixel.Red;
                    _imageBytes320[idx++] = pixel.Green;
                    _imageBytes320[idx++] = pixel.Blue;
                }
            }
        }

        // 640x480画像データを準備
        _imageBytes640 = new byte[640 * 480 * 3];
        using (var bitmap = SKBitmap.Decode(@"D:\学習データ\people640x480.png"))
        {
            var pixels = bitmap.Pixels;
            var idx = 0;
            for (var y = 0; y < 480; y++)
            {
                for (var x = 0; x < 640; x++)
                {
                    var pixel = pixels[y * 640 + x];
                    _imageBytes640[idx++] = pixel.Red;
                    _imageBytes640[idx++] = pixel.Green;
                    _imageBytes640[idx++] = pixel.Blue;
                }
            }
        }

        // モデル初期化
        _detector320 = new FaceDetector("version-RFB-320.onnx", useGpu: false);
        _detector640 = new FaceDetector("version-RFB-640.onnx", useGpu: false);
        _detectorSlim320 = new FaceDetector("version-slim-320.onnx", useGpu: false);

        //// GPU版（利用可能な場合）
        //try
        //{
        //    _detector320Gpu = new FaceDetector("version-RFB-320.onnx", useGpu: true);
        //    _detector640Gpu = new FaceDetector("version-RFB-640.onnx", useGpu: true);
        //}
        //catch
        //{
        //    // GPU利用不可の場合はnullのまま
        //}
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _detector320?.Dispose();
        _detector640?.Dispose();
        _detectorSlim320?.Dispose();
        //_detector320Gpu?.Dispose();
        //_detector640Gpu?.Dispose();
    }

    //[Benchmark(OperationsPerInvoke = N)]
    //public void RFB320_CPU_NoResize()
    //{
    //    for (var i = 0; i < N; i++)
    //    {
    //        _detector320!.Detect(_imageBytes320!, 320, 240, ConfidenceThreshold, IouThreshold);
    //    }
    //}

    [Benchmark(OperationsPerInvoke = N)]
    public void RFB320_CPU_WithResize()
    {
        for (var i = 0; i < N; i++)
        {
            _detector320!.Detect(_imageBytes640!, 640, 480, ConfidenceThreshold, IouThreshold);
        }
    }

    [Benchmark(OperationsPerInvoke = N)]
    public void RFB640_CPU_NoResize()
    {
        for (var i = 0; i < N; i++)
        {
            _detector640!.Detect(_imageBytes640!, 640, 480, ConfidenceThreshold, IouThreshold);
        }
    }

    //[Benchmark(OperationsPerInvoke = N)]
    //public void RFB640_CPU_WithResize()
    //{
    //    for (var i = 0; i < N; i++)
    //    {
    //        _detector640!.Detect(_imageBytes320!, 320, 240, ConfidenceThreshold, IouThreshold);
    //    }
    //}

    //[Benchmark(OperationsPerInvoke = N)]
    //public void Slim320_CPU_NoResize()
    //{
    //    for (var i = 0; i < N; i++)
    //    {
    //        _detectorSlim320!.Detect(_imageBytes320!, 320, 240, ConfidenceThreshold, IouThreshold);
    //    }
    //}

    //[Benchmark(OperationsPerInvoke = N)]
    //public void Slim320_CPU_WithResize()
    //{
    //    for (var i = 0; i < N; i++)
    //    {
    //        _detectorSlim320!.Detect(_imageBytes640!, 640, 480, ConfidenceThreshold, IouThreshold);
    //    }
    //}

    //[Benchmark(OperationsPerInvoke = N)]
    //public List<FaceBox>? RFB320_GPU_NoResize()
    //{
    //    return _detector320Gpu?.Detect(_imageBytes320!, 320, 240, ConfidenceThreshold, IouThreshold);
    //}

    //[Benchmark(OperationsPerInvoke = N)]
    //public List<FaceBox>? RFB640_GPU_NoResize()
    //{
    //    return _detector640Gpu?.Detect(_imageBytes640!, 640, 480, ConfidenceThreshold, IouThreshold);
    //}
}

public sealed class FaceDetector : IDisposable
{
    private readonly InferenceSession _session;
    private readonly DenseTensor<float> _inputTensor;
    private readonly List<float> _scores;
    private readonly List<float> _boxes;
    private readonly List<FaceBox> _detectionBuffer;

    public int ModelWidth { get; }
    public int ModelHeight { get; }

    public FaceDetector(string modelPath, bool useGpu = false, int intraOpNumThreads = 0, int interOpNumThreads = 0)
    {
        var options = new SessionOptions
        {
            LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR,
            EnableCpuMemArena = true,
            EnableMemoryPattern = true,
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
        };

        // パラレル実行のスレッド数設定
        if (intraOpNumThreads > 0)
        {
            options.IntraOpNumThreads = intraOpNumThreads;
        }
        if (interOpNumThreads > 0)
        {
            options.InterOpNumThreads = interOpNumThreads;
        }

        // GPU使用設定
        if (useGpu)
        {
            try
            {
                options.AppendExecutionProvider_CUDA(0);
                Console.WriteLine("GPU (CUDA) を使用します。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GPU (CUDA) の初期化に失敗しました: {ex.Message}");
                Console.WriteLine("CPUで実行します。");
            }
        }

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

        // 再利用するListを初期化（十分な容量を確保）
        _scores = new List<float>(10000);
        _boxes = new List<float>(10000);
        _detectionBuffer = new List<FaceBox>(100);
    }

    public void Dispose()
    {
        _session?.Dispose();
    }

    public List<FaceBox> Detect(ReadOnlySpan<byte> image, int width, int height, float confidenceThreshold, float iouThreshold)
    {
        var mean = new[] { 127f, 127f, 127f };
        var scale = 128f;

        // サイズが一致する場合はリサイズ不要
        if (width == ModelWidth && height == ModelHeight)
        {
            CopyDirectToTensor(image, _inputTensor, width, height, mean, scale);
        }
        else
        {
            ResizeBilinearDirectToTensor(image, _inputTensor, width, height, ModelWidth, ModelHeight, mean, scale);
        }

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(_session.InputMetadata.First().Key, _inputTensor)
        };

        using var results = _session.Run(inputs);
        var outputList = results.ToList();

        // 再利用するListをクリアしてからデータをコピー
        _scores.Clear();
        _boxes.Clear();

        foreach (var score in outputList[0].AsEnumerable<float>())
        {
            _scores.Add(score);
        }

        foreach (var box in outputList[1].AsEnumerable<float>())
        {
            _boxes.Add(box);
        }

        var s = (outputList[0].Value as DenseTensor<float>)?.Dimensions[1];

        var scoresDims = (outputList[0].Value as DenseTensor<float>)?.Dimensions.ToArray();
        var numBoxes = scoresDims?[1] ?? 0;

        _detectionBuffer.Clear();

        for (var i = 0; i < numBoxes; i++)
        {
            var faceScore = _scores[i * 2 + 1];

            if (faceScore > confidenceThreshold)
            {
                _detectionBuffer.Add(new FaceBox
                {
                    Left = _boxes[i * 4],
                    Top = _boxes[i * 4 + 1],
                    Right = _boxes[i * 4 + 2],
                    Bottom = _boxes[i * 4 + 3],
                    Confidence = faceScore
                });
            }
        }

        var nmsResults = ApplyNMS(_detectionBuffer, iouThreshold);

        return nmsResults;
    }

    private static List<FaceBox> ApplyNMS(List<FaceBox> boxes, float iouThreshold)
    {
        if (boxes.Count == 0)
        {
            return new List<FaceBox>();
        }

        var count = boxes.Count;

        // FaceBox配列を直接ソート（インプレース）
        var boxArray = ArrayPool<FaceBox>.Shared.Rent(count);
        var suppressed = ArrayPool<bool>.Shared.Rent(count);

        try
        {
            // Listから配列にコピー
            for (var i = 0; i < count; i++)
            {
                boxArray[i] = boxes[i];
                suppressed[i] = false;
            }

            // FaceBox配列をConfidence降順でソート
            Array.Sort(boxArray, 0, count, FaceBox.ConfidenceComparer);

            var results = new List<FaceBox>();

            for (var i = 0; i < count; i++)
            {
                if (suppressed[i])
                {
                    continue;
                }

                ref readonly var currentBox = ref boxArray[i];
                results.Add(currentBox);

                // 残りのボックスとのIOUをチェック
                for (var j = i + 1; j < count; j++)
                {
                    if (suppressed[j])
                    {
                        continue;
                    }

                    if (CalculateIOU(in currentBox, in boxArray[j]) >= iouThreshold)
                    {
                        suppressed[j] = true;
                    }
                }
            }

            return results;
        }
        finally
        {
            ArrayPool<FaceBox>.Shared.Return(boxArray);
            ArrayPool<bool>.Shared.Return(suppressed);
        }
    }

    private static float CalculateIOU(in FaceBox box1, in FaceBox box2)
    {
        var x1 = Math.Max(box1.Left, box2.Left);
        var y1 = Math.Max(box1.Top, box2.Top);
        var x2 = Math.Min(box1.Right, box2.Right);
        var y2 = Math.Min(box1.Bottom, box2.Bottom);

        var intersectionArea = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
        var box1Area = (box1.Right - box1.Left) * (box1.Bottom - box1.Top);
        var box2Area = (box2.Right - box2.Left) * (box2.Bottom - box2.Top);
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

    private static void CopyDirectToTensor(ReadOnlySpan<byte> source, DenseTensor<float> tensor, int width, int height, float[] mean, float scale)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var idx = (y * width + x) * 3;

                tensor[0, 0, y, x] = (source[idx] - mean[0]) / scale;
                tensor[0, 1, y, x] = (source[idx + 1] - mean[1]) / scale;
                tensor[0, 2, y, x] = (source[idx + 2] - mean[2]) / scale;
            }
        }
    }

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

public readonly struct FaceBox
{
    public float Left { get; init; }
    public float Top { get; init; }
    public float Right { get; init; }
    public float Bottom { get; init; }
    public float Confidence { get; init; }

    public override string ToString()
    {
        return $"Face at ({Left:F4}, {Top:F4}) to ({Right:F4}, {Bottom:F4}) - Confidence: {Confidence:F2}";
    }

    // FaceBox専用のConfidence降順Comparer
    private sealed class ConfidenceDescendingComparer : IComparer<FaceBox>
    {
        public int Compare(FaceBox x, FaceBox y)
        {
            // FaceBoxはstructなので参照比較は不要
            return y.Confidence.CompareTo(x.Confidence);
        }
    }

    private static readonly ConfidenceDescendingComparer s_confidenceComparer = new();

    public static IComparer<FaceBox> ConfidenceComparer => s_confidenceComparer;
}
