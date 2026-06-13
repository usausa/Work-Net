namespace WorkCar;

/// <summary>
/// 走行デモ用の車両シミュレータ。
/// ストレート→ブレーキ→コーナーのフェーズを自動再生し、
/// RPM / 速度 / シフト / ブーストポット / 温度系などの計器値を生成する。
/// Space キー(BoostRequested)で手動ブーストも可能。
/// </summary>
public class VehicleSimulator
{
    // ---- 外部入力 ----
    public bool BoostRequested;

    // ---- テレメトリ(描画側が参照) ----
    public float Rpm;
    public float Speed;                  // km/h
    public int Gear = 1;                 // 1..8
    public float Throttle;               // 0..1
    public float Brake;                  // 0..1
    public float BoostCharge = 1f;       // 0..1 ブーストポット残量
    public bool BoostActive;
    public float TurboPressure;          // bar
    public float WaterTemp = 71f;        // ℃
    public float OilTemp = 77f;          // ℃
    public float OilPressure;            // bar
    public float Fuel = 0.92f;           // 0..1
    public float Battery = 13.6f;        // V
    public float ErsOutput;              // kW(回生時は負)
    public float GLong, GLat;            // G
    public int Lap = 23;
    public int TotalLaps = 51;
    public int Position = 1;
    public float LapTime;                // s
    public float BestLap = 77.892f;      // s
    public float SessionTime;            // s
    public float SpeedMax = 612f;

    public const float MaxRpm = 19000f;
    public const float RedlineRpm = 17000f;
    public const float ShiftRpm = 17800f;
    public const float IdleRpm = 1400f;
    public const int TopGear = 8;
    public const float TopSpeed = 680f;

    // 速度[km/h] = Rpm * SpeedPerRpm / ギア比
    const float SpeedPerRpm = 0.028f;
    static readonly float[] Ratios = { 0f, 2.90f, 2.20f, 1.80f, 1.52f, 1.30f, 1.12f, 0.97f, 0.86f };

    enum Phase { Straight, Brake, Corner }

    Phase _phase = Phase.Straight;
    float _phaseTime;
    float _phaseLen = 12f;
    float _cornerDir = 1f;
    float _shiftCut;        // 変速直後のスロットルカット残時間
    float _autoBoostTime;   // デモ用 自動ブースト残時間
    float _lapLen = 79.4f;
    readonly Random _rng = new(7);

    public void Update(float dt)
    {
        SessionTime += dt;
        UpdateDriver(dt);
        UpdateLap(dt);

        // ---- ブーストポット ----
        bool wantBoost = BoostRequested || _autoBoostTime > 0f;
        BoostActive = wantBoost && BoostCharge > 0.04f && Throttle > 0.25f;
        if (BoostActive)
            BoostCharge = MathF.Max(0f, BoostCharge - dt * 0.16f);
        else
            BoostCharge = MathF.Min(1f, BoostCharge + dt * (Throttle < 0.2f ? 0.085f : 0.045f));

        // ---- 縦方向の運動(単位: km/h/s) ----
        float ratio = Ratios[Gear];
        float thrust = Throttle * (95f + 360f / Gear);
        if (BoostActive) thrust *= 1.65f;
        if (_shiftCut > 0f) { thrust *= 0.15f; _shiftCut -= dt; }
        float drag = Speed * Speed * 0.00045f + 9f;
        float accel = MathF.Min(thrust, 165f) - drag - Brake * 175f;
        if (Speed <= 0.01f && accel < 0f) accel = 0f;
        Speed = Math.Clamp(Speed + accel * dt, 0f, TopSpeed);
        SpeedMax = MathF.Max(SpeedMax, Speed);

        // ---- RPM(速度とギアから導出、応答に一次遅れ) ----
        float driven = Speed * ratio / SpeedPerRpm;
        float rpmTarget = MathF.Max(IdleRpm + Throttle * 900f, driven);
        Rpm += (rpmTarget - Rpm) * MathF.Min(1f, dt * 14f);
        Rpm = Math.Clamp(Rpm, 0f, MaxRpm);

        // ---- 変速 ----
        if (Rpm > ShiftRpm && Gear < TopGear && Throttle > 0.4f)
        {
            Gear++;
            _shiftCut = 0.13f;
        }
        else if (Gear > 1 && Rpm < 9000f && (Brake > 0.2f || Throttle < 0.15f)
                 && Speed * Ratios[Gear - 1] / SpeedPerRpm < 15500f)
        {
            Gear--;
            _shiftCut = 0.10f;
        }

        // ---- ターボ圧 ----
        float boostTarget = 0.25f + Throttle * 1.45f
                          + (BoostActive ? 0.95f : 0f)
                          - (_shiftCut > 0f ? 0.8f : 0f);
        TurboPressure += (boostTarget - TurboPressure) * MathF.Min(1f, dt * 5f);
        TurboPressure = MathF.Max(0f, TurboPressure);

        // ---- ERS 出力(制動時は回生で負側) ----
        float ersTarget = BoostActive ? 280f : Throttle * 120f - Brake * 90f;
        ErsOutput += (ersTarget - ErsOutput) * MathF.Min(1f, dt * 4f);

        // ---- G ----
        GLong += (accel / 35.3f - GLong) * MathF.Min(1f, dt * 6f);
        float latTarget = 0f;
        if (_phase == Phase.Corner)
            latTarget = _cornerDir * (2.2f + 1.4f * MathF.Sin(_phaseTime / _phaseLen * MathF.PI))
                        * MathF.Min(1f, Speed / 200f);
        GLat += (latTarget - GLat) * MathF.Min(1f, dt * 5f);

        // ---- 温度・油圧・電圧・燃料 ----
        float load = Throttle * 0.7f + (BoostActive ? 0.55f : 0f);
        float waterTarget = 82f + load * 17f + Speed * 0.013f;
        WaterTemp += (waterTarget - WaterTemp) * dt * 0.05f;
        float oilTarget = 95f + load * 36f + Speed * 0.01f;
        OilTemp += (oilTarget - OilTemp) * dt * 0.04f;
        OilPressure = 1.6f + Rpm / MaxRpm * 7.2f + 0.15f * MathF.Sin(SessionTime * 9f);
        Battery = 13.5f + 0.25f * MathF.Sin(SessionTime * 0.9f) + (BoostActive ? -0.7f : 0f);
        Fuel -= dt * (0.00022f + 0.0012f * Throttle + (BoostActive ? 0.0015f : 0f));
        if (Fuel < 0.04f) Fuel = 0.95f; // ピット給油したことにする
    }

    void UpdateDriver(float dt)
    {
        _phaseTime += dt;
        switch (_phase)
        {
            case Phase.Straight:
                Throttle = MathF.Min(1f, Throttle + dt * 2.5f);
                Brake = 0f;
                // ストレート中盤、充電十分・高ギアなら自動ブースト発動
                if (_autoBoostTime <= 0f && _phaseTime > 3.5f && BoostCharge > 0.75f && Gear >= 6)
                    _autoBoostTime = 2.6f;
                if (_phaseTime > _phaseLen)
                    Next(Phase.Brake, 1.1f + (float)_rng.NextDouble() * 0.5f);
                break;

            case Phase.Brake:
                Throttle = 0f;
                Brake = MathF.Min(1f, Brake + dt * 6f);
                if (_phaseTime > _phaseLen || Speed < 130f)
                    Next(Phase.Corner, 1.8f + (float)_rng.NextDouble() * 1.4f);
                break;

            case Phase.Corner:
                Brake = MathF.Max(0f, Brake - dt * 4f);
                Throttle = 0.45f + 0.1f * MathF.Sin(SessionTime * 3f);
                if (_phaseTime > _phaseLen)
                    Next(Phase.Straight, 8f + (float)_rng.NextDouble() * 6f);
                break;
        }
        if (_autoBoostTime > 0f) _autoBoostTime -= dt;
    }

    void Next(Phase p, float len)
    {
        _phase = p;
        _phaseTime = 0f;
        _phaseLen = len;
        if (p == Phase.Corner) _cornerDir = _rng.Next(2) == 0 ? -1f : 1f;
    }

    void UpdateLap(float dt)
    {
        LapTime += dt;
        if (LapTime >= _lapLen)
        {
            if (LapTime < BestLap) BestLap = LapTime;
            LapTime = 0f;
            Lap++;
            if (Lap > TotalLaps) Lap = 1;
            _lapLen = 76f + (float)_rng.NextDouble() * 6f;
        }
    }
}
