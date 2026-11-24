using System.Collections.Generic;
using Godot;

/// <summary>Máquina fixa no mapa. Com fichas, perto dela e apertando E, abre o modal.</summary>
public partial class SlotMachine : Node2D
{
    public static readonly List<SlotMachine> All = new List<SlotMachine>(8);

    /// <summary>Unidades.</summary>
    [Export] public float InteractRadius { get; set; } = 2.2f;
    /// <summary>Label filho mostrado quando o jogador está perto.</summary>
    [Export] public Label Prompt { get; set; }

    int promptState = -1; // -1 escondido, 0 sem ficha, 1 pode girar

    public override void _EnterTree() => All.Add(this);
    public override void _ExitTree() => All.Remove(this);

    public override void _Process(double delta)
    {
        var player = PlayerController.Instance;
        var gm = GameManager.Instance;
        if (player == null || gm.State != GameState.Playing) { SetPrompt(-1); return; }

        float r = InteractRadius * Units.Px;
        bool inRange = (player.GlobalPosition - GlobalPosition).LengthSquared() <= r * r;
        bool canSpin = inRange && gm.Chips > 0;
        SetPrompt(!inRange ? -1 : canSpin ? 1 : 0);

        if (canSpin && Input.IsActionJustPressed("interact")) SlotMachineUI.Instance.Open();
    }

    void SetPrompt(int state)
    {
        if (state == promptState || Prompt == null) return; // só troca texto quando muda
        promptState = state;
        Prompt.Visible = state >= 0;
        if (state >= 0) Prompt.Text = state == 1 ? "[E] GIRAR" : "PRECISA DE FICHA";
    }

    public static SlotMachine FindNearest(Vector2 from)
    {
        SlotMachine best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < All.Count; i++)
        {
            float d = (All[i].GlobalPosition - from).LengthSquared();
            if (d < bestSqr) { bestSqr = d; best = All[i]; }
        }
        return best;
    }
}
