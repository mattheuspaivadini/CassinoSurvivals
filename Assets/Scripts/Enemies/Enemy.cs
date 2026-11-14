using UnityEngine;

/// <summary>Inimigo pooled: persegue o jogador via Rigidbody2D (empurrões entre a horda) e causa dano de contato por distância.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class Enemy : MonoBehaviour
{
    public EnemyData Data { get; private set; }
    public float Radius { get; private set; }
    public float HpRatio => hp / maxHp;

    /// <summary>Índice na lista ativa do EnemySpawner (remoção O(1) por swap).</summary>
    [System.NonSerialized] public int activeIndex = -1;

    Rigidbody2D rb;
    SpriteRenderer sr;
    float hp, maxHp, contactTimer, flashTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    public void Init(EnemyData data, Vector2 pos, float hpMultiplier)
    {
        Data = data;
        maxHp = hp = data.fixedHp ? data.maxHp : data.maxHp * hpMultiplier;
        transform.position = pos;
        transform.localScale = new Vector3(data.scale, data.scale, 1f);
        rb.position = pos;
        rb.linearVelocity = Vector2.zero;
        rb.mass = data.mass;
        if (data.sprite != null) sr.sprite = data.sprite;
        sr.color = data.color;
        sr.sortingOrder = data.isBoss ? 2 : 1;
        Radius = data.scale * 0.5f;
        contactTimer = 0f;
        flashTimer = 0f;
    }

    void FixedUpdate()
    {
        var player = PlayerController.Instance;
        if (player == null) return;

        Vector2 toPlayer = player.Position - rb.position;
        rb.linearVelocity = toPlayer.normalized * Data.moveSpeed;

        contactTimer -= Time.fixedDeltaTime;
        float reach = Radius + player.radius + 0.1f;
        if (contactTimer <= 0f && toPlayer.sqrMagnitude <= reach * reach)
        {
            player.TakeDamage(Data.contactDamage);
            contactTimer = Data.contactCooldown;
        }
    }

    void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f) sr.color = Data.color;
        }
    }

    public void TakeDamage(float amount)
    {
        if (hp <= 0f) return;
        hp -= amount;
        sr.color = Color.white;
        flashTimer = 0.06f;
        AudioManager.Instance?.PlayEnemyHit();
        if (hp <= 0f) Die();
    }

    void Die()
    {
        AudioManager.Instance?.PlayEnemyDeath();
        GameManager.Instance.AddScore(Data.points);
        DropManager.Instance.RollDrops(Data, transform.position);
        bool boss = Data.isBoss;
        EnemySpawner.Instance.Despawn(this);
        if (boss) GameManager.Instance.Victory();
    }
}
