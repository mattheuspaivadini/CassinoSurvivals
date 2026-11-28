using Godot;

/// <summary>
/// Regras puras do Modo Sandbox/Debug (issue #23): catálogo dos 7 arquétipos (comuns/Dourados)
/// e dos 3 Bosses com os parâmetros oficiais da campanha (MaxHp, FixedHp e BossDpsCap).
/// </summary>
public static class EnemySandboxRules
{
    public readonly struct EnemyOption
    {
        public readonly EnemyKind Kind;
        public readonly string Label;

        public EnemyOption(EnemyKind kind, string label)
        {
            Kind = kind;
            Label = label;
        }
    }

    public readonly struct BossOption
    {
        public readonly BossKind Boss;
        public readonly int OfficialStage;
        public readonly string Label;

        public BossOption(BossKind boss, int officialStage, string label)
        {
            Boss = boss;
            OfficialStage = officialStage;
            Label = label;
        }
    }

    public static readonly EnemyOption[] Enemies =
    {
        new EnemyOption(EnemyKind.Ficha, "Ficha"),
        new EnemyOption(EnemyKind.Fichona, "Fichona"),
        new EnemyOption(EnemyKind.Champanhe, "Garrafa"),
        new EnemyOption(EnemyKind.CartaEspadas, "Espadas ♠"),
        new EnemyOption(EnemyKind.CartaOuros, "Ouros ♦"),
        new EnemyOption(EnemyKind.CartaPaus, "Paus ♣"),
        new EnemyOption(EnemyKind.CartaCopas, "Copas ♥"),
    };

    public static readonly BossOption[] Bosses =
    {
        new BossOption(BossKind.Rainha, 5, "Rainha (Ato 1)"),
        new BossOption(BossKind.Rei, 10, "Rei (Ato 2)"),
        new BossOption(BossKind.MaquinaCacaNiquel, 15, "Máquina (Ato 3)"),
    };

    public static int OfficialStageFor(BossKind boss) => boss switch
    {
        BossKind.Rainha => 5,
        BossKind.Rei => 10,
        BossKind.MaquinaCacaNiquel => 15,
        _ => 15,
    };

    /// <summary>Constrói o EnemyData do arquétipo (comum ou Dourado) para spawn sob demanda.</summary>
    public static EnemyData BuildEnemyData(EnemyKind kind, bool golden)
    {
        var source = EnemyCatalog.Load(kind);
        if (source == null) return null;
        return golden ? EnemyData.CreateGoldenVariant(source) : source.CloneWithSharedFrames();
    }

    /// <summary>Constrói o EnemyData do Boss já com HP, BossDpsCap e recompensas oficiais da fase.</summary>
    public static EnemyData BuildBossData(BossKind boss, out StageSpec stageSpec)
    {
        int stage = OfficialStageFor(boss);
        stageSpec = CampaignRules.ForStage(stage);
        var source = EnemyCatalog.LoadBoss(boss);
        if (source == null) return null;

        var data = source.CloneWithSharedFrames();
        data.MaxHp = stageSpec.BossHp;
        data.BossDpsCap = stageSpec.BossDpsCap;
        data.FixedHp = true;
        data.GuaranteedChip = stageSpec.BossChips > 0;
        data.ChipDrops = stageSpec.BossChips;
        data.Dollars = stageSpec.BossDollars;
        return data;
    }

    /// <summary>Escala oficial de HP e Dano da fase (F1..F15).</summary>
    public static (float HpScale, float DamageScale) StageScaling(int stage)
    {
        var s = CampaignRules.ForStage(Mathf.Clamp(stage, 1, CampaignRules.TotalStages));
        return (s.HpScale, s.DamageScale);
    }
}
