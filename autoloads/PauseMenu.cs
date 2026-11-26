using Godot;

/// <summary>
/// Menu de pausa no ESC (autoload, vale em qualquer cena): volumes Geral/Música/Efeitos, continuar
/// e reiniciar. Não abre por cima do caça-níquel nem da tela final.
/// </summary>
public partial class PauseMenu : CanvasLayer
{
    Control root;
    bool open;

    public override void _Ready()
    {
        Layer = 30;
        ProcessMode = ProcessModeEnum.Always;
        Build();
        root.Visible = false;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!e.IsActionPressed("ui_cancel")) return;
        var gm = GameManager.Instance;
        if (open) Close();
        else if (gm.State == GameState.Playing) Open();
        else return; // pausado por outra tela (caça-níquel) ou fim de jogo
        GetViewport().SetInputAsHandled();
    }

    void Open()
    {
        open = true;
        root.Visible = true;
        GameManager.Instance.SetPaused(true);
    }

    void Close()
    {
        open = false;
        root.Visible = false;
        GameManager.Instance.SetPaused(false);
    }

    void Build()
    {
        root = new ColorRect { Color = new Color(0.03f, 0.01f, 0.06f, 0.88f), MouseFilter = Control.MouseFilterEnum.Stop };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);

        var box = new VBoxContainer();
        box.SetAnchorsPreset(Control.LayoutPreset.Center);
        box.OffsetLeft = -330f;
        box.OffsetRight = 330f;
        box.OffsetTop = -260f;
        box.OffsetBottom = 260f;
        box.AddThemeConstantOverride("separation", 18);
        root.AddChild(box);

        var title = new Label { Text = "PAUSADO", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 64);
        title.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.15f));
        box.AddChild(title);

        AddVolumeRow(box, "Geral", "Master");
        AddVolumeRow(box, "Música", AudioManager.MusicBus);
        AddVolumeRow(box, "Efeitos", AudioManager.SfxBus);

        box.AddChild(MakeButton("CONTINUAR  (ESC)", Close));
        box.AddChild(MakeButton("REINICIAR", () =>
        {
            open = false;
            root.Visible = false;
            GameManager.Instance.Restart();
        }));
    }

    static void AddVolumeRow(Container parent, string label, string bus)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 16);
        parent.AddChild(row);

        var name = new Label { Text = label, CustomMinimumSize = new Vector2(150f, 0f) };
        name.AddThemeFontSizeOverride("font_size", 30);
        row.AddChild(name);

        var slider = new HSlider
        {
            MinValue = 0, MaxValue = 100, Step = 5,
            Value = Mathf.Round(AudioManager.GetVolume(bus) * 100f),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            CustomMinimumSize = new Vector2(0f, 32f),
            FocusMode = Control.FocusModeEnum.None,
        };
        row.AddChild(slider);

        var value = new Label { Text = $"{slider.Value}%", CustomMinimumSize = new Vector2(90f, 0f), HorizontalAlignment = HorizontalAlignment.Right };
        value.AddThemeFontSizeOverride("font_size", 30);
        row.AddChild(value);

        slider.ValueChanged += v =>
        {
            AudioManager.SetVolume(bus, (float)v / 100f);
            value.Text = $"{v}%";
        };
    }

    static Button MakeButton(string text, System.Action onPressed)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(0f, 60f), FocusMode = Control.FocusModeEnum.None };
        b.AddThemeFontSizeOverride("font_size", 30);
        b.Pressed += onPressed;
        return b;
    }
}
