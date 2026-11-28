using Godot;

/// <summary>Modal do caça-níquel: pausa o jogo, gira 3 roletas (tempo real), jogador escolhe 1 slot.</summary>
public partial class SlotMachineUI : CanvasLayer
{
    public static SlotMachineUI Instance { get; private set; }

    /// <summary>Banco de armas sorteáveis.</summary>
    [Export] public Godot.Collections.Array<WeaponData> WeaponPool { get; set; }
    [Export] public WeaponSystem WeaponSystem { get; set; }

    [Export] public float SpinDuration { get; set; } = 1.2f;
    [Export] public float SpinTick { get; set; } = 0.06f;
    [Export] public float CloseDelay { get; set; } = 1.3f;

    readonly WeaponData[] reels = new WeaponData[3];
    readonly Button[] buttons = new Button[3];
    readonly ColorRect[] colors = new ColorRect[3];
    readonly Label[] labels = new Label[3];
    Control panel;
    Label resultText;
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
            buttons[i].Pressed += () => Choose(idx);
        }
        panel.Visible = false;
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
        AudioManager.Instance?.StopSlotSpin(); // o player é do autoload: sem isto o loop sobreviveria à cena
    }

    public void Open()
    {
        if (panel.Visible || GameManager.Instance.Chips <= 0) return;
        GameManager.Instance.SetPaused(true);
        panel.Visible = true;
        Spin();
    }

    async void Spin()
    {
        SetButtons(false);
        resultText.Text = "Girando...";
        AudioManager.Instance?.StartSlotSpin();

        for (float t = 0f; t < SpinDuration; t += SpinTick)
        {
            for (int i = 0; i < 3; i++) ShowReel(i, RandomWeapon());
            await ToSignal(GetTree().CreateTimer(SpinTick), SceneTreeTimer.SignalName.Timeout);
        }

        for (int i = 0; i < 3; i++) { reels[i] = RandomWeapon(); ShowReel(i, reels[i]); }
        AudioManager.Instance?.StopSlotSpin();

        bool jackpot = SlotRules.IsJackpot(reels);
        resultText.Text = jackpot ? "JACKPOT!!! Escolha sua arma" : "Escolha uma arma";
        if (jackpot) AudioManager.Instance?.PlayJackpot();
        else AudioManager.Instance?.PlaySlotWin();
        SetButtons(true);
        choosing = true;
    }

    async void Choose(int index)
    {
        if (!choosing) return;
        choosing = false;
        SetButtons(false);

        var chosen = reels[index];
        int matches = SlotRules.CountMatches(reels, chosen);
        int levels = SlotRules.LevelsFor(matches);
        bool owned = WeaponSystem.Find(chosen) != null;

        GameManager.Instance.TrySpendChip();
        WeaponSystem.AddOrUpgrade(chosen, levels);

        resultText.Text = owned
            ? $"{chosen.WeaponName} x{matches}: +{levels} nível(is)! Agora Nv {WeaponSystem.Find(chosen).Level}"
            : $"{chosen.WeaponName} x{matches}: DESBLOQUEADA no Nv {levels}!";

        await ToSignal(GetTree().CreateTimer(CloseDelay), SceneTreeTimer.SignalName.Timeout);
        panel.Visible = false;
        GameManager.Instance.SetPaused(false);
    }

    WeaponData RandomWeapon() => WeaponPool[GD.RandRange(0, WeaponPool.Count - 1)];

    void ShowReel(int i, WeaponData w)
    {
        colors[i].Color = w.ProjectileColor;
        labels[i].Text = w.WeaponName;
    }

    void SetButtons(bool on)
    {
        for (int i = 0; i < 3; i++)
        {
            buttons[i].Disabled = !on;
            colors[i].Modulate = new Color(1f, 1f, 1f, on ? 1f : 0.6f);
        }
    }
}
