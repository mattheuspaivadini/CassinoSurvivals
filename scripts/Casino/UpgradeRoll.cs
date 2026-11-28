using System;

public enum UpgradeTarget { Player, Weapon }

/// <summary>Os 3 primeiros são do Jogador, os 3 últimos da Arma (Rolo 2 do PDF).</summary>
public enum UpgradeStat { MaxHp, MoveSpeed, DashCooldown, Damage, Magazine, Reload }

public readonly struct UpgradeResult
{
    public readonly UpgradeTarget Target;
    public readonly UpgradeStat Stat;
    /// <summary>Valor literal do Rolo 3 (5 a 30).</summary>
    public readonly int Percent;

    public UpgradeResult(UpgradeTarget target, UpgradeStat stat, int percent)
    {
        Target = target;
        Stat = stat;
        Percent = percent;
    }
}

/// <summary>Caça-níquel de Melhorias (PDF, seção 5): Quem? (50/50) · O quê? (1 de 3) · Quanto? (Tabela B).</summary>
public static class UpgradeRoll
{
    public const int StatsPerTarget = 3;

    public static UpgradeResult Roll(int luck, Random rng)
    {
        var target = rng.Next(2) == 0 ? UpgradeTarget.Player : UpgradeTarget.Weapon;
        var stat = StatFor(target, rng.Next(StatsPerTarget));
        return new UpgradeResult(target, stat, LuckTables.RollUpgradePercent(luck, rng));
    }

    public static UpgradeStat StatFor(UpgradeTarget target, int index) =>
        (UpgradeStat)((target == UpgradeTarget.Player ? 0 : StatsPerTarget) + index);

    public static UpgradeTarget TargetOf(UpgradeStat stat) =>
        (int)stat < StatsPerTarget ? UpgradeTarget.Player : UpgradeTarget.Weapon;

    public static string TargetName(UpgradeTarget t) => t == UpgradeTarget.Player ? "JOGADOR" : "ARMA";

    public static string StatName(UpgradeStat s) => s switch
    {
        UpgradeStat.MaxHp => "HP MÁXIMO",
        UpgradeStat.MoveSpeed => "VELOCIDADE",
        UpgradeStat.DashCooldown => "RECARGA DO DASH",
        UpgradeStat.Damage => "DANO",
        UpgradeStat.Magazine => "BALAS NO PENTE",
        _ => "VEL. DE RECARGA",
    };
}
