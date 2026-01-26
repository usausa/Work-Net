using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace BTWATTCH2Controller;

// コンソールアプリケーション
class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("RS-BTWATTCH2 Controller");
            Console.WriteLine("Enter device Bluetooth address (e.g., AA:BB:CC:DD:EE:FF):");
            //var address = Console.ReadLine();
            var address = "E0:7F:A2:74:24:1C"; // <- ここにデバイスのBluetoothアドレスを入力してください

            var wattchecker = new BTWATTCH2(address);

            wattchecker.MeasurementReceived += (sender, e) =>
            {
                Console.WriteLine($"{{\"datetime\":\"{e.Timestamp:yyyy-MM-dd HH:mm:ss}\", " +
                                  $"\"wattage\":{e.Wattage:F3}, " +
                                  $"\"voltage\":{e.Voltage:F3}, " +
                                  $"\"current\":{e.Current:F3}}}");
            };

            if (!await wattchecker.ConnectAsync())
            {
                Console.WriteLine("Failed to connect.");
                return;
            }

            await wattchecker.SetTimerAsync();
            Console.WriteLine("Timer set.");

            Console.WriteLine("\nCommands: on, off, measure, exit");

            while (true)
            {
                Console.Write("> ");
                var command = Console.ReadLine()?.ToLower();

                switch (command)
                {
                    case "on":
                        await wattchecker.TurnOnAsync();
                        Console.WriteLine("Turned ON");
                        break;
                    case "off":
                        await wattchecker.TurnOffAsync();
                        Console.WriteLine("Turned OFF");
                        break;
                    case "measure":
                        await wattchecker.MeasureAsync();
                        break;
                    case "exit":
                        wattchecker.Dispose();
                        return;
                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}

public class MeasurementEventArgs : EventArgs
{
    public DateTime Timestamp { get; set; }
    public double Wattage { get; set; }
    public double Voltage { get; set; }
    public double Current { get; set; }
}

public class BTWATTCH2 : IDisposable
{
    private static readonly Guid UartServiceUuid = Guid.Parse("6E400001-B5A3-F393-E0A9-E50E24DCCA9E");
    private static readonly Guid UartTxUuid = Guid.Parse("6E400002-B5A3-F393-E0A9-E50E24DCCA9E");
    private static readonly Guid UartRxUuid = Guid.Parse("6E400003-B5A3-F393-E0A9-E50E24DCCA9E");

    private static readonly byte[] CMD_HEADER = new byte[] { 0xAA };
    private static readonly byte[] ID_TIMER = new byte[] { 0x01 };
    private static readonly byte[] ID_TURN_ON = new byte[] { 0xA7, 0x01 };
    private static readonly byte[] ID_TURN_OFF = new byte[] { 0xA7, 0x00 };
    private static readonly byte[] ID_ENERGY_USAGE = new byte[] { 0x08 };

    private BluetoothLEDevice device;
    private GattCharacteristic txCharacteristic;
    private GattCharacteristic rxCharacteristic;
    private byte[] receiveBuffer = new byte[0];

    public string Address { get; private set; }
    public event EventHandler<MeasurementEventArgs> MeasurementReceived;

    public BTWATTCH2(string address)
    {
        Address = address;
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            var cleanAddress = Address.Replace(":", "").Replace("-", "").Trim();
            if (cleanAddress.Length != 12)
            {
                Console.WriteLine("Invalid address format.");
                return false;
            }

            device = await BluetoothLEDevice.FromBluetoothAddressAsync(Convert.ToUInt64(cleanAddress, 16));
            if (device == null)
            {
                Console.WriteLine("Bluetooth connect failed.");
                return false;
            }

            var servicesResult = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            if (servicesResult.Status != GattCommunicationStatus.Success)
            {
                Console.WriteLine($"Get services failed. Status={servicesResult.Status}");
                return false;
            }

            var service = servicesResult.Services.FirstOrDefault(s => s.Uuid == UartServiceUuid);
            if (service == null)
            {
                Console.WriteLine("UART service not found.");
                return false;
            }

            var access = await service.RequestAccessAsync();
            if (access != DeviceAccessStatus.Allowed)
            {
                Console.WriteLine($"Request access failed. Status={access}");
                return false;
            }

            var charsResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            if (charsResult.Status != GattCommunicationStatus.Success)
            {
                Console.WriteLine($"Get characteristics failed. Status={charsResult.Status}");
                return false;
            }

            txCharacteristic = charsResult.Characteristics.FirstOrDefault(c => c.Uuid == UartTxUuid);
            rxCharacteristic = charsResult.Characteristics.FirstOrDefault(c => c.Uuid == UartRxUuid);

            if (txCharacteristic == null || rxCharacteristic == null)
            {
                Console.WriteLine("TX or RX characteristic not found.");
                return false;
            }

            // 通知を有効化
            var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;
            var status = await rxCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(cccdValue);
            if (status != GattCommunicationStatus.Success)
            {
                Console.WriteLine($"Failed to enable notifications. Status={status}");
                return false;
            }

            rxCharacteristic.ValueChanged += OnValueChanged;

            Console.WriteLine("Connected successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection error: {ex.Message}");
            return false;
        }
    }

    public async Task SetTimerAsync()
    {
        var now = DateTime.Now;
        var payload = new byte[]
        {
            ID_TIMER[0],
            (byte)now.Second,
            (byte)now.Minute,
            (byte)now.Hour,
            (byte)now.Day,
            (byte)(now.Month - 1),
            (byte)(now.Year - 1900),
            (byte)now.DayOfWeek
        };

        await WriteCommandAsync(payload);
    }

    public async Task TurnOnAsync()
    {
        await WriteCommandAsync(ID_TURN_ON);
    }

    public async Task TurnOffAsync()
    {
        await WriteCommandAsync(ID_TURN_OFF);
    }

    public async Task MeasureAsync()
    {
        await WriteCommandAsync(ID_ENERGY_USAGE);
        await Task.Delay(1100); // 1.1秒待機
    }

    private async Task WriteCommandAsync(byte[] payload)
    {
        var command = PackCommand(payload);
        using var writer = new DataWriter();
        writer.WriteBytes(command);
        var buffer = writer.DetachBuffer();

        var writeStatus = await txCharacteristic.WriteValueAsync(buffer, GattWriteOption.WriteWithResponse);
        if (writeStatus != GattCommunicationStatus.Success)
        {
            Console.WriteLine($"WriteValueAsync failed. Status={writeStatus}");
        }
    }

    private byte[] PackCommand(byte[] payload)
    {
        var length = BitConverter.GetBytes((ushort)payload.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(length); // Big endian

        var crc = CalculateCrc8(payload);

        var command = new byte[CMD_HEADER.Length + length.Length + payload.Length + 1];
        var offset = 0;

        Array.Copy(CMD_HEADER, 0, command, offset, CMD_HEADER.Length);
        offset += CMD_HEADER.Length;

        Array.Copy(length, 0, command, offset, length.Length);
        offset += length.Length;

        Array.Copy(payload, 0, command, offset, payload.Length);
        offset += payload.Length;

        command[offset] = crc;

        return command;
    }

    private byte CalculateCrc8(byte[] payload)
    {
        const byte POLYNOMIAL = 0x85;
        const byte MSBIT = 0x80;

        byte crc = 0x00;

        foreach (var b in payload)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                if ((crc & MSBIT) != 0)
                {
                    crc = (byte)((crc << 1) ^ POLYNOMIAL);
                }
                else
                {
                    crc = (byte)(crc << 1);
                }
            }
        }

        return crc;
    }

    private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        var data = new byte[args.CharacteristicValue.Length];
        using (var reader = DataReader.FromBuffer(args.CharacteristicValue))
        {
            reader.ReadBytes(data);
        }

        ProcessReceivedData(data);
    }

    private void ProcessReceivedData(byte[] data)
    {
        // バッファに追加
        var newBuffer = new byte[receiveBuffer.Length + data.Length];
        Array.Copy(receiveBuffer, newBuffer, receiveBuffer.Length);
        Array.Copy(data, 0, newBuffer, receiveBuffer.Length, data.Length);
        receiveBuffer = newBuffer;

        // パケット解析
        if (receiveBuffer.Length >= 4 && receiveBuffer[0] == CMD_HEADER[0])
        {
            var payloadLength = (receiveBuffer[1] << 8) | receiveBuffer[2];
            var totalLength = 1 + 2 + payloadLength + 1; // Header + Length + Payload + CRC

            if (receiveBuffer.Length >= totalLength)
            {
                var packet = new byte[totalLength];
                Array.Copy(receiveBuffer, packet, totalLength);

                // CRC検証
                var payload = new byte[payloadLength + 1]; // Payload + CRC
                Array.Copy(packet, 3, payload, 0, payload.Length);

                if (CalculateCrc8(payload) == 0)
                {
                    ClassifyResponse(packet);
                }

                // バッファをクリア
                receiveBuffer = new byte[0];
            }
        }
    }

    private void ClassifyResponse(byte[] data)
    {
        if (data.Length > 3 && data[3] == ID_ENERGY_USAGE[0])
        {
            var measurement = DecodeMeasurement(data);
            MeasurementReceived?.Invoke(this, measurement);
        }
    }

    private MeasurementEventArgs DecodeMeasurement(byte[] data)
    {
        // リトルエンディアンで6バイトの整数を読み取る
        long voltage = BitConverter.ToInt32(new byte[] { data[5], data[6], data[7], data[8], 0, 0, 0, 0 }, 0) |
                      ((long)data[9] << 32) | ((long)data[10] << 40);

        long current = BitConverter.ToInt32(new byte[] { data[11], data[12], data[13], data[14], 0, 0, 0, 0 }, 0) |
                      ((long)data[15] << 32) | ((long)data[16] << 40);

        long wattage = BitConverter.ToInt32(new byte[] { data[17], data[18], data[19], data[20], 0, 0, 0, 0 }, 0) |
                      ((long)data[21] << 32) | ((long)data[22] << 40);

        var timestamp = new DateTime(
            1900 + data[28],
            data[27] + 1,
            data[26],
            data[25],
            data[24],
            data[23]
        );

        return new MeasurementEventArgs
        {
            Voltage = voltage / Math.Pow(16, 6),
            Current = current / Math.Pow(32, 6) * 1000,
            Wattage = wattage / Math.Pow(16, 6),
            Timestamp = timestamp
        };
    }

    public void Dispose()
    {
        if (rxCharacteristic != null)
        {
            rxCharacteristic.ValueChanged -= OnValueChanged;
        }
        device?.Dispose();
    }
}
