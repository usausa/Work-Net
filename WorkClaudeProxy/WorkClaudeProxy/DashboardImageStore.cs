namespace WorkClaudeProxy;

internal sealed class DashboardImageStore
{
    private readonly Lock imageLock = new();
    private byte[]? currentImage;
    private readonly string outputPath;

    public DashboardImageStore(string outputPath)
    {
        this.outputPath = outputPath;
    }

    public void Update(byte[] imageData)
    {
        lock (imageLock)
        {
            currentImage = imageData;
        }
        File.WriteAllBytes(outputPath, imageData);
    }

    public byte[]? GetImage()
    {
        lock (imageLock)
        {
            return currentImage;
        }
    }
}
