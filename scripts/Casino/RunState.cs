using System;
using System.Collections.Generic;

/// <summary>
/// Tudo o que a run comprou na Sala de Descanso: bebidas ativas, melhorias permanentes da run e
/// passivas da Roleta. Estático para sobreviver à troca Arena ↔ Sala; zerado em GameManager.Restart.
/// O combate só lê os derivados (DamageBonus, FireRateMultiplier...).
/// </summary>
public static class RunState
{
    /// <summary>Teto de velocidade de movimento acumulada (PDF, seção 3: +60%).</summary>
    public const float MaxMoveSpeedMultiplier = 1.6f;
    const float MinTimeMultiplier = 0.2f;

    public static readonly Random Rng = new Random();

    /// <summary>Disparado quando uma melhoria/passiva muda algo que nós de cena cacheiam (ex.: HP máximo).</summary>
    public static event Action Changed;

    static readonly float[] drinkTimers = new float[DrinkRules.Count];
    static readonly float[] bonuses = new float[Enum.GetValues(typeof(UpgradeStat)).Length];
    static readonly HashSet<Passive> passives = new HashSet<Passive>();

    public static IReadOnlyCollection<Passive> Passives => passives;
    public static float? SavedHp { get; set; }
    public static readonly List<(string Path, int Level)> SavedWeapons = new List<(string, int)>();
    public static int SavedSelectedWeapon { get; set; }

    public static void Reset()
    {
        Array.Clear(drinkTimers, 0, drinkTimers.Length);
        Array.Clear(bonuses, 0, bonuses.Length);
        passives.Clear();
        SavedHp = null;
        SavedWeapons.Clear();
        SavedSelectedWeapon = 0;
        Changed?.Invoke();
    }

    /// <summary>Guarda HP atual e armas equipadas antes de trocar entre Arena e Sala de Descanso.</summary>
    public static void SavePlayerBuild()
    {
        var p = PlayerController.Instance;
        if (p == null || !Godot.GodotObject.IsInstanceValid(p)) return;
        SavedHp = p.CurrentHp;

        var ws = p.GetNodeOrNull<WeaponSystem>("WeaponSystem");
        if (ws == null || ws.Weapons.Count == 0) return;
        SavedWeapons.Clear();
        for (int i = 0; i < ws.Weapons.Count; i++)
        {
            var w = ws.Weapons[i];
            if (w.Data != null && !string.IsNullOrEmpty(w.Data.ResourcePath))
                SavedWeapons.Add((w.Data.ResourcePath, w.Level));
        }
        SavedSelectedWeapon = ws.Selected;
    }

    // ---------- Bebidas ----------

    public static float Efficiency => DrinkRules.Efficiency(MetaProgress.BarmanLevel, passives.Contains(Passive.OpenBar));

    public static float TimeLeft(DrinkType t) => drinkTimers[(int)t];
    public static bool IsActive(DrinkType t) => drinkTimers[(int)t] > 0f;

    /// <summary>Bebidas com duração: tipos diferentes acumulam; a mesma só reinicia os 30s.</summary>
    public static void StartDrink(DrinkType t)
    {
        if (DrinkRules.IsTimed(t)) drinkTimers[(int)t] = DrinkRules.Duration;
    }

    /// <summary>Efeito completo de beber (Bar ou drop): cura imediata ou inicia o buff.</summary>
    public static void Consume(DrinkType t)
    {
        AudioManager.Instance?.PlayDrink();
        if (t == DrinkType.Heal)
        {
            var p = PlayerController.Instance;
            if (p != null && Godot.GodotObject.IsInstanceValid(p)) p.Heal(p.MaxHp * DrinkRules.HealBase * Efficiency);
            return;
        }
        StartDrink(t);
    }

    /// <summary>Chamado pelo GameManager só com o jogo rodando (pausa e modais não gastam o buff).</summary>
    public static void Tick(float dt)
    {
        for (int i = 0; i < drinkTimers.Length; i++)
            if (drinkTimers[i] > 0f) drinkTimers[i] = Math.Max(0f, drinkTimers[i] - dt);
    }

    // ---------- Melhorias (Caça-níquel de Melhorias) ----------

    public static float Bonus(UpgradeStat s) => bonuses[(int)s];

    public static void ApplyUpgrade(UpgradeResult r)
    {
        bonuses[(int)r.Stat] += r.Percent / 100f;
        Changed?.Invoke();
    }

    // ---------- Passivas (Mesa de Roleta) ----------

    public static bool Has(Passive p) => passives.Contains(p);

    public static void AddPassive(Passive p)
    {
        if (passives.Add(p)) Changed?.Invoke();
    }

    // ---------- Derivados lidos pelo combate ----------

    /// <summary>Soma aditiva da fórmula do PDF: DanoBase × (1 + Nível + Arsenal + MelhoriasDano + BebidaDano).</summary>
    public static float DamageBonus =>
        Bonus(UpgradeStat.Damage) + (IsActive(DrinkType.Damage) ? DrinkRules.DamageBase * Efficiency : 0f);

    public static float FireRateMultiplier =>
        1f + (IsActive(DrinkType.Speed) ? DrinkRules.FireRateBase * Efficiency : 0f);

    public static float MoveSpeedMultiplier => Math.Min(MaxMoveSpeedMultiplier,
        1f + Bonus(UpgradeStat.MoveSpeed) + (IsActive(DrinkType.Speed) ? DrinkRules.MoveBase * Efficiency : 0f));

    public static float DashCooldownMultiplier => Math.Max(MinTimeMultiplier, 1f - Bonus(UpgradeStat.DashCooldown));
    public static float MagazineMultiplier => 1f + Bonus(UpgradeStat.Magazine);
    public static float ReloadMultiplier => Math.Max(MinTimeMultiplier, 1f - Bonus(UpgradeStat.Reload));
    public static float MaxHpBonus => Bonus(UpgradeStat.MaxHp);
}
