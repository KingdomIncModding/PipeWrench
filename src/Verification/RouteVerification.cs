using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using KingdomPipeWrench.Routing;
using KingdomPipeWrench.Interaction;
using KingdomPipeWrench.UI;

namespace KingdomPipeWrench.Verification
{
    internal static class RouteVerification
    {
        private static int passed;
        private static void Check(bool condition, string name)
        { if (!condition) throw new InvalidOperationException(name); passed++; }

        private static void Validate(List<RoutePoint> route, RoutePoint start, RoutePoint end, IReadOnlyList<RouteObstacle> obstacles, string name)
        {
            Check(route != null && route.Count >= 2, name + ": route exists");
            Check(route[0] == start && route[route.Count - 1] == end, name + ": anchored endpoints");
            Check(RouteGeometry.IsClear(route, obstacles), name + ": orthogonal, no building intersections");
            Check(route.Zip(route.Skip(1), (a, b) => a != b).All(value => value), name + ": no duplicate points");
        }

        public static void Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--presentation-routes") { PresentationRoutes.Write(args[1]); return; }
            var timer = Stopwatch.StartNew();
            RoutePolicyVerification.Run(Check);
            RouteSnapVerification.Run(Check);
            PipeSectionVerification.Run(Check);
            var toolbar = ToolbarLayout.Load();
            Check(toolbar.w == 568 && toolbar.h == 53 && toolbar.children.Length == 7, "Runtime parser reads the complete user toolbar group");
            var rules = toolbar.children.Where(child => child.type == "group").ToArray();
            Check(rules.Length == 4 && rules.All(rule => rule.children.Length == 2), "Runtime parser retains nested button images and labels");
            Check(rules.All(rule => rule.children[1].font == "sans" && rule.children[1].fontSize == 17), "Runtime parser retains the user button fonts");
            Check(rules.All(rule => rule.children[0].slice && rule.children[0].opacity == 1), "Runtime parser retains sprite slicing and opacity");
            Check(!PipeToolVisibility.Show(true, true, false, false, false, false), "Tools are hidden before starting pipe work");
            Check(PipeToolVisibility.Show(true, true, false, true, false, false), "Tools appear as soon as a connection starts");
            Check(PipeToolVisibility.Show(true, true, false, false, true, false), "Tools remain visible after connection while the pipe is selected");
            Check(PipeToolVisibility.Show(true, true, false, false, false, true), "Grab Inserter displays the tools");
            Check(!PipeToolVisibility.Show(true, true, true, true, true, true), "Game menus temporarily hide pipe tools");
            Check(!PipeToolVisibility.Show(false, true, false, true, true, true), "Disabling the mod hides the tools");
            Check(!PipeToolVisibility.Show(true, false, false, true, true, true), "Tools stay inside the workspace");
            var edit = new PipeEditState();
            Check(!edit.Active && edit.Shape == RouteShape.Center, "New pipe editing starts with the right-angle default");
            edit.Begin(10, RouteShape.Center, 4);
            Check(!edit.ShouldDismiss(10, true, false, false), "Connection click cannot dismiss the just-selected pipe");
            Check(!edit.ShouldDismiss(11, false, false, false), "Moving away leaves pipe editing active");
            Check(!edit.ShouldDismiss(11, true, true, false), "Clicking a grabber keeps editing active");
            Check(!edit.ShouldDismiss(11, true, false, true), "Dragging a grabber keeps editing active");
            Check(edit.ShouldDismiss(11, true, false, false), "Clicking away ends pipe editing");
            foreach (var expected in new[] { RouteShape.NearStart, RouteShape.NearEnd, RouteShape.Freeform, RouteShape.Center })
            { edit.Cycle(); Check(edit.Shape == expected, "Connected-pipe P cycle: " + expected); }
            edit.Adjust(1); Check(edit.Bends == 6, "Connected-pipe bend increase stays available");
            edit.Touch(12); Check(!edit.ShouldDismiss(12, true, false, false), "Removing a grabber does not dismiss the remaining markers on the same click");
            edit.Dismiss(); edit.Cycle(); Check(!edit.Active && edit.Shape == RouteShape.Center, "P cannot change a dismissed selection");

            var session = new object(); var pipeIdentity = new object();
            var grabs = new SessionGrabbers<object>(); grabs.SetSession(session);
            grabs.Add(pipeIdentity); Check(grabs.IsVisible(pipeIdentity, -1), "A new center slider is visible for this session");
            grabs.Hide(pipeIdentity, -1); Check(!grabs.IsVisible(pipeIdentity, -1), "Right-click removal survives marker refresh");
            Check(grabs.Contains(pipeIdentity), "Removing a marker does not remove its pipe identity");
            grabs.Restore(pipeIdentity, -1); Check(grabs.IsVisible(pipeIdentity, -1), "Grab Inserter can restore a removed center slider");
            grabs.SetSession(session); Check(grabs.Contains(pipeIdentity), "Switching views within the same session preserves sliders");
            grabs.SetSession(new object()); Check(!grabs.Contains(pipeIdentity), "Loading a different kingdom/session clears all slider ownership");
            var restarted = new SessionGrabbers<object>(); restarted.SetSession(session);
            Check(!restarted.Contains(pipeIdentity), "A restarted mod never restores markers from persisted identifiers");

            Check(RouteGeometry.IsStraight(new[] { new RoutePoint(0, 0), new RoutePoint(80, 40), new RoutePoint(160, 80) }), "Inserter recognizes a direct diagonal pipe");
            Check(RouteGeometry.IsStraight(new[] { new RoutePoint(0, 0), new RoutePoint(80, 0), new RoutePoint(160, 0) }), "Inserter recognizes redundant collinear corners");
            Check(!RouteGeometry.IsStraight(new[] { new RoutePoint(0, 0), new RoutePoint(80, 0), new RoutePoint(160, 80) }), "Inserter rejects an already-bent pipe");
            Check(!RouteGeometry.IsStraight(new[] { new RoutePoint(0, 0), new RoutePoint(0, 0) }), "Inserter rejects a zero-length pipe");
            var empty = Array.Empty<RouteObstacle>();
            var start = new RoutePoint(0, 0); var end = new RoutePoint(320, 160);
            foreach (var shape in new[] { RouteShape.Center, RouteShape.NearStart, RouteShape.NearEnd })
            {
                var route = RouteGeometry.Basic(start, end, 1, -1, shape);
                Validate(route, start, end, empty, shape.ToString());
                int expected = shape == RouteShape.Center ? 160 : shape == RouteShape.NearStart ? 24 : 296;
                Check(route.Count == 4 && route[1].X == expected && route[2].X == expected, "Selected spine position");
            }
            Check(RouteGeometry.Basic(start, new RoutePoint(320, 0), 1, -1, RouteShape.Center).Count == 2, "Aligned ports use straight line");
            var baseRoute = RouteGeometry.Basic(start, end, 1, -1, RouteShape.Center);
            for (int count = 2; count <= 12; count += 2)
            {
                var planned = RouteBendPlanner.Plan(baseRoute, count, empty);
                Validate(planned, start, end, empty, "Requested " + count + " bends");
                Check(RouteBendPlanner.Count(planned) == count, "Exact requested bend count " + count);
                Check(planned[1].X > start.X && planned[planned.Count - 2].X < end.X, "Extra bends retain port exit directions");
                Check(baseRoute.Count == 4, "Changing bend count preserves the base route snapshot");
            }
            Check(RouteBendPlanner.Normalize(1) == 2 && RouteBendPlanner.Normalize(99) == 12 && RouteBendPlanner.Normalize(5) == 4, "Bend settings stay even and bounded");
            var straight = new[] { start, new RoutePoint(320, 0) };
            Check(RouteBendPlanner.Count(RouteBendPlanner.Plan(straight, 2, empty)) == 0, "Aligned connection remains straight at default setting");
            Check(RouteBendPlanner.Count(RouteBendPlanner.Plan(straight, 4, empty)) == 4, "Adding bends offsets an aligned connection");
            Check(RouteBendPlanner.Count(RouteBendPlanner.Plan(straight, 6, empty)) == 6, "Aligned offset supports further bend adjustment");
            var detour = new[] { new RouteObstacle(100, -60, 220, 80) };
            foreach (var shape in new[] { RouteShape.Center, RouteShape.NearStart, RouteShape.NearEnd })
                Validate(ObstacleRouter.Find(start, new RoutePoint(320, 0), 1, -1, shape, detour), start, new RoutePoint(320, 0), detour, "Building detour " + shape);

            var sourceAndTarget = new[] { new RouteObstacle(-80, -50, -8, 50), new RouteObstacle(328, -50, 400, 50), detour[0] };
            Validate(ObstacleRouter.Find(start, new RoutePoint(320, 0), 1, -1, RouteShape.Center, sourceAndTarget), start, new RoutePoint(320, 0), sourceAndTarget, "Endpoint buildings retained as obstacles");
            Check(ObstacleRouter.Find(start, end, 1, -1, RouteShape.Center, new[] { new RouteObstacle(-1000, -1000, 1000, 1000) }) == null, "Enclosed endpoint fails visibly");
            var tight = new[] { new RouteObstacle(10, -20, 80, 20) };
            Validate(ObstacleRouter.Find(start, new RoutePoint(320, 0), 1, -1, RouteShape.Center, tight), start, new RoutePoint(320, 0), tight, "Short port exit in a tight layout");

            var backwards = new RoutePoint(-320, 160);
            foreach (int from in new[] { -1, 1 })
                foreach (int to in new[] { -1, 1 })
                    foreach (var shape in new[] { RouteShape.Center, RouteShape.NearStart, RouteShape.NearEnd })
                    {
                        var route = RouteGeometry.Basic(start, backwards, from, to, shape);
                        Validate(route, start, backwards, empty, "Mirrored/backward " + from + to + shape);
                        Check((route[1].X - start.X) * from > 0, "Exits start port outward");
                        Check((route[route.Count - 2].X - backwards.X) * to > 0, "Enters destination from outside");
                    }

            var corners = new[] { new RoutePoint(160, 0), new RoutePoint(160, 160) };
            var moved = RouteGeometry.Slide(corners, 0, 200);
            Validate(new List<RoutePoint> { start, moved[0], moved[1], end }, start, end, empty, "Vertical grab marker");
            Check(moved[0].X == 200 && moved[1].X == 200 && corners[0].X == 160, "Slide moves both corners and preserves cancel snapshot");
            var horizontal = RouteGeometry.Slide(new[] { new RoutePoint(24, 80), new RoutePoint(296, 80) }, 0, 120);
            Check(horizontal.All(p => p.Y == 120), "Horizontal grab marker moves vertically");
            var straightEnd = new RoutePoint(320, 0);
            var straightNodes = RouteGeometry.Slide(RouteGeometry.StraightSlideNodes(start, straightEnd), 1, 80);
            var straightPath = new List<RoutePoint> { start }; straightPath.AddRange(straightNodes); straightPath.Add(straightEnd);
            Validate(straightPath, start, straightEnd, empty, "Straight-pipe marker inserts square corners");
            Check(RouteGeometry.Simplify(new[] { start, new RoutePoint(20, 0), new RoutePoint(10, 0) }).Count == 3, "Simplification never erases reversal");

            var axes = new[] { -100, -50, 0, 30, 75, 100 };
            var boxes = new[] { new RouteObstacle(-70, -80, 40, 90), new RouteObstacle(-500, -20, 500, 20) };
            var grid = new RouteGrid(axes, axes, boxes);
            for (int y = 0; y < axes.Length; y++)
                for (int x = 0; x < axes.Length; x++)
                {
                    var a = new RoutePoint(axes[x], axes[y]);
                    if (x + 1 < axes.Length) Check(grid.IsClear(x, y, x + 1, y) == !boxes.Any(box => box.Blocks(a, new RoutePoint(axes[x + 1], axes[y]))), "Horizontal collision cache agrees with geometry");
                    if (y + 1 < axes.Length) Check(grid.IsClear(x, y, x, y + 1) == !boxes.Any(box => box.Blocks(a, new RoutePoint(axes[x], axes[y + 1]))), "Vertical collision cache agrees with geometry");
                }

            var random = new Random(1441);
            int found = 0;
            for (int trial = 0; trial < 200; trial++)
            {
                var destination = new RoutePoint(600, random.Next(-200, 201));
                var buildings = new List<RouteObstacle>();
                for (int i = 0; i < 15; i++)
                {
                    int x = random.Next(65, 510), y = random.Next(-240, 200);
                    buildings.Add(new RouteObstacle(x, y, x + random.Next(16, 65), y + random.Next(16, 80)));
                }
                var route = ObstacleRouter.Find(start, destination, 1, -1, (RouteShape)(1 + trial % 3), buildings);
                if (route == null) continue;
                Validate(route, start, destination, buildings, "Random layout " + trial);
                var extraBends = RouteBendPlanner.Plan(route, 8, buildings);
                Validate(extraBends, start, destination, buildings, "Bend adjustment with avoidance " + trial);
                Check(RouteBendPlanner.Count(extraBends) >= RouteBendPlanner.Count(route), "Avoidance keeps required building detours");
                Check(route[1].X > 0 && route[route.Count - 2].X < destination.X, "Avoidance preserves endpoint direction");
                found++;
            }
            Check(found >= 180, "At least 90% of open random layouts route successfully");
            Console.WriteLine($"PASS: {passed} assertions; {found}/200 random layouts routed; {timer.ElapsedMilliseconds} ms.");
        }
    }
}
