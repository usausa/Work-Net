namespace WorkNetwork;

using System.Runtime.InteropServices;

using static WorkNetwork.NativeMethods;

internal static class Program
{
    public static void Main()
    {
        var interfaces = NetworkInfoProvider.GetNetworkInterfaces();
        if (interfaces.Length == 0)
        {
            Console.WriteLine("No network interfaces found.");
            return;
        }

        for (var i = 0; i < interfaces.Length; i++)
        {
            var iface = interfaces[i];
            if (i > 0)
            {
                Console.WriteLine();
            }

            Console.WriteLine($"=== {iface.Name} ({iface.InterfaceTypeName}) ===");
            Console.WriteLine($"  State:           {iface.State}");
            Console.WriteLine($"  Flags:           0x{iface.Flags:X8} ({FormatFlags(iface.Flags)})");
            if (iface.MacAddress is not null)
            {
                Console.WriteLine($"  MAC Address:     {iface.MacAddress}");
            }

            Console.WriteLine($"  MTU:             {iface.Mtu}");
            Console.WriteLine($"  Link Speed:      {FormatSpeed(iface.LinkSpeed)}");

            foreach (var addr in iface.IPv4Addresses)
            {
                Console.WriteLine($"  IPv4:            {addr.Address}/{addr.PrefixLength}");
            }

            foreach (var addr in iface.IPv6Addresses)
            {
                Console.WriteLine($"  IPv6:            {addr.Address}/{addr.PrefixLength}");
            }

            Console.WriteLine($"  Rx Bytes:        {iface.RxBytes:N0}");
            Console.WriteLine($"  Rx Packets:      {iface.RxPackets:N0}");
            Console.WriteLine($"  Rx Errors:       {iface.RxErrors:N0}");
            Console.WriteLine($"  Rx Drops:        {iface.RxDrops:N0}");
            Console.WriteLine($"  Rx Multicast:    {iface.RxMulticast:N0}");
            Console.WriteLine($"  Tx Bytes:        {iface.TxBytes:N0}");
            Console.WriteLine($"  Tx Packets:      {iface.TxPackets:N0}");
            Console.WriteLine($"  Tx Errors:       {iface.TxErrors:N0}");
            Console.WriteLine($"  Tx Multicast:    {iface.TxMulticast:N0}");
            Console.WriteLine($"  Collisions:      {iface.Collisions:N0}");
            Console.WriteLine($"  No Proto:        {iface.NoProto:N0}");
        }
    }

    private static string FormatFlags(uint flags)
    {
        var parts = new List<string>();
        if ((flags & IFF_UP) != 0)
        {
            parts.Add("UP");
        }

        if ((flags & IFF_BROADCAST) != 0)
        {
            parts.Add("BROADCAST");
        }

        if ((flags & IFF_LOOPBACK) != 0)
        {
            parts.Add("LOOPBACK");
        }

        if ((flags & IFF_POINTOPOINT) != 0)
        {
            parts.Add("POINTOPOINT");
        }

        if ((flags & IFF_RUNNING) != 0)
        {
            parts.Add("RUNNING");
        }

        if ((flags & IFF_MULTICAST) != 0)
        {
            parts.Add("MULTICAST");
        }

        return string.Join(", ", parts);
    }

    private static string FormatSpeed(uint baudrate) => baudrate switch
    {
        0 => "N/A",
        < 1_000 => $"{baudrate} bps",
        < 1_000_000 => $"{baudrate / 1_000.0:F1} Kbps",
        < 1_000_000_000 => $"{baudrate / 1_000_000.0:F1} Mbps",
        _ => $"{baudrate / 1_000_000_000.0:F1} Gbps",
    };
}

// インターフェースの動作状態
internal enum InterfaceState
{
    // ダウン (IFF_UPが未設定、管理的に無効)
    Down,

    // キャリアなし (IFF_UP設定済みだがIFF_RUNNING未設定、物理的に未接続等)
    NoCarrier,

    // アップ (IFF_UPおよびIFF_RUNNINGが設定済み、正常稼働中)
    Up,
}

// アドレス情報 (CIDR表記用のプレフィックス長付き)
internal sealed record InterfaceAddress
{
    // アドレス文字列 (IPv4: "192.168.1.1"、IPv6: "fe80::1%4" 等)
    public required string Address { get; init; }

    // プレフィックス長 (サブネットマスクのビット数)
    public required int PrefixLength { get; init; }
}

// ネットワークインターフェース情報
internal sealed record NetworkInterfaceInfo
{
    // インターフェース名 (例: en0, lo0, utun0)
    public required string Name { get; init; }

    // インターフェースの動作状態 (Up/Down/NoCarrier)
    public required InterfaceState State { get; init; }

    // インターフェースフラグ (IFF_*の生値、ifconfig等で表示されるフラグに対応)
    public required uint Flags { get; init; }

    // ループバックインターフェースか (自ホスト向け通信用)
    public bool IsLoopback => (Flags & IFF_LOOPBACK) != 0;

    // ブロードキャスト送信をサポートするか
    public bool SupportsBroadcast => (Flags & IFF_BROADCAST) != 0;

    // マルチキャスト送信をサポートするか
    public bool SupportsMulticast => (Flags & IFF_MULTICAST) != 0;

    // ポイントツーポイント接続か (VPNトンネル等)
    public bool IsPointToPoint => (Flags & IFF_POINTOPOINT) != 0;

    // MACアドレス (リンクレイヤーアドレス、xx:xx:xx:xx:xx:xx形式。取得不可の場合null)
    public string? MacAddress { get; init; }

    // IPv4アドレス一覧 (CIDR表記用のプレフィックス長付き)
    public required IReadOnlyList<InterfaceAddress> IPv4Addresses { get; init; }

    // IPv6アドレス一覧 (CIDR表記用のプレフィックス長付き、リンクローカルはスコープID付き)
    public required IReadOnlyList<InterfaceAddress> IPv6Addresses { get; init; }

    // インターフェース種別コード (IFT_*の値。Ethernet=0x06, Loopback=0x18, Wi-Fi=0x47等)
    public byte InterfaceType { get; init; }

    // インターフェース種別の表示名
    public string InterfaceTypeName => InterfaceType switch
    {
        IFT_ETHER => "Ethernet",
        IFT_LOOP => "Loopback",
        IFT_IEEE80211 => "Wi-Fi",
        IFT_GIF => "GIF Tunnel",
        IFT_STF => "6to4 Tunnel",
        IFT_CELLULAR => "Cellular",
        IFT_BRIDGE => "Bridge",
        _ => $"Other(0x{InterfaceType:X2})",
    };

    // MTU: 最大転送単位 (1パケットで送信可能な最大バイト数)
    public uint Mtu { get; init; }

    // リンク速度 (bps単位、0は不明)
    public uint LinkSpeed { get; init; }

    // 受信バイト数 (インターフェースで受信した総バイト数)
    public uint RxBytes { get; init; }

    // 受信パケット数 (インターフェースで受信した総パケット数)
    public uint RxPackets { get; init; }

    // 受信エラー数 (CRCエラー等、受信時に検出されたエラーの数)
    public uint RxErrors { get; init; }

    // 受信ドロップ数 (バッファ不足等で破棄された受信パケットの数)
    public uint RxDrops { get; init; }

    // 受信マルチキャストパケット数 (マルチキャストグループ宛に受信したパケット数)
    public uint RxMulticast { get; init; }

    // 送信バイト数 (インターフェースから送信した総バイト数)
    public uint TxBytes { get; init; }

    // 送信パケット数 (インターフェースから送信した総パケット数)
    public uint TxPackets { get; init; }

    // 送信エラー数 (送信失敗等、送信時に検出されたエラーの数)
    public uint TxErrors { get; init; }

    // 送信マルチキャストパケット数 (マルチキャストグループ宛に送信したパケット数)
    public uint TxMulticast { get; init; }

    // コリジョン数 (CSMA/CDネットワークでの衝突回数、現代のスイッチ環境では通常0)
    public uint Collisions { get; init; }

    // 未対応プロトコルパケット数 (このインターフェースでサポートされていないプロトコルのパケット数)
    public uint NoProto { get; init; }
}

// ネットワークインターフェース情報取得
internal static class NetworkInfoProvider
{
    public static unsafe NetworkInterfaceInfo[] GetNetworkInterfaces()
    {
        if (getifaddrs(out var ifap) != 0)
        {
            return [];
        }

        try
        {
            var map = new Dictionary<string, InterfaceBuilder>(StringComparer.Ordinal);

            for (var ptr = ifap; ptr != nint.Zero;)
            {
                var ifa = Marshal.PtrToStructure<ifaddrs>(ptr);
                var name = Marshal.PtrToStringUTF8(ifa.ifa_name);

                if (name is not null)
                {
                    if (!map.TryGetValue(name, out var builder))
                    {
                        builder = new InterfaceBuilder { Name = name, Flags = ifa.ifa_flags };
                        map[name] = builder;
                    }

                    if (ifa.ifa_addr != nint.Zero)
                    {
                        var family = ((sockaddr*)ifa.ifa_addr)->sa_family;
                        switch (family)
                        {
                            case AF_LINK:
                                ProcessLinkAddress(ifa, builder);
                                break;
                            case AF_INET:
                                ProcessIPv4Address(ifa, builder);
                                break;
                            case AF_INET6:
                                ProcessIPv6Address(ifa, builder);
                                break;
                        }
                    }
                }

                ptr = ifa.ifa_next;
            }

            return [.. map.Values.Select(b => b.Build()).OrderBy(static i => i.Name, StringComparer.Ordinal)];
        }
        finally
        {
            freeifaddrs(ifap);
        }
    }

    private static unsafe void ProcessLinkAddress(ifaddrs ifa, InterfaceBuilder builder)
    {
        // AF_LINKの場合、ifa_dataはif_data構造体を指す (インターフェース統計情報)
        if (ifa.ifa_data != nint.Zero)
        {
            var data = (if_data*)ifa.ifa_data;
            builder.InterfaceType = data->ifi_type;
            builder.Mtu = data->ifi_mtu;
            builder.LinkSpeed = data->ifi_baudrate;
            builder.RxBytes = data->ifi_ibytes;
            builder.RxPackets = data->ifi_ipackets;
            builder.RxErrors = data->ifi_ierrors;
            builder.RxDrops = data->ifi_iqdrops;
            builder.RxMulticast = data->ifi_imcasts;
            builder.TxBytes = data->ifi_obytes;
            builder.TxPackets = data->ifi_opackets;
            builder.TxErrors = data->ifi_oerrors;
            builder.TxMulticast = data->ifi_omcasts;
            builder.Collisions = data->ifi_collisions;
            builder.NoProto = data->ifi_noproto;
        }

        // sockaddr_dlからMACアドレスを取得
        // sdl_dataはオフセット8から始まり、先頭sdl_nlenバイトがインターフェース名、
        // その後sdl_alenバイトがリンクレイヤーアドレス(MACアドレス)
        var sdl = (sockaddr_dl*)ifa.ifa_addr;
        if (sdl->sdl_alen > 0 && sdl->sdl_alen <= 8)
        {
            var macPtr = (byte*)sdl + sockaddr_dl.DataOffset + sdl->sdl_nlen;
            Span<char> buf = stackalloc char[(sdl->sdl_alen * 3) - 1];
            var pos = 0;
            for (var i = 0; i < sdl->sdl_alen; i++)
            {
                if (i > 0)
                {
                    buf[pos++] = ':';
                }

                var b = macPtr[i];
                var hi = b >> 4;
                var lo = b & 0x0F;
                buf[pos++] = ToHexChar(hi);
                buf[pos++] = ToHexChar(lo);
            }

            builder.MacAddress = new string(buf[..pos]);
        }
    }

    private static char ToHexChar(int value) => (char)(value < 10 ? '0' + value : 'a' + value - 10);

    private static unsafe void ProcessIPv4Address(ifaddrs ifa, InterfaceBuilder builder)
    {
        // sockaddr_in: [len(1)][family(1)][port(2)][sin_addr(4)][zero(8)]
        var sinAddr = (byte*)ifa.ifa_addr + sockaddr_in.AddrOffset;
        var buf = stackalloc byte[(int)INET_ADDRSTRLEN];

        if (inet_ntop(AF_INET, sinAddr, buf, INET_ADDRSTRLEN) == nint.Zero)
        {
            return;
        }

        var address = Marshal.PtrToStringUTF8((nint)buf);
        if (address is null)
        {
            return;
        }

        var prefix = 0;
        if (ifa.ifa_netmask != nint.Zero)
        {
            var maskAddr = (byte*)ifa.ifa_netmask + sockaddr_in.AddrOffset;
            prefix = CountPrefixBits(maskAddr, 4);
        }

        builder.IPv4Addresses.Add(new InterfaceAddress { Address = address, PrefixLength = prefix });
    }

    private static unsafe void ProcessIPv6Address(ifaddrs ifa, InterfaceBuilder builder)
    {
        // sockaddr_in6: [len(1)][family(1)][port(2)][flowinfo(4)][sin6_addr(16)][scope_id(4)]
        var sin6Addr = (byte*)ifa.ifa_addr + sockaddr_in6.AddrOffset;
        var buf = stackalloc byte[(int)INET6_ADDRSTRLEN];

        if (inet_ntop(AF_INET6, sin6Addr, buf, INET6_ADDRSTRLEN) == nint.Zero)
        {
            return;
        }

        var address = Marshal.PtrToStringUTF8((nint)buf);
        if (address is null)
        {
            return;
        }

        // リンクローカルアドレス等でスコープIDが設定されている場合、%scope_idを付加
        var scopeId = *(uint*)((byte*)ifa.ifa_addr + sockaddr_in6.ScopeIdOffset);
        if (scopeId != 0)
        {
            address = $"{address}%{scopeId}";
        }

        var prefix = 0;
        if (ifa.ifa_netmask != nint.Zero)
        {
            var maskAddr = (byte*)ifa.ifa_netmask + sockaddr_in6.AddrOffset;
            prefix = CountPrefixBits(maskAddr, 16);
        }

        builder.IPv6Addresses.Add(new InterfaceAddress { Address = address, PrefixLength = prefix });
    }

    // ネットマスクのバイト列からプレフィックス長(連続する1のビット数)を計算
    private static unsafe int CountPrefixBits(byte* mask, int length)
    {
        var bits = 0;
        for (var i = 0; i < length; i++)
        {
            if (mask[i] == 0xFF)
            {
                bits += 8;
                continue;
            }

            var b = mask[i];
            while ((b & 0x80) != 0)
            {
                bits++;
                b <<= 1;
            }

            break;
        }

        return bits;
    }

    // インターフェース情報の収集用ビルダー (getifaddrsの複数エントリを1つに集約)
    private sealed class InterfaceBuilder
    {
        public required string Name { get; init; }

        public uint Flags { get; init; }

        public byte InterfaceType { get; set; }

        public string? MacAddress { get; set; }

        public uint Mtu { get; set; }

        public uint LinkSpeed { get; set; }

        public uint RxBytes { get; set; }

        public uint RxPackets { get; set; }

        public uint RxErrors { get; set; }

        public uint RxDrops { get; set; }

        public uint RxMulticast { get; set; }

        public uint TxBytes { get; set; }

        public uint TxPackets { get; set; }

        public uint TxErrors { get; set; }

        public uint TxMulticast { get; set; }

        public uint Collisions { get; set; }

        public uint NoProto { get; set; }

        public List<InterfaceAddress> IPv4Addresses { get; } = [];

        public List<InterfaceAddress> IPv6Addresses { get; } = [];

        public NetworkInterfaceInfo Build() => new()
        {
            Name = Name,
            State = (Flags & IFF_UP) == 0
                ? InterfaceState.Down
                : (Flags & IFF_RUNNING) != 0
                    ? InterfaceState.Up
                    : InterfaceState.NoCarrier,
            Flags = Flags,
            MacAddress = MacAddress,
            InterfaceType = InterfaceType,
            Mtu = Mtu,
            LinkSpeed = LinkSpeed,
            IPv4Addresses = IPv4Addresses,
            IPv6Addresses = IPv6Addresses,
            RxBytes = RxBytes,
            RxPackets = RxPackets,
            RxErrors = RxErrors,
            RxDrops = RxDrops,
            RxMulticast = RxMulticast,
            TxBytes = TxBytes,
            TxPackets = TxPackets,
            TxErrors = TxErrors,
            TxMulticast = TxMulticast,
            Collisions = Collisions,
            NoProto = NoProto,
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
    //------------------------------------------------------------------------
    // アドレスファミリー定数 (sys/socket.h)
    //------------------------------------------------------------------------

    // IPv4
    public const byte AF_INET = 2;

    // リンクレイヤー (MACアドレス等)
    public const byte AF_LINK = 18;

    // IPv6
    public const byte AF_INET6 = 30;

    //------------------------------------------------------------------------
    // インターフェースフラグ定数 (net/if.h)
    //------------------------------------------------------------------------

    // インターフェースが管理的に有効
    public const uint IFF_UP = 0x1;

    // ブロードキャスト対応
    public const uint IFF_BROADCAST = 0x2;

    // ループバックインターフェース
    public const uint IFF_LOOPBACK = 0x8;

    // ポイントツーポイントリンク
    public const uint IFF_POINTOPOINT = 0x10;

    // インターフェースが動作中 (キャリア検出済み)
    public const uint IFF_RUNNING = 0x40;

    // マルチキャスト対応
    public const uint IFF_MULTICAST = 0x8000;

    //------------------------------------------------------------------------
    // インターフェース種別定数 (net/if_types.h)
    //------------------------------------------------------------------------

    // Ethernet (IEEE 802.3)
    public const byte IFT_ETHER = 0x06;

    // ソフトウェアループバック
    public const byte IFT_LOOP = 0x18;

    // GIFトンネル (Generic tunnel)
    public const byte IFT_GIF = 0x37;

    // 6to4トンネル
    public const byte IFT_STF = 0x39;

    // IEEE 802.11 無線LAN (Wi-Fi)
    public const byte IFT_IEEE80211 = 0x47;

    // ブリッジインターフェース
    public const byte IFT_BRIDGE = 0xD1;

    // セルラー (モバイル通信)
    public const byte IFT_CELLULAR = 0xFF;

    //------------------------------------------------------------------------
    // inet_ntop用バッファサイズ定数 (netinet/in.h)
    //------------------------------------------------------------------------

    // IPv4アドレス文字列の最大長 ("255.255.255.255" + null)
    public const uint INET_ADDRSTRLEN = 16;

    // IPv6アドレス文字列の最大長
    public const uint INET6_ADDRSTRLEN = 46;

    //------------------------------------------------------------------------
    // ネイティブ構造体定義
    //------------------------------------------------------------------------

    // getifaddrsが返すリンクリストのノード (ifaddrs.h)
    [StructLayout(LayoutKind.Sequential)]
    public struct ifaddrs
    {
        // 次のエントリへのポインタ (リスト終端ではNULL)
        public nint ifa_next;

        // インターフェース名 (例: "en0")
        public nint ifa_name;

        // インターフェースフラグ (IFF_*の組み合わせ)
        public uint ifa_flags;

        // アドレス情報 (sa_familyでアドレス種別を判別)
        public nint ifa_addr;

        // ネットマスク
        public nint ifa_netmask;

        // 宛先アドレス (ポイントツーポイントの場合) またはブロードキャストアドレス
        public nint ifa_dstaddr;

        // アドレスファミリー固有のデータ (AF_LINKの場合はif_data*)
        public nint ifa_data;
    }

    // ソケットアドレス基本構造体 (sys/socket.h)
    // sa_familyでアドレスファミリーを判別し、適切な派生型にキャストする
    [StructLayout(LayoutKind.Sequential)]
    public struct sockaddr
    {
        public byte sa_len;
        public byte sa_family;
    }

    // リンクレイヤーアドレス構造体 (net/if_dl.h)
    // MACアドレスの取得に使用。sdl_dataは可変長で、インターフェース名の後にリンクアドレスが続く
    [StructLayout(LayoutKind.Sequential)]
    public struct sockaddr_dl
    {
        public byte sdl_len;
        public byte sdl_family;
        public ushort sdl_index;
        public byte sdl_type;
        public byte sdl_nlen;
        public byte sdl_alen;
        public byte sdl_slen;

        // sdl_data[12] は可変長のためフィールドとして定義しない
        // sdl_dataの開始オフセット (sizeof(固定部分) = 8)
        public const int DataOffset = 8;
    }

    // IPv4アドレス用ソケットアドレス構造体 (netinet/in.h) のオフセット定義
    // [sin_len(1)][sin_family(1)][sin_port(2)][sin_addr(4)][sin_zero(8)]
    public static class sockaddr_in
    {
        // sin_addrフィールドのオフセット
        public const int AddrOffset = 4;
    }

    // IPv6アドレス用ソケットアドレス構造体 (netinet6/in6.h) のオフセット定義
    // [sin6_len(1)][sin6_family(1)][sin6_port(2)][sin6_flowinfo(4)][sin6_addr(16)][sin6_scope_id(4)]
    public static class sockaddr_in6
    {
        // sin6_addrフィールドのオフセット
        public const int AddrOffset = 8;

        // sin6_scope_idフィールドのオフセット
        public const int ScopeIdOffset = 24;
    }

    // インターフェース統計情報構造体 (net/if.h)
    // AF_LINKエントリのifa_dataが指す構造体。受送信バイト/パケット/エラー等の統計を保持
    // 注意: macOS LP64環境ではifi_lastchangeはstruct timeval (tv_sec=long, tv_usec=int)
    [StructLayout(LayoutKind.Sequential)]
    public struct if_data
    {
        // インターフェース種別 (IFT_*の値)
        public byte ifi_type;

        // フレーム種別IDの長さ
        public byte ifi_typelen;

        // 物理メディア種別 (AUI, 10base-T等)
        public byte ifi_physical;

        // メディアアドレス長
        public byte ifi_addrlen;

        // メディアヘッダ長
        public byte ifi_hdrlen;

        // 受信割り込みのポーリングクォータ
        public byte ifi_recvquota;

        // 送信割り込みのポーリングクォータ
        public byte ifi_xmitquota;

        // 予約 (将来使用)
        public byte ifi_unused1;

        // 最大転送単位 (MTU)
        public uint ifi_mtu;

        // ルーティングメトリック
        public uint ifi_metric;

        // リンク速度 (bps)
        public uint ifi_baudrate;

        // 受信パケット数
        public uint ifi_ipackets;

        // 受信エラー数
        public uint ifi_ierrors;

        // 送信パケット数
        public uint ifi_opackets;

        // 送信エラー数
        public uint ifi_oerrors;

        // コリジョン数
        public uint ifi_collisions;

        // 受信バイト数
        public uint ifi_ibytes;

        // 送信バイト数
        public uint ifi_obytes;

        // 受信マルチキャストパケット数
        public uint ifi_imcasts;

        // 送信マルチキャストパケット数
        public uint ifi_omcasts;

        // 受信ドロップ数 (入力キュー溢れ等)
        public uint ifi_iqdrops;

        // 未対応プロトコルパケット数
        public uint ifi_noproto;

        // 受信処理時間 (マイクロ秒)
        public uint ifi_recvtiming;

        // 送信処理時間 (マイクロ秒)
        public uint ifi_xmittiming;

        // 最終変更時刻 (macOS LP64: tv_sec=long(8bytes), tv_usec=int(4bytes) + padding(4bytes))
        public long ifi_lastchange_tv_sec;
        public int ifi_lastchange_tv_usec;
        private int ifi_lastchange_pad;

        // 予約フィールド
        public uint ifi_unused2;
        public uint ifi_hwassist;
        public uint ifi_reserved1;
        public uint ifi_reserved2;
    }

    //------------------------------------------------------------------------
    // P/Invoke: libc (ネットワーク関連)
    //------------------------------------------------------------------------

    private const string LibC = "libc";

    // ネットワークインターフェースアドレスの一覧を取得
    // 成功時0を返し、ifapにリンクリストの先頭ポインタを設定する
    [DllImport(LibC)]
    public static extern int getifaddrs(out nint ifap);

    // getifaddrsで取得したリンクリストを解放する
    [DllImport(LibC)]
    public static extern void freeifaddrs(nint ifa);

    // ネットワークアドレスのバイナリ表現を文字列に変換する
    // af: アドレスファミリー (AF_INET/AF_INET6)
    // src: バイナリアドレスへのポインタ
    // dst: 変換結果の出力バッファ
    // size: 出力バッファのサイズ
    // 成功時dstポインタ、失敗時IntPtr.Zeroを返す
    [DllImport(LibC)]
    public static extern unsafe nint inet_ntop(int af, void* src, byte* dst, uint size);
}
