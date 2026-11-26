using Godot;

/// <summary>Decide e cria drops quando um inimigo morre.</summary>
public partial class DropManager : Node
{
    public static DropManager Instance { get; private set; }

    [Export] public PackedScene DropScene { get; set; }

    [ExportGroup("Ficha de Cassino")]
    [Export] public Texture2D ChipSprite { get; set; }
    [Export] public Color ChipColor { get; set; } = new Color(1f, 0.85f, 0.1f);
    [Export] public float ChipScale { get; set; } = 0.6f;

    [ExportGroup("Cura")]
    [Export] public Texture2D HealSprite { get; set; }
    [Export] public Color HealColor { get; set; } = new Color(0.2f, 1f, 0.4f);
    [Export] public float HealScale { get; set; } = 0.45f;
    [Export] public float HealAmount { get; set; } = 20f;

    [ExportGroup("Bônus de Pontos")]
    [Export] public Texture2D PointsSprite { get; set; }
    [Export] public Color PointsColor { get; set; } = new Color(0.3f, 0.8f, 1f);
    [Export] public float PointsScale { get; set; } = 0.4f;
    [Export] public int PointsAmount { get; set; } = 50;

    public override void _Ready() => Instance = this;

    public void RollDrops(EnemyData data, Vector2 pos)
    {
        if (data.GuaranteedChip)
        {
            int n = Mathf.Max(1, data.ChipDrops);
            for (int i = 0; i < n; i++)  // várias fichas se espalham em volta do corpo
                Spawn(DropType.Chip, n == 1 ? pos : pos + Vector2.FromAngle(Mathf.Tau * i / n) * 0.8f * Units.Px);
            return;
        }

        // Uma única rolagem: no máximo 1 drop por inimigo comum.
        float r = GD.Randf();
        if (r < data.ChipDropChance) Spawn(DropType.Chip, pos);
        else if ((r -= data.ChipDropChance) < data.HealDropChance) Spawn(DropType.Heal, pos);
        else if (r - data.HealDropChance < data.PointsDropChance) Spawn(DropType.Points, pos);
    }

    public void Spawn(DropType type, Vector2 pos)
    {
        var d = DropScene.Instantiate<Drop>();
        AddChild(d);
        switch (type)
        {
            case DropType.Chip: d.Setup(type, 1, pos, ChipSprite, ChipColor, ChipScale); break;
            case DropType.Heal: d.Setup(type, HealAmount, pos, HealSprite, HealColor, HealScale); break;
            default: d.Setup(type, PointsAmount, pos, PointsSprite, PointsColor, PointsScale); break;
        }
    }
}
