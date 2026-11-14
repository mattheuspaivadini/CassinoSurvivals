using UnityEngine;

/// <summary>Definição estática de uma arma. Os valores por nível são calculados aqui; o estado de runtime fica em WeaponInstance.</summary>
[CreateAssetMenu(menuName = "Cassino Survivals/Weapon", fileName = "Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName = "Arma";
    public Color projectileColor = Color.white;

    [Header("Dano")]
    public float baseDamage = 10f;
    public float damagePerLevel = 4f;

    [Header("Pente")]
    public int baseMagazine = 8;
    [Tooltip("Balas extras por nível (fração acumula: 0.5 = +1 a cada 2 níveis).")]
    public float magazinePerLevel = 1f;

    [Header("Recarga")]
    public float baseReloadTime = 1.5f;
    [Tooltip("Multiplicador aplicado a cada nível (0.9 = 10% mais rápido por nível).")]
    public float reloadMultiplierPerLevel = 0.92f;
    public float minReloadTime = 0.25f;

    [Header("Disparo")]
    [Tooltip("Segundos entre tiros enquanto há balas.")]
    public float fireInterval = 0.25f;
    public int projectilesPerShot = 1;
    [Tooltip("Abertura total do leque em graus (só com mais de 1 projétil).")]
    public float spreadAngle = 0f;
    public float range = 10f;

    [Header("Projétil")]
    public float projectileSpeed = 15f;
    public float projectileLifetime = 1.2f;
    public float projectileScale = 0.3f;

    public float Damage(int level) => baseDamage + damagePerLevel * (level - 1);
    public int Magazine(int level) => baseMagazine + Mathf.FloorToInt(magazinePerLevel * (level - 1));
    public float ReloadTime(int level) =>
        Mathf.Max(minReloadTime, baseReloadTime * Mathf.Pow(reloadMultiplierPerLevel, level - 1));
}
