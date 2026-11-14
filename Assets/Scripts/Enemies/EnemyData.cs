using UnityEngine;

/// <summary>Tipo de inimigo (comum, elite ou boss). Um único prefab pooled lê estes dados no spawn.</summary>
[CreateAssetMenu(menuName = "Cassino Survivals/Enemy", fileName = "Enemy")]
public class EnemyData : ScriptableObject
{
    public string enemyName = "Capanga";
    public Sprite sprite;
    public Color color = Color.red;
    public float scale = 0.8f;
    public float mass = 1f;

    [Header("Combate")]
    public float maxHp = 20f;
    public float contactDamage = 8f;
    public float contactCooldown = 0.6f;
    public float moveSpeed = 2.5f;
    [Tooltip("Se true, o HP não escala com o tempo de partida.")]
    public bool fixedHp;

    [Header("Recompensa")]
    public int points = 10;
    public bool guaranteedChip;
    [Range(0, 1)] public float chipDropChance = 0.005f;
    [Range(0, 1)] public float healDropChance = 0.03f;
    [Range(0, 1)] public float pointsDropChance = 0.05f;

    [Header("Boss")]
    public bool isBoss;
}
