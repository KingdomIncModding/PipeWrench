using System;

namespace KingdomPipeWrench.Routing
{
    /// <summary>Queries the live native snap function, including other mods' grid changes.</summary>
    internal sealed class RouteSnap
    {
        private readonly Func<RoutePoint, RoutePoint> snap;
        public RouteSnap(Func<RoutePoint, RoutePoint> snap) { this.snap = snap; }
        public int X(int value) => snap(new RoutePoint(value, 0)).X;
        public int Y(int value) => snap(new RoutePoint(0, value)).Y;
        public int BeyondX(int value, int direction) => Beyond(value, direction, X);
        public int BeyondY(int value, int direction) => Beyond(value, direction, Y);
        public int NextX(int value, int direction) => Next(X(value), direction, X);
        public int NextY(int value, int direction) => Next(Y(value), direction, Y);
        public string Signature => X(0) + "/" + NextX(0, 1) + "/" + Y(0) + "/" + NextY(0, 1);
        private static int Beyond(int value, int direction, Func<int, int> axis)
        {
            int result = axis(value);
            return (long)(result - value) * direction >= 0 ? result : Next(result, direction, axis);
        }
        private static int Next(int origin, int direction, Func<int, int> axis)
        {
            for (int delta = 1; delta <= 4096; delta *= 2)
            {
                int result = axis(origin + delta * direction);
                if ((long)(result - origin) * direction > 0) return result;
            }
            throw new InvalidOperationException("Could not locate the next native pipe grid position.");
        }
    }
}
