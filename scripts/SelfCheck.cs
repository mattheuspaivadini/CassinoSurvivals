using System;

/// <summary>Regras puras do caça-níquel/armas. Rodar: godot --headless --path . -- --selfcheck</summary>
public static class SelfCheck
{
    public static void Run()
    {
        var a = new WeaponData();
        var b = new WeaponData();
        var reels = new[] { a, a, b };
        Check(SlotRules.CountMatches(reels, a) == 2 && SlotRules.CountMatches(reels, b) == 1, "CountMatches");
        Check(SlotRules.LevelsFor(1) == 1 && SlotRules.LevelsFor(2) == 5 && SlotRules.LevelsFor(3) == 9, "LevelsFor");

        var w = new WeaponInstance(a, 1); // pente padrão = 8
        for (int i = 0; i < 8; i++) Check(w.Tick(1f, true), "deveria disparar com balas no pente");
        Check(w.IsReloading && !w.Tick(0.1f, true), "deveria estar recarregando");
        w.Tick(10f, true);
        Check(!w.IsReloading && w.Ammo == a.Magazine(1), "recarga deveria encher o pente");
        w.AddLevels(9);
        Check(w.Level == 10 && w.Ammo == a.Magazine(10) && a.ReloadTime(10) < a.ReloadTime(1), "escala por nível");

        FlowFieldContornaParede();
        PlanoDaHorda();
        RoundsDaFase();
        BestiarioEVariantes();
        SalaDeDescansoEBlockout3();
    }

    static void BestiarioEVariantes()
    {
        var ficha = Godot.GD.Load<EnemyData>("res://resources/enemies/CrupieVeloz.tres");
        var garrafa = Godot.GD.Load<EnemyData>("res://resources/enemies/Garrafa.tres");
        var fichona = Godot.GD.Load<EnemyData>("res://resources/enemies/Capanga.tres");
        var elite = Godot.GD.Load<EnemyData>("res://resources/enemies/SegurancaElite.tres");
        var boss = Godot.GD.Load<EnemyData>("res://resources/enemies/Tigrinho.tres");

        Check(ficha != null && garrafa != null && fichona != null && elite != null && boss != null, "recursos .tres de inimigos carregam");

        var ff = ficha.GetOrBuildFrames();
        Check(ff.HasAnimation("chase") && ff.GetFrameCount("chase") == 8, "Ficha: 8 frames de chase");
        Check(ff.HasAnimation("death") && ff.GetFrameCount("death") == 5, "Ficha: 5 frames de death");
        Check(ficha.Flanker, "Ficha usa flanqueamento");

        var gf = garrafa.GetOrBuildFrames();
        Check(gf.HasAnimation("idle") && gf.GetFrameCount("idle") == 4, "Garrafa: 4 frames de idle");
        Check(gf.HasAnimation("chase") && gf.GetFrameCount("chase") == 6, "Garrafa: 6 frames de chase");
        Check(gf.HasAnimation("death") && gf.GetFrameCount("death") == 3, "Garrafa: 3 frames de death");
        Check(!garrafa.Flanker && garrafa.Explodes && Godot.Mathf.IsEqualApprox(garrafa.ExplosionFuse, 0.5f), "Garrafa vai direto e explode em 0.5s");
        Check(Godot.Mathf.IsEqualApprox(garrafa.PuddleDuration, 4f) && Godot.Mathf.IsEqualApprox(garrafa.GoldenPuddleDuration, 6f), "duração da poça normal/dourada");

        var bf = fichona.GetOrBuildFrames();
        Check(bf.HasAnimation("chase") && bf.GetFrameCount("chase") == 8, "Fichona: 8 frames de chase");
        Check(!fichona.Flanker && fichona.SplitEnemy != null && fichona.SplitMinCount == 3 && fichona.SplitMaxCount == 4, "Fichona vai direto e divide em 3-4 no Troco");

        Check(!boss.Flanker && boss.IsBoss && boss.GetOrBuildFrames().GetFrameCount("chase") == 8, "Boss animado e sem flank");

        var garrafaDourada = EnemyData.CreateGoldenVariant(garrafa);
        Check(garrafaDourada.IsGolden && garrafaDourada.GuaranteedChip && garrafaDourada.Dollars == 5, "Dourado garante 1 ficha e $5");
        Check(Godot.Mathf.IsEqualApprox(garrafaDourada.MaxHp, garrafa.MaxHp * 1.8f), "Dourado 1.8x HP");
        Check(Godot.Mathf.IsEqualApprox(garrafaDourada.ExplosionDamage, garrafa.ExplosionDamage * 1.3f), "Dourado 1.3x Dano");
        Check(Godot.Mathf.IsEqualApprox(garrafaDourada.Scale, garrafa.Scale * 1.12f), "Dourado +12% escala");
        Check(Godot.Mathf.IsEqualApprox(garrafaDourada.PuddleDuration, 6f), "Garrafa Dourada tem poça de 6s");
        Check(ReferenceEquals(garrafaDourada.GetOrBuildFrames(), gf), "Dourado compartilha SpriteFrames em cache");

        var troco = EnemyData.CreateTrocoMinion(fichona.SplitEnemy);
        Check(!troco.IsGolden && !troco.GuaranteedChip && troco.Dollars == 0 && troco.ChipDropChance == 0f, "Trava contra farm infinito no Troco");
    }

    static void RoundsDaFase()
    {
        int bosses = 0;
        for (int r = 1; r <= RoundRules.RoundsPerStage; r++)
        {
            var s = RoundRules.For(r);
            if (s.Boss) bosses++;
            Check(s.Boss == (r == 7 || r == 14 || r == 21), $"boss só nos rounds 7/14/21 (round {r})");
            Check(s.Count > 0 && s.Duration > 0f && s.HpMultiplier >= 1f, $"round {r} válido");
            Check(!s.Boss || s.BossHp > 0f, $"boss do round {r} com HP");
            if (r > 1 && !s.Boss && !RoundRules.IsBossRound(r - 1))
                Check(s.Count >= RoundRules.For(r - 1).Count, "rounds normais não diminuem");
        }
        Check(bosses == 3, "3 bosses por fase");
        Check(RoundRules.For(1).Elites == 1 && RoundRules.For(6).Elites == 1, "rounds 1-6: 1 Dourado");
        Check(RoundRules.For(8).Elites == 2 && RoundRules.For(13).Elites == 2, "rounds 8-13: 2 Dourados");
        Check(RoundRules.For(15).Elites == 3 && RoundRules.For(20).Elites == 3, "rounds 15-20: 3 Dourados");
        Check(RoundRules.For(7).Elites == 0 && RoundRules.For(7).BossChips == 3, "boss 7 solta 3 fichas");
        Check(RoundRules.For(14).BossChips == 4 && RoundRules.For(21).BossChips == 5, "bosses 14/21 soltam 4/5 fichas");
        Check(RoundRules.For(7).BossDollars == 75 && RoundRules.For(21).BossDollars == 250, "$ dos bosses");
        Check(RoundRules.For(7).Count < RoundRules.For(6).Count, "round de boss é mini-horda");
    }

    /// <summary>Grade 10x5 com parede na coluna 5 (y 0..3); o único vão é a linha y=4.</summary>
    static void FlowFieldContornaParede()
    {
        var wall = new[] { new Godot.Rect2(5f, 0f, 1f, 4f) };
        var grid = new ArenaGrid(new Godot.Rect2(0f, 0f, 10f, 5f), 1f, wall, 0f);
        Check(grid.IsBlocked(new Godot.Vector2I(5, 2)) && !grid.IsBlocked(new Godot.Vector2I(5, 4)), "grade marca a parede");

        var flow = new FlowField(grid);
        var target = new Godot.Vector2I(9, 0);
        flow.Compute(target);
        var c = new Godot.Vector2I(0, 0);
        int steps = 0;
        while (c != target && steps < 100 && flow.NextCell(c, out var next))
        {
            Check(!grid.IsBlocked(next), "flow field atravessou a parede");
            c = next;
            steps++;
        }
        Check(c == target, "flow field deveria chegar ao alvo");
        Check(!grid.LineClear(new Godot.Vector2(0.5f, 0.5f), new Godot.Vector2(9.5f, 0.5f)), "linha pela parede não é livre");
        Check(grid.LineClear(new Godot.Vector2(0.5f, 4.5f), new Godot.Vector2(9.5f, 4.5f)), "linha pelo vão é livre");
    }

    static void PlanoDaHorda()
    {
        var rng = new Godot.RandomNumberGenerator { Seed = 7 };
        var plan = HordePlan.Build(25, 60f, 6, 2, new[] { 2, 5 }, 2.5f, rng);
        Check(plan.Count == 25, "horda deveria ter 25 entradas");
        int elites = 0;
        for (int i = 0; i < plan.Count; i++)
        {
            var e = plan[i];
            if (e.Elite)
            {
                elites++;
                Check(i < 25 / 4 || i >= 25 * 3 / 4, "dourados no início e no fim");
            }
            Check(e.Time >= 0f && e.Time <= 60f, "tempo dentro da horda");
            Check(i == 0 || e.Time >= plan[i - 1].Time, "tempos crescentes");
            Check(e.Door >= 0 && e.Door < 6, "porta válida");
            Check(e.Time >= 2.5f || (e.Door != 2 && e.Door != 5), "portas de baixo respeitam o atraso");
        }
        Check(elites == 2, "horda deveria ter 2 dourados");
    }

    static void SalaDeDescansoEBlockout3()
    {
        // 1. Custos e limites oficiais das 4 estações
        Check(LoungeRules.GetCost(LoungeStationType.Bar) == 1, "Bar custa 1 ficha");
        Check(LoungeRules.GetCost(LoungeStationType.UpgradeSlot) == 1, "Melhorias custa 1 ficha");
        Check(LoungeRules.GetCost(LoungeStationType.WeaponSlot) == 2, "Armas custa 2 fichas");
        Check(LoungeRules.GetCost(LoungeStationType.Roulette, 4) == 4 && LoungeRules.GetCost(LoungeStationType.Roulette, 5) == 5, "Roleta custa 4-5 fichas");
        Check(LoungeRules.GetCost(LoungeStationType.Roulette, 2) == 4 && LoungeRules.GetCost(LoungeStationType.Roulette, 9) == 5, "Roleta faz clamp entre 4 e 5");

        Check(LoungeRules.GetMaxUsesPerRoom(LoungeStationType.Bar) == 0 && LoungeRules.GetMaxUsesPerRun(LoungeStationType.Bar) == 0, "Bar tem uso livre");
        Check(LoungeRules.GetMaxUsesPerRoom(LoungeStationType.UpgradeSlot) == 0 && LoungeRules.GetMaxUsesPerRun(LoungeStationType.UpgradeSlot) == 0, "Melhorias tem uso livre");
        Check(LoungeRules.GetMaxUsesPerRoom(LoungeStationType.WeaponSlot) == 0 && LoungeRules.GetMaxUsesPerRun(LoungeStationType.WeaponSlot) == 0, "Armas tem uso livre");
        Check(LoungeRules.GetMaxUsesPerRoom(LoungeStationType.Roulette) == 1, "Roleta máx. 1 por sala");
        Check(LoungeRules.GetMaxUsesPerRun(LoungeStationType.Roulette, 3) == 3 && LoungeRules.GetMaxUsesPerRun(LoungeStationType.Roulette, 4) == 4, "Roleta máx. 3-4 por run");

        Check(LoungeRules.CanUse(LoungeStationType.Bar, 1, 5, 10), "Bar liberado com 1 ficha e usos repetidos");
        Check(!LoungeRules.CanUse(LoungeStationType.WeaponSlot, 1, 0, 0), "Armas bloqueado com apenas 1 ficha");
        Check(LoungeRules.CanUse(LoungeStationType.WeaponSlot, 2, 0, 0), "Armas liberado com 2 fichas");
        Check(!LoungeRules.CanUse(LoungeStationType.Roulette, 3, 0, 0), "Roleta bloqueada com 3 fichas");
        Check(LoungeRules.CanUse(LoungeStationType.Roulette, 4, 0, 0), "Roleta liberada com 4 fichas");
        Check(!LoungeRules.CanUse(LoungeStationType.Roulette, 5, 1, 1), "Roleta respeita limite de 1/sala");
        Check(!LoungeRules.CanUse(LoungeStationType.Roulette, 5, 0, 3, 1, 4, 3), "Roleta respeita limite de 3/run");
        Check(!LoungeRules.CanUse(LoungeStationType.Roulette, 5, 0, 4, 1, 4, 4), "Roleta respeita limite de 4/run");

        // Pós-Fase 14: máquinas de Armas e Melhorias somem, Bar e Roleta continuam
        Check(!LoungeRules.IsAvailableInStage(LoungeStationType.WeaponSlot, 14) && !LoungeRules.IsAvailableInStage(LoungeStationType.UpgradeSlot, 14), "Caça-níqueis somem após a Fase 14");
        Check(LoungeRules.IsAvailableInStage(LoungeStationType.Bar, 14) && LoungeRules.IsAvailableInStage(LoungeStationType.Roulette, 14), "Bar e Roleta permanecem após a Fase 14");

        // 2. Cena fixa da Sala de Descanso (Blockout 3) e leitura/fluxo espacial
        var packed = Godot.GD.Load<Godot.PackedScene>("res://scenes/levels/SalaDescanso.tscn");
        Check(packed != null, "SalaDescanso.tscn carrega");
        var root = packed.Instantiate<Arena>();
        try
        {
            Check(root != null && root.FixedCamera, "SalaDescanso usa câmera fixa sem scroll");

            var stations = root.GetNode("Stations");
            var weaponStation = stations.GetNode<LoungeStation>("WeaponSlot");
            var upgradeStation = stations.GetNode<LoungeStation>("UpgradeSlot");
            var rouletteStation = stations.GetNode<LoungeStation>("Roulette");
            var barStation = stations.GetNode<LoungeStation>("Bar");

            Check(weaponStation.StationType == LoungeStationType.WeaponSlot && LoungeRules.GetCost(weaponStation.StationType, weaponStation.Cost) == 2, "Estação Armas configurada (2 fichas)");
            Check(upgradeStation.StationType == LoungeStationType.UpgradeSlot && LoungeRules.GetCost(upgradeStation.StationType, upgradeStation.Cost) == 1, "Estação Melhorias configurada (1 ficha)");
            Check(rouletteStation.StationType == LoungeStationType.Roulette && rouletteStation.Cost >= 4 && rouletteStation.Cost <= 5 && rouletteStation.MaxUsesPerRoom == 1 && rouletteStation.MaxUsesPerRun >= 3 && rouletteStation.MaxUsesPerRun <= 4, "Estação Roleta configurada (4-5 fichas, 1/sala, 3-4/run)");
            Check(barStation.StationType == LoungeStationType.Bar && LoungeRules.GetCost(barStation.StationType, barStation.Cost) == 1, "Estação Bar configurada (1 ficha)");

            var player = root.GetNode<Godot.Node2D>("Player");
            var entrance = root.GetNode<Godot.Node2D>("Entrance");
            var exit = root.GetNode<ExitDoor>("Exit");
            Check(player != null && entrance != null && exit != null && exit.StartsOpen, "Entrada, Jogador [J] e Porta Superior presentes");

            var solids = new System.Collections.Generic.List<Godot.Rect2>();
            foreach (var child in root.GetNode("Obstacles").GetChildren())
            {
                if (child is Godot.Node2D obs && obs.IsInGroup("solid"))
                {
                    var cs = obs.GetNodeOrNull<Godot.CollisionShape2D>("Shape");
                    if (cs?.Shape is Godot.RectangleShape2D rect)
                        solids.Add(new Godot.Rect2(obs.Position + cs.Position - rect.Size / 2f, rect.Size));
                }
            }
            Check(solids.Count == 6, "SalaDescanso possui 6 obstáculos sólidos (2 sofás + 4 estações)");

            bool layoutOk = LoungeRules.ValidateBlockoutLayout(
                root.HalfExtents,
                player.Position / Units.Px,
                exit.Position / Units.Px,
                weaponStation.Position / Units.Px,
                upgradeStation.Position / Units.Px,
                rouletteStation.Position / Units.Px,
                barStation.Position / Units.Px,
                solids,
                out string layoutError);
            Check(layoutOk, $"Blockout 3 válido: {layoutError}");

            // Valida fluxo de circulação real via ArenaGrid + FlowField a partir de [J]
            var half = root.HalfExtents * Units.Px;
            var bounds = new Godot.Rect2(-half, half * 2f);
            var grid = new ArenaGrid(bounds, Units.Px, solids, root.Inflate * Units.Px);
            var flow = new FlowField(grid);
            flow.Compute(grid.ToCell(player.Position));

            Check(flow.DistanceAt(grid.ToCell(new Godot.Vector2(weaponStation.Position.X, weaponStation.Position.Y + 60f))) >= 0, "Caminho livre de [J] até Armas");
            Check(flow.DistanceAt(grid.ToCell(new Godot.Vector2(upgradeStation.Position.X, upgradeStation.Position.Y + 60f))) >= 0, "Caminho livre de [J] até Melhorias");
            Check(flow.DistanceAt(grid.ToCell(new Godot.Vector2(rouletteStation.Position.X + 75f, rouletteStation.Position.Y))) >= 0, "Caminho livre de [J] até Roleta");
            Check(flow.DistanceAt(grid.ToCell(new Godot.Vector2(barStation.Position.X - 95f, barStation.Position.Y))) >= 0, "Caminho livre de [J] até Bar");
            Check(flow.DistanceAt(grid.ToCell(new Godot.Vector2(exit.Position.X, exit.Position.Y + 60f))) >= 0, "Caminho livre de [J] até Próxima Horda");
        }
        finally
        {
            root?.Free();
        }
    }

    static void Check(bool ok, string what)
    {
        if (!ok) throw new Exception("Self-check falhou: " + what);
    }
}
