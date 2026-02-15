namespace MacDotNet.SystemInfo;

using System.Runtime.InteropServices;

using static MacDotNet.SystemInfo.NativeMethods;

public sealed record SmcSensorReading
{
    public required string Key { get; init; }

    public required string Description { get; init; }

    public required double Value { get; init; }

    public required string DataType { get; init; }
}

public sealed record SmcFanEntry
{
    public required int Index { get; init; }

    public required double ActualRpm { get; init; }

    public required double MinRpm { get; init; }

    public required double MaxRpm { get; init; }

    public required double TargetRpm { get; init; }
}

public static class SmcInfo
{
    public static SmcSensorReading[] GetTemperatureSensors() =>
        GetSmcReadingsByPrefix('T', GetTemperatureDescription);

    public static SmcSensorReading[] GetPowerReadings() =>
        GetSmcReadingsByPrefix('P', GetPowerDescription);

    public static SmcSensorReading[] GetVoltageReadings() =>
        GetSmcReadingsByPrefix('V', GetVoltageDescription);

    public static int? ReadSmcTemperature(string key)
    {
        var value = ReadSmcValue(key);
        if (value is not null && value != 128)
        {
            return (int)value;
        }

        return null;
    }

    public static double? ReadSmcValue(string key)
    {
        if (!TryOpenSmcConnection(out var service, out var conn))
        {
            return null;
        }

        try
        {
            return ReadSmcFloat(conn, key);
        }
        finally
        {
            IOServiceClose(conn);
            IOObjectRelease(service);
        }
    }

    public static double? GetTotalSystemPower() => ReadSmcValue("PSTR");

    public static unsafe SmcFanEntry[] GetFanInfo()
    {
        if (!TryOpenSmcConnection(out var service, out var conn))
        {
            return [];
        }

        try
        {
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

            var fans = new SmcFanEntry[fanCount];
            for (var i = 0; i < fanCount; i++)
            {
                fans[i] = new SmcFanEntry
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

    private static bool TryOpenSmcConnection(out uint service, out uint conn)
    {
        service = IOServiceGetMatchingService(0, IOServiceMatching("AppleSMC"));
        if (service == 0)
        {
            conn = 0;
            return false;
        }

        var ret = IOServiceOpen(service, task_self_trap(), 0, out conn);
        if (ret != KERN_SUCCESS)
        {
            IOObjectRelease(service);
            conn = 0;
            return false;
        }

        return true;
    }

    private static unsafe SmcSensorReading[] GetSmcReadingsByPrefix(char prefix, Func<string, string> descriptionProvider)
    {
        if (!TryOpenSmcConnection(out var service, out var conn))
        {
            return [];
        }

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

                var firstChar = (char)((key >> 24) & 0xFF);
                if (firstChar != prefix)
                {
                    continue;
                }

                if (!SmcReadKeyInfo(conn, key, out var dataSize, out var dataType))
                {
                    continue;
                }

                if (dataSize == 0)
                {
                    continue;
                }

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

        return (output.bytes[0] << 24) | (output.bytes[1] << 16) | (output.bytes[2] << 8) | output.bytes[3];
    }

    private static unsafe uint SmcReadIndex(uint conn, uint index)
    {
        SMCKeyData_t input = default;
        SMCKeyData_t output = default;
        input.data8 = SMC_CMD_READ_INDEX;
        input.data32 = index;
        return SmcCall(conn, &input, &output) == KERN_SUCCESS ? output.key : 0;
    }

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

    private static unsafe double? DecodeValue(byte* bytes, uint dataType, uint dataSize)
    {
        if (dataType == DATA_TYPE_FLT && dataSize == 4)
        {
            return *(float*)bytes;
        }

        if (dataType == DATA_TYPE_SP78 && dataSize == 2)
        {
            var raw = (short)((bytes[0] << 8) | bytes[1]);
            return raw / 256.0;
        }

        if (dataType == DATA_TYPE_FPE2 && dataSize == 2)
        {
            var raw = (ushort)((bytes[0] << 8) | bytes[1]);
            return raw / 4.0;
        }

        if (dataType == DATA_TYPE_IOFT && dataSize == 8)
        {
            var raw = *(int*)bytes;
            return raw / 65536.0;
        }

        if (dataType == DATA_TYPE_UI8 && dataSize == 1)
        {
            return bytes[0];
        }

        if (dataType == DATA_TYPE_UI16 && dataSize == 2)
        {
            return (ushort)((bytes[0] << 8) | bytes[1]);
        }

        if (dataType == DATA_TYPE_UI32 && dataSize == 4)
        {
            return (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
        }

        return null;
    }

    private static uint KeyToUInt32(string key) =>
        ((uint)key[0] << 24) | ((uint)key[1] << 16) | ((uint)key[2] << 8) | key[3];

    private static string UInt32ToKey(uint key) => new(
    [
        (char)((key >> 24) & 0xFF),
        (char)((key >> 16) & 0xFF),
        (char)((key >> 8) & 0xFF),
        (char)(key & 0xFF),
    ]);

    private static string GetTemperatureDescription(string key) => key switch
    {
        "TC0P" => "CPU Proximity",
        "TC0E" => "CPU E-Cluster",
        "TC0F" => "CPU P-Cluster",
        "TCXC" => "CPU PECI/Die",
        "TCDX" => "CPU Die",
        "TCMb" => "CPU Die (average)",
        "TCMz" => "CPU Die (max)",
        "TG0P" => "GPU Proximity",
        "TGDD" => "GPU Die - Diode",
        "TG0D" => "GPU Die",
        "TG0T" => "GPU Transistor",
        "TB0T" => "Battery",
        "TPCD" => "Platform Controller Hub Die",
        "TPMP" => "PMU",
        "TPSD" => "SSD",
        "TPSP" => "SoC Package",
        "TW0P" => "WiFi Proximity",
        "TH0P" => "Thunderbolt Proximity",
        "TM0P" => "Memory Proximity",
        "TM1P" => "Memory Proximity 1",
        "TMVR" => "Memory VRM",
        "TA0P" => "Ambient",
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
