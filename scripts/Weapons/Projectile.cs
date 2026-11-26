using Godot;

/// <summary>Projétil sem física: move por posição e testa acerto ao longo do segmento percorrido no frame, evitando tunneling.</summary>
public partial class Projectile : Node2D
{
    Sprite2D sprite;
    Vector2 velocity;
    float damage, life, radius;

    public override void _Ready() => sprite = GetNode<Sprite2D>("Sprite");

    /// <summary>Usa o recorte de bala da folha, sem tingir o sprite e com escala em pixels.</summary>
    public void Launch(Vector2 pos, Vector2 vel, float dmg, float lifetime, AtlasTexture texture, float scale)
    {
        Position = pos;
        Rotation = vel.Angle(); // a bala aponta para +X, mesma convenção das armas

        if (sprite != null && texture != null)
        {
            sprite.Texture = texture;
            // Única escala aplicada: a cena não define Scale e o código não usa Units.Fit.
            sprite.Scale = Vector2.One * Mathf.Max(0.01f, scale);
            sprite.Modulate = Colors.White; // preserva as cores nativas do sprite da bala
            // Colisão por distância (não há CollisionShape2D): proporcional ao sprite renderizado.
            radius = Mathf.Max(4f, texture.Region.Size.X * sprite.Scale.X * 0.35f);
        }
        else radius = 6f;

        velocity = vel;
        damage = dmg;
        life = lifetime;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        life -= dt;
        if (life <= 0f) { QueueFree(); return; }

        Vector2 previous = GlobalPosition;
        GlobalPosition += velocity * dt;

        var hit = FindSegmentHit(previous, GlobalPosition);
        if (hit != null)
        {
            hit.TakeDamage(damage);
            QueueFree();
            return;
        }

        // Colisão única do PDF: obstáculos da arena também param o tiro.
        if (Arena.Instance != null && Arena.Instance.IsSolid(GlobalPosition)) QueueFree();
    }

    /// <summary>
    /// Testa o segmento entre a posição anterior e a atual. Com 1000 px/s a bala pode
    /// atravessar um inimigo pequeno entre dois frames; o ponto mais próximo do segmento
    /// mantém o acerto mesmo nesse caso. Varremos os filhos do spawner (sem consultar física
    /// e sem alterar a classe dos inimigos) e escolhemos o alvo mais próximo do início do tiro.
    /// </summary>
    Enemy FindSegmentHit(Vector2 from, Vector2 to)
    {
        var spawner = EnemySpawner.Instance;
        if (spawner == null) return null;

        Enemy best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < spawner.GetChildCount(); i++)
        {
            if (spawner.GetChild(i) is not Enemy enemy || enemy.ActiveIndex < 0) continue;
            float reach = radius + enemy.Radius;
            Vector2 closest = Geometry2D.GetClosestPointToSegment(enemy.GlobalPosition, from, to);
            if ((enemy.GlobalPosition - closest).LengthSquared() > reach * reach) continue;

            float distance = (closest - from).LengthSquared();
            if (distance < bestDistance) { bestDistance = distance; best = enemy; }
        }
        return best;
    }
}
