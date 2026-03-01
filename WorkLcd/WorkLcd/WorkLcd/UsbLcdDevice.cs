using System.Buffers.Binary;
using HidSharp;
using SkiaSharp;

namespace WorkLcd;

/// <summary>
/// USB接続LCDデバイス (VID:0x0416 PID:0x5302, 1280x480) の制御クラス。
/// Thermalright Trofeo Vision 360 ARGB 互換プロトコルを使用。
/// </summary>
public sealed class UsbLcdDevice : IDisposable
{
    /// <summary>USB Vendor ID。</summary>
    public const ushort VendorId = 0x0416;

    /// <summary>USB Product ID。</summary>
    public const ushort ProductId = 0x5302;

    /// <summary>ディスプレイの幅 (ピクセル)。</summary>
    public const int Width = 1280;

    /// <summary>ディスプレイの高さ (ピクセル)。</summary>
    public const int Height = 480;

    // HID report: Report ID (1 byte) + Data (512 bytes)
    private const int HidReportSize = 513;
    private const int DataPerPacket = 512;
    private const int HeaderSize = 20;
    private const byte ReportId = 0x00;

    // プロトコルヘッダーマジックバイト: DA DB DC DD
    private static readonly byte[] HeaderMagic = [0xDA, 0xDB, 0xDC, 0xDD];

    // プロトコルコマンド/圧縮タイプ
    private const byte CommandImage = 0x02;
    private const byte CompressionJpeg = 0x02;
    private const byte CompressionRgb565 = 0x01;

    private HidStream? _stream;
    private bool _disposed;

    /// <summary>
    /// デバイスが開いているかどうかを取得します。
    /// </summary>
    public bool IsOpen => _stream is not null;

    /// <summary>
    /// LCDデバイスを検索して接続を開きます。
    /// </summary>
    /// <exception cref="InvalidOperationException">デバイスが既に開いている、またはデバイスが見つからない場合。</exception>
    /// <exception cref="IOException">デバイスのオープンに失敗した場合。</exception>
    public void Open()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_stream is not null)
        {
            throw new InvalidOperationException("Device is already open.");
        }

        var device = DeviceList.Local
            .GetHidDevices(VendorId, ProductId)
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"LCD device (VID:0x{VendorId:X4} PID:0x{ProductId:X4}) not found.");

        _stream = device.Open();
        _stream.WriteTimeout = 5000;
    }

    /// <summary>
    /// デバイス接続を閉じます。
    /// </summary>
    public void Close()
    {
        _stream?.Dispose();
        _stream = null;
    }

    /// <summary>
    /// ディスプレイを黒でクリアします。
    /// </summary>
    public void Clear()
    {
        Clear(SKColors.Black);
    }

    /// <summary>
    /// ディスプレイを指定色でクリアします。
    /// </summary>
    /// <param name="color">塗りつぶし色。</param>
    public void Clear(SKColor color)
    {
        using var bitmap = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        bitmap.Erase(color);
        SendBitmap(bitmap);
    }

    /// <summary>
    /// SKBitmap を JPEG エンコードして LCD に転送します。
    /// サイズが異なる場合は 1280x480 にリサイズされます。
    /// </summary>
    /// <param name="bitmap">転送するビットマップ。</param>
    /// <param name="jpegQuality">JPEG 品質 (1-100)。既定値は 95。</param>
    public void SendBitmap(SKBitmap bitmap, int jpegQuality = 95)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        SKBitmap? resized = null;
        try
        {
            var source = bitmap;
            if (bitmap.Width != Width || bitmap.Height != Height)
            {
                resized = bitmap.Resize(new SKImageInfo(Width, Height), SKFilterQuality.High);
                source = resized ?? throw new InvalidOperationException("Failed to resize bitmap.");
            }

            using var image = SKImage.FromBitmap(source);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, jpegQuality);
            SendJpeg(data.AsSpan());
        }
        finally
        {
            resized?.Dispose();
        }
    }

    /// <summary>
    /// SKBitmap を RGB565 形式で LCD に転送します。
    /// サイズが異なる場合は 1280x480 にリサイズされます。
    /// </summary>
    /// <param name="bitmap">転送するビットマップ。</param>
    public void SendBitmapAsRgb565(SKBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        SKBitmap? resized = null;
        try
        {
            var source = bitmap;
            if (bitmap.Width != Width || bitmap.Height != Height)
            {
                resized = bitmap.Resize(new SKImageInfo(Width, Height), SKFilterQuality.High);
                source = resized ?? throw new InvalidOperationException("Failed to resize bitmap.");
            }

            var rgb565 = ConvertToRgb565(source);
            SendRgb565(rgb565);
        }
        finally
        {
            resized?.Dispose();
        }
    }

    /// <summary>
    /// 生の JPEG データを LCD に転送します。
    /// </summary>
    /// <param name="jpegData">JPEG エンコード済みデータ。</param>
    public void SendJpeg(ReadOnlySpan<byte> jpegData)
    {
        SendImageData(jpegData, CompressionJpeg);
    }

    /// <summary>
    /// 生の RGB565 ピクセルデータを LCD に転送します。
    /// データサイズは Width * Height * 2 バイトである必要があります。
    /// </summary>
    /// <param name="rgb565Data">RGB565 ピクセルデータ。</param>
    public void SendRgb565(ReadOnlySpan<byte> rgb565Data)
    {
        var expectedSize = Width * Height * 2;
        if (rgb565Data.Length != expectedSize)
        {
            throw new ArgumentException(
                $"RGB565 data must be {expectedSize} bytes, but was {rgb565Data.Length}.",
                nameof(rgb565Data));
        }

        SendImageData(rgb565Data, CompressionRgb565);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Close();
        _disposed = true;
    }

    /// <summary>
    /// プロトコルヘッダー付き画像データをデバイスに送信します。
    /// </summary>
    private void SendImageData(ReadOnlySpan<byte> imageData, byte compressionType)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_stream is null)
        {
            throw new InvalidOperationException("Device is not open.");
        }

        // 20バイトのプロトコルヘッダーを構築
        Span<byte> header = stackalloc byte[HeaderSize];
        HeaderMagic.CopyTo(header);
        header[4] = CommandImage;
        // [5..7] 予約: 0x00
        BinaryPrimitives.WriteUInt16LittleEndian(header[8..], Width);
        BinaryPrimitives.WriteUInt16LittleEndian(header[10..], Height);
        header[12] = compressionType;
        // [13..15] 予約: 0x00
        BinaryPrimitives.WriteInt32LittleEndian(header[16..], imageData.Length);

        // ペイロード = ヘッダー + 画像データ
        var totalLength = HeaderSize + imageData.Length;
        var payload = new byte[totalLength];
        header.CopyTo(payload);
        imageData.CopyTo(payload.AsSpan(HeaderSize));

        // HID レポート単位に分割して送信
        Span<byte> packet = stackalloc byte[HidReportSize];
        var offset = 0;
        while (offset < payload.Length)
        {
            packet.Clear();
            packet[0] = ReportId;

            var chunkSize = Math.Min(payload.Length - offset, DataPerPacket);
            payload.AsSpan(offset, chunkSize).CopyTo(packet[1..]);

            _stream.Write(packet);
            offset += chunkSize;
        }
    }

    /// <summary>
    /// SKBitmap を RGB565 バイト配列に変換します。
    /// </summary>
    private static byte[] ConvertToRgb565(SKBitmap bitmap)
    {
        var buffer = new byte[bitmap.Width * bitmap.Height * 2];
        var offset = 0;

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);

                // RGB565: RRRRRGGG GGGBBBBB (リトルエンディアン)
                var rgb565 = (ushort)(
                    ((pixel.Red & 0xF8) << 8) |
                    ((pixel.Green & 0xFC) << 3) |
                    (pixel.Blue >> 3));

                buffer[offset++] = (byte)(rgb565 & 0xFF);
                buffer[offset++] = (byte)(rgb565 >> 8);
            }
        }

        return buffer;
    }
}
