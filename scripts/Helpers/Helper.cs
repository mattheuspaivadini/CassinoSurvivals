using Godot;

/// <summary>NPC aliado: parado no mapa até o jogador encostar; depois segue o jogador e cura periodicamente com um pulso verde.</summary>
public partial class Helper : Node2D
{
    [Export] public float ActivationRadius { get; set; } = 1.5f;
    [Export] public float FollowDistance { get; set; } = 1.6f;
    [Export] public float FollowSharpness { get; set; } = 6f;
    [Export] public float HealAmount { get; set; } = 4f;
    [Export] public float HealInterval { get; set; } = 3f;
    [Export] public Color IdleColor { get; set; } = new Color(0.3f, 0.45f, 0.3f);
    [Export] public Color ActiveColor { get; set; } = new Color(0.3f, 1f, 0.5f);
    [Export] public Color PulseColor { get; set; } = new Color(0.7f, 1f, 0.7f);

    Sprite2D sprite;
    bool rescued;
    float healTimer, pulse;
    Vector2 offset;
    Vector2 baseScale;

    public override void _Ready()
    {
        sprite = GetNode<Sprite2D>("Sprite");
        sprite.Modulate = IdleColor;
        baseScale = Scale;
    }

    public override void _Process(double delta)
    {
        var player = PlayerController.Instance;
        if (player == null) return;
        Vector2 pos = GlobalPosition;

        if (!rescued)
        {
            float r = ActivationRadius * Units.Px;
            if ((player.GlobalPosition - pos).LengthSquared() > r * r) return;
            rescued = true;
            // cada ajudante pega um lugar diferente ao redor
            offset = Vector2.FromAngle(GD.Randf() * Mathf.Tau) * FollowDistance * Units.Px;
            sprite.Modulate = ActiveColor;
            healTimer = HealInterval;
            AudioManager.Instance?.PlayPickup();
            return;
        }

        float dt = (float)delta;
        // Lerp independente de framerate
        GlobalPosition = pos.Lerp(player.GlobalPosition + offset, 1f - Mathf.Exp(-FollowSharpness * dt));

        healTimer -= dt;
        if (healTimer <= 0f)
        {
            healTimer = HealInterval;
            player.Heal(HealAmount);
            pulse = 1f;
        }

        if (pulse > 0f)
        {
            pulse = Mathf.Max(0f, pulse - dt * 3f);
            Scale = baseScale * (1f + 0.6f * pulse);
            sprite.Modulate = ActiveColor.Lerp(PulseColor, pulse);
        }
    }
}
