using System.Diagnostics;
using Microsoft.ML;
using Microsoft.ML.Data;
using SkiaSharp;

var ctx = new MLContext();
var labels = File.ReadAllLines("labels.txt");

var pipeline =
    ctx.Transforms.LoadImages(outputColumnName: "Image", null, inputColumnName: "ImagePath")
        .Append(ctx.Transforms.ResizeImages(outputColumnName: "ResizedImage", imageWidth: 320, imageHeight: 320, inputColumnName: "Image", resizing: Microsoft.ML.Transforms.Image.ImageResizingEstimator.ResizingKind.Fill))
        .Append(ctx.Transforms.ExtractPixels(outputColumnName: "Pixels", inputColumnName: "ResizedImage", offsetImage: 255, scaleImage: 1))
        .Append(ctx.Transforms.CopyColumns(outputColumnName: "image_tensor", "Pixels"))
        .Append(ctx.Transforms.ApplyOnnxModel(modelFile: "model.onnx"));

var emptyDv = ctx.Data.LoadFromEnumerable(new ModelInput[] { });

var model = pipeline.Fit(emptyDv);

//ctx.Model.Save(model, emptyDv.Schema, "model.mlnet");

var input = new ModelInput { ImagePath = "image.jpg" };
var predictionEngine = ctx.Model.CreatePredictionEngine<ModelInput, ModelOutput>(model);
var prediction = predictionEngine.Predict(input);

var boundingBoxes = prediction.ToBoundingBoxes(labels, MLImage.CreateFromFile(input.ImagePath));
var topBoundingBoxes =
    boundingBoxes
        //.Where(x => x.Probability > confidence)
        .OrderByDescending(x => x.Probability)
        .ToArray();

using var skBitmap = SKBitmap.Decode("image.jpg");
using var skCanvas = new SKCanvas(skBitmap);
var paint = new SKPaint
{
    Color = SKColors.Red,
    StrokeWidth = 1,
    IsStroke = true
};

foreach (var b in topBoundingBoxes.Take(10))
{
    Debug.WriteLine(b);
    skCanvas.DrawRect(new SKRect(b.TopLeft.X, b.TopLeft.Y, b.BottomRight.X, b.BottomRight.Y), paint);
}

using var outputStream = File.OpenWrite("output.jpg");
skBitmap.Encode(outputStream, SKEncodedImageFormat.Jpeg, 100);

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
            this.Boxes!
                .Chunk(4)
                .ToArray();

        var boundingBoxes =
            bboxCoordinates
                .Select((coordinates, idx) =>
                    new BoundingBox
                    {
                        TopLeft = (X: coordinates[0] * originalImage.Width, Y: coordinates[1] * originalImage.Height),
                        BottomRight = (X: coordinates[2] * originalImage.Width, Y: coordinates[3] * originalImage.Height),
                        PredictedClass = labels[this.Classes![idx]],
                        Probability = this.Scores![idx]
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
