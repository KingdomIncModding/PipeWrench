using System;
using System.Collections.Generic;
using KingdomFoundation.Api;
using KingdomPipeWrench.Routing;
using UnityEngine;

namespace KingdomPipeWrench.Interaction
{
    internal static class WorkspaceGeometry
    {
        public static RoutePoint Point(Vector2Int value) => new RoutePoint(value.x, value.y);
        public static Vector2Int Vector(RoutePoint value) => new Vector2Int(value.X, value.Y);
        public static RouteSnap NativeSnap() => new RouteSnap(point => Point(FoundationApi.NativeIO.SnapRouteNode(Vector(point))));
        public static RouteConstraints Constraints(WorkspacePipe edited, AvoidanceMode mode, int clearance, RoutePoint? start = null, RoutePoint? end = null)
        {
            var pipes = new List<RouteSegment>();
            var workspace = FoundationApi.Kingdom?.ActiveWorkspace;
            if (workspace != null && AvoidanceRules.Overlap(mode))
                foreach (var other in workspace.Pipes)
                {
                    if (other == edited) continue;
                    var path = FullPath(other);
                    for (int i = 1; i < path.Count; i++) if (path[i - 1] != path[i]) pipes.Add(new RouteSegment(path[i - 1], path[i]));
                }
            return new RouteConstraints(AvoidanceRules.Buildings(mode) ? Obstacles(clearance) : (IReadOnlyList<RouteObstacle>)Array.Empty<RouteObstacle>(),
                pipes, AvoidanceRules.Crossings(mode), start, end, NativeSnap());
        }
        public static List<RouteObstacle> Obstacles(int clearance)
        {
            var result = new List<RouteObstacle>();
            var workspace = FoundationApi.Kingdom?.ActiveWorkspace;
            if (workspace == null) return result;
            var corners = new Vector3[4];
            foreach (var item in workspace.Buildings)
            {
                var pos = item.Position;
                var size = item.World ? FoundationApi.WorkspaceVisuals.Size(item.World) : item.Size;
                result.Add(new RouteObstacle(pos.x - clearance, pos.y - Mathf.CeilToInt(size.y) - clearance,
                    pos.x + Mathf.CeilToInt(size.x) + clearance, pos.y + clearance));
                if (item.World) AddLabelObstacle(result, FoundationApi.WorkspaceVisuals.BuildingLabel(item.World), clearance, corners);
            }
            return result;
        }
        private static void AddLabelObstacle(List<RouteObstacle> result, RectTransform label, int clearance, Vector3[] corners)
        {
            if (!label || !label.gameObject.activeInHierarchy || label.rect.width <= 0 || label.rect.height <= 0) return;
            label.GetWorldCorners(corners);
            float left = corners[0].x, right = left, bottom = corners[0].y, top = bottom;
            for (int i = 1; i < corners.Length; i++)
            {
                left = Mathf.Min(left, corners[i].x); right = Mathf.Max(right, corners[i].x);
                bottom = Mathf.Min(bottom, corners[i].y); top = Mathf.Max(top, corners[i].y);
            }
            result.Add(new RouteObstacle(Mathf.FloorToInt(left) - clearance, Mathf.FloorToInt(bottom) - clearance,
                Mathf.CeilToInt(right) + clearance, Mathf.CeilToInt(top) + clearance));
        }
        public static List<RoutePoint> FullPath(WorkspacePipe pipe)
        {
            var result = new List<RoutePoint>();
            if (pipe?.ConnectedOutput?.World == null || pipe.ConnectedInput?.World == null) return result;
            result.Add(Point(FoundationApi.PipeDrag.ConnectionPoint(pipe.ConnectedOutput)));
            foreach (var node in pipe.Nodes) result.Add(Point(node));
            result.Add(Point(FoundationApi.PipeDrag.ConnectionPoint(pipe.ConnectedInput)));
            return result;
        }
    }
}
