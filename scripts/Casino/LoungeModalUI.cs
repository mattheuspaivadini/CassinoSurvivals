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
    readonly List<TextureRect> optionIcons = new List<TextureRect>(3);
    readonly List<Label> optionLabels = new List<Label>(3);
    readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();
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
        AudioManager.Instance?.StopSlotSpin(); // o loop é do autoload e sobreviveria à cena
    }

    void Build()
    {
        root = new ColorRect { Color = new Color(0f, 0f, 0f, 0.75f) };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var box = new PanelContainer
        {
            AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft = -560f, OffsetRight = 560f, OffsetTop = -345f, OffsetBottom = 345f,
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
                CustomMinimumSize = new Vector2(310f, 340f),
                FocusMode = Control.FocusModeEnum.None,
                ClipText = false,
            };
            b.Pressed += () => OnOption(idx);

            var inner = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            inner.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            inner.OffsetLeft = 12f; inner.OffsetRight = -12f;
            inner.OffsetTop = 12f; inner.OffsetBottom = -12f;
            inner.AddThemeConstantOverride("separation", 8);
            b.AddChild(inner);

            var icon = new TextureRect
            {
                CustomMinimumSize = new Vector2(210f, 210f),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Visible = false,
            };
            inner.AddChild(icon);

            var lbl = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            lbl.AddThemeFontSizeOverride("font_size", 19);
            lbl.AddThemeFontOverride("font", new SystemFont { FontWeight = 700 });
            inner.AddChild(lbl);

            optionsRow.AddChild(b);
            options.Add(b);
            optionIcons.Add(icon);
            optionLabels.Add(lbl);
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

    void SetOptionCard(int i, string text, string texPath)
    {
        var tex = LoadTexture(texPath);
        optionIcons[i].Texture = tex;
        optionIcons[i].Visible = tex != null;
        optionLabels[i].Text = text;
    }

    Texture2D LoadTexture(string resPath)
    {
        if (string.IsNullOrEmpty(resPath)) return null;
        if (texCache.TryGetValue(resPath, out var cached)) return cached;
        var tex = ResourceLoader.Exists(resPath) ? GD.Load<Texture2D>(resPath) : null;
        if (tex == null)
        {
            string abs = ProjectSettings.GlobalizePath(resPath);
            if (System.IO.File.Exists(abs))
            {
                var img = new Image();
                if (img.Load(abs) == Error.Ok) tex = ImageTexture.CreateFromImage(img);
            }
        }
        texCache[resPath] = tex;
        return tex;
    }

    static string DrinkFramePath(DrinkType t) => t switch
    {
        DrinkType.Heal => "res://assets/molduras/bar/cura.png",
        DrinkType.Damage => "res://assets/molduras/bar/dano.png",
        _ => "res://assets/molduras/bar/velocidade.png",
    };

    static string WhoFramePath(UpgradeTarget target)
    {
        if (target == UpgradeTarget.Player) return "res://assets/molduras/quem/player.png";
        var ws = PlayerController.Instance?.GetNodeOrNull<WeaponSystem>("WeaponSystem");
        WeaponData active = ws != null && ws.Weapons.Count > 0
            ? ws.Weapons[Mathf.Clamp(ws.Selected, 0, ws.Weapons.Count - 1)].Data
            : null;
        return SlotMachineUI.WeaponFramePath(active);
    }

    static string WhatFramePath(UpgradeStat stat) => stat switch
    {
        UpgradeStat.MaxHp => "res://assets/molduras/o_que/vida.png",
        UpgradeStat.MoveSpeed => "res://assets/molduras/o_que/velocidade.png",
        UpgradeStat.DashCooldown => "res://assets/molduras/o_que/dash.png",
        UpgradeStat.Damage => "res://assets/molduras/o_que/dano.png",
        UpgradeStat.Magazine => "res://assets/molduras/o_que/balas.png",
        _ => "res://assets/molduras/o_que/recarga.png",
    };

    static string PassiveFramePath(Passive p) => p switch
    {
        Passive.CalibrePesado => "res://assets/molduras/roleta/calibre_pesado.png",
        Passive.EfeitoDomino => "res://assets/molduras/roleta/efeito_domino.png",
        Passive.GeloNoCopo => "res://assets/molduras/roleta/gelo_no_copo.png",
        Passive.MaoQuente => "res://assets/molduras/roleta/mao_quente.png",
        Passive.AllIn => "res://assets/molduras/roleta/all_in.png",
        Passive.SaqueRapido => "res://assets/molduras/roleta/saque_rapido.png",
        Passive.VirarAMesa => "res://assets/molduras/roleta/virar_a_mesa.png",
        Passive.BlefePerfeito => "res://assets/molduras/roleta/blefe_perfeito.png",
        Passive.OpenBar => "res://assets/molduras/roleta/open_bar.png",
        Passive.TrocoEmSangue => "res://assets/molduras/roleta/troco_em_sangue.png",
        Passive.Critico777 => "res://assets/molduras/roleta/777.png",
        _ => "res://assets/molduras/roleta/sangue_frio.png",
    };

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
            SetOptionCard(i, $"{DrinkRules.Name(t)}\n{DrinkRules.Describe(t, eff)}{active}", DrinkFramePath(t));
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
        SetOptionCard(0, "QUEM?", null);
        SetOptionCard(1, "O QUÊ?", null);
        SetOptionCard(2, "QUANTO?", null);
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
        AudioManager.Instance?.StartSlotSpin();

        // Sorte (Tabela B) decide o valor antes da animação.
        var roll = UpgradeRoll.Roll(MetaProgress.LuckLevel, RunState.Rng);

        for (float t = 0f; t < SpinDuration; t += SpinTick)
        {
            var target = (UpgradeTarget)RunState.Rng.Next(2);
            ShowUpgradeReels(target, UpgradeRoll.StatFor(target, RunState.Rng.Next(UpgradeRoll.StatsPerTarget)), 5 * (1 + RunState.Rng.Next(6)));
            await ToSignal(GetTree().CreateTimer(SpinTick), SceneTreeTimer.SignalName.Timeout);
        }

        ShowUpgradeReels(roll.Target, roll.Stat, roll.Percent);
        AudioManager.Instance?.StopSlotSpin();
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
        SetOptionCard(0, UpgradeRoll.TargetName(target), WhoFramePath(target));
        SetOptionCard(1, UpgradeRoll.StatName(stat), WhatFramePath(stat));
        SetOptionCard(2, $"{percent}%", $"res://assets/molduras/quanto/{percent}.png");
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
        AudioManager.Instance?.PlayRouletteSpin();

        offered.Clear();
        offered.AddRange(PassiveCatalog.Draw(RunState.Passives, 2, RunState.Rng));
        SetOptions(offered.Count, false);

        for (float t = 0f; t < SpinDuration; t += SpinTick)
        {
            for (int i = 0; i < offered.Count; i++)
            {
                var rp = (Passive)RunState.Rng.Next(PassiveCatalog.Count);
                SetOptionCard(i, PassiveCatalog.Name(rp), PassiveFramePath(rp));
            }
            await ToSignal(GetTree().CreateTimer(SpinTick), SceneTreeTimer.SignalName.Timeout);
        }

        for (int i = 0; i < offered.Count; i++)
        {
            var p = offered[i];
            SetOptionCard(i, $"{PassiveCatalog.Name(p)}\n{PassiveCatalog.Description(p)}", PassiveFramePath(p));
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
