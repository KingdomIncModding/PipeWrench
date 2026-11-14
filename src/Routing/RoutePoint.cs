using System;

namespace KingdomPipeWrench.Routing
{
    internal readonly struct RoutePoint : IEquatable<RoutePoint>
    {
        public readonly int X, Y;
        public RoutePoint(int x, int y) { X = x; Y = y; }
        public bool Equals(RoutePoint other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is RoutePoint point && Equals(point);
        public override int GetHashCode() => unchecked(X * 397 ^ Y);
        public static bool operator ==(RoutePoint a, RoutePoint b) => a.Equals(b);
        public static bool operator !=(RoutePoint a, RoutePoint b) => !a.Equals(b);
        public static int Distance(RoutePoint a, RoutePoint b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        public override string ToString() => $"({X},{Y})";
    }
}
