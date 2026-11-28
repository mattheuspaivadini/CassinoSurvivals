using Godot;

/// <summary>Alterna entre tela cheia e janela e salva a escolha em user://settings.cfg.
/// Em janela, o tamanho cabe na área útil do monitor (sem cobrir a barra de tarefas).</summary>
public static class DisplaySettings
{
    const string SettingsPath = "user://settings.cfg";
    const float WindowFill = 0.85f;

    static bool loaded, fullscreen = true;

    public static bool Fullscreen
    {
        get { Load(); return fullscreen; }
        set
        {
            Load();
            fullscreen = value;
            var cfg = new ConfigFile();
            cfg.Load(SettingsPath);
            cfg.SetValue("video", "fullscreen", value);
            cfg.Save(SettingsPath);
            Apply();
        }
    }

    public static void Apply()
    {
        if (DisplayServer.GetName() == "headless") return;
        if (Fullscreen)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
            return;
        }

        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        int screen = DisplayServer.WindowGetCurrentScreen();
        Rect2I usable = DisplayServer.ScreenGetUsableRect(screen);
        Vector2I decorations = DisplayServer.WindowGetSizeWithDecorations() - DisplayServer.WindowGetSize();

        float maxWidth = (usable.Size.X - decorations.X) * WindowFill;
        float maxHeight = (usable.Size.Y - decorations.Y) * WindowFill;
        float width = Mathf.Min(maxWidth, maxHeight * 16f / 9f);
        var size = new Vector2I(Mathf.RoundToInt(width), Mathf.RoundToInt(width * 9f / 16f));

        DisplayServer.WindowSetSize(size);
        DisplayServer.WindowSetPosition(usable.Position + (usable.Size - size - decorations) / 2 + new Vector2I(0, decorations.Y));
    }

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        var cfg = new ConfigFile();
        if (cfg.Load(SettingsPath) == Error.Ok) fullscreen = (bool)cfg.GetValue("video", "fullscreen", true);
    }
}
