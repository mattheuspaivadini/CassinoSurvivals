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
    /// <summary>Fichas que o boss solta ao morrer.</summary>
    public readonly int BossChips;
    /// <summary>$ pago pelo boss (PDF: $75 / $125 / $250).</summary>
    public readonly int BossDollars;

    public RoundSpec(int round, int count, float duration, int elites, float hpMultiplier, bool boss, float bossHp,
        int bossChips, int bossDollars)
    {
        BossChips = bossChips;
        BossDollars = bossDollars;
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
        int count = 30 + 6 * round;
        float duration = 18f + 1.2f * round;
        if (boss)
        {
            count = Mathf.CeilToInt(count / 2f);
            duration *= 0.5f;
        }
        // Fichas: 1 Dourado por round até o 1º boss, 2 até o 2º, 3 depois. Rounds de boss não têm
        // Dourado; o próprio boss solta 3, 4 e 5 fichas.
        int act = (round - 1) / BossEvery; // 0, 1, 2
        int elites = boss ? 0 : act + 1;
        float hp = 1f + 0.06f * (round - 1);
        float bossHp = boss ? 2500f * (round / BossEvery) : 0f;
        int bossChips = boss ? 2 + round / BossEvery : 0;
        int bossDollars = !boss ? 0 : (round / BossEvery) switch { 1 => 75, 2 => 125, _ => 250 };
        return new RoundSpec(round, count, duration, elites, hp, boss, bossHp, bossChips, bossDollars);
    }
}
