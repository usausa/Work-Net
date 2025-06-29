using System.Diagnostics;
using SkiaSharp;

using System.Runtime.InteropServices;

using var bitmap = SKBitmap.Decode("rgb.png");

var results = ColorAnalyzer.Build(new SkiaImageSource(bitmap));
Debug.WriteLine(results.Count);

public interface IImageSource
{
    int Width { get; }
    int Height { get; }

    (byte R, byte G, byte B) GetColor(int x, int y);
}

public sealed class SkiaImageSource : IImageSource
{
    private readonly SKBitmap bitmap;

    public SkiaImageSource(SKBitmap bitmap)
    {
        this.bitmap = bitmap;
    }
    public int Width => bitmap.Width;

    public int Height => bitmap.Height;

    public (byte R, byte G, byte B) GetColor(int x, int y)
    {
        var pixel = bitmap.GetPixel(x, y);
        return (pixel.Red, pixel.Green, pixel.Blue);
    }
}

public class ColorAnalyzer
{
    private const int Seed = 123456789; // 固定のシード値

    public static List<ColorCount> Build(IImageSource source, int redResolution = 5, int greenResolution = 6, int blueResolution = 5, int numberOfClusters = 50, int maxIterations = 50)
    {
        var redCount = (1 << redResolution);
        var greenCount = (1 << greenResolution);
        var blueCount = (1 << blueResolution);

        var table = new int[redCount * greenCount * blueCount];

        var redReduce = 8 - redResolution;
        var greenReduce = 8 - greenResolution;
        var blueReduce = 8 - blueResolution;
        int redMask = (1 << redResolution) - 1;
        int greenMask = (1 << greenResolution) - 1;
        int blueMask = (1 << blueResolution) - 1;
        var redShift = greenResolution + blueResolution;
        var greenShift = blueResolution;


        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var color = source.GetColor(x, y);
                var index = (color.R >> redReduce << redShift) | (color.G >> greenReduce << greenShift) | (color.B >> blueReduce);
                ref var count = ref table[index];
                count++;
            }
        }

        var points = table.Count(static x => x > 0);
        var dataPoints = new double[points][];
        var dataPointsIndex = 0;
        for (var i = 0; i < table.Length; i++)
        {
            var c = table[i];
            if (c > 0)
            {
                int r = (c >> redShift) & redMask;
                int g = (c >> greenShift) & greenMask;
                int b = c & blueMask;
                dataPoints[dataPointsIndex] = [r, g, b];
                dataPointsIndex++;
            }
        }

        Random rand = new Random(Seed);

        // --- ここからK-Means++の初期化ロジック ---
        var centroids = new double[numberOfClusters][];
        List<int> chosenIndices = new List<int>(); // 既に選ばれたデータポイントのインデックスを追跡

        // 1. 最初の中心をランダムに選択
        int firstCentroidIndex = rand.Next(dataPoints.Length);
        centroids[0] = dataPoints[firstCentroidIndex];
        chosenIndices.Add(firstCentroidIndex);

        // 残りの中心を選択
        for (int k = 1; k < numberOfClusters; k++)
        {
            double[] minSquaredDistances = new double[dataPoints.Length];
            double totalSquaredDistance = 0.0;

            // 2. 各データポイントと、既に選択されている中心群の最小距離の2乗を計算
            for (int i = 0; i < dataPoints.Length; i++)
            {
                // 既に中心として選ばれた点はスキップ (ただし、確率計算には含める)
                // if (chosenIndices.Contains(i)) continue; // これをすると計算がおかしくなるので注意

                double minDistSq = double.MaxValue;
                for (int j = 0; j < k; j++) // 既に選択されている k 個の中心と比較
                {
                    double distSq = CalculateSquaredEuclideanDistance(dataPoints[i], centroids[j]);
                    if (distSq < minDistSq)
                    {
                        minDistSq = distSq;
                    }
                }
                minSquaredDistances[i] = minDistSq;
                totalSquaredDistance += minDistSq;
            }

            // 3. 距離の2乗に比例する確率で次の中心を選択
            // もし全ての点が既に中心として選ばれているか、全距離の合計が0ならループを抜ける (起こる可能性は低い)
            if (totalSquaredDistance == 0)
            {
                // 全ての点が既に中心として選ばれているか、全て同じ座標の場合
                // 残りの中心をランダムに選ぶか、処理を終了する
                for (int i = k; i < numberOfClusters; i++)
                {
                    centroids[i] = dataPoints[rand.Next(dataPoints.Length)];
                }
                break;
            }

            double r = rand.NextDouble() * totalSquaredDistance;
            double cumulativeProbability = 0.0;
            int nextCentroidIndex = -1;

            for (int i = 0; i < dataPoints.Length; i++)
            {
                cumulativeProbability += minSquaredDistances[i];
                if (cumulativeProbability >= r)
                {
                    nextCentroidIndex = i;
                    break;
                }
            }

            // もし何らかの理由で選べなかった場合、ランダムに選ぶ（フォールバック）
            if (nextCentroidIndex == -1 || chosenIndices.Contains(nextCentroidIndex))
            {
                // 既に選択済みの場合や、確率的に選べなかった場合は、まだ選ばれていない中からランダムに選択
                do
                {
                    nextCentroidIndex = rand.Next(dataPoints.Length);
                } while (chosenIndices.Contains(nextCentroidIndex));
            }

            centroids[k] = dataPoints[nextCentroidIndex];
            chosenIndices.Add(nextCentroidIndex);
        }
        // --- K-Means++の初期化ロジックここまで ---

        int[] assignments = new int[dataPoints.Length];
        bool changed = true;
        int iteration = 0;

        while (changed && iteration < maxIterations)
        {
            changed = false;

            // 各データポイントを最も近いクラスターに割り当て
            for (int i = 0; i < dataPoints.Length; i++)
            {
                int closestCentroidIndex = GetClosestCentroid(dataPoints[i], centroids);
                if (assignments[i] != closestCentroidIndex)
                {
                    assignments[i] = closestCentroidIndex;
                    changed = true;
                }
            }

            // 新しいクラスター中心の計算
            var newCentroids = new double[numberOfClusters][];
            var clusterCounts = new int[numberOfClusters];

            for (int i = 0; i < numberOfClusters; i++)
            {
                newCentroids[i] = new double[3]; // R, G, B
            }

            for (int i = 0; i < dataPoints.Length; i++)
            {
                int clusterIndex = assignments[i];
                newCentroids[clusterIndex][0] += dataPoints[i][0]; // R
                newCentroids[clusterIndex][1] += dataPoints[i][1]; // G
                newCentroids[clusterIndex][2] += dataPoints[i][2]; // B
                clusterCounts[clusterIndex]++;
            }

            for (int i = 0; i < numberOfClusters; i++)
            {
                if (clusterCounts[i] > 0)
                {
                    newCentroids[i][0] /= clusterCounts[i];
                    newCentroids[i][1] /= clusterCounts[i];
                    newCentroids[i][2] /= clusterCounts[i];
                }
                else
                {
                    // クラスターが空になった場合、新しいランダムな点に中心を再割り当て
                    // これにより、空のクラスターが続くのを防ぎ、より良い収束を促す
                    newCentroids[i] = dataPoints[rand.Next(dataPoints.Length)];
                }
            }
            centroids = newCentroids;
            iteration++;
        }

        // クラスタリング結果の集計
        var clusteredColors = new List<ColorCount>();
        var aggregatedCounts = new Dictionary<int, int>();

        for (int i = 0; i < dataPoints.Length; i++)
        {
            int clusterIndex = assignments[i];
            aggregatedCounts.TryAdd(clusterIndex, 0);
            // TODO fix
            aggregatedCounts[clusterIndex] += table[i];
        }

        for (int i = 0; i < numberOfClusters; i++)
        {
            if (aggregatedCounts.TryGetValue(i, out var count))
            {
                clusteredColors.Add(new ColorCount
                {
                    R = (byte)Math.Round(centroids[i][0]),
                    G = (byte)Math.Round(centroids[i][1]),
                    B = (byte)Math.Round(centroids[i][2]),
                    Count = count
                });
            }
            else
            {
                // クラスタリングの結果、割り当てられなかった空のクラスターはスキップ
                // または、その中心だけをCount=0で追加することも可能
            }
        }

        return clusteredColors;
    }

    /// <summary>
    /// ユークリッド距離を計算し、最も近い中心のインデックスを返す
    /// </summary>
    private static int GetClosestCentroid(double[] dataPoint, double[][] centroids)
    {
        double minDistance = double.MaxValue;
        int closestCentroidIndex = -1;

        for (int i = 0; i < centroids.Length; i++)
        {
            double distance = CalculateEuclideanDistance(dataPoint, centroids[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestCentroidIndex = i;
            }
        }
        return closestCentroidIndex;
    }

    /// <summary>
    /// 2点間のユークリッド距離の2乗を計算 (K-Means++で利用)
    /// </summary>
    private static double CalculateSquaredEuclideanDistance(double[] point1, double[] point2)
    {
        double sumOfSquares = 0.0;
        for (int i = 0; i < point1.Length; i++)
        {
            sumOfSquares += Math.Pow(point1[i] - point2[i], 2);
        }
        return sumOfSquares; // ここで平方根を取らない
    }

    /// <summary>
    /// 2点間のユークリッド距離を計算 (K-Means割り当てで利用)
    /// </summary>
    private static double CalculateEuclideanDistance(double[] point1, double[] point2)
    {
        return Math.Sqrt(CalculateSquaredEuclideanDistance(point1, point2));
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ColorCount
{
    public byte R;
    public byte G;
    public byte B;
    public int Count;
}
