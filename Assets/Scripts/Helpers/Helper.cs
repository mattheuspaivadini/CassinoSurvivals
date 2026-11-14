using UnityEngine;

/// <summary>NPC aliado: parado no mapa até o jogador encostar; depois segue o jogador e cura periodicamente com um pulso verde.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Helper : MonoBehaviour
{
    public float activationRadius = 1.5f;
    public float followDistance = 1.6f;
    public float followSharpness = 6f;
    public float healAmount = 4f;
    public float healInterval = 3f;
    public Color idleColor = new Color(0.3f, 0.45f, 0.3f);
    public Color activeColor = new Color(0.3f, 1f, 0.5f);
    public Color pulseColor = new Color(0.7f, 1f, 0.7f);

    SpriteRenderer sr;
    bool rescued;
    float healTimer, pulse;
    Vector2 offset;
    Vector3 baseScale;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.color = idleColor;
        baseScale = transform.localScale;
    }

    void Update()
    {
        var player = PlayerController.Instance;
        if (player == null) return;
        Vector2 pos = transform.position;

        if (!rescued)
        {
            float r = activationRadius;
            if ((player.Position - pos).sqrMagnitude > r * r) return;
            rescued = true;
            float a = Random.value * Mathf.PI * 2f; // cada ajudante pega um lugar diferente ao redor
            offset = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * followDistance;
            sr.color = activeColor;
            healTimer = healInterval;
            AudioManager.Instance?.PlayPickup();
            return;
        }

        float dt = Time.deltaTime;
        // Lerp independente de framerate
        transform.position = Vector2.Lerp(pos, player.Position + offset, 1f - Mathf.Exp(-followSharpness * dt));

        healTimer -= dt;
        if (healTimer <= 0f)
        {
            healTimer = healInterval;
            player.Heal(healAmount);
            pulse = 1f;
        }

        if (pulse > 0f)
        {
            pulse = Mathf.Max(0f, pulse - dt * 3f);
            transform.localScale = baseScale * (1f + 0.6f * pulse);
            sr.color = Color.Lerp(activeColor, pulseColor, pulse);
        }
    }
}
