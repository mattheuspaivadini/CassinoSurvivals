using System.Collections.Generic;
using Godot;

/// <summary>Um inimigo agendado: quando a porta começa o alerta, qual porta e se é Dourado.</summary>
public readonly struct HordeEntry
{
    public readonly float Time;
    public readonly int Door;
    public readonly bool Elite;

    public HordeEntry(float time, int door, bool elite)
    {
        Time = time;
        Door = door;
        Elite = elite;
    }
}

/// <summary>
/// Agenda da horda finita (PDF, seções 2 e 10): taxa constante do início ao fim, portas sorteadas,
/// portas próximas ao jogador com atraso inicial e Dourados espaçados entre início e fim.
/// </summary>
public static class HordePlan
{
    public static List<HordeEntry> Build(int total, float duration, int doorCount, int eliteCount,
        ICollection<int> delayedDoors, float doorDelay, RandomNumberGenerator rng)
    {
        var elites = new HashSet<int>();
        for (int e = 0; e < eliteCount; e++)
        {
            float f = eliteCount == 1 ? 0.5f : 0.1f + 0.8f * e / (eliteCount - 1);
            elites.Add(Mathf.RoundToInt((total - 1) * f));
        }

        var plan = new List<HordeEntry>(total);
        int lastDoor = -1;
        for (int i = 0; i < total; i++)
        {
            float t = total > 1 ? duration * i / (total - 1) : 0f;
            int door;
            int tries = 0;
            do door = rng.RandiRange(0, doorCount - 1);
            while ((door == lastDoor && ++tries < 4) || (t < doorDelay && delayedDoors.Contains(door)));
            lastDoor = door;
            plan.Add(new HordeEntry(t, door, elites.Contains(i)));
        }
        return plan;
    }
}
