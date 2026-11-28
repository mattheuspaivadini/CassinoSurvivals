using System.Collections.Generic;
using Godot;

/// <summary>
/// Abertura em quadrinhos: exibe todas as páginas juntas em uma única tela,
/// revelando as seguintes progressivamente ao clicar em CONTINUAR (ou Espaço/Enter/clique).
/// Na última página revelada o botão vira JOGAR e carrega a primeira fase.
/// </summary>
public partial class CutscenePlayer : Control
{
    [Export] public Godot.Collections.Array<Texture2D> Pages { get; set; } = new();
    [Export(PropertyHint.File, "*.tscn")] public string NextScene { get; set; } = "res://scenes/levels/Fase1.tscn";
    [Export] public float FadeTime { get; set; } = 0.35f;

    HBoxContainer strip;
    readonly List<TextureRect> pageNodes = new();
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

        strip = new HBoxContainer
        {
            AnchorRight = 1f, AnchorBottom = 1f,
            OffsetLeft = 10f, OffsetRight = -10f, OffsetTop = 10f, OffsetBottom = -82f,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        strip.AddThemeConstantOverride("separation", 10);
        AddChild(strip);

        for (int i = 0; i < Pages.Count; i++)
        {
            var rect = new TextureRect
            {
                Texture = Pages[i],
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                MouseFilter = MouseFilterEnum.Ignore,
                Modulate = i == 0 ? Colors.White : new Color(1f, 1f, 1f, 0f),
            };
            strip.AddChild(rect);
            pageNodes.Add(rect);
        }

        advance = new Button
        {
            AnchorLeft = 1f, AnchorRight = 1f, AnchorTop = 1f, AnchorBottom = 1f,
            OffsetLeft = -280f, OffsetRight = -16f, OffsetTop = -72f, OffsetBottom = -12f,
            FocusMode = FocusModeEnum.None,
        };
        advance.AddThemeFontSizeOverride("font_size", 30);
        advance.AddThemeFontOverride("font", new SystemFont { FontWeight = 800 });
        advance.Pressed += Next;
        AddChild(advance);

        counter = new Label
        {
            AnchorTop = 1f, AnchorBottom = 1f,
            OffsetLeft = 16f, OffsetRight = 320f, OffsetTop = -62f, OffsetBottom = -20f,
        };
        counter.AddThemeFontSizeOverride("font_size", 22);
        counter.AddThemeColorOverride("font_color", new Color(0.8f, 0.7f, 0.85f));
        AddChild(counter);

        if (Pages.Count == 0) { StartGame(); return; }
        UpdateUi(0);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        bool key = e is InputEventKey k && k.Pressed && !k.Echo && (k.Keycode == Key.Space || k.Keycode == Key.Enter || k.Keycode == Key.KpEnter);
        bool click = e is InputEventMouseButton m && m.Pressed && m.ButtonIndex == MouseButton.Left;
        if (!key && !click) return;
        GetViewport().SetInputAsHandled();
        Next();
    }

    void UpdateUi(int i)
    {
        index = i;
        bool last = i >= Pages.Count - 1;
        advance.Text = last ? "JOGAR ▶" : "CONTINUAR ▶";
        counter.Text = $"{i + 1} / {Pages.Count}";
    }

    async void Next()
    {
        if (busy) return;
        busy = true;
        if (index >= Pages.Count - 1)
        {
            var fadeOut = CreateTween();
            fadeOut.TweenProperty(strip, "modulate:a", 0f, FadeTime);
            await ToSignal(fadeOut, Tween.SignalName.Finished);
            StartGame();
            return;
        }

        int nextIndex = index + 1;
        UpdateUi(nextIndex);
        var tween = CreateTween();
        tween.TweenProperty(pageNodes[nextIndex], "modulate:a", 1f, FadeTime);
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
