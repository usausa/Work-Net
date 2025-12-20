namespace WorkImageOnyx;

public static class Program
{
    public static void Main()
    {
        // TODO
        // 画像ロード
    }
}

public sealed class FaceDetector
{

}

public class FaceBox
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public float Confidence { get; set; }

    public override string ToString()
    {
        return $"Face at ({X}, {Y}) [{Width}x{Height}] - Confidence: {Confidence:F2}";
    }
}
