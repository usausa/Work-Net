# MacDotNet.SystemInfo vs LinuxDotNet.SystemInfo 機能比較表

## 凡例

- **取得方式**
  - `PlatformProvider`: `PlatformProvider.GetXxx()` 経由で取得
  - `Static Method`: 静的メソッドで直接取得
  - `Instance`: インスタンスのプロパティ/メソッドで取得

- **データ更新**
  - `Update()`: `Update()` メソッドで値を更新可能
  - `Snapshot`: 取得時点のスナップショット値（更新不可）
  - `Live`: プロパティアクセス時に都度読み取り
  - `Static`: 固定値（staticファクトリーメソッドで取得）

---

## システム基本情報

| 機能 | Mac | Linux | Mac取得方式 | Linux取得方式 | Macデータ更新 | Linuxデータ更新 |
|------|-----|-------|-------------|---------------|---------------|-----------------|
| Uptime | ✅ `UptimeInfo` | ✅ `UptimeInfo` | PlatformProvider | PlatformProvider | Update() | Update() |
| Load Average | ✅ `LoadAverageInfo` | ✅ `LoadAverageInfo` | PlatformProvider | PlatformProvider | Update() | Update() |

---

## CPU情報

| 機能 | Mac | Linux | Mac取得方式 | Linux取得方式 | Macデータ更新 | Linuxデータ更新 |
|------|-----|-------|-------------|---------------|---------------|-----------------|
| CPU基本情報 | ✅ `HardwareInfo` | ✅ `CpuDevice` | PlatformProvider | PlatformProvider | Static | - |
| CPU使用率ティック | ✅ `CpuUsageInfo` | ✅ `StaticsInfo` | PlatformProvider | PlatformProvider | Update() | Update() |
| 論理/物理CPU数 | ✅ | ✅ | - | - | - | - |
| CPUブランド名 | ✅ | ✅ | - | - | - | - |
| CPU周波数 | ✅ | ✅ | - | - | - | - |
| コア毎の周波数 | ❌ | ✅ `CpuCore` | - | Instance | - | Update() |
| CPU電力(RAPL) | ❌ | ✅ `CpuPower` | - | Instance | - | Update() |

---

## メモリ情報

| 機能 | Mac | Linux | Mac取得方式 | Linux取得方式 | Macデータ更新 | Linuxデータ更新 |
|------|-----|-------|-------------|---------------|---------------|-----------------|
| メモリ情報 | ✅ `MemoryInfo` | ✅ `MemoryInfo` | PlatformProvider | PlatformProvider | Update() | Update() |
| スワップ情報 | ✅ `SwapInfo` | ✅ (MemoryInfo内) | PlatformProvider | PlatformProvider | Update() | Update() |
| VM統計 | ✅ (MemoryInfo内) | ✅ `VirtualMemoryInfo` | PlatformProvider | PlatformProvider | Update() | Update() |
| 物理メモリ総量 | ✅ | ✅ | - | - | - | - |
| Active/Inactive/Wired | ✅ | ✅ | - | - | - | - |
| コンプレッサー情報 | ✅ | ❌ | - | - | - | - |

---

## ネットワーク情報

| 機能 | Mac | Linux | Mac取得方式 | Linux取得方式 | Macデータ更新 | Linuxデータ更新 |
|------|-----|-------|-------------|---------------|---------------|-----------------|
| インターフェース一覧 | ✅ `NetworkInfo` | ✅ `NetworkInfo` | PlatformProvider | PlatformProvider | Snapshot | Live |
| Up/Down状態 | ✅ `InterfaceState` | ✅ `InterfaceState` | Instance | Instance | Snapshot | Live |
| MACアドレス | ✅ | ✅ | Instance | Instance | Snapshot | Live |
| MTU | ✅ | ✅ | Instance | Instance | Snapshot | Live |
| リンク速度 | ✅ | ✅ | Instance | Instance | Snapshot | Live |
| インターフェースタイプ | ✅ | ✅ | Instance | Instance | Snapshot | Live |
| IPv4/IPv6アドレス | ✅ | ✅ | Instance | Instance | Snapshot | Live |
| Rx/Txバイト | ✅ | ✅ | Instance | Instance | Snapshot | Live |
| Rx/Txパケット | ✅ | ✅ | Instance | Instance | Snapshot | Live |
| Rx/Txエラー | ✅ | ✅ | Instance | Instance | Snapshot | Live |
| 統計情報(累積) | ❌ | ✅ `NetworkStaticInfo` | - | PlatformProvider | - | Update() |
| TCP接続状態 | ❌ | ✅ `TcpInfo` | - | PlatformProvider | - | Update() |

---

## プロセス情報

| 機能 | Mac | Linux | Mac取得方式 | Linux取得方式 | Macデータ更新 | Linuxデータ更新 |
|------|-----|-------|-------------|---------------|---------------|-----------------|
| プロセス一覧 | ✅ `ProcessInfo` | ✅ `ProcessInfo` | PlatformProvider | PlatformProvider | Snapshot | Snapshot |
| プロセスサマリー | ❌ | ✅ `ProcessSummaryInfo` | - | PlatformProvider | - | Update() |
| PID/PPID | ✅ | ✅ | - | - | - | - |
| プロセス名 | ✅ | ✅ | - | - | - | - |
| 実行パス | ✅ | ✅ | - | - | - | - |
| コマンドライン | ❌ | ✅ | - | - | - | - |
| UID/GID | ✅ | ✅ | - | - | - | - |
| Nice値 | ✅ | ✅ | - | - | - | - |
| オープンファイル数 | ✅ | ✅ | - | - | - | - |
| スレッド数 | ✅ | ✅ | - | - | - | - |
| プロセス状態 | ❌ | ✅ `State` | - | - | - | - |
| 仮想メモリサイズ | ✅ | ✅ | - | - | - | - |
| 常駐メモリサイズ | ✅ | ✅ | - | - | - | - |
| 共有メモリサイズ | ❌ | ✅ | - | - | - | - |
| User/System時間 | ✅ | ✅ | - | - | - | - |
| ページフォルト | ✅ | ✅ | - | - | - | - |
| コンテキストスイッチ | ✅ | ❌ | - | - | - | - |
| システムコール数 | ✅ | ❌ | - | - | - | - |

---

## ファイルシステム情報

| 機能 | Mac | Linux | Mac取得方式 | Linux取得方式 | Macデータ更新 | Linuxデータ更新 |
|------|-----|-------|-------------|---------------|---------------|-----------------|
| ファイルシステム一覧 | ✅ `FileSystemInfo` | ✅ `FileSystemInfo` | PlatformProvider | PlatformProvider | Snapshot | Snapshot |
| パーティション一覧 | ❌ | ✅ `Partition` | - | PlatformProvider | - | Snapshot |
| マウントポイント | ✅ | ✅ | - | - | - | - |
| デバイス名 | ✅ | ✅ | - | - | - | - |
| ファイルシステムタイプ | ✅ | ✅ | - | - | - | - |
| 合計/空き/利用可能サイズ | ✅ | ✅ | - | - | - | - |
| 使用率 | ✅ | ✅ | - | - | - | - |
| inode情報 | ✅ | ✅ | - | - | - | - |
| 読み取り専用フラグ | ✅ | ✅ | - | - | - | - |
| ローカル判定 | ✅ | ✅ | - | - | - | - |
| マウントオプション | ❌ | ✅ | - | - | - | - |
| ディスクI/O統計 | ❌ | ✅ `DiskStaticsInfo` | - | PlatformProvider | - | Update() |
| ファイルディスクリプタ | ❌ | ✅ `FileDescriptorInfo` | - | PlatformProvider | - | Update() |

---

## ハードウェア/カーネル情報

| 機能 | Mac | Linux | Mac取得方式 | Linux取得方式 | Macデータ更新 | Linuxデータ更新 |
|------|-----|-------|-------------|---------------|---------------|-----------------|
| ハードウェア情報 | ✅ `HardwareInfo` | ✅ `HardwareInfo` | PlatformProvider | PlatformProvider | Static | Static |
| カーネル情報 | ✅ `KernelInfo` | ✅ `KernelInfo` | PlatformProvider | PlatformProvider | Static | Static |
| モデル名 | ✅ | ✅ | - | - | - | - |
| マシンアーキテクチャ | ✅ | ✅ | - | - | - | - |
| CPUブランド名 | ✅ | ✅ | - | - | - | - |
| 論理/物理CPU数 | ✅ | ✅ | - | - | - | - |
| メモリサイズ | ✅ | ✅ | - | - | - | - |
| ページサイズ | ✅ | ✅ | - | - | - | - |
| キャッシュ情報 | ✅ | ✅ | - | - | - | - |
| パフォーマンスレベル(Apple Silicon) | ✅ | ❌ | PlatformProvider | - | Snapshot | - |
| OSタイプ | ✅ | ✅ | - | - | - | - |
| OSリリース | ✅ | ✅ | - | - | - | - |
| OS製品バージョン | ✅ | ✅ | - | - | - | - |
| ブート時刻 | ✅ | ✅ | - | - | - | - |
| 最大プロセス数 | ✅ | ✅ | - | - | - | - |
| 最大ファイル数 | ✅ | ✅ | - | - | - | - |
| UUID | ✅ | ❌ | - | - | - | - |
| ベンダー名 | ❌ | ✅ | - | - | - | - |
| OS名/PrettyName | ❌ | ✅ | - | - | - | - |

---

## GPU情報

| 機能 | Mac | Linux | Mac取得方式 | Linux取得方式 | Macデータ更新 | Linuxデータ更新 |
|------|-----|-------|-------------|---------------|---------------|-----------------|
| GPU一覧 | ✅ `GpuInfo` | ❌ | PlatformProvider | - | Snapshot | - |
| GPUモデル名 | ✅ | ❌ | - | - | - | - |
| GPU使用率 | ✅ | ❌ | - | - | - | - |
| GPUメモリ | ✅ | ❌ | - | - | - | - |
| GPU構成情報 | ✅ | ❌ | - | - | - | - |

---

## バッテリー/電源情報

| 機能 | Mac | Linux | Mac取得方式 | Linux取得方式 | Macデータ更新 | Linuxデータ更新 |
|------|-----|-------|-------------|---------------|---------------|-----------------|
| バッテリー情報 | ✅ `BatteryInfo` | ✅ `BatteryDevice` | PlatformProvider | PlatformProvider | Update() | Update() |
| AC電源情報 | ✅ (BatteryInfo内) | ✅ `MainsAdapterDevice` | PlatformProvider | PlatformProvider | Update() | Update() |
| バッテリー残量 | ✅ | ✅ | - | - | - | - |
| 充電状態 | ✅ | ✅ | - | - | - | - |
| 充電完了までの時間 | ✅ | ❌ | - | - | - | - |
| 空になるまでの時間 | ✅ | ❌ | - | - | - | - |
| バッテリー健康状態 | ✅ | ❌ | - | - | - | - |

---

## センサー/モニタリング情報

| 機能 | Mac | Linux | Mac取得方式 | Linux取得方式 | Macデータ更新 | Linuxデータ更新 |
|------|-----|-------|-------------|---------------|---------------|-----------------|
| 温度センサー | ✅ `SmcInfo` | ✅ `HardwareMonitor` | PlatformProvider | PlatformProvider | Snapshot | Update() |
| ファン情報 | ✅ `SmcInfo` | ✅ `HardwareMonitor` | PlatformProvider | PlatformProvider | Snapshot | Update() |
| 電力読み取り | ✅ `SmcInfo` | ✅ `HardwareMonitor` | PlatformProvider | PlatformProvider | Snapshot | Update() |
| 電圧読み取り | ✅ `SmcInfo` | ✅ `HardwareMonitor` | PlatformProvider | PlatformProvider | Snapshot | Update() |

---

## 設計方針

### 固定値クラス
- `HardwareInfo`, `KernelInfo` は固定値（システム起動後変化しない）を扱う
- staticファクトリーメソッド `Create()` で取得
- `Update()` メソッドは持たない

### 動的値クラス
- `MemoryInfo`, `LoadAverageInfo`, `CpuUsageInfo`, `BatteryInfo` 等は動的に変化する値を扱う
- `Update()` メソッドで最新値を取得可能
- `PhysicalMemory` 等の固定部分はコンストラクタ時のみ取得

### スナップショット値
- `ProcessInfo`, `FileSystemInfo`, `GpuInfo` 等は呼び出し時点のスナップショットを返す
- 毎回新しいインスタンスを生成

### Live読み取り (Linux NetworkInfo)
- プロパティアクセス時に `/sys/class/net/` から都度読み取り
- 無駄なファイルアクセスを避ける設計

---

## 使用例

### Mac

```csharp
// PlatformProvider経由
var uptime = PlatformProvider.GetUptime();
var memory = PlatformProvider.GetMemory();
var battery = PlatformProvider.GetBattery();

// Update()で値を更新（動的値のみ）
memory.Update();

// 固定値の取得（staticファクトリーメソッド経由）
var hardware = PlatformProvider.GetHardware();  // HardwareInfo.Create()
var kernel = PlatformProvider.GetKernel();      // KernelInfo.Create()

// スナップショット値の取得
var processes = PlatformProvider.GetProcesses();
var gpus = PlatformProvider.GetGpus();
```

### Linux

```csharp
// PlatformProvider経由
var uptime = PlatformProvider.GetUptime();
var memory = PlatformProvider.GetMemory();
var battery = PlatformProvider.GetBattery();

// Update()で値を更新（動的値のみ）
memory.Update();

// 固定値の取得（staticファクトリーメソッド経由）
var hardware = PlatformProvider.GetHardware();  // HardwareInfo.Create()
var kernel = PlatformProvider.GetKernel();      // KernelInfo.Create()

// スナップショット値の取得
var processes = PlatformProvider.GetProcesses();
var fileSystems = PlatformProvider.GetFileSystems();

// Live読み取り（ネットワーク）
var interfaces = PlatformProvider.GetNetworkInterfaces();
foreach (var iface in interfaces)
{
    // プロパティアクセス時にファイルを読み取り
    Console.WriteLine($"{iface.Name}: {iface.State}, {iface.RxBytes} bytes");
}

// 個別インターフェース取得
var eth0 = PlatformProvider.GetNetworkInterface("eth0");

// 個別プロセス取得
var process = PlatformProvider.GetProcess(1);
```
