using Godot;

/// <summary>
/// Loja Externa do PDF (seção 7): dólares ($) acumulados entre partidas e upgrades permanentes.
/// Salvo em user://meta.cfg. Catálogo completo: 5 upgrades × 5 níveis.
/// </summary>
public static class MetaProgress
{
    public enum Upgrade { Vigor, Arsenal, Imortalidade, Barman, Sorte }

    public const int MaxLevel = 5;
    const string SavePath = "user://meta.cfg";

    public static readonly string[] Names = { "Vigor", "Arsenal", "Imortalidade", "Barman", "Sorte" };
    public static readonly string[] Descriptions =
    {
        "+6% de HP máximo por nível",
        "+5% de dano por nível",
        "Chance de reviver 1x por partida (10% a 50%)",
        "+8% de eficiência nas bebidas por nível",
        "Melhora as chances dos caça-níqueis",
    };
    // Custos do PDF por nível (Nv 1..5).
    static readonly int[][] Costs =
    {
        new[] { 100, 220, 400, 700, 1100 },
        new[] { 120, 250, 480, 800, 1300 },
        new[] { 180, 360, 650, 1100, 1750 },
        new[] { 100, 200, 350, 600, 950 },
        new[] { 150, 300, 550, 900, 1450 },
    };

    public static int Dollars { get; private set; }
    static readonly int[] levels = new int[Names.Length];
    static bool loaded;

    public static int Level(Upgrade u) { Load(); return levels[(int)u]; }

    /// <summary>Custo do próximo nível; -1 se já está no máximo.</summary>
    public static int NextCost(Upgrade u)
    {
        int l = Level(u);
        return l >= MaxLevel ? -1 : Costs[(int)u][l];
    }

    public static int CostOf(Upgrade u, int level) => Costs[(int)u][level];

    public static bool TryBuy(Upgrade u)
    {
        int cost = NextCost(u);
        if (cost < 0 || Dollars < cost) return false;
        Dollars -= cost;
        levels[(int)u]++;
        Save();
        return true;
    }

    /// <summary>Soma o ganho da partida ao saldo e salva.</summary>
    public static void Bank(int amount)
    {
        Load();
        Dollars += amount;
        Save();
    }

    public static float HpMultiplier => 1f + 0.06f * Level(Upgrade.Vigor);
    public static float DamageMultiplier => 1f + 0.05f * Level(Upgrade.Arsenal);
    public static float ReviveChance => 0.1f * Level(Upgrade.Imortalidade);
    /// <summary>Revive com 25% (Nv 1) até 50% (Nv 5) do HP.</summary>
    public static float ReviveHpFraction => 0.25f + 0.0625f * Mathf.Max(0, Level(Upgrade.Imortalidade) - 1);

    /// <summary>Linha das Tabelas A e B de Sorte (0..5). Único ponto de leitura: o Hardcore força 0 aqui.</summary>
    public static int LuckLevel => Level(Upgrade.Sorte);
    public static int BarmanLevel => Level(Upgrade.Barman);

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        var cfg = new ConfigFile();
        if (cfg.Load(SavePath) != Error.Ok) return;
        Dollars = (int)cfg.GetValue("meta", "dollars", 0);
        for (int i = 0; i < levels.Length; i++)
            levels[i] = Mathf.Clamp((int)cfg.GetValue("upgrades", Names[i], 0), 0, MaxLevel);
    }

    static void Save()
    {
        var cfg = new ConfigFile();
        cfg.SetValue("meta", "dollars", Dollars);
        for (int i = 0; i < levels.Length; i++) cfg.SetValue("upgrades", Names[i], levels[i]);
        cfg.Save(SavePath);
    }
}
