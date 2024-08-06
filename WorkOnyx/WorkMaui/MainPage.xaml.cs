namespace WorkMaui;

using Microsoft.ML;
using Microsoft.ML.Data;

using Smart.IO;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnButtonClicked(object sender, EventArgs e)
    {
        await using var labelStream = await FileSystem.OpenAppPackageFileAsync("labels.txt");
        using var reader = new StreamReader(labelStream);
        var labels = reader.ReadLines().ToArray();

        await using var modelStream = await FileSystem.OpenAppPackageFileAsync("model.onnx");
        var modelPath = Path.Combine(FileSystem.Current.AppDataDirectory, "model.onnx");
        await using var tempStream = File.OpenWrite(modelPath);
        await modelStream.CopyToAsync(tempStream);
        tempStream.Close();

        var ctx = new MLContext();
        var pipeline =
            ctx.Transforms.LoadImages(outputColumnName: "Image", null, inputColumnName: "ImagePath")
                .Append(ctx.Transforms.ResizeImages(outputColumnName: "ResizedImage", imageWidth: 320, imageHeight: 320, inputColumnName: "Image", resizing: Microsoft.ML.Transforms.Image.ImageResizingEstimator.ResizingKind.Fill))
                .Append(ctx.Transforms.ExtractPixels(outputColumnName: "Pixels", inputColumnName: "ResizedImage", offsetImage: 255, scaleImage: 1))
                .Append(ctx.Transforms.CopyColumns(outputColumnName: "image_tensor", "Pixels"))
                .Append(ctx.Transforms.ApplyOnnxModel(modelFile: modelPath));
        var emptyDv = ctx.Data.LoadFromEnumerable(new ModelInput[] { });
        var model = pipeline.Fit(emptyDv);
        var predictionEngine = ctx.Model.CreatePredictionEngine<ModelInput, ModelOutput>(model);

        // TODO use on memory image
    }
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
