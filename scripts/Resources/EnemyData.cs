using Godot;

/// <summary>Tipo de inimigo (comum, elite ou boss). Uma única cena lê estes dados no spawn.</summary>
[GlobalClass]
public partial class EnemyData : Resource
{
    [Export] public string EnemyName { get; set; } = "Capanga";
    [Export] public Texture2D Sprite { get; set; }
    [Export] public Color Color { get; set; } = Colors.Red;
    [Export] public float Scale { get; set; } = 0.8f;
    [Export] public float Mass { get; set; } = 1f;

    [ExportGroup("Combate")]
    [Export] public float MaxHp { get; set; } = 20f;
    [Export] public float ContactDamage { get; set; } = 8f;
    [Export] public float ContactCooldown { get; set; } = 0.6f;
    [Export] public float MoveSpeed { get; set; } = 2.5f;
    /// <summary>Se true, o HP não escala com o tempo de partida.</summary>
    [Export] public bool FixedHp { get; set; }

    [ExportGroup("Recompensa")]
    [Export] public int Points { get; set; } = 10;
    [Export] public bool GuaranteedChip { get; set; }
    [Export(PropertyHint.Range, "0,1")] public float ChipDropChance { get; set; } = 0.005f;
    [Export(PropertyHint.Range, "0,1")] public float HealDropChance { get; set; } = 0.03f;
    [Export(PropertyHint.Range, "0,1")] public float PointsDropChance { get; set; } = 0.05f;

    [ExportGroup("Boss")]
    [Export] public bool IsBoss { get; set; }
}
