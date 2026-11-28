using System;
using System.Collections.Generic;
using Godot;

/// <summary>Tipos do bestiário oficial da campanha (ordem de desbloqueio em escada).</summary>
public enum EnemyKind
{
    Ficha,
    Fichona,
    Champanhe,
    CartaEspadas,
    CartaOuros,
    CartaPaus,
    CartaCopas,
}

/// <summary>Chefes oficiais dos 3 Atos (Fases 5, 10 e 15).</summary>
public enum BossKind
{
    None,
    Rainha,
    Rei,
    MaquinaCacaNiquel,
}

/// <summary>Tipo de sala de combate.</summary>
public enum ArenaKind
{
    Normal,
    Boss,
}

/// <summary>Peso de sorteio de um tipo de inimigo na composição da horda de uma fase.</summary>
public readonly struct EnemyWeight
{
    public readonly EnemyKind Kind;
    public readonly int Weight;

    public EnemyWeight(EnemyKind kind, int weight)
    {
        Kind = kind;
        Weight = weight;
    }
}

/// <summary>Identidade visual e cenas associadas a cada um dos 3 Atos.</summary>
public readonly struct ActTheme
{
    public readonly int Act;
    public readonly string Name;
    public readonly string NormalScenePath;
    public readonly string BossScenePath;
    public readonly Color CarpetModulate;
    public readonly Color RugModulate;
    public readonly Color WallModulate;
    public readonly Color NeonTrimColor;

    public ActTheme(
        int act,
        string name,
        string normalScenePath,
        string bossScenePath,
        Color carpetModulate,
        Color rugModulate,
        Color wallModulate,
        Color neonTrimColor)
    {
        Act = act;
        Name = name;
        NormalScenePath = normalScenePath;
        BossScenePath = bossScenePath;
        CarpetModulate = carpetModulate;
        RugModulate = rugModulate;
        WallModulate = wallModulate;
        NeonTrimColor = neonTrimColor;
    }
}

/// <summary>Especificação completa de uma das 15 fases oficiais da campanha (v4.5).</summary>
public readonly struct StageSpec
{
    public readonly int Stage;
    public readonly int Act;
    public readonly int StageInAct;
    public readonly ArenaKind Arena;
    public readonly int TotalMobs;
    public readonly float Duration;
    public readonly float HpScale;
    public readonly float DamageScale;
    public readonly int GoldenCount;
    public readonly int ChipsReward;
    public readonly int ClearBonusDollars;
    public readonly bool IsBossStage;
    public readonly BossKind Boss;
    public readonly float BossHp;
    public readonly float BossDpsCap;
    public readonly int BossChips;
    public readonly int BossDollars;
    public readonly bool RemovesSlotMachinesAfterClear;
    public readonly IReadOnlyList<EnemyKind> UnlockedEnemies;
    public readonly IReadOnlyList<EnemyWeight> SpawnWeights;
    public readonly string StageLabel;
    public readonly string ScenePath;

    public int RoomClearDollars => IsBossStage ? 0 : ClearBonusDollars;
    public int ExpectedRevenueDollars => (TotalMobs - GoldenCount) + GoldenCount * 5 + ClearBonusDollars;

    public StageSpec(
        int stage,
        int act,
        int stageInAct,
        ArenaKind arena,
        int totalMobs,
        float duration,
        float hpScale,
        float damageScale,
        int goldenCount,
        int chipsReward,
        int clearBonusDollars,
        BossKind boss,
        float bossHp,
        float bossDpsCap,
        int bossChips,
        int bossDollars,
        IReadOnlyList<EnemyKind> unlockedEnemies,
        IReadOnlyList<EnemyWeight> spawnWeights,
        string scenePath)
    {
        Stage = stage;
        Act = act;
        StageInAct = stageInAct;
        Arena = arena;
        TotalMobs = totalMobs;
        Duration = duration;
        HpScale = hpScale;
        DamageScale = damageScale;
        GoldenCount = goldenCount;
        ChipsReward = chipsReward;
        ClearBonusDollars = clearBonusDollars;
        IsBossStage = boss != BossKind.None;
        Boss = boss;
        BossHp = bossHp;
        BossDpsCap = bossDpsCap;
        BossChips = bossChips;
        BossDollars = bossDollars;
        RemovesSlotMachinesAfterClear = stage == 14;
        UnlockedEnemies = unlockedEnemies;
        SpawnWeights = spawnWeights;
        StageLabel = $"ATO {act} — FASE {stageInAct}/{CampaignRules.StagesPerAct}";
        ScenePath = scenePath;
    }
}

/// <summary>
/// Curva oficial das 15 fases (3 Atos x 5), introdução em escada dos inimigos, temas dos 3 Atos
/// e validação geométrica das arenas normais (Blockout 1) e de Boss (Blockout 2).
/// Classe C# pura testada no SelfCheck.
/// </summary>
public static class CampaignRules
{
    public const int TotalStages = 15;
    public const int ActsCount = 3;
    public const int StagesPerAct = 5;

    public const float CameraWidthUnits = 32f;
    public const float CameraHeightUnits = 18f;
    public const float MinCorridorUnits = 2.8f; // >= 3.5x largura do jogador (0.8u)
    public const float MinDoorFrontClearUnits = 3.0f;

    public const string BossScenePath = "res://scenes/levels/ArenaBoss.tscn";

    static readonly ActTheme[] Themes =
    {
        new ActTheme(
            1,
            "Salão das Fichas",
            "res://scenes/levels/Fase1.tscn",
            BossScenePath,
            new Color(1f, 1f, 1f, 1f),
            new Color(1f, 1f, 1f, 1f),
            new Color(0.16f, 0.06f, 0.2f, 1f),
            new Color(1f, 0.2f, 0.7f, 1f)),
        new ActTheme(
            2,
            "Mesa High Stakes",
            "res://scenes/levels/ArenaAto2.tscn",
            BossScenePath,
            new Color(0.32f, 0.68f, 0.45f, 1f),
            new Color(0.92f, 0.76f, 0.28f, 1f),
            new Color(0.06f, 0.14f, 0.14f, 1f),
            new Color(1f, 0.78f, 0.18f, 1f)),
        new ActTheme(
            3,
            "Sala VIP",
            "res://scenes/levels/ArenaAto3.tscn",
            BossScenePath,
            new Color(0.48f, 0.26f, 0.66f, 1f),
            new Color(1f, 0.78f, 0.26f, 1f),
            new Color(0.10f, 0.05f, 0.18f, 1f),
            new Color(0.25f, 0.92f, 1f, 1f)),
    };

    static readonly EnemyKind[] PoolF1ToF2 =
        { EnemyKind.Ficha, EnemyKind.Fichona, EnemyKind.Champanhe };
    static readonly EnemyKind[] PoolF3 =
        { EnemyKind.Ficha, EnemyKind.Fichona, EnemyKind.Champanhe, EnemyKind.CartaEspadas };
    static readonly EnemyKind[] PoolF4ToF5 =
        { EnemyKind.Ficha, EnemyKind.Fichona, EnemyKind.Champanhe, EnemyKind.CartaEspadas, EnemyKind.CartaOuros };
    static readonly EnemyKind[] PoolF6 =
        { EnemyKind.Ficha, EnemyKind.Fichona, EnemyKind.Champanhe, EnemyKind.CartaEspadas, EnemyKind.CartaOuros, EnemyKind.CartaPaus };
    static readonly EnemyKind[] PoolF7ToF15 =
        { EnemyKind.Ficha, EnemyKind.Fichona, EnemyKind.Champanhe, EnemyKind.CartaEspadas, EnemyKind.CartaOuros, EnemyKind.CartaPaus, EnemyKind.CartaCopas };

    static readonly StageSpec[] Stages = BuildStages();

    static StageSpec[] BuildStages() => new[]
    {
        MakeNormal(1, 25, 55f, 1.00f, 1.00f, 2, 10, PoolF1ToF2, new[]
        {
            W(EnemyKind.Ficha, 62), W(EnemyKind.Fichona, 18), W(EnemyKind.Champanhe, 20),
        }),
        MakeNormal(2, 30, 58f, 1.08f, 1.03f, 2, 10, PoolF1ToF2, new[]
        {
            W(EnemyKind.Ficha, 56), W(EnemyKind.Fichona, 22), W(EnemyKind.Champanhe, 22),
        }),
        MakeNormal(3, 34, 60f, 1.16f, 1.06f, 2, 10, PoolF3, new[]
        {
            W(EnemyKind.Ficha, 46), W(EnemyKind.Fichona, 18), W(EnemyKind.Champanhe, 18), W(EnemyKind.CartaEspadas, 18),
        }),
        MakeNormal(4, 38, 62f, 1.25f, 1.10f, 2, 10, PoolF4ToF5, new[]
        {
            W(EnemyKind.Ficha, 38), W(EnemyKind.Fichona, 15), W(EnemyKind.Champanhe, 15), W(EnemyKind.CartaEspadas, 16), W(EnemyKind.CartaOuros, 16),
        }),
        MakeBoss(5, 18, 24f, 1.25f, 1.10f, BossKind.Rainha, 3200f, 55f, 4, 75, PoolF4ToF5, new[]
        {
            W(EnemyKind.Ficha, 36), W(EnemyKind.Fichona, 14), W(EnemyKind.Champanhe, 16), W(EnemyKind.CartaEspadas, 18), W(EnemyKind.CartaOuros, 16),
        }),
        MakeNormal(6, 40, 60f, 1.40f, 1.15f, 2, 20, PoolF6, new[]
        {
            W(EnemyKind.Ficha, 32), W(EnemyKind.Fichona, 13), W(EnemyKind.Champanhe, 14), W(EnemyKind.CartaEspadas, 13), W(EnemyKind.CartaOuros, 13), W(EnemyKind.CartaPaus, 15),
        }),
        MakeNormal(7, 44, 60f, 1.52f, 1.18f, 3, 20, PoolF7ToF15, new[]
        {
            W(EnemyKind.Ficha, 28), W(EnemyKind.Fichona, 12), W(EnemyKind.Champanhe, 12), W(EnemyKind.CartaEspadas, 12), W(EnemyKind.CartaOuros, 12), W(EnemyKind.CartaPaus, 12), W(EnemyKind.CartaCopas, 12),
        }),
        MakeNormal(8, 47, 62f, 1.65f, 1.22f, 2, 20, PoolF7ToF15, new[]
        {
            W(EnemyKind.Ficha, 22), W(EnemyKind.Fichona, 11), W(EnemyKind.Champanhe, 13), W(EnemyKind.CartaEspadas, 14), W(EnemyKind.CartaOuros, 14), W(EnemyKind.CartaPaus, 14), W(EnemyKind.CartaCopas, 12),
        }),
        MakeNormal(9, 50, 64f, 1.80f, 1.25f, 3, 20, PoolF7ToF15, new[]
        {
            W(EnemyKind.Ficha, 20), W(EnemyKind.Fichona, 12), W(EnemyKind.Champanhe, 14), W(EnemyKind.CartaEspadas, 14), W(EnemyKind.CartaOuros, 14), W(EnemyKind.CartaPaus, 14), W(EnemyKind.CartaCopas, 12),
        }),
        MakeBoss(10, 24, 30f, 1.80f, 1.25f, BossKind.Rei, 6800f, 105f, 5, 125, PoolF7ToF15, new[]
        {
            W(EnemyKind.Ficha, 20), W(EnemyKind.Fichona, 12), W(EnemyKind.Champanhe, 12), W(EnemyKind.CartaEspadas, 14), W(EnemyKind.CartaOuros, 14), W(EnemyKind.CartaPaus, 16), W(EnemyKind.CartaCopas, 12),
        }),
        MakeNormal(11, 52, 60f, 1.95f, 1.30f, 3, 30, PoolF7ToF15, new[]
        {
            W(EnemyKind.Ficha, 20), W(EnemyKind.Fichona, 12), W(EnemyKind.Champanhe, 14), W(EnemyKind.CartaEspadas, 14), W(EnemyKind.CartaOuros, 14), W(EnemyKind.CartaPaus, 14), W(EnemyKind.CartaCopas, 12),
        }),
        MakeNormal(12, 55, 62f, 2.10f, 1.35f, 3, 30, PoolF7ToF15, new[]
        {
            W(EnemyKind.Ficha, 16), W(EnemyKind.Fichona, 11), W(EnemyKind.Champanhe, 19), W(EnemyKind.CartaEspadas, 14), W(EnemyKind.CartaOuros, 14), W(EnemyKind.CartaPaus, 14), W(EnemyKind.CartaCopas, 12),
        }),
        MakeNormal(13, 58, 64f, 2.25f, 1.40f, 3, 30, PoolF7ToF15, new[]
        {
            W(EnemyKind.Ficha, 22), W(EnemyKind.Fichona, 10), W(EnemyKind.Champanhe, 16), W(EnemyKind.CartaEspadas, 18), W(EnemyKind.CartaOuros, 12), W(EnemyKind.CartaPaus, 10), W(EnemyKind.CartaCopas, 12),
        }),
        MakeNormal(14, 60, 65f, 2.40f, 1.45f, 3, 30, PoolF7ToF15, new[]
        {
            W(EnemyKind.Ficha, 16), W(EnemyKind.Fichona, 13), W(EnemyKind.Champanhe, 16), W(EnemyKind.CartaEspadas, 15), W(EnemyKind.CartaOuros, 14), W(EnemyKind.CartaPaus, 14), W(EnemyKind.CartaCopas, 12),
        }),
        MakeBoss(15, 28, 35f, 2.40f, 1.45f, BossKind.MaquinaCacaNiquel, 14000f, 180f, 0, 250, PoolF7ToF15, new[]
        {
            W(EnemyKind.Ficha, 16), W(EnemyKind.Fichona, 12), W(EnemyKind.Champanhe, 18), W(EnemyKind.CartaEspadas, 15), W(EnemyKind.CartaOuros, 14), W(EnemyKind.CartaPaus, 13), W(EnemyKind.CartaCopas, 12),
        }),
    };

    static EnemyWeight W(EnemyKind kind, int weight) => new EnemyWeight(kind, weight);

    static StageSpec MakeNormal(
        int stage,
        int totalMobs,
        float duration,
        float hpScale,
        float damageScale,
        int goldens,
        int clearBonusDollars,
        EnemyKind[] unlocked,
        EnemyWeight[] weights)
    {
        int act = (stage - 1) / StagesPerAct + 1;
        int stageInAct = (stage - 1) % StagesPerAct + 1;
        var theme = GetActTheme(act);
        return new StageSpec(
            stage,
            act,
            stageInAct,
            ArenaKind.Normal,
            totalMobs,
            duration,
            hpScale,
            damageScale,
            goldens,
            goldens,
            clearBonusDollars,
            BossKind.None,
            0f,
            0f,
            0,
            0,
            unlocked,
            weights,
            theme.NormalScenePath);
    }

    static StageSpec MakeBoss(
        int stage,
        int totalMobs,
        float duration,
        float hpScale,
        float damageScale,
        BossKind boss,
        float bossHp,
        float bossDpsCap,
        int bossChips,
        int bossDollars,
        EnemyKind[] unlocked,
        EnemyWeight[] weights)
    {
        int act = (stage - 1) / StagesPerAct + 1;
        int stageInAct = (stage - 1) % StagesPerAct + 1;
        var theme = GetActTheme(act);
        return new StageSpec(
            stage,
            act,
            stageInAct,
            ArenaKind.Boss,
            totalMobs,
            duration,
            hpScale,
            damageScale,
            0,
            bossChips,
            bossDollars,
            boss,
            bossHp,
            bossDpsCap,
            bossChips,
            bossDollars,
            unlocked,
            weights,
            theme.BossScenePath);
    }

    public static bool IsBossStage(int stage) => stage >= 1 && stage <= TotalStages && stage % StagesPerAct == 0;

    public static StageSpec ForStage(int stage)
    {
        int clamped = Mathf.Clamp(stage, 1, TotalStages);
        return Stages[clamped - 1];
    }

    public static ActTheme GetActTheme(int act)
    {
        int clamped = Mathf.Clamp(act, 1, ActsCount);
        return Themes[clamped - 1];
    }

    /// <summary>Sorteia um inimigo respeitando os pesos oficiais da fase.</summary>
    public static EnemyKind PickEnemyKind(StageSpec spec, RandomNumberGenerator rng)
    {
        var weights = spec.SpawnWeights;
        if (weights == null || weights.Count == 0) return EnemyKind.Ficha;

        int total = 0;
        for (int i = 0; i < weights.Count; i++) total += Mathf.Max(0, weights[i].Weight);
        if (total <= 0) return weights[0].Kind;

        int roll = rng.RandiRange(0, total - 1);
        int acc = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            acc += Mathf.Max(0, weights[i].Weight);
            if (roll < acc) return weights[i].Kind;
        }
        return weights[weights.Count - 1].Kind;
    }

    /// <summary>Valida métricas espaciais, 4 portas, 4 pilares e vãos mínimos da Arena de Boss (Blockout 2).</summary>
    public static bool ValidateBossArenaLayout(
        Vector2 halfExtentsUnits,
        Vector2 playerSpawnUnits,
        Vector2 exitUnits,
        Vector2 bossSpawnUnits,
        IReadOnlyList<Vector2> doorPositionsUnits,
        IReadOnlyList<Vector2> pillarPositionsUnits,
        IReadOnlyList<Rect2> solidRectsPx,
        out string error)
    {
        float width = halfExtentsUnits.X * 2f;
        float height = halfExtentsUnits.Y * 2f;
        if (width < CameraWidthUnits * 1.6f || width > CameraWidthUnits * 1.8f)
        {
            error = $"Largura da Arena de Boss fora de 1.6x–1.8x câmera ({width:0.0}u)";
            return false;
        }
        if (height < CameraHeightUnits * 1.3f || height > CameraHeightUnits * 1.4f)
        {
            error = $"Altura da Arena de Boss fora de 1.3x–1.4x câmera ({height:0.0}u)";
            return false;
        }
        if (doorPositionsUnits.Count != 4)
        {
            error = $"Arena de Boss deve ter 4 portas laterais (encontradas: {doorPositionsUnits.Count})";
            return false;
        }

        int leftDoors = 0, rightDoors = 0;
        for (int i = 0; i < doorPositionsUnits.Count; i++)
        {
            if (doorPositionsUnits[i].X < 0f) leftDoors++;
            else if (doorPositionsUnits[i].X > 0f) rightDoors++;
        }
        if (leftDoors != 2 || rightDoors != 2)
        {
            error = $"Arena de Boss deve ter 2 portas à esquerda e 2 à direita ({leftDoors}/{rightDoors})";
            return false;
        }

        if (pillarPositionsUnits.Count != 4)
        {
            error = $"Arena de Boss deve ter 4 pilares centrais (encontrados: {pillarPositionsUnits.Count})";
            return false;
        }
        bool nw = false, ne = false, sw = false, se = false;
        for (int i = 0; i < pillarPositionsUnits.Count; i++)
        {
            Vector2 p = pillarPositionsUnits[i];
            if (Mathf.Abs(p.X) < 6f)
            {
                error = $"Pilar {i} invade o corredor central (|x| = {Mathf.Abs(p.X):0.0}u < 6u)";
                return false;
            }
            if (p.X < 0f && p.Y < 0f) nw = true;
            else if (p.X > 0f && p.Y < 0f) ne = true;
            else if (p.X < 0f && p.Y > 0f) sw = true;
            else if (p.X > 0f && p.Y > 0f) se = true;
        }
        if (!nw || !ne || !sw || !se)
        {
            error = "Os 4 pilares do Blockout 2 devem ocupar os 4 quadrantes";
            return false;
        }

        if (playerSpawnUnits.Y <= 4f || Mathf.Abs(playerSpawnUnits.X) > 2f)
        {
            error = "Spawn [J] da Arena de Boss fora da base central";
            return false;
        }
        if (bossSpawnUnits.Y >= -4f || Mathf.Abs(bossSpawnUnits.X) > 2f)
        {
            error = "Marcador [BOSS] fora da zona superior central";
            return false;
        }
        if (exitUnits.Y >= -halfExtentsUnits.Y + 0.5f || Mathf.Abs(exitUnits.X) > 2f)
        {
            error = "Saída da Arena de Boss fora do topo central";
            return false;
        }

        return ValidateSolidsAndDoorClearance(doorPositionsUnits, solidRectsPx, out error);
    }

    /// <summary>Valida métricas espaciais, 6 portas e vãos mínimos das arenas normais (Blockout 1).</summary>
    public static bool ValidateNormalArenaLayout(
        Vector2 halfExtentsUnits,
        Vector2 playerSpawnUnits,
        Vector2 exitUnits,
        IReadOnlyList<Vector2> doorPositionsUnits,
        IReadOnlyList<Rect2> solidRectsPx,
        out string error)
    {
        float width = halfExtentsUnits.X * 2f;
        float height = halfExtentsUnits.Y * 2f;
        if (width < 40f || height < 40f)
        {
            error = $"Arena normal muito pequena ({width:0.0}x{height:0.0}u)";
            return false;
        }
        if (doorPositionsUnits.Count != 6)
        {
            error = $"Arena normal deve ter 6 portas laterais (encontradas: {doorPositionsUnits.Count})";
            return false;
        }

        int leftDoors = 0, rightDoors = 0;
        for (int i = 0; i < doorPositionsUnits.Count; i++)
        {
            if (doorPositionsUnits[i].X < 0f) leftDoors++;
            else if (doorPositionsUnits[i].X > 0f) rightDoors++;
        }
        if (leftDoors != 3 || rightDoors != 3)
        {
            error = $"Arena normal deve ter 3 portas à esquerda e 3 à direita ({leftDoors}/{rightDoors})";
            return false;
        }

        if (playerSpawnUnits.Y <= halfExtentsUnits.Y * 0.5f || Mathf.Abs(playerSpawnUnits.X) > 2f)
        {
            error = "Spawn [J] da arena normal fora da base central";
            return false;
        }
        if (exitUnits.Y >= -halfExtentsUnits.Y + 0.5f || Mathf.Abs(exitUnits.X) > 2f)
        {
            error = "Saída da arena normal fora do topo central";
            return false;
        }

        return ValidateSolidsAndDoorClearance(doorPositionsUnits, solidRectsPx, out error);
    }

    static bool ValidateSolidsAndDoorClearance(
        IReadOnlyList<Vector2> doorPositionsUnits,
        IReadOnlyList<Rect2> solidRectsPx,
        out string error)
    {
        float minGapPx = MinCorridorUnits * Units.Px;
        for (int i = 0; i < solidRectsPx.Count; i++)
        {
            for (int j = i + 1; j < solidRectsPx.Count; j++)
            {
                float dist = RectDistance(solidRectsPx[i], solidRectsPx[j]);
                if (dist < minGapPx)
                {
                    error = $"Vão insuficiente entre obstáculos {i} e {j}: {dist / Units.Px:0.00}u < {MinCorridorUnits:0.00}u";
                    return false;
                }
            }
        }

        float minDoorClearPx = MinDoorFrontClearUnits * Units.Px;
        for (int d = 0; d < doorPositionsUnits.Count; d++)
        {
            Vector2 doorPx = doorPositionsUnits[d] * Units.Px;
            for (int s = 0; s < solidRectsPx.Count; s++)
            {
                float dist = PointToRectDistance(doorPx, solidRectsPx[s]);
                if (dist < minDoorClearPx)
                {
                    error = $"Obstáculo {s} bloqueia a frente da porta {d + 1}: {dist / Units.Px:0.00}u < {MinDoorFrontClearUnits:0.00}u";
                    return false;
                }
            }
        }

        error = string.Empty;
        return true;
    }

    static float RectDistance(Rect2 a, Rect2 b)
    {
        float dx = Mathf.Max(0f, Mathf.Max(a.Position.X - b.End.X, b.Position.X - a.End.X));
        float dy = Mathf.Max(0f, Mathf.Max(a.Position.Y - b.End.Y, b.Position.Y - a.End.Y));
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    static float PointToRectDistance(Vector2 p, Rect2 r)
    {
        float dx = Mathf.Max(0f, Mathf.Max(r.Position.X - p.X, p.X - r.End.X));
        float dy = Mathf.Max(0f, Mathf.Max(r.Position.Y - p.Y, p.Y - r.End.Y));
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
}
