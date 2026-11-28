using System.Collections.Generic;
using Godot;

/// <summary>
/// Painel e atalhos de debug do Modo Sandbox (issue #23).
/// Instanciado exclusivamente quando a flag CLI --enemy-sandbox está ativa; 100% ausente na campanha normal.
/// Atalhos: [F1] mostra/oculta painel, [G] Dourado, [I] God Mode, [H] Cura, [F6] Trava, [F7] Fúria, [F8/Backspace] Limpar.
/// </summary>
public partial class EnemySandboxUI : CanvasLayer
{
    PanelContainer panel;
    Button collapsedPill;
    Button goldenButton, godModeButton, forceLockButton, forceFuryButton;
    Label statusLabel, stageLabel, bossInfoLabel;

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
        EnableWeaponSandbox();
        BuildUi();
        RefreshStateLabels();
    }

    void EnableWeaponSandbox()
    {
        var ws = PlayerController.Instance?.GetNodeOrNull<WeaponSystem>("WeaponSystem");
        if (ws != null) ws.SandboxMode = true;
    }

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
            OffsetLeft = -180f, OffsetRight = -16f, OffsetTop = 148f, OffsetBottom = 186f,
            FocusMode = Control.FocusModeEnum.None,
            Visible = false,
        };
        collapsedPill.AddThemeFontSizeOverride("font_size", 16);
        collapsedPill.Pressed += TogglePanel;
        AddChild(collapsedPill);

        panel = new PanelContainer
        {
            AnchorLeft = 1f, AnchorRight = 1f, AnchorTop = 0f, AnchorBottom = 1f,
            OffsetLeft = -340f, OffsetRight = -16f, OffsetTop = 144f, OffsetBottom = -140f,
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

        statusLabel = MakeLabel("Pronto · [1-8/Q/E] armas", 13, new Color(0.78f, 0.84f, 0.92f));
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

        var weaponRow = new HBoxContainer();
        weaponRow.AddThemeConstantOverride("separation", 6);
        col.AddChild(weaponRow);
        weaponRow.AddChild(MakeLabel("Arma:", 13, new Color(0.85f, 0.85f, 0.9f)));
        weaponRow.AddChild(MakeButton("Nv 1", () => SetSelectedWeaponLevel(1)));
        weaponRow.AddChild(MakeButton("Nv 5", () => SetSelectedWeaponLevel(5)));
        weaponRow.AddChild(MakeButton("Nv 10 ★", () => SetSelectedWeaponLevel(10)));

        // 7 arquétipos de inimigos
        col.AddChild(MakeSectionHeader("INIMIGOS (7 ARQUÉTIPOS)"));
        var enemyGrid = new GridContainer { Columns = 2 };
        enemyGrid.AddThemeConstantOverride("h_separation", 6);
        enemyGrid.AddThemeConstantOverride("v_separation", 6);
        col.AddChild(enemyGrid);
        foreach (var opt in EnemySandboxRules.Enemies)
        {
            var captured = opt;
            enemyGrid.AddChild(MakeButton(captured.Label, () => SpawnEnemy(captured)));
        }

        // 3 Bosses
        col.AddChild(MakeSectionHeader("BOSSES (OFICIAIS)"));
        foreach (var bossOpt in EnemySandboxRules.Bosses)
        {
            var captured = bossOpt;
            col.AddChild(MakeButton($"Invocar {captured.Label}", () => SpawnBoss(captured)));
        }

        // Transições de Trava de Fase / Fúria
        col.AddChild(MakeSectionHeader("TRANSIÇÕES DO BOSS"));
        bossInfoLabel = MakeLabel("Nenhum boss ativo", 13, new Color(0.85f, 0.8f, 0.7f));
        col.AddChild(bossInfoLabel);

        var bossCtrlRow = new HBoxContainer();
        bossCtrlRow.AddThemeConstantOverride("separation", 6);
        col.AddChild(bossCtrlRow);
        forceLockButton = MakeButton("TRAVA [F6]", ForceBossLock);
        forceFuryButton = MakeButton("FÚRIA [F7]", ForceBossFury);
        bossCtrlRow.AddChild(forceLockButton);
        bossCtrlRow.AddChild(forceFuryButton);

        // Limpar arena
        var clearBtn = MakeButton("LIMPAR ARENA [F8 / Backspace]", ClearArena);
        clearBtn.AddThemeColorOverride("font_color", new Color(1f, 0.45f, 0.45f));
        col.AddChild(clearBtn);
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

    void SetSelectedWeaponLevel(int level)
    {
        var ws = PlayerController.Instance?.GetNodeOrNull<WeaponSystem>("WeaponSystem");
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
