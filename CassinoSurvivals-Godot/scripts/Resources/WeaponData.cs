using Godot;

/// <summary>Definição estática de uma arma. Os valores por nível são calculados aqui; o estado de runtime fica em WeaponInstance.</summary>
[GlobalClass]
public partial class WeaponData : Resource
{
    [Export] public string WeaponName { get; set; } = "Arma";
    [Export] public Color ProjectileColor { get; set; } = Colors.White;

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

    [ExportGroup("Projétil")]
    [Export] public float ProjectileSpeed { get; set; } = 15f;
    [Export] public float ProjectileLifetime { get; set; } = 1.2f;
    [Export] public float ProjectileScale { get; set; } = 0.3f;

    public float Damage(int level) => BaseDamage + DamagePerLevel * (level - 1);
    public int Magazine(int level) => BaseMagazine + Mathf.FloorToInt(MagazinePerLevel * (level - 1));
    public float ReloadTime(int level) =>
        Mathf.Max(MinReloadTime, BaseReloadTime * Mathf.Pow(ReloadMultiplierPerLevel, level - 1));
}
