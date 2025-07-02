using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace ImageKMeansClustering
{
    public class RgbData
    {
        public float R;
        public float G;
        public float B;
    }

    public class ClusterPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint ClusterId;

        [ColumnName("Score")]
        public float[] Distances = default!;
    }

    class Program
    {
        static void Main(string[] args)
        {
            var watch = new Stopwatch();
            //string imagePath = "rgb.png";
            string imagePath = "image.jpg";
            int maxClusters = 20;

            // TODO 最小クラスタ数をHashで計算も！
            // 配列作成
            var bitmap = SKBitmap.Decode(File.ReadAllBytes(imagePath));
            var width = bitmap.Width;
            var height = bitmap.Height;
            var pixelData = new RgbData[width * height];
            int idx = 0;

            var hash = new HashSet<SKColor>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    SKColor color = bitmap.GetPixel(x, y);
                    pixelData[idx++] = new RgbData { R = color.Red, G = color.Green, B = color.Blue };

                    if (hash.Count < maxClusters)
                    {
                        hash.Add(color);
                    }
                }
            }

            maxClusters = Math.Min(maxClusters, hash.Count);

            // ML.NETでクラスタリング
            var mlContext = new MLContext();

            watch.Restart();
            var dataView = mlContext.Data.LoadFromEnumerable(pixelData);
            Debug.WriteLine($"* LoadFromEnumerable : {watch.ElapsedMilliseconds}");

            watch.Restart();
            var options = new KMeansTrainer.Options
            {
                FeatureColumnName = "Features",
                NumberOfClusters = maxClusters,
                MaximumNumberOfIterations = 25,
                OptimizationTolerance = 1e-3f,
                InitializationAlgorithm = KMeansTrainer.InitializationAlgorithm.KMeansPlusPlus,
            };
            var pipeline = mlContext.Transforms
                .Concatenate("Features", nameof(RgbData.R), nameof(RgbData.G), nameof(RgbData.B))
                .Append(mlContext.Clustering.Trainers.KMeans(options));
            Debug.WriteLine($"* Transforms : {watch.ElapsedMilliseconds}");

            watch.Restart();
            var model = pipeline.Fit(dataView);
            Debug.WriteLine($"* Fit : {watch.ElapsedMilliseconds}");

            // 全ピクセルを予測
            watch.Restart();
            var transformed = model.Transform(dataView);
            Debug.WriteLine($"* Transform : {watch.ElapsedMilliseconds}");

            watch.Restart();
            var predictions = mlContext.Data.CreateEnumerable<ClusterPrediction>(transformed, reuseRowObject: false)
                .Select(p => p.ClusterId)
                .ToArray();
            Debug.WriteLine($"* CreateEnumerable : {watch.ElapsedMilliseconds}");

            // クラスタごとの件数・インデックスをグループ化
            var clusterGroups = predictions
                .Select((clusterId, i) => new { clusterId, i })
                .GroupBy(x => x.clusterId)
                .Select(g => new
                {
                    ClusterId = g.Key,
                    Count = g.Count(),
                    Indices = g.Select(x => x.i).ToArray()
                })
                .OrderByDescending(g => g.Count)
                .ToArray();

            // モデルからクラスタ中心色(センター)を取得
            var kmeansModel = model.LastTransformer.Model;
            VBuffer<float>[] centroids = default;
            kmeansModel.GetClusterCentroids(ref centroids, out int k);

            Debug.WriteLine($"クラスタ数: {maxClusters}");
            foreach (var cluster in clusterGroups)
            {
                // クラスタ中心のRGB値を取得
                var centroid = centroids[(int)cluster.ClusterId - 1].DenseValues().ToArray();
                int r = (int)Math.Round(centroid[0]);
                int g = (int)Math.Round(centroid[1]);
                int b = (int)Math.Round(centroid[2]);
                Debug.WriteLine($"クラスタ {cluster.ClusterId}: {cluster.Count} ピクセル, 中心RGB=({r},{g},{b})");
            }
        }
    }
}
