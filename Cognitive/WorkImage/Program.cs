using Azure;

namespace WorkImage;

using Azure.AI.Vision.ImageAnalysis;

using System.Diagnostics;
using System.IO;

public static class Program
{
    static async Task Main(string[] args)
    {
        var client = new ImageAnalysisClient(new Uri(args[1]), new AzureKeyCredential(args[0]));

        //await RecognizeText(client, "D:\\ocr.jpg");
        //await DetectPeople(client, "D:\\people.jpg");
        //await DetectObject(client, "D:\\objects.jpg");
        //await DetectTag(client, "D:\\objects.jpg");
        //await DetectCaption(client, "D:\\people.jpg");
    }


    private static async Task RecognizeText(ImageAnalysisClient client, string imagePath)
    {
        await using var stream = new FileStream(imagePath, FileMode.Open);

        ImageAnalysisResult? result = await client.AnalyzeAsync(await BinaryData.FromStreamAsync(stream), VisualFeatures.Read);

        Debug.WriteLine($"Image read results:");
        Debug.WriteLine($" Metadata: Model: {result.ModelVersion} Image dimensions: {result.Metadata.Width} x {result.Metadata.Height}");
        Debug.WriteLine($" Text:");
        foreach (var line in result.Read.Blocks.SelectMany(block => block.Lines))
        {
            Debug.WriteLine($"   Line: '{line.Text}', Bounding Polygon: [{string.Join(" ", line.BoundingPolygon)}]");
            foreach (var word in line.Words)
            {
                Debug.WriteLine($"     Word: '{word.Text}', Confidence {word.Confidence.ToString("#.####")}, Bounding Polygon: [{string.Join(" ", word.BoundingPolygon)}]");
            }
        }
    }

    private static async Task DetectPeople(ImageAnalysisClient client, string imagePath)
    {
        await using var stream = new FileStream(imagePath, FileMode.Open);

        ImageAnalysisResult result = await client.AnalyzeAsync(await BinaryData.FromStreamAsync(stream), VisualFeatures.People);

        // Print people detection results to the console
        Debug.WriteLine($"Image analysis results:");
        Debug.WriteLine($" Metadata: Model: {result.ModelVersion} Image dimensions: {result.Metadata.Width} x {result.Metadata.Height}");
        Debug.WriteLine($" People:");
        foreach (DetectedPerson person in result.People.Values.Where(x => x.Confidence >= 0.5))
        {
            Debug.WriteLine($"   Person: Bounding box {person.BoundingBox}, Confidence {person.Confidence:F4}");
        }
    }

    private static async Task DetectObject(ImageAnalysisClient client, string imagePath)
    {
        await using var stream = new FileStream(imagePath, FileMode.Open);

        ImageAnalysisResult result = await client.AnalyzeAsync(await BinaryData.FromStreamAsync(stream), VisualFeatures.Objects);

        // Print object detection results to the console
        Debug.WriteLine($"Image analysis results:");
        Debug.WriteLine($" Metadata: Model: {result.ModelVersion} Image dimensions: {result.Metadata.Width} x {result.Metadata.Height}");
        Debug.WriteLine($" Objects:");
        foreach (DetectedObject detectedObject in result.Objects.Values)
        {
            Debug.WriteLine($"   Object: '{detectedObject.Tags.First().Name}', Bounding box {detectedObject.BoundingBox}");
        }
    }

    private static async Task DetectTag(ImageAnalysisClient client, string imagePath)
    {
        await using var stream = new FileStream(imagePath, FileMode.Open);

        ImageAnalysisResult result = await client.AnalyzeAsync(await BinaryData.FromStreamAsync(stream), VisualFeatures.Tags);

        Debug.WriteLine($"Image analysis results:");
        Debug.WriteLine($" Metadata: Model: {result.ModelVersion} Image dimensions: {result.Metadata.Width} x {result.Metadata.Height}");
        Debug.WriteLine($" Tags:");
        foreach (DetectedTag tag in result.Tags.Values)
        {
            Debug.WriteLine($"   '{tag.Name}', Confidence {tag.Confidence:F4}");
        }
    }

    private static async Task DetectCaption(ImageAnalysisClient client, string imagePath)
    {
        await using var stream = new FileStream(imagePath, FileMode.Open);

        ImageAnalysisResult result = await client.AnalyzeAsync(await BinaryData.FromStreamAsync(stream), VisualFeatures.DenseCaptions, new ImageAnalysisOptions
        {
            GenderNeutralCaption = null
        });

        Debug.WriteLine($"Image analysis results:");
        Debug.WriteLine($" Metadata: Model: {result.ModelVersion} Image dimensions: {result.Metadata.Width} x {result.Metadata.Height}");
        Debug.WriteLine($" Dense Captions:");
        foreach (DenseCaption denseCaption in result.DenseCaptions.Values)
        {
            Debug.WriteLine($"   Region: '{denseCaption.Text}', Confidence {denseCaption.Confidence:F4}, Bounding box {denseCaption.BoundingBox}");
        }
    }
}
