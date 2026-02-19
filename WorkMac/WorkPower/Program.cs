namespace WorkPower;

using System.Runtime.InteropServices;

using static WorkPower.NativeMethods;

internal static class Program
{
    public static void Main()
    {
        // 温度センサー (全検出)
        var temps = SmcInfoProvider.GetTemperatureSensors();
        Console.WriteLine("=== Temperature Sensors ===");
        if (temps.Length > 0)
        {
            Console.WriteLine($"{"Key",-6} {"Value",8}  {"Type",-5}  Description");
            Console.WriteLine(new string('-', 70));
            foreach (var s in temps)
            {
                Console.WriteLine($"{s.Key,-6} {s.Value,7:F1} C  {s.DataType,-5}  {s.Description}");
            }
        }
        else
        {
            Console.WriteLine("No temperature sensors found.");
        }

        Console.WriteLine();

        // 要件で指定されたセンサーのサポート状況チェック
        Console.WriteLine("=== Required Sensor Support ===");
        var requiredSensors = new (string Key, string Description)[]
        {
            ("TC0P", "CPU Proximity"),
            ("TC0E", "CPU E-Cluster"),
            ("TC0F", "CPU P-Cluster"),
            ("TCXC", "CPU PECI/Die"),
            ("TG0P", "GPU Proximity"),
            ("TGDD", "GPU Die - Diode"),
            ("TG0D", "GPU Die"),
            ("TG0T", "GPU Transistor"),
            ("TB0T", "Battery"),
            ("TPCD", "Platform Controller Hub Die"),
            ("TW0P", "WiFi Proximity"),
            ("TH0P", "Thunderbolt Proximity"),
            ("Ts0P", "Palm Rest"),
            ("TM0P", "Memory Proximity"),
            ("TA0P", "Ambient"),
        };

        var tempDict = new Dictionary<string, SmcSensorReading>();
        foreach (var t in temps)
        {
            tempDict[t.Key] = t;
        }

        Console.WriteLine($"{"Key",-6} {"Status",-15} {"Value",8}  Description");
        Console.WriteLine(new string('-', 55));
        foreach (var (key, desc) in requiredSensors)
        {
            if (tempDict.TryGetValue(key, out var reading))
            {
                Console.WriteLine($"{key,-6} {"Available",-15} {reading.Value,7:F1} C  {desc}");
            }
            else
            {
                Console.WriteLine($"{key,-6} {"Not Available",-15} {"---",8}  {desc}");
            }
        }

        Console.WriteLine();

        // ファン情報
        var fans = SmcInfoProvider.GetFanInfo();
        Console.WriteLine("=== Fan Info ===");
        if (fans.Length > 0)
        {
            Console.WriteLine($"Fan count: {fans.Length}");
            foreach (var fan in fans)
            {
                Console.WriteLine($"  Fan {fan.Index}:");
                Console.WriteLine($"    Actual:  {fan.ActualRpm:F0} RPM");
                Console.WriteLine($"    Min:     {fan.MinRpm:F0} RPM");
                Console.WriteLine($"    Max:     {fan.MaxRpm:F0} RPM");
                Console.WriteLine($"    Target:  {fan.TargetRpm:F0} RPM");
            }
        }
        else
        {
            Console.WriteLine("No fans found.");
        }

        Console.WriteLine();

        // 電力消費
        var powers = SmcInfoProvider.GetPowerReadings();
        Console.WriteLine("=== Power Readings ===");
        if (powers.Length > 0)
        {
            Console.WriteLine($"{"Key",-6} {"Value",8}  Description");
            Console.WriteLine(new string('-', 55));
            foreach (var p in powers)
            {
                Console.WriteLine($"{p.Key,-6} {p.Value,7:F2} W  {p.Description}");
            }
        }
        else
        {
            Console.WriteLine("No power readings found.");
        }

        Console.WriteLine();

        // 電圧
        var voltages = SmcInfoProvider.GetVoltageReadings();
        Console.WriteLine("=== Voltage Readings ===");
        if (voltages.Length > 0)
        {
            Console.WriteLine($"{"Key",-6} {"Value",8}  Description");
            Console.WriteLine(new string('-', 55));
            foreach (var v in voltages)
            {
                Console.WriteLine($"{v.Key,-6} {v.Value,7:F3} V  {v.Description}");
            }
        }
        else
        {
            Console.WriteLine("No voltage readings found.");
        }
    }
}

// SMCセンサー読み取り値
internal sealed record SmcSensorReading
{
    // SMCキー名 (4文字、例: "TA0P")
    public required string Key { get; init; }

    // センサーの説明
    public required string Description { get; init; }

    // 読み取り値 (温度:℃、電力:W、電圧:V)
    public required double Value { get; init; }

    // SMCデータ型名 (例: "flt ", "sp78")
    public required string DataType { get; init; }
}

// ファン情報
internal sealed record SmcFanInfo
{
    // ファンインデックス (0始まり)
    public required int Index { get; init; }

    // 実測回転数 (RPM) — F{i}Ac
    public required double ActualRpm { get; init; }

    // 最小回転数 (RPM) — F{i}Mn
    public required double MinRpm { get; init; }

    // 最大回転数 (RPM) — F{i}Mx
    public required double MaxRpm { get; init; }

    // 目標回転数 (RPM) — F{i}Tg
    public required double TargetRpm { get; init; }
}

// SMC情報取得
internal static class SmcInfoProvider
{
    // 温度センサー一覧を取得 (T* キーを列挙)
    public static SmcSensorReading[] GetTemperatureSensors() =>
        GetSmcReadingsByPrefix('T', GetTemperatureDescription);

    // 電力読み取り一覧を取得 (P* キーを列挙)
    public static SmcSensorReading[] GetPowerReadings() =>
        GetSmcReadingsByPrefix('P', GetPowerDescription);

    // 電圧読み取り一覧を取得 (V* キーを列挙)
    public static SmcSensorReading[] GetVoltageReadings() =>
        GetSmcReadingsByPrefix('V', GetVoltageDescription);

    // ファン情報を取得 (FNum + F{i}Ac/Mn/Mx/Tg)
    public static unsafe SmcFanInfo[] GetFanInfo()
    {
        var (service, conn) = OpenSmcConnection();
        try
        {
            // ファン数を取得
            var fanCountVal = ReadSmcFloat(conn, "FNum");
            if (fanCountVal is null)
            {
                return [];
            }

            var fanCount = (int)fanCountVal.Value;
            if (fanCount <= 0)
            {
                return [];
            }

            var fans = new SmcFanInfo[fanCount];
            for (var i = 0; i < fanCount; i++)
            {
                fans[i] = new SmcFanInfo
                {
                    Index = i,
                    ActualRpm = ReadSmcFloat(conn, $"F{i}Ac") ?? 0,
                    MinRpm = ReadSmcFloat(conn, $"F{i}Mn") ?? 0,
                    MaxRpm = ReadSmcFloat(conn, $"F{i}Mx") ?? 0,
                    TargetRpm = ReadSmcFloat(conn, $"F{i}Tg") ?? 0,
                };
            }

            return fans;
        }
        finally
        {
            IOServiceClose(conn);
            IOObjectRelease(service);
        }
    }

    // SMC接続を開く
    private static (uint Service, uint Connection) OpenSmcConnection()
    {
        var service = IOServiceGetMatchingService(0, IOServiceMatching("AppleSMC"));
        if (service == 0)
        {
            throw new InvalidOperationException("AppleSMC service not found.");
        }

        var ret = IOServiceOpen(service, task_self_trap(), 0, out var conn);
        if (ret != KERN_SUCCESS)
        {
            IOObjectRelease(service);
            throw new InvalidOperationException($"IOServiceOpen failed: {ret}");
        }

        return (service, conn);
    }

    // プレフィックスで始まるSMCキーを全列挙して読み取り値を返す
    private static unsafe SmcSensorReading[] GetSmcReadingsByPrefix(
        char prefix,
        Func<string, string> descriptionProvider)
    {
        var (service, conn) = OpenSmcConnection();
        try
        {
            var keyCount = GetKeyCount(conn);
            if (keyCount <= 0)
            {
                return [];
            }

            var results = new List<SmcSensorReading>();

            for (uint i = 0; i < (uint)keyCount; i++)
            {
                var key = SmcReadIndex(conn, i);
                if (key == 0)
                {
                    continue;
                }

                // プレフィックスチェック (キーの最上位バイト)
                var firstChar = (char)((key >> 24) & 0xFF);
                if (firstChar != prefix)
                {
                    continue;
                }

                // キー情報を取得
                if (!SmcReadKeyInfo(conn, key, out var dataSize, out var dataType))
                {
                    continue;
                }

                if (dataSize == 0)
                {
                    continue;
                }

                // 値を読み取り
                SMCKeyData_t input = default;
                SMCKeyData_t output = default;
                input.key = key;
                input.keyInfo.dataSize = dataSize;
                input.data8 = SMC_CMD_READ_BYTES;

                if (SmcCall(conn, &input, &output) != KERN_SUCCESS)
                {
                    continue;
                }

                var value = DecodeValue(output.bytes, dataType, dataSize);
                if (value is null)
                {
                    continue;
                }

                var keyStr = UInt32ToKey(key);
                var typeStr = UInt32ToKey(dataType);

                results.Add(new SmcSensorReading
                {
                    Key = keyStr,
                    Description = descriptionProvider(keyStr),
                    Value = value.Value,
                    DataType = typeStr,
                });
            }

            return [.. results];
        }
        finally
        {
            IOServiceClose(conn);
            IOObjectRelease(service);
        }
    }

    // SMCキーの総数を取得 (#KEYキーを読む)
    private static unsafe int GetKeyCount(uint conn)
    {
        SMCKeyData_t input = default;
        SMCKeyData_t output = default;

        var key = KeyToUInt32("#KEY");
        if (!SmcReadKeyInfo(conn, key, out var dataSize, out _))
        {
            return 0;
        }

        input.key = key;
        input.keyInfo.dataSize = dataSize;
        input.data8 = SMC_CMD_READ_BYTES;

        if (SmcCall(conn, &input, &output) != KERN_SUCCESS)
        {
            return 0;
        }

        // ビッグエンディアンuint32
        return (output.bytes[0] << 24) | (output.bytes[1] << 16) | (output.bytes[2] << 8) | output.bytes[3];
    }

    // インデックスからSMCキーを取得
    private static unsafe uint SmcReadIndex(uint conn, uint index)
    {
        SMCKeyData_t input = default;
        SMCKeyData_t output = default;
        input.data8 = SMC_CMD_READ_INDEX;
        input.data32 = index;
        return SmcCall(conn, &input, &output) == KERN_SUCCESS ? output.key : 0;
    }

    // SMCキーのデータサイズと型を取得
    private static unsafe bool SmcReadKeyInfo(uint conn, uint key, out uint dataSize, out uint dataType)
    {
        SMCKeyData_t input = default;
        SMCKeyData_t output = default;
        input.key = key;
        input.data8 = SMC_CMD_READ_KEYINFO;

        if (SmcCall(conn, &input, &output) == KERN_SUCCESS)
        {
            dataSize = output.keyInfo.dataSize;
            dataType = output.keyInfo.dataType;
            return true;
        }

        dataSize = 0;
        dataType = 0;
        return false;
    }

    // 指定キーのfloat値を読み取る (ファン情報等で使用)
    private static unsafe double? ReadSmcFloat(uint conn, string keyStr)
    {
        var key = KeyToUInt32(keyStr);
        if (!SmcReadKeyInfo(conn, key, out var dataSize, out var dataType))
        {
            return null;
        }

        if (dataSize == 0)
        {
            return null;
        }

        SMCKeyData_t input = default;
        SMCKeyData_t output = default;
        input.key = key;
        input.keyInfo.dataSize = dataSize;
        input.data8 = SMC_CMD_READ_BYTES;

        if (SmcCall(conn, &input, &output) != KERN_SUCCESS)
        {
            return null;
        }

        return DecodeValue(output.bytes, dataType, dataSize);
    }

    // IOConnectCallStructMethodのラッパー
    private static unsafe int SmcCall(uint conn, SMCKeyData_t* input, SMCKeyData_t* output)
    {
        var outputSize = (nuint)sizeof(SMCKeyData_t);
        return IOConnectCallStructMethod(
            conn,
            KERNEL_INDEX_SMC,
            input,
            (nuint)sizeof(SMCKeyData_t),
            output,
            &outputSize);
    }

    // SMCバイト列から数値を復号
    private static unsafe double? DecodeValue(byte* bytes, uint dataType, uint dataSize)
    {
        // flt : リトルエンディアンIEEE 754 float (Apple Silicon/Intel共通)
        if (dataType == DATA_TYPE_FLT && dataSize == 4)
        {
            return *(float*)bytes;
        }

        // sp78: ビッグエンディアン符号付き固定小数点 7.8
        if (dataType == DATA_TYPE_SP78 && dataSize == 2)
        {
            var raw = (short)((bytes[0] << 8) | bytes[1]);
            return raw / 256.0;
        }

        // fpe2: ビッグエンディアン符号なし固定小数点 14.2
        if (dataType == DATA_TYPE_FPE2 && dataSize == 2)
        {
            var raw = (ushort)((bytes[0] << 8) | bytes[1]);
            return raw / 4.0;
        }

        // ioft: リトルエンディアン固定小数点 16.16 (8バイトのうち下位4バイト使用)
        if (dataType == DATA_TYPE_IOFT && dataSize == 8)
        {
            var raw = *(int*)bytes;
            return raw / 65536.0;
        }

        // ui8 : 符号なし8ビット整数
        if (dataType == DATA_TYPE_UI8 && dataSize == 1)
        {
            return bytes[0];
        }

        // ui16: ビッグエンディアン符号なし16ビット整数
        if (dataType == DATA_TYPE_UI16 && dataSize == 2)
        {
            return (ushort)((bytes[0] << 8) | bytes[1]);
        }

        // ui32: ビッグエンディアン符号なし32ビット整数
        if (dataType == DATA_TYPE_UI32 && dataSize == 4)
        {
            return (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
        }

        return null;
    }

    // SMCキー文字列をuint32に変換 (ビッグエンディアン)
    private static uint KeyToUInt32(string key) =>
        ((uint)key[0] << 24) | ((uint)key[1] << 16) | ((uint)key[2] << 8) | key[3];

    // uint32をSMCキー文字列に変換
    private static string UInt32ToKey(uint key) => new(
    [
        (char)((key >> 24) & 0xFF),
        (char)((key >> 16) & 0xFF),
        (char)((key >> 8) & 0xFF),
        (char)(key & 0xFF),
    ]);

    // 温度センサーの説明を取得
    private static string GetTemperatureDescription(string key) => key switch
    {
        // CPU (Intel向け)
        "TC0P" => "CPU Proximity",
        "TC0E" => "CPU E-Cluster",
        "TC0F" => "CPU P-Cluster",
        "TCXC" => "CPU PECI/Die",
        // CPU (Apple Silicon)
        "TCDX" => "CPU Die",
        "TCMb" => "CPU Die (average)",
        "TCMz" => "CPU Die (max)",
        // GPU (Intel向け)
        "TG0P" => "GPU Proximity",
        "TGDD" => "GPU Die - Diode",
        "TG0D" => "GPU Die",
        "TG0T" => "GPU Transistor",
        // バッテリー
        "TB0T" => "Battery",
        // プラットフォーム
        "TPCD" => "Platform Controller Hub Die",
        "TPMP" => "PMU",
        "TPSD" => "SSD",
        "TPSP" => "SoC Package",
        // ワイヤレス
        "TW0P" => "WiFi Proximity",
        // Thunderbolt
        "TH0P" => "Thunderbolt Proximity",
        // メモリ
        "TM0P" => "Memory Proximity",
        "TM1P" => "Memory Proximity 1",
        "TMVR" => "Memory VRM",
        // アンビエント
        "TA0P" => "Ambient",
        // サーフェス
        "Ts0P" => "Palm Rest",
        "Ts1P" => "Palm Rest Right",
        _ => GetTemperatureDescriptionByPrefix(key),
    };

    private static string GetTemperatureDescriptionByPrefix(string key)
    {
        if (key.Length < 2)
        {
            return key;
        }

        return key[..2] switch
        {
            "TP" => $"Die Sensor ({key})",
            "TR" => $"DRAM ({key})",
            "TS" or "Ts" => $"Surface ({key})",
            "TV" => $"Video ({key})",
            "TH" => $"Thunderbolt ({key})",
            "TI" => $"I/O ({key})",
            "Ta" => $"Ambient/SoC ({key})",
            "Te" => $"E-Cluster ({key})",
            "Tf" => $"Thermal Filter ({key})",
            _ => key,
        };
    }

    // 電力の説明を取得
    private static string GetPowerDescription(string key) => key switch
    {
        "PDTR" => "Total System",
        "PHPC" => "High Power Controller",
        "PHPM" => "High Power Module",
        "PHPS" => "High Power System",
        "PHPB" => "High Power Budget",
        "PPMR" => "Memory Rail",
        "PPSR" => "System Rail",
        _ => GetPowerDescriptionByPrefix(key),
    };

    private static string GetPowerDescriptionByPrefix(string key)
    {
        if (key.Length < 2)
        {
            return key;
        }

        return key[..2] switch
        {
            "PC" => $"CPU Power ({key})",
            "PH" => $"High Power ({key})",
            "PP" => $"Power Rail ({key})",
            "PR" => $"Power Rail ({key})",
            "PA" => $"Amplifier ({key})",
            "PF" => $"Fan Power ({key})",
            "PI" => $"I/O Power ({key})",
            "PM" => $"Memory Power ({key})",
            "PO" => $"Other Power ({key})",
            _ => key,
        };
    }

    // 電圧の説明を取得
    private static string GetVoltageDescription(string key) => key switch
    {
        "V5SC" => "5V Supply",
        "VD0R" => "Main DC Input",
        "VDMA" => "DMA",
        "VDMM" => "Memory Controller",
        "VMVC" => "Main Voltage Controller",
        "VRTC" => "RTC Battery",
        _ => GetVoltageDescriptionByPrefix(key),
    };

    private static string GetVoltageDescriptionByPrefix(string key)
    {
        if (key.Length < 2)
        {
            return key;
        }

        return key[..2] switch
        {
            "VC" => $"CPU Core ({key})",
            "VD" => $"DC Rail ({key})",
            "VP" => $"Power Rail ({key})",
            "VR" => $"Regulator ({key})",
            _ => key,
        };
    }
}

// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006
#pragma warning disable CA2101
#pragma warning disable CA5392
#pragma warning disable CS8981
internal static class NativeMethods
{
    // SMC selector (IOConnectCallStructMethodのselector引数)
    public const uint KERNEL_INDEX_SMC = 2;

    // SMCコマンド
    public const byte SMC_CMD_READ_BYTES = 5;
    public const byte SMC_CMD_READ_KEYINFO = 9;
    public const byte SMC_CMD_READ_INDEX = 8;

    // 成功コード (mach/kern_return.h)
    public const int KERN_SUCCESS = 0;

    // SMCデータ型定数 (4文字をビッグエンディアンuint32にエンコード)
    public const uint DATA_TYPE_FLT = 0x666C7420;   // "flt "
    public const uint DATA_TYPE_SP78 = 0x73703738;   // "sp78"
    public const uint DATA_TYPE_FPE2 = 0x66706532;   // "fpe2"
    public const uint DATA_TYPE_IOFT = 0x696F6674;   // "ioft"
    public const uint DATA_TYPE_UI8 = 0x75693820;    // "ui8 "
    public const uint DATA_TYPE_UI16 = 0x75693136;   // "ui16"
    public const uint DATA_TYPE_UI32 = 0x75693332;   // "ui32"

    //------------------------------------------------------------------------
    // SMC Struct
    //------------------------------------------------------------------------

    // SMCバージョン情報
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SMCKeyData_vers_t
    {
        public byte major;
        public byte minor;
        public byte build;
        public byte reserved;
        public ushort release;
    }

    // SMC電力制限データ
    [StructLayout(LayoutKind.Sequential)]
    internal struct SMCKeyData_pLimitData_t
    {
        public ushort version;
        public ushort length;
        public uint cpuPLimit;
        public uint gpuPLimit;
        public uint memPLimit;
    }

    // SMCキー情報
    [StructLayout(LayoutKind.Sequential)]
    internal struct SMCKeyData_keyInfo_t
    {
        public uint dataSize;
        public uint dataType;
        public byte dataAttributes;
    }

    // SMCキーデータ (IOConnectCallStructMethod用の入出力構造体、80バイト)
    [StructLayout(LayoutKind.Explicit, Size = 80)]
    internal unsafe struct SMCKeyData_t
    {
        [FieldOffset(0)]
        public uint key;

        [FieldOffset(4)]
        public SMCKeyData_vers_t vers;

        [FieldOffset(12)]
        public SMCKeyData_pLimitData_t pLimitData;

        [FieldOffset(28)]
        public SMCKeyData_keyInfo_t keyInfo;

        [FieldOffset(40)]
        public byte result;

        [FieldOffset(41)]
        public byte status;

        [FieldOffset(42)]
        public byte data8;

        [FieldOffset(44)]
        public uint data32;

        [FieldOffset(48)]
        public fixed byte bytes[32];
    }

    //------------------------------------------------------------------------
    // IOKit
    //------------------------------------------------------------------------

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static extern uint IOServiceGetMatchingService(uint mainPort, IntPtr matching);

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static extern int IOServiceOpen(uint service, uint owningTask, uint type, out uint connect);

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static extern int IOServiceClose(uint connect);

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static extern int IOObjectRelease(uint @object);

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static extern IntPtr IOServiceMatching(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport("/System/Library/Frameworks/IOKit.framework/IOKit")]
    public static extern unsafe int IOConnectCallStructMethod(
        uint connection,
        uint selector,
        void* inputStruct,
        nuint inputStructCnt,
        void* outputStruct,
        nuint* outputStructCnt);

    //------------------------------------------------------------------------
    // Mach
    //------------------------------------------------------------------------

    [DllImport("libSystem.dylib")]
    public static extern uint task_self_trap();
}
