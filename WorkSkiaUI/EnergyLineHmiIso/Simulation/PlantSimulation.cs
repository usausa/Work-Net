using System;
using System.Collections.Generic;
using System.Linq;

namespace EnergyLineHmiIso.Simulation;

public enum EquipState { Running, Stopped, Alarm }

public enum Severity { Info, Warn, Alarm }

public sealed class Equipment
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public bool CanToggle { get; init; }
    public EquipState State { get; set; } = EquipState.Running;
}

public sealed class EventEntry
{
    public DateTime Time { get; } = DateTime.Now;
    public required string Message { get; init; }
    public Severity Severity { get; init; }
    public bool Active { get; set; }
}

/// <summary>
/// 工場エネルギーライン（電力 / 都市ガス / 蒸気 / 復水）の簡易プロセスシミュレータ。
/// 実測値の代わりにランダムウォークで揺らいだ値を生成する。
/// </summary>
public sealed class PlantSimulation
{
    public const double ContractKw = 2000;   // 契約電力
    public const int TrendCapacity = 180;    // 0.5s 間隔 × 180 点 = 90 秒

    readonly Random _rnd = new();

    public Dictionary<string, Equipment> Equip { get; } = new();
    public List<EventEntry> Events { get; } = new();          // 先頭が最新
    public Queue<float> Trend { get; } = new();

    // ---- 計測値 ----
    public double GridKw, CgsKw, PvKw;
    public double LineAKw, LineBKw, UtilKw;
    public double MiscKw = 180;                                // その他共通負荷
    public double GasCgs, GasBlr;
    public double SteamBlr, SteamCgs, SteamHeatA, SteamDry;
    public double SteamPressure = 0.78;
    public double FeedwaterFlow, MakeupFlow, CoolingFlow;
    public double TankLevel = 62;      // 給水タンク T-1 水位 %
    public double MakeupLevel = 78;    // 補給水タンク T-2 水位 %
    public double VoltageKv = 6.6, FrequencyHz = 50.0;
    public double AirFlow => GasBlr * 9.6;   // ボイラ燃焼空気 m³/h

    public double TotalLoadKw => LineAKw + LineBKw + UtilKw + MiscKw;
    public double OnSiteGenKw => CgsKw + PvKw;
    public double GasTotal => GasCgs + GasBlr;
    public double SteamSupply => SteamBlr + SteamCgs;
    public double SteamDemand => SteamHeatA + SteamDry;
    public double CondensateFlow => SteamSupply * 0.62;
    public int ActiveAlarmCount => Events.Count(ev => ev.Active && ev.Severity != Severity.Info);

    double _trendTimer;
    double _faultTimer = 15;
    double _faultRemain;
    Equipment? _faulted;
    EventEntry? _faultEntry;
    bool _demandAlarm, _steamAlarm, _lvlAlarm;
    EventEntry? _demandEntry, _steamEntry, _lvlEntry;

    // ランダムウォーク状態
    double _nA, _nB, _nU, _nCgs, _nPv, _nGas, _nSa, _nSd, _nV, _nF;

    public PlantSimulation()
    {
        Add("GRID", "受電変電所 66kV", false);
        Add("TR1", "主変圧器 TR-1", false);
        Add("PV", "太陽光発電 PV-1", true);
        Add("CGS", "ガスエンジン CGS-1", true);
        Add("GASIN", "都市ガス受入", false);
        Add("BLR", "貫流ボイラ B-1", true);
        Add("LINEA", "生産ライン A", true);
        Add("LINEB", "生産ライン B", true);
        Add("UTIL", "ユーティリティ", true);
        Add("HEATA", "加熱工程 (A)", false);
        Add("DRY", "乾燥炉 D-1", true);

        // 初期値（初回フレームから動いて見えるように）
        LineAKw = 760; LineBKw = 620; UtilKw = 540;
        CgsKw = 480; PvKw = 280;
        GridKw = Math.Max(30, TotalLoadKw - OnSiteGenKw);
        SteamHeatA = 4.6; SteamDry = 2.8; SteamCgs = 1.5; SteamBlr = 5.9;
        GasCgs = 130; GasBlr = 450;
        FeedwaterFlow = 6.1; CoolingFlow = 45;
        for (int i = 0; i < TrendCapacity; i++) Trend.Enqueue((float)TotalLoadKw);

        AddEvent(Severity.Info, "エネルギー監視システム起動");
    }

    void Add(string id, string name, bool toggle) =>
        Equip[id] = new Equipment { Id = id, Name = name, CanToggle = toggle };

    public bool IsOn(string id) => Equip[id].State != EquipState.Stopped;

    /// <summary>機器のクリックで起動 / 停止を切り替える。</summary>
    public void Toggle(string id)
    {
        if (!Equip.TryGetValue(id, out var eq) || !eq.CanToggle) return;

        if (eq.State == EquipState.Stopped)
        {
            eq.State = EquipState.Running;
            AddEvent(Severity.Info, $"{eq.Name} 起動操作");
        }
        else
        {
            if (eq == _faulted) ClearFault(restore: false);
            eq.State = EquipState.Stopped;
            AddEvent(Severity.Info, $"{eq.Name} 停止操作");
        }
    }

    public void Update(double dt)
    {
        if (dt <= 0) return;

        // ---- 電力負荷 ----
        LineAKw = Approach(LineAKw, IsOn("LINEA") ? 760 * Noise(ref _nA, dt) : 0, dt);
        LineBKw = Approach(LineBKw, IsOn("LINEB") ? 620 * Noise(ref _nB, dt) : 0, dt);
        UtilKw  = Approach(UtilKw,  IsOn("UTIL")  ? 540 * Noise(ref _nU, dt) : 0, dt);

        // ---- 構内発電 ----
        CgsKw = Approach(CgsKw, IsOn("CGS") ? 480 * Noise(ref _nCgs, dt, 0.04) : 0, dt, 2.5);
        PvKw  = Approach(PvKw,  IsOn("PV")  ? 280 * Noise(ref _nPv, dt, 0.18) : 0, dt, 4.0);

        GridKw = Math.Max(30, TotalLoadKw - OnSiteGenKw);

        // ---- 蒸気系 ----
        Equip["HEATA"].State = IsOn("LINEA") ? EquipState.Running : EquipState.Stopped;
        SteamHeatA = Approach(SteamHeatA, IsOn("LINEA") ? 4.6 * Noise(ref _nSa, dt) : 0, dt);
        SteamDry   = Approach(SteamDry,   IsOn("DRY")   ? 2.8 * Noise(ref _nSd, dt) : 0, dt);
        SteamCgs   = Approach(SteamCgs, IsOn("CGS") ? 1.5 : 0, dt, 3.0);

        double blrTarget = IsOn("BLR") ? Math.Clamp(SteamDemand - SteamCgs, 0.4, 9.0) : 0;
        SteamBlr = Approach(SteamBlr, blrTarget, dt, 2.2);

        double pressRatio = SteamDemand < 0.1 ? 1.0 : Math.Clamp(SteamSupply / SteamDemand, 0.5, 1.04);
        SteamPressure = Approach(SteamPressure, 0.78 * pressRatio, dt, 2.5);

        // ---- ガス系 ----
        GasCgs = Approach(GasCgs, IsOn("CGS") ? 130 * Noise(ref _nGas, dt, 0.05) : 0, dt, 2.5);
        GasBlr = Approach(GasBlr, SteamBlr * 76, dt, 2.2);

        // ---- 給水・タンク・冷却水・電源品質 ----
        FeedwaterFlow = Approach(FeedwaterFlow, IsOn("BLR") ? SteamBlr * 1.04 : 0, dt, 2.0);
        double makeupTarget = Math.Clamp((62 - TankLevel) * 0.25 + (FeedwaterFlow - CondensateFlow), 0, 4);
        if (MakeupLevel < 5) makeupTarget = 0;
        MakeupFlow = Approach(MakeupFlow, makeupTarget, dt, 2.5);
        TankLevel = Math.Clamp(TankLevel + (CondensateFlow + MakeupFlow - FeedwaterFlow) * dt * 0.5, 2, 98);
        MakeupLevel = Math.Clamp(MakeupLevel + ((80 - MakeupLevel) * 0.02 - MakeupFlow * 0.30) * dt, 2, 98);
        CoolingFlow = Approach(CoolingFlow, IsOn("CGS") ? 45 : 0, dt, 2.5);
        VoltageKv = 6.6 * Noise(ref _nV, dt, 0.012);
        FrequencyHz = 50.0 * Noise(ref _nF, dt, 0.0008);

        UpdateFaults(dt);
        UpdateProcessAlarms();

        // ---- トレンド ----
        _trendTimer += dt;
        if (_trendTimer >= 0.5)
        {
            _trendTimer = 0;
            Trend.Enqueue((float)TotalLoadKw);
            while (Trend.Count > TrendCapacity) Trend.Dequeue();
        }
    }

    /// <summary>ランダムなタイミングで運転中機器に擬似異常を発生させ、数秒後に自動復旧する。</summary>
    void UpdateFaults(double dt)
    {
        if (_faulted == null)
        {
            _faultTimer -= dt;
            if (_faultTimer <= 0)
            {
                var candidates = Equip.Values
                    .Where(e => e.CanToggle && e.State == EquipState.Running).ToList();
                if (candidates.Count > 0)
                {
                    _faulted = candidates[_rnd.Next(candidates.Count)];
                    _faulted.State = EquipState.Alarm;
                    _faultRemain = 6 + _rnd.NextDouble() * 4;
                    _faultEntry = AddEvent(Severity.Alarm, $"{_faulted.Name} 異常検出", active: true);
                }
                _faultTimer = 20 + _rnd.NextDouble() * 25;
            }
        }
        else
        {
            _faultRemain -= dt;
            if (_faultRemain <= 0) ClearFault(restore: true);
        }
    }

    void ClearFault(bool restore)
    {
        if (_faulted == null) return;
        if (_faultEntry != null) _faultEntry.Active = false;
        if (restore && _faulted.State == EquipState.Alarm)
        {
            _faulted.State = EquipState.Running;
            AddEvent(Severity.Info, $"{_faulted.Name} 復旧");
        }
        _faulted = null;
        _faultEntry = null;
    }

    void UpdateProcessAlarms()
    {
        // デマンド警報（契約電力の 95% 超過でセット、88% 未満でリセット）
        if (!_demandAlarm && GridKw > ContractKw * 0.95)
        {
            _demandAlarm = true;
            _demandEntry = AddEvent(Severity.Warn, "デマンド警報: 受電電力が契約の95%を超過", active: true);
        }
        else if (_demandAlarm && GridKw < ContractKw * 0.88)
        {
            _demandAlarm = false;
            if (_demandEntry != null) _demandEntry.Active = false;
        }

        // 給水タンク低水位
        if (!_lvlAlarm && TankLevel < 25)
        {
            _lvlAlarm = true;
            _lvlEntry = AddEvent(Severity.Warn, "給水タンク T-1 低水位", active: true);
        }
        else if (_lvlAlarm && TankLevel > 32)
        {
            _lvlAlarm = false;
            if (_lvlEntry != null) _lvlEntry.Active = false;
        }

        // 蒸気供給不足
        if (!_steamAlarm && SteamDemand > SteamSupply + 0.4)
        {
            _steamAlarm = true;
            _steamEntry = AddEvent(Severity.Warn, "蒸気供給不足: ヘッダ圧力低下", active: true);
        }
        else if (_steamAlarm && SteamSupply >= SteamDemand - 0.1)
        {
            _steamAlarm = false;
            if (_steamEntry != null) _steamEntry.Active = false;
        }
    }

    EventEntry AddEvent(Severity sev, string msg, bool active = false)
    {
        var e = new EventEntry { Message = msg, Severity = sev, Active = active };
        Events.Insert(0, e);
        if (Events.Count > 30) Events.RemoveAt(Events.Count - 1);
        return e;
    }

    /// <summary>1.0 を中心に ±amp で揺らぐ係数を返す。</summary>
    double Noise(ref double state, double dt, double amp = 0.07)
    {
        state += (_rnd.NextDouble() * 2 - 1) * dt * 0.9;
        state = Math.Clamp(state, -1, 1);
        return 1 + state * amp;
    }

    /// <summary>一次遅れで target に漸近させる（tau: 時定数 秒）。</summary>
    static double Approach(double cur, double target, double dt, double tau = 1.6) =>
        cur + (target - cur) * Math.Min(1.0, dt / tau);
}
