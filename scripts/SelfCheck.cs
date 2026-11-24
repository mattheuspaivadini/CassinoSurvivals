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
    }

    static void Check(bool ok, string what)
    {
        if (!ok) throw new Exception("Self-check falhou: " + what);
    }
}
