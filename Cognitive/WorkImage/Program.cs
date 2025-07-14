using Azure;

namespace WorkImage;

using Azure.AI.Vision.ImageAnalysis;

using System;
using System.Diagnostics;
using System.IO;

public static class Program
{
    static async Task Main(string[] args)
    {
        var client = new ImageAnalysisClient(new Uri(args[1]), new AzureKeyCredential(args[0]));

        //await DetectPeople(client, "D:\\people.jpg");
        await RecognizeText(client, "D:\\ocr.jpg");
        //await DetectObject(client, "D:\\objects.jpg");
        //await DetectTag(client, "D:\\objects.jpg");
        //await DetectCaption(client, "D:\\people.jpg");
    }


    private static async Task RecognizeText(ImageAnalysisClient client, string imagePath)
    {
        await using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
        using var draw = await DrawWrapper.CreateAsync(imagePath);

        ImageAnalysisResult? result = await client.AnalyzeAsync(await BinaryData.FromStreamAsync(stream), VisualFeatures.Read);

        Debug.WriteLine($" Metadata: Model: {result.ModelVersion} Image dimensions: {result.Metadata.Width} x {result.Metadata.Height}");
        Debug.WriteLine($" Text:");
        foreach (var line in result.Read.Blocks.SelectMany(block => block.Lines))
        {
            Debug.WriteLine($"   Line: '{line.Text}', Bounding Polygon: [{string.Join(" ", line.BoundingPolygon)}]");
            foreach (var word in line.Words)
            {
                Debug.WriteLine($"     Word: '{word.Text}', Confidence {word.Confidence:#.####}, Bounding Polygon: [{string.Join(" ", word.BoundingPolygon)}]");

                draw.DrawRectangle(word.BoundingPolygon[0].X, word.BoundingPolygon[0].Y, 100, 100, $"{word.Text} {word.Confidence:F4}");
            }
        }

        await draw.OutputAsync(Path.ChangeExtension(imagePath, "output.jpg"));
    }

    private static async Task DetectPeople(ImageAnalysisClient client, string imagePath)
    {
        await using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
        using var draw = await DrawWrapper.CreateAsync(imagePath);

        ImageAnalysisResult result = await client.AnalyzeAsync(await BinaryData.FromStreamAsync(stream), VisualFeatures.People);

        Debug.WriteLine($"Metadata: Model: {result.ModelVersion} Image dimensions: {result.Metadata.Width} x {result.Metadata.Height}");
        Debug.WriteLine($"People:");
        foreach (DetectedPerson person in result.People.Values.Where(x => x.Confidence >= 0.5))
        {
            Debug.WriteLine($"  Person: Bounding box {person.BoundingBox}, Confidence {person.Confidence:F4}");

            draw.DrawRectangle(person.BoundingBox.X, person.BoundingBox.Y, person.BoundingBox.Width, person.BoundingBox.Height, $"{person.Confidence:F4}");
        }

        await draw.OutputAsync(Path.ChangeExtension(imagePath, "output.jpg"));
    }

    private static async Task DetectObject(ImageAnalysisClient client, string imagePath)
    {
        await using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
        using var draw = await DrawWrapper.CreateAsync(imagePath);

        ImageAnalysisResult result = await client.AnalyzeAsync(await BinaryData.FromStreamAsync(stream), VisualFeatures.Objects);

        Debug.WriteLine($"Metadata: Model: {result.ModelVersion} Image dimensions: {result.Metadata.Width} x {result.Metadata.Height}");
        Debug.WriteLine($"Objects:");
        foreach (DetectedObject detectedObject in result.Objects.Values)
        {
            Debug.WriteLine($"  Object: '{detectedObject.Tags.First().Name}', Bounding box {detectedObject.BoundingBox}");

            draw.DrawRectangle(detectedObject.BoundingBox.X, detectedObject.BoundingBox.Y, detectedObject.BoundingBox.Width, detectedObject.BoundingBox.Height, detectedObject.Tags.First().Name);
        }

        await draw.OutputAsync(Path.ChangeExtension(imagePath, "output.jpg"));
    }

    private static async Task DetectTag(ImageAnalysisClient client, string imagePath)
    {
        await using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);

        ImageAnalysisResult result = await client.AnalyzeAsync(await BinaryData.FromStreamAsync(stream), VisualFeatures.Tags);

        Debug.WriteLine($"Metadata: Model: {result.ModelVersion} Image dimensions: {result.Metadata.Width} x {result.Metadata.Height}");
        Debug.WriteLine($"Tags:");
        foreach (DetectedTag tag in result.Tags.Values)
        {
            Debug.WriteLine($"  '{tag.Name}', Confidence {tag.Confidence:F4}");
        }
    }

    //private static async Task DetectCaption(ImageAnalysisClient client, string imagePath)
    //{
    //    await using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
    //    using var draw = await DrawWrapper.CreateAsync(imagePath);

    //    ImageAnalysisResult result = await client.AnalyzeAsync(await BinaryData.FromStreamAsync(stream), VisualFeatures.DenseCaptions, new ImageAnalysisOptions
    //    {
    //        GenderNeutralCaption = null
    //    });

    //    Debug.WriteLine($"Metadata: Model: {result.ModelVersion} Image dimensions: {result.Metadata.Width} x {result.Metadata.Height}");
    //    Debug.WriteLine($"Dense Captions:");
    //    foreach (DenseCaption denseCaption in result.DenseCaptions.Values)
    //    {
    //        Debug.WriteLine($"  Region: '{denseCaption.Text}', Confidence {denseCaption.Confidence:F4}, Bounding box {denseCaption.BoundingBox}");

    //        draw.DrawRectangle(denseCaption.BoundingBox.X, denseCaption.BoundingBox.Y, denseCaption.BoundingBox.Width, denseCaption.BoundingBox.Height, $"{denseCaption.Text} {denseCaption.Confidence:F4}");
    //    }

    //    await draw.OutputAsync(Path.ChangeExtension(imagePath, "output.jpg"));
    //}
}
