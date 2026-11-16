using System;
using System.Collections.Generic;
using System.Linq;

namespace KingdomPipeWrench.Routing
{
    /// <summary>Bounded, coordinate-compressed rectilinear A*: no per-pixel world grid.</summary>
    internal static class ObstacleRouter
    {
        public static List<RoutePoint> Find(RoutePoint start, RoutePoint end, int startDirection, int endDirection,
            RouteShape shape, IReadOnlyList<RouteObstacle> obstacles)
            => Find(start, end, startDirection, endDirection, shape, new RouteConstraints(obstacles));

        public static List<RoutePoint> Find(RoutePoint start, RoutePoint end, int startDirection, int endDirection,
            RouteShape shape, RouteConstraints constraints)
        {
            var basic = RouteGeometry.Basic(start, end, startDirection, endDirection, shape, constraints.Snap);
            if (constraints.IsClear(basic)) return basic;
            var exit = ExitStub(start, startDirection, constraints);
            var entrance = ExitStub(end, endDirection, constraints);
            if (!exit.HasValue || !entrance.HasValue) return null;
            var a = exit.Value; var b = entrance.Value;
            for (int margin = 128; margin <= 512; margin *= 4)
            {
                var route = Search(a, b, startDirection, endDirection, shape, constraints, margin);
                if (route == null) continue;
                route.Insert(0, start); route.Add(end);
                route = RouteGeometry.Simplify(route);
                if (constraints.IsClear(route)) return route;
            }
            return null;
        }

        private static RoutePoint? ExitStub(RoutePoint port, int direction, RouteConstraints constraints)
        {
            if (constraints.Snap != null)
            {
                var snapped = new RoutePoint(direction == 0 ? port.X : constraints.Snap.BeyondX(port.X + direction, direction), port.Y);
                if (constraints.IsClear(new[] { port, snapped })) return snapped;
                if (direction == 0) return null;
            }
            foreach (int length in new[] { 24, 16, 8, 4, 1 })
            {
                var point = new RoutePoint(port.X + length * direction, port.Y);
                if (constraints.IsClear(new[] { port, point })) return point;
            }
            return null;
        }

        private static List<RoutePoint> Search(RoutePoint start, RoutePoint end, int startDirection, int endDirection,
            RouteShape shape, RouteConstraints constraints, int margin)
        {
            int left = Math.Min(start.X, end.X) - margin, right = Math.Max(start.X, end.X) + margin;
            int bottom = Math.Min(start.Y, end.Y) - margin, top = Math.Max(start.Y, end.Y) + margin;
            var obstacles = constraints.Buildings.Where(o => o.Right > left && o.Left < right && o.Top > bottom && o.Bottom < top).ToArray();
            var xs = new SortedSet<int> { start.X, end.X, left, right, RouteGeometry.Spine(start, end, shape) };
            var ys = new SortedSet<int> { start.Y, end.Y, bottom, top };
            var snap = constraints.Snap;
            if (snap != null)
            {
                xs = new SortedSet<int> { start.X, end.X, snap.BeyondX(left, 1), snap.BeyondX(right, -1), snap.X(RouteGeometry.Spine(start, end, shape)) };
                ys = new SortedSet<int> { start.Y, end.Y, snap.BeyondY(bottom, 1), snap.BeyondY(top, -1) };
            }
            foreach (var o in obstacles)
            {
                int x0 = snap?.BeyondX(o.Left, -1) ?? o.Left, x1 = snap?.BeyondX(o.Right, 1) ?? o.Right;
                int y0 = snap?.BeyondY(o.Bottom, -1) ?? o.Bottom, y1 = snap?.BeyondY(o.Top, 1) ?? o.Top;
                if (x0 >= left && x0 <= right) xs.Add(x0);
                if (x1 >= left && x1 <= right) xs.Add(x1);
                if (y0 >= bottom && y0 <= top) ys.Add(y0);
                if (y1 >= bottom && y1 <= top) ys.Add(y1);
            }
            foreach (var segment in constraints.Pipes)
            {
                if (Math.Max(segment.A.X, segment.B.X) < left || Math.Min(segment.A.X, segment.B.X) > right
                    || Math.Max(segment.A.Y, segment.B.Y) < bottom || Math.Min(segment.A.Y, segment.B.Y) > top) continue;
                foreach (var point in new[] { segment.A, segment.B })
                    foreach (int direction in new[] { -1, 0, 1 })
                    {
                        int px = snap == null ? point.X + direction * 8 : direction == 0 ? snap.X(point.X) : snap.BeyondX(point.X + direction, direction);
                        int py = snap == null ? point.Y + direction * 8 : direction == 0 ? snap.Y(point.Y) : snap.BeyondY(point.Y + direction, direction);
                        if (px >= left && px <= right) xs.Add(px);
                        if (py >= bottom && py <= top) ys.Add(py);
                    }
            }
            var x = xs.ToArray(); var y = ys.ToArray();
            if ((long)x.Length * y.Length > 60000) return null;
            int width = x.Length, count = width * y.Length * 2;
            int source = (Array.IndexOf(y, start.Y) * width + Array.IndexOf(x, start.X)) * 2;
            int target = (Array.IndexOf(y, end.Y) * width + Array.IndexOf(x, end.X)) * 2;
            var cost = Enumerable.Repeat(double.PositiveInfinity, count).ToArray();
            var previous = Enumerable.Repeat(-1, count).ToArray();
            var closed = new bool[count]; var queue = new RouteQueue();
            var grid = new RouteGrid(x, y, obstacles, constraints);
            cost[source] = 0; queue.Push(source, RoutePoint.Distance(start, end));
            int preferredX = RouteGeometry.Spine(start, end, shape), expanded = 0;
            while (queue.Count > 0 && expanded++ < 100000)
            {
                int state = queue.Pop();
                if (closed[state]) continue;
                closed[state] = true;
                int cell = state / 2, axis = state % 2, ix = cell % width, iy = cell / width;
                var here = new RoutePoint(x[ix], y[iy]);
                if (state / 2 == target / 2 && CanFinish(state, axis, source, target, endDirection, previous, x, width))
                    return Reconstruct(state, previous, x, y);
                for (int direction = 0; direction < 4; direction++)
                {
                    int nx = ix + (direction == 0 ? -1 : direction == 1 ? 1 : 0);
                    int ny = iy + (direction == 2 ? -1 : direction == 3 ? 1 : 0);
                    int nextAxis = direction < 2 ? 0 : 1;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= y.Length) continue;
                    if (state == source && startDirection != 0 && nextAxis == 0 && (x[nx] - x[ix]) * startDirection <= 0) continue;
                    var next = new RoutePoint(x[nx], y[ny]);
                    if (!grid.IsClear(ix, iy, nx, ny)) continue;
                    int nextState = (ny * width + nx) * 2 + nextAxis;
                    double distance = RoutePoint.Distance(here, next);
                    double preference = nextAxis == 1 ? distance * Math.Abs((double)here.X - preferredX) / (64 + Math.Abs((double)end.X - start.X)) * 0.04 : 0;
                    double value = cost[state] + distance + (axis != nextAxis ? 48 : 0) + preference;
                    if (value >= cost[nextState]) continue;
                    cost[nextState] = value; previous[nextState] = state;
                    queue.Push(nextState, value + RoutePoint.Distance(next, end));
                }
            }
            return null;
        }

        private static bool CanFinish(int state, int axis, int source, int target, int endDirection, int[] previous, int[] x, int width)
        {
            if (endDirection == 0 || axis == 1 || state == source || source == target) return true;
            int prev = previous[state];
            if (prev < 0) return false;
            int prevX = x[(prev / 2) % width], hereX = x[(target / 2) % width];
            return (long)(prevX - hereX) * endDirection > 0;
        }

        private static List<RoutePoint> Reconstruct(int state, int[] previous, int[] x, int[] y)
        {
            var route = new List<RoutePoint>();
            while (state >= 0)
            {
                int cell = state / 2; route.Add(new RoutePoint(x[cell % x.Length], y[cell / x.Length])); state = previous[state];
            }
            route.Reverse(); return RouteGeometry.Simplify(route);
        }
    }
}
