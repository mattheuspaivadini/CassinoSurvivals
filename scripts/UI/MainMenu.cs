using Godot;

/// <summary>
/// Menu Principal (issue #24): JOGAR (cutscene e Fase 1), LOJA (upgrades permanentes em $),
/// CONFIGURAÇÕES (volumes) e SAIR. Montado em código; o jogo fica pausado enquanto ele está aberto.
/// </summary>
public partial class MainMenu : Control
{
    [Export] public Texture2D Background { get; set; }

    Control mainPage, shopPage, settingsPage;
    ShopPanel shop;
    Label dollars;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        GameManager.Instance?.SetPaused(true); // ESC aqui não abre a pausa e o tempo da partida não conta
        AudioManager.Instance?.PlayMainMusic();
        SetAnchorsPreset(LayoutPreset.FullRect);

        AddChild(new ColorRect { Color = new Color(0.04f, 0.02f, 0.06f), AnchorRight = 1f, AnchorBottom = 1f, MouseFilter = MouseFilterEnum.Ignore });
        if (Background != null)
        {
            AddChild(new TextureRect
            {
                Texture = Background, AnchorRight = 1f, AnchorBottom = 1f,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                Modulate = new Color(0.32f, 0.28f, 0.38f), MouseFilter = MouseFilterEnum.Ignore,
            });
        }
        AddChild(new ColorRect { Color = new Color(0.03f, 0.01f, 0.06f, 0.6f), AnchorRight = 1f, AnchorBottom = 1f, MouseFilter = MouseFilterEnum.Ignore });

        mainPage = BuildMainPage();
        shopPage = BuildShopPage();
        settingsPage = BuildSettingsPage();
        ShowPage(mainPage);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!e.IsActionPressed("ui_cancel") || mainPage.IsVisibleInTree()) return;
        ShowPage(mainPage);
        GetViewport().SetInputAsHandled();
    }

    Control BuildMainPage()
    {
        var col = Page(-330f, 330f, -370f, 380f);

        var title = new Label { Text = "CASSINO\nSURVIVALS", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 92);
        title.AddThemeFontOverride("font", new SystemFont { FontWeight = 900 });
        title.AddThemeColorOverride("font_color", new Color(1f, 0.82f, 0.2f));
        title.AddThemeColorOverride("font_outline_color", new Color(0.55f, 0.05f, 0.35f));
        title.AddThemeConstantOverride("outline_size", 14);
        col.AddChild(title);

        dollars = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        dollars.AddThemeFontSizeOverride("font_size", 26);
        dollars.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.5f));
        col.AddChild(dollars);
        col.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 16f) });

        col.AddChild(MakeButton("JOGAR", () => GameManager.Instance.StartNewRun(showCutscene: true), true));
        col.AddChild(MakeButton("LOJA", () => { shop.Refresh(); ShowPage(shopPage); }));
        col.AddChild(MakeButton("CONFIGURAÇÕES", () => ShowPage(settingsPage)));
        col.AddChild(MakeButton("SANDBOX", () => GameManager.Instance.StartEnemySandbox()));
        col.AddChild(MakeButton("SAIR", () => GetTree().Quit()));
        return col;
    }

    Control BuildShopPage()
    {
        var col = Page(-560f, 560f, -300f, 300f);
        col.AddChild(Header("LOJA EXTERNA"));
        var hint = new Label { Text = "Upgrades permanentes: valem em todas as runs.", HorizontalAlignment = HorizontalAlignment.Center };
        hint.AddThemeFontSizeOverride("font_size", 22);
        col.AddChild(hint);
        shop = new ShopPanel { Title = "" };
        col.AddChild(shop);
        col.AddChild(MakeButton("VOLTAR  (ESC)", () => ShowPage(mainPage)));
        return col;
    }

    Control BuildSettingsPage()
    {
        var col = Page(-380f, 380f, -290f, 290f);
        col.AddChild(Header("CONFIGURAÇÕES"));
        PauseMenu.AddVolumeRow(col, "Geral", "Master");
        PauseMenu.AddVolumeRow(col, "Música", AudioManager.MusicBus);
        PauseMenu.AddVolumeRow(col, "Efeitos", AudioManager.SfxBus);
        PauseMenu.AddShakeToggle(col);
        PauseMenu.AddFullscreenToggle(col);
        col.AddChild(MakeButton("VOLTAR  (ESC)", () => ShowPage(mainPage)));
        return col;
    }

    /// <summary>Página centralizada sobre um painel escuro (o quadrinho do fundo não compete com o texto).</summary>
    VBoxContainer Page(float left, float right, float top, float bottom)
    {
        var panel = new PanelContainer
        {
            AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft = left, OffsetRight = right, OffsetTop = top, OffsetBottom = bottom,
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.02f, 0.11f, 0.9f),
            BorderColor = new Color(1f, 0.82f, 0.2f, 0.6f),
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 14, CornerRadiusTopRight = 14, CornerRadiusBottomLeft = 14, CornerRadiusBottomRight = 14,
            ContentMarginLeft = 36, ContentMarginRight = 36, ContentMarginTop = 24, ContentMarginBottom = 24,
        });
        AddChild(panel);
        var col = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        col.AddThemeConstantOverride("separation", 14);
        panel.AddChild(col);
        return col;
    }

    static Label Header(string text)
    {
        var l = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
        l.AddThemeFontSizeOverride("font_size", 56);
        l.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.15f));
        return l;
    }

    static Button MakeButton(string text, System.Action onPressed, bool primary = false)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(0f, primary ? 76f : 62f), FocusMode = FocusModeEnum.None };
        b.AddThemeFontSizeOverride("font_size", primary ? 38 : 30);
        b.AddThemeFontOverride("font", new SystemFont { FontWeight = 800 });
        b.Pressed += onPressed;
        return b;
    }

    void ShowPage(Control page)
    {
        dollars.Text = $"SALDO: ${MetaProgress.Dollars}"; // muda depois de comprar na loja
        // As páginas são as colunas; quem aparece/some é o painel em volta.
        mainPage.GetParent<Control>().Visible = page == mainPage;
        shopPage.GetParent<Control>().Visible = page == shopPage;
        settingsPage.GetParent<Control>().Visible = page == settingsPage;
    }
}
