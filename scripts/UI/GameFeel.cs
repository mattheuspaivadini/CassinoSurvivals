using Godot;

/// <summary>
/// "Game feel" neon (issue #31): tremida de tela opcional e explosões de partículas.
/// A tremida pode ser desligada nas Configurações/Pausa e a escolha fica salva em user://settings.cfg.
/// </summary>
public static class GameFeel
{
    const string SettingsPath = "user://settings.cfg";

    public static readonly Color Gold = new Color(1f, 0.82f, 0.2f);
    public static readonly Color Neon = new Color(1f, 0.3f, 0.85f);

    static bool loaded, shakeEnabled = true;
    static Tween shakeTween;

    public static bool ShakeEnabled
    {
        get { Load(); return shakeEnabled; }
        set
        {
            Load();
            shakeEnabled = value;
            var cfg = new ConfigFile();
            cfg.Load(SettingsPath);
            cfg.SetValue("video", "screen_shake", value);
            cfg.Save(SettingsPath);
        }
    }

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        var cfg = new ConfigFile();
        if (cfg.Load(SettingsPath) == Error.Ok) shakeEnabled = (bool)cfg.GetValue("video", "screen_shake", true);
    }

    /// <summary>Treme a câmera do jogador por <paramref name="seconds"/>, com força em pixels.</summary>
    public static void Shake(float strengthPx, float seconds)
    {
        if (!ShakeEnabled) return;
        var player = PlayerController.Instance;
        if (player == null || !GodotObject.IsInstanceValid(player)) return;
        var cam = player.GetNodeOrNull<Camera2D>("Camera");
        if (cam == null) return;

        if (shakeTween != null && shakeTween.IsValid()) shakeTween.Kill();
        shakeTween = cam.CreateTween();
        int steps = Mathf.Max(2, Mathf.RoundToInt(seconds / 0.04f));
        for (int i = 0; i < steps; i++)
        {
            float falloff = 1f - (float)i / steps;
            var offset = new Vector2((float)GD.RandRange(-1.0, 1.0), (float)GD.RandRange(-1.0, 1.0)) * strengthPx * falloff;
            shakeTween.TweenProperty(cam, "offset", offset, seconds / steps);
        }
        shakeTween.TweenProperty(cam, "offset", Vector2.Zero, 0.04f);
    }

    /// <summary>Explosão de partículas que se apaga sozinha. Em UI, passe um nó de CanvasLayer como pai.</summary>
    public static void Burst(Node parent, Vector2 position, Color color, int amount = 24, float speed = 260f, float lifetime = 0.7f, float size = 10f)
    {
        if (parent == null || !GodotObject.IsInstanceValid(parent)) return;
        var p = new CpuParticles2D
        {
            Position = position,
            Emitting = false,
            OneShot = true,
            Explosiveness = 0.95f,
            Amount = Mathf.Max(1, amount),
            Lifetime = lifetime,
            Direction = Vector2.Up,
            Spread = 180f,
            Gravity = new Vector2(0f, 420f),
            InitialVelocityMin = speed * 0.45f,
            InitialVelocityMax = speed,
            ScaleAmountMin = size * 0.6f,
            ScaleAmountMax = size,
            Color = color,
            ColorRamp = Fade(color),
            ProcessMode = Node.ProcessModeEnum.Always, // aparece também nos modais (jogo pausado)
            ZIndex = 10,
        };
        parent.AddChild(p);
        p.Emitting = true;
        p.Finished += p.QueueFree;
    }

    /// <summary>Explosão no mundo (morte de Dourado/boss, golpes de área).</summary>
    public static void WorldBurst(Node context, Vector2 globalPos, Color color, int amount = 24, float speed = 260f)
    {
        var root = context?.GetTree()?.CurrentScene;
        if (root is Node2D) Burst(root, globalPos, color, amount, speed);
    }

    static Gradient Fade(Color c)
    {
        var g = new Gradient();
        g.SetColor(0, new Color(1f, 1f, 1f, 1f));
        g.AddPoint(0.25f, c);
        g.SetColor(g.GetPointCount() - 1, new Color(c, 0f));
        return g;
    }

    /// <summary>Clarão rápido na tela inteira (jackpots). <paramref name="layer"/> é o CanvasLayer do modal.</summary>
    public static void Flash(Node layer, Color color, float seconds = 0.35f)
    {
        if (layer == null || !GodotObject.IsInstanceValid(layer)) return;
        var rect = new ColorRect { Color = new Color(color, 0.45f), MouseFilter = Control.MouseFilterEnum.Ignore, ProcessMode = Node.ProcessModeEnum.Always };
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(rect);
        var t = rect.CreateTween();
        t.TweenProperty(rect, "modulate:a", 0f, seconds);
        t.TweenCallback(Callable.From(rect.QueueFree));
    }
}
