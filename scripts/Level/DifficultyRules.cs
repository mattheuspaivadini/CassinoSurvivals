using Godot;

/// <summary>
/// Camada de dificuldade do grupo por cima da curva oficial (CampaignRules, PDF seção 8).
/// Objetivo: a primeira run sem upgrades da Loja Externa não deve zerar; o jogador repete e compra
/// Vigor/Arsenal/Barman/Sorte/Imortalidade para vencer. Todos os ajustes ficam aqui para calibrar no playtest (#26).
/// </summary>
public static class DifficultyRules
{
    /// <summary>Mais monstros por horda (a curva oficial vai de 25 a 60).</summary>
    public const float MobCountScale = 1.4f;
    /// <summary>HP extra: +15% na F1, crescendo +2% por fase.</summary>
    public const float HpBase = 1.15f, HpPerStage = 0.02f;
    /// <summary>Dano extra: +10% na F1, crescendo +1,5% por fase.</summary>
    public const float DamageBase = 1.10f, DamagePerStage = 0.015f;
    /// <summary>Velocidade dos inimigos: +2,5% por fase (F15 = +35%).</summary>
    public const float SpeedPerStage = 0.025f;
    /// <summary>Bosses mais longos (o teto de DPS continua o do PDF).</summary>
    public const float BossHpScale = 1.25f;

    public static float HpScale(int stage) => HpBase + HpPerStage * (Clamp(stage) - 1);
    public static float DamageScale(int stage) => DamageBase + DamagePerStage * (Clamp(stage) - 1);
    public static float SpeedScale(int stage) => 1f + SpeedPerStage * (Clamp(stage) - 1);
    public static int MobCount(int baseCount) => Mathf.CeilToInt(baseCount * MobCountScale);

    /// <summary>Aplica a camada de dificuldade a uma fase da curva oficial.</summary>
    public static RoundSpec Apply(RoundSpec s) => new RoundSpec(
        s.Round, s.Act, s.StageInAct,
        MobCount(s.Count),
        s.Duration * Mathf.Sqrt(MobCountScale), // horda maior sai um pouco mais densa, não só mais longa
        s.Elites,
        s.HpMultiplier * HpScale(s.Round),
        s.DamageMultiplier * DamageScale(s.Round),
        s.Boss,
        s.BossHp * BossHpScale,
        s.BossChips,
        s.BossDollars,
        s.RoomClearDollars);

    static int Clamp(int stage) => Mathf.Clamp(stage, 1, CampaignRules.TotalStages);
}
