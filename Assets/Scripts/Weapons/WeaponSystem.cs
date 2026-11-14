using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Armas equipadas no jogador (várias ao mesmo tempo). Mira automática no inimigo mais próximo.</summary>
public class WeaponSystem : MonoBehaviour
{
    public WeaponData[] startingWeapons;

    public readonly List<WeaponInstance> Weapons = new List<WeaponInstance>(8);
    public event Action OnWeaponsChanged;

    float maxRange;

    void Start()
    {
        foreach (var w in startingWeapons) AddOrUpgrade(w, 1);
    }

    public WeaponInstance Find(WeaponData data)
    {
        for (int i = 0; i < Weapons.Count; i++)
            if (Weapons[i].Data == data) return Weapons[i];
        return null;
    }

    /// <summary>Se já tem a arma: +levels. Se não tem: desbloqueia no nível = levels.</summary>
    public void AddOrUpgrade(WeaponData data, int levels)
    {
        var w = Find(data);
        if (w == null) Weapons.Add(new WeaponInstance(data, levels));
        else w.AddLevels(levels);

        maxRange = 0f;
        for (int i = 0; i < Weapons.Count; i++) maxRange = Mathf.Max(maxRange, Weapons[i].Data.range);
        OnWeaponsChanged?.Invoke();
    }

    void Update()
    {
        if (GameManager.Instance.State != GameState.Playing || EnemySpawner.Instance == null) return;

        Vector2 pos = transform.position;
        // Uma busca por frame para todas as armas.
        Enemy target = EnemySpawner.Instance.FindNearest(pos, maxRange, out float sqrDist);
        float dt = Time.deltaTime;

        for (int i = 0; i < Weapons.Count; i++)
        {
            var w = Weapons[i];
            bool inRange = target != null && sqrDist <= w.Data.range * w.Data.range;
            if (w.Tick(dt, inRange)) Fire(w, pos, target.transform.position);
        }
    }

    void Fire(WeaponInstance w, Vector2 origin, Vector2 targetPos)
    {
        var d = w.Data;
        Vector2 dir = (targetPos - origin).normalized;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        int n = Mathf.Max(1, d.projectilesPerShot);
        float damage = d.Damage(w.Level);

        for (int i = 0; i < n; i++)
        {
            float offset = n == 1 ? 0f : d.spreadAngle * ((float)i / (n - 1) - 0.5f);
            float a = (baseAngle + offset) * Mathf.Deg2Rad;
            var v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d.projectileSpeed;
            PoolManager.Instance.Projectiles.Get()
                .Launch(origin, v, damage, d.projectileLifetime, d.projectileColor, d.projectileScale);
        }
        AudioManager.Instance?.PlayShoot();
    }
}
