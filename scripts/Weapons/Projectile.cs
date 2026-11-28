using System.Collections.Generic;
using Godot;

/// <summary>Projétil sem física: move por posição e testa acerto ao longo do segmento percorrido no frame, evitando tunneling e suportando perfuração.</summary>
public partial class Projectile : Node2D
{
    Sprite2D sprite;
    Vector2 velocity;
    float damage, life, radius;
    int remainingPierce;
    bool isCritical;
    HashSet<Enemy> hitEnemies;
    readonly List<(Enemy Enemy, float DistSq)> segmentHits = new List<(Enemy, float)>(8);

    public override void _Ready() => sprite = GetNode<Sprite2D>("Sprite");

    /// <summary>Usa o recorte de bala da folha, sem tingir o sprite (exceto 777 Crítico dourado) e com escala em pixels.</summary>
    public void Launch(Vector2 pos, Vector2 vel, float dmg, float lifetime, AtlasTexture texture, float scale, int pierceCount = 0, bool isCrit = false)
    {
        Position = pos;
        Rotation = vel.Angle(); // a bala aponta para +X, mesma convenção das armas
        isCritical = isCrit;

        if (sprite != null && texture != null)
        {
            sprite.Texture = texture;
            // Única escala aplicada: a cena não define Scale e o código não usa Units.Fit.
            float finalScale = Mathf.Max(0.01f, scale) * (isCrit ? 1.25f : 1f);
            sprite.Scale = Vector2.One * finalScale;
            sprite.Modulate = isCrit ? new Color(1f, 0.90f, 0.25f) : Colors.White;
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
            Vector2 hitDir = velocity.LengthSquared() > 1f ? velocity.Normalized() : Vector2.Right;
            for (int i = 0; i < segmentHits.Count; i++)
            {
                var enemy = segmentHits[i].Enemy;
                if (!IsInstanceValid(enemy) || enemy.ActiveIndex < 0 || enemy.IsDead) continue;

                enemy.TakeDamage(damage, isCrit: isCritical);
                if (isCritical)
                {
                    GameFeel.WorldBurst(this, enemy.GlobalPosition, GameFeel.Gold, 14, 240f);
                    SpawnCritPopup(enemy.GlobalPosition);
                }
                if (IsInstanceValid(enemy) && !enemy.IsDead)
                {
                    if (RunState.Has(Passive.GeloNoCopo)) enemy.ApplySlow();
                    if (RunState.Has(Passive.MaoQuente)) enemy.ApplyBurn(damage);
                    if (RunState.Has(Passive.AllIn)) enemy.ApplyKnockback(hitDir, PassiveRules.AllInKnockbackSpeedPx);
                }

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

    void SpawnCritPopup(Vector2 worldPos)
    {
        var root = GetTree()?.CurrentScene;
        if (root is not Node2D) return;
        var label = new Label
        {
            Text = "777!",
            Position = worldPos + new Vector2(-24f, -38f),
            ZIndex = 12,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", 20);
        label.AddThemeColorOverride("font_color", GameFeel.Gold);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 4);
        root.AddChild(label);

        var t = label.CreateTween();
        t.SetParallel(true);
        t.TweenProperty(label, "position:y", label.Position.Y - 34f, 0.45f);
        t.TweenProperty(label, "modulate:a", 0f, 0.45f);
        t.Chain().TweenCallback(Callable.From(label.QueueFree));
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
