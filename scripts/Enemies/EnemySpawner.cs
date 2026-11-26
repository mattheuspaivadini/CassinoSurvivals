using System.Collections.Generic;
using Godot;

/// <summary>
/// Spawner central + registro dos inimigos ativos. Também responde às buscas de alvo/colisão
/// por LengthSquared (sem consultas de física, sem alocação).
/// </summary>
public partial class EnemySpawner : Node
{
    public static EnemySpawner Instance { get; private set; }

    /// <summary>false nas fases com horda finita: quem spawna é o HordeDirector (sem spawn por tempo nem boss).</summary>
    [Export] public bool AutoSpawn { get; set; } = true;

    [ExportGroup("Tipos")]
    [Export] public Godot.Collections.Array<EnemyData> CommonTypes { get; set; }
    [Export] public EnemyData EliteType { get; set; }
    [Export] public EnemyData BossType { get; set; }
    [Export] public PackedScene EnemyScene { get; set; }

    [ExportGroup("Ritmo")]
    [Export] public float InitialInterval { get; set; } = 1.0f;
    [Export] public float MinInterval { get; set; } = 0.2f;
    /// <summary>Segundos até o intervalo chegar ao mínimo.</summary>
    [Export] public float RampDuration { get; set; } = 240f;
    /// <summary>A cada X segundos, +1 inimigo por leva.</summary>
    [Export] public float BatchGrowthSeconds { get; set; } = 45f;
    [Export] public float EliteChance { get; set; } = 0.03f;
    [Export] public float EliteChancePerMinute { get; set; } = 0.01f;
    /// <summary>HP extra por minuto de partida (0.5 = +50%/min).</summary>
    [Export] public float HpGrowthPerMinute { get; set; } = 0.4f;
    [Export] public int MaxAlive { get; set; } = 400;

    [ExportGroup("Área (unidades)")]
    [Export] public Vector2 MapHalfExtents { get; set; } = new Vector2(30f, 30f);
    [Export] public float SpawnMargin { get; set; } = 2f;

    [Signal] public delegate void BossSpawnedEventHandler();
    /// <summary>Um inimigo saiu da lista ativa (morreu).</summary>
    [Signal] public delegate void EnemyRemovedEventHandler();

    public bool HasBossSpawned { get; private set; }
    public IReadOnlyList<Enemy> Active => active;

    readonly List<Enemy> active = new List<Enemy>(512);
    float timer;

    public override void _Ready() => Instance = this;

    public override void _Process(double delta)
    {
        if (!AutoSpawn) return;
        var gm = GameManager.Instance;
        if (gm.State != GameState.Playing || PlayerController.Instance == null) return;

        if (!HasBossSpawned && gm.IsBossTime) SpawnBoss();

        timer -= (float)delta;
        if (timer > 0f) return;

        float t = gm.Elapsed;
        // Mathf.Lerp do Godot não limita t a [0,1] como o da Unity.
        timer = Mathf.Lerp(InitialInterval, MinInterval, Mathf.Clamp(t / RampDuration, 0f, 1f));
        int batch = 1 + (int)(t / BatchGrowthSeconds);
        float hpMul = 1f + HpGrowthPerMinute * (t / 60f);
        float elite = EliteChance + EliteChancePerMinute * (t / 60f);

        for (int i = 0; i < batch && active.Count < MaxAlive; i++)
        {
            var type = GD.Randf() < elite ? EliteType : CommonTypes[GD.RandRange(0, CommonTypes.Count - 1)];
            Spawn(type, RandomSpawnPosition(), hpMul);
        }
    }

    void SpawnBoss()
    {
        Spawn(BossType, RandomSpawnPosition(), 1f);
        AnnounceBoss();
    }

    /// <summary>Som e banner de entrada do boss (também usado pelos rounds de boss da fase).</summary>
    public void AnnounceBoss()
    {
        HasBossSpawned = true;
        AudioManager.Instance?.PlayBossSpawn();
        EmitSignal(SignalName.BossSpawned);
    }

    public Enemy Spawn(EnemyData data, Vector2 pos, float hpMultiplier)
    {
        var e = EnemyScene.Instantiate<Enemy>();
        e.Position = pos; // antes do AddChild: o corpo já nasce no lugar certo
        AddChild(e);
        e.Init(data, hpMultiplier);
        e.ActiveIndex = active.Count;
        active.Add(e);
        return e;
    }

    public void Despawn(Enemy e)
    {
        int i = e.ActiveIndex;
        if (i < 0) return;
        int last = active.Count - 1;
        active[i] = active[last];
        active[i].ActiveIndex = i;
        active.RemoveAt(last);
        e.ActiveIndex = -1;
        e.QueueFree();
        EmitSignal(SignalName.EnemyRemoved);
    }

    /// <summary>Soma de afastamento dos vizinhos que se sobrepõem (peso cai linearmente com a distância).</summary>
    public Vector2 Separation(Enemy self)
    {
        Vector2 push = Vector2.Zero;
        Vector2 p = self.GlobalPosition;
        for (int i = 0; i < active.Count; i++)
        {
            var e = active[i];
            if (e == self) continue;
            Vector2 d = p - e.GlobalPosition;
            float r = (self.Radius + e.Radius) * 1.3f;
            float l2 = d.LengthSquared();
            if (l2 < 0.0001f || l2 >= r * r) continue;
            float l = Mathf.Sqrt(l2);
            push += d / l * (1f - l / r);
        }
        return push;
    }

    /// <summary>Spawna fora da visão da câmera, preso dentro do mapa.</summary>
    Vector2 RandomSpawnPosition()
    {
        float dist = (GetViewport().GetVisibleRect().Size / 2f).Length() + SpawnMargin * Units.Px;
        Vector2 p = PlayerController.Instance.GlobalPosition + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * dist;
        Vector2 limit = (MapHalfExtents - Vector2.One) * Units.Px;
        return p.Clamp(-limit, limit);
    }

    public Enemy FindNearest(Vector2 from, float maxDistance, out float sqrDist)
    {
        Enemy best = null;
        sqrDist = maxDistance * maxDistance;
        for (int i = 0; i < active.Count; i++)
        {
            float d = (active[i].GlobalPosition - from).LengthSquared();
            if (d < sqrDist) { sqrDist = d; best = active[i]; }
        }
        return best;
    }

    public Enemy FindHit(Vector2 point, float radius)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var e = active[i];
            float r = radius + e.Radius;
            if ((e.GlobalPosition - point).LengthSquared() <= r * r) return e;
        }
        return null;
    }
}
