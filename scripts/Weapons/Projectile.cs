using Godot;

/// <summary>Projétil sem física: move por posição e testa acerto por distância (LengthSquared) contra a lista de inimigos ativos.</summary>
public partial class Projectile : Node2D
{
    Sprite2D sprite;
    Vector2 velocity;
    float damage, life, radius;

    public override void _Ready() => sprite = GetNode<Sprite2D>("Sprite");

    public void Launch(Vector2 pos, Vector2 vel, float dmg, float lifetime, Color color, float scale)
    {
        Position = pos;
        Rotation = vel.Angle();
        Units.Fit(sprite, scale);
        sprite.Modulate = color;
        velocity = vel;
        damage = dmg;
        life = lifetime;
        radius = scale * 0.5f * Units.Px;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        life -= dt;
        if (life <= 0f) { QueueFree(); return; }

        Position += velocity * dt;

        var hit = EnemySpawner.Instance.FindHit(Position, radius);
        if (hit != null)
        {
            hit.TakeDamage(damage);
            QueueFree();
        }
    }
}
