using UnityEngine;

public enum DropType { Chip, Heal, Points }

/// <summary>Item no chão (pooled). Coleta por distância ao jogador, sem trigger de física.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Drop : MonoBehaviour
{
    public float pickupRadius = 0.7f;
    [Tooltip("Cura e pontos somem depois disso; fichas nunca somem.")]
    public float lifetime = 40f;

    SpriteRenderer sr;
    DropType type;
    float value, life;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    public void Setup(DropType t, float v, Vector2 pos, Sprite sprite, Color color, float scale)
    {
        type = t;
        value = v;
        life = lifetime;
        transform.position = pos;
        transform.localScale = new Vector3(scale, scale, 1f);
        sr.sprite = sprite;
        sr.color = color;
    }

    void Update()
    {
        if (type != DropType.Chip)
        {
            life -= Time.deltaTime;
            if (life <= 0f) { Release(); return; }
        }

        var player = PlayerController.Instance;
        if (player == null) return;
        float r = pickupRadius + player.radius;
        if ((player.Position - (Vector2)transform.position).sqrMagnitude > r * r) return;

        switch (type)
        {
            case DropType.Chip: GameManager.Instance.AddChips((int)value); break;
            case DropType.Heal: player.Heal(value); break;
            case DropType.Points: GameManager.Instance.AddScore((int)value); break;
        }
        AudioManager.Instance?.PlayPickup();
        Release();
    }

    void Release() => PoolManager.Instance.Drops.Release(this);
}
