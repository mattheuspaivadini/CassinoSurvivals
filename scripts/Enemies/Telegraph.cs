using System;
using Godot;

/// <summary>
/// Aviso vermelho no chão (círculo, faixa ou cone) que se preenche até o golpe e então chama
/// <c>onStrike</c>. Só avança com o jogo rodando. Criado solto na cena atual, abaixo dos personagens.
/// </summary>
public partial class Telegraph : Node2D
{
    enum Shape { Circle, Line, Cone }

    static readonly Color Warn = new Color(1f, 0.18f, 0.15f);

    Shape shape;
    Vector2 a, b;
    float radius, halfWidth, spread, duration, timer;
    Action onStrike;
    Func<bool> cancelled;

    public static Telegraph Circle(Node context, Vector2 center, float radiusPx, float seconds, Action onStrike, Func<bool> cancelled = null) =>
        Create(context, Shape.Circle, center, center, radiusPx, 0f, 0f, seconds, onStrike, cancelled);

    public static Telegraph Line(Node context, Vector2 from, Vector2 to, float halfWidthPx, float seconds, Action onStrike, Func<bool> cancelled = null) =>
        Create(context, Shape.Line, from, to, 0f, halfWidthPx, 0f, seconds, onStrike, cancelled);

    public static Telegraph Cone(Node context, Vector2 origin, Vector2 aim, float lengthPx, float spreadDegrees, float seconds, Action onStrike, Func<bool> cancelled = null) =>
        Create(context, Shape.Cone, origin, origin + aim.Normalized() * lengthPx, lengthPx, 0f, Mathf.DegToRad(spreadDegrees), seconds, onStrike, cancelled);

    static Telegraph Create(Node context, Shape shape, Vector2 a, Vector2 b, float radius, float halfWidth, float spread, float seconds, Action onStrike, Func<bool> cancelled)
    {
        var t = new Telegraph
        {
            shape = shape, a = a, b = b, radius = radius, halfWidth = halfWidth, spread = spread,
            duration = Mathf.Max(0.05f, seconds), onStrike = onStrike, cancelled = cancelled, ZIndex = 0,
        };
        var root = context.GetTree()?.CurrentScene ?? context;
        root.AddChild(t);
        return t;
    }

    public override void _Process(double delta)
    {
        if (cancelled != null && cancelled()) { QueueFree(); return; }
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

        timer += (float)delta;
        QueueRedraw();
        if (timer < duration) return;
        onStrike?.Invoke();
        QueueFree();
    }

    public override void _Draw()
    {
        float p = Mathf.Clamp(timer / duration, 0f, 1f);
        var fill = new Color(Warn, 0.14f + 0.22f * p);
        var edge = new Color(Warn, 0.9f);

        switch (shape)
        {
            case Shape.Circle:
                DrawCircle(a, radius, new Color(Warn, 0.12f));
                DrawCircle(a, radius * p, fill);
                DrawArc(a, radius, 0f, Mathf.Tau, 48, edge, 3f);
                break;

            case Shape.Line:
            {
                Vector2 dir = (b - a).Normalized();
                Vector2 n = dir.Orthogonal() * halfWidth;
                Vector2 tip = a + (b - a) * p;
                DrawColoredPolygon(new[] { a + n, b + n, b - n, a - n }, new Color(Warn, 0.12f));
                DrawColoredPolygon(new[] { a + n, tip + n, tip - n, a - n }, fill);
                DrawPolyline(new[] { a + n, b + n, b - n, a - n, a + n }, edge, 3f);
                break;
            }

            case Shape.Cone:
            {
                float baseAngle = (b - a).Angle();
                const int steps = 16;
                var outer = new Vector2[steps + 2];
                var inner = new Vector2[steps + 2];
                outer[0] = inner[0] = a;
                for (int i = 0; i <= steps; i++)
                {
                    float ang = baseAngle - spread / 2f + spread * i / steps;
                    outer[i + 1] = a + Vector2.FromAngle(ang) * radius;
                    inner[i + 1] = a + Vector2.FromAngle(ang) * radius * p;
                }
                DrawColoredPolygon(outer, new Color(Warn, 0.12f));
                DrawColoredPolygon(inner, fill);
                break;
            }
        }
    }
}
