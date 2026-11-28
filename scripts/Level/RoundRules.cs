using Godot;

/// <summary>Configuração de um round/fase da campanha.</summary>
public readonly struct RoundSpec
{
    public readonly int Round;
    public readonly int Act;
    public readonly int StageInAct;
    /// <summary>Inimigos comuns + Dourados (sem contar o boss).</summary>
    public readonly int Count;
    public readonly float Duration;
    public readonly int Elites;
    public readonly float HpMultiplier;
    public readonly float DamageMultiplier;
    public readonly bool Boss;
    public readonly float BossHp;
    /// <summary>Fichas que o boss solta ao morrer.</summary>
    public readonly int BossChips;
    /// <summary>$ pago pelo boss (PDF: $75 / $125 / $250).</summary>
    public readonly int BossDollars;
    /// <summary>$ pago ao limpar a sala normal (Ato 1 $10, Ato 2 $20, Ato 3 $30; 0 em fase de boss).</summary>
    public readonly int RoomClearDollars;

    public RoundSpec(
        int round,
        int act,
        int stageInAct,
        int count,
        float duration,
        int elites,
        float hpMultiplier,
        float damageMultiplier,
        bool boss,
        float bossHp,
        int bossChips,
        int bossDollars,
        int roomClearDollars)
    {
        Round = round;
        Act = act;
        StageInAct = stageInAct;
        Count = count;
        Duration = duration;
        Elites = elites;
        HpMultiplier = hpMultiplier;
        DamageMultiplier = damageMultiplier;
        Boss = boss;
        BossHp = bossHp;
        BossChips = bossChips;
        BossDollars = bossDollars;
        RoomClearDollars = roomClearDollars;
    }
}

/// <summary>
/// Adaptador da progressão oficial de 15 fases (3 Atos x 5, bosses nas fases 5/10/15) sobre
/// <see cref="CampaignRules"/>. Classe pura: testada no SelfCheck.
/// </summary>
public static class RoundRules
{
    public const int RoundsPerStage = CampaignRules.TotalStages;
    public const int BossEvery = CampaignRules.StagesPerAct;

    public static bool IsBossRound(int round) => CampaignRules.IsBossStage(round);

    public static RoundSpec For(int round)
    {
        StageSpec s = CampaignRules.ForStage(round);
        return new RoundSpec(
            s.Stage,
            s.Act,
            s.StageInAct,
            s.TotalMobs,
            s.Duration,
            s.GoldenCount,
            s.HpScale,
            s.DamageScale,
            s.IsBossStage,
            s.BossHp,
            s.BossChips,
            s.BossDollars,
            s.RoomClearDollars);
    }
}
