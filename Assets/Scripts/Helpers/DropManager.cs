using UnityEngine;

/// <summary>Decide e cria drops (via pool) quando um inimigo morre.</summary>
public class DropManager : MonoBehaviour
{
    public static DropManager Instance { get; private set; }

    [Header("Ficha de Cassino")]
    public Sprite chipSprite;
    public Color chipColor = new Color(1f, 0.85f, 0.1f);
    public float chipScale = 0.6f;

    [Header("Cura")]
    public Sprite healSprite;
    public Color healColor = new Color(0.2f, 1f, 0.4f);
    public float healScale = 0.45f;
    public float healAmount = 20f;

    [Header("Bônus de Pontos")]
    public Sprite pointsSprite;
    public Color pointsColor = new Color(0.3f, 0.8f, 1f);
    public float pointsScale = 0.4f;
    public int pointsAmount = 50;

    void Awake() => Instance = this;

    public void RollDrops(EnemyData data, Vector2 pos)
    {
        if (data.guaranteedChip) { Spawn(DropType.Chip, pos); return; }

        // Uma única rolagem: no máximo 1 drop por inimigo comum.
        float r = Random.value;
        if (r < data.chipDropChance) Spawn(DropType.Chip, pos);
        else if ((r -= data.chipDropChance) < data.healDropChance) Spawn(DropType.Heal, pos);
        else if (r - data.healDropChance < data.pointsDropChance) Spawn(DropType.Points, pos);
    }

    public void Spawn(DropType type, Vector2 pos)
    {
        var d = PoolManager.Instance.Drops.Get();
        switch (type)
        {
            case DropType.Chip: d.Setup(type, 1, pos, chipSprite, chipColor, chipScale); break;
            case DropType.Heal: d.Setup(type, healAmount, pos, healSprite, healColor, healScale); break;
            default: d.Setup(type, pointsAmount, pos, pointsSprite, pointsColor, pointsScale); break;
        }
    }
}
