using Godot;

/// <summary>
/// Comportamento plugável de um inimigo (naipes e bosses). O Enemy cuida de HP, física, animação
/// e morte; o cérebro decide a velocidade desejada a cada frame e dispara os ataques.
/// </summary>
public abstract class EnemyBrain
{
    protected Enemy Self { get; private set; }
    protected EnemyData Data => Self.Data;

    /// <summary>true: a velocidade retornada é aplicada na hora (investidas), sem a inércia padrão.</summary>
    public bool SnapVelocity { get; protected set; }
    /// <summary>false enquanto o inimigo não deve causar dano de contato (ex.: no ar).</summary>
    public bool ContactDamage { get; protected set; } = true;
    /// <summary>Multiplicador do dano de contato (investidas batem mais forte).</summary>
    public float ContactScale { get; protected set; } = 1f;
    /// <summary>Sprite olha para o jogador mesmo parado (atiradores, bosses).</summary>
    public virtual bool FacePlayer => false;
    public virtual bool NeedsRedraw => false;

    public static EnemyBrain Create(EnemyBehavior behavior) => behavior switch
    {
        EnemyBehavior.Espadas => new EspadasBrain(),
        EnemyBehavior.Ouros => new OurosBrain(),
        EnemyBehavior.Copas => new CopasBrain(),
        EnemyBehavior.Rainha => new RainhaBrain(),
        EnemyBehavior.Rei => new ReiBrain(),
        EnemyBehavior.Tigrinho => new TigrinhoBrain(),
        _ => null,
    };

    public virtual void Attach(Enemy self) => Self = self;

    /// <summary>Velocidade desejada em px/s.</summary>
    public abstract Vector2 Think(float dt, Vector2 toPlayer);

    /// <summary>Bosses: teto de DPS e invulnerabilidade. Retorna o dano que de fato entra.</summary>
    public virtual float FilterDamage(float amount) => amount;

    public virtual void OnDeath() { }

    /// <summary>Desenho extra no canvas do inimigo (escudo etc.).</summary>
    public virtual void Draw(Enemy canvas) { }

    protected static PlayerController Player => PlayerController.Instance;

    protected bool HasLineOfSight(Vector2 to) => Arena.Instance == null || Arena.Instance.ShotClear(Self.GlobalPosition, to);

    /// <summary>Mantém distância entre min e max (unidades) do jogador, andando de lado no meio.</summary>
    protected Vector2 KeepDistance(Vector2 toPlayer, float minUnits, float maxUnits, float speed, ref float strafeSign)
    {
        float d = toPlayer.Length() / Units.Px;
        Vector2 playerPos = Self.GlobalPosition + toPlayer;
        if (d > maxUnits || !HasLineOfSight(playerPos)) return Self.SteerTo(playerPos, speed);
        if (d < minUnits) return Self.WithSeparation(-toPlayer.Normalized(), speed);
        return Self.WithSeparation(toPlayer.Normalized().Orthogonal() * strafeSign, speed * 0.6f);
    }

    /// <summary>Distância (px) livre até um obstáculo sólido na direção dada, até <paramref name="maxPx"/>.</summary>
    protected float FreeDistance(Vector2 from, Vector2 dir, float maxPx)
    {
        var arena = Arena.Instance;
        if (arena == null) return maxPx;
        float step = 0.25f * Units.Px;
        Vector2 d = dir.Normalized();
        for (float t = step; t <= maxPx; t += step)
            if (arena.IsSolid(from + d * t)) return Mathf.Max(0f, t - Self.Radius);
        return maxPx;
    }
}
