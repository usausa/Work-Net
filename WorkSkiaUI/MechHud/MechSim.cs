namespace MechHud;

public enum MoveMode { Walk, Dash, Hover }
public enum WeaponSide { Left, Right }
public enum RightArm { Cannon, Missile }

public sealed class GroundContact
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public float BearingDeg;
    public float RangeM;
    public float BearingDrift; // deg/s
    public float RangeRate;    // m/s, negative = closing
    public float ElevDeg;      // pseudo elevation offset for HUD projection
    public float ClosureMs => -RangeRate;
}

public sealed class SquadUnit
{
    public required string Code { get; init; }
    public required string Platoon { get; init; }
    public required bool NearOwn { get; init; } // own platoon moves with own unit

    public float OffX, OffY;       // meters in map space
    public float Ax, Ay, Fx, Fy, Px, Py; // wander sine parameters

    public float PosX(float t, float ownX) => (NearOwn ? ownX : 0f) + OffX + Ax * MathF.Sin(t * Fx + Px);
    public float PosY(float t, float ownY) => (NearOwn ? ownY : 0f) + OffY + Ay * MathF.Sin(t * Fy + Py);

    public float HeadingRad(float t)
    {
        float vx = Ax * Fx * MathF.Cos(t * Fx + Px);
        float vy = Ay * Fy * MathF.Cos(t * Fy + Py);
        return MathF.Atan2(vx, -vy);
    }
}

/// <summary>
/// Self-running ground-mech simulation: movement mode cycle (walk / dash /
/// NOE hover), propellant, part damage, L/R weapon swapping, comm traffic,
/// ground contacts and a 12-unit company laid out on the tactical map.
/// Map space: meters, origin at map center, +Y = north. Map covers 3000x2100 m.
/// </summary>
public sealed class MechSim
{
    public float Time { get; private set; }

    public float HeadingDeg { get; private set; } = 132f;
    public float PitchDeg { get; private set; }
    public float RollDeg { get; private set; }
    public float SpeedKmh { get; private set; } = 40f;
    public float AglM { get; private set; }
    public MoveMode Mode { get; private set; } = MoveMode.Walk;
    public float StepHz { get; private set; } = 1.7f;

    public float PropL { get; private set; } = 86f;
    public float PropR { get; private set; } = 82f;

    public const int PartCount = 8;
    public static readonly string[] PartNames = { "HEAD", "CORE", "L-ARM", "R-ARM", "L-LEG", "R-LEG", "L-JU", "R-JU" };
    public float[] PartHp { get; } = { 100f, 100f, 100f, 100f, 100f, 100f, 100f, 100f };
    public int ImpactPart { get; private set; } = -1;
    public float ImpactTimer { get; private set; }

    public WeaponSide ActiveSide { get; private set; } = WeaponSide.Left;
    public RightArm RightWeapon { get; private set; } = RightArm.Cannon;
    public float SwapFlash { get; private set; }

    public const int SmgMagSize = 240;
    public const float SmgReloadTime = 2.6f;
    public int SmgRounds => (int)_smgRounds;
    public int SmgMags { get; private set; } = 8;
    public bool SmgFiring { get; private set; }
    public float SmgReload { get; private set; }

    public const int CannonMax = 12;
    public const float CannonInterval = 4.5f;
    public const float CannonRackTime = 6f;
    public int CannonShells { get; private set; } = CannonMax;
    public float CannonFlash { get; private set; }
    public float CannonRackReload { get; private set; }
    public float CannonCharge => CannonShells > 0 && CannonRackReload <= 0f
        ? Math.Clamp(1f - _cannonCd / CannonInterval, 0f, 1f)
        : 0f;

    public const int MslMax = 8;
    public const float MslReloadTime = 9f;
    public int MslCount { get; private set; } = MslMax;
    public float MslFlash { get; private set; }
    public float MslReload { get; private set; }

    public float RadarSweepDeg { get; private set; }
    public IReadOnlyList<GroundContact> Contacts => _contacts;
    public GroundContact? LockedTarget { get; private set; }
    public bool LockSteady { get; private set; }

    public float OwnX { get; private set; }
    public float OwnY { get; private set; }
    public IReadOnlyList<SquadUnit> Squad => _squad;

    public int ActiveChannel { get; private set; }
    public string SecureKey { get; private set; } = "KEY-7A2F";
    public float TxTimer { get; private set; }
    public bool Transmitting => TxTimer > 0f;
    public string TxFrom { get; private set; } = "D1-2";

    public float IncomingTimer { get; private set; }

    private readonly List<GroundContact> _contacts = new();
    private readonly List<SquadUnit> _squad = new();
    private readonly Random _rng = new();
    private float _smgRounds = SmgMagSize;
    private float _cannonCd = 1.5f;
    private float _mslCd = 3f;
    private float _lockTime;
    private float _nextHit = 12f;
    private float _nextSwap = 12f;
    private float _nextCh = 13f;
    private float _nextKey = 20f;
    private float _nextTx = 5f;
    private float _nextIncoming = 33f;

    public MechSim()
    {
        _contacts.Add(new GroundContact { Name = "E1", Type = "ZK-09 MECH", BearingDeg = 25f,  RangeM = 850f,  BearingDrift = 0.6f,  RangeRate = -6f,  ElevDeg = 0.5f });
        _contacts.Add(new GroundContact { Name = "E2", Type = "AFV-7",      BearingDeg = 60f,  RangeM = 1500f, BearingDrift = -0.4f, RangeRate = -10f, ElevDeg = 0.0f });
        _contacts.Add(new GroundContact { Name = "E3", Type = "ZK-11 MECH", BearingDeg = 340f, RangeM = 2100f, BearingDrift = 0.8f,  RangeRate = -4f,  ElevDeg = 0.8f });
        _contacts.Add(new GroundContact { Name = "E4", Type = "SPG-4 ARTY", BearingDeg = 290f, RangeM = 2400f, BearingDrift = 0.2f,  RangeRate = 2f,   ElevDeg = 1.2f });
        _contacts.Add(new GroundContact { Name = "E5", Type = "T-88 TANK",  BearingDeg = 130f, RangeM = 1200f, BearingDrift = -0.7f, RangeRate = -3f,  ElevDeg = -0.4f });

        // platoon D1 (own = D1-1, drawn separately) sticks close to own unit
        _squad.Add(new SquadUnit { Code = "D1-2", Platoon = "D1", NearOwn = true,  OffX = -140f, OffY = 80f,   Ax = 50f,  Ay = 40f,  Fx = 0.050f, Fy = 0.040f, Px = 0.0f, Py = 1.0f });
        _squad.Add(new SquadUnit { Code = "D1-3", Platoon = "D1", NearOwn = true,  OffX = 150f,  OffY = 130f,  Ax = 45f,  Ay = 55f,  Fx = 0.042f, Fy = 0.055f, Px = 2.1f, Py = 0.4f });
        _squad.Add(new SquadUnit { Code = "D1-4", Platoon = "D1", NearOwn = true,  OffX = 80f,   OffY = -170f, Ax = 60f,  Ay = 45f,  Fx = 0.036f, Fy = 0.047f, Px = 4.0f, Py = 2.6f });
        // platoon D2 holds the west sector
        _squad.Add(new SquadUnit { Code = "D2-1", Platoon = "D2", NearOwn = false, OffX = -780f, OffY = 470f,  Ax = 90f,  Ay = 70f,  Fx = 0.030f, Fy = 0.024f, Px = 0.7f, Py = 3.1f });
        _squad.Add(new SquadUnit { Code = "D2-2", Platoon = "D2", NearOwn = false, OffX = -660f, OffY = 380f,  Ax = 70f,  Ay = 90f,  Fx = 0.026f, Fy = 0.033f, Px = 1.9f, Py = 0.2f });
        _squad.Add(new SquadUnit { Code = "D2-3", Platoon = "D2", NearOwn = false, OffX = -700f, OffY = 540f,  Ax = 100f, Ay = 60f,  Fx = 0.021f, Fy = 0.029f, Px = 3.3f, Py = 1.5f });
        _squad.Add(new SquadUnit { Code = "D2-4", Platoon = "D2", NearOwn = false, OffX = -590f, OffY = 460f,  Ax = 80f,  Ay = 80f,  Fx = 0.035f, Fy = 0.022f, Px = 5.0f, Py = 2.2f });
        // platoon D3 holds the east ridge
        _squad.Add(new SquadUnit { Code = "D3-1", Platoon = "D3", NearOwn = false, OffX = 690f,  OffY = 620f,  Ax = 85f,  Ay = 75f,  Fx = 0.028f, Fy = 0.031f, Px = 0.3f, Py = 4.0f });
        _squad.Add(new SquadUnit { Code = "D3-2", Platoon = "D3", NearOwn = false, OffX = 600f,  OffY = 520f,  Ax = 75f,  Ay = 95f,  Fx = 0.033f, Fy = 0.025f, Px = 1.4f, Py = 5.2f });
        _squad.Add(new SquadUnit { Code = "D3-3", Platoon = "D3", NearOwn = false, OffX = 760f,  OffY = 500f,  Ax = 95f,  Ay = 65f,  Fx = 0.024f, Fy = 0.036f, Px = 2.8f, Py = 0.9f });
        _squad.Add(new SquadUnit { Code = "D3-4", Platoon = "D3", NearOwn = false, OffX = 650f,  OffY = 700f,  Ax = 65f,  Ay = 85f,  Fx = 0.038f, Fy = 0.027f, Px = 4.4f, Py = 1.8f });
    }

    public void Update(float dt)
    {
        Time += dt;
        float t = Time;

        // ---- movement mode cycle: walk -> dash -> walk -> NOE hover
        float cyc = t % 39f;
        Mode = cyc < 14f ? MoveMode.Walk : cyc < 22f ? MoveMode.Dash : cyc < 28f ? MoveMode.Walk : MoveMode.Hover;
        StepHz = Mode switch { MoveMode.Walk => 1.7f, MoveMode.Dash => 2.6f, _ => 0.4f };

        float tgtSpd = Mode switch
        {
            MoveMode.Walk => 42f + 8f * MathF.Sin(t * 0.30f),
            MoveMode.Dash => 94f + 6f * MathF.Sin(t * 0.50f),
            _ => 128f + 10f * MathF.Sin(t * 0.27f),
        };
        SpeedKmh = MoveTo(SpeedKmh, tgtSpd, 30f * dt);

        float tgtAgl = Mode == MoveMode.Hover ? 3.5f + 2.2f * MathF.Sin(t * 0.9f) : 0f;
        AglM = MoveTo(AglM, tgtAgl, 4f * dt);

        RollDeg = 2.5f * MathF.Sin(t * 0.5f) + 1.2f * MathF.Sin(t * 1.3f);
        PitchDeg = 1.8f * MathF.Sin(t * 0.4f) + 1.0f * MathF.Sin(t * 0.9f) + (Mode == MoveMode.Dash ? 1.6f : 0f);
        HeadingDeg = Wrap360(HeadingDeg + 5f * MathF.Sin(t * 0.11f) * dt);

        OwnX = 220f * MathF.Sin(t * 0.021f);
        OwnY = 160f * MathF.Sin(t * 0.013f + 1.2f);

        // ---- propellant: jump units burn in hover/dash, field-recharge while walking
        float dl = Mode switch { MoveMode.Hover => -2.3f, MoveMode.Dash => -0.90f, _ => 0.6f };
        float dr = Mode switch { MoveMode.Hover => -2.1f, MoveMode.Dash => -0.85f, _ => 0.6f };
        PropL = Math.Clamp(PropL + dl * dt, 2f, 100f);
        PropR = Math.Clamp(PropR + dr * dt, 2f, 100f);

        // ---- incoming damage + slow nano repair
        ImpactTimer = MathF.Max(0f, ImpactTimer - dt);
        if (t >= _nextHit)
        {
            _nextHit += 17f;
            ImpactPart = _rng.Next(PartCount);
            PartHp[ImpactPart] = MathF.Max(8f, PartHp[ImpactPart] - (12f + 18f * (float)_rng.NextDouble()));
            ImpactTimer = 0.9f;
        }
        for (int i = 0; i < PartCount; i++)
            PartHp[i] = MathF.Min(100f, PartHp[i] + 0.8f * dt);

        // ---- L/R weapon swap; right arm alternates cannon <-> missile box
        SwapFlash = MathF.Max(0f, SwapFlash - dt);
        if (t >= _nextSwap)
        {
            _nextSwap += 12f;
            ActiveSide = ActiveSide == WeaponSide.Left ? WeaponSide.Right : WeaponSide.Left;
            if (ActiveSide == WeaponSide.Right)
                RightWeapon = RightWeapon == RightArm.Cannon ? RightArm.Missile : RightArm.Cannon;
            SwapFlash = 1.6f;
            _cannonCd = MathF.Max(_cannonCd, 1.2f);
        }

        // ---- SMG (left arm)
        if (SmgReload > 0f)
        {
            SmgFiring = false;
            SmgReload -= dt;
            if (SmgReload <= 0f)
            {
                _smgRounds = SmgMagSize;
                SmgMags = SmgMags > 1 ? SmgMags - 1 : 8;
            }
        }
        else if (ActiveSide == WeaponSide.Left)
        {
            float ph = t % 4f;
            SmgFiring = ph >= 2.8f && _smgRounds > 0f;
            if (SmgFiring) _smgRounds = MathF.Max(0f, _smgRounds - 28f * dt);
            if (_smgRounds <= 0f)
            {
                SmgReload = SmgReloadTime;
                SmgFiring = false;
            }
        }
        else
        {
            SmgFiring = false;
        }

        // ---- long range cannon (right arm, option A)
        CannonFlash = MathF.Max(0f, CannonFlash - dt);
        if (CannonRackReload > 0f)
        {
            CannonRackReload -= dt;
            if (CannonRackReload <= 0f) CannonShells = CannonMax;
        }
        else if (ActiveSide == WeaponSide.Right && RightWeapon == RightArm.Cannon)
        {
            _cannonCd -= dt;
            if (_cannonCd <= 0f && CannonShells > 0)
            {
                CannonShells--;
                CannonFlash = 0.5f;
                _cannonCd = CannonInterval;
                if (CannonShells == 0) CannonRackReload = CannonRackTime;
            }
        }

        // ---- missile box (right arm, option B): volleys of two
        MslFlash = MathF.Max(0f, MslFlash - dt);
        if (MslReload > 0f)
        {
            MslReload -= dt;
            if (MslReload <= 0f) MslCount = MslMax;
        }
        else if (ActiveSide == WeaponSide.Right && RightWeapon == RightArm.Missile)
        {
            _mslCd -= dt;
            if (_mslCd <= 0f)
            {
                int n = Math.Min(2, MslCount);
                if (n > 0)
                {
                    MslCount -= n;
                    MslFlash = 1.2f;
                    _mslCd = 7f;
                    if (MslCount == 0) MslReload = MslReloadTime;
                }
            }
        }

        // ---- comm traffic
        if (t >= _nextCh) { _nextCh += 13f; ActiveChannel = (ActiveChannel + 1) % 3; }
        if (t >= _nextKey) { _nextKey += 20f; SecureKey = $"KEY-{_rng.Next(0x10000):X4}"; }
        if (TxTimer > 0f)
        {
            TxTimer -= dt;
        }
        else if (t >= _nextTx)
        {
            _nextTx = t + 6f + 3f * (float)_rng.NextDouble();
            TxTimer = 1.8f + 1.2f * (float)_rng.NextDouble();
            int pick = _rng.Next(_squad.Count + 1);
            TxFrom = pick == _squad.Count ? "HQ" : _squad[pick].Code;
        }

        // ---- artillery warning
        if (IncomingTimer > 0f) IncomingTimer -= dt;
        if (t >= _nextIncoming) { _nextIncoming += 33f; IncomingTimer = 3f; }

        // ---- radar + contacts
        RadarSweepDeg = (RadarSweepDeg + 90f * dt) % 360f;
        foreach (var c in _contacts)
        {
            c.BearingDeg = Wrap360(c.BearingDeg + c.BearingDrift * dt);
            c.RangeM += c.RangeRate * dt;
            if (c.RangeM < 180f) { c.RangeM = 180f; c.RangeRate = MathF.Abs(c.RangeRate); }
            if (c.RangeM > 2600f) { c.RangeM = 2600f; c.RangeRate = -MathF.Abs(c.RangeRate); }
        }

        GroundContact? best = null;
        foreach (var c in _contacts)
            if (best == null || c.RangeM < best.RangeM)
                best = c;
        if (best != LockedTarget) { LockedTarget = best; _lockTime = 0f; }
        else _lockTime += dt;
        LockSteady = LockedTarget != null && _lockTime > 1.2f;
    }

    private static float MoveTo(float cur, float target, float maxDelta)
    {
        float d = target - cur;
        return MathF.Abs(d) <= maxDelta ? target : cur + MathF.Sign(d) * maxDelta;
    }

    public static float Wrap360(float a)
    {
        a %= 360f;
        return a < 0f ? a + 360f : a;
    }

    public static float AngDiff(float a, float b)
    {
        float d = (a - b) % 360f;
        if (d > 180f) d -= 360f;
        if (d < -180f) d += 360f;
        return d;
    }
}
