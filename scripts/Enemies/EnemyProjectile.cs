using Godot;

/// <summary>
/// Projétil inimigo (losango da Ouros, cartas da Rainha). Desenhado em código, sem física:
/// acerta o jogador por distância ao longo do segmento do frame e para em obstáculos sólidos.
/// </summary>
public partial class EnemyProjectile : Node2D
{
    Vector2 velocity;
    float damage, life, radius;
    Color color;
    Texture2D texture;
    Rect2 region;

    public static EnemyProjectile Fire(Node context, Vector2 from, Vector2 dir, float speedUnits, float damage, Color color, float radiusUnits = 0.22f, float lifetime = 4f, Texture2D texture = null, Rect2 region = default)
    {
        var p = new EnemyProjectile
        {
            texture = texture,
            region = region.Size == Vector2.Zero && texture != null ? new Rect2(Vector2.Zero, texture.GetSize()) : region,
            velocity = dir.Normalized() * speedUnits * Units.Px,
            damage = damage,
            life = lifetime,
            radius = radiusUnits * Units.Px,
            color = color,
            GlobalPosition = from,
            Rotation = dir.Angle(),
            ZIndex = 3,
        };
        (context.GetTree()?.CurrentScene ?? context).AddChild(p);
        return p;
    }

    public override void _Process(double delta)
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;
        float dt = (float)delta;
        life -= dt;
        if (life <= 0f) { QueueFree(); return; }

        Vector2 previous = GlobalPosition;
        GlobalPosition += velocity * dt;

        var player = PlayerController.Instance;
        if (player != null && IsInstanceValid(player))
        {
            float reach = radius + player.RadiusPx;
            Vector2 closest = Geometry2D.GetClosestPointToSegment(player.GlobalPosition, previous, GlobalPosition);
            if (player.GlobalPosition.DistanceSquaredTo(closest) <= reach * reach)
            {
                player.TakeDamage(damage);
                QueueFree();
                return;
            }
        }

        if (Arena.Instance != null && Arena.Instance.IsSolid(GlobalPosition)) QueueFree();
    }

    public override void _Draw()
    {
        if (texture != null)
        {
            // A textura aponta para cima; o nó gira para a direção do tiro (+X).
            float s = radius * 3f / Mathf.Max(region.Size.X, region.Size.Y);
            DrawSetTransform(Vector2.Zero, Mathf.Pi / 2f, Vector2.One * s);
            DrawTextureRectRegion(texture, new Rect2(-region.Size / 2f, region.Size), region);
            DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
            return;
        }
        float r = radius * 1.4f;
        var pts = new[] { new Vector2(r, 0f), new Vector2(0f, r * 0.6f), new Vector2(-r, 0f), new Vector2(0f, -r * 0.6f) };
        DrawColoredPolygon(pts, color);
        DrawPolyline(new[] { pts[0], pts[1], pts[2], pts[3], pts[0] }, Colors.White, 1.5f);
    }
}
