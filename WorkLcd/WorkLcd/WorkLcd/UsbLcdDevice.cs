using System.Buffers.Binary;
using HidSharp;

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
        header.Clear();
        HeaderMagic.CopyTo(header);
        header[4] = CommandImage;
        BinaryPrimitives.WriteUInt16LittleEndian(header[8..], Width);
        BinaryPrimitives.WriteUInt16LittleEndian(header[10..], Height);
        header[12] = compressionType;
        BinaryPrimitives.WriteInt32LittleEndian(header[16..], imageData.Length);

        // HID レポート単位で送信（payloadバッファは作らず、先頭パケットにヘッダーを同梱）
        Span<byte> packet = stackalloc byte[HidReportSize];

        packet.Clear();
        packet[0] = ReportId;
        header.CopyTo(packet[1..]);

        var sentDataOffset = 0;
        var firstDataBytes = Math.Min(imageData.Length, DataPerPacket - HeaderSize);
        if (firstDataBytes > 0)
        {
            imageData[..firstDataBytes].CopyTo(packet[(1 + HeaderSize)..]);
            sentDataOffset = firstDataBytes;
        }

        _stream.Write(packet);

        while (sentDataOffset < imageData.Length)
        {
            packet.Clear();
            packet[0] = ReportId;

            var chunkSize = Math.Min(imageData.Length - sentDataOffset, DataPerPacket);
            imageData.Slice(sentDataOffset, chunkSize).CopyTo(packet[1..]);

            _stream.Write(packet);
            sentDataOffset += chunkSize;
        }
    }
}