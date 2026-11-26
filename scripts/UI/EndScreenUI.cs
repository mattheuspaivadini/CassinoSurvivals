using Godot;

/// <summary>Tela de Game Over / Vitória com botão de reiniciar.</summary>
public partial class EndScreenUI : CanvasLayer
{
    [Export] public string VictoryTitle { get; set; } = "VOCÊ QUEBROU A BANCA!";

    Control panel;
    Label title, stats;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; // aparece com o jogo pausado
        panel = GetNode<Control>("Panel");
        title = GetNode<Label>("Panel/Title");
        stats = GetNode<Label>("Panel/Stats");
        panel.Visible = false;
        GameManager.Instance.StateChanged += OnStateChanged;
        GetNode<Button>("Panel/Restart").Pressed += GameManager.Instance.Restart;
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
        stats.Text = $"Pontos: {gm.Score}\nTempo: {t / 60}:{t % 60:00}";
        panel.Visible = true;
    }
}
