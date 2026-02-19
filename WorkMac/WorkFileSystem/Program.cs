namespace WorkFileSystem;

using System.Runtime.InteropServices;

using static WorkFileSystem.NativeMethods;

internal static class Program
{
    public static void Main()
    {
        var entries = FileSystemInfoProvider.GetFileSystemEntries();
        if (entries.Length == 0)
        {
            Console.WriteLine("No file systems found.");
            return;
        }

        foreach (var fs in entries)
        {
            Console.WriteLine($"Mount Point:    {fs.MountPoint}");
            Console.WriteLine($"Device:         {fs.DeviceName}");
            Console.WriteLine($"Type:           {fs.TypeName}");
            Console.WriteLine($"Total Size:     {FormatBytes(fs.TotalSize)}");
            Console.WriteLine($"Free Size:      {FormatBytes(fs.FreeSize)}");
            Console.WriteLine($"Available:      {FormatBytes(fs.AvailableSize)}");
            Console.WriteLine($"Usage:          {fs.UsagePercent:F1}%");
            Console.WriteLine($"Block Size:     {fs.BlockSize}");
            Console.WriteLine($"I/O Size:       {fs.IOSize}");
            Console.WriteLine($"Total Inodes:   {fs.TotalFiles}");
            Console.WriteLine($"Free Inodes:    {fs.FreeFiles}");
            Console.WriteLine($"Read Only:      {fs.IsReadOnly}");
            Console.WriteLine($"Local:          {fs.IsLocal}");
            Console.WriteLine($"Flags:          0x{fs.Flags:X8}");
            Console.WriteLine();
        }
    }

    private static string FormatBytes(ulong bytes) => bytes switch
    {
        >= 1UL << 40 => $"{bytes / (double)(1UL << 40):F2} TiB",
        >= 1UL << 30 => $"{bytes / (double)(1UL << 30):F2} GiB",
        >= 1UL << 20 => $"{bytes / (double)(1UL << 20):F2} MiB",
        >= 1UL << 10 => $"{bytes / (double)(1UL << 10):F2} KiB",
        _ => $"{bytes} B"
    };
}

// ファイルシステム情報
internal sealed record FileSystemEntry
{
    // マウントポイント
    public required string MountPoint { get; init; }

    // ファイルシステム種別名 (apfs, hfs, nfs等)
    public required string TypeName { get; init; }

    // マウント元デバイス名
    public required string DeviceName { get; init; }

    // ブロックサイズ(バイト)
    public required uint BlockSize { get; init; }

    // 最適I/O転送サイズ(バイト)
    public required int IOSize { get; init; }

    // 合計ブロック数
    public required ulong TotalBlocks { get; init; }

    // 空きブロック数
    public required ulong FreeBlocks { get; init; }

    // 利用可能ブロック数 (非スーパーユーザー向け)
    public required ulong AvailableBlocks { get; init; }

    // 合計サイズ(バイト、TotalBlocks * BlockSizeから算出)
    public ulong TotalSize => TotalBlocks * BlockSize;

    // 空きサイズ(バイト、FreeBlocks * BlockSizeから算出)
    public ulong FreeSize => FreeBlocks * BlockSize;

    // 利用可能サイズ(バイト、AvailableBlocks * BlockSizeから算出)
    public ulong AvailableSize => AvailableBlocks * BlockSize;

    // 使用率(パーセント、算出値)
    public double UsagePercent => TotalBlocks > 0 ? 100.0 * (TotalBlocks - AvailableBlocks) / TotalBlocks : 0;

    // 合計ファイルノード数(inode)
    public required ulong TotalFiles { get; init; }

    // 空きファイルノード数(inode)
    public required ulong FreeFiles { get; init; }

    // マウントフラグ (MNT_* 定数のビットOR)
    public required uint Flags { get; init; }

    // ファイルシステムサブタイプ
    public required uint SubType { get; init; }

    // マウントしたユーザーのUID
    public required uint OwnerUid { get; init; }

    // 読み取り専用か (MNT_RDONLYフラグから判定)
    public bool IsReadOnly => (Flags & MNT_RDONLY) != 0;

    // ローカルファイルシステムか (MNT_LOCALフラグから判定)
    public bool IsLocal => (Flags & MNT_LOCAL) != 0;
}

// ファイルシステム情報取得
internal static class FileSystemInfoProvider
{
    public static unsafe FileSystemEntry[] GetFileSystemEntries()
    {
        // 1回目: マウント数を取得
        var count = getfsstat(null, 0, MNT_NOWAIT);
        if (count <= 0)
        {
            return [];
        }

        // バッファ確保
        var buffer = new statfs[count];
        fixed (statfs* ptr = buffer)
        {
            var bufsize = count * sizeof(statfs);

            // 2回目: 情報取得
            var actual = getfsstat(ptr, bufsize, MNT_NOWAIT);
            if (actual <= 0)
            {
                return [];
            }

            // バッファサイズ以下に制限 (2回目の呼び出しで件数が増えた場合の安全対策)
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
    // 定数 (sys/mount.h)
    public const int MFSTYPENAMELEN = 16;
    public const int MAXPATHLEN = 1024;

    // getfsstat モード (sys/mount.h)
    public const int MNT_WAIT = 1;
    public const int MNT_NOWAIT = 2;

    // マウントフラグ (sys/mount.h)
    public const uint MNT_RDONLY = 0x00000001;
    public const uint MNT_LOCAL = 0x00001000;

    //------------------------------------------------------------------------
    // Struct
    //------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    internal struct fsid_t
    {
        public int val0;
        public int val1;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct statfs
    {
        public uint f_bsize;            // fundamental file system block size
        public int f_iosize;            // optimal transfer block size
        public ulong f_blocks;          // total data blocks in file system
        public ulong f_bfree;           // free blocks in fs
        public ulong f_bavail;          // free blocks avail to non-superuser
        public ulong f_files;           // total file nodes in file system
        public ulong f_ffree;           // free file nodes in fs
        public fsid_t f_fsid;           // file system id
        public uint f_owner;            // user that mounted the filesystem
        public uint f_type;             // type of filesystem
        public uint f_flags;            // copy of mount exported flags
        public uint f_fssubtype;        // fs sub-type (flavor)
        public fixed byte f_fstypename[16];     // MFSTYPENAMELEN
        public fixed byte f_mntonname[1024];    // MAXPATHLEN
        public fixed byte f_mntfromname[1024];  // MAXPATHLEN
        public uint f_flags_ext;        // extended flags
        public fixed uint f_reserved[7];        // for future use
    }

    //------------------------------------------------------------------------
    // P/Invoke
    //------------------------------------------------------------------------

    [DllImport("libc")]
    public static extern unsafe int getfsstat(statfs* buf, int bufsize, int mode);
}
