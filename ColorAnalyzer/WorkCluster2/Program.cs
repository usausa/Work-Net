using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public struct ColorCount
{
    public byte R;
    public byte G;
    public byte B;
    public int Count; // DBSCANでは直接使わないが、クラスタリング後の集計に利用
}

public enum PointType
{
    Noise,       // ノイズ点
    Core,        // コア点
    Border       // 境界点
}

public class DbscanColorClustering
{
    // 各データポイントの状態を管理するための内部クラス
    private class DbscanPoint
    {
        public double[] Coordinates { get; set; } // R, G, B
        public int OriginalIndex { get; set; }    // originalColors配列内の元のインデックス
        public int ClusterId { get; set; }        // 所属するクラスターID (-1: ノイズ, その他: クラスターID)
        public bool IsVisited { get; set; }       // 訪問済みフラグ
        public PointType Type { get; set; }       // 点のタイプ (コア、境界、ノイズ)

        public DbscanPoint(double[] coords, int originalIndex)
        {
            Coordinates = coords;
            OriginalIndex = originalIndex;
            ClusterId = -1; // 初期値は未割り当て
            IsVisited = false;
            Type = PointType.Noise; // 初期値はノイズ
        }
    }

    /// <summary>
    /// DBSCANクラスタリングを実行し、クラスタリングされたColorCountのリストを返します。
    /// </summary>
    /// <param name="originalColors">元のColorCount配列。</param>
    /// <param name="epsilon">近傍とみなす最大距離 (R,G,B空間でのユークリッド距離)。</param>
    /// <param name="minPts">コア点とみなすための最小近傍点数（自分自身を含む）。</param>
    /// <returns>クラスタリングされたColorCountのリスト。</returns>
    public static List<ColorCount> ClusterColors(ColorCount[] originalColors, double epsilon, int minPts)
    {
        if (originalColors == null || originalColors.Length == 0)
        {
            return new List<ColorCount>();
        }

        // DbscanPointのリストを作成
        var dbscanPoints = new List<DbscanPoint>();
        for (int i = 0; i < originalColors.Length; i++)
        {
            dbscanPoints.Add(new DbscanPoint(new double[] { originalColors[i].R, originalColors[i].G, originalColors[i].B }, i));
        }

        int clusterId = 0; // 現在のクラスターID

        // 全ての点を走査
        foreach (var point in dbscanPoints)
        {
            if (point.IsVisited)
            {
                continue; // 既に訪問済みならスキップ
            }

            point.IsVisited = true;

            // 近傍点を探索
            List<DbscanPoint> neighbors = GetNeighbors(point, dbscanPoints, epsilon);

            // minPts未満ならノイズ点
            if (neighbors.Count < minPts)
            {
                point.Type = PointType.Noise;
            }
            else // minPts以上ならコア点、新しいクラスターを形成
            {
                point.Type = PointType.Core;
                point.ClusterId = clusterId;
                ExpandCluster(point, neighbors, dbscanPoints, epsilon, minPts, clusterId);
                clusterId++; // 次のクラスターIDへ
            }
        }

        // クラスタリング結果の集計
        var clusteredColors = new List<ColorCount>();
        var clusterAggregations = new Dictionary<int, (double rSum, double gSum, double bSum, int countSum, int pointCount)>();

        foreach (var point in dbscanPoints)
        {
            if (point.ClusterId != -1) // クラスターに属する点のみ
            {
                if (!clusterAggregations.ContainsKey(point.ClusterId))
                {
                    clusterAggregations[point.ClusterId] = (0, 0, 0, 0, 0);
                }

                var current = clusterAggregations[point.ClusterId];
                current.rSum += point.Coordinates[0];
                current.gSum += point.Coordinates[1];
                current.bSum += point.Coordinates[2];
                current.countSum += originalColors[point.OriginalIndex].Count;
                current.pointCount++;
                clusterAggregations[point.ClusterId] = current;
            }
        }

        foreach (var entry in clusterAggregations.OrderBy(e => e.Key))
        {
            var data = entry.Value;
            clusteredColors.Add(new ColorCount
            {
                R = (byte)Math.Round(data.rSum / data.pointCount),
                G = (byte)Math.Round(data.gSum / data.pointCount),
                B = (byte)Math.Round(data.bSum / data.pointCount),
                Count = data.countSum
            });
        }

        return clusteredColors;
    }

    /// <summary>
    /// 指定された点の近傍にある全ての点を取得します。
    /// </summary>
    private static List<DbscanPoint> GetNeighbors(DbscanPoint p, List<DbscanPoint> points, double epsilon)
    {
        List<DbscanPoint> neighbors = new List<DbscanPoint>();
        foreach (var otherP in points)
        {
            if (p != otherP && CalculateEuclideanDistance(p.Coordinates, otherP.Coordinates) <= epsilon)
            {
                neighbors.Add(otherP);
            }
        }
        return neighbors;
    }

    /// <summary>
    /// クラスターを拡張します。
    /// </summary>
    private static void ExpandCluster(DbscanPoint corePoint, List<DbscanPoint> neighbors, List<DbscanPoint> allPoints, double epsilon, int minPts, int clusterId)
    {
        Queue<DbscanPoint> queue = new Queue<DbscanPoint>(neighbors); // BFSのために近傍点をキューに追加

        while (queue.Count > 0)
        {
            DbscanPoint current = queue.Dequeue();

            if (!current.IsVisited) // 未訪問の点であれば処理
            {
                current.IsVisited = true;
                List<DbscanPoint> currentNeighbors = GetNeighbors(current, allPoints, epsilon);

                if (currentNeighbors.Count >= minPts) // コア点であれば
                {
                    current.Type = PointType.Core;
                    foreach (var neighbor in currentNeighbors)
                    {
                        if (neighbor.ClusterId == -1) // まだクラスターに属していない点なら
                        {
                            neighbor.ClusterId = clusterId;
                            queue.Enqueue(neighbor); // キューに追加してさらに拡張
                        }
                    }
                }
                else // 境界点であれば
                {
                    current.Type = PointType.Border;
                }
            }

            if (current.ClusterId == -1) // 訪問済みだがまだクラスターに属していない場合（境界点になる可能性）
            {
                current.ClusterId = clusterId;
            }
        }
    }

    /// <summary>
    /// 2点間のユークリッド距離を計算します。
    /// </summary>
    private static double CalculateEuclideanDistance(double[] point1, double[] point2)
    {
        double sumOfSquares = 0.0;
        for (int i = 0; i < point1.Length; i++)
        {
            sumOfSquares += Math.Pow(point1[i] - point2[i], 2);
        }
        return Math.Sqrt(sumOfSquares);
    }
}

// 使用例
public class Program
{
    public static void Main(string[] args)
    {
        // サンプルのColorCount配列 (実際にはもっと多くのデータ)
        ColorCount[] originalColors = new ColorCount[]
        {
            new ColorCount { R = 255, G = 0, B = 0, Count = 10 },    // 赤1
            new ColorCount { R = 250, G = 5, B = 5, Count = 5 },     // 赤2
            new ColorCount { R = 245, G = 10, B = 10, Count = 3 },   // 赤3
            new ColorCount { R = 0, G = 255, B = 0, Count = 20 },    // 緑1
            new ColorCount { R = 5, G = 250, B = 5, Count = 15 },    // 緑2
            new ColorCount { R = 10, G = 245, B = 10, Count = 12 },  // 緑3
            new ColorCount { R = 0, G = 0, B = 255, Count = 30 },    // 青1
            new ColorCount { R = 5, G = 5, B = 250, Count = 25 },    // 青2
            new ColorCount { R = 128, G = 128, B = 128, Count = 50 },// グレー1
            new ColorCount { R = 130, G = 130, B = 130, Count = 45 },// グレー2
            new ColorCount { R = 255, G = 255, B = 0, Count = 8 },   // 黄色1
            new ColorCount { R = 250, G = 250, B = 5, Count = 7 },   // 黄色2
            new ColorCount { R = 100, G = 100, B = 200, Count = 5 }, // 紫っぽいノイズ1 (遠い)
            new ColorCount { R = 150, G = 50, B = 50, Count = 2 },   // 別のノイズ点 (遠い)
            new ColorCount { R = 20, G = 20, B = 20, Count = 1 },    // ほぼ黒 (ノイズの可能性)
            new ColorCount { R = 220, G = 220, B = 220, Count = 6 }  // ほぼ白
        };

        // DBSCANのパラメータ設定
        // epsilon: 色空間での距離。例えば、10はRGB各成分で最大10の差がある点が近傍とみなされる。
        //          (255,0,0)と(245,10,10)の距離は約14.14。
        // minPts:  1つのクラスターを形成するために必要な点の最小数。
        double epsilon = 15.0; // 例: RGB空間での許容距離
        int minPts = 3;        // 例: 3点以上でコア点とみなす

        Debug.WriteLine($"Original colors count: {originalColors.Length}");
        Debug.WriteLine($"DBSCAN Parameters: Epsilon = {epsilon}, MinPts = {minPts}");

        List<ColorCount> clusteredResult = DbscanColorClustering.ClusterColors(originalColors, epsilon, minPts);

        Debug.WriteLine($"\nClustered colors count: {clusteredResult.Count}");
        Debug.WriteLine("Clustered Colors (R, G, B, Total Count):");
        foreach (var color in clusteredResult)
        {
            Debug.WriteLine($"R:{color.R}, G:{color.G}, B:{color.B}, Count:{color.Count}");
        }

        // ノイズ点も表示したい場合は、DbscanPointリストを外部で保持・参照する必要がある
        // または、DbscanColorClusteringクラスからノイズ点リストを返すように修正
    }
}
