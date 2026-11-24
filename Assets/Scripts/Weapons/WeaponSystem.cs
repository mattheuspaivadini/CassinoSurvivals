using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Armas do jogador. Só a selecionada (scroll do mouse) atira, com mira automática no inimigo mais próximo.</summary>
public class WeaponSystem : MonoBehaviour
{
    public WeaponData[] startingWeapons;

    public readonly List<WeaponInstance> Weapons = new List<WeaponInstance>(8);
    public event Action OnWeaponsChanged;

    public int Selected { get; private set; }

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

        OnWeaponsChanged?.Invoke();
    }

    void Update()
    {
        if (GameManager.Instance.State != GameState.Playing || EnemySpawner.Instance == null) return;

        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0f && Weapons.Count > 1)
        {
            // Scroll para cima = arma anterior, para baixo = próxima.
            Selected = ((Selected + (scroll < 0f ? 1 : -1)) % Weapons.Count + Weapons.Count) % Weapons.Count;
            OnWeaponsChanged?.Invoke();
        }

        var w = Weapons[Selected];
        Vector2 pos = transform.position;
        Enemy target = EnemySpawner.Instance.FindNearest(pos, w.Data.range, out _);
        if (w.Tick(Time.deltaTime, target != null)) Fire(w, pos, target.transform.position);
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
