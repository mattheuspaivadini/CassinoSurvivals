using System.Collections.Generic;
using Godot;

/// <summary>
/// Indicadores dinâmicos do HUD (PDF, seção 2): seta para a saída depois do clear e setas na borda
/// da tela para os últimos inimigos vivos fora da câmera.
/// </summary>
public partial class OffscreenPointers : Node2D
{
    [Export] public Texture2D ArrowTexture { get; set; }
    [Export] public int MaxEnemyPointers { get; set; } = 3;
    /// <summary>Distância da borda da tela, em px.</summary>
    [Export] public float Margin { get; set; } = 56f;
    [Export] public Color EnemyColor { get; set; } = new Color(1f, 0.25f, 0.3f);
    [Export] public Color ExitColor { get; set; } = new Color(1f, 0.85f, 0.15f);

    readonly List<Sprite2D> pool = new List<Sprite2D>();
    float bob;

    public override void _Process(double delta)
    {
        bob += (float)delta;
        int used = 0;
        var horde = HordeDirector.Instance;
        if (horde != null)
        {
            if (horde.IsCleared && horde.Exit != null)
                Point(horde.Exit.GlobalPosition, ExitColor, true, ref used);
            else if (horde.AllSpawned && horde.Remaining <= MaxEnemyPointers)
                foreach (var e in EnemySpawner.Instance.Active)
                    Point(e.GlobalPosition, EnemyColor, false, ref used);
        }
        for (int i = used; i < pool.Count; i++) pool[i].Visible = false;
    }

    /// <summary>Fora da tela: seta presa na borda. Dentro: só a saída ganha seta balançando acima dela.</summary>
    void Point(Vector2 world, Color color, bool showOnScreen, ref int used)
    {
        Vector2 size = GetViewportRect().Size;
        Vector2 screen = GetViewport().GetCanvasTransform() * world;
        Vector2 center = size / 2f;
        Vector2 pos;
        float rot;

        if (new Rect2(Vector2.Zero, size).Grow(-Margin).HasPoint(screen))
        {
            if (!showOnScreen) return;
            pos = screen + new Vector2(0f, 110f + Mathf.Sin(bob * 6f) * 12f);
            rot = 0f; // aponta para cima, para a porta
        }
        else
        {
            Vector2 dir = screen - center;
            Vector2 half = center - Vector2.One * Margin;
            float t = Mathf.Min(Mathf.Abs(half.X / Mathf.Max(Mathf.Abs(dir.X), 0.001f)),
                                Mathf.Abs(half.Y / Mathf.Max(Mathf.Abs(dir.Y), 0.001f)));
            pos = center + dir * t;
            rot = dir.Angle() + Mathf.Pi / 2f; // o sprite aponta para cima
        }

        var s = Get(used++);
        s.Position = pos;
        s.Rotation = rot;
        s.Modulate = color;
        s.Visible = true;
    }

    Sprite2D Get(int i)
    {
        if (i < pool.Count) return pool[i];
        var s = new Sprite2D { Texture = ArrowTexture, Scale = Vector2.One * 0.6f };
        AddChild(s);
        pool.Add(s);
        return s;
    }
}
