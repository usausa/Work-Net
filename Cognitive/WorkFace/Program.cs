using System.Diagnostics;

namespace WorkFace;

using Azure;
using Azure.AI.Vision.Face;

using SkiaSharp;

internal class Program
{
    static async Task Main(string[] args)
    {
        Uri endpoint = new Uri(args[1]);
        var client = new FaceClient(endpoint, new AzureKeyCredential(args[0]));

        var filename = "D:\\face.jpg";
        await using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read);

        var detectResponse = await client.DetectAsync(
            await BinaryData.FromStreamAsync(stream),
            FaceDetectionModel.Detection03,
            FaceRecognitionModel.Recognition04,
            returnFaceId: false,
            returnFaceAttributes: [FaceAttributeType.Detection03.HeadPose, FaceAttributeType.Detection03.Mask, FaceAttributeType.Recognition04.QualityForRecognition
            ],
            returnFaceLandmarks: true,
            returnRecognitionModel: true,
            faceIdTimeToLive: 120);

        var detectedFaces = detectResponse.Value;
        Debug.WriteLine($"Detected {detectedFaces.Count} face(s) in the image.");
        foreach (var detectedFace in detectedFaces)
        {
            Debug.WriteLine($"Face Rectangle: left={detectedFace.FaceRectangle.Left}, top={detectedFace.FaceRectangle.Top}, width={detectedFace.FaceRectangle.Width}, height={detectedFace.FaceRectangle.Height}");
            Debug.WriteLine($"Head pose: pitch={detectedFace.FaceAttributes.HeadPose.Pitch}, roll={detectedFace.FaceAttributes.HeadPose.Roll}, yaw={detectedFace.FaceAttributes.HeadPose.Yaw}");
            Debug.WriteLine($"Mask: NoseAndMouthCovered={detectedFace.FaceAttributes.Mask.NoseAndMouthCovered}, Type={detectedFace.FaceAttributes.Mask.Type}");
            Debug.WriteLine($"Quality: {detectedFace.FaceAttributes.QualityForRecognition}");
            Debug.WriteLine($"Recognition model: {detectedFace.RecognitionModel}");
            Debug.WriteLine($"Landmarks: ");

            Debug.WriteLine($"    PupilLeft: ({detectedFace.FaceLandmarks.PupilLeft.X}, {detectedFace.FaceLandmarks.PupilLeft.Y})");
            Debug.WriteLine($"    PupilRight: ({detectedFace.FaceLandmarks.PupilRight.X}, {detectedFace.FaceLandmarks.PupilRight.Y})");
            Debug.WriteLine($"    NoseTip: ({detectedFace.FaceLandmarks.NoseTip.X}, {detectedFace.FaceLandmarks.NoseTip.Y})");
            Debug.WriteLine($"    MouthLeft: ({detectedFace.FaceLandmarks.MouthLeft.X}, {detectedFace.FaceLandmarks.MouthLeft.Y})");
            Debug.WriteLine($"    MouthRight: ({detectedFace.FaceLandmarks.MouthRight.X}, {detectedFace.FaceLandmarks.MouthRight.Y})");
            Debug.WriteLine($"    EyebrowLeftOuter: ({detectedFace.FaceLandmarks.EyebrowLeftOuter.X}, {detectedFace.FaceLandmarks.EyebrowLeftOuter.Y})");
            Debug.WriteLine($"    EyebrowLeftInner: ({detectedFace.FaceLandmarks.EyebrowLeftInner.X}, {detectedFace.FaceLandmarks.EyebrowLeftInner.Y})");
            Debug.WriteLine($"    EyeLeftOuter: ({detectedFace.FaceLandmarks.EyeLeftOuter.X}, {detectedFace.FaceLandmarks.EyeLeftOuter.Y})");
            Debug.WriteLine($"    EyeLeftTop: ({detectedFace.FaceLandmarks.EyeLeftTop.X}, {detectedFace.FaceLandmarks.EyeLeftTop.Y})");
            Debug.WriteLine($"    EyeLeftBottom: ({detectedFace.FaceLandmarks.EyeLeftBottom.X}, {detectedFace.FaceLandmarks.EyeLeftBottom.Y})");
            Debug.WriteLine($"    EyeLeftInner: ({detectedFace.FaceLandmarks.EyeLeftInner.X}, {detectedFace.FaceLandmarks.EyeLeftInner.Y})");
            Debug.WriteLine($"    EyebrowRightInner: ({detectedFace.FaceLandmarks.EyebrowRightInner.X}, {detectedFace.FaceLandmarks.EyebrowRightInner.Y})");
            Debug.WriteLine($"    EyebrowRightOuter: ({detectedFace.FaceLandmarks.EyebrowRightOuter.X}, {detectedFace.FaceLandmarks.EyebrowRightOuter.Y})");
            Debug.WriteLine($"    EyeRightInner: ({detectedFace.FaceLandmarks.EyeRightInner.X}, {detectedFace.FaceLandmarks.EyeRightInner.Y})");
            Debug.WriteLine($"    EyeRightTop: ({detectedFace.FaceLandmarks.EyeRightTop.X}, {detectedFace.FaceLandmarks.EyeRightTop.Y})");
            Debug.WriteLine($"    EyeRightBottom: ({detectedFace.FaceLandmarks.EyeRightBottom.X}, {detectedFace.FaceLandmarks.EyeRightBottom.Y})");
            Debug.WriteLine($"    EyeRightOuter: ({detectedFace.FaceLandmarks.EyeRightOuter.X}, {detectedFace.FaceLandmarks.EyeRightOuter.Y})");
            Debug.WriteLine($"    NoseRootLeft: ({detectedFace.FaceLandmarks.NoseRootLeft.X}, {detectedFace.FaceLandmarks.NoseRootLeft.Y})");
            Debug.WriteLine($"    NoseRootRight: ({detectedFace.FaceLandmarks.NoseRootRight.X}, {detectedFace.FaceLandmarks.NoseRootRight.Y})");
            Debug.WriteLine($"    NoseLeftAlarTop: ({detectedFace.FaceLandmarks.NoseLeftAlarTop.X}, {detectedFace.FaceLandmarks.NoseLeftAlarTop.Y})");
            Debug.WriteLine($"    NoseRightAlarTop: ({detectedFace.FaceLandmarks.NoseRightAlarTop.X}, {detectedFace.FaceLandmarks.NoseRightAlarTop.Y})");
            Debug.WriteLine($"    NoseLeftAlarOutTip: ({detectedFace.FaceLandmarks.NoseLeftAlarOutTip.X}, {detectedFace.FaceLandmarks.NoseLeftAlarOutTip.Y})");
            Debug.WriteLine($"    NoseRightAlarOutTip: ({detectedFace.FaceLandmarks.NoseRightAlarOutTip.X}, {detectedFace.FaceLandmarks.NoseRightAlarOutTip.Y})");
            Debug.WriteLine($"    UpperLipTop: ({detectedFace.FaceLandmarks.UpperLipTop.X}, {detectedFace.FaceLandmarks.UpperLipTop.Y})");
            Debug.WriteLine($"    UpperLipBottom: ({detectedFace.FaceLandmarks.UpperLipBottom.X}, {detectedFace.FaceLandmarks.UpperLipBottom.Y})");
            Debug.WriteLine($"    UnderLipTop: ({detectedFace.FaceLandmarks.UnderLipTop.X}, {detectedFace.FaceLandmarks.UnderLipTop.Y})");
            Debug.WriteLine($"    UnderLipBottom: ({detectedFace.FaceLandmarks.UnderLipBottom.X}, {detectedFace.FaceLandmarks.UnderLipBottom.Y})");
        }

        await using var inputStream = File.OpenRead(filename);
        using var original = SKBitmap.Decode(inputStream);

        var image = new SKImageInfo(original.Width, original.Height);
        using var surface = SKSurface.Create(image);
        var canvas = surface.Canvas;

        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(original, 0, 0);

        var paint = new SKPaint
        {
            Color = SKColors.Red,
            StrokeWidth = 1,
            IsStroke = true,
            Style = SKPaintStyle.Stroke
        };

        foreach (var detectedFace in detectedFaces)
        {
            canvas.DrawRect(detectedFace.FaceRectangle.Left, detectedFace.FaceRectangle.Top, detectedFace.FaceRectangle.Width, detectedFace.FaceRectangle.Height, paint);
        }

        using var outputImage = surface.Snapshot();
        using var data = outputImage.Encode(SKEncodedImageFormat.Jpeg, 90); // 品質90でエンコード
        await using var outputStream = File.OpenWrite("output.jpg");
        data.SaveTo(outputStream);
    }
}
