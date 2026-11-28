using Godot;

/// <summary>
/// Tela de Game Over / Vitória com resumo de $ (PDF, seção 7: Cash Out e Loja Externa) e a loja
/// de upgrades permanentes, montada em código abaixo do resumo.
/// </summary>
public partial class EndScreenUI : CanvasLayer
{
    [Export] public string VictoryTitle { get; set; } = "VOCÊ QUEBROU A BANCA!";
    [Export] public bool ShowShop { get; set; } = true;

    Control panel;
    Label title, stats, balance;
    readonly Button[] buyButtons = new Button[MetaProgress.Names.Length];

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; // aparece com o jogo pausado
        panel = GetNode<Control>("Panel");
        title = GetNode<Label>("Panel/Title");
        stats = GetNode<Label>("Panel/Stats");
        panel.Visible = false;
        GameManager.Instance.StateChanged += OnStateChanged;
        GetNode<Button>("Panel/Restart").Pressed += GameManager.Instance.Restart;
        if (ShowShop) BuildShop();
    }

    // GameManager é autoload e sobrevive ao reload: desconecta para não chamar um nó liberado.
    public override void _ExitTree() => GameManager.Instance.StateChanged -= OnStateChanged;

    void OnStateChanged(int state)
    {
        var s = (GameState)state;
        if (s != GameState.GameOver && s != GameState.Victory) return;
        var gm = GameManager.Instance;
        bool win = s == GameState.Victory;
        title.Text = win ? VictoryTitle : "GAME OVER";
        title.AddThemeColorOverride("font_color", win ? new Color(1f, 0.85f, 0.1f) : new Color(1f, 0.25f, 0.35f));
        int t = (int)gm.Elapsed;
        string cash = gm.CashedOutChips > 0
            ? $"  (inclui {gm.CashedOutChips} ficha(s) x ${GameManager.CashOutPerChip})"
            : "";
        stats.Text = $"Pontos: {gm.Score}   Tempo: {t / 60}:{t % 60:00}\nGanhou: ${gm.RunDollars}{cash}";
        RefreshShop();
        panel.Visible = true;
    }

    /// <summary>Sobe título e resumo e encaixa a loja entre eles e o botão de reiniciar.</summary>
    void BuildShop()
    {
        // 5 upgrades: sobe mais o cabeçalho para a loja caber acima do botão de reiniciar.
        title.OffsetTop -= 300f;
        title.OffsetBottom -= 300f;
        stats.OffsetTop -= 300f;
        stats.OffsetBottom -= 270f;

        var box = new VBoxContainer
        {
            AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft = -520f, OffsetRight = 520f, OffsetTop = -190f, OffsetBottom = 100f,
        };
        box.AddThemeConstantOverride("separation", 6);
        panel.AddChild(box);

        balance = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        balance.AddThemeFontSizeOverride("font_size", 34);
        balance.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.5f));
        box.AddChild(balance);

        for (int i = 0; i < buyButtons.Length; i++)
        {
            int index = i;
            var b = new Button { CustomMinimumSize = new Vector2(0f, 42f), FocusMode = Control.FocusModeEnum.None };
            b.AddThemeFontSizeOverride("font_size", 22);
            b.Pressed += () =>
            {
                if (MetaProgress.TryBuy((MetaProgress.Upgrade)index)) RefreshShop();
            };
            box.AddChild(b);
            buyButtons[i] = b;
        }
    }

    void RefreshShop()
    {
        if (balance == null) return;
        balance.Text = $"LOJA  -  SALDO: ${MetaProgress.Dollars}";
        for (int i = 0; i < buyButtons.Length; i++)
        {
            var u = (MetaProgress.Upgrade)i;
            int lvl = MetaProgress.Level(u), cost = MetaProgress.NextCost(u);
            string price = cost < 0 ? "MÁXIMO" : $"${cost}";
            buyButtons[i].Text = $"{MetaProgress.Names[i]}  Nv {lvl}/{MetaProgress.MaxLevel}  -  {MetaProgress.Descriptions[i]}  -  {price}";
            buyButtons[i].Disabled = cost < 0 || MetaProgress.Dollars < cost;
        }
    }
}
