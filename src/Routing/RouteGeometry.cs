using System;
using System.Collections.Generic;

namespace KingdomPipeWrench.Routing
{
    internal static class RouteGeometry
    {
        public static List<RoutePoint> Simplify(IEnumerable<RoutePoint> points)
        {
            var result = new List<RoutePoint>();
            foreach (var point in points)
            {
                if (result.Count > 0 && result[result.Count - 1] == point) continue;
                while (result.Count >= 2 && Between(result[result.Count - 2], result[result.Count - 1], point))
                    result.RemoveAt(result.Count - 1);
                result.Add(point);
            }
            return result;
        }
        private static bool Between(RoutePoint a, RoutePoint b, RoutePoint c)
            => a.X == b.X && b.X == c.X && (long)(b.Y - a.Y) * (c.Y - b.Y) >= 0
            || a.Y == b.Y && b.Y == c.Y && (long)(b.X - a.X) * (c.X - b.X) >= 0;

        public static bool IsClear(IReadOnlyList<RoutePoint> points, IReadOnlyList<RouteObstacle> obstacles)
        {
            for (int i = 1; i < points.Count; i++)
            {
                if (points[i - 1].X != points[i].X && points[i - 1].Y != points[i].Y) return false;
                for (int j = 0; j < obstacles.Count; j++)
                    if (obstacles[j].Blocks(points[i - 1], points[i])) return false;
            }
            return true;
        }

        public static bool IsStraight(IReadOnlyList<RoutePoint> points)
        {
            if (points.Count < 2 || points[0] == points[points.Count - 1]) return false;
            var a = points[0]; var b = points[points.Count - 1];
            foreach (var p in points)
                if ((long)(p.X - a.X) * (b.Y - a.Y) != (long)(p.Y - a.Y) * (b.X - a.X)
                    || p.X < Math.Min(a.X, b.X) || p.X > Math.Max(a.X, b.X)
                    || p.Y < Math.Min(a.Y, b.Y) || p.Y > Math.Max(a.Y, b.Y)) return false;
            return true;
        }

        public static int Spine(RoutePoint a, RoutePoint b, RouteShape shape)
            => shape == RouteShape.NearStart ? a.X : shape == RouteShape.NearEnd ? b.X : a.X + (b.X - a.X) / 2;

        public static List<RoutePoint> Basic(RoutePoint start, RoutePoint end, int startDirection, int endDirection, RouteShape shape, RouteSnap snap = null)
        {
            var a = new RoutePoint(snap != null && startDirection != 0 ? snap.BeyondX(start.X + startDirection, startDirection) : start.X + 24 * startDirection, start.Y);
            var b = new RoutePoint(snap != null && endDirection != 0 ? snap.BeyondX(end.X + endDirection, endDirection) : end.X + 24 * endDirection, end.Y);
            if (start.Y != end.Y && TryTwoElbowSpine(start, end, a.X, b.X, startDirection, endDirection, shape, snap, out int spine))
                return Simplify(new[] { start, new RoutePoint(spine, start.Y), new RoutePoint(spine, end.Y), end });
            if (start.Y == end.Y && (startDirection == 0 || endDirection == 0 || (startDirection == -endDirection && (end.X - start.X) * startDirection >= 0)))
                return Simplify(new[] { start, end });
            if (start.Y == end.Y && startDirection == endDirection && a.X == b.X && snap != null)
                b = new RoutePoint(snap.NextX(b.X, endDirection), b.Y);
            int laneY = start.Y == end.Y ? start.Y + 64 : start.Y + (end.Y - start.Y) / 2;
            if (shape == RouteShape.NearStart && start.Y != end.Y) laneY = start.Y + Math.Sign(end.Y - start.Y) * Math.Min(24, Math.Abs(end.Y - start.Y) / 2);
            if (shape == RouteShape.NearEnd && start.Y != end.Y) laneY = end.Y - Math.Sign(end.Y - start.Y) * Math.Min(24, Math.Abs(end.Y - start.Y) / 2);
            if (snap != null) laneY = start.Y == end.Y ? snap.NextY(start.Y, 1) : snap.Y(laneY);
            return Simplify(new[] { start, a, new RoutePoint(a.X, laneY), new RoutePoint(b.X, laneY), b, end });
        }

        private static bool TryTwoElbowSpine(RoutePoint start, RoutePoint end, int ax, int bx, int startDirection, int endDirection,
            RouteShape shape, RouteSnap snap, out int spine)
        {
            if (startDirection != 0 && startDirection == endDirection)
            {
                spine = startDirection > 0 ? Math.Max(ax, bx) : Math.Min(ax, bx);
                return true;
            }
            if (startDirection != 0 && startDirection == -endDirection)
            {
                if ((end.X - start.X) * startDirection <= 0) { spine = 0; return false; }
                int preferred = Spine(new RoutePoint(ax, start.Y), new RoutePoint(bx, end.Y), shape);
                if (snap != null) preferred = snap.X(preferred);
                if (FitsPort(preferred, start.X, startDirection, 1) && FitsPort(preferred, end.X, endDirection, 1))
                { spine = preferred; return true; }
                if (snap != null)
                {
                    int mid = snap.X(start.X + (end.X - start.X) / 2);
                    foreach (int candidate in new[] { mid, ax, bx })
                        if (FitsPort(candidate, start.X, startDirection, 1) && FitsPort(candidate, end.X, endDirection, 1))
                        { spine = candidate; return true; }
                }
                if (Math.Abs(end.X - start.X) >= 2)
                { spine = start.X + (end.X - start.X) / 2; return true; }
                spine = 0; return false;
            }
            if (startDirection == 0 && endDirection == 0)
            {
                int raw = Spine(start, end, shape);
                spine = snap != null ? snap.X(raw) : raw;
                return true;
            }
            if (startDirection != 0)
            {
                int raw = Spine(new RoutePoint(ax, start.Y), end, shape);
                int candidate = snap != null ? snap.X(raw) : raw;
                spine = FitsPort(candidate, start.X, startDirection, 1) && (end.X - candidate) * startDirection >= 0 ? candidate : ax;
                return true;
            }
            int rawEnd = Spine(start, new RoutePoint(bx, end.Y), shape);
            int candidateEnd = snap != null ? snap.X(rawEnd) : rawEnd;
            spine = FitsPort(candidateEnd, end.X, endDirection, 1) && (start.X - candidateEnd) * endDirection >= 0 ? candidateEnd : bx;
            return true;
        }

        private static bool FitsPort(int x, int portX, int direction, int minDistance)
            => direction == 0 || (long)(x - portX) * direction >= minDistance;

        /// <summary>Slide an internal segment; endpoints remain anchored and adjacent segments stay orthogonal.</summary>
        public static List<RoutePoint> StraightSlideNodes(RoutePoint start, RoutePoint end, RouteSnap snap = null)
        {
            if (start.Y != end.Y) throw new ArgumentException("Expected a horizontal port-to-port pipe.");
            int length = Math.Abs(end.X - start.X);
            if (length < 12) return null;
            int distance = Math.Min(24, length / 3), direction = Math.Sign(end.X - start.X);
            var a = new RoutePoint(start.X + distance * direction, start.Y);
            var b = new RoutePoint(end.X - distance * direction, end.Y);
            if (snap != null)
            {
                var sa = new RoutePoint(snap.BeyondX(start.X + direction, direction), start.Y);
                var sb = new RoutePoint(snap.BeyondX(end.X - direction, -direction), end.Y);
                if ((sb.X - sa.X) * direction > 0) { a = sa; b = sb; }
            }
            return new List<RoutePoint> { a, a, b, b };
        }

        /// <summary>Slide an internal segment; endpoints remain anchored and adjacent segments stay orthogonal.</summary>
        public static List<RoutePoint> Slide(IReadOnlyList<RoutePoint> nodes, int segment, int coordinate)
        {
            if (segment < 0 || segment + 1 >= nodes.Count) throw new ArgumentOutOfRangeException(nameof(segment));
            var a = nodes[segment]; var b = nodes[segment + 1];
            if (a.X != b.X && a.Y != b.Y) throw new ArgumentException("Expected an orthogonal segment.");
            var result = new List<RoutePoint>(nodes);
            result[segment] = a.X == b.X ? new RoutePoint(coordinate, a.Y) : new RoutePoint(a.X, coordinate);
            result[segment + 1] = a.X == b.X ? new RoutePoint(coordinate, b.Y) : new RoutePoint(b.X, coordinate);
            return result;
        }
    }
}
