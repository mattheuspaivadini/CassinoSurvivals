using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Fases da campanha: cada fase tem uma horda finita saindo das portas laterais. Fases múltiplas de 5
/// (5, 10 e 15) trazem mini-horda de aquecimento seguida de Boss.
/// </summary>
public partial class HordeDirector : Node
{
    enum Phase { Break, Horde, BossIncoming, BossFight, Done }

    public static HordeDirector Instance { get; private set; }

    [ExportGroup("Inimigos")]
    [Export] public Godot.Collections.Array<EnemyData> CommonTypes { get; set; }
    [Export] public EnemyData FichaType { get; set; }
    [Export] public EnemyData FichonaType { get; set; }
    [Export] public EnemyData ChampanheType { get; set; }
    [Export] public EnemyData EspadasType { get; set; }
    [Export] public EnemyData OurosType { get; set; }
    [Export] public EnemyData PausType { get; set; }
    [Export] public EnemyData CopasType { get; set; }
    /// <summary>Dourado: drop de ficha garantido.</summary>
    [Export] public EnemyData EliteType { get; set; }
    /// <summary>Base do boss; o HP vem da fase (RoundRules.BossHp).</summary>
    [Export] public EnemyData BossType { get; set; }

    /// <summary>Chance de drop de pontos dos comuns na fase (o "XP" da horda).</summary>
    [Export(PropertyHint.Range, "0,1")] public float CommonPointsDropChance { get; set; } = 0.25f;
    /// <summary>$ por round limpo legado (quando RoomClearDollars == 0 em sala normal).</summary>
    [Export] public int RoundClearDollars { get; set; } = 10;

    [ExportGroup("Campanha")]
    [Export(PropertyHint.Range, "1,15")] public int StartStage { get; set; } = 1;
    [Export(PropertyHint.Range, "1,15")] public int EndStage { get; set; } = CampaignRules.TotalStages;
    [Export] public Node2D BossSpawnPoint { get; set; }

    [ExportGroup("Ritmo")]
    /// <summary>Pausa entre fases (s).</summary>
    [Export] public float BreakTime { get; set; } = 3f;
    /// <summary>Pausa dramática entre a mini-horda e a entrada do boss (s).</summary>
    [Export] public float BossDelay { get; set; } = 2f;
    /// <summary>Índices (0-based) das portas perto do spawn do jogador.</summary>
    [Export] public Godot.Collections.Array<int> LowerDoors { get; set; } = new() { 2, 5 };
    [Export] public float LowerDoorDelay { get; set; } = 2.5f;

    [ExportGroup("Sala")]
    [Export] public Node DoorsRoot { get; set; }
    [Export] public ExitDoor Exit { get; set; }
    [Export] public string StageLabel { get; set; } = "ATO 1 — FASE 1/5";

    [Signal] public delegate void RoundStartedEventHandler(int round, bool boss);
    [Signal] public delegate void ClearedEventHandler();

    public int Round { get; private set; }
    /// <summary>Velocidade dos inimigos da fase atual (DifficultyRules); lida pelo Enemy no spawn.</summary>
    public float EnemySpeedMultiplier { get; private set; } = 1f;
    /// <summary>Liberados para as portas mas ainda não nascidos (contam no teto de vivos).</summary>
    int queued;
    float hordeClock;
    public int Rounds => Mathf.Max(1, EndStage - StartStage + 1);
    public bool IsSingleStage => StartStage >= EndStage;
    public StageSpec CurrentStageSpec => stageSpec;
    public bool IsBossRound => RoundRules.IsBossRound(Round);
    public bool InBreak => phase == Phase.Break;
    /// <summary>Inimigos que ainda faltam nesta fase (inclui os que nem saíram e o boss).</summary>
    public int Remaining => roundTotal - roundKilled;
    public bool AllSpawned => phase != Phase.Break && roundSpawned >= roundTotal;
    public bool IsCleared => phase == Phase.Done;

    readonly RandomNumberGenerator rng = new RandomNumberGenerator();
    readonly Dictionary<EnemyKind, EnemyData> kindLookup = new Dictionary<EnemyKind, EnemyData>();
    List<SpawnDoor> doors;
    List<HordeEntry> plan;
    RoundSpec spec;
    StageSpec stageSpec;
    Phase phase = Phase.Break;
    int next, roundTotal, roundSpawned, roundKilled, extraSpawned;
    float timer;

    public override void _Ready()
    {
        Instance = this;
        rng.Randomize();
        doors = DoorsRoot.GetChildren().OfType<SpawnDoor>().ToList();
        // PDF: comuns não soltam ficha (só Dourados e bosses). Mais drop de pontos.
        var commons = new Godot.Collections.Array<EnemyData>();
        if (CommonTypes != null)
        {
            foreach (var t in CommonTypes)
                commons.Add(NormalizeCommon(t));
        }
        CommonTypes = commons;

        RegisterKind(EnemyKind.Ficha, FichaType);
        RegisterKind(EnemyKind.Fichona, FichonaType);
        RegisterKind(EnemyKind.Champanhe, ChampanheType);
        RegisterKind(EnemyKind.CartaEspadas, EspadasType);
        RegisterKind(EnemyKind.CartaOuros, OurosType);
        RegisterKind(EnemyKind.CartaPaus, PausType);
        RegisterKind(EnemyKind.CartaCopas, CopasType);

        int initialStage = Mathf.Clamp(StartStage, 1, CampaignRules.TotalStages);
        EndStage = Mathf.Clamp(EndStage, initialStage, CampaignRules.TotalStages);
        StartStage = initialStage;
        Round = initialStage - 1;
        stageSpec = CampaignRules.ForStage(initialStage);
        StageLabel = stageSpec.StageLabel;
        if (Arena.Instance != null && (Arena.Instance.IsBossArena || Arena.Instance.ActNumber != stageSpec.Act))
            Arena.Instance.ApplyActTheme(stageSpec.Act);

        AudioManager.Instance?.PlayMainMusic(); // reiniciar no meio de um boss volta à trilha normal
        EnemySpawner.Instance.EnemyRemoved += OnEnemyRemoved;
        EnemySpawner.Instance.ExtraEnemySpawned += OnExtraEnemySpawned;
        timer = 0.5f; // primeira fase começa quase de imediato
    }

    EnemyData NormalizeCommon(EnemyData source)
    {
        var c = source.CloneWithSharedFrames();
        c.ChipDropChance = 0f;
        c.PointsDropChance = CommonPointsDropChance;
        return c;
    }

    /// <summary>Sem export na cena, carrega o .tres padrão do tipo (as 4 cartas entram sem editar as arenas).</summary>
    void RegisterKind(EnemyKind kind, EnemyData source)
    {
        source ??= EnemyCatalog.Load(kind);
        if (source != null) kindLookup[kind] = NormalizeCommon(source);
    }

    /// <summary>
    /// Reforços de boss pelas portas (Guarda da Corte, Decreto, Lacaios): $0, sem drop, nunca Dourados.
    /// Entram no total da fase já na fila, para a sala não fechar com porta pendente. Retorna false se o tipo não existe ou não há portas.
    /// </summary>
    public bool SummonAtDoors(EnemyKind kind, int count, System.Action<Enemy> onSpawned)
    {
        if (count <= 0 || doors == null || doors.Count == 0 || !kindLookup.TryGetValue(kind, out var source)) return false;
        var data = EnemyData.CreateSummon(source);
        roundTotal += count;
        for (int i = 0; i < count; i++)
        {
            var door = doors[rng.RandiRange(0, doors.Count - 1)];
            door.Enqueue(d =>
            {
                var e = EnemySpawner.Instance.Spawn(data, d.SpawnPosition, spec.HpMultiplier, damageMultiplier: spec.DamageMultiplier);
                e.SetEntry(d.EntryPosition);
                roundSpawned++;
                onSpawned?.Invoke(e);
            });
        }
        return true;
    }

    public override void _ExitTree()
    {
        if (EnemySpawner.Instance != null)
        {
            EnemySpawner.Instance.EnemyRemoved -= OnEnemyRemoved;
            EnemySpawner.Instance.ExtraEnemySpawned -= OnExtraEnemySpawned;
        }
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
                ReleaseDue(dt);
                break;

            case Phase.BossIncoming:
                ReleaseDue(dt); // a horda continua chegando junto com o boss
                timer -= dt;
                if (timer <= 0f) SpawnBoss();
                break;

            case Phase.BossFight:
                ReleaseDue(dt);
                break;
        }
    }

    /// <summary>
    /// Solta a horda conforme o plano. Teto de vivos (DifficultyRules.MaxAlive): o atraso acumula e a
    /// fila sai assim que abrir vaga. Continua durante a luta contra o boss.
    /// </summary>
    void ReleaseDue(float dt)
    {
        hordeClock += dt;
        while (next < plan.Count && plan[next].Time <= hordeClock && EnemySpawner.Instance.Active.Count + queued < DifficultyRules.MaxAlive)
        {
            queued++;
            Release(plan[next++]);
        }
    }

    void StartRound(int round)
    {
        Round = round;
        stageSpec = CampaignRules.ForStage(round);
        spec = DifficultyRules.Apply(RoundRules.For(round));
        EnemySpeedMultiplier = DifficultyRules.SpeedScale(round);
        StageLabel = stageSpec.StageLabel;
        if (Arena.Instance != null && (Arena.Instance.IsBossArena || Arena.Instance.ActNumber != stageSpec.Act))
            Arena.Instance.ApplyActTheme(stageSpec.Act);

        plan = HordePlan.Build(spec.Count, spec.Duration, doors.Count, spec.Elites, LowerDoors.ToList(), LowerDoorDelay, rng);
        next = roundSpawned = roundKilled = extraSpawned = queued = 0;
        hordeClock = 0f;
        roundTotal = spec.Count + (spec.Boss ? 1 : 0);
        timer = 0f;
        phase = Phase.Horde;
        if (spec.Boss) AudioManager.Instance?.PlayBossMusic();
        else AudioManager.Instance?.PlayMainMusic();
        EmitSignal(SignalName.RoundStarted, round, spec.Boss);
    }

    void Release(HordeEntry entry)
    {
        var baseType = ResolveEnemyDataForStage(stageSpec);
        var type = entry.Elite ? EnemyData.CreateGoldenVariant(baseType) : baseType;
        doors[entry.Door].Enqueue(door =>
        {
            var enemy = EnemySpawner.Instance.Spawn(type, door.SpawnPosition, spec.HpMultiplier, damageMultiplier: spec.DamageMultiplier);
            enemy.SetEntry(door.EntryPosition);
            roundSpawned++;
            queued = Mathf.Max(0, queued - 1);
        });
    }

    EnemyData ResolveEnemyDataForStage(StageSpec s)
    {
        if (kindLookup.Count > 0 && s.SpawnWeights != null)
        {
            int availableTotal = 0;
            for (int i = 0; i < s.SpawnWeights.Count; i++)
            {
                if (kindLookup.ContainsKey(s.SpawnWeights[i].Kind))
                    availableTotal += Mathf.Max(0, s.SpawnWeights[i].Weight);
            }
            if (availableTotal > 0)
            {
                int roll = rng.RandiRange(0, availableTotal - 1);
                int acc = 0;
                for (int i = 0; i < s.SpawnWeights.Count; i++)
                {
                    var w = s.SpawnWeights[i];
                    if (!kindLookup.TryGetValue(w.Kind, out var candidate)) continue;
                    acc += Mathf.Max(0, w.Weight);
                    if (roll < acc) return candidate;
                }
            }
        }
        return CommonTypes[rng.RandiRange(0, CommonTypes.Count - 1)];
    }

    /// <summary>O boss entra pela porta mais longe do jogador, para dar tempo de ler a entrada.</summary>
    void SpawnBoss()
    {
        phase = Phase.BossFight;
        // Rainha (F5), Rei de Paus (F10) e Tigrinho (F15, chefe final). BossType da cena é o fallback.
        var source = EnemyCatalog.LoadBoss(stageSpec.Boss) ?? BossType;
        var data = source.CloneWithSharedFrames();
        data.MaxHp = spec.BossHp;
        data.BossDpsCap = stageSpec.BossDpsCap;
        data.FixedHp = true;
        data.GuaranteedChip = spec.BossChips > 0;
        data.ChipDrops = spec.BossChips;
        data.Dollars = spec.BossDollars;

        Vector2 playerPos = PlayerController.Instance.GlobalPosition;
        var candidateDoors = BossSpawnPoint != null
            ? doors.Where(d => d.GlobalPosition.Y <= 0f).DefaultIfEmpty(doors[0])
            : doors;
        var door = candidateDoors.OrderByDescending(d => d.GlobalPosition.DistanceSquaredTo(playerPos)).First();
        door.Enqueue(d =>
        {
            // O boss é largo: nasce mais para dentro para não ficar preso na parede.
            float inset = data.Scale * 0.5f + 0.5f;
            var boss = EnemySpawner.Instance.Spawn(data, d.GlobalPosition + d.InwardDir * inset * Units.Px, 1f, damageMultiplier: spec.DamageMultiplier);
            Vector2 entryTarget = BossSpawnPoint != null
                ? BossSpawnPoint.GlobalPosition
                : d.GlobalPosition + d.InwardDir * (inset + 2f) * Units.Px;
            boss.SetEntry(entryTarget);
            roundSpawned++;
            EnemySpawner.Instance.AnnounceBoss();
        });
    }

    void OnExtraEnemySpawned()
    {
        if (phase != Phase.Horde && phase != Phase.BossIncoming && phase != Phase.BossFight) return;
        roundTotal++;
        roundSpawned++;
        if (phase == Phase.Horde) extraSpawned++;
    }

    void OnEnemyRemoved()
    {
        if (phase != Phase.Horde && phase != Phase.BossIncoming && phase != Phase.BossFight) return;
        roundKilled++;

        // O boss entra no meio da horda (DifficultyRules.BossEntryFraction) e luta junto com o resto dela.
        if (phase == Phase.Horde && spec.Boss && roundKilled >= DifficultyRules.BossEntryKills(spec.Count))
        {
            phase = Phase.BossIncoming;
            timer = BossDelay;
            return;
        }
        if (roundKilled < roundTotal) return;

        int clearDollars = spec.RoomClearDollars > 0 ? spec.RoomClearDollars : (spec.Boss ? 0 : RoundClearDollars);
        if (clearDollars > 0) GameManager.Instance.AddDollars(clearDollars);
        if (spec.Boss) AudioManager.Instance?.PlayMainMusic();

        if (Round >= EndStage)
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
