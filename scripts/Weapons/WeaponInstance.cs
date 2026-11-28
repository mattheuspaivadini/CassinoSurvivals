using Godot;

/// <summary>Estado de runtime de uma arma equipada: nível, munição e timers.</summary>
public class WeaponInstance
{
    public readonly WeaponData Data;
    public readonly string ColorHex; // cache para o HUD não alocar a cada refresh

    /// <summary>PDF, seção 4: nível máximo por arma.</summary>
    public const int MaxLevel = 10;

    public int Level { get; private set; }
    public int Ammo { get; private set; }
    public bool IsGolden => Level >= MaxLevel;
    public bool IsReloading => reloadTimer > 0f;
    public float ReloadProgress => IsReloading ? 1f - reloadTimer / reloadDuration : 1f;
    /// <summary>Pente com o bônus de "Balas no Pente" do Caça-níquel de Melhorias.</summary>
    public int MagazineSize => Mathf.CeilToInt(Data.Magazine(Level) * RunState.MagazineMultiplier - 0.0001f);

    float fireTimer, reloadTimer, reloadDuration = 1f, holsterTimer;

    public WeaponInstance(WeaponData data, int level)
    {
        Data = data;
        ColorHex = data.ProjectileColor.ToHtml(false);
        Level = Mathf.Clamp(level, 1, MaxLevel);
        Ammo = MagazineSize;
    }

    /// <summary>Sobe níveis e recarrega o pente na hora (recompensa do caça-níquel).</summary>
    public void AddLevels(int levels)
    {
        Level = Mathf.Min(MaxLevel, Level + levels);
        Ammo = MagazineSize;
        reloadTimer = 0f;
        holsterTimer = 0f;
    }

    public void ResetHolsterTimer() => holsterTimer = 0f;

    /// <summary>
    /// Saque Rápido (passiva #6): arma no slot inativo recarrega automaticamente após 2s fora da mão.
    /// Retorna true no frame em que completa a recarga no coldre.
    /// </summary>
    public bool TickInactive(float dt, bool quickDrawActive)
    {
        if (!quickDrawActive || (!IsReloading && Ammo >= MagazineSize))
        {
            holsterTimer = 0f;
            return false;
        }

        holsterTimer += dt;
        if (holsterTimer < PassiveRules.QuickDrawDelay) return false;

        Ammo = MagazineSize;
        reloadTimer = 0f;
        holsterTimer = 0f;
        return true;
    }

    /// <summary>Avança timers. Retorna true se deve disparar neste frame.</summary>
    public bool Tick(float dt, bool hasTarget)
    {
        holsterTimer = 0f;
        if (reloadTimer > 0f)
        {
            reloadTimer -= dt;
            if (reloadTimer <= 0f) Ammo = MagazineSize;
            return false;
        }

        if (fireTimer > 0f) fireTimer -= dt;
        if (!hasTarget || fireTimer > 0f || Ammo <= 0) return false;

        fireTimer = Data.FireIntervalAt(Level) / RunState.FireRateMultiplier; // Marco Nv 5 + Bebida de Velocidade
        Ammo--;
        if (Ammo <= 0) reloadTimer = reloadDuration = Data.ReloadTime(Level) * RunState.ReloadMultiplier;
        return true;
    }
}
