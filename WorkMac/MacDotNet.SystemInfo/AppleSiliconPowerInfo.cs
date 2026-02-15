namespace MacDotNet.SystemInfo;

using System.Runtime.InteropServices;

using static MacDotNet.SystemInfo.NativeMethods;

public sealed class AppleSiliconPowerInfo
{
    private nint channels;
    private nint subscription;

    private double prevCpuEnergy;
    private double prevGpuEnergy;
    private double prevAneEnergy;
    private double prevRamEnergy;

    public double CpuPower { get; private set; }

    public double GpuPower { get; private set; }

    public double AnePower { get; private set; }

    public double RamPower { get; private set; }

    public double TotalPower => CpuPower + GpuPower + AnePower + RamPower;

    public bool Supported { get; }

    private AppleSiliconPowerInfo()
    {
        Supported = InitializeReporting();
    }

    public static AppleSiliconPowerInfo Create() => new();

    private bool InitializeReporting()
    {
        if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
        {
            return false;
        }

        channels = GetEnergyModelChannels();
        if (channels == nint.Zero)
        {
            return false;
        }

        subscription = IOReportCreateSubscription(nint.Zero, channels, out _, 0, nint.Zero);
        return subscription != nint.Zero;
    }

    public bool Update()
    {
        if (!Supported || subscription == nint.Zero)
        {
            return false;
        }

        var samples = IOReportCreateSamples(subscription, channels, nint.Zero);
        if (samples == nint.Zero)
        {
            return false;
        }

        try
        {
            var channelsKey = CFStringCreateWithCString(nint.Zero, "IOReportChannels", kCFStringEncodingUTF8);
            var channelsArray = CFDictionaryGetValue(samples, channelsKey);
            CFRelease(channelsKey);

            if (channelsArray == nint.Zero || CFGetTypeID(channelsArray) != CFArrayGetTypeID())
            {
                return false;
            }

            double cpuEnergy = 0, gpuEnergy = 0, aneEnergy = 0, ramEnergy = 0;

            var count = CFArrayGetCount(channelsArray);
            for (var i = 0L; i < count; i++)
            {
                var item = CFArrayGetValueAtIndex(channelsArray, i);
                if (item == nint.Zero)
                {
                    continue;
                }

                var groupPtr = IOReportChannelGetGroup(item);
                var group = groupPtr != nint.Zero ? CfStringToManaged(groupPtr) : null;
                if (group != "Energy Model")
                {
                    continue;
                }

                var channelNamePtr = IOReportChannelGetChannelName(item);
                var channelName = channelNamePtr != nint.Zero ? CfStringToManaged(channelNamePtr) : null;
                if (string.IsNullOrEmpty(channelName))
                {
                    continue;
                }

                var unitPtr = IOReportChannelGetUnitLabel(item);
                var unit = unitPtr != nint.Zero ? CfStringToManaged(unitPtr) : null;

                var value = (double)IOReportSimpleGetIntegerValue(item, 0);
                var power = ConvertToPower(value, unit);

                if (channelName.EndsWith("CPU Energy", StringComparison.Ordinal))
                {
                    cpuEnergy = power;
                }
                else if (channelName.EndsWith("GPU Energy", StringComparison.Ordinal))
                {
                    gpuEnergy = power;
                }
                else if (channelName.StartsWith("ANE", StringComparison.Ordinal))
                {
                    aneEnergy = power;
                }
                else if (channelName.StartsWith("DRAM", StringComparison.Ordinal))
                {
                    ramEnergy = power;
                }
            }

            if (prevCpuEnergy > 0)
            {
                CpuPower = cpuEnergy - prevCpuEnergy;
                GpuPower = gpuEnergy - prevGpuEnergy;
                AnePower = aneEnergy - prevAneEnergy;
                RamPower = ramEnergy - prevRamEnergy;
            }

            prevCpuEnergy = cpuEnergy;
            prevGpuEnergy = gpuEnergy;
            prevAneEnergy = aneEnergy;
            prevRamEnergy = ramEnergy;

            return true;
        }
        finally
        {
            CFRelease(samples);
        }
    }

    private static nint GetEnergyModelChannels()
    {
        var channel = IOReportCopyChannelsInGroup("Energy Model", null, 0, 0, 0);
        if (channel == nint.Zero)
        {
            return nint.Zero;
        }

        return CFDictionaryCreateMutableCopy(nint.Zero, 1, channel);
    }

    private static double ConvertToPower(double value, string? unit)
    {
        return unit switch
        {
            "mJ" => value / 1000.0,
            "uJ" => value / 1_000_000.0,
            "nJ" => value / 1_000_000_000.0,
            _ => value / 1_000_000_000.0,
        };
    }
}
