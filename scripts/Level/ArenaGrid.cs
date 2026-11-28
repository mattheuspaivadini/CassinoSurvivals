using System.Collections.Generic;
using Godot;

/// <summary>
/// Grade de navegação da arena. Uma célula é bloqueada quando toca um obstáculo inflado pelo raio
/// dos inimigos. Classe pura (sem nós): testada no SelfCheck.
/// </summary>
public class ArenaGrid
{
    public readonly int Width, Height;
    /// <summary>Canto superior esquerdo da grade, em px.</summary>
    public readonly Vector2 Origin;
    /// <summary>Lado da célula, em px.</summary>
    public readonly float Cell;

    readonly bool[] blocked;

    public ArenaGrid(Rect2 bounds, float cell, IEnumerable<Rect2> solids, float inflate)
    {
        Origin = bounds.Position;
        Cell = cell;
        Width = Mathf.CeilToInt(bounds.Size.X / cell);
        Height = Mathf.CeilToInt(bounds.Size.Y / cell);
        blocked = new bool[Width * Height];

        foreach (var solid in solids)
        {
            var r = solid.Grow(inflate);
            int x0 = Mathf.Max(0, Mathf.FloorToInt((r.Position.X - Origin.X) / cell));
            int y0 = Mathf.Max(0, Mathf.FloorToInt((r.Position.Y - Origin.Y) / cell));
            int x1 = Mathf.Min(Width - 1, Mathf.CeilToInt((r.End.X - Origin.X) / cell) - 1);
            int y1 = Mathf.Min(Height - 1, Mathf.CeilToInt((r.End.Y - Origin.Y) / cell) - 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    blocked[y * Width + x] = true;
        }
    }

    public bool InBounds(Vector2I c) => c.X >= 0 && c.Y >= 0 && c.X < Width && c.Y < Height;

    public int Index(Vector2I c) => c.Y * Width + c.X;

    /// <summary>Fora da grade conta como bloqueado.</summary>
    public bool IsBlocked(Vector2I c) => !InBounds(c) || blocked[Index(c)];

    public Vector2I ToCell(Vector2 p) =>
        new Vector2I(Mathf.FloorToInt((p.X - Origin.X) / Cell), Mathf.FloorToInt((p.Y - Origin.Y) / Cell));

    /// <summary>Centro da célula, em px.</summary>
    public Vector2 ToWorld(Vector2I c) => Origin + (new Vector2(c.X, c.Y) + new Vector2(0.5f, 0.5f)) * Cell;

    /// <summary>Amostra o segmento a cada meia célula; false se passar por célula bloqueada.</summary>
    public bool LineClear(Vector2 a, Vector2 b)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(a.DistanceTo(b) / (Cell * 0.5f)));
        for (int i = 0; i <= steps; i++)
            if (IsBlocked(ToCell(a.Lerp(b, (float)i / steps)))) return false;
        return true;
    }
}
