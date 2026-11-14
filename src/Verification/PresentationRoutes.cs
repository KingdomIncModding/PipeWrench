using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using KingdomPipeWrench.Routing;

namespace KingdomPipeWrench.Verification
{
    /// <summary>Exports actual routing results for the illustrated promotional animation.</summary>
    internal static class PresentationRoutes
    {
        public static void Write(string path)
        {
            var start = new RoutePoint(224, -232); var end = new RoutePoint(736, -360);
            var snap = RouteSnapVerification.Grid(16);
            var empty = new RouteObstacle[0];
            var basic = RouteGeometry.Basic(start, end, 1, -1, RouteShape.Center, snap);
            var routes = new Dictionary<string, int[][]>();
            foreach (int count in new[] { 2, 4, 6 })
                routes["bends" + count] = Screen(RouteBendPlanner.Plan(basic, count, new RouteConstraints(empty, snap: snap)));
            routes["nearStart"] = Screen(RouteGeometry.Basic(start, end, 1, -1, RouteShape.NearStart, snap));
            routes["nearEnd"] = Screen(RouteGeometry.Basic(start, end, 1, -1, RouteShape.NearEnd, snap));
            var buildings = new[] { new RouteObstacle(64, -284, 216, -180), new RouteObstacle(744, -412, 896, -308), new RouteObstacle(432, -360, 544, -224) };
            var avoidance = ObstacleRouter.Find(start, end, 1, -1, RouteShape.Center, new RouteConstraints(buildings, snap: snap));
            if (avoidance == null || !RouteGeometry.IsClear(avoidance, buildings)) throw new System.InvalidOperationException("Demo route must avoid the depicted buildings.");
            routes["avoid"] = Screen(avoidance);
            WritePolicyRoutes(routes, start, end, buildings);
            foreach (var route in routes.Values)
                foreach (var p in route)
                    if (snap.X(p[0]) != p[0] || snap.Y(-p[1]) != -p[1]) throw new System.InvalidOperationException("Showcase must use Snappy's researched pipe lattice.");
            File.WriteAllText(path, JsonSerializer.Serialize(routes));
        }
        private static void WritePolicyRoutes(Dictionary<string, int[][]> routes, RoutePoint start, RoutePoint end, RouteObstacle[] buildings)
        {
            var pipes = new[] {
                new RouteSegment(new RoutePoint(320, -232), new RoutePoint(608, -232)),
                new RouteSegment(new RoutePoint(320, -216), new RoutePoint(608, -216)),
                new RouteSegment(new RoutePoint(640, -200), new RoutePoint(640, -392))
            };
            for (int i = 0; i < pipes.Length; i++) routes["policyPipe" + i] = Screen(new[] { pipes[i].A, pipes[i].B });
            var modes = new[] { AvoidanceMode.Buildings, AvoidanceMode.Overlap, AvoidanceMode.BuildingsAndOverlap, AvoidanceMode.Strict };
            var names = new[] { "policyBuildings", "policyOverlap", "policyBoth", "policyStrict" };
            for (int i = 0; i < modes.Length; i++)
            {
                var mode = modes[i];
                var constraints = new RouteConstraints(AvoidanceRules.Buildings(mode) ? buildings : new RouteObstacle[0],
                    AvoidanceRules.Overlap(mode) ? pipes : new RouteSegment[0], AvoidanceRules.Crossings(mode), start, end, RouteSnapVerification.Grid(16));
                var path = ObstacleRouter.Find(start, end, 1, -1, RouteShape.Center, constraints);
                if (path == null || !constraints.IsClear(path)) throw new System.InvalidOperationException("Invalid showcase policy route: " + mode);
                bool overlaps = path.Skip(1).Select((p, j) => pipes.Any(pipe => pipe.Blocks(path[j], p, false))).Any(b => b);
                bool crosses = path.Skip(1).Select((p, j) => pipes[2].Blocks(path[j], p, true)).Any(b => b);
                if (overlaps != (i == 0) || crosses != (i != 3))
                    throw new System.InvalidOperationException("Showcase must visibly distinguish overlap and crossing: " + mode + " " + JsonSerializer.Serialize(Screen(path)));
                if (path.Any(p => p.X < 224 || p.X > 736 || -p.Y + 28 < 208 || -p.Y + 28 > 450))
                    throw new System.InvalidOperationException("Showcase route is outside the viewport: " + mode + " " + JsonSerializer.Serialize(Screen(path)));
                routes[names[i]] = Screen(path);
            }
        }
        private static int[][] Screen(IEnumerable<RoutePoint> points) => points.Select(p => new[] { p.X, -p.Y }).ToArray();
    }
}
