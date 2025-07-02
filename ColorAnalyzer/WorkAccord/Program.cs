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
            int maxIterations,
            double tolerance)
        {
            var width = bitmap.Width;
            var height = bitmap.Height;

            var observations = new double[width * height][];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var c = bitmap.GetPixel(x, y);
                    observations[(y * width) + x] = [c.Red, c.Green, c.Blue];
                }
            }

            var actualClusters = Math.Min(maxClusters, observations.Length);

            // KMeans
            var kmeans = new KMeans(actualClusters)
            {
                MaxIterations = maxIterations,
                Tolerance = tolerance
            };
            var clusters = kmeans.Learn(observations);
            var labels = clusters.Decide(observations);

            // Count by cluster
            var counts = new int[actualClusters];
            foreach (var label in labels)
            {
                counts[label]++;
            }

            var result = new List<ColorCount>(actualClusters);
            for (var i = 0; i < actualClusters; i++)
            {
                var centroid = clusters.Centroids[i];
                var r = (byte)Math.Clamp((int)Math.Round(centroid[0]), 0, 255);
                var g = (byte)Math.Clamp((int)Math.Round(centroid[1]), 0, 255);
                var b = (byte)Math.Clamp((int)Math.Round(centroid[2]), 0, 255);
                result.Add(new ColorCount(r, g, b, counts[i]));
            }

            result.Sort(static (x, y) => y.Count - x.Count);
            return result;
        }

        public static List<ColorCount> ClusterColors2(
            SKBitmap bitmap,
            int maxClusters,
            int maxIterations,
            double tolerance)
        {
            var width = bitmap.Width;
            var height = bitmap.Height;

            var observations = new double[width * height][];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var c = bitmap.GetPixel(x, y);
                    observations[(y * width) + x] = [c.Red, c.Green, c.Blue];
                }
            }

            var actualClusters = Math.Min(maxClusters, observations.Length);

            // KMeans
            var kmeans = new MiniBatchKMeans(actualClusters, 25)
            {
                //MaxIterations = maxIterations,
                //Tolerance = tolerance
            };
            var clusters = kmeans.Learn(observations);
            var labels = clusters.Decide(observations);

            // Count by cluster
            var counts = new int[actualClusters];
            foreach (var label in labels)
            {
                counts[label]++;
            }

            var result = new List<ColorCount>(actualClusters);
            for (var i = 0; i < actualClusters; i++)
            {
                var centroid = clusters.Centroids[i];
                var r = (byte)Math.Clamp((int)Math.Round(centroid[0]), 0, 255);
                var g = (byte)Math.Clamp((int)Math.Round(centroid[1]), 0, 255);
                var b = (byte)Math.Clamp((int)Math.Round(centroid[2]), 0, 255);
                result.Add(new ColorCount(r, g, b, counts[i]));
            }

            result.Sort(static (x, y) => y.Count - x.Count);
            return result;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // サンプル実行
            using var bitmap = SKBitmap.Decode("image.jpg");
            //using var bitmap = SKBitmap.Decode("rgb.png");
            //var clusters = ColorClusteringService.ClusterColors(
            //    bitmap,
            //    maxClusters: 25,
            //    maxIterations: 100,
            //    tolerance: 1e-5
            //);
            var clusters = ColorClusteringService.ClusterColors2(
                bitmap,
                maxClusters: 25,
                maxIterations: 100,
                tolerance: 1e-5
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
