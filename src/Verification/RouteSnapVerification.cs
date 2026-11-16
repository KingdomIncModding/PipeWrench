using System;
using System.Linq;
using KingdomPipeWrench.Interaction;
using KingdomPipeWrench.Routing;

namespace KingdomPipeWrench.Verification
{
    internal static class RouteSnapVerification
    {
        public static RouteSnap Grid(int step) => new RouteSnap(p => new RoutePoint(
            (int)Math.Round((p.X + 32d) / step) * step - 32,
            (int)Math.Round((p.Y + 40d) / step) * step - 40));
        public static void Run(Action<bool, string> check)
        {
            int activeStep = 64;
            var live = new RouteSnap(p => new RoutePoint(Grid(activeStep).X(p.X), Grid(activeStep).Y(p.Y)));
            string before = live.Signature; activeStep = 16;
            check(before != live.Signature && live.X(17) == 16, "Live snap changes when Snappy research changes the native grid");
            foreach (int step in new[] { 64, 16 })
            {
                var snap = Grid(step);
                check(snap.NextX(-32, 1) == -32 + step && snap.NextY(24, -1) == 24 - step, "Grid neighbors use the active interval " + step);
                check(snap.BeyondX(33, 1) >= 33 && snap.BeyondX(31, -1) <= 31, "Grid bounds round outward " + step);
                var start = new RoutePoint(224, -232); var end = new RoutePoint(736, -360);
                var building = new[] { new RouteObstacle(432, -360, 544, -224) };
                var pipes = new[] { new RouteSegment(new RoutePoint(320, -232), new RoutePoint(608, -232)),
                    new RouteSegment(new RoutePoint(320, -360), new RoutePoint(608, -360)),
                    new RouteSegment(new RoutePoint(640, -200), new RoutePoint(640, -392)) };
                foreach (AvoidanceMode mode in Enum.GetValues(typeof(AvoidanceMode)))
                    foreach (RouteShape shape in new[] { RouteShape.Center, RouteShape.NearStart, RouteShape.NearEnd })
                    {
                        var constraints = new RouteConstraints(AvoidanceRules.Buildings(mode) ? building : Array.Empty<RouteObstacle>(),
                            AvoidanceRules.Overlap(mode) ? pipes : Array.Empty<RouteSegment>(), AvoidanceRules.Crossings(mode), start, end, snap);
                        var route = ObstacleRouter.Find(start, end, 1, -1, shape, constraints);
                        check(route != null && constraints.IsClear(route), "Snapped routing respects avoidance " + step + "/" + mode + "/" + shape);
                        for (int bends = 2; bends <= 12; bends += 2)
                        {
                            var refined = RouteBendPlanner.Plan(route, bends, constraints);
                            check(constraints.IsClear(refined) && refined.All(p => snap.X(p.X) == p.X && snap.Y(p.Y) == p.Y), "All automatic corners and added bends stay on the active lattice " + step + "/" + mode + "/" + bends);
                            check(refined[0] == start && refined.Last() == end && refined[1].X > start.X && refined[refined.Count - 2].X < end.X, "Snapping retains ports and outward directions " + step);
                        }
                    }
                var straight = new[] { start, new RoutePoint(736, -232) };
                var plan = PipeSectionPlan.Create(straight, 0, snap);
                var moved = plan.Move(snap.NextY(start.Y, -1));
                check(moved.All(p => snap.X(p.X) == p.X && snap.Y(p.Y) == p.Y), "Inserted slider corners follow the active grid " + step);
                check(moved.First() == straight[0] && moved.Last() == straight[1], "Slider snapping retains both ports " + step);
                var shortHorizontal = new[] { new RoutePoint(208, -232), new RoutePoint(272, -232) };
                var shortPlan = PipeSectionPlan.Create(shortHorizontal, -1, snap);
                check(shortPlan != null && shortPlan.Move(snap.NextY(-232, 1)).First() == shortHorizontal[0], "Short horizontal pipe still slides on " + step + " grid");
                var offGridVertical = new[] { new RoutePoint(0, 0), new RoutePoint(0, 160) };
                var verticalPlan = PipeSectionPlan.Create(offGridVertical, -1, snap);
                check(verticalPlan != null && verticalPlan.Coordinate == 0, "Off-grid vertical pipe preserves its actual X coordinate on " + step + " grid");
                var closeSnapFacing = RouteGeometry.Basic(new RoutePoint(208, 24), new RoutePoint(224, -104), 1, -1, RouteShape.Center, snap);
                check(RouteBendPlanner.Count(closeSnapFacing) == 2, "Close snapped facing ports stay at 2 elbows on " + step + " grid");
            }
        }
    }
}
