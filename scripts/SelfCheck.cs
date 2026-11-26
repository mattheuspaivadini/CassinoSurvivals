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

    static void Check(bool ok, string what)
    {
        if (!ok) throw new Exception("Self-check falhou: " + what);
    }
}
