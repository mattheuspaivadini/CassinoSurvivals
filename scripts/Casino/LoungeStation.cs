using System.Collections.Generic;
using Godot;

/// <summary>
/// Estação interativa da Sala de Descanso (Blockout 3): Bar (1 ficha), Melhorias (1 ficha),
/// Armas (2 fichas) e Roleta (4–5 fichas, máx. 1/sala e 3–4/run).
/// </summary>
public partial class LoungeStation : Node2D
{
    public static readonly List<LoungeStation> All = new List<LoungeStation>(8);
    public static int RouletteUsesInRun { get; set; }

    [Export] public LoungeStationType StationType { get; set; } = LoungeStationType.WeaponSlot;
    [Export] public int Cost { get; set; } = 1;
    [Export] public int MaxUsesPerRoom { get; set; } = 0;
    [Export] public int MaxUsesPerRun { get; set; } = 0;
    [Export] public int CurrentStage { get; set; } = 1;
    [Export] public bool RemoveAfterStage14 { get; set; } = false;
    [Export] public float InteractRadius { get; set; } = 2.4f;
    [Export] public Label Prompt { get; set; }
    [Export] public Label TitleLabel { get; set; }
    [Export] public Node2D VisualObstacle { get; set; }

    public int UsesInRoom { get; private set; }
    public bool IsRemoved { get; private set; }

    int promptState = int.MinValue;

    public override void _EnterTree() => All.Add(this);
    public override void _ExitTree() => All.Remove(this);

    public override void _Ready()
    {
        Cost = LoungeRules.GetCost(StationType, Cost);
        MaxUsesPerRoom = LoungeRules.GetMaxUsesPerRoom(StationType);
        MaxUsesPerRun = StationType == LoungeStationType.Roulette
            ? Mathf.Clamp(MaxUsesPerRun > 0 ? MaxUsesPerRun : LoungeRules.RouletteDefaultRunLimit, LoungeRules.RouletteMinRunLimit, LoungeRules.RouletteMaxRunLimit)
            : 0;
        RemoveAfterStage14 = LoungeRules.IsRemovedAfterStage14(StationType);

        if (!LoungeRules.IsAvailableInStage(StationType, CurrentStage))
        {
            IsRemoved = true;
            Visible = false;
            if (VisualObstacle != null)
            {
                VisualObstacle.Visible = false;
                foreach (Node child in VisualObstacle.GetChildren())
                    if (child is CollisionShape2D cs) cs.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
            }
            return;
        }

        RefreshTitleLabel();
    }

    public override void _Process(double delta)
    {
        if (IsRemoved) { SetPrompt(-1, string.Empty); return; }

        var player = PlayerController.Instance;
        var gm = GameManager.Instance;
        if (player == null || gm == null || gm.State != GameState.Playing)
        {
            SetPrompt(-1, string.Empty);
            return;
        }

        float r = InteractRadius * Units.Px;
        bool inRange = (player.GlobalPosition - GlobalPosition).LengthSquared() <= r * r;
        if (!inRange)
        {
            SetPrompt(-1, string.Empty);
            return;
        }

        int state = GetAvailabilityState(gm, out string msg);
        SetPrompt(state, msg);

        if (state == 1 && Input.IsActionJustPressed("interact"))
            Activate(gm);
    }

    public bool CanInteract(int currentChips)
    {
        if (IsRemoved || NoPassivesLeft) return false;
        int usesInRun = StationType == LoungeStationType.Roulette ? RouletteUsesInRun : 0;
        int runLimit = MaxUsesPerRun > 0 ? MaxUsesPerRun : LoungeRules.RouletteDefaultRunLimit;
        return LoungeRules.CanUse(StationType, currentChips, UsesInRoom, usesInRun, CurrentStage, Cost, runLimit);
    }

    public void RecordUse()
    {
        UsesInRoom++;
        if (StationType == LoungeStationType.Roulette)
            RouletteUsesInRun++;
        promptState = int.MinValue;
    }

    /// <summary>Roleta sem passivas restantes no catálogo fica indisponível.</summary>
    bool NoPassivesLeft => StationType == LoungeStationType.Roulette && PassiveCatalog.Remaining(RunState.Passives) <= 0;

    /// <summary>Cobra as fichas e conta o uso. Chamado pelo modal no momento do giro/compra.</summary>
    public bool TryPay()
    {
        var gm = GameManager.Instance;
        if (gm == null || !CanInteract(gm.Chips)) return false;
        for (int i = 0; i < Cost; i++) gm.TrySpendChip();
        RecordUse();
        return true;
    }

    int GetAvailabilityState(GameManager gm, out string text)
    {
        if (NoPassivesLeft)
        {
            text = "SEM PASSIVAS RESTANTES";
            return 2;
        }
        int usesInRun = StationType == LoungeStationType.Roulette ? RouletteUsesInRun : 0;
        if (MaxUsesPerRun > 0 && usesInRun >= MaxUsesPerRun)
        {
            text = $"LIMITE DA RUN ({usesInRun}/{MaxUsesPerRun})";
            return 2;
        }
        if (MaxUsesPerRoom > 0 && UsesInRoom >= MaxUsesPerRoom)
        {
            text = $"USADA NESTA SALA ({UsesInRoom}/{MaxUsesPerRoom})";
            return 3;
        }
        if (gm.Chips < Cost)
        {
            string plural = Cost == 1 ? "FICHA" : "FICHAS";
            text = $"PRECISA DE {Cost} {plural}";
            return 0;
        }

        string fichaLabel = Cost == 1 ? "1 FICHA" : $"{Cost} FICHAS";
        text = $"[E] USAR ({fichaLabel})";
        return 1;
    }

    /// <summary>Abre o modal da estação; as fichas só saem quando o jogador gira/compra lá dentro.</summary>
    void Activate(GameManager gm)
    {
        if (!CanInteract(gm.Chips)) return;
        if (StationType == LoungeStationType.WeaponSlot) SlotMachineUI.Instance?.Open(this);
        else LoungeModalUI.Open(this);
    }

    void RefreshTitleLabel()
    {
        if (TitleLabel == null) return;
        string costText = StationType == LoungeStationType.Roulette
            ? $"{Cost} Fichas · máx 1/sala"
            : Cost == 1 ? "1 Ficha" : $"{Cost} Fichas";
        TitleLabel.Text = $"{LoungeRules.GetDisplayName(StationType)}\n({costText})";
    }

    void SetPrompt(int state, string text)
    {
        if (Prompt == null) return;
        if (state == promptState && Prompt.Text == text) return;
        promptState = state;
        Prompt.Visible = state >= 0;
        if (state >= 0) Prompt.Text = text;
    }
}
