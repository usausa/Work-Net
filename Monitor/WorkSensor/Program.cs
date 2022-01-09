namespace WorkSensor;

using System.Buffers.Binary;
using System.IO.Ports;

public static class Program
{
    public static void Main()
    {
        var port = new SerialPort("COM9")
        {
            BaudRate = 115200,
            DataBits = 8,
            StopBits = StopBits.One,
            Parity = Parity.None,
            Handshake = Handshake.None
        };

        port.ReadTimeout = 5000;

        port.Open();
        port.DiscardOutBuffer();
        port.DiscardInBuffer();

        // Send
        var command = new byte[]
        {
            0x52, 0x42,         // Header
            0x05, 0x00,         // Length 5
            0x01, 0x21, 0x50,   // Read 0x5021
            0x00, 0x00          // CRC Area
        };

        var crc = CalcCrc(command.AsSpan(0, command.Length - 2));
        BinaryPrimitives.WriteUInt16LittleEndian(command.AsSpan(command.Length - 2, 2), crc);

        Console.WriteLine("Send: " + BitConverter.ToString(command, 0, command.Length));

        port.Write(command, 0, command.Length);

        var buffer = new byte[256];
        try
        {
            var read = 0;
            var length = 4;
            while (true)
            {
                read += port.Read(buffer, read, length - read);
                if (read == length)
                {
                    if (read == 4)
                    {
                        length += BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(2, 2));
                    }
                    else
                    {
                        break;
                    }
                }
            }
            Console.WriteLine("Recv: " + BitConverter.ToString(buffer, 0, read));

            var temperature = (float)BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(8, 2)) / 100;
            var humidity = (float)BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(10, 2)) / 100;
            var light = (float)BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(12, 2));
            var barometric = (float)BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(14, 4)) / 1000;
            var noise = (float)BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(18, 2)) / 100;

            var discomfort = (float)BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(24, 2)) / 100;
            var heat = (float)BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(26, 2)) / 100;

            var etvoc = (float)BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(20, 2));
            var eco2 = (float)BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(22, 2));

            var seismic = (float)BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(33, 2)) / 1000;
            var vibration = (float)buffer[28];
            var si = (float)BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(29, 2)) / 10;
            var pga = (float)BinaryPrimitives.ReadInt16LittleEndian(buffer.AsSpan(31, 2)) / 10;

            Console.WriteLine($"Temperature(C): {temperature}");
            Console.WriteLine($"Humidity(%): {humidity}");
            Console.WriteLine($"Light(lx): {light}");
            Console.WriteLine($"Barometric pressure(hPa): {barometric}");
            Console.WriteLine($"Sound noise(dB): {noise}");

            Console.WriteLine($"Discomfort index(): {discomfort}");
            Console.WriteLine($"Heat stroke(): {heat}");

            Console.WriteLine($"eTVOC(): {etvoc}");
            Console.WriteLine($"eCO2(): {eco2}");

            Console.WriteLine($"Seismic intensity(): {seismic}");
            Console.WriteLine($"Vibration information(): {vibration}");
            Console.WriteLine($"SI value(): {si}");
            Console.WriteLine($"PGA(): {pga}");
        }
        catch (TimeoutException e)
        {
            Console.WriteLine(e);
        }
    }

    private static ushort CalcCrc(Span<byte> span)
    {
        var crc = (ushort)0xFFFF;
        for (var i = 0; i < span.Length; i++)
        {
            crc = (ushort)(crc ^ span[i]);
            for (var j = 0; j < 8; j++)
            {
                var carry = crc & 1;
                if (carry != 0)
                {
                    crc = (ushort)((crc >> 1) ^ 0xA001);
                }
                else
                {
                    crc >>= 1;
                }
            }
        }

        return crc;
    }
}
