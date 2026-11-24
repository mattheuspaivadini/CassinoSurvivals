using Godot;

/// <summary>Seta orbitando o jogador que aponta para o caça-níquel mais próximo enquanto houver fichas.</summary>
public partial class Wayfinder : Node2D
{
    [Export] public Node2D Arrow { get; set; }
    /// <summary>Unidades.</summary>
    [Export] public float OrbitRadius { get; set; } = 1.2f;

    public override void _Process(double delta)
    {
        bool show = GameManager.Instance.Chips > 0 && SlotMachine.All.Count > 0;
        if (Arrow.Visible != show) Arrow.Visible = show;
        if (!show) return;

        Vector2 pos = GlobalPosition;
        Vector2 dir = (SlotMachine.FindNearest(pos).GlobalPosition - pos).Normalized();
        Arrow.GlobalPosition = pos + dir * OrbitRadius * Units.Px;
        Arrow.GlobalRotation = dir.Angle() + Mathf.Pi / 2f; // o sprite aponta para cima; no Godot +Y é para baixo
    }
}
