using System;
using System.Collections.Generic;
using KingdomPipeWrench.Routing;

namespace KingdomPipeWrench.Interaction
{
    internal static class PipeSectionPicker
    {
        public static int Nearest(IReadOnlyList<RoutePoint> path, RoutePoint mouse, double maximumSquared, out double distanceSquared)
        {
            int selected = -1; distanceSquared = maximumSquared;
            for (int i = 0; i + 1 < path.Count; i++)
            {
                var a = path[i]; var b = path[i + 1];
                double dx = (double)b.X - a.X, dy = (double)b.Y - a.Y, length = dx * dx + dy * dy;
                if (length == 0) continue;
                double t = Math.Max(0, Math.Min(1, (((double)mouse.X - a.X) * dx + ((double)mouse.Y - a.Y) * dy) / length));
                double x = mouse.X - a.X - dx * t, y = mouse.Y - a.Y - dy * t, distance = x * x + y * y;
                if (distance >= distanceSquared) continue;
                selected = i; distanceSquared = distance;
            }
            return selected;
        }
    }
}
