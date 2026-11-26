using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Rounds da fase: cada round é uma horda finita saindo das portas laterais. Rounds múltiplos de 7
/// trazem um boss depois da mini-horda. A saída só abre quando o último round (21) é limpo.
/// </summary>
public partial class HordeDirector : Node
{
    enum Phase { Break, Horde, BossIncoming, BossFight, Done }

    public static HordeDirector Instance { get; private set; }

    [ExportGroup("Inimigos")]
    [Export] public Godot.Collections.Array<EnemyData> CommonTypes { get; set; }
    /// <summary>Dourado: drop de ficha garantido.</summary>
    [Export] public EnemyData EliteType { get; set; }
    /// <summary>Base do boss; o HP vem do round (RoundRules.BossHp).</summary>
    [Export] public EnemyData BossType { get; set; }

    [ExportGroup("Ritmo")]
    /// <summary>Pausa entre rounds (s).</summary>
    [Export] public float BreakTime { get; set; } = 3f;
    /// <summary>Pausa dramática entre a mini-horda e a entrada do boss (s).</summary>
    [Export] public float BossDelay { get; set; } = 2f;
    /// <summary>Índices (0-based) das portas perto do spawn do jogador.</summary>
    [Export] public Godot.Collections.Array<int> LowerDoors { get; set; } = new() { 2, 5 };
    [Export] public float LowerDoorDelay { get; set; } = 2.5f;

    [ExportGroup("Sala")]
    [Export] public Node DoorsRoot { get; set; }
    [Export] public ExitDoor Exit { get; set; }
    [Export] public string StageLabel { get; set; } = "ATO 1 - FASE 1";

    [Signal] public delegate void RoundStartedEventHandler(int round, bool boss);
    [Signal] public delegate void ClearedEventHandler();

    public int Round { get; private set; }
    public int Rounds => RoundRules.RoundsPerStage;
    public bool IsBossRound => RoundRules.IsBossRound(Round);
    public bool InBreak => phase == Phase.Break;
    /// <summary>Inimigos que ainda faltam neste round (inclui os que nem saíram e o boss).</summary>
    public int Remaining => roundTotal - roundKilled;
    public bool AllSpawned => phase != Phase.Break && roundSpawned >= roundTotal;
    public bool IsCleared => phase == Phase.Done;

    readonly RandomNumberGenerator rng = new RandomNumberGenerator();
    List<SpawnDoor> doors;
    List<HordeEntry> plan;
    RoundSpec spec;
    Phase phase = Phase.Break;
    int next, roundTotal, roundSpawned, roundKilled;
    float timer;

    public override void _Ready()
    {
        Instance = this;
        rng.Randomize();
        doors = DoorsRoot.GetChildren().OfType<SpawnDoor>().ToList();
        EnemySpawner.Instance.EnemyRemoved += OnEnemyRemoved;
        timer = 0.5f; // primeiro round começa quase de imediato
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public override void _Process(double delta)
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        float dt = (float)delta;

        switch (phase)
        {
            case Phase.Break:
                timer -= dt;
                if (timer <= 0f) StartRound(Round + 1);
                break;

            case Phase.Horde:
                timer += dt;
                while (next < plan.Count && plan[next].Time <= timer) Release(plan[next++]);
                break;

            case Phase.BossIncoming:
                timer -= dt;
                if (timer <= 0f) SpawnBoss();
                break;
        }
    }

    void StartRound(int round)
    {
        Round = round;
        spec = RoundRules.For(round);
        plan = HordePlan.Build(spec.Count, spec.Duration, doors.Count, spec.Elites, LowerDoors.ToList(), LowerDoorDelay, rng);
        next = roundSpawned = roundKilled = 0;
        roundTotal = spec.Count + (spec.Boss ? 1 : 0);
        timer = 0f;
        phase = Phase.Horde;
        EmitSignal(SignalName.RoundStarted, round, spec.Boss);
    }

    void Release(HordeEntry entry)
    {
        var type = entry.Elite ? EliteType : CommonTypes[rng.RandiRange(0, CommonTypes.Count - 1)];
        doors[entry.Door].Enqueue(door =>
        {
            var enemy = EnemySpawner.Instance.Spawn(type, door.SpawnPosition, spec.HpMultiplier);
            enemy.SetEntry(door.EntryPosition);
            roundSpawned++;
        });
    }

    /// <summary>O boss entra pela porta mais longe do jogador, para dar tempo de ler a entrada.</summary>
    void SpawnBoss()
    {
        phase = Phase.BossFight;
        var data = (EnemyData)BossType.Duplicate();
        data.MaxHp = spec.BossHp;
        data.FixedHp = true;

        Vector2 playerPos = PlayerController.Instance.GlobalPosition;
        var door = doors.OrderByDescending(d => d.GlobalPosition.DistanceSquaredTo(playerPos)).First();
        door.Enqueue(d =>
        {
            // O boss é largo: nasce mais para dentro para não ficar preso na parede.
            float inset = data.Scale * 0.5f + 0.5f;
            var boss = EnemySpawner.Instance.Spawn(data, d.GlobalPosition + d.InwardDir * inset * Units.Px, 1f);
            boss.SetEntry(d.GlobalPosition + d.InwardDir * (inset + 2f) * Units.Px);
            roundSpawned++;
            EnemySpawner.Instance.AnnounceBoss();
        });
    }

    void OnEnemyRemoved()
    {
        if (phase != Phase.Horde && phase != Phase.BossFight) return;
        roundKilled++;

        if (phase == Phase.Horde && spec.Boss && roundKilled >= spec.Count)
        {
            phase = Phase.BossIncoming;
            timer = BossDelay;
            return;
        }
        if (roundKilled < roundTotal) return;

        if (Round >= Rounds)
        {
            phase = Phase.Done;
            Exit?.Open();
            EmitSignal(SignalName.Cleared);
            return;
        }
        phase = Phase.Break;
        timer = BreakTime;
    }
}
