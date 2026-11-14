using UnityEngine;

/// <summary>Dono único dos pools de projéteis, inimigos e drops.</summary>
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    public Projectile projectilePrefab;
    public Enemy enemyPrefab;
    public Drop dropPrefab;

    public int projectilePrewarm = 300, projectileMax = 1500;
    public int enemyPrewarm = 250, enemyMax = 600;
    public int dropPrewarm = 80, dropMax = 400;

    public PrefabPool<Projectile> Projectiles { get; private set; }
    public PrefabPool<Enemy> Enemies { get; private set; }
    public PrefabPool<Drop> Drops { get; private set; }

    void Awake()
    {
        Instance = this;
        Projectiles = new PrefabPool<Projectile>(projectilePrefab, Group("Projectiles"), projectilePrewarm, projectileMax);
        Enemies = new PrefabPool<Enemy>(enemyPrefab, Group("Enemies"), enemyPrewarm, enemyMax);
        Drops = new PrefabPool<Drop>(dropPrefab, Group("Drops"), dropPrewarm, dropMax);
    }

    Transform Group(string n)
    {
        var t = new GameObject(n).transform;
        t.SetParent(transform, false);
        return t;
    }
}
