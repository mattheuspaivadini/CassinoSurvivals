using System.Collections.Generic;
using UnityEngine;

/// <summary>Máquina fixa no mapa. Com fichas, perto dela e apertando E, abre o modal.</summary>
public class SlotMachine : MonoBehaviour
{
    public static readonly List<SlotMachine> All = new List<SlotMachine>(8);

    public float interactRadius = 2.2f;
    [Tooltip("TextMesh filho mostrado quando o jogador está perto.")]
    public TextMesh prompt;

    int promptState = -1; // -1 escondido, 0 sem ficha, 1 pode girar

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Update()
    {
        var player = PlayerController.Instance;
        var gm = GameManager.Instance;
        if (player == null || gm.State != GameState.Playing) { SetPrompt(-1); return; }

        bool inRange = (player.Position - (Vector2)transform.position).sqrMagnitude <= interactRadius * interactRadius;
        bool canSpin = inRange && gm.Chips > 0;
        SetPrompt(!inRange ? -1 : canSpin ? 1 : 0);

        if (canSpin && Input.GetKeyDown(KeyCode.E)) SlotMachineUI.Instance.Open();
    }

    void SetPrompt(int state)
    {
        if (state == promptState || prompt == null) return; // só troca texto quando muda
        promptState = state;
        prompt.gameObject.SetActive(state >= 0);
        if (state >= 0) prompt.text = state == 1 ? "[E] GIRAR" : "PRECISA DE FICHA";
    }

    public static SlotMachine FindNearest(Vector2 from)
    {
        SlotMachine best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < All.Count; i++)
        {
            float d = ((Vector2)All[i].transform.position - from).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = All[i]; }
        }
        return best;
    }
}
