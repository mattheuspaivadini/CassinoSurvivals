/// <summary>Regra de combinação das 3 roletas (pura, sem Unity, fácil de testar).</summary>
public static class SlotRules
{
    public static int CountMatches(WeaponData[] reels, WeaponData chosen)
    {
        int n = 0;
        for (int i = 0; i < reels.Length; i++) if (reels[i] == chosen) n++;
        return n;
    }

    /// <summary>1x → 1 nível, 2x → 5, 3x (Jackpot) → 9. Serve tanto para upar quanto para o nível de desbloqueio.</summary>
    public static int LevelsFor(int matches) => matches >= 3 ? 9 : matches == 2 ? 5 : 1;
}
