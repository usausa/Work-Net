namespace WorkRead;

using System.Text;

using PCSC;
using PCSC.Monitoring;

internal static class Program
{
    static void Main()
    {
        // PIN入力
        Console.Write("券面事項入力補助用PIN(4桁)を入力してください: ");
        var pin = Console.ReadLine();
        if (string.IsNullOrEmpty(pin) || pin.Length != 4)
        {
            Console.WriteLine("PINは4桁の数字である必要があります");
            return;
        }

        using var reader = new Reader(pin);
        reader.Start();

        Console.WriteLine("* Start");

        Console.ReadLine();

        reader.Stop();

        Console.WriteLine("* Stop");
    }
}

internal sealed class Reader : IDisposable
{
    private readonly ISCardMonitor monitor;

    private readonly string pin;

    public bool IsRunning { get; private set; }

    public Reader(string pin)
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
            Console.WriteLine($"接続プロトコル: {reader.Protocol}");

            // 券面事項入力補助APの選択
            var selectApData = new byte[] { 0xD3, 0x92, 0x10, 0x00, 0x31, 0x00, 0x01, 0x01, 0x04, 0x08 };
            var selectAp = new byte[4 + 1 + selectApData.Length];
            selectAp[0] = 0x00; // CLA
            selectAp[1] = 0xA4; // INS
            selectAp[2] = 0x04; // P1
            selectAp[3] = 0x0C; // P2
            selectAp[4] = (byte)selectApData.Length; // Lc
            selectApData.CopyTo(selectAp.AsSpan(5, selectApData.Length));

            Console.WriteLine("券面事項入力補助APを選択...");
            var response = SendCommand(reader, selectAp);
            if (!response.IsSuccess())
            {
                Console.WriteLine($"AP選択失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }
            Console.WriteLine("AP選択成功!");

            // 券面入力補助用PIN(EF)の選択
            var selectPinData = new byte[] { 0x00, 0x11 };
            var selectPin = new byte[4 + 1 + selectPinData.Length];
            selectPin[0] = 0x00; // CLA
            selectPin[1] = 0xA4; // INS
            selectPin[2] = 0x02; // P1
            selectPin[3] = 0x0C; // P2
            selectPin[4] = (byte)selectPinData.Length; // Lc
            selectPinData.CopyTo(selectPin.AsSpan(5, selectPinData.Length));

            Console.WriteLine("暗証番号を選択...");
            response = SendCommand(reader, selectPin);
            if (!response.IsSuccess())
            {
                Console.WriteLine($"暗証番号選択失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }
            Console.WriteLine("暗証番号選択成功!");

            // VERIFYコマンドでPIN認証
            var pinBytes = Encoding.ASCII.GetBytes(pin);
            var verifyPin = new byte[4 + 1 + pinBytes.Length];
            verifyPin[0] = 0x00; // CLA
            verifyPin[1] = 0x20; // INS
            verifyPin[2] = 0x00; // P1
            verifyPin[3] = 0x80; // P2
            verifyPin[4] = (byte)pinBytes.Length; // Lc
            pinBytes.CopyTo(verifyPin.AsSpan(5, pinBytes.Length));

            Console.WriteLine("PIN認証...");
            response = SendCommand(reader, verifyPin);
            if (!response.IsSuccess())
            {
                Console.WriteLine($"PIN認証失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }
            Console.WriteLine("PIN認証成功!");

            // マイナンバー選択
            var selectMyNumberData = new byte[] { 0x00, 0x01 };
            var selectMyNumber = new byte[4 + 1 + selectMyNumberData.Length];
            selectMyNumber[0] = 0x00; // CLA
            selectMyNumber[1] = 0xA4; // INS
            selectMyNumber[2] = 0x02; // P1
            selectMyNumber[3] = 0x0C; // P2
            selectMyNumber[4] = (byte)selectMyNumberData.Length; // Lc
            selectMyNumberData.CopyTo(selectMyNumber.AsSpan(5, selectMyNumberData.Length));

            Console.WriteLine("マイナンバー選択...");
            response = SendCommand(reader, selectMyNumber);
            if (!response.IsSuccess())
            {
                Console.WriteLine($"マイナンバー選択失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }
            Console.WriteLine("マイナンバー選択成功!");

            // 個人番号読み取り
            var readId = new byte[] { 0x00, 0xB0, 0x00, 0x00, 0x00 };

            Console.WriteLine("個人番号読み取り...");
            response = SendCommand(reader, readId);
            if (!response.IsSuccess())
            {
                Console.WriteLine($"個人番号読み取り失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }
            Console.WriteLine("個人番号読み取り成功!");

            var id = Encoding.ASCII.GetString(response.Data.Slice(3, 12));
            Console.WriteLine($"個人番号: {id}");

            // 基本4情報選択
            var selectInformationData = new byte[] { 0x00, 0x02 };
            var selectInformation = new byte[4 + 1 + selectInformationData.Length];
            selectInformation[0] = 0x00; // CLA
            selectInformation[1] = 0xA4; // INS
            selectInformation[2] = 0x02; // P1
            selectInformation[3] = 0x0C; // P2
            selectInformation[4] = (byte)selectInformationData.Length; // Lc
            selectInformationData.CopyTo(selectInformation.AsSpan(5, selectInformationData.Length));

            Console.WriteLine("基本4情報選択...");
            response = SendCommand(reader, selectInformation);
            if (!response.IsSuccess())
            {
                Console.WriteLine($"基本4情報選択失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }
            Console.WriteLine("基本4情報選択成功!");

            // データ長読み取り
            var readLength = new byte[] { 0x00, 0xB0, 0x00, 0x02, 0x01 };

            Console.WriteLine("データ長読み取り...");
            response = SendCommand(reader, readLength);
            if (!response.IsSuccess())
            {
                Console.WriteLine($"データ長読み取り失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }
            Console.WriteLine("データ長読み取り成功!");

            var length = response.Data[0];
            Console.WriteLine($"データ長: {length}");

            // 基本4情報読み取り
            var readInformation = new byte[] { 0x00, 0xB0, 0x00, 0x00, (byte)(3 + length) };

            Console.WriteLine("基本4情報読み取り読み取り...");
            response = SendCommand(reader, readInformation);
            if (!response.IsSuccess())
            {
                Console.WriteLine($"基本4情報読み取り読み取り失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }
            Console.WriteLine("基本4情報読み取り読み取り成功!");

            // パース
            var map = ParseTlv(response.Data);
            Console.WriteLine($"制御情報(DF21): {Convert.ToHexString(map.GetValueOrDefault(0xDF21, []))}");
            Console.WriteLine($"氏名(DF22): {Encoding.UTF8.GetString(map.GetValueOrDefault(0xDF22, []))}");
            Console.WriteLine($"住所(DF23): {Encoding.UTF8.GetString(map.GetValueOrDefault(0xDF23, []))}");
            Console.WriteLine($"生年月日(DF24): {Encoding.ASCII.GetString(map.GetValueOrDefault(0xDF24, []))}");
            Console.WriteLine($"性別(DF25): {Encoding.ASCII.GetString(map.GetValueOrDefault(0xDF25, []))}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"エラー: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            reader.Disconnect(SCardReaderDisposition.Leave);
        }
    }

    private ResponseData SendCommand(ICardReader reader, byte[] command)
    {
        var receiveBuffer = new byte[258]; // SW1+SW2を含む

        Console.WriteLine($"送信: {BitConverter.ToString(command)}");

        var bytesReceived = reader.Transmit(command, receiveBuffer);

        Console.WriteLine($"受信: {BitConverter.ToString(receiveBuffer, 0, bytesReceived)}");

        return new ResponseData(receiveBuffer, bytesReceived);
    }

    private sealed class ResponseData
    {
        private readonly byte[] buffer;

        private readonly int length;

        public ReadOnlySpan<byte> Data => buffer.AsSpan(0, length >= 2 ? length - 2 : 0);

        public byte SW1 { get; }

        public byte SW2 { get; }

        public ResponseData(byte[] buffer, int length)
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

        public bool IsSuccess()
        {
            return SW1 == 0x90 && SW2 == 0x00;
        }
    }

    private static Dictionary<int, byte[]> ParseTlv(ReadOnlySpan<byte> tlvData)
    {
        var index = tlvData.IndexOf((byte)0xDF);
        if (index < 0)
        {
            return [];
        }

        var map = new Dictionary<int, byte[]>();
        while (index < tlvData.Length)
        {
            // タグ
            if (index >= tlvData.Length)
            {
                break;
            }

            var tag1 = tlvData[index++];
            int tag;

            if (tag1 == 0xDF)
            {
                // 2バイトタグ (DFxx)
                if (index >= tlvData.Length)
                {
                    break;
                }

                var tag2 = tlvData[index++];
                tag = (tag1 << 8) | tag2;
            }
            else
            {
                // 1バイトタグ (FF や他のタグを想定)
                tag = tag1;
            }

            // 長さ（1バイト長さ想定）
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

            // Override
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
}

