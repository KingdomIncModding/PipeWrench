using System;
using System.Collections.Generic;
using System.Linq;
using KingdomPipeWrench.Interaction;
using KingdomPipeWrench.Routing;

namespace KingdomPipeWrench.Verification
{
    internal static class PipeSectionVerification
    {
        public static void Run(Action<bool, string> check)
        {
            var bent = new[] { new RoutePoint(0, 0), new RoutePoint(120, 0), new RoutePoint(120, 160), new RoutePoint(320, 160) };
            check(!RouteGeometry.IsStraight(bent), "Regression fixture: vanilla pipe is bent overall");
            int picked = PipeSectionPicker.Nearest(bent, new RoutePoint(120, 80), 22 * 22, out double distance);
            check(picked == 1 && distance == 0, "Grab Inserter selects a straight section of a bent vanilla pipe");
            var middle = PipeSectionPlan.Create(bent, picked);
            var movedMiddle = middle.Move(160);
            check(movedMiddle.SequenceEqual(new[] { bent[0], new RoutePoint(160, 0), new RoutePoint(160, 160), bent[3] }), "Internal square corners slide together");
            check(bent[1].X == 120 && middle.Segment == picked, "Planning and sliding preserve the original cancel snapshot");
            check(middle.Remap(0) == 0 && middle.Remap(1) == 1 && middle.Remap(2) == 2, "Sliding existing corners retains other marker identities");
            check(PipeSectionPicker.Nearest(bent, new RoutePoint(60, 0), 22 * 22, out _) == 0, "Output-side run can be picked");
            check(PipeSectionPicker.Nearest(bent, new RoutePoint(220, 160), 22 * 22, out _) == 2, "Input-side run can be picked");
            check(PipeSectionPicker.Nearest(bent, new RoutePoint(500, 500), 22 * 22, out _) == -1, "Empty space does not select an arbitrary pipe");
            check(PipeSectionPicker.Nearest(new[] { bent[0], bent[0] }, bent[0], 22 * 22, out _) == -1, "Zero-length sections are not insertable");

            foreach (int section in new[] { 0, 2 })
            {
                var plan = PipeSectionPlan.Create(bent, section);
                var moved = plan.Move(plan.Coordinate + 64);
                check(moved[0] == bent[0] && moved.Last() == bent.Last(), "Terminal-section slide keeps both ports fixed");
                check(moved.Contains(bent[1]) && moved.Contains(bent[2]), "Terminal-section slide retains all original bend positions");
                check(plan.Affected(moved).First() == bent[section] && plan.Affected(moved).Last() == bent[section + 1], "Only the selected terminal section is expanded");
                check(RouteGeometry.IsClear(moved, Array.Empty<RouteObstacle>()), "Terminal-section slide adds square corners");
                check(plan.Remap(section) == plan.Segment, "Inserted terminal handle follows its offset run");
                check(moved.Count == bent.Length + 4, "Terminal horizontal move inserts local offset nodes");
                var again = PipeSectionPlan.Create(moved, plan.Segment);
                check(again.Move(again.Coordinate + 16).Count == moved.Count, "Repeated sliding reuses inserted corners");
            }

            var vertical = new[] { new RoutePoint(0, 0), new RoutePoint(0, 160), new RoutePoint(240, 160) };
            var verticalPlan = PipeSectionPlan.Create(vertical, 0);
            var movedVertical = verticalPlan.Move(64);
            check(verticalPlan.Vertical && movedVertical[0] == vertical[0] && movedVertical.Last() == vertical.Last(), "Vertical port-side section offsets sideways");
            check(movedVertical.Contains(vertical[1]), "Vertical port-side insertion retains its original bend");
            check(RouteGeometry.IsClear(movedVertical, Array.Empty<RouteObstacle>()), "Vertical port-side offset is orthogonal");

            var diagonal = new[] { new RoutePoint(-100, -50), new RoutePoint(0, 0), new RoutePoint(120, 80), new RoutePoint(240, 80) };
            int diagonalPick = PipeSectionPicker.Nearest(diagonal, new RoutePoint(60, 40), 22 * 22, out _);
            check(diagonalPick == 1, "Diagonal section of a bent native pipe is pickable");
            var diagonalPlan = PipeSectionPlan.Create(diagonal, diagonalPick);
            var movedDiagonal = diagonalPlan.Move(diagonalPlan.Coordinate + 32);
            check(movedDiagonal[0] == diagonal[0] && movedDiagonal[1] == diagonal[1] && movedDiagonal.Last() == diagonal.Last(), "Unrelated diagonal run and port positions remain unchanged");
            check(RouteGeometry.IsClear(diagonalPlan.Affected(movedDiagonal), Array.Empty<RouteObstacle>()), "Selected diagonal section gains square local corners");
            check(!RouteGeometry.IsClear(movedDiagonal, Array.Empty<RouteObstacle>()), "Fixture retains the unrelated native diagonal");
            var localRules = new RouteConstraints(new[] { new RouteObstacle(-90, -45, -50, -20) });
            check(localRules.IsClear(diagonalPlan.Affected(movedDiagonal)), "Unchanged distant native geometry does not block a local slide");
            var block = new RouteConstraints(new[] { new RouteObstacle(80, 20, 100, 60) });
            check(!block.IsClear(diagonalPlan.Affected(movedDiagonal)), "New local geometry still respects building avoidance");

            foreach (var straight in new[] {
                new[] { new RoutePoint(0, 0), new RoutePoint(320, 0) },
                new[] { new RoutePoint(0, 0), new RoutePoint(120, 80), new RoutePoint(240, 160) },
                new[] { new RoutePoint(0, 0), new RoutePoint(160, 0), new RoutePoint(320, 0) } })
            {
                var snapshot = straight.ToArray();
                var plan = PipeSectionPlan.Create(straight, -1);
                check(plan != null && straight.SequenceEqual(snapshot), "Whole straight-pipe inserter still preserves its native geometry until moved");
                var moved = plan.Move(plan.Coordinate + 64);
                check(moved[0] == straight[0] && moved.Last() == straight.Last(), "Whole straight-pipe slide retains both ports");
                check(RouteGeometry.IsClear(moved, Array.Empty<RouteObstacle>()), "Whole straight-pipe slide remains square");
                check(plan.Remap(-1) == plan.Segment, "Whole-pipe center marker follows its new run");
            }

            var session = new object(); var pipe = new object();
            var markers = new SessionGrabbers<object>(); markers.SetSession(session);
            markers.Restore(pipe, 0); markers.Restore(pipe, 2);
            check(markers.IsVisible(pipe, 0, false) && markers.IsVisible(pipe, 2, false), "Native pipe has only the requested section markers");
            check(!markers.IsVisible(pipe, 1) && !markers.IsVisible(pipe, -1), "Inserting a section does not add markers to unrelated sections");
            var terminalPlan = PipeSectionPlan.Create(bent, 0);
            markers.Remap(pipe, terminalPlan.Remap);
            check(markers.IsVisible(pipe, terminalPlan.Segment) && markers.IsVisible(pipe, 6, false), "Moving one terminal marker reindexes another marker on the same pipe");
            check(!markers.IsVisible(pipe, 0, false), "The old terminal marker is not recreated after moving its run");
            markers.Hide(pipe, terminalPlan.Segment);
            check(!markers.IsVisible(pipe, terminalPlan.Segment) && markers.IsVisible(pipe, 6, false), "Right-click removes only its chosen section marker");
            markers.Restore(pipe, terminalPlan.Segment);
            check(markers.IsVisible(pipe, terminalPlan.Segment), "Grab Inserter can restore a removed section marker");
            markers.SetSession(new object());
            check(!markers.Contains(pipe) && !markers.IsVisible(pipe, 6), "Section markers and remapping are session-only");
            markers.Add(pipe);
            check(markers.IsVisible(pipe, 1) && !markers.IsVisible(pipe, 0, false), "Tool-routed pipes retain automatic internal markers without extra port markers");
            markers.Restore(pipe, 0);
            check(markers.IsVisible(pipe, 0, false), "An explicit port-side marker can be added to a tool-routed pipe");
            var anotherPipe = new object(); markers.Restore(anotherPipe, 6);
            markers.Add(anotherPipe);
            check(markers.IsVisible(anotherPipe, 1), "Rerouting an inserted native pipe enables its new internal handles");

            var rng = new Random(48175);
            for (int i = 0; i < 100; i++)
            {
                int x = rng.Next(8, 40) * 8, y = rng.Next(8, 40) * 8;
                var path = new[] { new RoutePoint(0, 0), new RoutePoint(x, 0), new RoutePoint(x, y), new RoutePoint(x + 160, y) };
                for (int section = 0; section < 3; section++)
                {
                    var a = path[section]; var b = path[section + 1];
                    var midpoint = new RoutePoint((a.X + b.X) / 2, (a.Y + b.Y) / 2);
                    check(PipeSectionPicker.Nearest(path, midpoint, 22 * 22, out _) == section, "Random bent-pipe section pick");
                    var plan = PipeSectionPlan.Create(path, section);
                    var moved = plan.Move(plan.Coordinate + 32);
                    check(moved[0] == path[0] && moved.Last() == path.Last(), "Random section slide anchors ports");
                    check(RouteGeometry.IsClear(plan.Affected(moved), Array.Empty<RouteObstacle>()), "Random local slide keeps right angles");
                }
            }
        }
    }
}
