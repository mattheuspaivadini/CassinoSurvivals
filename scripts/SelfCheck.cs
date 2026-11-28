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
        ArenasDosAtosEBlockout2();
        NaipesEBosses();
        CamadaDeDificuldade();
    }

    static void CamadaDeDificuldade()
    {
        Check(Near(DifficultyRules.HpScale(1), 1.15f) && Near(DifficultyRules.DamageScale(1), 1.10f) && Near(DifficultyRules.SpeedScale(1), 1f), "dificuldade na F1");
        Check(Near(DifficultyRules.SpeedScale(15), 1.35f), "inimigos 35% mais rápidos na F15");
        for (int st = 2; st <= CampaignRules.TotalStages; st++)
            Check(DifficultyRules.SpeedScale(st) > DifficultyRules.SpeedScale(st - 1) && DifficultyRules.HpScale(st) > DifficultyRules.HpScale(st - 1), $"F{st} mais difícil que a anterior");

        var baseF1 = RoundRules.For(1);
        var f1 = DifficultyRules.Apply(baseF1);
        Check(f1.Count >= 100, $"F1 com ~100 monstros ({f1.Count})");
        var f14 = DifficultyRules.Apply(RoundRules.For(14));
        Check(f14.Count >= 280, $"F14 com ~300 monstros ({f14.Count})");
        Check(DifficultyRules.MaxAlive >= 100 && DifficultyRules.MaxAlive < f14.Count, "teto de vivos segura a horda grande sem cortar o total");
        for (int st = 2; st <= 14; st++)
            if (!CampaignRules.IsBossStage(st) && !CampaignRules.IsBossStage(st - 1))
                Check(DifficultyRules.Apply(RoundRules.For(st)).Count >= DifficultyRules.Apply(RoundRules.For(st - 1)).Count, $"F{st} não tem menos monstros que a anterior");
        Check(f1.HpMultiplier > baseF1.HpMultiplier && f1.DamageMultiplier > baseF1.DamageMultiplier, "HP e dano acima da curva oficial");
        var baseF10 = RoundRules.For(10);
        var f10 = DifficultyRules.Apply(baseF10);
        Check(f10.Boss && Near(f10.BossHp, baseF10.BossHp * DifficultyRules.BossHpScale) && f10.BossChips == baseF10.BossChips, "boss mais longo, recompensa igual");
        Check(Near(CampaignRules.ForStage(1).HpScale, 1.00f), "curva oficial do Lemuel continua intacta");
    }

    static bool Near(float a, float b) => Math.Abs(a - b) < 0.001f;

    static void NaipesEBosses()
    {
        // Armadura de Boss: dentro do teto entra 100%, excedente a 20%.
        Check(Near(EnemyRules.ApplyDpsCap(0f, 50f, 55f), 50f), "dano dentro do teto entra inteiro");
        Check(Near(EnemyRules.ApplyDpsCap(50f, 15f, 55f), 5f + 10f * 0.2f), "excedente do teto entra a 20%");
        Check(Near(EnemyRules.ApplyDpsCap(100f, 10f, 55f), 2f), "teto estourado no segundo: tudo a 20%");
        Check(Near(EnemyRules.ApplyDpsCap(0f, 10f, 0f), 10f), "sem teto: dano normal");

        // Trava de Fase em 50%: a barra não passa do marco.
        Check(Near(EnemyRules.ClampToLock(1700f, 3200f, 0.5f, 500f), 100f), "golpe não atravessa a trava de 50%");
        Check(Near(EnemyRules.ClampToLock(1600f, 3200f, 0.5f, 500f), 500f), "após a trava o dano volta ao normal");

        // Geometria dos avisos.
        Check(EnemyRules.InCircle(new Godot.Vector2(2f, 0f), Godot.Vector2.Zero, 2f) && !EnemyRules.InCircle(new Godot.Vector2(2.1f, 0f), Godot.Vector2.Zero, 2f), "ponto no círculo do aviso");
        Check(EnemyRules.InLine(new Godot.Vector2(5f, 0.9f), Godot.Vector2.Zero, new Godot.Vector2(10f, 0f), 1f), "ponto na faixa da investida");
        Check(!EnemyRules.InLine(new Godot.Vector2(12f, 0f), Godot.Vector2.Zero, new Godot.Vector2(10f, 0f), 1f), "fora da ponta da faixa");
        var fan = EnemyRules.FanDirections(Godot.Vector2.Right, 5, 50f);
        Check(fan.Length == 5 && Near(fan[2].Angle(), 0f) && Near(Godot.Mathf.RadToDeg(fan[4].Angle() - fan[0].Angle()), 50f), "leque de 5 cartas em 50°");

        // Copas: teto de 6 coraçõezinhos.
        Check(EnemyRules.SummonRoom(0, 3, 6) == 3 && EnemyRules.SummonRoom(5, 3, 6) == 1 && EnemyRules.SummonRoom(6, 3, 6) == 0, "teto de invocação");

        // Recursos dos naipes e bosses.
        var expect = new (EnemyKind kind, EnemyBehavior b)[]
        {
            (EnemyKind.CartaEspadas, EnemyBehavior.Espadas), (EnemyKind.CartaOuros, EnemyBehavior.Ouros),
            (EnemyKind.CartaPaus, EnemyBehavior.Chase), (EnemyKind.CartaCopas, EnemyBehavior.Copas),
        };
        foreach (var (kind, b) in expect)
        {
            var d = EnemyCatalog.Load(kind);
            Check(d != null && d.Behavior == b, $"{kind} carrega com o comportamento {b}");
            Check(d.GetOrBuildFrames().HasAnimation("chase"), $"{kind} tem animação");
        }
        var copas = EnemyCatalog.Load(EnemyKind.CartaCopas);
        Check(copas.SummonEnemy != null && copas.SummonEnemy.Dollars == 0, "Copas invoca coraçõezinhos de $0");
        Check(EnemyCatalog.Load(EnemyKind.CartaPaus).KnockbackResistant, "Paus resiste a empurrão");
        Check(EnemyCatalog.Load(EnemyKind.CartaEspadas).GetOrBuildFrames().HasAnimation("chase_back"), "Espadas tem sprite de costas");

        var rainha = EnemyCatalog.LoadBoss(BossKind.Rainha);
        var rei = EnemyCatalog.LoadBoss(BossKind.Rei);
        var tigre = EnemyCatalog.LoadBoss(CampaignRules.ForStage(15).Boss);
        Check(rainha.Behavior == EnemyBehavior.Rainha && rainha.IsBoss, "F5: A Rainha");
        Check(rei.Behavior == EnemyBehavior.Rei && rei.IsBoss, "F10: O Rei de Paus");
        Check(tigre.Behavior == EnemyBehavior.Tigrinho && tigre.EnemyName == "Tigrinho", "F15: Tigrinho é o chefe final");
        var rf = rainha.GetOrBuildFrames().GetFrameTexture("chase", 0) as Godot.AtlasTexture;
        Check(rf != null && Near(rf.Region.Size.Y, 51f) && Near(rf.Region.Size.X, 32f), "frames da Rainha 32x51");

        var summon = EnemyData.CreateSummon(EnemyData.CreateGoldenVariant(EnemyCatalog.Load(EnemyKind.Ficha)));
        Check(!summon.IsGolden && summon.Dollars == 0 && !summon.GuaranteedChip && summon.ChipDropChance == 0f, "invocado: nunca Dourado, $0 e sem drop");

        Check(EnemyBrain.Create(EnemyBehavior.Tigrinho) is TigrinhoBrain && EnemyBrain.Create(EnemyBehavior.Chase) == null, "cérebros por comportamento");
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
        Check(CampaignRules.TotalStages == 15 && CampaignRules.ActsCount == 3 && CampaignRules.StagesPerAct == 5, "campanha 15 fases (3 Atos x 5)");
        Check(RoundRules.RoundsPerStage == 15 && RoundRules.BossEvery == 5, "RoundRules alinhado com 15 fases");

        int[] expectedMobs = { 25, 30, 34, 38, 18, 40, 44, 47, 50, 24, 52, 55, 58, 60, 28 };
        int[] expectedGoldens = { 2, 2, 2, 2, 0, 2, 3, 2, 3, 0, 3, 3, 3, 3, 0 };
        int[] expectedChips = { 2, 2, 2, 2, 4, 2, 3, 2, 3, 5, 3, 3, 3, 3, 0 };

        int bosses = 0, totalMobs = 0, totalGoldens = 0, totalChips = 0, totalClearBonus = 0, totalRevenue = 0;
        var rng = new Godot.RandomNumberGenerator { Seed = 2026 };

        for (int r = 1; r <= RoundRules.RoundsPerStage; r++)
        {
            var s = RoundRules.For(r);
            var c = CampaignRules.ForStage(r);
            if (s.Boss) bosses++;

            Check(s.Boss == (r == 5 || r == 10 || r == 15), $"boss só nas fases 5/10/15 (fase {r})");
            Check(s.Count == expectedMobs[r - 1], $"volume de horda fase {r} ({s.Count})");
            Check(s.Elites == expectedGoldens[r - 1], $"dourados fase {r} ({s.Elites})");
            Check(c.ChipsReward == expectedChips[r - 1], $"fichas fase {r} ({c.ChipsReward})");
            Check(s.Duration > 0f && s.HpMultiplier >= 1f && s.DamageMultiplier >= 1f, $"fase {r} válida");
            Check(!s.Boss || s.BossHp > 0f, $"boss da fase {r} com HP");
            Check(c.RemovesSlotMachinesAfterClear == (r == 14), $"flag pós-F14 exclusiva da fase 14 (fase {r})");

            if (r > 1)
            {
                var prev = CampaignRules.ForStage(r - 1);
                Check(c.HpScale >= prev.HpScale && c.DamageScale >= prev.DamageScale, $"escala monotônica na fase {r}");
                if (c.IsBossStage)
                {
                    Check(Godot.Mathf.IsEqualApprox(c.HpScale, prev.HpScale), $"mini-horda {r} mantém HP da fase anterior");
                    Check(Godot.Mathf.IsEqualApprox(c.DamageScale, prev.DamageScale), $"mini-horda {r} mantém Dano da fase anterior");
                    Check(c.TotalMobs < prev.TotalMobs, $"fase {r} tem mini-horda menor que fase anterior");
                }
                else if (!prev.IsBossStage)
                {
                    Check(c.TotalMobs >= prev.TotalMobs, $"fases normais não diminuem ({r})");
                }
            }

            // Introdução em escada dos inimigos
            int expectedPoolSize = r <= 2 ? 3 : r == 3 ? 4 : r <= 5 ? 5 : r == 6 ? 6 : 7;
            Check(c.UnlockedEnemies.Count == expectedPoolSize, $"escada de inimigos fase {r}");
            var unlocked = new System.Collections.Generic.HashSet<EnemyKind>(c.UnlockedEnemies);
            for (int k = 0; k < 20; k++)
                Check(unlocked.Contains(CampaignRules.PickEnemyKind(c, rng)), $"sorteio respeita pool da fase {r}");

            totalMobs += c.TotalMobs;
            totalGoldens += c.GoldenCount;
            totalChips += c.ChipsReward;
            totalClearBonus += c.ClearBonusDollars;
            totalRevenue += c.ExpectedRevenueDollars;
        }

        Check(bosses == 3, "3 bosses na campanha");
        Check(totalMobs == 603, $"total de mobs = 603 (obtido {totalMobs})");
        Check(totalGoldens == 30, $"total de dourados = 30 (obtido {totalGoldens})");
        Check(totalChips == 39, $"total de fichas = 39 (obtido {totalChips})");
        Check(totalClearBonus == 690, $"bônus de sala/boss = $690 (obtido {totalClearBonus})");
        Check(totalRevenue == 1413, $"receita total = $1413 (obtido {totalRevenue})");

        Check(Godot.Mathf.IsEqualApprox(CampaignRules.ForStage(1).HpScale, 1.00f) && Godot.Mathf.IsEqualApprox(CampaignRules.ForStage(1).DamageScale, 1.00f), "escala F1 (1.00 / 1.00)");
        Check(Godot.Mathf.IsEqualApprox(CampaignRules.ForStage(14).HpScale, 2.40f) && Godot.Mathf.IsEqualApprox(CampaignRules.ForStage(14).DamageScale, 1.45f), "escala F14 (2.40 / 1.45)");

        var f5 = CampaignRules.ForStage(5);
        var f10 = CampaignRules.ForStage(10);
        var f15 = CampaignRules.ForStage(15);
        Check(f5.Boss == BossKind.Rainha && Godot.Mathf.IsEqualApprox(f5.BossHp, 3200f) && Godot.Mathf.IsEqualApprox(f5.BossDpsCap, 55f) && f5.BossChips == 4 && f5.BossDollars == 75, "Boss Ato 1: Rainha");
        Check(f10.Boss == BossKind.Rei && Godot.Mathf.IsEqualApprox(f10.BossHp, 6800f) && Godot.Mathf.IsEqualApprox(f10.BossDpsCap, 105f) && f10.BossChips == 5 && f10.BossDollars == 125, "Boss Ato 2: Rei");
        Check(f15.Boss == BossKind.MaquinaCacaNiquel && Godot.Mathf.IsEqualApprox(f15.BossHp, 14000f) && Godot.Mathf.IsEqualApprox(f15.BossDpsCap, 180f) && f15.BossChips == 0 && f15.BossDollars == 250, "Boss Ato 3: Máquina de Caça-Níquel");
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

    static void ArenasDosAtosEBlockout2()
    {
        // 1. Arena de Boss (Blockout 2)
        var bossPacked = Godot.GD.Load<Godot.PackedScene>(CampaignRules.BossScenePath);
        Check(bossPacked != null, "ArenaBoss.tscn carrega");
        var bossRoot = bossPacked.Instantiate<Arena>();
        try
        {
            Check(bossRoot != null && bossRoot.IsBossArena, "ArenaBoss marcada como IsBossArena");
            var player = bossRoot.GetNode<Godot.Node2D>("Player");
            var exit = bossRoot.GetNode<ExitDoor>("Exit");
            var bossSpawn = bossRoot.GetNode<Godot.Node2D>("BossSpawn");
            var horde = bossRoot.GetNode<HordeDirector>("Managers/HordeDirector");
            Check(!exit.StartsOpen && horde != null && horde.LowerDoors.Contains(1) && horde.LowerDoors.Contains(3), "ArenaBoss configura saída fechada e atraso nas portas inferiores (1, 3)");
            Check(horde.LowerDoorDelay >= 2f && horde.LowerDoorDelay <= 3f, "ArenaBoss atraso inicial 2s–3s");

            var doors = new System.Collections.Generic.List<SpawnDoor>();
            var doorPosUnits = new System.Collections.Generic.List<Godot.Vector2>();
            foreach (var child in bossRoot.GetNode("Doors").GetChildren())
            {
                if (child is SpawnDoor sd)
                {
                    doors.Add(sd);
                    doorPosUnits.Add(sd.Position / Units.Px);
                }
            }

            var pillarsUnits = new System.Collections.Generic.List<Godot.Vector2>();
            var solids = CollectObstacleSolids(bossRoot.GetNode("Obstacles"));
            for (int p = 1; p <= 4; p++)
            {
                var pillar = bossRoot.GetNodeOrNull<Godot.Node2D>($"Obstacles/P{p}");
                Check(pillar != null, $"ArenaBoss possui pilar P{p}");
                pillarsUnits.Add(pillar.Position / Units.Px);
            }
            Check(solids.Count == 6, $"ArenaBoss possui 6 obstáculos sólidos (4 pilares + 2 balcões, encontrados: {solids.Count})");

            bool bossOk = CampaignRules.ValidateBossArenaLayout(
                bossRoot.HalfExtents,
                player.Position / Units.Px,
                exit.Position / Units.Px,
                bossSpawn.Position / Units.Px,
                doorPosUnits,
                pillarsUnits,
                solids,
                out string bossErr);
            Check(bossOk, $"Blockout 2 válido: {bossErr}");

            ValidateArenaNavigation(bossRoot.HalfExtents, bossRoot.Inflate, player.Position, exit.Position, doors, solids, bossSpawn.Position, "ArenaBoss");
            CheckNoSlotMachines(bossRoot, "ArenaBoss");

            for (int act = 1; act <= CampaignRules.ActsCount; act++)
            {
                bossRoot.ApplyActTheme(act);
                var t = CampaignRules.GetActTheme(act);
                Check(bossRoot.GetNode<Godot.Sprite2D>("Floor/Carpet").Modulate == t.CarpetModulate, $"ArenaBoss aplica carpete do Ato {act}");
                Check(bossRoot.GetNode<Godot.Sprite2D>("Floor/CorridorCarpet").Modulate == t.CarpetModulate, $"ArenaBoss aplica corredor do Ato {act}");
                Check(bossRoot.GetNode<Godot.Sprite2D>("Floor/RugBossArena").Modulate == t.RugModulate, $"ArenaBoss aplica tapete central do Ato {act}");
                Check(bossRoot.GetNode<Godot.Sprite2D>("Walls/NorthTrim").Modulate == t.NeonTrimColor, $"ArenaBoss aplica neon da parede do Ato {act}");
                Check(bossRoot.GetNode<Godot.Sprite2D>("Doors/Porta1/PanelA/Neon").Modulate == t.NeonTrimColor, $"ArenaBoss aplica neon das portas do Ato {act}");
            }
        }
        finally
        {
            bossRoot?.Free();
        }

        // 2. Arenas normais dos 3 Atos: navegação + variação espacial e ambiental
        var layoutSignatures = new System.Collections.Generic.List<string>();
        var carpetColors = new System.Collections.Generic.List<Godot.Color>();
        var trimColors = new System.Collections.Generic.List<Godot.Color>();

        for (int act = 1; act <= CampaignRules.ActsCount; act++)
        {
            var theme = CampaignRules.GetActTheme(act);
            var packed = Godot.GD.Load<Godot.PackedScene>(theme.NormalScenePath);
            Check(packed != null, $"Cena do Ato {act} carrega ({theme.NormalScenePath})");
            var root = packed.Instantiate<Arena>();
            try
            {
                Check(root != null && root.ActNumber == act && !root.IsBossArena, $"Ato {act} configurado corretamente");
                var player = root.GetNode<Godot.Node2D>("Player");
                var exit = root.GetNode<ExitDoor>("Exit");
                var horde = root.GetNode<HordeDirector>("Managers/HordeDirector");
                Check(!exit.StartsOpen && horde != null && horde.LowerDoors.Contains(2) && horde.LowerDoors.Contains(5), $"Ato {act} atrasa portas inferiores (2, 5)");

                var doors = new System.Collections.Generic.List<SpawnDoor>();
                var doorPosUnits = new System.Collections.Generic.List<Godot.Vector2>();
                foreach (var child in root.GetNode("Doors").GetChildren())
                {
                    if (child is SpawnDoor sd)
                    {
                        doors.Add(sd);
                        doorPosUnits.Add(sd.Position / Units.Px);
                    }
                }

                var solids = CollectObstacleSolids(root.GetNode("Obstacles"));
                bool ok = CampaignRules.ValidateNormalArenaLayout(
                    root.HalfExtents,
                    player.Position / Units.Px,
                    exit.Position / Units.Px,
                    doorPosUnits,
                    solids,
                    out string err);
                Check(ok, $"Layout Ato {act} válido: {err}");

                ValidateArenaNavigation(root.HalfExtents, root.Inflate, player.Position, exit.Position, doors, solids, null, $"Ato{act}");
                CheckNoSlotMachines(root, $"Ato{act}");

                var sig = new System.Text.StringBuilder();
                foreach (var r in solids) sig.Append($"{r.Position.X:0},{r.Position.Y:0},{r.Size.X:0},{r.Size.Y:0};");
                layoutSignatures.Add(sig.ToString());
                carpetColors.Add(root.GetNode<Godot.Sprite2D>("Floor/Carpet").Modulate);
                trimColors.Add(root.GetNode<Godot.Sprite2D>("Walls/NorthTrim").Modulate);
            }
            finally
            {
                root?.Free();
            }
        }

        Check(layoutSignatures[0] != layoutSignatures[1] && layoutSignatures[1] != layoutSignatures[2] && layoutSignatures[0] != layoutSignatures[2],
            "Arenas dos 3 Atos têm posicionamento de obstáculos distinto");
        Check(carpetColors[0] != carpetColors[1] && carpetColors[1] != carpetColors[2] && carpetColors[0] != carpetColors[2],
            "Arenas dos 3 Atos têm carpetes distintos");
        Check(trimColors[0] != trimColors[1] && trimColors[1] != trimColors[2] && trimColors[0] != trimColors[2],
            "Arenas dos 3 Atos têm iluminação neon distinta");
    }

    static void CheckNoSlotMachines(Godot.Node node, string label)
    {
        Check(node.GetNodeOrNull("Canvas_SlotMachine") == null, $"{label}: sem Canvas_SlotMachine");
        Check(!HasSlotNodeRecursive(node), $"{label}: sem SlotMachine ou SlotMachineUI nas salas de combate");
    }

    static bool HasSlotNodeRecursive(Godot.Node node)
    {
        if (node is SlotMachine || node is SlotMachineUI) return true;
        if (node is Godot.Sprite2D s && s.RegionEnabled)
        {
            var p = s.RegionRect.Position;
            if ((Godot.Mathf.IsEqualApprox(p.X, 784f) || Godot.Mathf.IsEqualApprox(p.X, 816f)) && Godot.Mathf.IsEqualApprox(p.Y, 17f))
                return true;
        }
        foreach (var child in node.GetChildren())
            if (HasSlotNodeRecursive(child)) return true;
        return false;
    }

    static System.Collections.Generic.List<Godot.Rect2> CollectObstacleSolids(Godot.Node obstaclesRoot)
    {
        var solids = new System.Collections.Generic.List<Godot.Rect2>();
        foreach (var child in obstaclesRoot.GetChildren())
        {
            if (child is Godot.Node2D obs && obs.IsInGroup("solid"))
            {
                var cs = obs.GetNodeOrNull<Godot.CollisionShape2D>("Shape");
                if (cs?.Shape is Godot.RectangleShape2D rect)
                    solids.Add(new Godot.Rect2(obs.Position + cs.Position - rect.Size / 2f, rect.Size));
            }
        }
        return solids;
    }

    static void ValidateArenaNavigation(
        Godot.Vector2 halfExtentsUnits,
        float inflateUnits,
        Godot.Vector2 playerPos,
        Godot.Vector2 exitPos,
        System.Collections.Generic.IReadOnlyList<SpawnDoor> doors,
        System.Collections.Generic.IReadOnlyList<Godot.Rect2> solids,
        Godot.Vector2? extraPoint,
        string label)
    {
        var half = halfExtentsUnits * Units.Px;
        var bounds = new Godot.Rect2(-half, half * 2f);
        var grid = new ArenaGrid(bounds, Units.Px, solids, inflateUnits * Units.Px);
        var flow = new FlowField(grid);
        flow.Compute(grid.ToCell(playerPos));

        Check(flow.DistanceAt(grid.ToCell(new Godot.Vector2(exitPos.X, exitPos.Y + 60f))) >= 0, $"{label}: caminho livre até a saída");
        if (extraPoint.HasValue)
            Check(flow.DistanceAt(grid.ToCell(extraPoint.Value)) >= 0, $"{label}: caminho livre até spawn do Boss");

        for (int i = 0; i < doors.Count; i++)
        {
            var entryPos = doors[i].Position + doors[i].Inward.Normalized() * doors[i].EntryDistance * Units.Px;
            Check(flow.DistanceAt(grid.ToCell(entryPos)) >= 0, $"{label}: caminho livre até porta {i + 1}");
        }
    }

    static void Check(bool ok, string what)
    {
        if (!ok) throw new Exception("Self-check falhou: " + what);
    }
}
