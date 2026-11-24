using Godot;

/// <summary>Movimento top-down + HP do jogador.</summary>
public partial class PlayerController : RigidBody2D
{
    public static PlayerController Instance { get; private set; }

    [Export] public float MaxHp { get; set; } = 100f;
    [Export] public float MoveSpeed { get; set; } = 6f;
    /// <summary>Raio (unidades) usado nas checagens de contato/coleta por distância.</summary>
    [Export] public float Radius { get; set; } = 0.4f;

    /// <summary>(hpAtual, hpMax)</summary>
    [Signal] public delegate void HpChangedEventHandler(float hp, float max);

    public float CurrentHp { get; private set; }
    public float RadiusPx => Radius * Units.Px;

    Sprite2D sprite;
    Color baseColor;
    float flashTimer;

    public override void _Ready()
    {
        Instance = this;
        sprite = GetNode<Sprite2D>("Sprite");
        baseColor = sprite.Modulate;
        CurrentHp = MaxHp;
    }

    public override void _PhysicsProcess(double delta)
    {
        LinearVelocity = Input.GetVector("move_left", "move_right", "move_up", "move_down") * MoveSpeed * Units.Px;
    }

    public override void _Process(double delta)
    {
        if (flashTimer <= 0f) return;
        flashTimer -= (float)delta;
        if (flashTimer <= 0f) sprite.Modulate = baseColor;
    }

    public void TakeDamage(float amount)
    {
        if (CurrentHp <= 0f || GameManager.Instance.State != GameState.Playing) return;
        CurrentHp = Mathf.Max(0f, CurrentHp - amount);
        Flash(Colors.Red);
        AudioManager.Instance?.PlayPlayerHurt();
        EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
        if (CurrentHp <= 0f) GameManager.Instance.GameOver();
    }

    public void Heal(float amount)
    {
        if (CurrentHp <= 0f) return;
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        Flash(Colors.Green);
        AudioManager.Instance?.PlayHeal();
        EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
    }

    void Flash(Color c)
    {
        sprite.Modulate = c;
        flashTimer = 0.1f;
    }
}
