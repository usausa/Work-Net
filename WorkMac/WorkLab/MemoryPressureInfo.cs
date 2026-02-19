namespace MacDotNet.SystemInfo.Lab;

using static NativeMethods;

/// <summary>
/// メモリプレッシャーレベル
/// </summary>
public enum MemoryPressureLevel
{
    Normal = 1,
    Warning = 2,
    Critical = 4,
}

/// <summary>
/// メモリプレッシャー情報
/// </summary>
public sealed class MemoryPressureInfo
{
    /// <summary>
    /// プレッシャーレベル (1=Normal, 2=Warning, 4=Critical)
    /// </summary>
    public int Level { get; private set; }

    /// <summary>
    /// プレッシャー状態
    /// </summary>
    public MemoryPressureLevel Pressure => Level switch
    {
        2 => MemoryPressureLevel.Warning,
        4 => MemoryPressureLevel.Critical,
        _ => MemoryPressureLevel.Normal,
    };

    /// <summary>
    /// プレッシャー状態の文字列表現
    /// </summary>
    public string PressureName => Pressure switch
    {
        MemoryPressureLevel.Warning => "Warning",
        MemoryPressureLevel.Critical => "Critical",
        _ => "Normal",
    };

    private MemoryPressureInfo()
    {
        Update();
    }

    public static MemoryPressureInfo Create() => new();

    public unsafe bool Update()
    {
        int level;
        var len = (IntPtr)sizeof(int);
        if (sysctlbyname("kern.memorystatus_vm_pressure_level", &level, ref len, IntPtr.Zero, 0) == 0)
        {
            Level = level;
            return true;
        }

        return false;
    }
}
