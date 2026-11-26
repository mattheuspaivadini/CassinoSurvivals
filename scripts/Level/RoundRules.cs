using Godot;

/// <summary>Configuração de um round da fase.</summary>
public readonly struct RoundSpec
{
    public readonly int Round;
    /// <summary>Inimigos comuns + Dourados (sem contar o boss).</summary>
    public readonly int Count;
    public readonly float Duration;
    public readonly int Elites;
    public readonly float HpMultiplier;
    public readonly bool Boss;
    public readonly float BossHp;

    public RoundSpec(int round, int count, float duration, int elites, float hpMultiplier, bool boss, float bossHp)
    {
        Round = round;
        Count = count;
        Duration = duration;
        Elites = elites;
        HpMultiplier = hpMultiplier;
        Boss = boss;
        BossHp = bossHp;
    }
}

/// <summary>
/// Progressão da fase: 21 rounds, boss a cada múltiplo de 7. Rounds de boss seguem as 2 etapas do PDF
/// (mini-horda com metade do volume, depois o chefe). Classe pura: testada no SelfCheck.
/// </summary>
public static class RoundRules
{
    public const int RoundsPerStage = 21;
    public const int BossEvery = 7;

    public static bool IsBossRound(int round) => round % BossEvery == 0;

    public static RoundSpec For(int round)
    {
        bool boss = IsBossRound(round);
        int count = 6 + round;
        float duration = 12f + round;
        if (boss)
        {
            count = Mathf.CeilToInt(count / 2f);
            duration *= 0.5f;
        }
        int elites = round < 3 ? 0 : round < 10 ? 1 : 2;
        float hp = 1f + 0.06f * (round - 1);
        float bossHp = boss ? 2500f * (round / BossEvery) : 0f;
        return new RoundSpec(round, count, duration, elites, hp, boss, bossHp);
    }
}
