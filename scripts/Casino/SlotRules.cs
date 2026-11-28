/// <summary>Regra de combinação das 3 roletas (pura, sem engine, fácil de testar).</summary>
public static class SlotRules
{
    public static int CountMatches(WeaponData[] reels, WeaponData chosen)
    {
        int n = 0;
        for (int i = 0; i < reels.Length; i++) if (reels[i] == chosen) n++;
        return n;
    }

    public static bool IsJackpot(WeaponData[] reels) => CountMatches(reels, reels[0]) == reels.Length;

    /// <summary>PDF, seção 4: 1x → +1 nível, 2x → +2, 3x → +3. Arma nova desbloqueia nesse nível.</summary>
    public static int LevelsFor(int matches) => matches >= 3 ? 3 : matches == 2 ? 2 : 1;
}
