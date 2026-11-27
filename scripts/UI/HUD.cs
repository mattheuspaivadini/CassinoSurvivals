using System.Text;
using Godot;

/// <summary>
/// HUD dinâmico. HP/pontos/fichas só atualizam por signal; lista de armas e timer
/// atualizam a 10 Hz para não reconstruir o texto todo frame.
/// </summary>
public partial class HUD : CanvasLayer
{
    [Export] public WeaponSystem WeaponSystem { get; set; }
    [Export] public float RefreshInterval { get; set; } = 0.1f;
    /// <summary>Fases com horda finita: mostra Ato/Fase e inimigos restantes no lugar do timer do Tigrinho.</summary>
    [Export] public HordeDirector Horde { get; set; }

    readonly StringBuilder sb = new StringBuilder(512);
    ColorRect hpFill; // pivot X=0; a largura é controlada por Scale.X
    Label hpText, scoreText, chipsText, dollarsText, timerText, bannerText;
    RichTextLabel weaponsText;
    float refreshTimer, bannerTimer;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; // a lista de armas atualiza com o modal aberto
        hpFill = GetNode<ColorRect>("HpFill");
        hpText = GetNode<Label>("HpText");
        scoreText = GetNode<Label>("Score");
        chipsText = GetNode<Label>("Chips");
        timerText = GetNode<Label>("TimerLabel");
        bannerText = GetNode<Label>("Banner");
        weaponsText = GetNode<RichTextLabel>("Weapons");

        var gm = GameManager.Instance;
        gm.ScoreChanged += UpdateScore;
        gm.ChipsChanged += UpdateChips;
        // $ da partida logo abaixo das fichas (mesmo estilo).
        dollarsText = (Label)chipsText.Duplicate();
        dollarsText.Name = "Dollars";
        dollarsText.Position += new Vector2(0f, 45f);
        dollarsText.AddThemeColorOverride("font_color", new Color(0.45f, 1f, 0.5f));
        AddChild(dollarsText);
        gm.DollarsChanged += UpdateDollars;
        UpdateDollars(gm.RunDollars);
        PlayerController.Instance.HpChanged += UpdateHp;
        if (EnemySpawner.Instance != null)
            EnemySpawner.Instance.BossSpawned += ShowBossBanner;
        if (Horde != null)
        {
            Horde.Cleared += ShowClearedBanner;
            Horde.RoundStarted += ShowRoundBanner;
        }

        UpdateScore(gm.Score);
        UpdateChips(gm.Chips);
        UpdateHp(PlayerController.Instance.CurrentHp, PlayerController.Instance.MaxHp);
        bannerText.Visible = false;
    }

    // GameManager é autoload e sobrevive ao reload: desconecta para não chamar um nó liberado.
    public override void _ExitTree()
    {
        GameManager.Instance.ScoreChanged -= UpdateScore;
        GameManager.Instance.ChipsChanged -= UpdateChips;
        GameManager.Instance.DollarsChanged -= UpdateDollars;
    }

    public override void _Process(double delta)
    {
        if (bannerTimer > 0f && !GetTree().Paused) // como na Unity, o banner não conta com o jogo pausado
        {
            bannerTimer -= (float)delta;
            if (bannerTimer <= 0f) bannerText.Visible = false;
        }

        refreshTimer -= (float)delta;
        if (refreshTimer > 0f) return;
        refreshTimer = RefreshInterval;
        RefreshWeapons();
        RefreshTimer();
    }

    void RefreshWeapons()
    {
        sb.Clear();
        var list = WeaponSystem.Weapons;
        for (int i = 0; i < list.Count; i++)
        {
            var w = list[i];
            sb.Append(i == WeaponSystem.Selected ? "> " : "   ").Append("[color=#").Append(w.ColorHex).Append(']').Append(w.Data.WeaponName).Append("[/color]  Nv ")
              .Append(w.Level).Append("  ");
            if (w.IsReloading) sb.Append("[color=#FF5555]RECARREGANDO ").Append((int)(w.ReloadProgress * 100f)).Append("%[/color]");
            else sb.Append(w.Ammo).Append('/').Append(w.Data.Magazine(w.Level));
            sb.Append('\n');
        }
        sb.Append(AimSettings.Auto ? "[color=#7CFC9A]MIRA: AUTOMÁTICA[/color]" : "[color=#FFD54A]MIRA: MOUSE (clique atira)[/color]")
          .Append("  [color=#AAAAAA][T] troca · [1-").Append(Mathf.Min(list.Count, 9)).Append("] arma[/color]");
        weaponsText.Text = sb.ToString();
    }

    void RefreshTimer()
    {
        if (Horde != null)
        {
            if (Horde.IsCleared) timerText.Text = "SAÍDA ABERTA! SIGA PARA A PORTA DE CIMA";
            else if (Horde.IsSingleStage)
            {
                timerText.Text = (Horde.Round == 0 || Horde.InBreak)
                    ? $"{Horde.StageLabel}   |   PREPARAR..."
                    : $"{Horde.StageLabel}   |   INIMIGOS: {Horde.Remaining}";
            }
            else if (Horde.Round == 0 || Horde.InBreak)
                timerText.Text = $"{Horde.StageLabel}   |   PRÓXIMA: FASE {Horde.Round + 1}/{Horde.EndStage}";
            else
                timerText.Text = $"{Horde.StageLabel}   |   FASE {Horde.Round}/{Horde.EndStage}   |   INIMIGOS: {Horde.Remaining}";
            return;
        }
        var gm = GameManager.Instance;
        if (EnemySpawner.Instance == null)
        {
            timerText.Text = "SALA DE DESCANSO  |  SAÍDA SUPERIOR ABERTA";
            return;
        }
        if (EnemySpawner.Instance.HasBossSpawned) { timerText.Text = "TIGRINHO EM CAMPO!"; return; }
        int left = Mathf.Max(0, Mathf.CeilToInt(gm.BossSpawnTime - gm.Elapsed));
        timerText.Text = $"Tigrinho em {left / 60}:{left % 60:00}";
    }

    void UpdateHp(float hp, float max)
    {
        hpFill.Scale = new Vector2(Mathf.Clamp(hp / max, 0f, 1f), 1f);
        hpText.Text = $"{Mathf.CeilToInt(hp)} / {Mathf.CeilToInt(max)}";
    }

    void UpdateScore(int score) => scoreText.Text = $"PONTOS: {score}";
    void UpdateChips(int chips) => chipsText.Text = $"FICHAS: {chips}";
    void UpdateDollars(int dollars) => dollarsText.Text = $"$ {dollars}";

    void ShowClearedBanner() => ShowBanner("FASE LIMPA! A SAÍDA ABRIU", new Color(1f, 0.85f, 0.15f));

    void ShowRoundBanner(int round, bool boss)
    {
        string label = CampaignRules.ForStage(round).StageLabel;
        ShowBanner(boss ? $"{label} — BOSS!" : label, boss ? new Color(1f, 0.3f, 0.3f) : new Color(1f, 1f, 1f), 2f);
    }

    void ShowBossBanner() => ShowBanner("O TIGRINHO CHEGOU!", new Color(1f, 0.5f, 0f));

    void ShowBanner(string text, Color color, float seconds = 4f)
    {
        bannerText.Text = text;
        bannerText.AddThemeColorOverride("font_color", color);
        bannerText.Visible = true;
        bannerTimer = seconds;
    }
}
