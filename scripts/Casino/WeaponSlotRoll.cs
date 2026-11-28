using System;
using System.Collections.Generic;

/// <summary>Monta os 3 rolos do Caça-níquel de Armas a partir do tier já sorteado (Tabela A).</summary>
public static class WeaponSlotRoll
{
    /// <summary>
    /// Tier 1: 3 armas diferentes. Tier 2: 1 arma em 2 slots aleatórios + 1 diferente no restante.
    /// Tier 3: a mesma arma nos 3 slots. Pool menor que o tier pede cai para o tier possível.
    /// </summary>
    public static T[] BuildReels<T>(IReadOnlyList<T> pool, int tier, Random rng)
    {
        if (pool == null || pool.Count == 0) throw new ArgumentException("pool vazio");
        var reels = new T[3];
        int a = rng.Next(pool.Count);

        if (tier >= 3 || pool.Count == 1)
        {
            reels[0] = reels[1] = reels[2] = pool[a];
            return reels;
        }

        int b = OtherThan(pool.Count, rng, a);
        if (tier == 2 || pool.Count == 2)
        {
            int odd = rng.Next(3);
            for (int i = 0; i < 3; i++) reels[i] = i == odd ? pool[b] : pool[a];
            return reels;
        }

        int c = OtherThan(pool.Count, rng, a, b);
        reels[0] = pool[a];
        reels[1] = pool[b];
        reels[2] = pool[c];
        return reels;
    }

    static int OtherThan(int count, Random rng, int x, int y = -1)
    {
        int n;
        do n = rng.Next(count); while (n == x || n == y);
        return n;
    }
}
