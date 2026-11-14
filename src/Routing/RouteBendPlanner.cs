using System;
using System.Collections.Generic;

namespace KingdomPipeWrench.Routing
{
    /// <summary>Adds editable right-angle corners while preserving endpoints and port directions.</summary>
    internal static class RouteBendPlanner
    {
        public static int Normalize(int value) => Math.Max(2, Math.Min(12, value / 2 * 2));

        public static List<RoutePoint> Plan(IReadOnlyList<RoutePoint> original, int requested, IReadOnlyList<RouteObstacle> obstacles)
            => Plan(original, requested, new RouteConstraints(obstacles));

        public static List<RoutePoint> Plan(IReadOnlyList<RoutePoint> original, int requested, RouteConstraints constraints)
        {
            var route = RouteGeometry.Simplify(original);
            int target = Normalize(requested);
            while (Count(route) < target)
            {
                var next = RefineCorner(route, constraints);
                if (next == null && Count(route) + 4 <= target) next = OffsetSegment(route, constraints);
                if (next == null) break;
                route = next;
            }
            return route;
        }

        public static int Count(IReadOnlyList<RoutePoint> route)
        {
            int result = 0;
            for (int i = 1; i + 1 < route.Count; i++)
            {
                var a = route[i - 1]; var b = route[i]; var c = route[i + 1];
                if ((long)(b.X - a.X) * (c.Y - b.Y) != (long)(b.Y - a.Y) * (c.X - b.X)) result++;
            }
            return result;
        }

        private static List<RoutePoint> RefineCorner(List<RoutePoint> route, RouteConstraints constraints)
        {
            List<RoutePoint> best = null; int bestRoom = 0;
            for (int i = 1; i + 1 < route.Count; i++)
            {
                var a = route[i - 1]; var b = route[i]; var c = route[i + 1];
                int room = Math.Min(RoutePoint.Distance(a, b), RoutePoint.Distance(b, c));
                if (room < 24 || room <= bestRoom) continue;
                var first = new RoutePoint(a.X + (b.X - a.X) / 2, a.Y + (b.Y - a.Y) / 2);
                var last = new RoutePoint(b.X + (c.X - b.X) / 2, b.Y + (c.Y - b.Y) / 2);
                var snap = constraints.Snap;
                if (snap != null)
                {
                    first = a.X == b.X ? new RoutePoint(a.X, snap.Y(first.Y)) : new RoutePoint(snap.X(first.X), a.Y);
                    last = b.X == c.X ? new RoutePoint(b.X, snap.Y(last.Y)) : new RoutePoint(snap.X(last.X), b.Y);
                    if (first == a || first == b || last == b || last == c
                        || RoutePoint.Distance(a, first) + RoutePoint.Distance(first, b) != RoutePoint.Distance(a, b)
                        || RoutePoint.Distance(b, last) + RoutePoint.Distance(last, c) != RoutePoint.Distance(b, c)) continue;
                }
                var middle = new RoutePoint(first.X + last.X - b.X, first.Y + last.Y - b.Y);
                var candidate = new List<RoutePoint>(route);
                candidate.RemoveAt(i); candidate.InsertRange(i, new[] { first, middle, last });
                candidate = RouteGeometry.Simplify(candidate);
                if (Count(candidate) != Count(route) + 2 || !constraints.IsClear(candidate)) continue;
                best = candidate; bestRoom = room;
            }
            return best;
        }

        private static List<RoutePoint> OffsetSegment(List<RoutePoint> route, RouteConstraints constraints)
        {
            List<RoutePoint> best = null; int bestRoom = 0;
            for (int i = 0; i + 1 < route.Count; i++)
            {
                var a = route[i]; var b = route[i + 1];
                int length = RoutePoint.Distance(a, b);
                if (length < 72 || length <= bestRoom) continue;
                bool horizontal = a.Y == b.Y;
                int dx = Math.Sign(b.X - a.X), dy = Math.Sign(b.Y - a.Y);
                var first = new RoutePoint(a.X + dx * 24, a.Y + dy * 24);
                var last = new RoutePoint(b.X - dx * 24, b.Y - dy * 24);
                var snap = constraints.Snap;
                if (snap != null)
                {
                    first = horizontal ? new RoutePoint(snap.BeyondX(a.X + dx, dx), a.Y) : new RoutePoint(a.X, snap.BeyondY(a.Y + dy, dy));
                    last = horizontal ? new RoutePoint(snap.BeyondX(b.X - dx, -dx), b.Y) : new RoutePoint(b.X, snap.BeyondY(b.Y - dy, -dy));
                    if (first == last || RoutePoint.Distance(a, first) + RoutePoint.Distance(first, last) + RoutePoint.Distance(last, b) != length) continue;
                }
                foreach (int side in new[] { 1, -1 })
                {
                    var offsetA = new RoutePoint(first.X + (horizontal ? 0 : side * 32), first.Y + (horizontal ? side * 32 : 0));
                    var offsetB = new RoutePoint(last.X + (horizontal ? 0 : side * 32), last.Y + (horizontal ? side * 32 : 0));
                    if (snap != null)
                    {
                        offsetA = horizontal ? new RoutePoint(first.X, snap.BeyondY(first.Y + side, side)) : new RoutePoint(snap.BeyondX(first.X + side, side), first.Y);
                        offsetB = horizontal ? new RoutePoint(last.X, offsetA.Y) : new RoutePoint(offsetA.X, last.Y);
                    }
                    var candidate = new List<RoutePoint>(route);
                    candidate.InsertRange(i + 1, new[] { first, offsetA, offsetB, last });
                    candidate = RouteGeometry.Simplify(candidate);
                    if (Count(candidate) != Count(route) + 4 || !constraints.IsClear(candidate)) continue;
                    best = candidate; bestRoom = length; break;
                }
            }
            return best;
        }
    }
}
