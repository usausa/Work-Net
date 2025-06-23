using System.Diagnostics;
using Microsoft.ML;
using Microsoft.ML.Data;
using SkiaSharp;

var labels = File.ReadAllLines("labels.txt");

var ctx = new MLContext();
var pipeline =
    ctx.Transforms.LoadImages(outputColumnName: "Image", null, inputColumnName: "ImagePath")
        .Append(ctx.Transforms.ResizeImages(outputColumnName: "ResizedImage", imageWidth: 320, imageHeight: 320, inputColumnName: "Image", resizing: Microsoft.ML.Transforms.Image.ImageResizingEstimator.ResizingKind.Fill))
        .Append(ctx.Transforms.ExtractPixels(outputColumnName: "Pixels", inputColumnName: "ResizedImage"))
        .Append(ctx.Transforms.CopyColumns(outputColumnName: "image_tensor", "Pixels"))
        .Append(ctx.Transforms.ApplyOnnxModel(modelFile: "model.onnx"));
var emptyDv = ctx.Data.LoadFromEnumerable(new ModelInput[] { });
var model = pipeline.Fit(emptyDv);
var predictionEngine = ctx.Model.CreatePredictionEngine<ModelInput, ModelOutput>(model);

for (var i = 0; i < 10; i++)
{
    Process(predictionEngine, labels, $"image{i:D3}.jpg", $"output{i:D3}.jpg");
}

static void Process(PredictionEngine<ModelInput, ModelOutput> engine, string[] labels, string image, string output)
{
    var input = new ModelInput { ImagePath = image };
    var prediction = engine.Predict(input);

    var boundingBoxes = prediction.ToBoundingBoxes(labels, MLImage.CreateFromFile(input.ImagePath));
    var topBoundingBoxes =
        boundingBoxes
            .OrderByDescending(x => x.Probability)
            .ToArray();

    using var skBitmap = SKBitmap.Decode(image);
    using var skCanvas = new SKCanvas(skBitmap);
    var paint = new SKPaint
    {
        Color = SKColors.Red,
        StrokeWidth = 1,
        IsStroke = true
    };

    foreach (var b in topBoundingBoxes)
    {
        Debug.WriteLine(b);
        if (b.Probability > 0.5)
        {
            skCanvas.DrawRect(new SKRect(b.TopLeft.X, b.TopLeft.Y, b.BottomRight.X, b.BottomRight.Y), paint);
        }
    }

    using var outputStream = File.OpenWrite(output);
    skBitmap.Encode(outputStream, SKEncodedImageFormat.Jpeg, 100);
}

public class ModelInput
{
    public string? ImagePath { get; set; }
}

public class ModelOutput
{
    [ColumnName("detected_boxes")]
    [VectorType]
    public float[]? Boxes { get; set; }

    [ColumnName("detected_scores")]
    [VectorType]
    public float[]? Scores { get; set; }

    [ColumnName("detected_classes")]
    [VectorType]
    public long[]? Classes { get; set; }

    public BoundingBox[] ToBoundingBoxes(string[] labels, MLImage originalImage)
    {
        var bboxCoordinates =
            Boxes!
                .Chunk(4)
                .ToArray();

        var boundingBoxes =
            bboxCoordinates
                .Select((coordinates, idx) =>
                    new BoundingBox
                    {
                        TopLeft = (X: coordinates[0] * originalImage.Width, Y: coordinates[1] * originalImage.Height),
                        BottomRight = (X: coordinates[2] * originalImage.Width, Y: coordinates[3] * originalImage.Height),
                        PredictedClass = labels[Classes![idx]],
                        Probability = Scores![idx]
                    })
                .ToArray();

        return boundingBoxes;
    }
}

public class BoundingBox
{
    public (float X, float Y) TopLeft { get; set; }
    public (float X, float Y) BottomRight { get; set; }
    public string? PredictedClass { get; set; }
    public float Probability { get; set; }

    public override string ToString() =>
        $"Top Left (x,y): ({TopLeft.X},{TopLeft.Y})\nBottom Right (x,y): ({BottomRight.X},{BottomRight.Y})\nClass: {PredictedClass}\nProbability: {Probability})";
}
