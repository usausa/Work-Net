using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SkiaSharp;
using Accord.MachineLearning;

namespace ColorClusteringSample
{
    // 各色の統計を表すレコード
    public record ColorCount(
        byte R,
        byte G,
        byte B,
        int Count
    );

    public static class ColorClusteringService
    {
        /// <summary>
        /// SKBitmap 内の色をクラスタリングし、代表色と画素数を返します。
        /// Parallel.For は使わず、シンプルなループで実装しています。
        /// </summary>
        /// <param name="bitmap">入力画像の SKBitmap</param>
        /// <param name="maxClusters">最大クラスタ数（例: 32）</param>
        /// <param name="sampleStep">サンプリング間引きステップ（例: 4）</param>
        /// <param name="maxIterations">KMeans の最大反復回数（例: 50）</param>
        /// <param name="tolerance">KMeans の収束閾値（例: 1e-3）</param>
        /// <returns>ColorCount のリスト（Count 降順）</returns>
        public static List<ColorCount> ClusterColors(
            SKBitmap bitmap,
            int maxClusters,
            int sampleStep,
            int maxIterations,
            double tolerance)
        {
            if (bitmap is null)
                throw new ArgumentNullException(nameof(bitmap));
            if (maxClusters < 1)
                throw new ArgumentOutOfRangeException(nameof(maxClusters));
            if (sampleStep < 1)
                throw new ArgumentOutOfRangeException(nameof(sampleStep));

            int width = bitmap.Width;
            int height = bitmap.Height;

            // サンプリングしてピクセルデータを収集（シーケンシャル）
            var pixels = new List<double[]>(width * height / (sampleStep * sampleStep) + 1);
            for (int y = 0; y < height; y += sampleStep)
            {
                for (int x = 0; x < width; x += sampleStep)
                {
                    var c = bitmap.GetPixel(x, y);
                    pixels.Add(new[] { (double)c.Red, c.Green, c.Blue });
                }
            }

            var observations = pixels.ToArray();
            int actualClusters = Math.Min(maxClusters, observations.Length);
            if (actualClusters < 1)
                return new List<ColorCount>();

            // KMeans を実行
            var kmeans = new KMeans(actualClusters)
            {
                MaxIterations = maxIterations,
                Tolerance = tolerance
            };
            var clusters = kmeans.Learn(observations);
            int[] labels = clusters.Decide(observations);

            // クラスタごとに画素数を集計
            var counts = new int[actualClusters];
            foreach (var lbl in labels)
                counts[lbl]++;

            // 結果を ColorCount レコードに変換
            var result = new List<ColorCount>(actualClusters);
            for (int i = 0; i < actualClusters; i++)
            {
                var centroid = clusters.Centroids[i];
                byte r = (byte)Math.Clamp((int)Math.Round(centroid[0]), 0, 255);
                byte g = (byte)Math.Clamp((int)Math.Round(centroid[1]), 0, 255);
                byte b = (byte)Math.Clamp((int)Math.Round(centroid[2]), 0, 255);
                result.Add(new ColorCount(r, g, b, counts[i]));
            }

            // 画素数が多い順にソートして返す
            return result.OrderByDescending(cc => cc.Count).ToList();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // サンプル実行
            using var bitmap = SKBitmap.Decode("image.jpg");
            //using var bitmap = SKBitmap.Decode("rgb.png");
            var clusters = ColorClusteringService.ClusterColors(
                bitmap,
                maxClusters: 20,
                sampleStep: 1,
                maxIterations: 50,
                tolerance: 1e-3
            );

            var totalCount = clusters.Sum(cc => cc.Count);

            Debug.WriteLine("Count\t\tR\tG\tB");
            foreach (var cc in clusters.Where(x => x.Count > 0))
            {
                Debug.WriteLine($"{cc.Count}\t{(double)cc.Count / totalCount * 100:F2}\t{cc.R}\t{cc.G}\t{cc.B}");
            }
        }
    }
}
