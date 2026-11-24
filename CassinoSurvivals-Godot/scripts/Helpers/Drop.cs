using Godot;

/// <summary>Item no chão. Coleta por distância ao jogador, sem trigger de física.</summary>
public partial class Drop : Node2D
{
    /// <summary>Unidades.</summary>
    [Export] public float PickupRadius { get; set; } = 0.7f;
    /// <summary>Cura e pontos somem depois disso; fichas nunca somem.</summary>
    [Export] public float Lifetime { get; set; } = 40f;

    Sprite2D sprite;
    DropType type;
    float value, life;

    public override void _Ready() => sprite = GetNode<Sprite2D>("Sprite");

    /// <summary>Chamar depois de AddChild.</summary>
    public void Setup(DropType t, float v, Vector2 pos, Texture2D texture, Color color, float scale)
    {
        type = t;
        value = v;
        life = Lifetime;
        Position = pos;
        sprite.Texture = texture;
        Units.Fit(sprite, scale);
        sprite.Modulate = color;
    }

    public override void _Process(double delta)
    {
        if (type != DropType.Chip)
        {
            life -= (float)delta;
            if (life <= 0f) { QueueFree(); return; }
        }

        var player = PlayerController.Instance;
        if (player == null) return;
        float r = (PickupRadius + player.Radius) * Units.Px;
        if ((player.GlobalPosition - GlobalPosition).LengthSquared() > r * r) return;

        switch (type)
        {
            case DropType.Chip: GameManager.Instance.AddChips((int)value); break;
            case DropType.Heal: player.Heal(value); break;
            case DropType.Points: GameManager.Instance.AddScore((int)value); break;
        }
        AudioManager.Instance?.PlayPickup();
        QueueFree();
    }
}
