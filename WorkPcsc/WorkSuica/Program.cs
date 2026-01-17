namespace WorkSuica;

using PCSC;

using System.Buffers.Binary;

internal static class Program
{
    public static void Main()
    {
        using var context = ContextFactory.Instance.Establish(SCardScope.System);
        var readers = context.GetReaders();
        if (readers.Length > 0)
        {
            var reader = context.ConnectReader(readers[0], SCardShareMode.Shared, SCardProtocol.Any);
            try
            {
                Process(reader);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                reader.Disconnect(SCardReaderDisposition.Leave);
            }
        }
    }

    private static void Process(ICardReader reader)
    {
        // IDm取得 FF:ベンダ独自 CA:ベンダ固有GET DATA
        var response = SendCommand(reader, CreateCommand(0xFF, 0xCA, 0x00, 0x00, 0x00));
        if (!response.IsSuccess())
        {
            Console.WriteLine($"IDm取得失敗: SW={response.SW1:X2}{response.SW2:X2}");
            return;
        }

        var idm = Convert.ToHexString(response.Data);
        Console.WriteLine($"IDm: {idm}");

        // 基本情報選択  FF:ベンダ独自 A4:ベンダ固有SELECT 0x008B
        response = SendCommand(reader, CreateCommand(0xFF, 0xA4, 0x00, 0x01, [0x8B, 0x00]));
        if (!response.IsSuccess())
        {
            Console.WriteLine($"基本情報選択失敗: SW={response.SW1:X2}{response.SW2:X2}");
            return;
        }

        response = SendCommand(reader, CreateCommand(0xFF, 0xB0, 0x00, 0x00, 0x00));
        if (!response.IsSuccess())
        {
            Console.WriteLine($"基本情報読取失敗: SW={response.SW1:X2}{response.SW2:X2}");
            return;
        }

        Console.WriteLine($"残高: \\{SuicaLogic.ExtractAccessBalance(response.Data)}");

        // 利用履歴 FF:ベンダ独自 A4:ベンダ固有SELECT 0x090F
        response = SendCommand(reader, CreateCommand(0xFF, 0xA4, 0x00, 0x01, [0x0F, 0x09]));
        if (!response.IsSuccess())
        {
            Console.WriteLine($"利用履歴選択失敗: SW={response.SW1:X2}{response.SW2:X2}");
            return;
        }

        for (var i = 0; i < 20; i++)
        {
            response = SendCommand(reader, CreateCommand(0xFF, 0xB0, 0x00, (byte)i, 0x00));
            if (!response.IsSuccess())
            {
                Console.WriteLine($"利用履歴読取失敗: SW={response.SW1:X2}{response.SW2:X2}");
                return;
            }

            Console.WriteLine($"{SuicaLogic.ExtractLogDateTime(response.Data):yyyy/MM/dd HH:mm:ss}  " +
                              $"{SuicaLogic.ConvertTerminalString(SuicaLogic.ExtractLogTerminal(response.Data))}-" +
                              $"{SuicaLogic.ConvertProcessString(SuicaLogic.ExtractLogProcess(response.Data))}  " +
                              $" \\{SuicaLogic.ExtractLogBalance(response.Data)}");
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

    private static Response SendCommand(ICardReader reader, byte[] command)
    {
        Console.WriteLine($"[送信] {BitConverter.ToString(command)}");

        var receiveBuffer = new byte[258]; // SW1+SW2を含む
        var bytesReceived = reader.Transmit(command, receiveBuffer);

        Console.WriteLine($"[受信] {BitConverter.ToString(receiveBuffer, 0, bytesReceived)}");

        return new Response(receiveBuffer, bytesReceived);
    }
}

internal sealed class Response
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

    public bool IsSuccess()
    {
        return SW1 == 0x90 && SW2 == 0x00;
    }
}

public static class SuicaLogic
{
    private static readonly Dictionary<byte, string> TerminalNames = new()
    {
        { 3, "精算機" },
        { 4, "携帯型端末" },
        { 5, "車載端末" },
        { 7, "券売機" },
        { 8, "券売機" },
        { 9, "入金機" },
        { 18, "券売機" },
        { 20, "券売機等" },
        { 21, "券売機等" },
        { 22, "改札機" },
        { 23, "簡易改札機" },
        { 24, "窓口端末" },
        { 25, "窓口端末" },
        { 26, "改札端末" },
        { 27, "携帯電話" },
        { 28, "乗継精算機" },
        { 29, "連絡改札機" },
        { 31, "簡易入金機" },
        { 199, "物販端末" },
        { 200, "自販機" }
    };

    private static readonly Dictionary<byte, string> ProcessNames = new()
    {
        { 1, "運賃支払" },
        { 2, "チャージ" },
        { 3, "磁気券購入" },
        { 4, "精算" },
        { 5, "入場精算" },
        { 6, "改札窓口処理" },
        { 7, "新規発行" },
        { 8, "窓口控除" },
        { 13, "バス(PiTaPa系)" },
        { 15, "バス(IruCa系)" },
        { 17, "再発行処理" },
        { 19, "新幹線利用" },
        { 20, "入場時AC" },
        { 21, "出場時AC" },
        { 31, "バスチャージ" },
        { 35, "バス路面電車企画券購入" },
        { 70, "物販" },
        { 72, "特典チャージ" },
        { 73, "レジ入金" },
        { 74, "物販取消" },
        { 75, "入場物販" }
    };

    private static readonly HashSet<byte> ProcessOfSales = [70, 72, 73, 74, 75];

    private static readonly HashSet<byte> ProcessOfBus = [13, 15, 31, 35];

    private static readonly Dictionary<int, string> RegionNames = new()
    {
        { 0, "首都圏" },
        { 1, "中部圏" },
        { 2, "近畿圏" },
        { 3, "その他" }
    };

    public static string ConvertTerminalString(byte type) =>
        TerminalNames.TryGetValue(type, out var value) ? value : type.ToString("X");

    public static string ConvertProcessString(byte process)
    {
        var processType = ConvertProcessType(process);
        var withCache = (process & 0b10000000) != 0;

        var name = ProcessNames.TryGetValue(processType, out var value) ? value : processType.ToString("X");

        return withCache ? name + " 現金併用" : name;
    }

    public static byte ConvertProcessType(byte process) =>
        (byte)(process & 0b01111111);

    public static bool IsProcessOfSales(byte process)
    {
        var processType = ConvertProcessType(process);
        return ProcessOfSales.Contains(processType);
    }

    public static bool IsProcessOfBus(byte process)
    {
        var processType = ConvertProcessType(process);
        return ProcessOfBus.Contains(processType);
    }

    public static string ConvertRegionString(int region) =>
        RegionNames.TryGetValue(region, out var value) ? value : region.ToString("X");

    private static DateTime ExtractDate(ReadOnlySpan<byte> bytes)
    {
        var year = 2000 + (bytes[0] >> 1);
        var month = BinaryPrimitives.ReadUInt16BigEndian(bytes[..2]) >> 5 & 0b1111;
        var day = bytes[1] & 0b11111;
        return new DateTime(year, month, day);
    }

    private static DateTime ExtractDateTime(ReadOnlySpan<byte> bytes)
    {
        var year = 2000 + (bytes[0] >> 1);
        var month = BinaryPrimitives.ReadUInt16BigEndian(bytes[..2]) >> 5 & 0b1111;
        var day = bytes[1] & 0b11111;
        var hour = bytes[2] >> 3;
        var minute = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(2, 2)) >> 5 & 0b111111;
        return new DateTime(year, month, day, hour, minute, 0);
    }

    public static int ExtractAccessBalance(ReadOnlySpan<byte> bytes) =>
        BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(11, 2));

    public static int ExtractAccessTransactionId(ReadOnlySpan<byte> bytes) =>
        BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(14, 2));

    public static bool IsValidLog(ReadOnlySpan<byte> bytes) =>
        bytes[1] != 0x00;

    public static byte ExtractLogTerminal(ReadOnlySpan<byte> bytes) =>
        bytes[0];

    public static byte ExtractLogProcess(ReadOnlySpan<byte> bytes) =>
        bytes[1];

    public static DateTime ExtractLogDateTime(ReadOnlySpan<byte> bytes) =>
        IsProcessOfSales(ExtractLogProcess(bytes)) ? ExtractDateTime(bytes[4..]) : ExtractDate(bytes[4..]);

    public static int ExtractLogBalance(ReadOnlySpan<byte> bytes) =>
        BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(10, 2));

    public static int ExtractLogTransactionId(ReadOnlySpan<byte> bytes) =>
        BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(13, 2));
}
