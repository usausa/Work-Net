namespace WorkTss;

using System;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;

using LibUsbDotNet;
using LibUsbDotNet.Main;

internal static class Program
{
    static void Main()
    {
        using var device = new TuringDeviceMinimal();
        try
        {
            // デバイス初期化
            if (!device.Initialize())
            {
                Console.WriteLine("Failed to initialize device");
                return;
            }

            Console.WriteLine("Device initialized successfully");

            // PNGバイト配列を送信
            var pngData = File.ReadAllBytes("image.png");
            if (device.SendPngBytes(pngData))
            {
                Console.WriteLine("PNG bytes sent successfully");
            }

            // 画面クリア
            if (device.ClearScreen())
            {
                Console.WriteLine("Screen cleared");
            }
        }
        catch (TuringDeviceException ex)
        {
            Console.WriteLine($"Device error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

public class TuringDeviceException : Exception
{
    public TuringDeviceException(string message) : base(message) { }
    public TuringDeviceException(string message, Exception innerException) : base(message, innerException) { }
}

public class TuringDeviceMinimal : IDisposable
{
    private const int VENDOR_ID = 0x1cbe;
    private const int PRODUCT_ID = 0x0088;
    private const int CMD_PACKET_SIZE = 500;
    private const int FULL_PACKET_SIZE = 512;
    private const int COMMAND_TIMEOUT = 2000;
    private static readonly byte[] DES_KEY_BYTES = "slv3tuzx"u8.ToArray();
    private static readonly byte[] MAGIC_BYTES = { 161, 26 };

    private UsbDevice? _device;
    private UsbEndpointReader? _reader;
    private UsbEndpointWriter? _writer;
    private bool _disposed = false;

    public bool IsConnected => _device != null && _device.IsOpen;

    /// <summary>
    /// デバイスを初期化
    /// </summary>
    public bool Initialize()
    {
        try
        {
            var finder = new UsbDeviceFinder(VENDOR_ID, PRODUCT_ID);
            _device = UsbDevice.OpenUsbDevice(finder);

            if (_device == null)
            {
                throw new TuringDeviceException("Device not found. Please ensure the Turing device is connected.");
            }

            if (_device is IUsbDevice wholeUsbDevice)
            {
                wholeUsbDevice.SetConfiguration(1);
                wholeUsbDevice.ClaimInterface(0);
            }

            _reader = _device.OpenEndpointReader(ReadEndpointID.Ep01);
            _writer = _device.OpenEndpointWriter(WriteEndpointID.Ep01);

            if (_reader == null || _writer == null)
            {
                throw new TuringDeviceException("Failed to open USB endpoints.");
            }

            return true;
        }
        catch (TuringDeviceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new TuringDeviceException("Error initializing device", ex);
        }
    }

    /// <summary>
    /// コマンドパケットヘッダーを構築
    /// </summary>
    private byte[] BuildCommandPacketHeader(byte commandId)
    {
        var packet = ArrayPool<byte>.Shared.Rent(CMD_PACKET_SIZE);
        try
        {
            Array.Clear(packet, 0, CMD_PACKET_SIZE);

            packet[0] = commandId;
            packet[2] = 0x1A;
            packet[3] = 0x6D;

            // タイムスタンプ計算（当日0時からのミリ秒）
            var today = DateTime.UtcNow.Date;
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var dayStart = new DateTimeOffset(today).ToUnixTimeMilliseconds();
            var timestamp = now - dayStart;

            // リトルエンディアンで格納
            packet[4] = (byte)(timestamp & 0xFF);
            packet[5] = (byte)((timestamp >> 8) & 0xFF);
            packet[6] = (byte)((timestamp >> 16) & 0xFF);
            packet[7] = (byte)((timestamp >> 24) & 0xFF);

            // 結果をコピーして返す
            var result = new byte[CMD_PACKET_SIZE];
            Buffer.BlockCopy(packet, 0, result, 0, CMD_PACKET_SIZE);
            return result;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(packet);
        }
    }

    /// <summary>
    /// DES-CBC暗号化（.NET標準ライブラリ使用）
    /// </summary>
    private byte[] EncryptWithDES(byte[] data)
    {
        // 8バイト境界に切り上げ
        var paddedLen = (data.Length + 7) & ~7;
        var padded = ArrayPool<byte>.Shared.Rent(paddedLen);

        try
        {
            Array.Clear(padded, 0, paddedLen);
            Array.Copy(data, padded, data.Length);

            using var des = DES.Create();
            des.Key = DES_KEY_BYTES;
            des.IV = DES_KEY_BYTES;  // IVも同じ鍵を使用
            des.Mode = CipherMode.CBC;
            des.Padding = PaddingMode.None;  // 手動でパディング済み

            using var encryptor = des.CreateEncryptor();
            var encrypted = ArrayPool<byte>.Shared.Rent(paddedLen);
            try
            {
                var outputLen = encryptor.TransformBlock(padded, 0, paddedLen, encrypted, 0);

                // 実際の暗号化データのみをコピーして返す
                var result = new byte[outputLen];
                Buffer.BlockCopy(encrypted, 0, result, 0, outputLen);
                return result;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(encrypted);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(padded);
        }
    }

    /// <summary>
    /// コマンドパケットを暗号化して512バイトパケットに整形
    /// </summary>
    private byte[] EncryptCommandPacket(byte[] data)
    {
        var encrypted = EncryptWithDES(data);
        var finalPacket = ArrayPool<byte>.Shared.Rent(FULL_PACKET_SIZE);

        try
        {
            Array.Clear(finalPacket, 0, FULL_PACKET_SIZE);

            // 暗号化データをコピー（最大510バイト）
            var copyLen = Math.Min(encrypted.Length, FULL_PACKET_SIZE - 2);
            Buffer.BlockCopy(encrypted, 0, finalPacket, 0, copyLen);

            // マジックバイト追加
            finalPacket[FULL_PACKET_SIZE - 2] = MAGIC_BYTES[0];  // 161
            finalPacket[FULL_PACKET_SIZE - 1] = MAGIC_BYTES[1];  // 26

            // 結果をコピーして返す
            var result = new byte[FULL_PACKET_SIZE];
            Buffer.BlockCopy(finalPacket, 0, result, 0, FULL_PACKET_SIZE);
            return result;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(finalPacket);
        }
    }

    /// <summary>
    /// デバイスにデータを書き込み、応答を読み取る
    /// </summary>
    private bool WriteToDevice(byte[] data, int timeout = COMMAND_TIMEOUT)
    {
        if (_writer == null || _reader == null)
            return false;

        try
        {
            // データ送信
            var ec = _writer.Write(data, timeout, out var transferLength);

            if (ec != ErrorCode.None)
            {
                Console.WriteLine($"Write Error: {ec}");
                return false;
            }

            // 応答読み取り
            var readBuffer = ArrayPool<byte>.Shared.Rent(512);
            try
            {
                ec = _reader.Read(readBuffer, timeout, out transferLength);

                if (ec == ErrorCode.IoTimedOut)
                {
                    Console.WriteLine("USB read operation timed out");
                    return false;
                }
                else if (ec != ErrorCode.None)
                {
                    Console.WriteLine($"Read Error: {ec}");
                    return false;
                }

                if (transferLength == 0)
                {
                    Console.WriteLine("No data received from device");
                    return false;
                }

                // バッファフラッシュ
                ReadFlush();

                return true;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(readBuffer);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to device: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 読み取りバッファをフラッシュ
    /// </summary>
    private void ReadFlush(int maxAttempts = 5)
    {
        if (_reader == null)
            return;

        var readBuffer = ArrayPool<byte>.Shared.Rent(512);
        try
        {
            for (var i = 0; i < maxAttempts; i++)
            {
                try
                {
                    var ec = _reader.Read(readBuffer, 100, out var transferLength);

                    if (ec == ErrorCode.IoTimedOut || transferLength == 0)
                        break;

                    if (ec != ErrorCode.None)
                        break;
                }
                catch
                {
                    break;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
        }
    }

    /// <summary>
    /// PNGバイト配列をデバイスに送信（コマンドID 102）
    /// </summary>
    public bool SendPngBytes(byte[] pngData)
    {
        if (pngData == null || pngData.Length == 0)
        {
            throw new ArgumentException("PNG data cannot be null or empty");
        }

        var imgSize = pngData.Length;

        // コマンドパケット作成（ID 102）
        var cmdPacket = BuildCommandPacketHeader(102);

        // 画像サイズをビッグエンディアンで格納（バイト8-11）
        cmdPacket[8] = (byte)((imgSize >> 24) & 0xFF);
        cmdPacket[9] = (byte)((imgSize >> 16) & 0xFF);
        cmdPacket[10] = (byte)((imgSize >> 8) & 0xFF);
        cmdPacket[11] = (byte)(imgSize & 0xFF);

        // パケット暗号化
        var encryptedPacket = EncryptCommandPacket(cmdPacket);

        // 暗号化パケット + PNG生データを結合
        var fullPayload = ArrayPool<byte>.Shared.Rent(encryptedPacket.Length + pngData.Length);
        try
        {
            Buffer.BlockCopy(encryptedPacket, 0, fullPayload, 0, encryptedPacket.Length);
            Buffer.BlockCopy(pngData, 0, fullPayload, encryptedPacket.Length, pngData.Length);

            // 実際のペイロードサイズ分のみを送信用に切り出す
            var payload = new byte[encryptedPacket.Length + pngData.Length];
            Buffer.BlockCopy(fullPayload, 0, payload, 0, payload.Length);

            // USB経由で送信
            return WriteToDevice(payload);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(fullPayload);
        }
    }

    /// <summary>
    /// 画面をクリア（透明PNGを送信）
    /// </summary>
    public bool ClearScreen()
    {
        // 480x1920の最小透明PNG
        var imgData = new byte[] {
            0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0x00, 0x00, 0x00, 0x0d, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x01, 0xe0, 0x00, 0x00, 0x07, 0x80, 0x08, 0x06, 0x00, 0x00, 0x00, 0x16, 0xf0, 0x84,
            0xf5, 0x00, 0x00, 0x00, 0x01, 0x73, 0x52, 0x47, 0x42, 0x00, 0xae, 0xce, 0x1c, 0xe9, 0x00, 0x00,
            0x00, 0x04, 0x67, 0x41, 0x4d, 0x41, 0x00, 0x00, 0xb1, 0x8f, 0x0b, 0xfc, 0x61, 0x05, 0x00, 0x00,
            0x00, 0x09, 0x70, 0x48, 0x59, 0x73, 0x00, 0x00, 0x0e, 0xc3, 0x00, 0x00, 0x0e, 0xc3, 0x01, 0xc7,
            0x6f, 0xa8, 0x64, 0x00, 0x00, 0x0e, 0x0c, 0x49, 0x44, 0x41, 0x54, 0x78, 0x5e, 0xed, 0xc1, 0x01,
            0x0d, 0x00, 0x00, 0x00, 0xc2, 0xa0, 0xf7, 0x4f, 0x6d, 0x0f, 0x07, 0x14, 0x00, 0x00, 0x00, 0x00,
        };

        // 3568バイトのゼロパディング
        var paddingZeros = new byte[3568];

        // PNGフッター
        var footer = new byte[] {
            0x00, 0xf0, 0x66, 0x4a, 0xc8, 0x00, 0x01, 0x11, 0x9d, 0x82, 0x0a, 0x00, 0x00, 0x00, 0x00, 0x49,
            0x45, 0x4e, 0x44, 0xae, 0x42, 0x60, 0x82
        };

        // 結合
        var fullImgData = new byte[imgData.Length + paddingZeros.Length + footer.Length];
        Buffer.BlockCopy(imgData, 0, fullImgData, 0, imgData.Length);
        Buffer.BlockCopy(paddingZeros, 0, fullImgData, imgData.Length, paddingZeros.Length);
        Buffer.BlockCopy(footer, 0, fullImgData, imgData.Length + paddingZeros.Length, footer.Length);

        return SendPngBytes(fullImgData);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                if (_reader != null)
                {
                    _reader.Dispose();
                    _reader = null;
                }

                if (_writer != null)
                {
                    _writer.Dispose();
                    _writer = null;
                }

                if (_device != null)
                {
                    if (_device is IUsbDevice wholeUsbDevice)
                    {
                        wholeUsbDevice.ReleaseInterface(0);
                    }
                    _device.Close();
                    _device = null;
                }

                UsbDevice.Exit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during disposal: {ex.Message}");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
