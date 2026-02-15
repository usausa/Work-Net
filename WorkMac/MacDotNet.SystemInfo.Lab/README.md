# MacDotNet.SystemInfo.Lab

stats-masterにあり、MacDotNet.SystemInfoに実装されていない機能の検証用プロジェクト。

## クラス一覧

### 1. CpuLoadInfo.cs - CPU使用率情報

stats-masterで取得している項目で、MacDotNet.SystemInfoにない機能。

| 項目 | 取得方法 | 説明 |
|------|----------|------|
| User/System/Idle分離 | `host_statistics64(HOST_CPU_LOAD_INFO)` | CPU使用率をUser/System/Idleに分離 |
| コア毎の使用率 | `host_processor_info(PROCESSOR_CPU_LOAD_INFO)` | 各論理コアの使用率を個別に取得 |
| E-Core/P-Core別使用率 | `hw.perflevel{n}.logicalcpu` | Apple Silicon用、コアタイプ別の平均使用率 |
| ハイパースレッディング判定 | `hw.physicalcpu` vs `hw.logicalcpu` | 論理/物理CPUの差で判定 |

---

### 2. MemoryPressureInfo.cs - メモリプレッシャー情報

| 項目 | 取得方法 | 説明 |
|------|----------|------|
| プレッシャーレベル | `sysctlbyname("kern.memorystatus_vm_pressure_level")` | 1=Normal, 2=Warning, 4=Critical |

---

### 3. GpuDetailInfo.cs - GPU詳細情報

| 項目 | 取得方法 | 説明 |
|------|----------|------|
| GPU使用率 | `PerformanceStatistics["Device Utilization %"]` | IOAccelerator経由 |
| レンダラー使用率 | `PerformanceStatistics["Renderer Utilization %"]` | |
| タイラー使用率 | `PerformanceStatistics["Tiler Utilization %"]` | |
| GPU温度 | `PerformanceStatistics["Temperature(C)"]` または SMC (`TCGC`, `TGDD`) | |
| ファン速度 | `PerformanceStatistics["Fan Speed(%)"]` | |
| コアクロック | `PerformanceStatistics["Core Clock(MHz)"]` | |
| メモリクロック | `PerformanceStatistics["Memory Clock(MHz)"]` | |
| 電源状態 | `AGCInfo["poweredOffByAGC"]` | 0=Active, 1=Off |

---

### 4. NetworkDetailInfo.cs - ネットワーク詳細情報

| 項目 | 取得方法 | 説明 |
|------|----------|------|
| プライマリインターフェース | `SCDynamicStoreCopyValue("State:/Network/Global/IPv4")` | |
| VPN接続判定 | `CFNetworkCopySystemProxySettings` + ifconfig | utun/tun/tap/ppp/ipsec検出 |
| 接続タイプ | `SCNetworkInterfaceGetInterfaceType` | Ethernet/WiFi/Bluetooth等 |
| 帯域幅 (baudrate) | `getifaddrs` → `if_data.ifi_baudrate` | |
| WiFi SSID | `system_profiler SPAirPortDataType` または `networksetup` | |
| WiFi規格 | `system_profiler SPAirPortDataType` | 802.11n/ac/ax等 |

**注**: 完全なWiFi情報取得にはCoreWLAN (Objective-C) が必要。本実装ではsystem_profiler経由で代替。

---

### 5. BatteryDetailInfo.cs - バッテリー詳細情報

| 項目 | 取得方法 | 説明 |
|------|----------|------|
| 電圧 | `IORegistryEntryCreateCFProperty("Voltage")` / 1000 | V単位 |
| 電流 | `IORegistryEntryCreateCFProperty("Amperage")` | mA単位 |
| 温度 | `IORegistryEntryCreateCFProperty("Temperature")` / 100 | ℃単位 |
| サイクル数 | `IORegistryEntryCreateCFProperty("CycleCount")` | |
| 現在容量 | `IORegistryEntryCreateCFProperty("AppleRawCurrentCapacity")` | mAh |
| 設計容量 | `IORegistryEntryCreateCFProperty("DesignCapacity")` | mAh |
| 最大容量 | `AppleRawMaxCapacity` (ARM) / `MaxCapacity` (Intel) | mAh |
| AC電源ワット数 | `IOPSCopyExternalPowerAdapterDetails["Watts"]` | W |
| 充電電流/電圧 | `IORegistryEntryCreateCFProperty("ChargerData")` | mA/mV |
| 最適化充電 | `OptimizedBatteryChargingEngaged` | |

---

### 6. SensorDetailInfo.cs / AppleSiliconPowerInfo.cs - センサー/電力情報

#### Apple Silicon電力センサー (IOReport)

| 項目 | 取得方法 | 説明 |
|------|----------|------|
| CPU電力 | `IOReportCreateSamples` → "Energy Model" → "*CPU Energy" | W |
| GPU電力 | `IOReportCreateSamples` → "Energy Model" → "*GPU Energy" | W |
| ANE電力 | `IOReportCreateSamples` → "Energy Model" → "ANE*" | W |
| RAM電力 | `IOReportCreateSamples` → "Energy Model" → "DRAM*" | W |

#### ファン情報 (SMC)

| 項目 | 取得方法 | 説明 |
|------|----------|------|
| ファン数 | SMC `FNum` | |
| ファンモード | SMC `F{n}Md` または `FS! ` | 0=Automatic, 1=Forced |
| 最速ファン | SMC `F{n}Ac` から最大値を選択 | RPM |
| ファン速度 | SMC `F{n}Ac` | RPM |
| 最小/最大速度 | SMC `F{n}Mn`, `F{n}Mx` | RPM |

#### 電流センサー (SMC)

| 項目 | 取得方法 | 説明 |
|------|----------|------|
| 電流 | SMC `I*` キー | A |

#### 総消費電力

| 項目 | 取得方法 | 説明 |
|------|----------|------|
| 総消費電力 | SMC `PSTR` | W |

---

### 7. DiskDetailInfo.cs - ディスク詳細情報

| 項目 | 取得方法 | 説明 |
|------|----------|------|
| NVMe SMART対応 | `IORegistryEntryCreateCFProperty("NVMe SMART Capable")` | |
| ディスクI/O統計 | `IOBlockStorageDriver` → `Statistics` | bytes/operations |
| パージ可能領域 | `diskutil info` または `CSDiskSpaceGetRecoveryEstimate` | |

**注**: 完全なNVMe SMART情報取得には`IONVMeSMARTInterface`プラグインが必要。複雑なため本実装では基本情報のみ。

#### NVMe SMART (完全実装に必要な項目)

| 項目 | 取得方法 | 説明 |
|------|----------|------|
| 温度 | `nvme_smart_log.temperature` | ℃ |
| 寿命 | 100 - `nvme_smart_log.percent_used` | % |
| 総読み込み量 | `nvme_smart_log.data_units_read` × 512KB | bytes |
| 総書き込み量 | `nvme_smart_log.data_units_written` × 512KB | bytes |
| 電源サイクル数 | `nvme_smart_log.power_cycles` | |
| 電源オン時間 | `nvme_smart_log.power_on_hours` | hours |

---

### 8. SystemDetailInfo.cs - システム詳細情報

| 項目 | 取得方法 | 説明 |
|------|----------|------|
| モデルID | `sysctlbyname("hw.model")` | 例: "MacBookPro18,1" |
| モデル名 | `system_profiler SPHardwareDataType` | 例: "MacBook Pro" |
| シリアル番号 | `IOPlatformExpertDevice["IOPlatformSerialNumber"]` | |
| ディスプレイ情報 | `system_profiler SPDisplaysDataType` | 名前、解像度、メイン判定等 |
| DIMMスロット情報 | `system_profiler SPMemoryDataType` | サイズ、タイプ、速度 |
| コア周波数 | `hw.perflevel{n}.cpufreq_max` | Apple Silicon用 |

---

## 取得方法のまとめ

| API/ソース | 用途 |
|------------|------|
| `sysctlbyname` | CPU情報、メモリプレッシャー、システム情報 |
| `host_statistics64` | CPU使用率、VM統計 |
| `host_processor_info` | コア毎のCPU使用率 |
| `IORegistryEntryCreateCFProperty` | バッテリー、GPU、ディスク情報 |
| `IOAccelerator` (IOKit) | GPU詳細情報 |
| `IOReport` (IOKit) | Apple Silicon電力センサー |
| `AppleSMC` (IOKit) | 温度、ファン、電力センサー |
| `SCDynamicStore` | ネットワーク設定 |
| `SCNetworkInterface` | ネットワークインターフェース |
| `IOPowerSources` | バッテリー/AC電源 |
| `IOBlockStorageDriver` | ディスクI/O統計 |
| `system_profiler` | ハードウェア詳細情報 (JSON出力) |

---

## 実行方法

```bash
cd MacDotNet.SystemInfo.Lab
dotnet run
```

**注**: Mac上でのみ動作します。
