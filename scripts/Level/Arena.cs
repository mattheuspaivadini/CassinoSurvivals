using System.Collections.Generic;
using Godot;

/// <summary>
/// Raiz de uma fase: lê os obstáculos (grupo "solid"), monta a grade de navegação e mantém o flow
/// field apontando para o jogador. Também responde a colisão de projéteis sem consultas de física.
/// </summary>
public partial class Arena : Node2D
{
    public static Arena Instance { get; private set; }

    /// <summary>Meia área interna jogável, em unidades.</summary>
    [Export] public Vector2 HalfExtents { get; set; } = new Vector2(20f, 26f);
    /// <summary>Folga em volta dos obstáculos na grade (unidades), ~raio de um inimigo.</summary>
    [Export] public float Inflate { get; set; } = 0.5f;
    /// <summary>Margem da câmera além da arena (unidades); o topo ganha espaço extra pela saída.</summary>
    [Export] public float CameraMargin { get; set; } = 2f;
    [Export] public float CameraTopExtra { get; set; } = 4f;
    /// <summary>Sala de Descanso (Blockout 3): trava a câmera em 1 tela fixa (1920x1080) sem scroll.</summary>
    [Export] public bool FixedCamera { get; set; } = false;

    public Rect2 Bounds { get; private set; }
    public ArenaGrid Grid { get; private set; }

    readonly List<Rect2> solids = new List<Rect2>();
    FlowField flow;
    Vector2I lastPlayerCell = new Vector2I(int.MinValue, int.MinValue);

    public override void _Ready()
    {
        Instance = this;
        Vector2 half = HalfExtents * Units.Px;
        Bounds = new Rect2(-half, half * 2f);

        foreach (Node n in GetTree().GetNodesInGroup("solid"))
            foreach (Node child in n.GetChildren())
                if (child is CollisionShape2D cs && cs.Shape is RectangleShape2D rect)
                    solids.Add(new Rect2(cs.GlobalPosition - rect.Size / 2f, rect.Size));

        Grid = new ArenaGrid(Bounds, Units.Px, solids, Inflate * Units.Px);
        flow = new FlowField(Grid);
        LimitCamera();
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public override void _PhysicsProcess(double delta)
    {
        var player = PlayerController.Instance;
        if (player == null) return;
        var cell = Grid.ToCell(player.GlobalPosition);
        if (cell == lastPlayerCell) return;
        lastPlayerCell = cell;
        flow.Compute(cell);
    }

    /// <summary>Linha livre até o alvo: vai direto. Senão segue o flow field (que leva ao jogador).</summary>
    public Vector2 Steer(Vector2 from, Vector2 target)
    {
        if (Grid.LineClear(from, target)) return (target - from).Normalized();
        Vector2 d = flow.Direction(from);
        return d == Vector2.Zero ? (target - from).Normalized() : d;
    }

    /// <summary>Colisão única do PDF: obstáculos e tudo fora da área interna bloqueiam projéteis.</summary>
    public bool IsSolid(Vector2 p)
    {
        if (!Bounds.HasPoint(p)) return true;
        for (int i = 0; i < solids.Count; i++)
            if (solids[i].HasPoint(p)) return true;
        return false;
    }

    /// <summary>Tiro livre entre dois pontos (amostra a cada 20px contra os sólidos reais, sem inflar).</summary>
    public bool ShotClear(Vector2 a, Vector2 b)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(a.DistanceTo(b) / 20f));
        for (int i = 1; i < steps; i++)
            if (IsSolid(a.Lerp(b, (float)i / steps))) return false;
        return true;
    }

    void LimitCamera()
    {
        var cam = PlayerController.Instance?.GetNodeOrNull<Camera2D>("Camera");
        if (cam == null) return;
        if (FixedCamera)
        {
            cam.PositionSmoothingEnabled = false;
            cam.LimitLeft = -960;
            cam.LimitRight = 960;
            cam.LimitTop = -540;
            cam.LimitBottom = 540;
            return;
        }
        float m = CameraMargin * Units.Px;
        cam.LimitLeft = Mathf.FloorToInt(Bounds.Position.X - m);
        cam.LimitRight = Mathf.CeilToInt(Bounds.End.X + m);
        cam.LimitTop = Mathf.FloorToInt(Bounds.Position.Y - m - CameraTopExtra * Units.Px);
        cam.LimitBottom = Mathf.CeilToInt(Bounds.End.Y + m);
    }
}
