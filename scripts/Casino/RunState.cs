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
    /// <summary>Recarga restante do Blefe Perfeito (0 = próximo dash imune a contato).</summary>
    public static float PerfectBluffCooldownLeft { get; private set; }
    public static bool IsPerfectBluffReady => Has(Passive.BlefePerfeito) && PerfectBluffCooldownLeft <= 0f;
    /// <summary>Contador de inimigos comuns abatidos para Troco em Sangue (cura 5% a cada 20).</summary>
    public static int BloodChangeKillCount { get; private set; }

    public static void Reset()
    {
        Array.Clear(drinkTimers, 0, drinkTimers.Length);
        Array.Clear(bonuses, 0, bonuses.Length);
        passives.Clear();
        SavedHp = null;
        SavedWeapons.Clear();
        SavedSelectedWeapon = 0;
        PerfectBluffCooldownLeft = 0f;
        BloodChangeKillCount = 0;
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
        if (PerfectBluffCooldownLeft > 0f)
            PerfectBluffCooldownLeft = Math.Max(0f, PerfectBluffCooldownLeft - dt);
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
        if (!passives.Add(p)) return;
        if (p == Passive.BlefePerfeito) PerfectBluffCooldownLeft = 0f;
        Changed?.Invoke();
    }

    public static void RemovePassive(Passive p)
    {
        if (passives.Remove(p)) Changed?.Invoke();
    }

    /// <summary>Blefe Perfeito: consome a carga pronta ao iniciar o dash e inicia recarga de 15s.</summary>
    public static bool TryConsumePerfectBluff()
    {
        if (!IsPerfectBluffReady) return false;
        PerfectBluffCooldownLeft = PassiveRules.PerfectBluffCooldown;
        return true;
    }

    /// <summary>Troco em Sangue: 3% por Dourado e 5% a cada 20 inimigos comuns abatidos.</summary>
    public static float RegisterKillForBloodChange(bool isGolden)
    {
        if (!Has(Passive.TrocoEmSangue)) return 0f;
        if (isGolden) return PassiveRules.BloodChangeGoldenHeal;
        BloodChangeKillCount++;
        if (BloodChangeKillCount < PassiveRules.BloodChangeCommonKills) return 0f;
        BloodChangeKillCount -= PassiveRules.BloodChangeCommonKills;
        return PassiveRules.BloodChangeCommonHeal;
    }

    /// <summary>Sangue Frio: até +35% de velocidade e cadência conforme o HP do jogador cai.</summary>
    public static float ColdBloodBonus
    {
        get
        {
            if (!Has(Passive.SangueFrio)) return 0f;
            var p = PlayerController.Instance;
            if (p == null || !Godot.GodotObject.IsInstanceValid(p)) return 0f;
            return PassiveRules.ColdBloodBonus(p.CurrentHp, p.MaxHp);
        }
    }

    // ---------- Derivados lidos pelo combate ----------

    /// <summary>Soma aditiva da fórmula do PDF: DanoBase × (1 + Nível + Arsenal + MelhoriasDano + BebidaDano).</summary>
    public static float DamageBonus =>
        Bonus(UpgradeStat.Damage) + (IsActive(DrinkType.Damage) ? DrinkRules.DamageBase * Efficiency : 0f);

    public static float FireRateMultiplierFor(float coldBloodBonus) =>
        1f + (IsActive(DrinkType.Speed) ? DrinkRules.FireRateBase * Efficiency : 0f) + Math.Max(0f, coldBloodBonus);

    public static float MoveSpeedMultiplierFor(float coldBloodBonus) => Math.Min(MaxMoveSpeedMultiplier,
        1f + Bonus(UpgradeStat.MoveSpeed) + (IsActive(DrinkType.Speed) ? DrinkRules.MoveBase * Efficiency : 0f) + Math.Max(0f, coldBloodBonus));

    public static float FireRateMultiplier => FireRateMultiplierFor(ColdBloodBonus);
    public static float MoveSpeedMultiplier => MoveSpeedMultiplierFor(ColdBloodBonus);

    public static float DashCooldownMultiplier => Math.Max(MinTimeMultiplier, 1f - Bonus(UpgradeStat.DashCooldown));
    public static float MagazineMultiplier => 1f + Bonus(UpgradeStat.Magazine);
    public static float ReloadMultiplier => Math.Max(MinTimeMultiplier, 1f - Bonus(UpgradeStat.Reload));
    public static float MaxHpBonus => Bonus(UpgradeStat.MaxHp);
}
