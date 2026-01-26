using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;

namespace WorkTssUsb;

internal class Program
{
    static void Main(string[] args)
    {
        var finder = new UsbDeviceFinder(0x1CBE, 0x0088);
        var device = UsbDevice.OpenUsbDevice(finder);
        if (device is null)
        {
            Console.WriteLine("Device not found.");
            return;
        }

        using var screen = new ScreenDevice(device);

        if (!screen.Sync())
        {
            Debug.WriteLine("****0");
        }

        if (!screen.SetOrientation(Orientation.Portrait))
        {
            Debug.WriteLine("****1");
        }
        if (!screen.SetOrientation(Orientation.ReversePortrait))
        {
            Debug.WriteLine("****2");
        }
        if (!screen.SetOrientation(Orientation.Portrait))
        {
            Debug.WriteLine("****3");
        }

        if (!screen.DrawJpeg(File.ReadAllBytes("usa.jpg")))
        {
            Debug.WriteLine("****3");
        }

        for (var i = 0; i <= 100; i++)
        {
            var ret = screen.SetBrightness((byte)i);
            if (!ret)
            {
                Debug.WriteLine("*" + i);
            }
            Thread.Sleep(1);
        }

        UsbDevice.Exit();
    }
}

public enum Orientation : byte
{
    Portrait = 0,
    Landscape = 1,
    ReversePortrait = 2,
    ReverseLandscape = 3
}

public sealed class ScreenDevice : IDisposable
{
    private const int CommandSize = 500;
    private const int PaddingCommandSize = (CommandSize + 7) & ~7;
    private const int PacketSize = 512;

    private const int WriteTimeout = 1500;
    private const int ReadTimeout = 1500;

    private static readonly byte[] KeyIv = "slv3tuzx"u8.ToArray();

    private readonly UsbDevice usbDevice;

    private readonly UsbEndpointReader reader;

    private readonly UsbEndpointWriter writer;

    private readonly DES des = DES.Create();

    private byte[] commandBuffer;

    private byte[] encryptedBuffer;

    private byte[] readBuffer;

    // --------------------------------------------------------------------------------
    // Constructor
    // --------------------------------------------------------------------------------

    public ScreenDevice(UsbDevice usbDevice)
    {
        this.usbDevice = usbDevice;


        if (usbDevice is IUsbDevice wholeUsbDevice)
        {
            wholeUsbDevice.SetConfiguration(1);
            wholeUsbDevice.ClaimInterface(0);
        }

        reader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep01);
        writer = usbDevice.OpenEndpointWriter(WriteEndpointID.Ep01);

        des.Key = KeyIv;
        des.IV = KeyIv;

        commandBuffer = ArrayPool<byte>.Shared.Rent(PaddingCommandSize);
        encryptedBuffer = ArrayPool<byte>.Shared.Rent(PacketSize);
        readBuffer = ArrayPool<byte>.Shared.Rent(PacketSize);
    }

    public void Dispose()
    {
        if (usbDevice.IsOpen)
        {
            reader.Dispose();
            writer.Dispose();

            if (usbDevice is IUsbDevice wholeUsbDevice)
            {
                wholeUsbDevice.ReleaseInterface(0);
            }

            usbDevice.Close();
        }

        if (commandBuffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(commandBuffer);
            commandBuffer = [];
        }
        if (encryptedBuffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(encryptedBuffer);
            encryptedBuffer = [];
        }
        if (readBuffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
            readBuffer = [];
        }

        des.Dispose();
    }

    // --------------------------------------------------------------------------------
    // Helper
    // --------------------------------------------------------------------------------

    private void PrepareCommandHeader(byte commandId)
    {
        commandBuffer.AsSpan().Clear();

        commandBuffer[0] = commandId;

        commandBuffer[2] = 0x1A;
        commandBuffer[3] = 0x6D;

        var timestamp = (int)DateTime.Now.TimeOfDay.TotalMilliseconds;
        BinaryPrimitives.WriteInt32LittleEndian(commandBuffer.AsSpan(4, 4), timestamp);
    }

    private bool RequestResponse(byte[]? data = null)
    {
        encryptedBuffer.AsSpan().Clear();
        des.EncryptCbc(commandBuffer.AsSpan(0, PaddingCommandSize), KeyIv, encryptedBuffer, PaddingMode.None);

        // End marker
        encryptedBuffer[PacketSize - 2] = 0xA1;
        encryptedBuffer[PacketSize - 1] = 0x1A;

        // TODO Timeout behavior ?
        var errorCode = writer.Write(encryptedBuffer, 0, PacketSize, WriteTimeout, out var transferLength);
        if ((errorCode != ErrorCode.None) || (transferLength != PacketSize))
        {
            return false;
        }

        if (data is not null)
        {
            errorCode = writer.Write(data, 0, data.Length, WriteTimeout, out transferLength);
            if ((errorCode != ErrorCode.None) || (transferLength != data.Length))
            {
                return false;
            }
        }

        errorCode = reader.Read(readBuffer, 0, PacketSize, ReadTimeout, out transferLength);
        var result = (errorCode == ErrorCode.None) && (transferLength == PacketSize);

        // TODO Discard ?
        reader.ReadFlush();

        return result;
    }

    // --------------------------------------------------------------------------------
    // Command
    // --------------------------------------------------------------------------------

    // TODO reset?

    public bool Sync()
    {
        PrepareCommandHeader(10);
        return RequestResponse();
    }

    public bool SetOrientation(Orientation value)
    {
        PrepareCommandHeader(13);
        commandBuffer[8] = (byte)value;
        return RequestResponse();
    }

    public bool SetBrightness(byte value)
    {
        PrepareCommandHeader(14);
        commandBuffer[8] = value;
        return RequestResponse();
    }

    public bool DrawPng(byte[] imageBytes)
    {
        PrepareCommandHeader(102);
        BinaryPrimitives.WriteInt32BigEndian(commandBuffer.AsSpan(8, 4), imageBytes.Length);
        return RequestResponse(imageBytes);
    }

    public bool DrawJpeg(byte[] imageBytes)
    {
        PrepareCommandHeader(101);
        BinaryPrimitives.WriteInt32BigEndian(commandBuffer.AsSpan(8, 4), imageBytes.Length);
        return RequestResponse(imageBytes);
    }

    // TODO clear
}
