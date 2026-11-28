using Godot;

/// <summary>Regras puras de inimigos e bosses (PDF, seções 8 e 9). Sem nós: testadas no --selfcheck.</summary>
public static class EnemyRules
{
    /// <summary>Fração do dano que passa quando excede o teto de DPS no mesmo segundo.</summary>
    public const float OverCapFactor = 0.2f;
    public const float FuryHpFraction = 0.4f;
    public const float LockHpFraction = 0.5f;

    /// <summary>
    /// Armadura de Boss: dano dentro do teto do segundo entra 100%; o excedente entra a 20%.
    /// <paramref name="takenThisSecond"/> é o dano já aceito (sem redução) neste segundo.
    /// </summary>
    public static float ApplyDpsCap(float takenThisSecond, float incoming, float cap)
    {
        if (cap <= 0f || incoming <= 0f) return Mathf.Max(0f, incoming);
        float room = Mathf.Max(0f, cap - takenThisSecond);
        float full = Mathf.Min(incoming, room);
        return full + (incoming - full) * OverCapFactor;
    }

    /// <summary>Trava de fase: o dano não leva o HP abaixo do marco enquanto a trava não disparou.</summary>
    public static float ClampToLock(float hp, float maxHp, float lockFraction, float damage)
    {
        float floor = maxHp * lockFraction;
        if (hp <= floor) return damage;
        return Mathf.Min(damage, hp - floor);
    }

    public static bool InCircle(Vector2 point, Vector2 center, float radius) =>
        point.DistanceSquaredTo(center) <= radius * radius;

    /// <summary>Ponto dentro da faixa de largura 2·halfWidth ao longo do segmento from→to.</summary>
    public static bool InLine(Vector2 point, Vector2 from, Vector2 to, float halfWidth)
    {
        Vector2 closest = Geometry2D.GetClosestPointToSegment(point, from, to);
        return point.DistanceSquaredTo(closest) <= halfWidth * halfWidth;
    }

    /// <summary>Direções de um leque de <paramref name="count"/> projéteis, centrado em <paramref name="aim"/>.</summary>
    public static Vector2[] FanDirections(Vector2 aim, int count, float spreadDegrees)
    {
        var dirs = new Vector2[Mathf.Max(1, count)];
        Vector2 a = aim.LengthSquared() > 0f ? aim.Normalized() : Vector2.Right;
        float spread = Mathf.DegToRad(spreadDegrees);
        for (int i = 0; i < dirs.Length; i++)
        {
            float t = dirs.Length == 1 ? 0f : (float)i / (dirs.Length - 1) - 0.5f;
            dirs[i] = a.Rotated(spread * t);
        }
        return dirs;
    }

    /// <summary>Quantos invocados ainda cabem sem passar do teto simultâneo.</summary>
    public static int SummonRoom(int alive, int wanted, int max) => Mathf.Clamp(max - alive, 0, wanted);
}
