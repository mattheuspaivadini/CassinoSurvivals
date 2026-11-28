using System;

/// <summary>
/// Parâmetros numéricos e regras puras das 12 Passivas Únicas da Mesa de Roleta
/// (PDF, seção 6 e Balanceamento Numérico, pág. 1). Sem dependência de cena para facilitar testes no SelfCheck.
/// </summary>
public static class PassiveRules
{
    // 1. Calibre Pesado
    public const int CalibrePesadoExtraPierce = 1;

    // 2. Efeito Dominó
    public const float DominoRadiusUnits = 1.85f;
    public const float DominoDamageFraction = 0.35f;
    public const float DominoMinDamage = 18f;
    public const float DominoMaxDamage = 80f;

    // 3. Gelo no Copo
    public const float SlowSpeedMultiplier = 0.70f; // -30% velocidade
    public const float SlowDuration = 1.5f;

    // 4. Mão Quente (acumulador em ticks de 0,25s–0,5s)
    public const float BurnDamageFraction = 0.30f;
    public const float BurnDuration = 1.5f;
    public const float BurnTickInterval = 0.30f;

    // 5. All-In
    public const float AllInKnockbackSpeedPx = 260f;

    // 6. Saque Rápido
    public const float QuickDrawDelay = 2.0f;

    // 7. Virar a Mesa
    public const float FlipTableRadiusUnits = 3.0f;
    public const float FlipTableKnockbackSpeedPx = 520f;

    // 8. Blefe Perfeito
    public const float PerfectBluffCooldown = 15.0f;

    // 10. Troco em Sangue
    public const int BloodChangeCommonKills = 20;
    public const float BloodChangeCommonHeal = 0.05f;
    public const float BloodChangeGoldenHeal = 0.03f;

    // 11. 777 Crítico
    public const float CritChance = 0.20f;
    public const float CritMultiplier = 2.0f;

    // 12. Sangue Frio
    public const float ColdBloodMaxBonus = 0.35f;

    public static int ExtraPierce(bool hasCalibrePesado) => hasCalibrePesado ? CalibrePesadoExtraPierce : 0;

    public static float DominoDamage(float victimMaxHp) =>
        Math.Clamp(victimMaxHp * DominoDamageFraction, DominoMinDamage, DominoMaxDamage);

    /// <summary>Ganho progressivo de 0% (HP cheio) até +35% conforme o HP atual cai.</summary>
    public static float ColdBloodBonus(float currentHp, float maxHp)
    {
        if (maxHp <= 0f) return 0f;
        float missing = Math.Clamp(1f - currentHp / maxHp, 0f, 1f);
        return ColdBloodMaxBonus * missing;
    }

    /// <summary>
    /// Acumulador de Queimadura (Mão Quente): novos acertos somam +30% do dano do disparo ao saldo
    /// restante e reiniciam os 1,5s, drenando em ticks de 0,30s sem perder dano em armas rápidas.
    /// </summary>
    public struct BurnState
    {
        public float RemainingDamage { get; private set; }
        public float DurationRemaining { get; private set; }
        float tickTimer;

        public bool IsActive => RemainingDamage > 0.0001f;

        public void Clear()
        {
            RemainingDamage = 0f;
            DurationRemaining = 0f;
            tickTimer = 0f;
        }

        public void Apply(float shotDamage)
        {
            if (shotDamage <= 0f) return;
            bool wasActive = IsActive;
            RemainingDamage += shotDamage * BurnDamageFraction;
            DurationRemaining = BurnDuration;
            if (!wasActive) tickTimer = BurnTickInterval;
        }

        /// <summary>Avança o tempo e retorna o dano de queimadura a aplicar neste frame (0 se não houve tick).</summary>
        public float Tick(float dt)
        {
            if (!IsActive || dt <= 0f) return 0f;

            DurationRemaining = Math.Max(0f, DurationRemaining - dt);
            tickTimer -= dt;

            if (DurationRemaining <= 0f)
            {
                float finalDmg = RemainingDamage;
                Clear();
                return finalDmg;
            }
            if (tickTimer > 0f) return 0f;

            float drained = 0f;
            while (tickTimer <= 0f && RemainingDamage > 0.0001f)
            {
                tickTimer += BurnTickInterval;
                int ticksLeft = Math.Max(1, (int)Math.Ceiling((DurationRemaining - 0.0001f) / BurnTickInterval) + 1);
                float chunk = RemainingDamage / ticksLeft;
                RemainingDamage = Math.Max(0f, RemainingDamage - chunk);
                drained += chunk;
            }
            if (RemainingDamage <= 0.0001f) Clear();
            return drained;
        }
    }
}
