using System.Text;

using PCSC;
using PCSC.Monitoring;

Console.Write("PIN(4桁)を入力してください: ");
var pin = Console.ReadLine();
if (string.IsNullOrEmpty(pin) || pin.Length != 4)
{
    Console.WriteLine("PINは4桁の数字である必要があります");
    return;
}

using var reader = new MynaReader(pin);
reader.Start();

Console.ReadLine();

reader.Stop();

internal sealed class MynaReader : IDisposable
{
    private readonly ISCardMonitor monitor;

    private readonly string pin;

    public bool IsRunning { get; private set; }

    public MynaReader(string pin)
    {
        monitor = MonitorFactory.Instance.Create(SCardScope.System);
        monitor.CardInserted += OnCardInserted;

        this.pin = pin;
    }

    public void Dispose()
    {
        MonitorFactory.Instance.Release(monitor);
    }


    private void OnCardInserted(object sender, CardStatusEventArgs e)
    {
        using var context = ContextFactory.Instance.Establish(SCardScope.System);
        var reader = context.ConnectReader(e.ReaderName, SCardShareMode.Shared, SCardProtocol.Any);
        try
        {
            // 券面事項入力補助APの選択: SELECT(0xA4) AIDによる選択(0x04) 券面事項入力補助AP[D3 92 10 00 31 00 01 01 01 01]
            var response = SendCommand(reader, CreateCommand(0x00, 0xA4, 0x04, 0x0C, [0xD3, 0x92, 0x10, 0x00, 0x31, 0x00, 0x01, 0x01, 0x04, 0x08]));
            if (!response.IsSuccess())
            {
                Console.WriteLine($"AP選択失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }

            // PINの選択: SELECT(0xA4) FIDによる選択(0x02)
            response = SendCommand(reader, CreateCommand(0x00, 0xA4, 0x02, 0x0C, [0x00, 0x11]));
            if (!response.IsSuccess())
            {
                Console.WriteLine($"暗証番号選択失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }

            // VERIFYコマンドでPIN認証: VERIFY(0x20)
            response = SendCommand(reader, CreateCommand(0x00, 0x20, 0x00, 0x80, Encoding.ASCII.GetBytes(pin)));
            if (!response.IsSuccess())
            {
                Console.WriteLine($"PIN認証失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }

            // 個人番号選択: SELECT(0xA4) FIDによる選択(0x02)
            response = SendCommand(reader, CreateCommand(0x00, 0xA4, 0x02, 0x0C, [0x00, 0x01]));
            if (!response.IsSuccess())
            {
                Console.WriteLine($"マイナンバー選択失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }

            // 個人番号読み取り: READ BINARY(0xB0)
            response = SendCommand(reader, CreateCommand(0x00, 0xB0, 0x00, 0x00, 0x00));
            if (!response.IsSuccess())
            {
                Console.WriteLine($"個人番号読み取り失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }

            var id = Encoding.ASCII.GetString(response.Data.Slice(3, 12));
            Console.WriteLine($"個人番号: {id}");

            // 基本4情報選択: SELECT(0xA4) FIDによる選択(0x02)
            response = SendCommand(reader, CreateCommand(0x00, 0xA4, 0x02, 0x0C, [0x00, 0x02]));
            if (!response.IsSuccess())
            {
                Console.WriteLine($"基本4情報選択失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }

            // データ長読み取り: READ BINARY(0xB0) offset=0x02, length=1
            response = SendCommand(reader, CreateCommand(0x00, 0xB0, 0x00, 0x02, 0x01));
            if (!response.IsSuccess())
            {
                Console.WriteLine($"データ長読み取り失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }

            var length = response.Data[0];
            Console.WriteLine($"データ長: {length}");

            // 基本4情報読み取り: READ BINARY(0xB0) offset=0x03, length=length
            response = SendCommand(reader, CreateCommand(0x00, 0xB0, 0x00, 0x03, length));
            if (!response.IsSuccess())
            {
                Console.WriteLine($"基本4情報読み取り読み取り失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }

            // パース
            var map = ParseTlv(response.Data);
            //Console.WriteLine($"制御情報(DF21): {Convert.ToHexString(map.GetValueOrDefault(0xDF21, []))}");
            Console.WriteLine($"氏名(DF22): {Encoding.UTF8.GetString(map.GetValueOrDefault(0xDF22, []))}");
            Console.WriteLine($"住所(DF23): {Encoding.UTF8.GetString(map.GetValueOrDefault(0xDF23, []))}");
            Console.WriteLine($"生年月日(DF24): {Encoding.ASCII.GetString(map.GetValueOrDefault(0xDF24, []))}");
            Console.WriteLine($"性別(DF25): {Encoding.ASCII.GetString(map.GetValueOrDefault(0xDF25, []))}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            reader.Disconnect(SCardReaderDisposition.Leave);
        }
    }

    // 送信用コマンド作成
    private static byte[] CreateCommand(byte cla, byte ins, byte p1, byte p2, byte[] data)
    {
        var command = new byte[4 + 1 + data.Length];
        command[0] = cla;
        command[1] = ins;
        command[2] = p1;
        command[3] = p2;
        command[4] = (byte)data.Length; // Lc
        data.CopyTo(command.AsSpan(5, data.Length));
        return command;
    }

    // 受信用コマンド作成
    private static byte[] CreateCommand(byte cla, byte ins, byte p1, byte p2, int le)
    {
        var command = new byte[5];
        command[0] = cla;
        command[1] = ins;
        command[2] = p1;
        command[3] = p2;
        command[4] = (byte)le; // Le
        return command;
    }

    private Response SendCommand(ICardReader reader, byte[] command)
    {
        Console.WriteLine($"送信: {BitConverter.ToString(command)}");

        var receiveBuffer = new byte[258]; // SW1+SW2を含む
        var bytesReceived = reader.Transmit(command, receiveBuffer);

        Console.WriteLine($"受信: {BitConverter.ToString(receiveBuffer, 0, bytesReceived)}");

        return new Response(receiveBuffer, bytesReceived);
    }

    private static Dictionary<int, byte[]> ParseTlv(ReadOnlySpan<byte> tlvData)
    {
        var map = new Dictionary<int, byte[]>();

        var index = 0;
        while (index < tlvData.Length)
        {
            if (index >= tlvData.Length)
            {
                break;
            }

            var tag1 = tlvData[index++];
            int tag;

            if (tag1 == 0xDF)
            {
                // 2バイトタグ)
                if (index >= tlvData.Length)
                {
                    break;
                }

                var tag2 = tlvData[index++];
                tag = (tag1 << 8) | tag2;
            }
            else
            {
                // 1バイトタグ
                tag = tag1;
            }

            if (index >= tlvData.Length)
            {
                break;
            }

            var length = tlvData[index++];
            if (index + length > tlvData.Length)
            {
                break;
            }

            var value = tlvData.Slice(index, length).ToArray();

            map[tag] = value;

            index += length;
        }

        return map;
    }

    public bool Start()
    {
        if (IsRunning)
        {
            return false;
        }

        using var context = ContextFactory.Instance.Establish(SCardScope.System);
        var readers = context.GetReaders();
        if (readers.Length == 0)
        {
            return false;
        }

        monitor.Start(readers[0]);

        IsRunning = true;

        return true;
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        monitor.Cancel();

        IsRunning = false;
    }

    private sealed class Response
    {
        private readonly byte[] buffer;

        private readonly int length;

        public ReadOnlySpan<byte> Data => buffer.AsSpan(0, length >= 2 ? length - 2 : 0);

        public byte SW1 { get; }

        public byte SW2 { get; }

        public Response(byte[] buffer, int length)
        {
            this.buffer = buffer;
            this.length = length;

            if (length >= 2)
            {
                SW1 = buffer[length - 2];
                SW2 = buffer[length - 1];
            }
            else
            {
                SW1 = 0x00;
                SW2 = 0x00;
            }
        }

        public bool IsSuccess() => SW1 == 0x90 && SW2 == 0x00;
    }
}
