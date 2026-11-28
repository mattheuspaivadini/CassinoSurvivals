using System;
using System.Collections.Generic;

/// <summary>Catálogo fechado das 12 Passivas Únicas da Mesa de Roleta (PDF, seção 6), na ordem do documento.</summary>
public enum Passive
{
    CalibrePesado,
    EfeitoDomino,
    GeloNoCopo,
    MaoQuente,
    AllIn,
    SaqueRapido,
    VirarAMesa,
    BlefePerfeito,
    OpenBar,
    TrocoEmSangue,
    Critico777,
    SangueFrio,
}

/// <summary>Nomes, descrições e sorteio sem repetição das 12 Passivas Únicas (regras de combate em PassiveRules).</summary>
public static class PassiveCatalog
{
    public const int Count = 12;

    static readonly string[] Names =
    {
        "Calibre Pesado", "Efeito Dominó", "Gelo no Copo", "Mão Quente", "All-In", "Saque Rápido",
        "Virar a Mesa", "Blefe Perfeito", "Open Bar", "Troco em Sangue", "777 Crítico", "Sangue Frio",
    };

    static readonly string[] Descriptions =
    {
        "Todas as armas ganham +1 de perfuração",
        "Inimigos abatidos explodem em área curta",
        "Projéteis aplicam -30% de velocidade por 1,5s",
        "Projéteis queimam: +30% do dano ao longo de 1,5s",
        "Todas as balas aplicam empurrão",
        "A arma guardada recarrega após 2s fora da mão",
        "Ao sofrer dano, libera um pulso que empurra inimigos",
        "A cada 15s, o próximo dash atravessa sem dano",
        "Bebidas 50% mais eficientes",
        "Cura 5% a cada 20 abates e 3% por Dourado",
        "20% de chance de crítico (2x de dano)",
        "Menos HP, mais velocidade e cadência (até +35%)",
    };

    public static string Name(Passive p) => Names[(int)p];
    public static string Description(Passive p) => Descriptions[(int)p];

    public static int Remaining(IReadOnlyCollection<Passive> owned) => Count - owned.Count;

    /// <summary>Até <paramref name="count"/> passivas distintas, todas fora de <paramref name="owned"/>, com peso igual.</summary>
    public static List<Passive> Draw(IEnumerable<Passive> owned, int count, Random rng)
    {
        var have = new HashSet<Passive>(owned);
        var pool = new List<Passive>(Count);
        for (int i = 0; i < Count; i++)
            if (!have.Contains((Passive)i)) pool.Add((Passive)i);

        var result = new List<Passive>(count);
        while (result.Count < count && pool.Count > 0)
        {
            int k = rng.Next(pool.Count);
            result.Add(pool[k]);
            pool.RemoveAt(k);
        }
        return result;
    }
}
