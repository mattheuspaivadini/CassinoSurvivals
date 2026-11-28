using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Campo de fluxo por BFS (8 vizinhos, sem cortar quinas) a partir da célula do jogador.
/// Um cálculo serve a horda inteira: cada inimigo só consulta o vizinho mais próximo do alvo.
/// </summary>
public class FlowField
{
    static readonly Vector2I[] Dirs =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
    };

    readonly ArenaGrid grid;
    readonly int[] dist;
    readonly Queue<Vector2I> queue = new Queue<Vector2I>(256);

    public Vector2I Target { get; private set; } = new Vector2I(-1, -1);

    public FlowField(ArenaGrid grid)
    {
        this.grid = grid;
        dist = new int[grid.Width * grid.Height];
        Array.Fill(dist, -1);
    }

    /// <summary>Distância em passos até o alvo; -1 se inalcançável ou fora da grade.</summary>
    public int DistanceAt(Vector2I c) => grid.InBounds(c) ? dist[grid.Index(c)] : -1;

    /// <summary>O alvo é semente mesmo bloqueado: o jogador pode encostar num pilar.</summary>
    public void Compute(Vector2I target)
    {
        Target = target;
        Array.Fill(dist, -1);
        queue.Clear();
        if (!grid.InBounds(target)) return;

        dist[grid.Index(target)] = 0;
        queue.Enqueue(target);
        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            int next = dist[grid.Index(c)] + 1;
            foreach (var d in Dirs)
            {
                var n = c + d;
                if (grid.IsBlocked(n) || dist[grid.Index(n)] >= 0 || CutsCorner(c, d)) continue;
                dist[grid.Index(n)] = next;
                queue.Enqueue(n);
            }
        }
    }

    /// <summary>Vizinho com a menor distância até o alvo. Funciona mesmo partindo de célula bloqueada.</summary>
    public bool NextCell(Vector2I from, out Vector2I next)
    {
        next = from;
        int here = DistanceAt(from);
        int best = here >= 0 ? here : int.MaxValue;
        foreach (var d in Dirs)
        {
            var n = from + d;
            if (CutsCorner(from, d)) continue;
            int nd = DistanceAt(n);
            if (nd < 0 || nd >= best) continue;
            if (grid.IsBlocked(n) && n != Target) continue;
            best = nd;
            next = n;
        }
        return next != from;
    }

    /// <summary>Direção (normalizada, em px) para o centro da próxima célula; zero se não há caminho.</summary>
    public Vector2 Direction(Vector2 worldPos)
    {
        if (!NextCell(grid.ToCell(worldPos), out var next)) return Vector2.Zero;
        return (grid.ToWorld(next) - worldPos).Normalized();
    }

    bool CutsCorner(Vector2I c, Vector2I d) =>
        d.X != 0 && d.Y != 0 && (grid.IsBlocked(new Vector2I(c.X + d.X, c.Y)) || grid.IsBlocked(new Vector2I(c.X, c.Y + d.Y)));
}
