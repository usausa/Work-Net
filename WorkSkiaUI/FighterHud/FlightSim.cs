namespace FighterHud;

public enum Iff
{
    Hostile,
    Friendly,
    Unknown,
}

public sealed class Contact
{
    public required string Name { get; init; }
    public required Iff Iff { get; init; }

    public float BearingDeg;   // world bearing as seen from own ship
    public float RangeNm;
    public float AltitudeFt;
    public float SpeedKt;
    public float BearingDrift; // deg/s
    public float RangeRate;    // nm/s (negative = closing)

    public float ClosureKt => -RangeRate * 3600f;

    public float ElevationDeg(float ownAltFt) =>
        (float)(Math.Atan2(AltitudeFt - ownAltFt, RangeNm * 6076.0) * 180.0 / Math.PI);
}

/// <summary>
/// Fake flight dynamics: every value is driven by layered sine waves so the
/// HUD looks alive without any user input.
/// </summary>
public sealed class FlightSim
{
    public float Time { get; private set; }

    public float HeadingDeg { get; private set; } = 47f;
    public float PitchDeg { get; private set; }
    public float RollDeg { get; private set; }
    public float SpeedKt { get; private set; } = 470f;
    public float Mach { get; private set; }
    public float AltitudeFt { get; private set; } = 11800f;
    public float ClimbFpm { get; private set; }
    public float GForce { get; private set; } = 1f;
    public float AoaDeg { get; private set; }
    public float FuelLbs { get; private set; } = 9340f;
    public float ThrottlePct => 62f + 30f * MathF.Sin(Time * 0.13f);

    public const int GunAmmoMax = 510;
    public const int MissilesMax = 6;
    public int GunAmmo => (int)_gunAmmo;
    public bool GunFiring { get; private set; }
    public int Missiles { get; private set; } = MissilesMax;
    public float Fox2Timer { get; private set; }
    public float WarnTimer { get; private set; }

    public float RadarSweepDeg { get; private set; }
    public IReadOnlyList<Contact> Contacts => _contacts;
    public Contact? LockedTarget { get; private set; }
    public bool LockSteady { get; private set; }

    private readonly List<Contact> _contacts = new();
    private float _gunAmmo = GunAmmoMax;
    private float _lockTime;
    private float _nextMissile = 16f;
    private float _nextWarn = 31f;
    private float _rearmAt = -1f;

    public FlightSim()
    {
        _contacts.Add(new Contact { Name = "BND-01", Iff = Iff.Hostile,  BearingDeg = 38f,  RangeNm = 9.5f, AltitudeFt = 9800f,  SpeedKt = 540f, BearingDrift = 0.55f,  RangeRate = -0.020f });
        _contacts.Add(new Contact { Name = "BND-02", Iff = Iff.Hostile,  BearingDeg = 71f,  RangeNm = 21f,  AltitudeFt = 14600f, SpeedKt = 480f, BearingDrift = -0.85f, RangeRate = -0.035f });
        _contacts.Add(new Contact { Name = "BND-03", Iff = Iff.Hostile,  BearingDeg = 331f, RangeNm = 30f,  AltitudeFt = 7600f,  SpeedKt = 600f, BearingDrift = 1.20f,  RangeRate = -0.012f });
        _contacts.Add(new Contact { Name = "ALY-01", Iff = Iff.Friendly, BearingDeg = 198f, RangeNm = 11f,  AltitudeFt = 12500f, SpeedKt = 450f, BearingDrift = -0.40f, RangeRate = 0.008f });
        _contacts.Add(new Contact { Name = "ALY-02", Iff = Iff.Friendly, BearingDeg = 152f, RangeNm = 24f,  AltitudeFt = 16800f, SpeedKt = 430f, BearingDrift = 0.30f,  RangeRate = -0.006f });
        _contacts.Add(new Contact { Name = "UNK-01", Iff = Iff.Unknown,  BearingDeg = 265f, RangeNm = 35f,  AltitudeFt = 22000f, SpeedKt = 380f, BearingDrift = 0.65f,  RangeRate = -0.009f });
    }

    public void Update(float dt)
    {
        Time += dt;
        float t = Time;

        RollDeg = 16f * MathF.Sin(t * 0.23f) + 5f * MathF.Sin(t * 0.71f);
        PitchDeg = 5.5f * MathF.Sin(t * 0.19f) + 2.2f * MathF.Sin(t * 0.47f);
        HeadingDeg = Wrap360(HeadingDeg + 4.5f * MathF.Sin(t * 0.09f) * dt);

        SpeedKt = 470f + 55f * MathF.Sin(t * 0.13f) + 12f * MathF.Sin(t * 0.43f);
        Mach = SpeedKt / 645f;
        ClimbFpm = PitchDeg * 240f;
        AltitudeFt = Math.Clamp(AltitudeFt + ClimbFpm / 60f * dt, 6500f, 17500f);
        GForce = 1f + MathF.Abs(RollDeg) / 24f + MathF.Max(0f, PitchDeg) / 8f;
        AoaDeg = 3.6f + PitchDeg * 0.35f;
        FuelLbs = MathF.Max(0f, FuelLbs - 1.25f * dt);

        RadarSweepDeg = (RadarSweepDeg + 75f * dt) % 360f;

        // gun: a short burst near the end of every 9 s cycle, rearm when depleted
        float gunPhase = t % 9f;
        GunFiring = gunPhase >= 7.4f && _gunAmmo > 0f;
        if (GunFiring) _gunAmmo = MathF.Max(0f, _gunAmmo - 95f * dt);
        else if (_gunAmmo < 30f && gunPhase < 1f) _gunAmmo = GunAmmoMax;

        if (Fox2Timer > 0f) Fox2Timer -= dt;
        if (t >= _nextMissile)
        {
            _nextMissile += 16f;
            if (Missiles > 0)
            {
                Missiles--;
                Fox2Timer = 2.2f;
                if (Missiles == 0) _rearmAt = t + 9f;
            }
        }
        if (_rearmAt > 0f && t >= _rearmAt)
        {
            Missiles = MissilesMax;
            _rearmAt = -1f;
        }

        if (WarnTimer > 0f) WarnTimer -= dt;
        if (t >= _nextWarn)
        {
            _nextWarn += 31f;
            WarnTimer = 3.5f;
        }

        foreach (var c in _contacts)
        {
            c.BearingDeg = Wrap360(c.BearingDeg + c.BearingDrift * dt);
            c.RangeNm += c.RangeRate * dt;
            if (c.RangeNm < 2.5f) { c.RangeNm = 2.5f; c.RangeRate = MathF.Abs(c.RangeRate); }
            if (c.RangeNm > 38f) { c.RangeNm = 38f; c.RangeRate = -MathF.Abs(c.RangeRate); }
        }

        Contact? best = null;
        foreach (var c in _contacts)
            if (c.Iff == Iff.Hostile && (best == null || c.RangeNm < best.RangeNm))
                best = c;

        if (best != LockedTarget) { LockedTarget = best; _lockTime = 0f; }
        else _lockTime += dt;
        LockSteady = LockedTarget != null && _lockTime > 1.4f;
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
