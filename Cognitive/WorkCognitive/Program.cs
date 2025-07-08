namespace WorkCognitive;

using System.Diagnostics;

using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

public static class Program
{
    static async Task Main(string[] args)
    {
        var client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(args[0]))
        {
            Endpoint = args[1]
        };

        //await AnalyzeDescription(client, "D:\\face.jpg");
        //await DetectObject(client, "D:\\face.jpg"); // TODO 別画像
        //await AnalyzeFace(client, "D:\\face.jpg");
        //await AnalyzeColor(client, "D:\\face.jpg");
        //await AnalyzeAdult(client, "D:\\face.jpg");
        await RecognizeText(client, "D:\\ocr.jpg");
    }

    public static async Task RecognizeText(ComputerVisionClient client, string imagePath)
    {
        await using Stream imageStream = File.OpenRead(imagePath);

        var result = await client.RecognizePrintedTextInStreamWithHttpMessagesAsync(true, imageStream);

        foreach (var region in result.Body.Regions)
        {
            Debug.WriteLine($"{region.BoundingBox}");
            foreach (var line in region.Lines)
            {
                Debug.WriteLine($"{line.BoundingBox}");
                foreach (var word in line.Words)
                {
                    Debug.WriteLine($"  {word.Text} ({word.BoundingBox})");
                }
            }
        }
    }

    private static async Task AnalyzeFace(ComputerVisionClient client, string imagePath)
    {
        await using Stream imageStream = File.OpenRead(imagePath);
        var features = new VisualFeatureTypes?[]
        {
            VisualFeatureTypes.Faces
        };
        var result = await client.AnalyzeImageInStreamAsync(imageStream, features);

        foreach (var face in result.Faces)
        {
            Debug.WriteLine($"Gender=[{face.Gender}], Age=[{face.Age}], Rectangle=[{face.FaceRectangle.Left},{face.FaceRectangle.Top},{face.FaceRectangle.Width},{face.FaceRectangle.Height}]");
        }
    }

    private static async Task AnalyzeColor(ComputerVisionClient client, string imagePath)
    {
        await using Stream imageStream = File.OpenRead(imagePath);
        var features = new VisualFeatureTypes?[]
        {
            VisualFeatureTypes.Color
        };
        var result = await client.AnalyzeImageInStreamAsync(imageStream, features);

        Debug.WriteLine($"主な色: {string.Join(", ", result.Color.DominantColors)}");
        Debug.WriteLine($"背景色: {result.Color.DominantColorBackground}");
        Debug.WriteLine($"前景色: {result.Color.DominantColorForeground}");
        Debug.WriteLine($"白黒画像か: {result.Color.IsBWImg}");
    }

    private static async Task AnalyzeAdult(ComputerVisionClient client, string imagePath)
    {
        await using Stream imageStream = File.OpenRead(imagePath);
        var features = new VisualFeatureTypes?[]
        {
            VisualFeatureTypes.Adult
        };
        var result = await client.AnalyzeImageInStreamAsync(imageStream, features);

        Debug.WriteLine($"IsAdultContent: {result.Adult.IsAdultContent} (信頼度: {result.Adult.AdultScore})");
        Debug.WriteLine($"IsRacyContent: {result.Adult.IsRacyContent} (信頼度: {result.Adult.RacyScore})");
        Debug.WriteLine($"IsGoryContent: {result.Adult.IsGoryContent} (信頼度: {result.Adult.GoreScore})");
    }

    private static async Task AnalyzeDescription(ComputerVisionClient client, string imagePath)
    {
        await using Stream imageStream = File.OpenRead(imagePath);
        var features = new VisualFeatureTypes?[]
        {
            VisualFeatureTypes.Description, // シーン説明
            VisualFeatureTypes.Tags,         // タグ
        };
        var result = await client.AnalyzeImageInStreamAsync(imageStream, features);

        // 説明文
        foreach (var caption in result.Description.Captions)
        {
            Debug.WriteLine($"説明: {caption.Text} (信頼度: {caption.Confidence})");
        }

        // タグ
        Debug.WriteLine("タグ:");
        foreach (var tag in result.Tags)
        {
            Debug.WriteLine($"{tag.Name} (信頼度: {tag.Confidence})");
        }
    }

    private static async Task DetectObject(ComputerVisionClient client, string imagePath)
    {
        await using Stream imageStream = File.OpenRead(imagePath);
        var detectedObjects = await client.DetectObjectsInStreamAsync(imageStream);

        foreach (var obj in detectedObjects.Objects)
        {
            Debug.WriteLine($"物体: {obj.ObjectProperty}, 信頼度: {obj.Confidence}, 座標: left={obj.Rectangle.X}, top={obj.Rectangle.Y}, width={obj.Rectangle.W}, height={obj.Rectangle.H}");
        }
    }
}
