using System;
using System.Linq;
using KingdomPipeWrench.Routing;

namespace KingdomPipeWrench.Verification
{
    internal static class RoutePolicyVerification
    {
        public static void Run(Action<bool, string> check)
        {
            var mode = AvoidanceMode.Buildings;
            foreach (var expected in new[] { AvoidanceMode.Overlap, AvoidanceMode.BuildingsAndOverlap, AvoidanceMode.Strict, AvoidanceMode.Buildings })
            { mode = AvoidanceRules.Next(mode); check(mode == expected, "Shift avoidance order: " + expected); }
            foreach (AvoidanceMode rule in Enum.GetValues(typeof(AvoidanceMode)))
            {
                check(AvoidanceRules.Buildings(rule) == (rule != AvoidanceMode.Overlap), "Building policy: " + rule);
                check(AvoidanceRules.Overlap(rule) == (rule != AvoidanceMode.Buildings), "Overlap policy: " + rule);
                check(AvoidanceRules.Crossings(rule) == (rule == AvoidanceMode.Strict), "Crossing policy: " + rule);
            }
            var start = new RoutePoint(0, 0); var end = new RoutePoint(320, 0);
            var empty = Array.Empty<RouteObstacle>();
            var shared = new RouteSegment(new RoutePoint(80, 0), new RoutePoint(240, 0));
            check(shared.Blocks(start, end, false), "Overlap is blocked even with crossing allowed");
            check(shared.Blocks(end, start, false), "Reversed shared runs are also overlap");
            check(!shared.Blocks(start, new RoutePoint(80, 0), false), "Single-point contact is not shared length");
            check(!shared.Blocks(new RoutePoint(0, 8), new RoutePoint(320, 8), true), "Nearby parallel pipes do not overlap");
            var crossing = new RouteSegment(new RoutePoint(160, -100), new RoutePoint(160, 100));
            check(!crossing.Blocks(start, end, false), "Perpendicular crossing is allowed in overlap mode");
            check(crossing.Blocks(start, end, true), "Perpendicular crossing is blocked in strict mode");
            var diagonal = new RouteSegment(new RoutePoint(80, -80), new RoutePoint(240, 80));
            check(!diagonal.Blocks(start, end, false) && diagonal.Blocks(start, end, true), "Native diagonal crossings follow the same rules");
            var distant = new RouteSegment(new RoutePoint(400, 20), new RoutePoint(500, 40));
            check(!distant.Blocks(start, end, true), "Nonintersecting diagonal is clear");

            foreach (bool strict in new[] { false, true })
                foreach (var blocker in new[] { shared, crossing, diagonal })
                {
                    var rules = new RouteConstraints(empty, new[] { blocker }, strict, start, end);
                    var route = ObstacleRouter.Find(start, end, 1, -1, RouteShape.Center, rules);
                    check(route != null && rules.IsClear(route), "Router respects " + (strict ? "strict" : "overlap") + " pipe constraint");
                    check(route[0] == start && route.Last() == end, "Pipe avoidance preserves both ports");
                    check(route[1].X > start.X && route[route.Count - 2].X < end.X, "Pipe avoidance preserves port directions");
                    if (!strict && blocker.Equals(crossing)) check(route.Count == 2, "Allowed crossing keeps the direct route");
                    for (int bends = 2; bends <= 12; bends += 2)
                        check(rules.IsClear(RouteBendPlanner.Plan(route, bends, rules)), "Extra bends retain pipe policy");
                }
            var sharedPort = new RouteSegment(start, new RoutePoint(0, -120));
            var sharedRules = new RouteConstraints(empty, new[] { sharedPort }, true, start, end);
            check(sharedRules.IsClear(new[] { start, end }), "Strict routes can leave a shared port");
            check(ObstacleRouter.Find(start, end, 1, -1, RouteShape.Center, sharedRules)?.Count == 2, "Shared terminal permits a direct outward route");
            var touching = new RouteSegment(new RoutePoint(160, -80), new RoutePoint(160, 0));
            var touchRules = new RouteConstraints(empty, new[] { touching }, true, start, end);
            var touchRoute = ObstacleRouter.Find(start, end, 1, -1, RouteShape.Center, touchRules);
            check(touchRoute != null && touchRules.IsClear(touchRoute) && touchRoute.Count > 2, "Strict route cannot slip through an existing pipe endpoint");
            var building = new RouteObstacle(96, -32, 224, 32);
            foreach (AvoidanceMode policy in Enum.GetValues(typeof(AvoidanceMode)))
            {
                var constraints = new RouteConstraints(AvoidanceRules.Buildings(policy) ? new[] { building } : empty,
                    AvoidanceRules.Overlap(policy) ? new[] { new RouteSegment(new RoutePoint(160, -112), new RoutePoint(160, 112)) } : Array.Empty<RouteSegment>(),
                    AvoidanceRules.Crossings(policy), start, end);
                var result = ObstacleRouter.Find(start, end, 1, -1, RouteShape.Center, constraints);
                check(result != null && constraints.IsClear(result), "Combined building/pipe layout follows " + policy);
                if (policy == AvoidanceMode.Overlap) check(result.Count == 2, "Overlap-only mode can cross the building and perpendicular pipe");
            }

            var rng = new Random(48174);
            for (int sample = 0; sample < 100; sample++)
            {
                var pipes = Enumerable.Range(0, 8).Select(_ => new RouteSegment(
                    new RoutePoint(rng.Next(6, 34) * 8, rng.Next(-20, 20) * 8),
                    new RoutePoint(rng.Next(6, 34) * 8, rng.Next(-20, 20) * 8))).ToArray();
                var rules = new RouteConstraints(empty, pipes, sample % 2 == 0, start, end);
                var route = ObstacleRouter.Find(start, end, 1, -1, RouteShape.Center, rules);
                check(route != null && rules.IsClear(route), "Random native pipe detour " + sample);
                int[] x = { 0, 64, 128, 192, 256, 320 }, y = { -192, -96, 0, 96, 192 };
                var grid = new RouteGrid(x, y, empty, rules);
                for (int iy = 0; iy < y.Length; iy++)
                    for (int ix = 0; ix < x.Length; ix++)
                    {
                        var a = new RoutePoint(x[ix], y[iy]);
                        if (ix + 1 < x.Length) check(grid.IsClear(ix, iy, ix + 1, iy) == rules.IsClear(new[] { a, new RoutePoint(x[ix + 1], y[iy]) }), "Pipe grid horizontal edge matches geometry");
                        if (iy + 1 < y.Length) check(grid.IsClear(ix, iy, ix, iy + 1) == rules.IsClear(new[] { a, new RoutePoint(x[ix], y[iy + 1]) }), "Pipe grid vertical edge matches geometry");
                    }
            }
        }
    }
}
