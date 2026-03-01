using System.Buffers;
using SkiaSharp;
using WorkLcd;

using var lcd = new UsbLcdDevice();
lcd.Open();

using var bitmap = new SKBitmap(UsbLcdDevice.Width, UsbLcdDevice.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
using var canvas = new SKCanvas(bitmap);

canvas.Clear(SKColors.DarkBlue);

using var paint = new SKPaint
{
    Color = SKColors.White,
    IsAntialias = true,
};
using var font = new SKFont
{
    Size = 64,
};
canvas.DrawText("Hello, LCD!", 100, 260, SKTextAlign.Left, font, paint);

var jpegBytes = SkiaBitmapHelper.ToJpegBytes(bitmap, 95);

// デバイスは一定時間フレームを受信しないと画面をクリアするため、定期的に再送する
var interval = TimeSpan.FromSeconds(1);
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
Console.WriteLine("Ctrl+C で終了します。");

while (!cts.Token.IsCancellationRequested)
{
    lcd.SendJpeg(jpegBytes);

    try
    {
        await Task.Delay(interval, cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }
}

var frameSize = UsbLcdDevice.Width * UsbLcdDevice.Height * 2;
var pooledBlackFrame = ArrayPool<byte>.Shared.Rent(frameSize);
try
{
    Array.Clear(pooledBlackFrame, 0, frameSize);
    lcd.SendRgb565(pooledBlackFrame.AsSpan(0, frameSize));
}
finally
{
    ArrayPool<byte>.Shared.Return(pooledBlackFrame);
}