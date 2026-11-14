using System;
using System.Collections.Generic;

namespace KingdomPipeWrench.Routing
{
    /// <summary>Cached edge collisions avoid scanning every building at each A* step.</summary>
    internal sealed class RouteGrid
    {
        private readonly bool[] horizontal, vertical;
        private readonly int width;
        public RouteGrid(int[] x, int[] y, IReadOnlyList<RouteObstacle> obstacles, RouteConstraints constraints = null)
        {
            width = x.Length;
            horizontal = new bool[(width - 1) * y.Length]; vertical = new bool[width * (y.Length - 1)];
            foreach (var box in obstacles)
            {
                int firstX = Math.Max(0, Upper(x, box.Left) - 1), lastX = Math.Min(width - 2, Lower(x, box.Right) - 1);
                for (int iy = Upper(y, box.Bottom); iy < Lower(y, box.Top); iy++)
                    for (int ix = firstX; ix <= lastX; ix++) horizontal[iy * (width - 1) + ix] = true;
                int firstY = Math.Max(0, Upper(y, box.Bottom) - 1), lastY = Math.Min(y.Length - 2, Lower(y, box.Top) - 1);
                for (int ix = Upper(x, box.Left); ix < Lower(x, box.Right); ix++)
                    for (int iy = firstY; iy <= lastY; iy++) vertical[iy * width + ix] = true;
            }
            if (constraints == null) return;
            foreach (var pipe in constraints.Pipes)
            {
                int firstX = Math.Max(0, Lower(x, Math.Min(pipe.A.X, pipe.B.X)) - 1);
                int lastX = Math.Min(width - 1, Upper(x, Math.Max(pipe.A.X, pipe.B.X)));
                int firstY = Math.Max(0, Lower(y, Math.Min(pipe.A.Y, pipe.B.Y)) - 1);
                int lastY = Math.Min(y.Length - 1, Upper(y, Math.Max(pipe.A.Y, pipe.B.Y)));
                for (int iy = firstY; iy <= lastY; iy++)
                    for (int ix = firstX; ix <= lastX; ix++)
                    {
                        var a = new RoutePoint(x[ix], y[iy]);
                        if (ix + 1 < width && pipe.Blocks(a, new RoutePoint(x[ix + 1], y[iy]), constraints.AvoidCrossings, constraints.Start, constraints.End))
                            horizontal[iy * (width - 1) + ix] = true;
                        if (iy + 1 < y.Length && pipe.Blocks(a, new RoutePoint(x[ix], y[iy + 1]), constraints.AvoidCrossings, constraints.Start, constraints.End))
                            vertical[iy * width + ix] = true;
                    }
            }
        }
        public bool IsClear(int x, int y, int nextX, int nextY)
            => y == nextY ? !horizontal[y * (width - 1) + Math.Min(x, nextX)] : !vertical[Math.Min(y, nextY) * width + x];
        private static int Lower(int[] values, int value)
        { int i = Array.BinarySearch(values, value); return i < 0 ? ~i : i; }
        private static int Upper(int[] values, int value)
        { int i = Array.BinarySearch(values, value); return i < 0 ? ~i : i + 1; }
    }
}
