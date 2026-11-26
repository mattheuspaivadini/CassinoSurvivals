using Godot;

/// <summary>
/// Porta superior da sala: bloqueia a passagem até a horda ser limpa. Aberta, o gatilho no corredor
/// encerra a fase (vitória, até existir a Sala de Descanso).
/// </summary>
public partial class ExitDoor : StaticBody2D
{
    public bool IsOpen { get; private set; }

    CollisionShape2D shape;
    CanvasItem bars;

    public override void _Ready()
    {
        shape = GetNode<CollisionShape2D>("Shape");
        bars = GetNode<CanvasItem>("Bars");
        GetNode<Area2D>("Trigger").BodyEntered += OnBodyEntered;
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
