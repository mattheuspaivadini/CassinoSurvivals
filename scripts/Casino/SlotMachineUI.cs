using System.Collections.Generic;
using Godot;

/// <summary>
/// Modal do Caça-níquel de Armas: pausa o jogo, cobra o giro, sorteia o tier pela Tabela A de Sorte
/// e só então anima os 3 rolos. O jogador escolhe 1 slot ou RECUSA o giro (a ficha não volta).
/// </summary>
public partial class SlotMachineUI : CanvasLayer
{
    public static SlotMachineUI Instance { get; private set; }

    /// <summary>Banco de armas sorteáveis.</summary>
    [Export] public Godot.Collections.Array<WeaponData> WeaponPool { get; set; }
    [Export] public WeaponSystem WeaponSystem { get; set; }

    [Export] public float SpinDuration { get; set; } = 1.2f;
    [Export] public float SpinTick { get; set; } = 0.06f;
    [Export] public float CloseDelay { get; set; } = 1.3f;

    WeaponData[] reels = new WeaponData[3];
    readonly Button[] buttons = new Button[3];
    readonly ColorRect[] colors = new ColorRect[3];
    readonly TextureRect[] icons = new TextureRect[3];
    readonly Label[] labels = new Label[3];
    readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();
    Control panel;
    Label resultText;
    Button declineButton;
    bool choosing;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always; // roda com o jogo pausado
        panel = GetNode<Control>("Panel");
        resultText = GetNode<Label>("Panel/Result");
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            buttons[i] = GetNode<Button>($"Panel/Slot{i}");
            colors[i] = GetNode<ColorRect>($"Panel/Slot{i}/Color");
            labels[i] = GetNode<Label>($"Panel/Slot{i}/Label");
            labels[i].AddThemeFontOverride("font", new SystemFont { FontWeight = 700 });

            icons[i] = new TextureRect
            {
                AnchorLeft = 0f, AnchorRight = 1f, AnchorTop = 0f, AnchorBottom = 1f,
                OffsetLeft = 14f, OffsetRight = -14f, OffsetTop = 10f, OffsetBottom = -48f,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Visible = false,
            };
            buttons[i].AddChild(icons[i]);
            buttons[i].MoveChild(icons[i], labels[i].GetIndex());

            labels[i].AnchorLeft = 0f; labels[i].AnchorRight = 1f;
            labels[i].AnchorTop = 1f; labels[i].AnchorBottom = 1f;
            labels[i].OffsetLeft = 8f; labels[i].OffsetRight = -8f;
            labels[i].OffsetTop = -44f; labels[i].OffsetBottom = -6f;
            labels[i].AddThemeFontSizeOverride("font_size", 22);
            labels[i].AddThemeColorOverride("font_color", Colors.White);

            buttons[i].Pressed += () => Choose(idx);
        }

        declineButton = new Button
        {
            Text = "RECUSAR GIRO",
            AnchorLeft = 0.5f, AnchorRight = 0.5f, AnchorTop = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft = -150f, OffsetRight = 150f, OffsetTop = 118f, OffsetBottom = 160f,
            FocusMode = Control.FocusModeEnum.None,
        };
        declineButton.AddThemeFontSizeOverride("font_size", 22);
        declineButton.Pressed += Decline;
        panel.AddChild(declineButton);

        panel.Visible = false;
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
        AudioManager.Instance?.StopSlotSpin(); // o player é do autoload: sem isto o loop sobreviveria à cena
    }

    /// <summary>Sem estação (máquina antiga da arena/sandbox): custa 1 ficha.</summary>
    public void Open(LoungeStation fromStation = null)
    {
        var gm = GameManager.Instance;
        if (panel.Visible || WeaponPool == null || WeaponPool.Count == 0) return;
        if (fromStation != null ? !fromStation.CanInteract(gm.Chips) : gm.Chips <= 0) return;
        bool paid = fromStation != null ? fromStation.TryPay() : gm.TrySpendChip();
        if (!paid) return;

        gm.SetPaused(true);
        panel.Visible = true;
        Spin();
    }

    async void Spin()
    {
        SetButtons(false);
        resultText.Text = "Girando...";
        AudioManager.Instance?.StartSlotSpin();

        // Sorte (Tabela A) decide o tier ANTES da animação; os rolos só exibem o resultado.
        int tier = LuckTables.RollWeaponTier(MetaProgress.LuckLevel, RunState.Rng);
        reels = WeaponSlotRoll.BuildReels(new List<WeaponData>(WeaponPool), tier, RunState.Rng);

        for (float t = 0f; t < SpinDuration; t += SpinTick)
        {
            for (int i = 0; i < 3; i++) ShowReel(i, RandomWeapon());
            await ToSignal(GetTree().CreateTimer(SpinTick), SceneTreeTimer.SignalName.Timeout);
        }

        for (int i = 0; i < 3; i++) ShowReel(i, reels[i]);
        AudioManager.Instance?.StopSlotSpin();

        bool jackpot = SlotRules.IsJackpot(reels);
        resultText.Text = jackpot ? "JACKPOT!!! Escolha sua arma" : "Escolha uma arma ou recuse o giro";
        if (jackpot)
        {
            AudioManager.Instance?.PlayJackpot();
            JackpotFx();
        }
        else AudioManager.Instance?.PlaySlotWin();
        SetButtons(true);
        choosing = true;
    }

    void Choose(int index)
    {
        if (!choosing) return;
        choosing = false;
        SetButtons(false);

        var chosen = reels[index];
        int matches = SlotRules.CountMatches(reels, chosen);
        int levels = SlotRules.LevelsFor(matches);
        bool owned = WeaponSystem.Find(chosen) != null;

        WeaponSystem.AddOrUpgrade(chosen, levels);

        int now = WeaponSystem.Find(chosen).Level;
        resultText.Text = owned
            ? $"{chosen.WeaponName} x{matches}: +{levels} nível(is)! Agora Nv {now}" + (now >= WeaponInstance.MaxLevel ? " (MÁX)" : "")
            : $"{chosen.WeaponName} x{matches}: DESBLOQUEADA no Nv {now}!";
        CloseLater();
    }

    void Decline()
    {
        if (!choosing) return;
        choosing = false;
        SetButtons(false);
        resultText.Text = "Giro recusado. Suas armas continuam as mesmas.";
        CloseLater();
    }

    async void CloseLater()
    {
        await ToSignal(GetTree().CreateTimer(CloseDelay), SceneTreeTimer.SignalName.Timeout);
        panel.Visible = false;
        GameManager.Instance.SetPaused(false);
    }

    /// <summary>Jackpot: clarão dourado e confete saindo dos 3 rolos.</summary>
    void JackpotFx()
    {
        GameFeel.Flash(this, GameFeel.Gold);
        for (int i = 0; i < 3; i++)
            GameFeel.Burst(this, buttons[i].GetGlobalRect().GetCenter(), i == 1 ? GameFeel.Gold : GameFeel.Neon, 40, 520f, 1.1f, 16f);
    }

    WeaponData RandomWeapon() => WeaponPool[RunState.Rng.Next(WeaponPool.Count)];

    void ShowReel(int i, WeaponData w)
    {
        var tex = LoadTexture(WeaponFramePath(w));
        icons[i].Texture = tex;
        icons[i].Visible = tex != null;
        colors[i].Color = tex != null ? new Color(0.10f, 0.06f, 0.16f, 0.92f) : w.ProjectileColor;
        labels[i].AddThemeColorOverride("font_color", tex != null ? Colors.White : Colors.Black);
        labels[i].Text = w.WeaponName;
    }

    public static string WeaponFramePath(WeaponData w)
    {
        if (w == null) return "res://assets/molduras/armas/pistola.png";
        string key = (w.WeaponName + " " + w.ResourcePath).ToLowerInvariant();
        if (key.Contains("revolver") || key.Contains("revólver")) return "res://assets/molduras/armas/revolver.png";
        if (key.Contains("escopeta") || key.Contains("shotgun")) return "res://assets/molduras/armas/escopeta.png";
        if (key.Contains("metralhadora") || key.Contains("smg") || key.Contains("machine")) return "res://assets/molduras/armas/metralhadora.png";
        if (key.Contains("ak47")) return "res://assets/molduras/armas/ak47.png";
        if (key.Contains("m4")) return "res://assets/molduras/armas/m4.png";
        if (key.Contains("sniper")) return "res://assets/molduras/armas/sniper.png";
        if (key.Contains("rifle") || key.Contains("fuzil") || key.Contains("lancadados")) return "res://assets/molduras/armas/rifle.png";
        return "res://assets/molduras/armas/pistola.png";
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

    void SetButtons(bool on)
    {
        for (int i = 0; i < 3; i++)
        {
            buttons[i].Disabled = !on;
            colors[i].Modulate = new Color(1f, 1f, 1f, on ? 1f : 0.6f);
        }
        declineButton.Disabled = !on;
    }
}
