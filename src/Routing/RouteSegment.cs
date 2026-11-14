using System;

namespace KingdomPipeWrench.Routing
{
    /// <summary>Existing pipe centerline, including native diagonal segments.</summary>
    internal readonly struct RouteSegment
    {
        public readonly RoutePoint A, B;
        public RouteSegment(RoutePoint a, RoutePoint b) { A = a; B = b; }
        private static long Cross(RoutePoint a, RoutePoint b, RoutePoint c)
            => ((long)b.X - a.X) * ((long)c.Y - a.Y) - ((long)b.Y - a.Y) * ((long)c.X - a.X);
        private static bool On(RoutePoint p, RoutePoint a, RoutePoint b)
            => Cross(a, b, p) == 0 && p.X >= Math.Min(a.X, b.X) && p.X <= Math.Max(a.X, b.X)
                && p.Y >= Math.Min(a.Y, b.Y) && p.Y <= Math.Max(a.Y, b.Y);

        public bool Blocks(RoutePoint a, RoutePoint b, bool crossings, RoutePoint? allowedStart = null, RoutePoint? allowedEnd = null)
        {
            if (A == B || a == b) return false;
            long c = Cross(a, b, A), d = Cross(a, b, B), e = Cross(A, B, a), f = Cross(A, B, b);
            if (c == 0 && d == 0)
            {
                bool horizontal = a.X != b.X;
                int lo = Math.Max(Math.Min(horizontal ? a.X : a.Y, horizontal ? b.X : b.Y), Math.Min(horizontal ? A.X : A.Y, horizontal ? B.X : B.Y));
                int hi = Math.Min(Math.Max(horizontal ? a.X : a.Y, horizontal ? b.X : b.Y), Math.Max(horizontal ? A.X : A.Y, horizontal ? B.X : B.Y));
                if (hi > lo) return true; // Positive length shared run, regardless of direction.
                if (hi < lo || !crossings) return false;
            }
            else if (!crossings || !((c == 0 && On(A, a, b)) || (d == 0 && On(B, a, b))
                || (e == 0 && On(a, A, B)) || (f == 0 && On(b, A, B))
                || (Math.Sign(c) != Math.Sign(d) && Math.Sign(e) != Math.Sign(f)))) return false;
            // Only shared connection terminals may touch in strict mode. Grid corners cannot slip across a pipe.
            return !Allowed(allowedStart, a, b) && !Allowed(allowedEnd, a, b);
        }
        private bool Allowed(RoutePoint? terminal, RoutePoint a, RoutePoint b)
            => terminal.HasValue && (terminal.Value == A || terminal.Value == B) && On(terminal.Value, a, b);
    }
}
