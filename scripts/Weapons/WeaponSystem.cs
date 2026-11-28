using System.Collections.Generic;
using Godot;

/// <summary>
/// Armas do jogador. Dois modos de mira (AimSettings, alterna com T): automático, que mira e
/// atira no inimigo visível mais próximo dentro do alcance, ou manual, com mira no mouse e
/// gatilho no botão esquerdo. Teclas 1-9 escolhem a arma; scroll alterna. No sandbox, Q/E também.
/// </summary>
public partial class WeaponSystem : Node
{
    static readonly string[] SandboxWeaponPaths =
    {
        "res://resources/weapons/PistolaNeon.tres",
        "res://resources/weapons/RevolverHighRoller.tres",
        "res://resources/weapons/SMGViceCity.tres",
        "res://resources/weapons/EscopetaRoyalFlush.tres",
        "res://resources/weapons/LancaDados.tres",
    };

    const string ProjectileSheetPath = "res://assets/GunsAssetPack/Gun_Sprite_Sheet/gun_sprite_sheet.png";
    // Únicos recortes de bala usados (alpha > 0), em coordenadas de textura.
    static readonly Rect2 BulletRect = new Rect2(0f, 16f, 3f, 1f);
    static readonly Rect2 GoldBulletRect = new Rect2(32f, 16f, 3f, 1f);

    const string LightShotPath = "res://assets/GunsAssetPack/sounds/tiro_metralhadora.wav";
    const string HeavyShotPath = "res://assets/GunsAssetPack/sounds/tiro_pistola.wav";
    const int FirePlayerCount = 4;
    const float ReloadSoundInterval = 2f;

    readonly Dictionary<WeaponData, AtlasTexture> projectileTextures = new Dictionary<WeaponData, AtlasTexture>();
    readonly List<AudioStreamPlayer> firePlayers = new List<AudioStreamPlayer>(FirePlayerCount);
    int nextFirePlayer;

    [Export] public Godot.Collections.Array<WeaponData> StartingWeapons { get; set; }
    [Export] public PackedScene ProjectileScene { get; set; }

    public readonly List<WeaponInstance> Weapons = new List<WeaponInstance>(8);

    [Signal] public delegate void WeaponsChangedEventHandler();
    /// <summary>Emitido somente após um disparo validado (munição consumida e projéteis criados).</summary>
    [Signal] public delegate void WeaponFiredEventHandler();

    public int Selected { get; private set; }

    bool sandboxMode;
    OrbitingWeapon orbitingWeapon;

    /// <summary>Arma orbitante: única fonte de mira e ponto de saída do tiro.</summary>
    OrbitingWeapon GetOrbitingWeapon()
    {
        if (orbitingWeapon == null)
            orbitingWeapon = GetParent()?.GetNodeOrNull<OrbitingWeapon>("OrbitingWeapon");
        return orbitingWeapon;
    }

    /// <summary>Modo de teste: inventário completo, teclas 1-5/Q/E e tiro sem inimigo.</summary>
    public bool SandboxMode
    {
        get => sandboxMode;
        set
        {
            if (sandboxMode == value) return;
            sandboxMode = value;
            if (!sandboxMode) return;

            foreach (string path in SandboxWeaponPaths)
            {
                WeaponData data = GD.Load<WeaponData>(path);
                if (data != null && Find(data) == null) AddOrUpgrade(data, 1);
            }
        }
    }

    public override void _Ready()
    {
        // Pool fixo: tiros rápidos não cortam o som anterior e nenhum nó é vazado.
        for (int i = 0; i < FirePlayerCount; i++)
        {
            var player = new AudioStreamPlayer { Bus = AudioManager.SfxBus, VolumeDb = -6f };
            AddChild(player);
            firePlayers.Add(player);
        }

        if (StartingWeapons == null) return;
        foreach (var w in StartingWeapons) AddOrUpgrade(w, 1);
    }

    public WeaponInstance Find(WeaponData data)
    {
        for (int i = 0; i < Weapons.Count; i++)
            if (Weapons[i].Data == data) return Weapons[i];
        return null;
    }

    /// <summary>Se já tem a arma: +levels. Se não tem: desbloqueia no nível = levels.</summary>
    public void AddOrUpgrade(WeaponData data, int levels)
    {
        var w = Find(data);
        if (w == null) Weapons.Add(new WeaponInstance(data, levels));
        else w.AddLevels(levels);

        EmitSignal(SignalName.WeaponsChanged);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
        if (Weapons.Count == 0) return;

        if (e.IsActionPressed("toggle_aim"))
        {
            AimSettings.Auto = !AimSettings.Auto;
            EmitSignal(SignalName.WeaponsChanged);
            return;
        }

        if (e is InputEventKey key && key.Pressed && !key.Echo)
        {
            int index = key.Keycode >= Key.Key1 && key.Keycode <= Key.Key9 ? (int)(key.Keycode - Key.Key1) : -1;
            if (index >= 0)
            {
                if (index < Weapons.Count) Select(index);
                return;
            }
            if (sandboxMode && (key.Keycode == Key.Q || key.Keycode == Key.E))
            {
                Cycle(key.Keycode == Key.E ? 1 : -1);
                return;
            }
        }

        // Scroll para cima = arma anterior, para baixo = próxima.
        if (Weapons.Count <= 1) return;
        int step = e.IsActionPressed("weapon_next") ? 1 : e.IsActionPressed("weapon_prev") ? -1 : 0;
        if (step != 0) Cycle(step);
    }

    void Select(int index)
    {
        Selected = Mathf.Clamp(index, 0, Weapons.Count - 1);
        EmitSignal(SignalName.WeaponsChanged);
    }

    void Cycle(int step) => Select((Selected + step + Weapons.Count) % Weapons.Count);

    /// <summary>Recorte padrão por nome de arma quando o .tres não trouxe uma região válida.</summary>
    static Rect2 ResolveProjectileRegion(WeaponData d)
    {
        string name = d?.WeaponName ?? string.Empty;
        if (name.Contains("rev", System.StringComparison.OrdinalIgnoreCase)) return GoldBulletRect;
        // Escopeta usa a bala do Rifle (recorte 32,16,3,1) em leque.
        if (name.Contains("escopeta", System.StringComparison.OrdinalIgnoreCase)) return GoldBulletRect;
        if (name.Contains("rifle", System.StringComparison.OrdinalIgnoreCase) ||
            name.Contains("dados", System.StringComparison.OrdinalIgnoreCase)) return GoldBulletRect;
        return BulletRect; // Pistola e Metralhadora
    }

    /// <summary>Atlas da bala, cacheado por arma (os projéteis compartilham o mesmo resource).</summary>
    AtlasTexture GetProjectileTexture(WeaponData d)
    {
        if (projectileTextures.TryGetValue(d, out AtlasTexture cached)) return cached;

        Texture2D sheet = d.ProjectileSheet ?? GD.Load<Texture2D>(ProjectileSheetPath);
        Rect2 region = d.ProjectileRegion;
        if (sheet == null || region.Size.X <= 0f || region.Size.Y <= 0f ||
            region.Position.X < 0f || region.Position.Y < 0f ||
            region.Position.X + region.Size.X > sheet.GetWidth() ||
            region.Position.Y + region.Size.Y > sheet.GetHeight())
            region = ResolveProjectileRegion(d);

        var atlas = new AtlasTexture { Atlas = sheet, Region = region };
        projectileTextures[d] = atlas;
        return atlas;
    }

    /// <summary>Som padrão por nome de arma se o .tres vier sem FireSound.</summary>
    static AudioStream ResolveFireSound(WeaponData d)
    {
        string name = d?.WeaponName ?? string.Empty;
        // O clipe de metralhadora é curto/nitido (armas leves); o de pistola tem cauda
        // mais longa e serve para as armas encorpadas com pitch mais grave.
        if (name.Contains("metralhadora", System.StringComparison.OrdinalIgnoreCase) ||
            name.Contains("smg", System.StringComparison.OrdinalIgnoreCase) ||
            name.Contains("vice", System.StringComparison.OrdinalIgnoreCase) ||
            name.Contains("pistola", System.StringComparison.OrdinalIgnoreCase))
            return GD.Load<AudioStream>(LightShotPath);
        return GD.Load<AudioStream>(HeavyShotPath);
    }

    /// <summary>Pitch padrão por nome de arma se o .tres vier sem FirePitchScale.</summary>
    static float ResolveFirePitch(WeaponData d)
    {
        string name = d?.WeaponName ?? string.Empty;
        if (name.Contains("escopeta", System.StringComparison.OrdinalIgnoreCase)) return 0.82f;
        if (name.Contains("rifle", System.StringComparison.OrdinalIgnoreCase) ||
            name.Contains("dados", System.StringComparison.OrdinalIgnoreCase)) return 0.88f;
        if (name.Contains("rev", System.StringComparison.OrdinalIgnoreCase)) return 0.92f;
        if (name.Contains("metralhadora", System.StringComparison.OrdinalIgnoreCase) ||
            name.Contains("smg", System.StringComparison.OrdinalIgnoreCase)) return 1.06f;
        return 1.0f;
    }

    /// <summary>Próximo player livre do pool, com leve variação de pitch.</summary>
    void PlayFireSound(WeaponData d)
    {
        if (firePlayers.Count == 0) return;
        AudioStream stream = d.FireSound ?? ResolveFireSound(d);
        if (stream == null) return;

        AudioStreamPlayer player = null;
        for (int i = 0; i < firePlayers.Count; i++)
        {
            var candidate = firePlayers[(nextFirePlayer + i) % firePlayers.Count];
            if (candidate.Playing) continue;
            player = candidate;
            nextFirePlayer = (nextFirePlayer + i + 1) % firePlayers.Count;
            break;
        }
        if (player == null)
        {
            player = firePlayers[nextFirePlayer];
            nextFirePlayer = (nextFirePlayer + 1) % firePlayers.Count;
        }

        float pitch = d.FirePitchScale > 0f ? d.FirePitchScale : ResolveFirePitch(d);
        player.Stream = stream;
        player.PitchScale = Mathf.Clamp(pitch + (float)GD.RandRange(-0.04, 0.04), 0.05f, 4f);
        player.Play();
    }

    public override void _Process(double delta)
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
        if (Weapons.Count == 0) return;

        // Tick continua avançando o cooldown/recarga mesmo sem gatilho.
        var w = Weapons[Mathf.Clamp(Selected, 0, Weapons.Count - 1)];
        bool trigger = AimSettings.Auto ? TryGetAutoAim(out _) : Input.IsMouseButtonPressed(MouseButton.Left);
        float reloadBefore = ReloadElapsed(w);
        bool fired = w.Tick((float)delta, trigger);
        // Recarga longa: o som repete a cada intervalo completado enquanto ela durar.
        if (w.IsReloading && Mathf.FloorToInt(ReloadElapsed(w) / ReloadSoundInterval) > Mathf.FloorToInt(reloadBefore / ReloadSoundInterval))
            AudioManager.Instance?.PlayWeaponReload();
        if (!fired) return;

        // Disparo confirmado: munição consumida e projéteis instanciados.
        Fire(w);
        if (w.IsReloading) AudioManager.Instance?.PlayWeaponReload(); // o último tiro do pente inicia a recarga
        EmitSignal(SignalName.WeaponFired);
    }

    static float ReloadElapsed(WeaponInstance w) =>
        w.IsReloading ? w.ReloadProgress * w.Data.ReloadTime(w.Level) : 0f;

    Enemy autoTarget;
    ulong autoTargetFrame = ulong.MaxValue;

    /// <summary>
    /// Alvo da mira automática: inimigo mais próximo dentro do alcance da arma selecionada e sem
    /// obstáculo no caminho do tiro. Calculado uma vez por frame (arma e disparo consultam).
    /// </summary>
    public bool TryGetAutoAim(out Vector2 target)
    {
        ulong frame = Engine.GetProcessFrames();
        if (frame != autoTargetFrame)
        {
            autoTargetFrame = frame;
            autoTarget = FindAutoTarget();
        }
        target = autoTarget != null && IsInstanceValid(autoTarget) ? autoTarget.GlobalPosition : Vector2.Zero;
        return autoTarget != null && IsInstanceValid(autoTarget);
    }

    Enemy FindAutoTarget()
    {
        var spawner = EnemySpawner.Instance;
        if (spawner == null || Weapons.Count == 0) return null;
        var d = Weapons[Mathf.Clamp(Selected, 0, Weapons.Count - 1)].Data;
        float range = Mathf.Min(d.Range * Units.Px, d.ProjectileSpeed * d.ProjectileLifetime);
        Vector2 from = ((Node2D)GetParent()).GlobalPosition;
        var arena = Arena.Instance;

        Enemy best = null;
        float bestSq = range * range;
        foreach (var e in spawner.Active)
        {
            float sq = (e.GlobalPosition - from).LengthSquared();
            if (sq >= bestSq) continue;
            if (arena != null && !arena.ShotClear(from, e.GlobalPosition)) continue;
            bestSq = sq;
            best = e;
        }
        return best;
    }

    void Fire(WeaponInstance w)
    {
        if (ProjectileScene == null) return;

        var d = w.Data;
        var player = (Node2D)GetParent();
        var orbiting = GetOrbitingWeapon();

        // Posição e direção vêm exclusivamente da arma orbitante (já atualizadas no frame atual).
        Vector2 muzzle = orbiting != null ? orbiting.GetMuzzleGlobalPosition() : player.GlobalPosition;
        Vector2 baseDir = orbiting != null
            ? orbiting.GetAimDirection()
            : player.GetGlobalMousePosition() - player.GlobalPosition;
        if (baseDir.LengthSquared() < 1f) baseDir = Vector2.Right;
        baseDir = baseDir.Normalized();

        int n = Mathf.Max(1, d.ProjectilesPerShot);
        float damage = d.Damage(w.Level) * MetaProgress.DamageMultiplier; // Arsenal (Loja Externa)

        for (int i = 0; i < n; i++)
        {
            // Única variação permitida: o spread intencional da arma.
            float offset = n == 1 ? 0f : Mathf.DegToRad(d.SpreadAngle) * ((float)i / (n - 1) - 0.5f);
            var v = baseDir.Rotated(offset) * d.ProjectileSpeed; // ProjectileSpeed já é px/s
            var p = ProjectileScene.Instantiate<Projectile>();
            GetTree().CurrentScene.AddChild(p);
            p.Launch(muzzle, v, damage, d.ProjectileLifetime, GetProjectileTexture(d), d.ProjectileScale);
        }
        PlayFireSound(d);
    }
}
