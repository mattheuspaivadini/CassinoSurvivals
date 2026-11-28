using Godot;

/// <summary>Decide e cria drops quando um inimigo morre.</summary>
public partial class DropManager : Node
{
    public static DropManager Instance { get; private set; }

    [Export] public PackedScene DropScene { get; set; }

    [ExportGroup("Ficha de Cassino")]
    [Export] public Texture2D ChipSprite { get; set; }
    [Export] public Texture2D ChipCustomSprite { get; set; }
    [Export] public Color ChipColor { get; set; } = new Color(1f, 0.85f, 0.1f);
    [Export] public float ChipScale { get; set; } = 0.42f;

    [ExportGroup("Cura")]
    [Export] public Texture2D HealSprite { get; set; }
    [Export] public Color HealColor { get; set; } = new Color(0.2f, 1f, 0.4f);
    [Export] public float HealScale { get; set; } = 0.75f;
    [Export] public float HealAmount { get; set; } = 20f;

    [ExportGroup("Bônus de Pontos")]
    [Export] public Texture2D PointsSprite { get; set; }
    [Export] public Texture2D PointsCustomSprite { get; set; }
    [Export] public Color PointsColor { get; set; } = new Color(0.3f, 0.8f, 1f);
    [Export] public float PointsScale { get; set; } = 0.42f;
    [Export] public int PointsAmount { get; set; } = 50;

    [ExportGroup("Bebida")]
    /// <summary>Se vazio, usa o sprite da cura tingido com a cor da bebida.</summary>
    [Export] public Texture2D DrinkSprite { get; set; }
    [Export] public Texture2D DrinkHealSheet { get; set; }
    [Export] public Texture2D DrinkDamageSheet { get; set; }
    [Export] public Texture2D DrinkSpeedSheet { get; set; }
    [Export] public float DrinkAnimFps { get; set; } = 8f;
    [Export] public float DrinkScale { get; set; } = 0.85f;
    /// <summary>Cor por DrinkType: Cura, Dano, Velocidade.</summary>
    [Export] public Color[] DrinkColors { get; set; } =
    {
        new Color(1f, 0.45f, 0.75f),
        new Color(1f, 0.35f, 0.2f),
        new Color(0.7f, 0.45f, 1f),
    };

    const int DrinkFrameSize = 32;
    SpriteFrames[] drinkFrames;

    public override void _Ready()
    {
        Instance = this;
        EnsureDropVisuals();
    }

    static Texture2D LoadTexture(string resPath)
    {
        var tex = ResourceLoader.Exists(resPath) ? GD.Load<Texture2D>(resPath) : null;
        if (tex != null) return tex;

        string abs = ProjectSettings.GlobalizePath(resPath);
        if (!System.IO.File.Exists(abs)) return null;
        var img = new Image();
        if (img.Load(abs) != Error.Ok) return null;
        return ImageTexture.CreateFromImage(img);
    }

    void EnsureDropVisuals()
    {
        ChipCustomSprite ??= LoadTexture("res://assets/sprites/ficha_dourada.png");
        PointsCustomSprite ??= LoadTexture("res://assets/sprites/pontos_azul.png");
        if (drinkFrames != null) return;
        DrinkHealSheet ??= LoadTexture("res://assets/sprites/cura.png");
        DrinkDamageSheet ??= LoadTexture("res://assets/sprites/dano.png");
        DrinkSpeedSheet ??= LoadTexture("res://assets/sprites/velocidade.png");

        drinkFrames = new SpriteFrames[DrinkRules.Count];
        drinkFrames[(int)DrinkType.Heal] = BuildGridFrames(DrinkHealSheet, DrinkFrameSize, 5, DrinkAnimFps);
        drinkFrames[(int)DrinkType.Damage] = BuildGridFrames(DrinkDamageSheet, DrinkFrameSize, 5, DrinkAnimFps);
        drinkFrames[(int)DrinkType.Speed] = BuildGridFrames(DrinkSpeedSheet, DrinkFrameSize, 8, DrinkAnimFps);
    }

    static SpriteFrames BuildGridFrames(Texture2D sheet, int frameSize, int frameCount, float fps)
    {
        if (sheet == null) return null;
        int cols = Mathf.Max(1, sheet.GetWidth() / frameSize);
        var frames = new SpriteFrames();
        if (frames.HasAnimation("default")) frames.RemoveAnimation("default");
        frames.AddAnimation("idle");
        frames.SetAnimationSpeed("idle", fps);
        frames.SetAnimationLoopMode("idle", SpriteFrames.LoopMode.Linear);
        for (int i = 0; i < frameCount; i++)
        {
            frames.AddFrame("idle", new AtlasTexture
            {
                Atlas = sheet,
                Region = new Rect2((i % cols) * frameSize, (i / cols) * frameSize, frameSize, frameSize)
            });
        }
        return frames;
    }

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
        else if ((r -= data.HealDropChance) < data.PointsDropChance) Spawn(DropType.Points, pos);
        else if (!data.IsBoss && r - data.PointsDropChance < data.DrinkDropChance) Spawn(DropType.Drink, pos);
    }

    public void Spawn(DropType type, Vector2 pos)
    {
        EnsureDropVisuals();
        var d = DropScene.Instantiate<Drop>();
        AddChild(d);
        switch (type)
        {
            case DropType.Chip:
                if (ChipCustomSprite != null) d.Setup(type, 1, pos, ChipCustomSprite, Colors.White, ChipScale);
                else d.Setup(type, 1, pos, ChipSprite, ChipColor, ChipScale);
                break;
            case DropType.Heal:
                var healFrames = drinkFrames[(int)DrinkType.Heal];
                if (healFrames != null) d.SetupAnimated(type, HealAmount, pos, healFrames, Colors.White, HealScale);
                else d.Setup(type, HealAmount, pos, HealSprite, HealColor, HealScale);
                break;
            case DropType.Drink:
                int drink = RunState.Rng.Next(DrinkRules.Count);
                var frames = drinkFrames[drink];
                if (frames != null) d.SetupAnimated(type, drink, pos, frames, Colors.White, DrinkScale);
                else d.Setup(type, drink, pos, DrinkSprite ?? HealSprite, DrinkColors[drink], DrinkScale);
                break;
            default:
                if (PointsCustomSprite != null) d.Setup(type, PointsAmount, pos, PointsCustomSprite, Colors.White, PointsScale);
                else d.Setup(type, PointsAmount, pos, PointsSprite, PointsColor, PointsScale);
                break;
        }
    }
}
