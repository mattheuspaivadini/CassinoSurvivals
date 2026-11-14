using System;
using UnityEngine;

/// <summary>Movimento top-down (Input clássico) + HP do jogador.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    public float maxHp = 100f;
    public float moveSpeed = 6f;
    [Tooltip("Raio usado nas checagens de contato/coleta por distância.")]
    public float radius = 0.4f;

    public float CurrentHp { get; private set; }
    public Vector2 Position => rb.position;

    /// <summary>(hpAtual, hpMax)</summary>
    public event Action<float, float> OnHpChanged;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Color baseColor;
    float flashTimer;
    Vector2 input;

    void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
        CurrentHp = maxHp;
    }

    void Start() => OnHpChanged?.Invoke(CurrentHp, maxHp);

    void Update()
    {
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f) input.Normalize();

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f) sr.color = baseColor;
        }
    }

    void FixedUpdate() => rb.linearVelocity = input * moveSpeed;

    public void TakeDamage(float amount)
    {
        if (CurrentHp <= 0f || GameManager.Instance.State != GameState.Playing) return;
        CurrentHp = Mathf.Max(0f, CurrentHp - amount);
        Flash(Color.red);
        AudioManager.Instance?.PlayPlayerHurt();
        OnHpChanged?.Invoke(CurrentHp, maxHp);
        if (CurrentHp <= 0f) GameManager.Instance.GameOver();
    }

    public void Heal(float amount)
    {
        if (CurrentHp <= 0f) return;
        CurrentHp = Mathf.Min(maxHp, CurrentHp + amount);
        Flash(Color.green);
        AudioManager.Instance?.PlayHeal();
        OnHpChanged?.Invoke(CurrentHp, maxHp);
    }

    void Flash(Color c)
    {
        sr.color = c;
        flashTimer = 0.1f;
    }
}
