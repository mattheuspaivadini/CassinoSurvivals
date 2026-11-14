using UnityEngine;

/// <summary>Seta orbitando o jogador que aponta para o caça-níquel mais próximo enquanto houver fichas.</summary>
public class Wayfinder : MonoBehaviour
{
    public Transform arrow;
    public float orbitRadius = 1.2f;

    void LateUpdate()
    {
        var gm = GameManager.Instance;
        bool show = gm.Chips > 0 && SlotMachine.All.Count > 0;
        if (arrow.gameObject.activeSelf != show) arrow.gameObject.SetActive(show);
        if (!show) return;

        Vector2 pos = transform.position;
        Vector2 dir = ((Vector2)SlotMachine.FindNearest(pos).transform.position - pos).normalized;
        arrow.position = pos + dir * orbitRadius;
        arrow.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
    }
}
