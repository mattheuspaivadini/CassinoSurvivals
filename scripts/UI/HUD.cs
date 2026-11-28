using System.Collections.Generic;
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
    TextureProgressBar playerHpBar;
    Texture2D playerHpFillTex, playerHpFlashTex;
    float playerHpFlashTimer, lastPlayerHp = -1f;
    Label hpText, scoreText, chipsText, dollarsText, timerText, bannerText;
    RichTextLabel weaponsText;
    HBoxContainer passivesRow;
    readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();
    float refreshTimer, bannerTimer;
    const float PlayerBarScale = 4f;
    const float PlayerFillOffsetX = 9f;
    const float PlayerFillOffsetY = 26f;
    const float PlayerHpFlashDuration = 0.14f;
    sealed class BossBarView
    {
        public Control Root;
        public TextureProgressBar Fill;
        public Label Name;
    }

    // Barra de HP do boss (topo central), montada em código.
    Control bossBar;
    ColorRect bossFill;
    Label bossName;
    readonly Dictionary<EnemyBehavior, BossBarView> customBossBars = new Dictionary<EnemyBehavior, BossBarView>();
    const float BossBarWidth = 760f;
    const float BossBarScale = 4f;
    const float BossFillOffsetX = 27f;
    const float BossFillOffsetY = 18f;

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
        if (Horde != null)
        {
            Horde.Cleared += ShowClearedBanner;
            Horde.RoundStarted += ShowRoundBanner;
        }

        UpdateScore(gm.Score);
        UpdateChips(gm.Chips);
        BuildPlayerHpBar();
        BuildPassivesRow();
        RunState.Changed += RefreshPassives;
        RefreshPassives();
        UpdateHp(PlayerController.Instance.CurrentHp, PlayerController.Instance.MaxHp);
        bannerText.Visible = false;
        if (gm.InLounge && gm.CurrentStage == 14)
            ShowBanner("AS MÁQUINAS DE CAÇA-NÍQUEL SUMIRAM!\nSÓ RESTAM FIOS SOLTOS... O BOSS FINAL TE ESPERA!", new Color(1f, 0.35f, 0.25f), 6.5f);
        BuildBossBar();
    }

    void BuildPlayerHpBar()
    {
        var frameTex = LoadTexture("res://assets/sprites/player_vbar_frame_empty.png");
        playerHpFillTex = LoadTexture("res://assets/sprites/player_vbar_fill.png");
        playerHpFlashTex = LoadTexture("res://assets/sprites/player_vbar_fill_flash.png");
        if (frameTex == null || playerHpFillTex == null) return;

        hpFill.Visible = false;
        GetNodeOrNull<CanvasItem>("../Canvas_Static/HpBarBG")?.Hide();
        weaponsText.OffsetTop = 30f;

        float w = frameTex.GetWidth() * PlayerBarScale;
        float h = frameTex.GetHeight() * PlayerBarScale;
        var root = new Control
        {
            AnchorLeft = 0f, AnchorRight = 0f,
            AnchorTop = 1f, AnchorBottom = 1f,
            OffsetLeft = 28f, OffsetRight = 28f + w,
            OffsetTop = -(144f + h), OffsetBottom = -144f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        root.AddChild(new TextureRect
        {
            Texture = frameTex,
            TextureFilter = Control.TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            Size = new Vector2(w, h),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        playerHpBar = new TextureProgressBar
        {
            TextureProgress = playerHpFillTex,
            TextureFilter = Control.TextureFilterEnum.Nearest,
            NinePatchStretch = false,
            FillMode = (int)TextureProgressBar.FillModeEnum.BottomToTop,
            MinValue = 0,
            MaxValue = 1,
            Step = 0.001,
            Value = 1,
            Position = new Vector2(PlayerFillOffsetX * PlayerBarScale, PlayerFillOffsetY * PlayerBarScale),
            Size = new Vector2(playerHpFillTex.GetWidth(), playerHpFillTex.GetHeight()),
            Scale = Vector2.One * PlayerBarScale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        root.AddChild(playerHpBar);
        AddChild(root);

        hpText.AnchorLeft = 0f; hpText.AnchorRight = 0f;
        hpText.AnchorTop = 1f; hpText.AnchorBottom = 1f;
        hpText.OffsetLeft = 20f; hpText.OffsetRight = 140f;
        hpText.OffsetTop = -142f; hpText.OffsetBottom = -116f;
        hpText.AddThemeFontSizeOverride("font_size", 20);
    }

    void BuildPassivesRow()
    {
        passivesRow = new HBoxContainer
        {
            AnchorLeft = 0f, AnchorRight = 0f,
            AnchorTop = 1f, AnchorBottom = 1f,
            OffsetLeft = 28f, OffsetRight = 1040f,
            OffsetTop = -104f, OffsetBottom = -20f,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        passivesRow.AddThemeConstantOverride("separation", 10);
        AddChild(passivesRow);
    }

    void RefreshPassives()
    {
        if (passivesRow == null) return;
        foreach (Node child in passivesRow.GetChildren())
            child.QueueFree();

        foreach (Passive p in RunState.Passives)
        {
            var tex = LoadTexture(LoungeModalUI.PassiveFramePath(p));
            if (tex == null) continue;
            passivesRow.AddChild(new TextureRect
            {
                Texture = tex,
                CustomMinimumSize = new Vector2(76f, 76f),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            });
        }
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

    void BuildBossBar()
    {
        bossBar = new Control
        {
            AnchorLeft = 0.5f, AnchorRight = 0.5f,
            OffsetLeft = -BossBarWidth / 2f, OffsetRight = BossBarWidth / 2f, OffsetTop = 78f, OffsetBottom = 128f,
            MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false,
        };
        bossBar.AddChild(new ColorRect { Color = new Color(0f, 0f, 0f, 0.7f), Position = new Vector2(0f, 22f), Size = new Vector2(BossBarWidth, 24f) });
        bossFill = new ColorRect { Color = new Color(0.9f, 0.15f, 0.2f), Position = new Vector2(3f, 25f), Size = new Vector2(BossBarWidth - 6f, 18f) };
        bossBar.AddChild(bossFill);
        bossName = new Label { HorizontalAlignment = HorizontalAlignment.Center, Size = new Vector2(BossBarWidth, 22f) };
        bossName.AddThemeFontSizeOverride("font_size", 20);
        bossBar.AddChild(bossName);
        AddChild(bossBar);

        BuildCustomBossBar(EnemyBehavior.Rainha, "res://assets/sprites/queen_healthbar_frame_empty.png", "res://assets/sprites/queen_healthbar_fill.png");
        BuildCustomBossBar(EnemyBehavior.Rei, "res://assets/sprites/king_healthbar_frame_empty.png", "res://assets/sprites/king_healthbar_fill.png");
        BuildCustomBossBar(EnemyBehavior.MaquinaCacaNiquel, "res://assets/sprites/slot_healthbar_frame_empty.png", "res://assets/sprites/slot_healthbar_fill.png");
    }

    void BuildCustomBossBar(EnemyBehavior behavior, string framePath, string fillPath)
    {
        var frameTex = LoadTexture(framePath);
        var fillTex = LoadTexture(fillPath);
        if (frameTex == null || fillTex == null) return;

        float w = frameTex.GetWidth() * BossBarScale;
        float h = frameTex.GetHeight() * BossBarScale;
        var root = new Control
        {
            AnchorLeft = 0.5f, AnchorRight = 0.5f,
            OffsetLeft = -w / 2f, OffsetRight = w / 2f, OffsetTop = 64f, OffsetBottom = 64f + h,
            MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false,
        };
        root.AddChild(new TextureRect
        {
            Texture = frameTex,
            TextureFilter = Control.TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            Size = new Vector2(w, h),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        var fill = new TextureProgressBar
        {
            TextureProgress = fillTex,
            TextureFilter = Control.TextureFilterEnum.Nearest,
            NinePatchStretch = false,
            FillMode = (int)TextureProgressBar.FillModeEnum.LeftToRight,
            MinValue = 0,
            MaxValue = 1,
            Step = 0.001,
            Value = 1,
            Position = new Vector2(BossFillOffsetX * BossBarScale, BossFillOffsetY * BossBarScale),
            Size = new Vector2(fillTex.GetWidth(), fillTex.GetHeight()),
            Scale = Vector2.One * BossBarScale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        root.AddChild(fill);
        var nameLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(BossFillOffsetX * BossBarScale, 32f),
            Size = new Vector2(fillTex.GetWidth() * BossBarScale, 28f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 20);
        root.AddChild(nameLabel);
        AddChild(root);

        customBossBars[behavior] = new BossBarView { Root = root, Fill = fill, Name = nameLabel };
    }

    /// <summary>Mostra o boss vivo da sala; dourada enquanto ele está invulnerável (Trava de Fase).</summary>
    void RefreshBossBar()
    {
        Enemy boss = null;
        var spawner = EnemySpawner.Instance;
        if (spawner != null)
            foreach (var e in spawner.Active)
                if (e.Data != null && e.Data.IsBoss && !e.IsDead) { boss = e; break; }

        BossBarView activeCustom = null;
        if (boss != null) customBossBars.TryGetValue(boss.Data.Behavior, out activeCustom);

        bossBar.Visible = boss != null && activeCustom == null;
        foreach (var view in customBossBars.Values)
            view.Root.Visible = view == activeCustom;
        if (boss == null) return;

        float frac = boss.MaxHp > 0f ? Mathf.Clamp(boss.Hp / boss.MaxHp, 0f, 1f) : 0f;
        bool shielded = boss.Brain is BossBrain bb && bb.Invulnerable;
        bool fury = boss.Brain is BossBrain bf && bf.Fury;
        string label = shielded ? $"{boss.Data.EnemyName.ToUpper()}  —  PROTEGIDO" : boss.Data.EnemyName.ToUpper();

        if (activeCustom != null)
        {
            activeCustom.Fill.Value = frac;
            activeCustom.Fill.Modulate = shielded ? new Color(1f, 0.88f, 0.35f) : Colors.White;
            activeCustom.Name.Text = label;
            return;
        }

        bossFill.Size = new Vector2((BossBarWidth - 6f) * frac, 18f);
        bossFill.Color = shielded ? new Color(1f, 0.82f, 0.25f) : fury ? new Color(1f, 0.35f, 0.1f) : new Color(0.9f, 0.15f, 0.2f);
        bossName.Text = label;
    }

    // GameManager é autoload e sobrevive ao reload: desconecta para não chamar um nó liberado.
    public override void _ExitTree()
    {
        RunState.Changed -= RefreshPassives;
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

        if (playerHpFlashTimer > 0f)
        {
            playerHpFlashTimer -= (float)delta;
            if (playerHpFlashTimer <= 0f && playerHpBar != null && playerHpFillTex != null)
                playerHpBar.TextureProgress = playerHpFillTex;
        }

        RefreshBossBar();

        refreshTimer -= (float)delta;
        if (refreshTimer > 0f) return;
        refreshTimer = RefreshInterval;
        RefreshWeapons();
        RefreshTimer();
    }

    // Cores das bebidas no HUD (mesmas dos drops): Cura, Dano, Velocidade.
    static readonly string[] DrinkHex = { "FF73BF", "FF5933", "B373FF" };

    /// <summary>Bebidas ativas com segundos restantes.</summary>
    void AppendRunBuffs()
    {
        bool any = false;
        for (int i = 0; i < DrinkRules.Count; i++)
        {
            var t = (DrinkType)i;
            if (!RunState.IsActive(t)) continue;
            sb.Append("[color=#").Append(DrinkHex[i]).Append("]● ").Append(DrinkRules.Name(t)).Append(' ')
              .Append(Mathf.CeilToInt(RunState.TimeLeft(t))).Append("s[/color]   ");
            any = true;
        }
        if (any) sb.Append('\n');
    }

    void RefreshWeapons()
    {
        sb.Clear();
        AppendRunBuffs();
        var list = WeaponSystem.Weapons;
        for (int i = 0; i < list.Count; i++)
        {
            var w = list[i];
            sb.Append(i == WeaponSystem.Selected ? "> " : "   ").Append("[color=#").Append(w.ColorHex).Append(']').Append(w.Data.WeaponName).Append("[/color]  Nv ")
              .Append(w.Level).Append("  ");
            if (w.IsReloading) sb.Append("[color=#FF5555]RECARREGANDO ").Append((int)(w.ReloadProgress * 100f)).Append("%[/color]");
            else sb.Append(w.Ammo).Append('/').Append(w.MagazineSize);
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
            timerText.Text = gm.CampaignActive && gm.CurrentStage == 14
                ? "SALA DE DESCANSO — MÁQUINAS SUMIRAM! SIGA PARA O BOSS FINAL"
                : gm.CampaignActive
                    ? $"SALA DE DESCANSO (APÓS FASE {gm.CurrentStage}/15)  |  SAÍDA SUPERIOR ABERTA"
                    : "SALA DE DESCANSO  |  SAÍDA SUPERIOR ABERTA";
            return;
        }
        if (EnemySpawner.Instance.HasBossSpawned) { timerText.Text = "TIGRINHO EM CAMPO!"; return; }
        int left = Mathf.Max(0, Mathf.CeilToInt(gm.BossSpawnTime - gm.Elapsed));
        timerText.Text = $"Tigrinho em {left / 60}:{left % 60:00}";
    }

    void UpdateHp(float hp, float max)
    {
        float frac = max > 0f ? Mathf.Clamp(hp / max, 0f, 1f) : 0f;
        if (playerHpBar != null)
        {
            playerHpBar.Value = frac;
            if (lastPlayerHp >= 0f && hp < lastPlayerHp && playerHpFlashTex != null)
            {
                playerHpBar.TextureProgress = playerHpFlashTex;
                playerHpFlashTimer = PlayerHpFlashDuration;
            }
            lastPlayerHp = hp;
        }
        else
        {
            hpFill.Scale = new Vector2(frac, 1f);
        }
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

    void ShowBanner(string text, Color color, float seconds = 4f)
    {
        bannerText.Text = text;
        bannerText.AddThemeColorOverride("font_color", color);
        bannerText.Visible = true;
        bannerTimer = seconds;
    }
}
