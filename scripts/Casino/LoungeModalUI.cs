using System.Collections.Generic;
using Godot;

/// <summary>
/// Modal das estações Balcão do Bar, Caça-níquel de Melhorias e Mesa de Roleta (PDF, seção 5).
/// Montado em código e criado sob demanda na cena atual. Pausa o jogo enquanto aberto.
/// Fechar antes de girar/comprar não cobra ficha; a cobrança é feita por LoungeStation.TryPay.
/// </summary>
public partial class LoungeModalUI : CanvasLayer
{
    const float SpinDuration = 1.2f;
    const float SpinTick = 0.06f;
    const float CloseDelay = 1.4f;

    static LoungeModalUI instance;

    LoungeStation station;
    Control root;
    Label title, body, result;
    HBoxContainer optionsRow;
    readonly List<Button> options = new List<Button>(3);
    Button actionButton, closeButton;
    bool busy;

    public static void Open(LoungeStation station)
    {
        if (instance == null || !IsInstanceValid(instance))
        {
            instance = new LoungeModalUI();
            station.GetTree().CurrentScene.AddChild(instance);
        }
        instance.Show(station);
    }

    public override void _Ready()
    {
        Layer = 10;
        ProcessMode = ProcessModeEnum.Always; // roda com o jogo pausado
        Build();
        root.Visible = false;
    }

    public override void _ExitTree()
    {
        if (instance == this) instance = null;
    }

    void Build()
    {
        root = new ColorRect { Color = new Color(0f, 0f, 0f, 0.75f) };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var box = new PanelContainer
        {
            AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft = -560f, OffsetRight = 560f, OffsetTop = -300f, OffsetBottom = 300f,
        };
        box.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.04f, 0.18f, 0.97f),
            ContentMarginLeft = 30, ContentMarginRight = 30, ContentMarginTop = 20, ContentMarginBottom = 20,
        });
        root.AddChild(box);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 14);
        box.AddChild(col);

        title = MakeLabel(48, new Color(1f, 0.85f, 0.15f));
        body = MakeLabel(24, new Color(0.85f, 0.85f, 0.9f));
        col.AddChild(title);
        col.AddChild(body);

        optionsRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        optionsRow.AddThemeConstantOverride("separation", 24);
        col.AddChild(optionsRow);
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            var b = new Button
            {
                CustomMinimumSize = new Vector2(320f, 220f),
                FocusMode = Control.FocusModeEnum.None,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                ClipText = false,
            };
            b.AddThemeFontSizeOverride("font_size", 26);
            b.AddThemeFontOverride("font", new SystemFont { FontWeight = 700 });
            b.Pressed += () => OnOption(idx);
            optionsRow.AddChild(b);
            options.Add(b);
        }

        result = MakeLabel(28, Colors.White);
        col.AddChild(result);

        var bottom = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        bottom.AddThemeConstantOverride("separation", 30);
        col.AddChild(bottom);
        actionButton = MakeButton(bottom, OnAction);
        closeButton = MakeButton(bottom, Close);
        closeButton.Text = "SAIR";
    }

    static Label MakeLabel(int size, Color color)
    {
        var l = new Label { HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", color);
        return l;
    }

    static Button MakeButton(Container parent, System.Action onPressed)
    {
        var b = new Button { CustomMinimumSize = new Vector2(260f, 52f), FocusMode = Control.FocusModeEnum.None };
        b.AddThemeFontSizeOverride("font_size", 24);
        b.Pressed += onPressed;
        parent.AddChild(b);
        return b;
    }

    void Show(LoungeStation s)
    {
        if (root.Visible) return;
        station = s;
        busy = false;
        result.Text = "";
        closeButton.Visible = true;
        closeButton.Disabled = false;
        title.Text = LoungeRules.GetDisplayName(s.StationType);

        switch (s.StationType)
        {
            case LoungeStationType.Bar: ShowBar(); break;
            case LoungeStationType.UpgradeSlot: ShowUpgradeIdle(); break;
            default: ShowRouletteIdle(); break;
        }

        GameManager.Instance.SetPaused(true);
        root.Visible = true;
    }

    void Close()
    {
        if (busy || !root.Visible) return;
        root.Visible = false;
        station = null;
        GameManager.Instance.SetPaused(false);
    }

    string ChipsLine => $"FICHAS: {GameManager.Instance.Chips}   ·   CUSTO: {station.Cost}";

    void SetOptions(int count, bool enabled)
    {
        for (int i = 0; i < options.Count; i++)
        {
            options[i].Visible = i < count;
            options[i].Disabled = !enabled;
        }
    }

    void OnOption(int i)
    {
        if (busy || station == null) return;
        switch (station.StationType)
        {
            case LoungeStationType.Bar: BuyDrink((DrinkType)i); break;
            case LoungeStationType.Roulette: PickPassive(i); break;
        }
    }

    void OnAction()
    {
        if (busy || station == null) return;
        if (station.StationType == LoungeStationType.UpgradeSlot) SpinUpgrade();
        else if (station.StationType == LoungeStationType.Roulette) SpinRoulette();
    }

    // ---------- Balcão do Bar ----------

    void ShowBar()
    {
        actionButton.Visible = false;
        float eff = RunState.Efficiency;
        body.Text = $"Escolha uma bebida. Tipos diferentes acumulam; repetir só reinicia os {DrinkRules.Duration:0}s.\n{ChipsLine}";
        SetOptions(DrinkRules.Count, true);
        for (int i = 0; i < DrinkRules.Count; i++)
        {
            var t = (DrinkType)i;
            string active = RunState.IsActive(t) ? $"\n(ativa: {Mathf.CeilToInt(RunState.TimeLeft(t))}s)" : "";
            options[i].Text = $"{DrinkRules.Name(t)}\n\n{DrinkRules.Describe(t, eff)}{active}";
            options[i].Disabled = !station.CanInteract(GameManager.Instance.Chips);
        }
    }

    void BuyDrink(DrinkType t)
    {
        if (!station.TryPay()) return;
        RunState.Consume(t);
        ShowBar();
        result.Text = $"Bebida de {DrinkRules.Name(t)}! {DrinkRules.Describe(t, RunState.Efficiency)}";
    }

    // ---------- Caça-níquel de Melhorias ----------

    void ShowUpgradeIdle()
    {
        body.Text = $"3 rolos: QUEM? · O QUÊ? · QUANTO? (5% a 30%)\n{ChipsLine}";
        SetOptions(3, false);
        options[0].Text = "QUEM?";
        options[1].Text = "O QUÊ?";
        options[2].Text = "QUANTO?";
        actionButton.Visible = true;
        actionButton.Text = $"GIRAR ({station.Cost} FICHA)";
        actionButton.Disabled = !station.CanInteract(GameManager.Instance.Chips);
    }

    async void SpinUpgrade()
    {
        if (!station.TryPay()) return;
        busy = true;
        actionButton.Disabled = closeButton.Disabled = true;
        result.Text = "Girando...";
        AudioManager.Instance?.PlaySlotSpin();

        // Sorte (Tabela B) decide o valor antes da animação.
        var roll = UpgradeRoll.Roll(MetaProgress.LuckLevel, RunState.Rng);

        for (float t = 0f; t < SpinDuration; t += SpinTick)
        {
            var target = (UpgradeTarget)RunState.Rng.Next(2);
            ShowUpgradeReels(target, UpgradeRoll.StatFor(target, RunState.Rng.Next(UpgradeRoll.StatsPerTarget)), 5 * (1 + RunState.Rng.Next(6)));
            await ToSignal(GetTree().CreateTimer(SpinTick), SceneTreeTimer.SignalName.Timeout);
        }

        ShowUpgradeReels(roll.Target, roll.Stat, roll.Percent);
        RunState.ApplyUpgrade(roll);
        if (roll.Percent >= 25) AudioManager.Instance?.PlayJackpot();
        else AudioManager.Instance?.PlaySlotWin();

        string verb = roll.Stat == UpgradeStat.DashCooldown || roll.Stat == UpgradeStat.Reload ? "-" : "+";
        result.Text = $"{UpgradeRoll.TargetName(roll.Target)}: {verb}{roll.Percent}% {UpgradeRoll.StatName(roll.Stat)} (permanente na run)";
        body.Text = $"3 rolos: QUEM? · O QUÊ? · QUANTO? (5% a 30%)\n{ChipsLine}";
        busy = false;
        closeButton.Disabled = false;
        actionButton.Text = $"GIRAR DE NOVO ({station.Cost} FICHA)";
        actionButton.Disabled = !station.CanInteract(GameManager.Instance.Chips);
    }

    void ShowUpgradeReels(UpgradeTarget target, UpgradeStat stat, int percent)
    {
        options[0].Text = UpgradeRoll.TargetName(target);
        options[1].Text = UpgradeRoll.StatName(stat);
        options[2].Text = $"{percent}%";
    }

    // ---------- Mesa de Roleta ----------

    readonly List<Passive> offered = new List<Passive>(2);

    void ShowRouletteIdle()
    {
        body.Text = $"Sorteia 2 passivas únicas e você escolhe 1. Máx. 1 por sala.\n{ChipsLine}";
        SetOptions(0, false);
        actionButton.Visible = true;
        actionButton.Text = $"GIRAR ({station.Cost} FICHAS)";
        actionButton.Disabled = !station.CanInteract(GameManager.Instance.Chips);
    }

    async void SpinRoulette()
    {
        if (!station.TryPay()) return;
        busy = true;
        actionButton.Visible = false;
        closeButton.Visible = false; // já pagou: precisa escolher uma
        result.Text = "A roleta está girando...";
        AudioManager.Instance?.PlaySlotSpin();

        offered.Clear();
        offered.AddRange(PassiveCatalog.Draw(RunState.Passives, 2, RunState.Rng));
        SetOptions(offered.Count, false);

        for (float t = 0f; t < SpinDuration; t += SpinTick)
        {
            for (int i = 0; i < offered.Count; i++) options[i].Text = PassiveCatalog.Name((Passive)RunState.Rng.Next(PassiveCatalog.Count));
            await ToSignal(GetTree().CreateTimer(SpinTick), SceneTreeTimer.SignalName.Timeout);
        }

        for (int i = 0; i < offered.Count; i++)
        {
            options[i].Text = $"{PassiveCatalog.Name(offered[i])}\n\n{PassiveCatalog.Description(offered[i])}";
            options[i].Disabled = false;
        }
        AudioManager.Instance?.PlaySlotWin();
        result.Text = "Escolha 1 passiva";
        busy = false;
    }

    async void PickPassive(int i)
    {
        if (i >= offered.Count) return;
        busy = true;
        var p = offered[i];
        RunState.AddPassive(p);
        SetOptions(offered.Count, false);
        AudioManager.Instance?.PlayJackpot();
        result.Text = $"{PassiveCatalog.Name(p)} equipada!";

        await ToSignal(GetTree().CreateTimer(CloseDelay), SceneTreeTimer.SignalName.Timeout);
        busy = false;
        Close();
    }
}
