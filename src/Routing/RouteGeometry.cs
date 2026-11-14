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
            int spine = Spine(a, b, shape);
            if (snap != null) spine = snap.X(spine);
            bool startFits = startDirection == 0 || (spine - start.X) * startDirection >= (snap == null ? 24 : 1);
            bool endFits = endDirection == 0 || (spine - end.X) * endDirection >= (snap == null ? 24 : 1);
            if (startFits && endFits)
                return Simplify(new[] { start, new RoutePoint(spine, start.Y), new RoutePoint(spine, end.Y), end });
            int laneY = start.Y == end.Y ? start.Y + 64 : start.Y + (end.Y - start.Y) / 2;
            if (shape == RouteShape.NearStart && start.Y != end.Y) laneY = start.Y + Math.Sign(end.Y - start.Y) * Math.Min(24, Math.Abs(end.Y - start.Y) / 2);
            if (shape == RouteShape.NearEnd && start.Y != end.Y) laneY = end.Y - Math.Sign(end.Y - start.Y) * Math.Min(24, Math.Abs(end.Y - start.Y) / 2);
            if (snap != null) laneY = start.Y == end.Y ? snap.NextY(start.Y, 1) : snap.Y(laneY);
            return Simplify(new[] { start, a, new RoutePoint(a.X, laneY), new RoutePoint(b.X, laneY), b, end });
        }

        /// <summary>Slide an internal segment; endpoints remain anchored and adjacent segments stay orthogonal.</summary>
        public static List<RoutePoint> StraightSlideNodes(RoutePoint start, RoutePoint end, RouteSnap snap = null)
        {
            if (start.Y != end.Y) throw new ArgumentException("Expected a horizontal port-to-port pipe.");
            int distance = Math.Min(24, Math.Abs(end.X - start.X) / 3), direction = Math.Sign(end.X - start.X);
            var a = new RoutePoint(start.X + distance * direction, start.Y);
            var b = new RoutePoint(end.X - distance * direction, end.Y);
            if (snap != null)
            {
                a = new RoutePoint(snap.BeyondX(start.X + direction, direction), start.Y);
                b = new RoutePoint(snap.BeyondX(end.X - direction, -direction), end.Y);
                if ((b.X - a.X) * direction <= 0) return null;
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
