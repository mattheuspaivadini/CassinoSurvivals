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
        Check(SlotRules.LevelsFor(1) == 1 && SlotRules.LevelsFor(2) == 2 && SlotRules.LevelsFor(3) == 3, "LevelsFor 1x/2x/3x = +1/+2/+3");
        Check(!SlotRules.IsJackpot(reels) && !SlotRules.IsJackpot(new[] { b, a, a }) && SlotRules.IsJackpot(new[] { a, a, a }), "IsJackpot: só 3 iguais");

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
        LojaExternaCompleta();
        TabelasDeSorte();
        BebidasEEstadoDaRun();
        MelhoriasEPassivas();
        CamadaDeDificuldade();
        ArsenalE2Slots();
    }

    static void ArsenalE2Slots()
    {
        // 1. Progressão Nv 1 -> Nv 10 + Marcos Nv 5 (+15% cadência) e Nv 10 (Arma Dourada +25% dano) + Fórmula Aditiva.
        var pistola = Godot.GD.Load<WeaponData>("res://resources/weapons/PistolaNeon.tres");
        var revolver = Godot.GD.Load<WeaponData>("res://resources/weapons/RevolverHighRoller.tres");
        var escopeta = Godot.GD.Load<WeaponData>("res://resources/weapons/EscopetaRoyalFlush.tres");
        var m4 = Godot.GD.Load<WeaponData>("res://resources/weapons/M4Tatica.tres");
        var ak47 = Godot.GD.Load<WeaponData>("res://resources/weapons/AK47Cassino.tres");
        var mg = Godot.GD.Load<WeaponData>("res://resources/weapons/SMGViceCity.tres");
        var rifle = Godot.GD.Load<WeaponData>("res://resources/weapons/LancaDados.tres");
        var sniper = Godot.GD.Load<WeaponData>("res://resources/weapons/SniperVip.tres");

        var all = new[] { pistola, revolver, escopeta, m4, ak47, mg, rifle, sniper };
        Check(WeaponSystem.OfficialWeaponPaths.Length == 8, "catálogo oficial tem 8 armas");
        for (int i = 0; i < all.Length; i++)
        {
            Check(all[i] != null, $"arma oficial #{i + 1} carrega");
            Check(all[i].WeaponSprite != null, $"{all[i].WeaponName} tem sprite");
            Check(!string.IsNullOrEmpty(SlotMachineUI.WeaponFramePath(all[i])), $"{all[i].WeaponName} tem moldura");
        }
        Check(SlotMachineUI.WeaponFramePath(ak47).EndsWith("ak47.png"), "moldura AK-47 mapeada");
        Check(SlotMachineUI.WeaponFramePath(m4).EndsWith("m4.png"), "moldura M4 mapeada");
        Check(SlotMachineUI.WeaponFramePath(sniper).EndsWith("sniper.png"), "moldura Sniper mapeada");

        // Conferência da tabela oficial de DPS Real (Balanceamento Numérico, pág. 2).
        Check(Near(pistola.LevelDamageBonus(1), 0f) && Near(pistola.LevelDamageBonus(5), 0.60f) && Near(pistola.LevelDamageBonus(9), 1.20f) && Near(pistola.LevelDamageBonus(10), 1.60f),
            "progressão de dano +15%/nv + 25% Arma Dourada (260% no Nv 10)");
        Check(Near(pistola.FireRateBonus(4), 0f) && Near(pistola.FireRateBonus(5), 0.15f), "marco Nv 5: +15% cadência");
        Check(pistola.Magazine(1) == 12 && pistola.Magazine(5) == 17 && pistola.Magazine(10) == 23, "pente Pistola usa ceil(+10%/nv)");
        Check(Near(pistola.ReloadTime(1), 1.0f) && Near(pistola.ReloadTime(5), 0.80f) && Near(pistola.ReloadTime(10), 0.55f), "recarga -5%/nv (teto -45%)");
        Check(Math.Abs(RealDps(pistola, 1) - 28.80f) < 0.05f && Math.Abs(RealDps(pistola, 5) - 56.99f) < 0.05f && Math.Abs(RealDps(pistola, 10) - 99.44f) < 0.05f,
            $"DPS real da Pistola bate com a planilha ({RealDps(pistola, 1):0.00}/{RealDps(pistola, 5):0.00}/{RealDps(pistola, 10):0.00})");
        Check(Math.Abs(RealDps(sniper, 1) - 39.74f) < 0.05f && Math.Abs(RealDps(sniper, 5) - 84.59f) < 0.05f && Math.Abs(RealDps(sniper, 10) - 156.30f) < 0.05f,
            $"DPS real da Sniper bate com a planilha ({RealDps(sniper, 1):0.00}/{RealDps(sniper, 5):0.00}/{RealDps(sniper, 10):0.00})");

        // Perfuração nativa exclusiva da Sniper (2 = atinge até 3 inimigos em linha).
        Check(sniper.PierceCount == 2, "Sniper tem perfuração 2 (atinge 3)");
        for (int i = 0; i < 7; i++) Check(all[i].PierceCount == 0, $"{all[i].WeaponName} tem perfuração base 0");

        // Fórmula aditiva: Base × (1 + Nível + Arsenal + Melhorias + Bebida) × Crítico.
        float additive = pistola.ComputeShotDamage(5, 0.25f, 0.70f, 1f);
        Check(Near(additive, 12f * (1f + 0.60f + 0.25f + 0.70f)), $"fórmula aditiva de dano ({additive:0.00})");
        Check(Near(pistola.ComputeShotDamage(10, 0.25f, 0.40f, 2f), 12f * (1f + 1.60f + 0.25f + 0.40f) * 2f), "fórmula aditiva com Arma Dourada + Crítico 2x");

        // 2. Sistema de 2 Slots (preenchimento automático Slot 2, soma em repetida e substituição).
        var ws = new WeaponSystem();
        try
        {
            Check(ws.AddOrUpgrade(pistola, 1) && ws.Weapons.Count == 1, "Slot 1 preenchido com Pistola");
            Check(!ws.NeedsSlotReplacement(revolver) && ws.AddOrUpgrade(revolver, 2) && ws.Weapons.Count == 2 && ws.Weapons[1].Level == 2,
                "Slot 2 preenchido automaticamente");
            Check(!ws.NeedsSlotReplacement(pistola) && ws.AddOrUpgrade(pistola, 3) && ws.Weapons[0].Level == 4 && ws.Weapons.Count == 2,
                "arma repetida soma níveis direto");
            Check(ws.NeedsSlotReplacement(sniper) && !ws.AddOrUpgrade(sniper, 1) && ws.Weapons.Count == 2,
                "3ª arma nova bloqueia adição direta e exige substituição");
            Check(ws.ReplaceSlot(1, sniper, 3) && ws.Weapons.Count == 2 && ws.Weapons[1].Data == sniper && ws.Weapons[1].Level == 3 && ws.Selected == 1,
                "substituição do Slot 2 aplica nova arma no nível sorteado");
            ws.AddOrUpgrade(sniper, 10);
            Check(ws.Weapons[1].Level == 10 && ws.Weapons[1].IsGolden, "Arma Dourada marcada no Nv 10");
        }
        finally
        {
            ws.Free();
        }

        // 3. SalaDescanso.tscn expõe as 8 armas no Caça-níquel de Armas e Player.tscn configura i-frames + camadas de ghosting.
        var loungePacked = Godot.GD.Load<Godot.PackedScene>("res://scenes/levels/SalaDescanso.tscn");
        var lounge = loungePacked.Instantiate();
        try
        {
            var slotUi = lounge.GetNode<SlotMachineUI>("Canvas_SlotMachine");
            Check(slotUi != null && slotUi.WeaponPool.Count == 8, $"SalaDescanso WeaponPool tem 8 armas (encontradas: {slotUi?.WeaponPool.Count})");
        }
        finally
        {
            lounge?.Free();
        }

        var playerPacked = Godot.GD.Load<Godot.PackedScene>("res://scenes/Player.tscn");
        var player = playerPacked.Instantiate<PlayerController>();
        try
        {
            Check(Near(player.InvulnerabilityDuration, 0.5f), "i-frames pós-dano = 0,5s");
            Check(player.CollisionLayer == 2 && player.CollisionMask == 5, "camadas do jogador compatíveis com ghosting no dash");
        }
        finally
        {
            player?.Free();
        }
    }

    static float RealDps(WeaponData d, int level)
    {
        int mag = d.Magazine(level);
        float shotDamage = d.Damage(level) * d.ProjectilesPerShot;
        float magTime = mag * d.FireIntervalAt(level);
        float cycleTime = magTime + d.ReloadTime(level);
        return (mag * shotDamage) / cycleTime;
    }

    static void CamadaDeDificuldade()
    {
        Check(Near(DifficultyRules.HpScale(1), 1.15f) && Near(DifficultyRules.DamageScale(1), 1.10f) && Near(DifficultyRules.SpeedScale(1), 1f), "dificuldade na F1");
        Check(Near(DifficultyRules.SpeedScale(15), 1.35f), "inimigos 35% mais rápidos na F15");
        for (int st = 2; st <= CampaignRules.TotalStages; st++)
            Check(DifficultyRules.SpeedScale(st) > DifficultyRules.SpeedScale(st - 1) && DifficultyRules.HpScale(st) > DifficultyRules.HpScale(st - 1), $"F{st} mais difícil que a anterior");

        var baseF1 = RoundRules.For(1);
        var f1 = DifficultyRules.Apply(baseF1);
        var f15 = DifficultyRules.Apply(RoundRules.For(15));
        Check(f1.Count == DifficultyRules.MobsStart && f15.Count == DifficultyRules.MobsEnd, $"curva de monstros {f1.Count} -> {f15.Count}");
        for (int st = 2; st <= CampaignRules.TotalStages; st++)
            Check(DifficultyRules.Apply(RoundRules.For(st)).Count > DifficultyRules.Apply(RoundRules.For(st - 1)).Count, $"F{st} tem mais monstros que a anterior (inclui bosses)");
        Check(f15.Boss && DifficultyRules.BossEntryKills(f15.Count) < f15.Count, "Tigrinho entra no meio da maior horda");
        Check(DifficultyRules.MaxAlive >= 100 && DifficultyRules.MaxAlive < f15.Count, "teto de vivos segura a horda grande sem cortar o total");
        for (int st = 2; st <= 14; st++)
            if (!CampaignRules.IsBossStage(st) && !CampaignRules.IsBossStage(st - 1))
                Check(DifficultyRules.Apply(RoundRules.For(st)).Count >= DifficultyRules.Apply(RoundRules.For(st - 1)).Count, $"F{st} não tem menos monstros que a anterior");
        Check(f1.HpMultiplier > baseF1.HpMultiplier && f1.DamageMultiplier > baseF1.DamageMultiplier, "HP e dano acima da curva oficial");
        var baseF10 = RoundRules.For(10);
        var f10 = DifficultyRules.Apply(baseF10);
        Check(f10.Boss && Near(f10.BossHp, baseF10.BossHp * DifficultyRules.BossHpScale) && f10.BossChips == baseF10.BossChips, "boss mais longo, recompensa igual");
        Check(Near(CampaignRules.ForStage(1).HpScale, 1.00f), "curva oficial do Lemuel continua intacta");

        int totalDollars = 0;
        for (int st = 1; st <= CampaignRules.TotalStages; st++)
        {
            var cs = CampaignRules.ForStage(st);
            var ds = DifficultyRules.Apply(RoundRules.For(st));
            totalDollars += DifficultyRules.CommonDollarBudget(cs) + cs.GoldenCount * 5 + cs.ClearBonusDollars;
            float dropScale = DifficultyRules.FloorDropScale(cs, ds.Count);
            Check(dropScale > 0f && dropScale <= 1f, $"FloorDropScale F{st} válido ({dropScale:0.00})");
        }
        Check(totalDollars == 1413, $"economia da Loja Externa preservada com hordas maiores ($1413, obtido {totalDollars})");
    }

    static bool Near(float a, float b) => Math.Abs(a - b) < 0.0005f;

    static void LojaExternaCompleta()
    {
        int[] barman = { 100, 200, 350, 600, 950 };
        int[] sorte = { 150, 300, 550, 900, 1450 };
        for (int i = 0; i < 5; i++)
        {
            Check(MetaProgress.CostOf(MetaProgress.Upgrade.Barman, i) == barman[i], $"custo Barman Nv {i + 1}");
            Check(MetaProgress.CostOf(MetaProgress.Upgrade.Sorte, i) == sorte[i], $"custo Sorte Nv {i + 1}");
        }
        Check(MetaProgress.Names.Length == 5 && MetaProgress.Descriptions.Length == 5, "Loja Externa com 5 upgrades");
    }

    static void TabelasDeSorte()
    {
        for (int luck = 0; luck <= LuckTables.MaxLevel; luck++)
        {
            int a = 0, b = 0;
            for (int t = 1; t <= 3; t++) a += LuckTables.WeaponTierPercent(luck, t);
            for (int f = 0; f < 3; f++) b += LuckTables.UpgradeBandPercent(luck, f);
            Check(a == 100, $"Tabela A Nv {luck} soma 100%");
            Check(b == 100, $"Tabela B Nv {luck} soma 100%");
        }
        Check(LuckTables.WeaponTierPercent(0, 1) == 70 && LuckTables.WeaponTierPercent(5, 3) == 20, "Tabela A: Nv 0 = 70% 1x, Nv 5 = 20% 3x");
        Check(LuckTables.UpgradeBandPercent(0, 0) == 60 && LuckTables.UpgradeBandPercent(5, 2) == 30, "Tabela B: Nv 0 = 60% Baixa, Nv 5 = 30% Alta");

        const int N = 20000;
        var rng = new Random(1234);
        foreach (int luck in new[] { 0, 5 })
        {
            var tiers = new int[4];
            var bands = new int[3];
            for (int i = 0; i < N; i++)
            {
                tiers[LuckTables.RollWeaponTier(luck, rng)]++;
                int pct = LuckTables.RollUpgradePercent(luck, rng);
                Check(pct >= 5 && pct <= 30 && pct % 5 == 0, "Rolo 3 só sorteia 5% a 30% em passos de 5");
                bands[(pct - 5) / 10]++;
            }
            for (int t = 1; t <= 3; t++)
                Check(Math.Abs(tiers[t] * 100.0 / N - LuckTables.WeaponTierPercent(luck, t)) < 2.0, $"distribuição Tabela A Nv {luck} tier {t}x");
            for (int f = 0; f < 3; f++)
                Check(Math.Abs(bands[f] * 100.0 / N - LuckTables.UpgradeBandPercent(luck, f)) < 2.0, $"distribuição Tabela B Nv {luck} faixa {f}");
        }

        var pool = new System.Collections.Generic.List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
        for (int i = 0; i < 300; i++)
        {
            for (int tier = 1; tier <= 3; tier++)
            {
                var reels = WeaponSlotRoll.BuildReels(pool, tier, rng);
                var distinct = new System.Collections.Generic.HashSet<int>(reels);
                Check(distinct.Count == 4 - tier, $"rolos do tier {tier}x têm {4 - tier} arma(s) distinta(s)");
            }
        }

        var wd = new WeaponData();
        var wi = new WeaponInstance(wd, 1);
        wi.AddLevels(50);
        Check(wi.Level == WeaponInstance.MaxLevel && WeaponInstance.MaxLevel == 10, "arma para no Nv 10");
    }

    static void BebidasEEstadoDaRun()
    {
        float openBar = DrinkRules.Efficiency(0, true), max = DrinkRules.Efficiency(5, true);
        Check(Near(DrinkRules.Efficiency(0, false), 1f), "eficiência base 1.0");
        Check(Near(DrinkRules.HealBase * openBar, 0.525f) && Near(DrinkRules.DamageBase * openBar, 0.60f), "Open Bar: 52,5% HP / +60% dano");
        Check(Near(DrinkRules.MoveBase * openBar, 0.375f) && Near(DrinkRules.FireRateBase * openBar, 0.45f), "Open Bar: +37,5% vel / +45% cadência");
        Check(Near(DrinkRules.HealBase * max, 0.665f) && Near(DrinkRules.DamageBase * max, 0.76f), "Open Bar + Barman 5: 66,5% HP / +76% dano");
        Check(Near(DrinkRules.MoveBase * max, 0.475f) && Near(DrinkRules.FireRateBase * max, 0.57f), "Open Bar + Barman 5: +47,5% vel / +57% cadência");

        RunState.Reset();
        RunState.StartDrink(DrinkType.Heal);
        Check(!RunState.IsActive(DrinkType.Heal), "Bebida de Cura é imediata, sem timer");
        RunState.StartDrink(DrinkType.Damage);
        RunState.Tick(10f);
        RunState.StartDrink(DrinkType.Damage);
        Check(Near(RunState.TimeLeft(DrinkType.Damage), DrinkRules.Duration), "mesma bebida só reinicia os 30s");
        RunState.StartDrink(DrinkType.Speed);
        Check(RunState.IsActive(DrinkType.Damage) && RunState.IsActive(DrinkType.Speed), "tipos diferentes acumulam");
        Check(RunState.DamageBonus > 0.39f && RunState.FireRateMultiplier > 1.29f && RunState.MoveSpeedMultiplier > 1.24f, "bebidas ativas alimentam os multiplicadores");
        RunState.Tick(30.5f);
        Check(!RunState.IsActive(DrinkType.Damage) && !RunState.IsActive(DrinkType.Speed), "bebidas expiram após 30s");
        Check(Near(RunState.DamageBonus, 0f) && Near(RunState.FireRateMultiplier, 1f) && Near(RunState.MoveSpeedMultiplier, 1f), "multiplicadores voltam ao base");
        RunState.Reset();
    }

    static void MelhoriasEPassivas()
    {
        var rng = new Random(99);
        for (int i = 0; i < 1000; i++)
        {
            var r = UpgradeRoll.Roll(i % 6, rng);
            Check(UpgradeRoll.TargetOf(r.Stat) == r.Target, "Rolo 2 compatível com o Rolo 1");
            Check(r.Percent >= 5 && r.Percent <= 30 && r.Percent % 5 == 0, "Rolo 3 literal 5%..30%");
        }

        RunState.Reset();
        RunState.ApplyUpgrade(new UpgradeResult(UpgradeTarget.Weapon, UpgradeStat.Magazine, 30));
        Check(Near(RunState.MagazineMultiplier, 1.3f), "+30% balas no pente");
        Check(new WeaponInstance(new WeaponData(), 1).MagazineSize == 11, "pente 8 com +30% = 11 (arredonda para cima)");
        for (int i = 0; i < 4; i++)
        {
            RunState.ApplyUpgrade(new UpgradeResult(UpgradeTarget.Player, UpgradeStat.MoveSpeed, 30));
            RunState.ApplyUpgrade(new UpgradeResult(UpgradeTarget.Player, UpgradeStat.DashCooldown, 30));
        }
        Check(Near(RunState.MoveSpeedMultiplier, RunState.MaxMoveSpeedMultiplier), "velocidade de movimento com teto de +60%");
        Check(Near(RunState.DashCooldownMultiplier, 0.2f), "recarga do dash com piso de 20%");
        RunState.ApplyUpgrade(new UpgradeResult(UpgradeTarget.Weapon, UpgradeStat.Damage, 15));
        Check(Near(RunState.DamageBonus, 0.15f), "melhoria de dano soma aditiva");

        RunState.Reset();
        Check(new WeaponInstance(new WeaponData(), 1).MagazineSize == 8 && Near(RunState.DamageBonus, 0f), "Reset zera melhorias");

        for (int round = 0; round < PassiveCatalog.Count; round++)
        {
            var offer = PassiveCatalog.Draw(RunState.Passives, 2, rng);
            int remaining = PassiveCatalog.Remaining(RunState.Passives);
            Check(offer.Count == Math.Min(2, remaining), "Roleta oferece 2 (ou o que restar)");
            Check(offer.Count < 2 || offer[0] != offer[1], "Roleta sem passivas repetidas");
            foreach (var p in offer) Check(!RunState.Has(p), "Roleta não oferece passiva já possuída");
            RunState.AddPassive(offer[0]);
        }
        Check(PassiveCatalog.Remaining(RunState.Passives) == 0 && PassiveCatalog.Draw(RunState.Passives, 2, rng).Count == 0, "catálogo esgota após 12 passivas");
        RunState.Reset();
        Check(RunState.Passives.Count == 0, "Reset zera passivas");
    }

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
        var maquina = EnemyCatalog.LoadBoss(CampaignRules.ForStage(15).Boss);
        Check(rainha.Behavior == EnemyBehavior.Rainha && rainha.IsBoss, "F5: A Rainha");
        Check(rei.Behavior == EnemyBehavior.Rei && rei.IsBoss, "F10: O Rei Tigrinho");
        Check(maquina.Behavior == EnemyBehavior.MaquinaCacaNiquel && maquina.EnemyName == "Máquina de Caça-Níquel", "F15: Máquina de Caça-Níquel é o chefe final");
        var rf = rainha.GetOrBuildFrames().GetFrameTexture("chase", 0) as Godot.AtlasTexture;
        Check(rf != null && Near(rf.Region.Size.Y, 51f) && Near(rf.Region.Size.X, 32f), "frames da Rainha 32x51");

        var summon = EnemyData.CreateSummon(EnemyData.CreateGoldenVariant(EnemyCatalog.Load(EnemyKind.Ficha)));
        Check(!summon.IsGolden && summon.Dollars == 0 && !summon.GuaranteedChip && summon.ChipDropChance == 0f && summon.DrinkDropChance == 0f, "invocado: nunca Dourado, $0 e sem drop");

        Check(EnemyBrain.Create(EnemyBehavior.MaquinaCacaNiquel) is MaquinaCacaNiquelBrain && EnemyBrain.Create(EnemyBehavior.Chase) == null, "cérebros por comportamento");
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

        foreach (EnemyKind kind in Enum.GetValues(typeof(EnemyKind)))
        {
            var src = EnemyCatalog.Load(kind);
            Check(src != null, $"bestiário carrega {kind}");
            var g = EnemyData.CreateGoldenVariant(src);
            Check(g.IsGolden && g.GuaranteedChip && g.Dollars == 5, $"Dourado válido para {kind}");
            Check(Near(g.MaxHp, src.MaxHp * EnemyData.GoldenHpMultiplier), $"Dourado {kind}: 1.8x HP");
            Check(Near(g.ContactDamage, src.ContactDamage * EnemyData.GoldenDamageMultiplier), $"Dourado {kind}: 1.3x Dano Contato");
            Check(Near(g.ProjectileDamage, src.ProjectileDamage * EnemyData.GoldenDamageMultiplier), $"Dourado {kind}: 1.3x Dano Projétil");
            Check(Near(g.Scale, src.Scale * EnemyData.GoldenScaleMultiplier), $"Dourado {kind}: 1.12x Escala");
        }
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
