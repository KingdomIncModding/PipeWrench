using System;
using System.Collections.Generic;

namespace KingdomPipeWrench.Routing
{
    internal sealed class RouteConstraints
    {
        public readonly IReadOnlyList<RouteObstacle> Buildings;
        public readonly IReadOnlyList<RouteSegment> Pipes;
        public readonly bool AvoidCrossings;
        public readonly RoutePoint? Start, End;
        public readonly RouteSnap Snap;
        public RouteConstraints(IReadOnlyList<RouteObstacle> buildings, IReadOnlyList<RouteSegment> pipes = null,
            bool avoidCrossings = false, RoutePoint? start = null, RoutePoint? end = null, RouteSnap snap = null)
        { Buildings = buildings; Pipes = pipes ?? Array.Empty<RouteSegment>(); AvoidCrossings = avoidCrossings; Start = start; End = end; Snap = snap; }
        public bool IsClear(IReadOnlyList<RoutePoint> path)
        {
            if (!RouteGeometry.IsClear(path, Buildings)) return false;
            for (int i = 1; i < path.Count; i++)
                foreach (var pipe in Pipes)
                    if (pipe.Blocks(path[i - 1], path[i], AvoidCrossings, Start, End)) return false;
            return true;
        }
    }
}
