using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public struct ColorCount
{
    public byte R;
    public byte G;
    public byte B;
    public int Count;
}

public class KMeansColorClustering
{
    public static List<ColorCount> ClusterColors(ColorCount[] originalColors, int numberOfClusters, int maxIterations = 100, int? randomSeed = null)
    {
        if (originalColors == null || originalColors.Length == 0)
        {
            return new List<ColorCount>();
        }

        var dataPoints = originalColors.Select(c => new double[] { c.R, c.G, c.B }).ToArray();
        Random rand = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();

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
            aggregatedCounts[clusterIndex] += originalColors[i].Count;
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

// 使用例 (変更なし)
public class Program
{
    public static void Main(string[] args)
    {
        // サンプルのColorCount配列 (実際にはもっと多くのデータ)
        ColorCount[] originalColors = new ColorCount[]
        {
            new ColorCount { R = 255, G = 0, B = 0, Count = 10 },    // 赤
            new ColorCount { R = 250, G = 10, B = 10, Count = 5 },   // 赤に近い
            new ColorCount { R = 0, G = 255, B = 0, Count = 20 },    // 緑
            new ColorCount { R = 10, G = 240, B = 5, Count = 15 },   // 緑に近い
            new ColorCount { R = 0, G = 0, B = 255, Count = 30 },    // 青
            new ColorCount { R = 5, G = 5, B = 245, Count = 25 },    // 青に近い
            new ColorCount { R = 128, G = 128, B = 128, Count = 50 },// グレー
            new ColorCount { R = 130, G = 130, B = 130, Count = 45 },// グレーに近い
            new ColorCount { R = 255, G = 255, B = 0, Count = 8 },   // 黄色
            new ColorCount { R = 240, G = 250, B = 5, Count = 7 },   // 黄色に近い
            new ColorCount { R = 255, G = 0, B = 255, Count = 12 },  // マゼンタ
            new ColorCount { R = 0, G = 255, B = 255, Count = 18 },  // シアン
            // さらに多くの色データを追加...
            new ColorCount { R = 200, G = 50, B = 50, Count = 3 },
            new ColorCount { R = 210, G = 40, B = 40, Count = 2 },
            new ColorCount { R = 30, G = 180, B = 60, Count = 9 },
            new ColorCount { R = 40, G = 170, B = 70, Count = 6 },
            new ColorCount { R = 70, G = 70, B = 200, Count = 11 },
            new ColorCount { R = 60, G = 60, B = 210, Count = 14 },
            new ColorCount { R = 100, G = 100, B = 100, Count = 22 },
            new ColorCount { R = 110, G = 110, B = 110, Count = 19 },
            new ColorCount { R = 230, G = 230, B = 30, Count = 13 },
            new ColorCount { R = 220, G = 240, B = 20, Count = 10 },
            new ColorCount { R = 250, G = 10, B = 240, Count = 16 },
            new ColorCount { R = 10, G = 240, B = 250, Count = 21 }
        };

        int desiredClusters = 5; // 例として5つのクラスター
        int fixedSeed = 123;     // K-Means++でもシードを固定することで再現性を確保

        Debug.WriteLine($"Original colors count: {originalColors.Length}");

        // K-Means++でクラスタリングを実行 (シードを渡す)
        List<ColorCount> clusteredResult = KMeansColorClustering.ClusterColors(originalColors, desiredClusters, randomSeed: fixedSeed);

        Debug.WriteLine($"\nClustered colors count: {clusteredResult.Count}");
        Debug.WriteLine("Clustered Colors (R, G, B, Total Count):");
        foreach (var color in clusteredResult.OrderBy(x => x.R).ThenBy(x => x.G).ThenBy(x => x.B))
        {
            Debug.WriteLine($"R:{color.R}, G:{color.G}, B:{color.B}, Count:{color.Count}");
        }
    }
}
