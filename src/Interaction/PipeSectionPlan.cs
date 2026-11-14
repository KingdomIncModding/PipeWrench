using System;
using System.Collections.Generic;
using System.Linq;
using KingdomPipeWrench.Routing;

namespace KingdomPipeWrench.Interaction
{
    /// <summary>Slides one selected run, preserving both ports and unrelated native pipe corners.</summary>
    internal sealed class PipeSectionPlan
    {
        private readonly List<RoutePoint> prepared;
        private readonly int originalSegment, inserted, affectedStart, affectedCount;
        private readonly bool whole;
        public int Segment { get; }
        public bool Vertical { get; }
        public int Coordinate => Vertical ? prepared[Segment].X : prepared[Segment].Y;

        private PipeSectionPlan(List<RoutePoint> prepared, int segment, int originalSegment, int inserted,
            int affectedStart, int affectedCount, bool whole)
        {
            this.prepared = prepared; Segment = segment; this.originalSegment = originalSegment;
            this.inserted = inserted; this.affectedStart = affectedStart; this.affectedCount = affectedCount; this.whole = whole;
            Vertical = prepared[segment].X == prepared[segment + 1].X;
        }

        public static PipeSectionPlan Create(IReadOnlyList<RoutePoint> path, int selected, RouteSnap snap = null)
        {
            if (path.Count < 2) return null;
            bool whole = selected == -1;
            if (whole && !RouteGeometry.IsStraight(path)) return null;
            int index = whole ? 0 : selected;
            if (index < 0 || index + 1 >= path.Count) return null;
            var a = path[index]; var b = whole ? path[path.Count - 1] : path[index + 1];
            if (a == b) return null;
            if (!whole && CanMoveCorners(path, index))
                return new PipeSectionPlan(path.ToList(), index, selected, 0, index - 1, 4, false);

            int midpoint = a.X + (b.X - a.X) / 2;
            if (snap != null) midpoint = snap.X(midpoint);
            var nodes = a.Y == b.Y ? RouteGeometry.StraightSlideNodes(a, b, snap)
                : new List<RoutePoint> { new RoutePoint(midpoint, a.Y), new RoutePoint(midpoint, b.Y) };
            if (nodes == null) return null;
            int middle = index + (a.Y == b.Y ? 2 : 1);
            var prepared = whole ? new List<RoutePoint> { a, b } : path.ToList();
            prepared.InsertRange(index + 1, nodes);
            return new PipeSectionPlan(prepared, middle, selected, nodes.Count, index, nodes.Count + 2, whole);
        }

        private static bool CanMoveCorners(IReadOnlyList<RoutePoint> path, int index)
        {
            if (index == 0 || index + 2 >= path.Count) return false;
            var prev = path[index - 1]; var a = path[index]; var b = path[index + 1]; var next = path[index + 2];
            return a.X == b.X ? prev.Y == a.Y && b.Y == next.Y
                : a.Y == b.Y && prev.X == a.X && b.X == next.X;
        }

        public List<RoutePoint> Move(int coordinate) => RouteGeometry.Slide(prepared, Segment, coordinate);
        public List<RoutePoint> Affected(IReadOnlyList<RoutePoint> path) => path.Skip(affectedStart).Take(affectedCount).ToList();
        public int? Remap(int previous)
        {
            if (previous == originalSegment) return Segment;
            if (whole) return null;
            if (previous < 0) return previous;
            return previous > originalSegment ? previous + inserted : previous;
        }
    }
}
