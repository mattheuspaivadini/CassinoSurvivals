using System;

/// <summary>
/// "Sorte nos Caça-níqueis" (PDF, seção 5). O tier/faixa é sorteado ANTES da animação e os rolos
/// só exibem o resultado. Vale apenas para os Caça-níqueis de Armas e de Melhorias.
/// </summary>
public static class LuckTables
{
    public const int MaxLevel = 5;

    // Tabela A — Caça-níquel de Armas: % de Tier 1x / 2x / 3x por Nível de Sorte (Nv 0..5).
    static readonly int[,] WeaponTiers =
    {
        { 70, 27, 3 },
        { 62, 33, 5 },
        { 54, 38, 8 },
        { 46, 43, 11 },
        { 38, 47, 15 },
        { 30, 50, 20 },
    };

    // Tabela B — Caça-níquel de Melhorias (Rolo 3): % de faixa Baixa / Média / Alta por Nível de Sorte.
    static readonly int[,] UpgradeBands =
    {
        { 60, 35, 5 },
        { 52, 39, 9 },
        { 44, 43, 13 },
        { 36, 46, 18 },
        { 28, 49, 23 },
        { 20, 50, 30 },
    };

    // Valores literais de cada faixa: Baixa 5/10 · Média 15/20 · Alta 25/30.
    static readonly int[,] BandValues = { { 5, 10 }, { 15, 20 }, { 25, 30 } };

    static int Clamp(int luck) => luck < 0 ? 0 : luck > MaxLevel ? MaxLevel : luck;

    /// <summary>tier 1..3.</summary>
    public static int WeaponTierPercent(int luck, int tier) => WeaponTiers[Clamp(luck), tier - 1];

    /// <summary>band 0 = Baixa, 1 = Média, 2 = Alta.</summary>
    public static int UpgradeBandPercent(int luck, int band) => UpgradeBands[Clamp(luck), band];

    /// <summary>Retorna 1, 2 ou 3 (quantas vezes a mesma arma aparece nos rolos).</summary>
    public static int RollWeaponTier(int luck, Random rng) => Pick(WeaponTiers, Clamp(luck), rng) + 1;

    /// <summary>Retorna o percentual literal do Rolo 3: 5, 10, 15, 20, 25 ou 30.</summary>
    public static int RollUpgradePercent(int luck, Random rng)
    {
        int band = Pick(UpgradeBands, Clamp(luck), rng);
        return BandValues[band, rng.Next(2)];
    }

    static int Pick(int[,] table, int row, Random rng)
    {
        int roll = rng.Next(100);
        for (int i = 0; i < 2; i++)
        {
            if (roll < table[row, i]) return i;
            roll -= table[row, i];
        }
        return 2;
    }
}
