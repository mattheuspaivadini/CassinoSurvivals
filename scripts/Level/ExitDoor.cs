using Godot;

/// <summary>
/// Porta superior da sala: bloqueia a passagem até a horda ser limpa (ou começa aberta na Sala de
/// Descanso). Aberta, o gatilho no corredor encerra/avança a sala.
/// </summary>
public partial class ExitDoor : StaticBody2D
{
    public static ExitDoor Instance { get; private set; }

    [Export] public bool StartsOpen { get; set; } = false;

    public bool IsOpen { get; private set; }

    CollisionShape2D shape;
    CanvasItem bars;

    public override void _Ready()
    {
        Instance = this;
        shape = GetNode<CollisionShape2D>("Shape");
        bars = GetNode<CanvasItem>("Bars");
        GetNode<Area2D>("Trigger").BodyEntered += OnBodyEntered;
        if (StartsOpen)
        {
            IsOpen = true;
            shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
            bars.Modulate = new Color(bars.Modulate.R, bars.Modulate.G, bars.Modulate.B, 0f);
        }
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        CreateTween().TweenProperty(bars, "modulate:a", 0f, 0.6f);
    }

    void OnBodyEntered(Node2D body)
    {
        if (IsOpen && body is PlayerController) GameManager.Instance.Victory();
    }
}
