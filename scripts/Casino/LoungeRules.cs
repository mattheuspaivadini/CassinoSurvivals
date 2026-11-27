using System.Collections.Generic;
using Godot;

/// <summary>Tipos das 4 estações de investimento de Fichas na Sala de Descanso (Blockout 3).</summary>
public enum LoungeStationType
{
    Bar,
    UpgradeSlot,
    WeaponSlot,
    Roulette,
}

/// <summary>
/// Regras puras da Sala de Descanso (PDF, seções 5 e 10 — Blockout 3):
/// custos por estação, limites por sala/run, remoção pós-Fase 14 e validação espacial.
/// </summary>
public static class LoungeRules
{
    public const int BarCost = 1;
    public const int UpgradeSlotCost = 1;
    public const int WeaponSlotCost = 2;
    public const int RouletteMinCost = 4;
    public const int RouletteMaxCost = 5;
    public const int RouletteDefaultCost = 4;

    public const int RouletteMaxPerRoom = 1;
    public const int RouletteMinRunLimit = 3;
    public const int RouletteMaxRunLimit = 4;
    public const int RouletteDefaultRunLimit = 4;

    public const int SlotMachinesRemovedAfterStage = 14;
    public const float MaxFixedScreenWidthUnits = 32f;
    public const float MaxFixedScreenHeightUnits = 18f;
    public const float MinCorridorUnits = 2.8f; // >= 3.5x largura do jogador (0.8u)

    public static int GetCost(LoungeStationType type, int rouletteCost = RouletteDefaultCost) => type switch
    {
        LoungeStationType.Bar => BarCost,
        LoungeStationType.UpgradeSlot => UpgradeSlotCost,
        LoungeStationType.WeaponSlot => WeaponSlotCost,
        LoungeStationType.Roulette => Mathf.Clamp(rouletteCost, RouletteMinCost, RouletteMaxCost),
        _ => 1,
    };

    /// <summary>0 = uso livre na sala.</summary>
    public static int GetMaxUsesPerRoom(LoungeStationType type) =>
        type == LoungeStationType.Roulette ? RouletteMaxPerRoom : 0;

    /// <summary>0 = uso livre na run.</summary>
    public static int GetMaxUsesPerRun(LoungeStationType type, int rouletteRunLimit = RouletteDefaultRunLimit) =>
        type == LoungeStationType.Roulette
            ? Mathf.Clamp(rouletteRunLimit, RouletteMinRunLimit, RouletteMaxRunLimit)
            : 0;

    /// <summary>Evento narrativo Fase 14 -> 15: Caça-níqueis de Armas e Melhorias somem antes do Boss Final.</summary>
    public static bool IsRemovedAfterStage14(LoungeStationType type) =>
        type == LoungeStationType.WeaponSlot || type == LoungeStationType.UpgradeSlot;

    public static bool IsAvailableInStage(LoungeStationType type, int clearedStage) =>
        !(IsRemovedAfterStage14(type) && clearedStage >= SlotMachinesRemovedAfterStage);

    public static bool CanUse(
        LoungeStationType type,
        int currentChips,
        int usesInCurrentRoom,
        int usesInRun,
        int clearedStage = 1,
        int rouletteCost = RouletteDefaultCost,
        int rouletteRunLimit = RouletteDefaultRunLimit)
    {
        if (!IsAvailableInStage(type, clearedStage)) return false;

        int maxRoom = GetMaxUsesPerRoom(type);
        if (maxRoom > 0 && usesInCurrentRoom >= maxRoom) return false;

        int maxRun = GetMaxUsesPerRun(type, rouletteRunLimit);
        if (maxRun > 0 && usesInRun >= maxRun) return false;

        return currentChips >= GetCost(type, rouletteCost);
    }

    public static string GetDisplayName(LoungeStationType type) => type switch
    {
        LoungeStationType.Bar => "BALCÃO DO BAR",
        LoungeStationType.UpgradeSlot => "CAÇA-NÍQUEL MELHORIAS",
        LoungeStationType.WeaponSlot => "CAÇA-NÍQUEL DE ARMAS",
        LoungeStationType.Roulette => "MESA DE ROLETA",
        _ => "ESTAÇÃO",
    };

    /// <summary>Valida as invariantes geométricas e de circulação do Blockout 3 em unidades e px.</summary>
    public static bool ValidateBlockoutLayout(
        Vector2 halfExtentsUnits,
        Vector2 playerSpawnUnits,
        Vector2 exitUnits,
        Vector2 weaponSlotUnits,
        Vector2 upgradeSlotUnits,
        Vector2 rouletteUnits,
        Vector2 barUnits,
        IReadOnlyList<Rect2> solidRectsPx,
        out string error)
    {
        if (halfExtentsUnits.X * 2f > MaxFixedScreenWidthUnits || halfExtentsUnits.Y * 2f > MaxFixedScreenHeightUnits)
        {
            error = $"Sala excede 1 tela fixa ({halfExtentsUnits.X * 2f}x{halfExtentsUnits.Y * 2f}u)";
            return false;
        }
        if (exitUnits.Y >= -5.5f || Mathf.Abs(exitUnits.X) > 2f)
        {
            error = "Porta superior fora do topo central";
            return false;
        }
        if (playerSpawnUnits.Y <= 4.5f || Mathf.Abs(playerSpawnUnits.X) > 2f)
        {
            error = "Spawn [J] fora da entrada inferior";
            return false;
        }
        if (weaponSlotUnits.X >= -1.5f || weaponSlotUnits.Y >= 0f)
        {
            error = "Caça-níquel de Armas fora do quadrante superior esquerdo";
            return false;
        }
        if (upgradeSlotUnits.X <= 1.5f || upgradeSlotUnits.Y >= 0f)
        {
            error = "Caça-níquel de Melhorias fora do quadrante superior direito";
            return false;
        }
        if (rouletteUnits.X >= -4f || rouletteUnits.Y <= 0f)
        {
            error = "Mesa de Roleta fora do quadrante inferior esquerdo";
            return false;
        }
        if (barUnits.X <= 4f || barUnits.Y <= 0f)
        {
            error = "Balcão do Bar fora do quadrante inferior direito";
            return false;
        }

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

        error = string.Empty;
        return true;
    }

    static float RectDistance(Rect2 a, Rect2 b)
    {
        float dx = Mathf.Max(0f, Mathf.Max(a.Position.X - b.End.X, b.Position.X - a.End.X));
        float dy = Mathf.Max(0f, Mathf.Max(a.Position.Y - b.End.Y, b.Position.Y - a.End.Y));
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
}
