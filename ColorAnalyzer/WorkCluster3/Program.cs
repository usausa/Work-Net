using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

using SkiaSharp;

using System.Diagnostics;

namespace WorkCluster3
{
    public record ColorCount(
        byte R,
        byte G,
        byte B,
        int Count);

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

            // センター取得
            var centroids = default(VBuffer<float>[]);
            model.LastTransformer.Model.GetClusterCentroids(ref centroids, out _);

            var counts = new int[centroids.Length];
            foreach (var prediction in mlContext.Data.CreateEnumerable<ClusterPrediction>(transformed, reuseRowObject: false))
            {
                counts[prediction.ClusterId - 1]++;
            }

            var list = new List<ColorCount>(counts.Length);
            for (var i = 0; i < counts.Length; i++)
            {
                var centroid = centroids[i].DenseValues().ToArray();
                var r = (byte)Math.Round(centroid[0]);
                var g = (byte)Math.Round(centroid[1]);
                var b = (byte)Math.Round(centroid[2]);
                list.Add(new ColorCount(r, g, b, counts[i]));
            }

            list.Sort(static (x, y) => y.Count - x.Count);

            foreach (var count in list)
            {
                Debug.WriteLine($"{count.Count}\t{count.R}\t{count.G}\t{count.B}");
            }
        }
    }
}
