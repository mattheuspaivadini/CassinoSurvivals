using UnityEngine;

/// <summary>Estado de runtime de uma arma equipada: nível, munição e timers.</summary>
public class WeaponInstance
{
    public readonly WeaponData Data;
    public readonly string ColorHex; // cache para o HUD não alocar a cada refresh

    public int Level { get; private set; }
    public int Ammo { get; private set; }
    public bool IsReloading => reloadTimer > 0f;
    public float ReloadProgress => IsReloading ? 1f - reloadTimer / Data.ReloadTime(Level) : 1f;

    float fireTimer, reloadTimer;

    public WeaponInstance(WeaponData data, int level)
    {
        Data = data;
        ColorHex = ColorUtility.ToHtmlStringRGB(data.projectileColor);
        Level = Mathf.Max(1, level);
        Ammo = data.Magazine(Level);
    }

    /// <summary>Sobe níveis e recarrega o pente na hora (recompensa do caça-níquel).</summary>
    public void AddLevels(int levels)
    {
        Level += levels;
        Ammo = Data.Magazine(Level);
        reloadTimer = 0f;
    }

    /// <summary>Avança timers. Retorna true se deve disparar neste frame.</summary>
    public bool Tick(float dt, bool hasTarget)
    {
        if (reloadTimer > 0f)
        {
            reloadTimer -= dt;
            if (reloadTimer <= 0f) Ammo = Data.Magazine(Level);
            return false;
        }

        if (fireTimer > 0f) fireTimer -= dt;
        if (!hasTarget || fireTimer > 0f) return false;

        fireTimer = Data.fireInterval;
        Ammo--;
        if (Ammo <= 0) reloadTimer = Data.ReloadTime(Level);
        return true;
    }
}
