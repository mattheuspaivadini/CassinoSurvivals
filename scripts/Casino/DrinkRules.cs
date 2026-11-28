public enum DrinkType { Heal, Damage, Speed }

/// <summary>
/// Tabela Definitiva das Bebidas (PDF, seção 5). Valor Final = Valor Base × (1 + Bônus de Eficiência),
/// somando apenas Barman (Loja Externa, +8%/nv) e Open Bar (Roleta, +50%).
/// </summary>
public static class DrinkRules
{
    public const int Count = 3;
    public const float Duration = 30f;
    public const float HealBase = 0.35f;      // fração do HP máximo, imediata
    public const float DamageBase = 0.40f;
    public const float MoveBase = 0.25f;
    public const float FireRateBase = 0.30f;
    public const float BarmanPerLevel = 0.08f;
    public const float OpenBarBonus = 0.5f;

    public static float Efficiency(int barmanLevel, bool openBar) =>
        1f + BarmanPerLevel * barmanLevel + (openBar ? OpenBarBonus : 0f);

    public static bool IsTimed(DrinkType t) => t != DrinkType.Heal;

    public static string Name(DrinkType t) => t switch
    {
        DrinkType.Heal => "CURA",
        DrinkType.Damage => "DANO",
        _ => "VELOCIDADE",
    };

    public static string Describe(DrinkType t, float eff) => t switch
    {
        DrinkType.Heal => $"Recupera {Pct(HealBase * eff)} do HP",
        DrinkType.Damage => $"+{Pct(DamageBase * eff)} de dano por {Duration:0}s",
        _ => $"+{Pct(MoveBase * eff)} mov. e +{Pct(FireRateBase * eff)} cadência por {Duration:0}s",
    };

    static string Pct(float f) => $"{f * 100f:0.#}%";
}
