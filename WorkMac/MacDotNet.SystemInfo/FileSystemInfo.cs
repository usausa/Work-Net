namespace MacDotNet.SystemInfo;

using System.Runtime.InteropServices;

using static MacDotNet.SystemInfo.NativeMethods;

public sealed record FileSystemEntry
{
    public required string MountPoint { get; init; }

    public required string TypeName { get; init; }

    public required string DeviceName { get; init; }

    public required uint BlockSize { get; init; }

    public required int IOSize { get; init; }

    public required ulong TotalBlocks { get; init; }

    public required ulong FreeBlocks { get; init; }

    public required ulong AvailableBlocks { get; init; }

    public ulong TotalSize => TotalBlocks * BlockSize;

    public ulong FreeSize => FreeBlocks * BlockSize;

    public ulong AvailableSize => AvailableBlocks * BlockSize;

    public double UsagePercent => TotalBlocks > 0 ? 100.0 * (TotalBlocks - AvailableBlocks) / TotalBlocks : 0;

    public required ulong TotalFiles { get; init; }

    public required ulong FreeFiles { get; init; }

    public required uint Flags { get; init; }

    public required uint SubType { get; init; }

    public required uint OwnerUid { get; init; }

    public bool IsReadOnly => (Flags & MNT_RDONLY) != 0;

    public bool IsLocal => (Flags & MNT_LOCAL) != 0;
}

public static class FileSystemInfo
{
    public static unsafe FileSystemEntry[] GetFileSystems()
    {
        var count = getfsstat(null, 0, MNT_NOWAIT);
        if (count <= 0)
        {
            return [];
        }

        var buffer = new statfs[count];
        fixed (statfs* ptr = buffer)
        {
            var bufsize = count * sizeof(statfs);
            var actual = getfsstat(ptr, bufsize, MNT_NOWAIT);
            if (actual <= 0)
            {
                return [];
            }

            var resultCount = Math.Min(actual, count);
            var result = new FileSystemEntry[resultCount];

            for (var i = 0; i < resultCount; i++)
            {
                result[i] = new FileSystemEntry
                {
                    MountPoint = Marshal.PtrToStringUTF8((IntPtr)ptr[i].f_mntonname) ?? string.Empty,
                    TypeName = Marshal.PtrToStringUTF8((IntPtr)ptr[i].f_fstypename) ?? string.Empty,
                    DeviceName = Marshal.PtrToStringUTF8((IntPtr)ptr[i].f_mntfromname) ?? string.Empty,
                    BlockSize = ptr[i].f_bsize,
                    IOSize = ptr[i].f_iosize,
                    TotalBlocks = ptr[i].f_blocks,
                    FreeBlocks = ptr[i].f_bfree,
                    AvailableBlocks = ptr[i].f_bavail,
                    TotalFiles = ptr[i].f_files,
                    FreeFiles = ptr[i].f_ffree,
                    Flags = ptr[i].f_flags,
                    SubType = ptr[i].f_fssubtype,
                    OwnerUid = ptr[i].f_owner,
                };
            }

            return result;
        }
    }
}
