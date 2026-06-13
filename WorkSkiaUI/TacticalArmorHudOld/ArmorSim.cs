namespace TacticalArmorHud;

public enum Iff { Enemy, Friend, Unknown }
public enum WeaponKind { Autocannon, Missile, Blade }

public sealed class Weapon
{
    public required string Name;     // Japanese display name
    public required string Model;    // RG-76 etc.
    public required WeaponKind Kind;
    public int Ammo;
    public int Max;
    public bool Melee => Kind == WeaponKind.Blade;
}

/// <summary>
/// One arm's weapon loadout: its own copy of all 3 weapons (autocannon /
/// missile / blade) with independent ammo, plus which one is selected and the
/// per-weapon fire/reload state.
/// </summary>
public sealed class ArmWeapons
{
    public required Weapon[] W;
    public int Sel;
    public Weapon Cur => W[Sel];

    public bool AutoFiring;
    public float BladeSwing;             // 1 -> 0 strike flash
    public float AutoAcc, AutoReload, MslCd = 2.5f, MslReload, BladeCd = 3.5f;

    public bool Reloading =>
        (Cur.Kind == WeaponKind.Autocannon && AutoReload > 0f) ||
        (Cur.Kind == WeaponKind.Missile && MslReload > 0f);

    public static ArmWeapons Create(bool blade) => new()
    {
        W = new[]
        {
            new Weapon { Name = "7.62mm 突撃機関砲", Model = "RG-76",    Kind = WeaponKind.Autocannon, Ammo = 2000, Max = 2000 },
            new Weapon { Name = "4連装ミサイル",     Model = "SRM-04",   Kind = WeaponKind.Missile,    Ammo = 4,    Max = 4 },
            new Weapon { Name = "高速振動短刀",      Model = "HF-Blade", Kind = WeaponKind.Blade },
        },
        Sel = blade ? 2 : 0,
    };

    public void Tick(float t, float dt, bool active, ArmorSim sim)
    {
        AutoFiring = false;
        BladeSwing = MathF.Max(0f, BladeSwing - dt * 1.6f);
        if (AutoReload > 0f) { AutoReload -= dt; if (AutoReload <= 0f) W[0].Ammo = W[0].Max; }
        if (MslReload > 0f) { MslReload -= dt; if (MslReload <= 0f) W[1].Ammo = W[1].Max; }
        if (!active) return;

        var w = Cur;
        if (w.Kind == WeaponKind.Autocannon && AutoReload <= 0f)
        {
            float ph = t % 3f;
            if (ph > 1.7f && w.Ammo > 0)
            {
                AutoFiring = true;
                AutoAcc += 620f * dt;
                while (AutoAcc >= 1f && w.Ammo > 0) { w.Ammo--; AutoAcc -= 1f; }
                if (w.Ammo <= 0) AutoReload = 4f;
            }
        }
        else if (w.Kind == WeaponKind.Missile && MslReload <= 0f)
        {
            MslCd -= dt;
            if (MslCd <= 0f && w.Ammo > 0)
            {
                int n = Math.Min(2, w.Ammo);
                w.Ammo -= n;
                sim.SignalFox();
                MslCd = 3.5f;
                if (w.Ammo <= 0) MslReload = 6f;
            }
        }
        else if (w.Kind == WeaponKind.Blade)
        {
            BladeCd -= dt;
            if (BladeCd <= 0f) { BladeSwing = 1f; BladeCd = 3.4f; }
        }
    }
}

public sealed class RadarContact
{
    public required string Code;
    public required Iff Iff;
    public float Bearing;     // deg from north
    public float Range;       // meters
    public float Drift;       // deg/s
    public float RangeRate;   // m/s, negative = closing
}

public sealed class MapMarker
{
    public required string Code;
    public required string Sub;       // second label line ("" if none)
    public required Iff Iff;
    public float X, Y;                // base position normalized 0..1 in map
    public float Ax, Ay, Fx, Fy, Px, Py;
    public float MX(float t) => X + Ax * MathF.Sin(t * Fx + Px);
    public float MY(float t) => Y + Ay * MathF.Sin(t * Fy + Py);
}

/// <summary>
/// Self-running tactical-armor (mecha) cockpit simulation tuned to match the
/// reference screen: low-speed walker, dual altitude tapes, 3 selectable
/// weapons (120mm cannon / 36mm autocannon / CQC blade), comms and a tactical
/// map with friendly unit codes.
/// </summary>
public sealed class ArmorSim
{
    public float Time { get; private set; }

    public float HeadingDeg { get; private set; } = 150f;
    public float PitchDeg { get; private set; }
    public float RollDeg { get; private set; }
    public float SpeedKmh { get; private set; } = 87f;
    public float AltM { get; private set; } = 3710f;   // displayed /10 on the tapes
    public float ClimbRel { get; private set; }        // -10..+10 fine indicator

    // each arm carries its own copy of all 3 weapons and its own selection
    public ArmWeapons Left { get; } = ArmWeapons.Create(blade: true);
    public ArmWeapons Right { get; } = ArmWeapons.Create(blade: false);
    public int ActiveArm { get; private set; } = 1;   // 0 = left, 1 = right (currently firing arm)
    public float FoxTimer { get; private set; }       // missile launch -> show FOX call-out
    public float SwapFlash { get; private set; }
    public void SignalFox() => FoxTimer = 2.0f;

    public float RadarSweepDeg { get; private set; }
    public IReadOnlyList<RadarContact> Contacts => _contacts;
    public IReadOnlyList<MapMarker> Markers => _markers;
    public RadarContact? Locked { get; private set; }
    public bool LockSteady { get; private set; }

    public bool CommTx { get; private set; }
    public string CommFrom { get; private set; } = "FELT-02";

    public float WarnTimer { get; private set; }

    // --- airframe damage, drawn as individual part blocks (head/body/arms/legs)
    public static readonly string[] PartTags = { "HEAD", "BODY", "R.ARM", "L.ARM", "R.LEG", "L.LEG", "R.JU", "L.JU" };
    public float[] PartHp { get; } = { 100f, 100f, 100f, 100f, 100f, 100f, 100f, 100f };
    public int ImpactPart { get; private set; } = -1;
    public float ImpactTimer { get; private set; }
    public float Integrity
    {
        get { float sum = 0f; foreach (var h in PartHp) sum += h; return sum / PartHp.Length; }
    }

    private readonly List<RadarContact> _contacts = new();
    private readonly List<MapMarker> _markers = new();
    private readonly Random _rng = new();

    private float _selTimer = 5f;
    private float _lockTime;
    private float _commTimer = 4f;
    private float _nextWarn = 36f;
    private float _nextHit = 9f;

    public ArmorSim()
    {
        _contacts.Add(new RadarContact { Code = "K3", Iff = Iff.Enemy,   Bearing = 32f,  Range = 1400f, Drift = 0.5f,  RangeRate = -8f });
        _contacts.Add(new RadarContact { Code = "K5", Iff = Iff.Enemy,   Bearing = 78f,  Range = 2200f, Drift = -0.4f, RangeRate = -12f });
        _contacts.Add(new RadarContact { Code = "K7", Iff = Iff.Unknown, Bearing = 325f, Range = 2600f, Drift = 0.7f,  RangeRate = 4f });
        _contacts.Add(new RadarContact { Code = "F2", Iff = Iff.Friend,  Bearing = 205f, Range = 900f,  Drift = -0.3f, RangeRate = 6f });
        _contacts.Add(new RadarContact { Code = "F3", Iff = Iff.Friend,  Bearing = 165f, Range = 1300f, Drift = 0.25f, RangeRate = -3f });

        _markers.Add(new MapMarker { Code = "FELT-02", Sub = "",    Iff = Iff.Friend,  X = 0.54f, Y = 0.20f, Ax = 0.03f, Ay = 0.02f, Fx = 0.30f, Fy = 0.22f, Px = 0.4f, Py = 1.7f });
        _markers.Add(new MapMarker { Code = "UNKNOWN", Sub = "K3",  Iff = Iff.Enemy,   X = 0.33f, Y = 0.46f, Ax = 0.02f, Ay = 0.03f, Fx = 0.24f, Fy = 0.31f, Px = 2.1f, Py = 0.3f });
        _markers.Add(new MapMarker { Code = "FELT-03", Sub = "",    Iff = Iff.Friend,  X = 0.62f, Y = 0.66f, Ax = 0.03f, Ay = 0.02f, Fx = 0.27f, Fy = 0.19f, Px = 4.0f, Py = 2.5f });
    }

    public void Update(float dt)
    {
        Time += dt;
        float t = Time;

        HeadingDeg = Wrap360(150f + 14f * MathF.Sin(t * 0.05f) + 4f * MathF.Sin(t * 0.19f));
        PitchDeg = 3f * MathF.Sin(t * 0.3f) + 1.2f * MathF.Sin(t * 0.7f);
        RollDeg = 5f * MathF.Sin(t * 0.4f) + 2f * MathF.Sin(t * 0.9f);
        SpeedKmh = MathF.Max(0f, 92f + 28f * MathF.Sin(t * 0.13f) + 6f * MathF.Sin(t * 0.5f));
        AltM = 3710f + 70f * MathF.Sin(t * 0.08f) + 18f * MathF.Sin(t * 0.31f);
        ClimbRel = Math.Clamp(6f * MathF.Sin(t * 0.25f) + 3f * MathF.Sin(t * 0.6f), -10f, 10f);

        RadarSweepDeg = (RadarSweepDeg + 80f * dt) % 360f;

        // arm selection / active-arm toggle (each arm can select all 3 weapons)
        SwapFlash = MathF.Max(0f, SwapFlash - dt);
        FoxTimer = MathF.Max(0f, FoxTimer - dt);
        _selTimer -= dt;
        if (_selTimer <= 0f)
        {
            _selTimer = 5f;
            ActiveArm = 1 - ActiveArm;
            var a = ActiveArm == 0 ? Left : Right;
            a.Sel = (a.Sel + 1) % a.W.Length;   // advance the newly-active arm's selection
            SwapFlash = 1.4f;
        }

        Left.Tick(t, dt, ActiveArm == 0, this);
        Right.Tick(t, dt, ActiveArm == 1, this);

        // contacts
        foreach (var c in _contacts)
        {
            c.Bearing = Wrap360(c.Bearing + c.Drift * dt);
            c.Range += c.RangeRate * dt;
            if (c.Range < 250f) { c.Range = 250f; c.RangeRate = MathF.Abs(c.RangeRate); }
            if (c.Range > 2900f) { c.Range = 2900f; c.RangeRate = -MathF.Abs(c.RangeRate); }
        }

        RadarContact? best = null;
        foreach (var c in _contacts)
            if (c.Iff != Iff.Friend && (best == null || c.Range < best.Range))
                best = c;
        if (best != Locked) { Locked = best; _lockTime = 0f; }
        else _lockTime += dt;
        LockSteady = Locked != null && _lockTime > 1.2f;

        // comms — toggle TX bursts
        _commTimer -= dt;
        if (_commTimer <= 0f)
        {
            CommTx = !CommTx;
            _commTimer = CommTx ? 1.6f + 1.2f * (float)_rng.NextDouble() : 3f + 2f * (float)_rng.NextDouble();
            if (CommTx) CommFrom = _rng.Next(3) switch { 0 => "FELT-02", 1 => "FELT-03", _ => "HQ" };
        }

        if (WarnTimer > 0f) WarnTimer -= dt;
        if (t >= _nextWarn) { _nextWarn += 36f; WarnTimer = 3f; }

        // per-part damage + slow auto-repair
        ImpactTimer = MathF.Max(0f, ImpactTimer - dt);
        if (t >= _nextHit)
        {
            _nextHit += 11f;
            ImpactPart = _rng.Next(PartHp.Length);
            PartHp[ImpactPart] = MathF.Max(6f, PartHp[ImpactPart] - (14f + 20f * (float)_rng.NextDouble()));
            ImpactTimer = 0.9f;
        }
        for (int i = 0; i < PartHp.Length; i++)
            PartHp[i] = MathF.Min(100f, PartHp[i] + 0.7f * dt);
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
