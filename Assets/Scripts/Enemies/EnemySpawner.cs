using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawner central + registro dos inimigos ativos. Também responde às buscas de alvo/colisão
/// por sqrMagnitude (sem Physics2D.Overlap*, sem alocação).
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Tipos")]
    public EnemyData[] commonTypes;
    public EnemyData eliteType;
    public EnemyData bossType;

    [Header("Ritmo")]
    public float initialInterval = 1.0f;
    public float minInterval = 0.2f;
    [Tooltip("Segundos até o intervalo chegar ao mínimo.")]
    public float rampDuration = 240f;
    [Tooltip("A cada X segundos, +1 inimigo por leva.")]
    public float batchGrowthSeconds = 45f;
    public float eliteChance = 0.03f;
    public float eliteChancePerMinute = 0.01f;
    [Tooltip("HP extra por minuto de partida (0.5 = +50%/min).")]
    public float hpGrowthPerMinute = 0.4f;
    public int maxAlive = 400;

    [Header("Área")]
    public Vector2 mapHalfExtents = new Vector2(30f, 30f);
    public float spawnMargin = 2f;

    public event Action OnBossSpawned;
    public bool BossSpawned { get; private set; }

    readonly List<Enemy> active = new List<Enemy>(512);
    Camera cam;
    float timer;

    public int ActiveCount => active.Count;

    void Awake()
    {
        Instance = this;
        cam = Camera.main; // cache único, nunca no Update
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm.State != GameState.Playing || PlayerController.Instance == null) return;

        if (!BossSpawned && gm.IsBossTime) SpawnBoss();

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        float t = gm.Elapsed;
        timer = Mathf.Lerp(initialInterval, minInterval, t / rampDuration);
        int batch = 1 + (int)(t / batchGrowthSeconds);
        float hpMul = 1f + hpGrowthPerMinute * (t / 60f);
        float elite = eliteChance + eliteChancePerMinute * (t / 60f);

        for (int i = 0; i < batch && active.Count < maxAlive; i++)
        {
            var type = UnityEngine.Random.value < elite
                ? eliteType
                : commonTypes[UnityEngine.Random.Range(0, commonTypes.Length)];
            Spawn(type, RandomSpawnPosition(), hpMul);
        }
    }

    void SpawnBoss()
    {
        BossSpawned = true;
        Spawn(bossType, RandomSpawnPosition(), 1f);
        AudioManager.Instance?.PlayBossSpawn();
        OnBossSpawned?.Invoke();
    }

    public Enemy Spawn(EnemyData data, Vector2 pos, float hpMultiplier)
    {
        var e = PoolManager.Instance.Enemies.Get();
        e.Init(data, pos, hpMultiplier);
        e.activeIndex = active.Count;
        active.Add(e);
        return e;
    }

    public void Despawn(Enemy e)
    {
        int i = e.activeIndex;
        if (i < 0) return;
        int last = active.Count - 1;
        active[i] = active[last];
        active[i].activeIndex = i;
        active.RemoveAt(last);
        e.activeIndex = -1;
        PoolManager.Instance.Enemies.Release(e);
    }

    /// <summary>Spawna fora da visão da câmera, preso dentro do mapa.</summary>
    Vector2 RandomSpawnPosition()
    {
        float h = cam.orthographicSize, w = h * cam.aspect;
        float dist = Mathf.Sqrt(w * w + h * h) + spawnMargin;
        float a = UnityEngine.Random.value * Mathf.PI * 2f;
        Vector2 p = PlayerController.Instance.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * dist;
        p.x = Mathf.Clamp(p.x, -mapHalfExtents.x + 1f, mapHalfExtents.x - 1f);
        p.y = Mathf.Clamp(p.y, -mapHalfExtents.y + 1f, mapHalfExtents.y - 1f);
        return p;
    }

    public Enemy FindNearest(Vector2 from, float maxDistance, out float sqrDist)
    {
        Enemy best = null;
        sqrDist = maxDistance * maxDistance;
        for (int i = 0; i < active.Count; i++)
        {
            float d = ((Vector2)active[i].transform.position - from).sqrMagnitude;
            if (d < sqrDist) { sqrDist = d; best = active[i]; }
        }
        return best;
    }

    public Enemy FindHit(Vector2 point, float radius)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var e = active[i];
            float r = radius + e.Radius;
            if (((Vector2)e.transform.position - point).sqrMagnitude <= r * r) return e;
        }
        return null;
    }
}
