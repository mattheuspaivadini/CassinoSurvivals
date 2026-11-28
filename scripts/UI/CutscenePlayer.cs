using Godot;

/// <summary>
/// Abertura em quadrinhos: mostra as páginas em sequência com o botão AVANÇAR (também Espaço/Enter/clique).
/// Na última página o botão vira JOGAR e carrega a primeira fase.
/// </summary>
public partial class CutscenePlayer : Control
{
    [Export] public Godot.Collections.Array<Texture2D> Pages { get; set; } = new();
    [Export(PropertyHint.File, "*.tscn")] public string NextScene { get; set; } = "res://scenes/levels/Fase1.tscn";
    [Export] public float FadeTime { get; set; } = 0.35f;

    TextureRect page;
    Button advance;
    Label counter;
    int index;
    bool busy;

    public override void _Ready()
    {
        // Jogo pausado durante a abertura: o tempo da partida (e o boss por tempo) só conta depois.
        ProcessMode = ProcessModeEnum.Always;
        GameManager.Instance?.SetPaused(true);
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore; // o padrão (Stop) consome o clique antes do _UnhandledInput
        AddChild(new ColorRect { Color = new Color(0.04f, 0.02f, 0.06f), AnchorRight = 1f, AnchorBottom = 1f, MouseFilter = MouseFilterEnum.Ignore });

        page = new TextureRect
        {
            AnchorRight = 1f, AnchorBottom = 1f,
            OffsetLeft = 40f, OffsetRight = -40f, OffsetTop = 30f, OffsetBottom = -110f,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(page);

        advance = new Button
        {
            AnchorLeft = 1f, AnchorRight = 1f, AnchorTop = 1f, AnchorBottom = 1f,
            OffsetLeft = -300f, OffsetRight = -40f, OffsetTop = -90f, OffsetBottom = -30f,
            FocusMode = FocusModeEnum.None,
        };
        advance.AddThemeFontSizeOverride("font_size", 30);
        advance.AddThemeFontOverride("font", new SystemFont { FontWeight = 800 });
        advance.Pressed += Next;
        AddChild(advance);

        counter = new Label
        {
            AnchorTop = 1f, AnchorBottom = 1f,
            OffsetLeft = 40f, OffsetRight = 400f, OffsetTop = -80f, OffsetBottom = -40f,
        };
        counter.AddThemeFontSizeOverride("font_size", 22);
        counter.AddThemeColorOverride("font_color", new Color(0.8f, 0.7f, 0.85f));
        AddChild(counter);

        if (Pages.Count == 0) { StartGame(); return; }
        Show(0);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        bool key = e is InputEventKey k && k.Pressed && !k.Echo && (k.Keycode == Key.Space || k.Keycode == Key.Enter || k.Keycode == Key.KpEnter);
        bool click = e is InputEventMouseButton m && m.Pressed && m.ButtonIndex == MouseButton.Left;
        if (!key && !click) return;
        GetViewport().SetInputAsHandled();
        Next();
    }

    void Show(int i)
    {
        index = i;
        page.Texture = Pages[i];
        bool last = i >= Pages.Count - 1;
        advance.Text = last ? "JOGAR ▶" : "AVANÇAR ▶";
        counter.Text = $"{i + 1} / {Pages.Count}";
    }

    async void Next()
    {
        if (busy) return;
        busy = true;
        var tween = CreateTween();
        tween.TweenProperty(page, "modulate:a", 0f, FadeTime);
        await ToSignal(tween, Tween.SignalName.Finished);

        if (index >= Pages.Count - 1) { StartGame(); return; }
        Show(index + 1);
        tween = CreateTween();
        tween.TweenProperty(page, "modulate:a", 1f, FadeTime);
        await ToSignal(tween, Tween.SignalName.Finished);
        busy = false;
    }

    void StartGame()
    {
        GameManager.Instance?.ActivateCampaignAt(1);
        GameManager.Instance?.SetPaused(false);
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, NextScene);
    }
}
