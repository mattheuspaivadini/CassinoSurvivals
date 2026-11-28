using Godot;

/// <summary>Definição estática de uma arma. Os valores por nível são calculados aqui; o estado de runtime fica em WeaponInstance.</summary>
[GlobalClass]
public partial class WeaponData : Resource
{
    [Export] public string WeaponName { get; set; } = "Arma";
    [Export] public Color ProjectileColor { get; set; } = Colors.White;

    [ExportGroup("Visual")]
    /// <summary>Sprite da arma orbitante; o cano aponta para +X.</summary>
    [Export] public Texture2D WeaponSprite { get; set; }
    [Export] public float WeaponSpriteScale { get; set; } = 1f;
    /// <summary>X absoluto da ponta do cano em pixels de textura (coluna final do sprite).</summary>
    [Export] public float MuzzleOffsetPx { get; set; } = 11f;
    /// <summary>Y absoluto do centro do cano no sprite individual, em pixels de textura.</summary>
    [Export] public float MuzzleOffsetY { get; set; } = 1.5f;
    /// <summary>Linha de 48x16 da folha com os frames de tiro DESTA arma; -1 = deduzir pelo nome.</summary>
    [Export] public int FiringRowIndex { get; set; } = -1;

    [ExportGroup("Dano")]
    [Export] public float BaseDamage { get; set; } = 10f;
    [Export] public float DamagePerLevel { get; set; } = 4f;

    [ExportGroup("Pente")]
    [Export] public int BaseMagazine { get; set; } = 8;
    /// <summary>Balas extras por nível (fração acumula: 0.5 = +1 a cada 2 níveis).</summary>
    [Export] public float MagazinePerLevel { get; set; } = 1f;

    [ExportGroup("Recarga")]
    [Export] public float BaseReloadTime { get; set; } = 1.5f;
    /// <summary>Multiplicador aplicado a cada nível (0.9 = 10% mais rápido por nível).</summary>
    [Export] public float ReloadMultiplierPerLevel { get; set; } = 0.92f;
    [Export] public float MinReloadTime { get; set; } = 0.25f;

    [ExportGroup("Disparo")]
    /// <summary>Segundos entre tiros enquanto há balas.</summary>
    [Export] public float FireInterval { get; set; } = 0.25f;
    [Export] public int ProjectilesPerShot { get; set; } = 1;
    /// <summary>Abertura total do leque em graus (só com mais de 1 projétil).</summary>
    [Export] public float SpreadAngle { get; set; } = 0f;
    /// <summary>Alcance em unidades de mundo (1 unidade = Units.Px pixels).</summary>
    [Export] public float Range { get; set; } = 10f;

    public const float DamageBonusPerLevel = 0.15f;
    public const float MagazineBonusPerLevel = 0.10f;
    public const float ReloadReductionPerLevel = 0.05f;
    public const int FireRateMilestoneLevel = 5;
    public const float FireRateMilestoneBonus = 0.15f;
    public const int GoldenLevel = 10;
    public const float GoldenDamageBonus = 0.25f;

    [ExportGroup("Projétil")]
    /// <summary>Velocidade do projétil em pixels por segundo (já em coordenadas do Godot).</summary>
    [Export] public float ProjectileSpeed { get; set; } = 1000f;
    [Export] public float ProjectileLifetime { get; set; } = 1.2f;
    /// <summary>Inimigos atravessados além do primeiro (0 = para no 1º alvo; Sniper = 2, atinge até 3).</summary>
    [Export] public int PierceCount { get; set; } = 0;
    /// <summary>Folha de balas (gun_sprite_sheet.png) e o recorte exato da bala desta arma.</summary>
    [Export] public Texture2D ProjectileSheet { get; set; }
    [Export] public Rect2 ProjectileRegion { get; set; }
    /// <summary>Escala única aplicada ao recorte da bala (pixels de textura × scale). Padrão: 3×1 → 18×6 px.</summary>
    [Export] public float ProjectileScale { get; set; } = 6.0f;

    [ExportGroup("Áudio")]
    /// <summary>Som de disparo desta arma; se vazio, o WeaponSystem resolve pelo nome.</summary>
    [Export] public AudioStream FireSound { get; set; }
    /// <summary>Pitch base do disparo (variação aleatória de ±0,04 aplicada na reprodução).</summary>
    [Export] public float FirePitchScale { get; set; } = 1.0f;

    /// <summary>Bônus aditivo de dano por nível (PDF, seção 4): +15%/nv + 25% no Nv 10 (Arma Dourada).</summary>
    public float LevelDamageBonus(int level)
    {
        int lv = Mathf.Clamp(level, 1, WeaponInstance.MaxLevel);
        return DamageBonusPerLevel * (lv - 1) + (lv >= GoldenLevel ? GoldenDamageBonus : 0f);
    }

    public float Damage(int level) => BaseDamage * (1f + LevelDamageBonus(level));

    /// <summary>
    /// Fórmula aditiva de dano (PDF, seção 4):
    /// DanoBase × (1 + BônusNível + Arsenal + MelhoriasDano + BebidaDano) × Crítico.
    /// </summary>
    public float ComputeShotDamage(int level, float arsenalBonus, float runDamageBonus, float critMultiplier = 1f) =>
        BaseDamage * (1f + LevelDamageBonus(level) + arsenalBonus + runDamageBonus) * critMultiplier;

    /// <summary>Pente por nível (PDF, seção 4): ceil(PenteBase × (1 + 0,10 × (Nivel - 1))).</summary>
    public int Magazine(int level)
    {
        int lv = Mathf.Clamp(level, 1, WeaponInstance.MaxLevel);
        return Mathf.Max(1, Mathf.CeilToInt(BaseMagazine * (1f + MagazineBonusPerLevel * (lv - 1)) - 0.0001f));
    }

    /// <summary>Recarga por nível (PDF, seção 4): -5% por nível (teto -45% no Nv 10).</summary>
    public float ReloadTime(int level)
    {
        int lv = Mathf.Clamp(level, 1, WeaponInstance.MaxLevel);
        return Mathf.Max(MinReloadTime, BaseReloadTime * (1f - ReloadReductionPerLevel * (lv - 1)));
    }

    /// <summary>Marco Nv 5 (PDF, seção 4): +15% de cadência de tiro permanente.</summary>
    public float FireRateBonus(int level) => level >= FireRateMilestoneLevel ? FireRateMilestoneBonus : 0f;

    public float FireIntervalAt(int level) => FireInterval / (1f + FireRateBonus(level));
}
