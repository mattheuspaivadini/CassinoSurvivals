using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Regras de sala (PDF, seção 2): horda finita entrando pelas portas laterais; a saída só abre quando
/// 100% dos inimigos morrem.
/// </summary>
public partial class HordeDirector : Node
{
    public static HordeDirector Instance { get; private set; }

    [ExportGroup("Inimigos")]
    [Export] public Godot.Collections.Array<EnemyData> CommonTypes { get; set; }
    /// <summary>Dourado: drop de ficha garantido.</summary>
    [Export] public EnemyData EliteType { get; set; }
    [Export] public float HpMultiplier { get; set; } = 1f;

    [ExportGroup("Horda")]
    [Export] public int Total { get; set; } = 25;
    [Export] public float Duration { get; set; } = 60f;
    [Export] public int EliteCount { get; set; } = 2;
    /// <summary>Índices (0-based) das portas perto do spawn do jogador.</summary>
    [Export] public Godot.Collections.Array<int> LowerDoors { get; set; } = new() { 2, 5 };
    [Export] public float LowerDoorDelay { get; set; } = 2.5f;

    [ExportGroup("Sala")]
    [Export] public Node DoorsRoot { get; set; }
    [Export] public ExitDoor Exit { get; set; }
    [Export] public string StageLabel { get; set; } = "ATO 1 - FASE 1/5";

    [Signal] public delegate void ProgressChangedEventHandler(int remaining, int total);
    [Signal] public delegate void ClearedEventHandler();

    public int Remaining => Total - killed;
    public bool AllSpawned => spawned >= Total;
    public bool IsCleared { get; private set; }

    readonly RandomNumberGenerator rng = new RandomNumberGenerator();
    List<SpawnDoor> doors;
    List<HordeEntry> plan;
    int next, spawned, killed;
    float elapsed;

    public override void _Ready()
    {
        Instance = this;
        rng.Randomize();
        doors = DoorsRoot.GetChildren().OfType<SpawnDoor>().ToList();
        plan = HordePlan.Build(Total, Duration, doors.Count, EliteCount, LowerDoors.ToList(), LowerDoorDelay, rng);
        EnemySpawner.Instance.EnemyRemoved += OnEnemyRemoved;
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public override void _Process(double delta)
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        elapsed += (float)delta;
        while (next < plan.Count && plan[next].Time <= elapsed)
        {
            var entry = plan[next++];
            var type = entry.Elite ? EliteType : CommonTypes[rng.RandiRange(0, CommonTypes.Count - 1)];
            doors[entry.Door].Enqueue(door =>
            {
                var enemy = EnemySpawner.Instance.Spawn(type, door.SpawnPosition, HpMultiplier);
                enemy.SetEntry(door.EntryPosition);
                spawned++;
            });
        }
    }

    void OnEnemyRemoved()
    {
        killed++;
        EmitSignal(SignalName.ProgressChanged, Remaining, Total);
        if (IsCleared || killed < Total) return;
        IsCleared = true;
        Exit?.Open();
        EmitSignal(SignalName.Cleared);
    }
}
