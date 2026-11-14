using System;

namespace KingdomPipeWrench.Routing
{
    internal readonly struct RouteObstacle
    {
        public readonly int Left, Bottom, Right, Top;
        public RouteObstacle(int left, int bottom, int right, int top)
        { Left = Math.Min(left, right); Right = Math.Max(left, right); Bottom = Math.Min(bottom, top); Top = Math.Max(bottom, top); }
        public bool Contains(RoutePoint p) => p.X > Left && p.X < Right && p.Y > Bottom && p.Y < Top;
        public bool Blocks(RoutePoint a, RoutePoint b)
        {
            if (a.X == b.X)
                return a.X > Left && a.X < Right && Math.Max(a.Y, b.Y) > Bottom && Math.Min(a.Y, b.Y) < Top;
            if (a.Y == b.Y)
                return a.Y > Bottom && a.Y < Top && Math.Max(a.X, b.X) > Left && Math.Min(a.X, b.X) < Right;
            return true;
        }
    }
}
