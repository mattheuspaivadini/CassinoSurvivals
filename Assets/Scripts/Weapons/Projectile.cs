using UnityEngine;

/// <summary>Projétil sem física: move por transform e testa acerto por distância (sqrMagnitude) contra a lista de inimigos ativos.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Projectile : MonoBehaviour
{
    SpriteRenderer sr;
    Vector2 velocity;
    float damage, life, radius;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    public void Launch(Vector2 pos, Vector2 vel, float dmg, float lifetime, Color color, float scale)
    {
        transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, 0f, Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg));
        transform.localScale = new Vector3(scale, scale, 1f);
        sr.color = color;
        velocity = vel;
        damage = dmg;
        life = lifetime;
        radius = scale * 0.5f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        life -= dt;
        if (life <= 0f) { Release(); return; }

        Vector2 p = (Vector2)transform.position + velocity * dt;
        transform.position = p;

        var hit = EnemySpawner.Instance.FindHit(p, radius);
        if (hit != null)
        {
            hit.TakeDamage(damage);
            Release();
        }
    }

    void Release() => PoolManager.Instance.Projectiles.Release(this);
}
