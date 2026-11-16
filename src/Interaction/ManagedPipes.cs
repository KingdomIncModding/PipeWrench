using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using KingdomFoundation.Api;
using KingdomPipeWrench.Routing;

namespace KingdomPipeWrench.Interaction
{
    /// <summary>Only the current loaded kingdom's in-memory markers are eligible for display.</summary>
    internal sealed class ManagedPipes
    {
        private readonly SessionGrabbers<WorkspacePipe> markers = new SessionGrabbers<WorkspacePipe>();
        private object autoSession;
        private ConditionalWeakTable<WorkspacePort, RoutePoint[]> autoNodes = new ConditionalWeakTable<WorkspacePort, RoutePoint[]>();
        private void Sync()
        {
            var kingdom = FoundationApi.Kingdom;
            markers.SetSession(kingdom);
            if (!ReferenceEquals(kingdom, autoSession))
            {
                autoSession = kingdom;
                autoNodes = new ConditionalWeakTable<WorkspacePort, RoutePoint[]>();
            }
        }
        public bool Contains(WorkspacePipe pipe) { Sync(); return markers.Contains(pipe); }
        public void Add(WorkspacePipe pipe) { Sync(); markers.Add(pipe); }
        public bool IsMarkerVisible(WorkspacePipe pipe, int segment, bool automatic = true) { Sync(); return markers.IsVisible(pipe, segment, automatic); }
        public bool ShouldRestoreWholeStraight(WorkspacePipe pipe) { Sync(); return markers.ShouldRestoreWholeStraight(pipe); }
        public void HideMarker(WorkspacePipe pipe, int segment) { Sync(); markers.Hide(pipe, segment); }
        public void RestoreMarker(WorkspacePipe pipe, int segment) { Sync(); markers.Restore(pipe, segment); }
        public void Remap(WorkspacePipe pipe, PipeSectionPlan plan) { Sync(); markers.Remap(pipe, plan.Remap); }

        public void RecordAutoNodes(WorkspacePort output, IEnumerable<RoutePoint> nodes)
        {
            Sync();
            if (output == null) return;
            autoNodes.Remove(output);
            autoNodes.Add(output, nodes.ToArray());
        }

        public void ClearAutoNodes(WorkspacePort output)
        {
            Sync();
            if (output != null) autoNodes.Remove(output);
        }

        public bool MatchesAutoNodes(WorkspacePort output, IReadOnlyList<RoutePoint> current)
        {
            Sync();
            return output != null && current != null && autoNodes.TryGetValue(output, out var recorded) && recorded.SequenceEqual(current);
        }
    }
}
