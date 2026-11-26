using System;
using Godot;

/// <summary>
/// Arma que flutua ao redor do jogador. Em repouso usa o sprite individual de 32x16;
/// ao atirar troca momentaneamente pela linha correspondente de
/// firing_gun_sprite_sheet.png da MESMA arma e retorna ao sprite individual.
/// MuzzleFlash e ShellCasing ficam ocultos permanentemente.
/// </summary>
public partial class OrbitingWeapon : Node2D
{
    const string FireSheetPath = "res://assets/GunsAssetPack/Firing_Gun_Spritesheet/firing_gun_sprite_sheet.png";
    const string FallbackWeaponPath = "res://resources/weapons/PistolaNeon.tres";

    /// <summary>Distância da empunhadura ao jogador, em unidades de mundo.</summary>
    [Export] public float OrbitRadius { get; set; } = 0.55f;
    /// <summary>Duração da animação de tiro, em segundos.</summary>
    [Export] public float FireTime { get; set; } = 0.14f;

    [Export] public Sprite2D WeaponSprite { get; set; }
    // Preservam os NodePaths da cena, mas nunca são exibidos/atualizados.
    [Export] public Sprite2D FlashSprite { get; set; }
    [Export] public Sprite2D ShellSprite { get; set; }
    /// <summary>Spritesheet de disparo (células de 48x16).</summary>
    [Export] public Texture2D FireSheet { get; set; }

    const float FrameWidth = 48f;
    const float FrameHeight = 16f;

    /// <summary>Primeiro pixel opaco à esquerda: medido como x=0 em todos os sprites.</summary>
    const float GripX = 0f;
    /// <summary>Centraliza a altura de 16 px em torno da origem local.</summary>
    const float GripOffsetY = -8f;
    /// <summary>Avanço da empunhadura em pixels de mundo (não escalado).</summary>
    const float GripAdvance = 4f;

    const float KickPx = 7f;
    const float KickTime = 0.12f;

    // Cada linha da folha traz uma arma nos frames 0/1 e outra nos frames 2/3.
    // Pistol/Revolver usam 0/1 (são a mesma arma); as demais usam 2/3 (com clarão).
    static readonly int[] EarlyFrames = { 0, 1 };
    static readonly int[] LateFrames = { 2, 3 };

    WeaponSystem weaponSystem;
    AtlasTexture firingAtlas;
    Texture2D staticWeaponTexture;
    WeaponData currentData;
    int firingRow = 11;
    int[] firingFrames = EarlyFrames;
    float weaponScale = 1f;
    float muzzleTipX = 13f;
    float muzzleCenterY = 2.5f;
    Vector2 lastAimDirection = Vector2.Right;
    float fireTimer;
    float kickTimer;
    int fireFrame;

    public override void _Ready()
    {
        // A arma deve atualizar a mira antes do WeaponSystem (que é irmão anterior).
        ProcessPriority = -1;

        // O editor pode re-serializar a cena e remover os NodePaths exportados.
        WeaponSprite ??= GetNodeOrNull<Sprite2D>("Weapon");
        FlashSprite ??= GetNodeOrNull<Sprite2D>("MuzzleFlash");
        ShellSprite ??= GetNodeOrNull<Sprite2D>("ShellCasing");

        if (WeaponSprite != null)
        {
            // Static 32x16 e frames 48x16 compartilham a empunhadura em x=0.
            WeaponSprite.Centered = false;
            WeaponSprite.Offset = new Vector2(-GripX, GripOffsetY);
        }
        if (FlashSprite != null) FlashSprite.Visible = false;
        if (ShellSprite != null) ShellSprite.Visible = false;

        FireSheet ??= GD.Load<Texture2D>(FireSheetPath);
        if (FireSheet != null)
        {
            firingAtlas = new AtlasTexture
            {
                Atlas = FireSheet,
                Region = new Rect2(0f, 11f * FrameHeight, FrameWidth, FrameHeight),
            };
        }

        weaponSystem = GetParent().GetNodeOrNull<WeaponSystem>("WeaponSystem");
        if (weaponSystem == null) return;
        weaponSystem.WeaponsChanged += OnWeaponsChanged;
        weaponSystem.WeaponFired += OnFired;
        UpdateWeapon();
        // WeaponSystem é irmão: _Ready() dele pode rodar depois deste nó.
        CallDeferred(nameof(UpdateWeapon));
    }

    public override void _ExitTree()
    {
        if (weaponSystem == null) return;
        weaponSystem.WeaponsChanged -= OnWeaponsChanged;
        weaponSystem.WeaponFired -= OnFired;
    }

    void OnWeaponsChanged() => UpdateWeapon();

    /// <summary>
    /// Animação/recuo disparados apenas pelo WeaponSystem após confirmar o tiro
    /// (munição consumida, fora de recarga e cooldown pronto). Nunca pelo input direto.
    /// </summary>
    public void OnFired()
    {
        fireTimer = FireTime;
        kickTimer = KickTime;
        fireFrame = 0;
    }

    /// <summary>.tres de fallback quando o sprite da arma atual não pôde ser lido.</summary>
    static string ResolveFallbackPath(string name)
    {
        if (name.Contains("rev", StringComparison.OrdinalIgnoreCase))
            return "res://resources/weapons/RevolverHighRoller.tres";
        if (name.Contains("metralhadora", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("smg", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("vice", StringComparison.OrdinalIgnoreCase))
            return "res://resources/weapons/SMGViceCity.tres";
        if (name.Contains("escopeta", StringComparison.OrdinalIgnoreCase))
            return "res://resources/weapons/EscopetaRoyalFlush.tres";
        if (name.Contains("rifle", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("dados", StringComparison.OrdinalIgnoreCase))
            return "res://resources/weapons/LancaDados.tres";
        return FallbackWeaponPath; // Pistola
    }

    /// <summary>
    /// Escala e boca do cano medidas nos PNGs Black_and_White_Outline (alpha &gt; 0).
    /// Usadas se o editor re-serializar um .tres sem esses valores.
    /// </summary>
    static (float Scale, float TipX, float CenterY) ResolveFallbackStats(WeaponData data)
    {
        string name = data?.WeaponName ?? string.Empty;
        string path = data?.WeaponSprite?.ResourcePath ?? string.Empty;
        if (name.Contains("rev", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("Revolver.png", StringComparison.OrdinalIgnoreCase)) return (2.8f, 17f, 2.5f);
        if (name.Contains("metralhadora", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("smg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("MachineGun.png", StringComparison.OrdinalIgnoreCase)) return (3.0f, 29f, 3.5f);
        if (name.Contains("escopeta", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("Shotgun.png", StringComparison.OrdinalIgnoreCase)) return (3.1f, 28f, 2.2f);
        if (name.Contains("rifle", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("dados", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("Rifle.png", StringComparison.OrdinalIgnoreCase)) return (3.1f, 31f, 2.0f);
        return (2.7f, 13f, 2.5f); // Pistola
    }

    static bool UsesEarlyFrames(string name, string path)
    {
        return name.Contains("pistola", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("rev", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith("Pistol.png", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith("Revolver.png", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Linha e frames da folha que pertencem exatamente à arma equipada.</summary>
    static (int Row, int[] Frames) ResolveFiringRow(WeaponData data)
    {
        string name = data?.WeaponName ?? string.Empty;
        string path = data?.WeaponSprite?.ResourcePath ?? string.Empty;
        int row = data?.FiringRowIndex ?? -1;

        if (row >= 0 && row <= 12)
            return (row, UsesEarlyFrames(name, path) ? EarlyFrames : LateFrames);

        // Fallback caso o .tres seja re-serializado sem FiringRowIndex.
        // Bloco Black_and_White_Outline da folha: linhas 9..12.
        if (name.Contains("pistola", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("Pistol.png", StringComparison.OrdinalIgnoreCase)) return (11, EarlyFrames);
        if (name.Contains("rev", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("Revolver.png", StringComparison.OrdinalIgnoreCase)) return (12, EarlyFrames);
        if (name.Contains("metralhadora", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("smg", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("vice", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("MachineGun.png", StringComparison.OrdinalIgnoreCase)) return (10, LateFrames);
        if (name.Contains("escopeta", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("Shotgun.png", StringComparison.OrdinalIgnoreCase)) return (9, LateFrames);
        if (name.Contains("rifle", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("dados", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("Rifle.png", StringComparison.OrdinalIgnoreCase)) return (11, LateFrames);
        if (path.EndsWith("AssaultRifle1.png", StringComparison.OrdinalIgnoreCase)) return (9, EarlyFrames);
        if (path.EndsWith("AssaultRifle2.png", StringComparison.OrdinalIgnoreCase)) return (10, EarlyFrames);
        return (11, EarlyFrames);
    }

    void UpdateWeapon()
    {
        if (WeaponSprite == null) return;
        weaponSystem ??= GetParent().GetNodeOrNull<WeaponSystem>("WeaponSystem");
        if (weaponSystem == null) return;

        // Se StartingWeapons foi perdido na re-serialização, garante ao menos a pistola.
        if (weaponSystem.Weapons.Count == 0)
        {
            WeaponData fallback = GD.Load<WeaponData>(FallbackWeaponPath);
            if (fallback != null) weaponSystem.AddOrUpgrade(fallback, 1);
        }
        if (weaponSystem.Weapons.Count == 0) return;

        int selected = Mathf.Clamp(weaponSystem.Selected, 0, weaponSystem.Weapons.Count - 1);
        WeaponData data = weaponSystem.Weapons[selected].Data;
        Texture2D texture = data?.WeaponSprite;
        if (texture == null)
        {
            data = GD.Load<WeaponData>(ResolveFallbackPath(data?.WeaponName ?? string.Empty));
            texture = data?.WeaponSprite;
            if (texture == null) return;
        }

        WeaponSprite.Centered = false;
        WeaponSprite.Offset = new Vector2(-GripX, GripOffsetY);
        currentData = data;
        (float fbScale, float fbTipX, float fbCenterY) = ResolveFallbackStats(data);
        weaponScale = data.WeaponSpriteScale > 0f ? data.WeaponSpriteScale : fbScale;
        muzzleTipX = data.MuzzleOffsetPx > 0f ? data.MuzzleOffsetPx : fbTipX;
        muzzleCenterY = data.MuzzleOffsetY >= 0f ? data.MuzzleOffsetY : fbCenterY;
        (firingRow, firingFrames) = ResolveFiringRow(data);
        staticWeaponTexture = texture;

        fireTimer = 0f;
        kickTimer = 0f;
        fireFrame = 0;
        WeaponSprite.Texture = texture;
        WeaponSprite.Scale = Vector2.One * weaponScale;
        WeaponSprite.Position = new Vector2(GripAdvance, 0f);

        if (FlashSprite != null) FlashSprite.Visible = false;
        if (ShellSprite != null) ShellSprite.Visible = false;
    }

    /// <summary>
    /// Atualiza posição/rotação a partir do mouse em relação ao centro do jogador.
    /// Idempotente: também é chamada pelas consultas de mira/boca para que o disparo
    /// nunca use a transformação do frame anterior.
    /// </summary>
    public void UpdateOrbit()
    {
        if (WeaponSprite == null) return;

        Vector2 origin = ((Node2D)GetParent()).GlobalPosition;
        Vector2 aim = GetGlobalMousePosition() - origin;
        // Mouse exatamente sobre o pivô: preserva a última direção válida.
        if (aim.LengthSquared() > 1f) lastAimDirection = aim.Normalized();

        Position = lastAimDirection * OrbitRadius * Units.Px;
        GlobalRotation = lastAimDirection.Angle();
        // Sprites apontam para +X; FlipV espelha a imagem quando a mira vai à esquerda.
        WeaponSprite.FlipV = Mathf.Cos(GlobalRotation) < 0f;
    }

    /// <summary>Direção exata da mira, derivada da mesma rotação aplicada à arma.</summary>
    public Vector2 GetAimDirection()
    {
        UpdateOrbit();
        return Vector2.Right.Rotated(GlobalRotation).Normalized();
    }

    /// <summary>
    /// Boca do cano em coordenadas globais. Espelha o deslocamento vertical quando
    /// a arma mira à esquerda, mantendo a saída acima do corpo em 360°.
    /// Fallback = centro do jogador.
    /// </summary>
    public Vector2 GetMuzzleGlobalPosition()
    {
        Node2D parent = GetParent() as Node2D;
        if (parent == null) return GlobalPosition;
        if (currentData == null || WeaponSprite == null) return parent.GlobalPosition;

        UpdateOrbit();
        float scale = Mathf.Abs(WeaponSprite.Scale.X);
        if (scale <= 0f) scale = weaponScale;
        float tipLocalX = WeaponSprite.Position.X + muzzleTipX * scale;
        float relY = muzzleCenterY + GripOffsetY; // -8 px centralizam o sprite de 16 px
        bool isLeft = Mathf.Cos(GlobalRotation) < 0f;
        float tipLocalY = (isLeft ? -relY : relY) * scale;
        return ToGlobal(new Vector2(tipLocalX, tipLocalY));
    }

    public override void _Process(double delta)
    {
        if (WeaponSprite == null) return;

        UpdateOrbit();

        float dt = (float)delta;
        float kickX = 0f;
        if (kickTimer > 0f)
        {
            kickTimer = Mathf.Max(0f, kickTimer - dt);
            float p = 1f - kickTimer / KickTime;
            kickX = -KickPx * (1f - p * p);
        }
        WeaponSprite.Position = new Vector2(GripAdvance + kickX, 0f);

        if (fireTimer > 0f && firingAtlas != null && staticWeaponTexture != null)
        {
            int frame = firingFrames[fireFrame % firingFrames.Length];
            firingAtlas.Region = new Rect2(frame * FrameWidth, firingRow * FrameHeight,
                                           FrameWidth, FrameHeight);
            WeaponSprite.Texture = firingAtlas;
            fireFrame++;
            fireTimer -= dt;
            if (fireTimer <= 0f)
            {
                fireTimer = 0f;
                WeaponSprite.Texture = staticWeaponTexture;
            }
        }
        else if (staticWeaponTexture != null && WeaponSprite.Texture != staticWeaponTexture)
        {
            WeaponSprite.Texture = staticWeaponTexture;
        }
    }
}
