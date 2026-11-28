using System.Collections.Generic;
using Godot;

/// <summary>Projétil sem física: move por posição e testa acerto ao longo do segmento percorrido no frame, evitando tunneling e suportando perfuração.</summary>
public partial class Projectile : Node2D
{
    Sprite2D sprite;
    Vector2 velocity;
    float damage, life, radius;
    int remainingPierce;
    HashSet<Enemy> hitEnemies;
    readonly List<(Enemy Enemy, float DistSq)> segmentHits = new List<(Enemy, float)>(8);

    public override void _Ready() => sprite = GetNode<Sprite2D>("Sprite");

    /// <summary>Usa o recorte de bala da folha, sem tingir o sprite e com escala em pixels.</summary>
    public void Launch(Vector2 pos, Vector2 vel, float dmg, float lifetime, AtlasTexture texture, float scale, int pierceCount = 0)
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
        remainingPierce = Mathf.Max(0, pierceCount);
        hitEnemies?.Clear();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        life -= dt;
        if (life <= 0f) { QueueFree(); return; }

        Vector2 previous = GlobalPosition;
        GlobalPosition += velocity * dt;

        CollectSegmentHits(previous, GlobalPosition);
        if (segmentHits.Count > 0)
        {
            if (segmentHits.Count > 1) segmentHits.Sort((a, b) => a.DistSq.CompareTo(b.DistSq));
            for (int i = 0; i < segmentHits.Count; i++)
            {
                var enemy = segmentHits[i].Enemy;
                if (!IsInstanceValid(enemy) || enemy.ActiveIndex < 0 || enemy.IsDead) continue;

                enemy.TakeDamage(damage);
                if (remainingPierce <= 0)
                {
                    QueueFree();
                    return;
                }
                remainingPierce--;
                hitEnemies ??= new HashSet<Enemy>();
                hitEnemies.Add(enemy);
            }
        }

        // Colisão única do PDF: obstáculos da arena também param o tiro.
        if (Arena.Instance != null && Arena.Instance.IsSolid(GlobalPosition)) QueueFree();
    }

    /// <summary>
    /// Testa o segmento entre a posição anterior e a atual, coletando todos os alvos ainda não atingidos
    /// ordenáveis por distância para perfuração em linha (Sniper / Calibre Pesado) sem tunneling.
    /// </summary>
    void CollectSegmentHits(Vector2 from, Vector2 to)
    {
        segmentHits.Clear();
        var spawner = EnemySpawner.Instance;
        if (spawner == null) return;

        var arena = Arena.Instance;
        for (int i = 0; i < spawner.GetChildCount(); i++)
        {
            if (spawner.GetChild(i) is not Enemy enemy || enemy.ActiveIndex < 0 || enemy.IsDead) continue;
            if (hitEnemies != null && hitEnemies.Contains(enemy)) continue;

            float reach = radius + enemy.Radius;
            Vector2 closest = Geometry2D.GetClosestPointToSegment(enemy.GlobalPosition, from, to);
            if ((enemy.GlobalPosition - closest).LengthSquared() > reach * reach) continue;
            if (arena != null && arena.IsSolid(closest)) continue;

            segmentHits.Add((enemy, (closest - from).LengthSquared()));
        }
    }
}
