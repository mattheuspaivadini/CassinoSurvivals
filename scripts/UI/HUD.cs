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

    readonly StringBuilder sb = new StringBuilder(512);
    ColorRect hpFill; // pivot X=0; a largura é controlada por Scale.X
    Label hpText, scoreText, chipsText, timerText, bannerText;
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
        PlayerController.Instance.HpChanged += UpdateHp;
        EnemySpawner.Instance.BossSpawned += ShowBossBanner;

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
        weaponsText.Text = sb.ToString();
    }

    void RefreshTimer()
    {
        var gm = GameManager.Instance;
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

    void ShowBossBanner()
    {
        bannerText.Text = "O TIGRINHO CHEGOU!";
        bannerText.Visible = true;
        bannerTimer = 4f;
    }
}
