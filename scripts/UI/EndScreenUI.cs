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
    Label title, stats;
    ShopPanel shop;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; // aparece com o jogo pausado
        panel = GetNode<Control>("Panel");
        title = GetNode<Label>("Panel/Title");
        stats = GetNode<Label>("Panel/Stats");
        panel.Visible = false;
        GameManager.Instance.StateChanged += OnStateChanged;
        var restart = GetNode<Button>("Panel/Restart");
        AddMenuButton(restart); // antes de conectar: a cópia não herda o Pressed de reiniciar
        restart.Pressed += GameManager.Instance.Restart;
        if (ShowShop) BuildShop();
    }

    /// <summary>Divide a linha do botão de reiniciar: JOGAR DE NOVO à esquerda, MENU PRINCIPAL à direita.</summary>
    void AddMenuButton(Button restart)
    {
        float left = restart.OffsetLeft, right = restart.OffsetRight;
        restart.OffsetRight = (left + right) / 2f - 10f;
        restart.OffsetLeft = restart.OffsetRight - (right - left);
        var menu = (Button)restart.Duplicate((int)(DuplicateFlags.Groups | DuplicateFlags.Scripts | DuplicateFlags.UseInstantiation));
        menu.Name = "MainMenu";
        menu.Text = "MENU PRINCIPAL";
        // Nas arenas o texto fica num Label filho (o botão é só a área clicável sobre um ColorRect).
        if (menu.GetNodeOrNull<Label>("Label") is Label label) label.Text = "MENU PRINCIPAL";
        if (menu.GetNodeOrNull<ColorRect>("Color") is ColorRect color) color.Color = new Color(0.35f, 0.2f, 0.55f);
        menu.OffsetLeft = (left + right) / 2f + 10f;
        menu.OffsetRight = menu.OffsetLeft + (right - left);
        menu.Pressed += GameManager.Instance.GoToMainMenu;
        panel.AddChild(menu);
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

        shop = new ShopPanel
        {
            AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft = -520f, OffsetRight = 520f, OffsetTop = -190f, OffsetBottom = 100f,
        };
        panel.AddChild(shop);
    }

    void RefreshShop() => shop?.Refresh();
}
