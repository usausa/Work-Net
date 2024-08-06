using SkiaSharp;

var rand = new Random();
for (var i = 0; i < 20; i++)
{
    using var skBitmap = new SKBitmap(320, 320);
    using var skCanvas = new SKCanvas(skBitmap);
    skCanvas.Clear(SKColors.White);

    var boxes = rand.Next(3) + 1;
    for (var j = 0; j < boxes; j++)
    {
        var paint = new SKPaint
        {
            Color = rand.Next(3) switch
            {
                0 => SKColors.Red,
                1 => SKColors.Blue,
                _ => SKColors.Green
            },
            Style = SKPaintStyle.Fill
        };

        var x = rand.Next(22) * 10;
        var y = rand.Next(22) * 10;
        var w = (rand.Next(6) + 6) * 10;
        var h = (rand.Next(6) + 6) * 10;
        skCanvas.DrawRect(x, y, w, h, paint);
    }

    using var outputStream = File.OpenWrite($"image{i:D3}.jpg");
    skBitmap.Encode(outputStream, SKEncodedImageFormat.Jpeg, 100);
}
