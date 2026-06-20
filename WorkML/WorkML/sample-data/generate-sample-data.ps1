# 装置ごとの長期間サンプルデータを生成する。
# 1日 = 288 件（5分間隔）、60 日分 = 17,280 件/チャンネル。
# 出力: {DeviceId}.csv（装置ごと、全チャンネルを含む）
$ErrorActionPreference = 'Stop'
$dir = $PSScriptRoot
$rng = [System.Random]::new(20260620)          # 再現性のため固定シード
$start = [DateTime]::new(2026, 4, 21, 0, 0, 0)  # 60 日前から
$perChannel = 288 * 60                           # 17,280 点/チャンネル

$devices = @(
    [pscustomobject]@{ Id = 'dev01'; Base = 100.0; Channels = 2; Degrade = $false }
    [pscustomobject]@{ Id = 'dev02'; Base = 200.0; Channels = 3; Degrade = $false }
    [pscustomobject]@{ Id = 'dev03'; Base = 200.0; Channels = 1; Degrade = $true }
)

foreach ($d in $devices) {
    $lines = [System.Collections.Generic.List[string]]::new($perChannel * $d.Channels + 1)
    $lines.Add('DeviceId,ChannelNo,Timestamp,Value')
    for ($ch = 1; $ch -le $d.Channels; $ch++) {
        for ($i = 0; $i -lt $perChannel; $i++) {
            $ts = $start.AddMinutes(5 * $i)
            $minOfDay = $ts.Hour * 60 + $ts.Minute
            $seasonal = $d.Base * 0.005 * [Math]::Sin(2 * [Math]::PI * $minOfDay / 1440.0)  # 日内の緩やかな変動
            $noise = $d.Base * ($rng.NextDouble() * 0.006 - 0.003)                          # ±0.3% ノイズ
            $val = $d.Base + $seasonal + $noise
            if ($d.Degrade -and $ch -eq 1) {
                $frac = $i / [double]$perChannel
                if ($frac -gt 0.8) {
                    $val -= $d.Base * 0.09 * (($frac - 0.8) / 0.2)   # 終盤 20% で約 0.91 倍まで電圧降下（劣化の予兆）
                }
            }
            $lines.Add(('{0},{1},{2},{3:F1}' -f $d.Id, $ch, $ts.ToString('yyyy-MM-ddTHH:mm:ss'), $val))
        }
    }
    $path = Join-Path $dir ($d.Id + '.csv')
    [System.IO.File]::WriteAllLines($path, $lines)
    Write-Output ('{0}: {1} rows' -f $d.Id, ($lines.Count - 1))
}

Remove-Item (Join-Path $dir 'readings.csv') -ErrorAction SilentlyContinue
Write-Output 'done'
