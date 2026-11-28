using System.Collections.Generic;
using Godot;

/// <summary>
/// Painel e atalhos de debug do Modo Sandbox (issue #23).
/// Aberto pelo botão SANDBOX do Menu Principal ou pela flag CLI --enemy-sandbox; 100% ausente na campanha normal.
/// Atalhos: [F1] mostra/oculta painel, [G] Dourado, [I] God Mode, [H] Cura, [F6] Trava, [F7] Fúria, [F8/Backspace] Limpar.
/// </summary>
public partial class EnemySandboxUI : CanvasLayer
{
    PanelContainer panel;
    Button collapsedPill;
    Button goldenButton, godModeButton, forceLockButton, forceFuryButton;
    Label statusLabel, stageLabel, bossInfoLabel;

    const int StatStepPercent = 10;
    static readonly Color TabActiveColor = new Color(0.35f, 0.95f, 1f);

    readonly List<VBoxContainer> pages = new List<VBoxContainer>();
    readonly List<Button> pageTabs = new List<Button>();
    readonly Button[] passiveButtons = new Button[PassiveCatalog.Count];
    readonly Label[] statLabels = new Label[System.Enum.GetValues(typeof(UpgradeStat)).Length];

    bool goldenMode;
    bool godMode;
    int stageScale = 1;
    float refreshTimer;

    public override void _Ready()
    {
        bool allowed = (GameManager.Instance != null && GameManager.Instance.EnemySandboxActive) ||
                       GameManager.HasEnemySandboxFlag(OS.GetCmdlineUserArgs());
        if (!allowed)
        {
            QueueFree();
            return;
        }

        Layer = 8;
        BuildUi();
        RefreshStateLabels();
    }

    static WeaponSystem CurrentWeapons() => PlayerController.Instance?.GetNodeOrNull<WeaponSystem>("WeaponSystem");

    public override void _Process(double delta)
    {
        refreshTimer -= (float)delta;
        if (refreshTimer > 0f) return;
        refreshTimer = 0.15f;
        RefreshBossInfo();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;
        if (e is not InputEventKey key || !key.Pressed || key.Echo) return;

        switch (key.Keycode)
        {
            case Key.F1:
                TogglePanel();
                GetViewport().SetInputAsHandled();
                break;
            case Key.G:
                ToggleGolden();
                GetViewport().SetInputAsHandled();
                break;
            case Key.I:
                ToggleGodMode();
                GetViewport().SetInputAsHandled();
                break;
            case Key.H:
                HealPlayer();
                GetViewport().SetInputAsHandled();
                break;
            case Key.F6:
                ForceBossLock();
                GetViewport().SetInputAsHandled();
                break;
            case Key.F7:
                ForceBossFury();
                GetViewport().SetInputAsHandled();
                break;
            case Key.F8:
            case Key.Backspace:
                ClearArena();
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    void BuildUi()
    {
        collapsedPill = new Button
        {
            Text = "SANDBOX [F1]",
            AnchorLeft = 1f, AnchorRight = 1f, AnchorTop = 0f, AnchorBottom = 0f,
            OffsetLeft = -180f, OffsetRight = -16f, OffsetTop = 200f, OffsetBottom = 238f,
            FocusMode = Control.FocusModeEnum.None,
            Visible = false,
        };
        collapsedPill.AddThemeFontSizeOverride("font_size", 16);
        collapsedPill.Pressed += TogglePanel;
        AddChild(collapsedPill);

        panel = new PanelContainer
        {
            AnchorLeft = 1f, AnchorRight = 1f, AnchorTop = 0f, AnchorBottom = 1f,
            OffsetLeft = -340f, OffsetRight = -16f, OffsetTop = 196f, OffsetBottom = -140f,
        };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.02f, 0.11f, 0.90f),
            BorderColor = new Color(0.35f, 0.92f, 1f, 0.85f),
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 10, CornerRadiusTopRight = 10, CornerRadiusBottomLeft = 10, CornerRadiusBottomRight = 10,
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 10, ContentMarginBottom = 10,
        });
        AddChild(panel);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        panel.AddChild(scroll);

        var col = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        col.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(col);

        var headerRow = new HBoxContainer();
        col.AddChild(headerRow);
        var title = MakeLabel("SANDBOX / DEBUG", 19, new Color(0.35f, 0.95f, 1f));
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        headerRow.AddChild(title);
        var hideBtn = MakeSmallButton("F1 −", TogglePanel);
        headerRow.AddChild(hideBtn);

        statusLabel = MakeLabel("Pronto · [1-2/Scroll] troca de arma", 13, new Color(0.78f, 0.84f, 0.92f));
        col.AddChild(statusLabel);

        // Opções de spawn e jogador
        var optRow = new HBoxContainer();
        optRow.AddThemeConstantOverride("separation", 6);
        col.AddChild(optRow);
        goldenButton = MakeButton("DOURADO: OFF [G]", ToggleGolden);
        godModeButton = MakeButton("GOD: OFF [I]", ToggleGodMode);
        optRow.AddChild(goldenButton);
        optRow.AddChild(godModeButton);

        var stageRow = new HBoxContainer();
        stageRow.AddThemeConstantOverride("separation", 6);
        col.AddChild(stageRow);
        stageRow.AddChild(MakeSmallButton("-", () => ChangeStage(-1)));
        stageLabel = MakeLabel("Escala F1 (1,00x)", 14, Colors.White);
        stageLabel.HorizontalAlignment = HorizontalAlignment.Center;
        stageLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        stageRow.AddChild(stageLabel);
        stageRow.AddChild(MakeSmallButton("+", () => ChangeStage(1)));
        stageRow.AddChild(MakeSmallButton("CURAR [H]", HealPlayer));

        var chipsRow = new HBoxContainer();
        chipsRow.AddThemeConstantOverride("separation", 6);
        col.AddChild(chipsRow);
        var chipsButton = MakeButton("+10 FICHAS", () => GiveChips(10));
        chipsButton.AddThemeColorOverride("font_color", GameFeel.Gold);
        chipsRow.AddChild(chipsButton);
        chipsRow.AddChild(MakeButton("+50 FICHAS", () => GiveChips(50)));

        var tabRow = new GridContainer { Columns = 3 };
        tabRow.AddThemeConstantOverride("h_separation", 4);
        tabRow.AddThemeConstantOverride("v_separation", 4);
        col.AddChild(tabRow);
        var enemiesPage = AddPage(col, tabRow, "INIMIGOS");
        var weaponsPage = AddPage(col, tabRow, "ARMAS");
        var passivesPage = AddPage(col, tabRow, "PASSIVAS");
        var statsPage = AddPage(col, tabRow, "ATRIBUTOS");
        var roomsPage = AddPage(col, tabRow, "SALAS");
        BuildPassivesPage(passivesPage);
        BuildStatsPage(statsPage);
        BuildRoomsPage(roomsPage);
        ShowPage(0);

        weaponsPage.AddChild(MakeSectionHeader("ARMAS (2 SLOTS · PEGAR TROCA A DA MÃO)"));
        foreach (string path in WeaponSystem.OfficialWeaponPaths)
        {
            var data = GD.Load<WeaponData>(path);
            if (data == null) continue;
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 4);
            weaponsPage.AddChild(row);
            var name = MakeLabel(data.WeaponName, 13, new Color(0.85f, 0.85f, 0.9f));
            name.AutowrapMode = TextServer.AutowrapMode.Off;
            name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(name);
            row.AddChild(MakeSmallButton("Pegar", () => GiveWeapon(data, 0)));
            row.AddChild(MakeSmallButton("+1", () => GiveWeapon(data, 1)));
            row.AddChild(MakeSmallButton("Máx ★", () => GiveWeapon(data, WeaponInstance.MaxLevel)));
        }

        var levelRow = new HBoxContainer();
        levelRow.AddThemeConstantOverride("separation", 6);
        weaponsPage.AddChild(levelRow);
        var levelLabel = MakeLabel("Na mão:", 13, new Color(0.85f, 0.85f, 0.9f));
        levelLabel.AutowrapMode = TextServer.AutowrapMode.Off;
        levelRow.AddChild(levelLabel);
        levelRow.AddChild(MakeButton("Nv 1", () => SetSelectedWeaponLevel(1)));
        levelRow.AddChild(MakeButton("Nv 5", () => SetSelectedWeaponLevel(5)));
        levelRow.AddChild(MakeButton("Nv 10 ★", () => SetSelectedWeaponLevel(10)));

        // 7 arquétipos de inimigos
        enemiesPage.AddChild(MakeSectionHeader("INIMIGOS (7 ARQUÉTIPOS)"));
        var enemyGrid = new GridContainer { Columns = 2 };
        enemyGrid.AddThemeConstantOverride("h_separation", 6);
        enemyGrid.AddThemeConstantOverride("v_separation", 6);
        enemiesPage.AddChild(enemyGrid);
        foreach (var opt in EnemySandboxRules.Enemies)
        {
            var captured = opt;
            enemyGrid.AddChild(MakeButton(captured.Label, () => SpawnEnemy(captured)));
        }

        // 3 Bosses
        enemiesPage.AddChild(MakeSectionHeader("BOSSES (OFICIAIS)"));
        foreach (var bossOpt in EnemySandboxRules.Bosses)
        {
            var captured = bossOpt;
            enemiesPage.AddChild(MakeButton($"Invocar {captured.Label}", () => SpawnBoss(captured)));
        }

        // Transições de Trava de Fase / Fúria
        enemiesPage.AddChild(MakeSectionHeader("TRANSIÇÕES DO BOSS"));
        bossInfoLabel = MakeLabel("Nenhum boss ativo", 13, new Color(0.85f, 0.8f, 0.7f));
        enemiesPage.AddChild(bossInfoLabel);

        var bossCtrlRow = new HBoxContainer();
        bossCtrlRow.AddThemeConstantOverride("separation", 6);
        enemiesPage.AddChild(bossCtrlRow);
        forceLockButton = MakeButton("TRAVA [F6]", ForceBossLock);
        forceFuryButton = MakeButton("FÚRIA [F7]", ForceBossFury);
        bossCtrlRow.AddChild(forceLockButton);
        bossCtrlRow.AddChild(forceFuryButton);

        // Limpar arena
        var clearBtn = MakeButton("LIMPAR ARENA [F8 / Backspace]", ClearArena);
        clearBtn.AddThemeColorOverride("font_color", new Color(1f, 0.45f, 0.45f));
        enemiesPage.AddChild(clearBtn);
    }

    VBoxContainer AddPage(VBoxContainer parent, GridContainer tabRow, string title)
    {
        int index = pages.Count;
        var page = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        page.AddThemeConstantOverride("separation", 8);
        parent.AddChild(page);
        pages.Add(page);

        var tab = MakeButton(title, () => ShowPage(index));
        tab.AddThemeFontSizeOverride("font_size", 12);
        tabRow.AddChild(tab);
        pageTabs.Add(tab);
        return page;
    }

    void ShowPage(int index)
    {
        for (int i = 0; i < pages.Count; i++)
        {
            bool active = i == index;
            pages[i].Visible = active;
            pageTabs[i].AddThemeColorOverride("font_color", active ? TabActiveColor : new Color(0.7f, 0.7f, 0.78f));
        }
        RefreshPassives();
        RefreshStats();
    }

    void BuildPassivesPage(VBoxContainer page)
    {
        page.AddChild(MakeSectionHeader("PASSIVAS DA ROLETA (CLIQUE LIGA/DESLIGA)"));
        for (int i = 0; i < PassiveCatalog.Count; i++)
        {
            var passive = (Passive)i;
            var button = MakeButton(PassiveCatalog.Name(passive), () => TogglePassive(passive));
            button.TooltipText = PassiveCatalog.Description(passive);
            page.AddChild(button);
            passiveButtons[i] = button;
            var description = MakeLabel(PassiveCatalog.Description(passive), 11, new Color(0.68f, 0.68f, 0.76f));
            page.AddChild(description);
        }
    }

    void BuildStatsPage(VBoxContainer page)
    {
        page.AddChild(MakeSectionHeader($"ATRIBUTOS DA RUN (±{StatStepPercent}% POR CLIQUE)"));
        foreach (UpgradeStat stat in System.Enum.GetValues(typeof(UpgradeStat)))
        {
            var captured = stat;
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 4);
            page.AddChild(row);
            var label = MakeLabel("", 13, new Color(0.85f, 0.85f, 0.9f));
            label.AutowrapMode = TextServer.AutowrapMode.Off;
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(label);
            statLabels[(int)stat] = label;
            row.AddChild(MakeSmallButton($"-{StatStepPercent}%", () => ChangeStat(captured, -StatStepPercent)));
            row.AddChild(MakeSmallButton($"+{StatStepPercent}%", () => ChangeStat(captured, StatStepPercent)));
        }

        page.AddChild(MakeSectionHeader("BEBIDAS"));
        var drinkRow = new HBoxContainer();
        drinkRow.AddThemeConstantOverride("separation", 4);
        page.AddChild(drinkRow);
        foreach (DrinkType drink in System.Enum.GetValues(typeof(DrinkType)))
        {
            var captured = drink;
            drinkRow.AddChild(MakeButton(DrinkRules.Name(captured), () =>
            {
                RunState.Consume(captured);
                SetStatus($"Bebida: {DrinkRules.Name(captured)}");
            }));
        }
    }

    void BuildRoomsPage(VBoxContainer page)
    {
        page.AddChild(MakeSectionHeader("IR PARA A SALA (SEM HORDA, MANTÉM A BUILD)"));
        for (int act = 1; act <= 3; act++)
        {
            var theme = CampaignRules.GetActTheme(act);
            page.AddChild(MakeButton($"Ato {act}: {theme.Name}", () => GoToRoom(theme.NormalScenePath)));
        }
        page.AddChild(MakeButton("Arena do Boss", () => GoToRoom(CampaignRules.BossScenePath)));
        page.AddChild(MakeButton("Sala de Descanso", () => GoToRoom(GameManager.LoungeScenePath)));
        page.AddChild(MakeLabel("A porta de saída de qualquer sala volta para a Arena do Boss.", 11, new Color(0.68f, 0.68f, 0.76f)));

        page.AddChild(MakeSectionHeader("RECURSOS (PARA AS ESTAÇÕES DA SALA)"));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);
        page.AddChild(row);
        row.AddChild(MakeButton("+10 fichas", () => GiveChips(10)));
        row.AddChild(MakeButton("+$100", () =>
        {
            GameManager.Instance?.AddDollars(100);
            SetStatus($"$ da run: {GameManager.Instance?.RunDollars}");
        }));
    }

    void GiveChips(int amount)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.AddChips(amount);
        SetStatus($"Fichas: {gm.Chips}");
    }

    void GoToRoom(string scenePath)
    {
        GameManager.Instance?.SandboxGoTo(scenePath);
    }

    static string StatName(UpgradeStat stat) => stat switch
    {
        UpgradeStat.MaxHp => "Vida máx",
        UpgradeStat.MoveSpeed => "Velocidade",
        UpgradeStat.DashCooldown => "Recarga dash",
        UpgradeStat.Damage => "Dano",
        UpgradeStat.Magazine => "Pente",
        UpgradeStat.Reload => "Recarga arma",
        _ => stat.ToString(),
    };

    void TogglePassive(Passive passive)
    {
        bool had = RunState.Has(passive);
        if (had) RunState.RemovePassive(passive);
        else RunState.AddPassive(passive);
        RefreshPassives();
        SetStatus($"{PassiveCatalog.Name(passive)} {(had ? "desligada" : "ligada")}");
    }

    void ChangeStat(UpgradeStat stat, int percent)
    {
        int current = Mathf.RoundToInt(RunState.Bonus(stat) * 100f);
        int delta = Mathf.Max(percent, -current);
        if (delta == 0) return;
        RunState.ApplyUpgrade(new UpgradeResult(UpgradeRoll.TargetOf(stat), stat, delta));
        RefreshStats();
        SetStatus($"{StatName(stat)}: +{current + delta}%");
    }

    void RefreshPassives()
    {
        for (int i = 0; i < passiveButtons.Length; i++)
        {
            var button = passiveButtons[i];
            if (button == null) continue;
            bool on = RunState.Has((Passive)i);
            button.Text = on ? $"✓ {PassiveCatalog.Name((Passive)i)}" : PassiveCatalog.Name((Passive)i);
            button.AddThemeColorOverride("font_color", on ? GameFeel.Gold : Colors.White);
        }
    }

    void RefreshStats()
    {
        foreach (UpgradeStat stat in System.Enum.GetValues(typeof(UpgradeStat)))
        {
            var label = statLabels[(int)stat];
            if (label != null) label.Text = $"{StatName(stat)}: +{Mathf.RoundToInt(RunState.Bonus(stat) * 100f)}%";
        }
    }

    static Label MakeLabel(string text, int size, Color color)
    {
        var l = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", color);
        return l;
    }

    static Label MakeSectionHeader(string text)
    {
        var l = MakeLabel(text, 13, new Color(1f, 0.85f, 0.25f));
        l.HorizontalAlignment = HorizontalAlignment.Left;
        return l;
    }

    static Button MakeButton(string text, System.Action onPressed)
    {
        var b = new Button
        {
            Text = text,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0f, 32f),
            FocusMode = Control.FocusModeEnum.None,
        };
        b.AddThemeFontSizeOverride("font_size", 14);
        b.Pressed += onPressed;
        return b;
    }

    static Button MakeSmallButton(string text, System.Action onPressed)
    {
        var b = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(36f, 30f),
            FocusMode = Control.FocusModeEnum.None,
        };
        b.AddThemeFontSizeOverride("font_size", 13);
        b.Pressed += onPressed;
        return b;
    }

    void TogglePanel()
    {
        panel.Visible = !panel.Visible;
        collapsedPill.Visible = !panel.Visible;
    }

    void ToggleGolden()
    {
        goldenMode = !goldenMode;
        RefreshStateLabels();
        SetStatus(goldenMode ? "Variante Dourada ATIVADA" : "Variante Dourada DESATIVADA");
    }

    void ToggleGodMode()
    {
        godMode = !godMode;
        if (PlayerController.Instance != null && IsInstanceValid(PlayerController.Instance))
            PlayerController.Instance.DebugGodMode = godMode;
        RefreshStateLabels();
        SetStatus(godMode ? "God Mode ATIVADO (sem dano)" : "God Mode DESATIVADO");
    }

    void ChangeStage(int delta)
    {
        stageScale = Mathf.Clamp(stageScale + delta, 1, CampaignRules.TotalStages);
        HordeDirector.Instance?.SetSandboxStageScale(stageScale);
        RefreshStateLabels();
    }

    void HealPlayer()
    {
        var p = PlayerController.Instance;
        if (p == null || !IsInstanceValid(p)) return;
        p.Heal(p.MaxHp);
        SetStatus("HP restaurado (100%)");
    }

    void GiveWeapon(WeaponData data, int extraLevels)
    {
        var ws = CurrentWeapons();
        if (ws == null) return;
        var owned = ws.Find(data);
        if (owned != null)
        {
            if (extraLevels > 0) ws.AddOrUpgrade(data, extraLevels);
            SetStatus($"{data.WeaponName} Nv {owned.Level}");
            return;
        }
        int level = Mathf.Max(1, extraLevels);
        if (ws.NeedsSlotReplacement(data)) ws.ReplaceSlot(ws.Selected, data, level);
        else ws.AddOrUpgrade(data, level);
        SetStatus($"Pegou {data.WeaponName} (Nv {level})");
    }

    void SetSelectedWeaponLevel(int level)
    {
        var ws = CurrentWeapons();
        if (ws == null || ws.Weapons.Count == 0) return;
        int idx = Mathf.Clamp(ws.Selected, 0, ws.Weapons.Count - 1);
        var data = ws.Weapons[idx].Data;
        ws.ReplaceSlot(idx, data, level);
        SetStatus($"{data.WeaponName} ajustada para Nv {level}");
    }

    void SpawnEnemy(EnemySandboxRules.EnemyOption opt)
    {
        var spawner = EnemySpawner.Instance;
        if (spawner == null) return;
        var data = EnemySandboxRules.BuildEnemyData(opt.Kind, goldenMode);
        if (data == null) return;

        var (hpScale, dmgScale) = EnemySandboxRules.StageScaling(stageScale);
        Vector2 pos = FindSafeSpawnPosition();
        spawner.Spawn(data, pos, hpScale, isExtra: false, damageMultiplier: dmgScale);
        SetStatus($"Invocado: {data.EnemyName} (F{stageScale})");
    }

    void SpawnBoss(EnemySandboxRules.BossOption opt)
    {
        var spawner = EnemySpawner.Instance;
        if (spawner == null) return;

        // Substitui boss anterior se já houver um em campo para manter barra/controles limpos.
        var existingBoss = FindActiveBoss();
        if (existingBoss != null) existingBoss.Kill();

        var data = EnemySandboxRules.BuildBossData(opt.Boss, out StageSpec stageSpec);
        if (data == null) return;

        Arena.Instance?.ApplyActTheme(stageSpec.Act);
        HordeDirector.Instance?.SetSandboxStageScale(stageSpec.Stage);

        Vector2 pos = FindBossSpawnPosition();
        spawner.Spawn(data, pos, 1f, isExtra: false, damageMultiplier: stageSpec.DamageScale);
        spawner.AnnounceBoss();
        AudioManager.Instance?.PlayBossMusic();
        RefreshBossInfo();
        SetStatus($"Boss: {data.EnemyName} ({data.MaxHp:0} HP | cap {data.BossDpsCap:0})");
    }

    void ForceBossLock()
    {
        var boss = FindActiveBoss();
        if (boss?.Brain is not BossBrain brain)
        {
            SetStatus("Nenhum boss ativo para travar");
            return;
        }
        bool ok = brain.DebugForceNextLock();
        RefreshBossInfo();
        SetStatus(ok ? $"Trava de Fase disparada em {boss.Data.EnemyName}!" : "Boss já em trava ou sem travas restantes");
    }

    void ForceBossFury()
    {
        var boss = FindActiveBoss();
        if (boss?.Brain is not BossBrain brain)
        {
            SetStatus("Nenhum boss ativo para Fúria");
            return;
        }
        brain.DebugForceFury();
        RefreshBossInfo();
        SetStatus($"Fúria ativada em {boss.Data.EnemyName} (<40% HP)!");
    }

    void ClearArena()
    {
        int removed = 0;
        var spawner = EnemySpawner.Instance;
        if (spawner != null)
        {
            var copy = new List<Enemy>(spawner.Active);
            for (int i = 0; i < copy.Count; i++)
            {
                if (!IsInstanceValid(copy[i]) || copy[i].IsDead) continue;
                copy[i].Kill();
                removed++;
            }
        }

        var root = GetTree()?.CurrentScene;
        if (root != null)
        {
            foreach (Node child in root.GetChildren())
            {
                if (child is EnemyProjectile || child is ChampagnePuddle || child is Telegraph)
                    child.QueueFree();
            }
        }

        AudioManager.Instance?.PlayMainMusic();
        RefreshBossInfo();
        SetStatus($"Arena limpa ({removed} inimigo(s) removidos)");
    }

    Enemy FindActiveBoss()
    {
        var spawner = EnemySpawner.Instance;
        if (spawner == null) return null;
        foreach (var e in spawner.Active)
            if (IsInstanceValid(e) && !e.IsDead && e.Data != null && e.Data.IsBoss) return e;
        return null;
    }

    Vector2 FindBossSpawnPosition()
    {
        var root = GetTree()?.CurrentScene;
        var marker = root?.GetNodeOrNull<Node2D>("BossSpawn");
        if (marker != null) return marker.GlobalPosition;
        if (Arena.Instance != null)
            return new Vector2(Arena.Instance.Bounds.GetCenter().X, Arena.Instance.Bounds.Position.Y + 3.2f * Units.Px);
        return (PlayerController.Instance?.GlobalPosition ?? Vector2.Zero) + new Vector2(0f, -5f * Units.Px);
    }

    Vector2 FindSafeSpawnPosition()
    {
        Vector2 origin = PlayerController.Instance != null && IsInstanceValid(PlayerController.Instance)
            ? PlayerController.Instance.GlobalPosition
            : Vector2.Zero;
        var arena = Arena.Instance;
        float dist = 4.5f * Units.Px;

        for (int attempt = 0; attempt < 16; attempt++)
        {
            float angle = GD.Randf() * Mathf.Tau;
            Vector2 candidate = origin + Vector2.FromAngle(angle) * dist;
            if (arena != null)
            {
                Rect2 inner = arena.Bounds.Grow(-1.4f * Units.Px);
                candidate = candidate.Clamp(inner.Position, inner.End);
                if (arena.IsSolid(candidate)) continue;
            }
            if ((candidate - origin).LengthSquared() >= (2.2f * Units.Px) * (2.2f * Units.Px))
                return candidate;
        }
        return arena != null ? arena.Bounds.GetCenter() : origin + new Vector2(dist, 0f);
    }

    void RefreshStateLabels()
    {
        goldenButton.Text = goldenMode ? "DOURADO: ON ★ [G]" : "DOURADO: OFF [G]";
        goldenButton.AddThemeColorOverride("font_color", goldenMode ? GameFeel.Gold : Colors.White);

        godModeButton.Text = godMode ? "GOD: ON [I]" : "GOD: OFF [I]";
        godModeButton.AddThemeColorOverride("font_color", godMode ? new Color(0.35f, 1f, 0.95f) : Colors.White);

        var (hpScale, dmgScale) = EnemySandboxRules.StageScaling(stageScale);
        stageLabel.Text = $"F{stageScale} (HP {hpScale:0.00}x · Dano {dmgScale:0.00}x)";
    }

    void RefreshBossInfo()
    {
        var boss = FindActiveBoss();
        if (boss == null)
        {
            bossInfoLabel.Text = "Nenhum boss ativo";
            bossInfoLabel.AddThemeColorOverride("font_color", new Color(0.68f, 0.68f, 0.76f));
            forceLockButton.Disabled = true;
            forceFuryButton.Disabled = true;
            return;
        }

        var brain = boss.Brain as BossBrain;
        float pct = boss.MaxHp > 0f ? boss.Hp / boss.MaxHp * 100f : 0f;
        string tags = "";
        if (brain != null && brain.Invulnerable) tags += " [TRAVA]";
        if (brain != null && brain.Fury) tags += " [FÚRIA]";
        if (brain != null && brain.HasPendingLock) tags += $" (próx: {brain.NextLockFraction * 100f:0}%)";

        bossInfoLabel.Text = $"{boss.Data.EnemyName}: {boss.Hp:0}/{boss.MaxHp:0} ({pct:0}%){tags}";
        bossInfoLabel.AddThemeColorOverride("font_color", brain != null && brain.Invulnerable ? GameFeel.Gold : new Color(1f, 0.55f, 0.45f));
        forceLockButton.Disabled = brain == null || !brain.HasPendingLock || brain.Invulnerable;
        forceFuryButton.Disabled = brain == null || brain.Fury;
    }

    void SetStatus(string msg) => statusLabel.Text = msg;
}
