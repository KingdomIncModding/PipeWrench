using KingdomFoundation.Api;

namespace KingdomPipeWrench.Interaction
{
    /// <summary>Only the current loaded kingdom's in-memory markers are eligible for display.</summary>
    internal sealed class ManagedPipes
    {
        private readonly SessionGrabbers<WorkspacePipe> markers = new SessionGrabbers<WorkspacePipe>();
        private void Sync() => markers.SetSession(FoundationApi.Kingdom);
        public bool Contains(WorkspacePipe pipe) { Sync(); return markers.Contains(pipe); }
        public void Add(WorkspacePipe pipe) { Sync(); markers.Add(pipe); }
        public bool IsMarkerVisible(WorkspacePipe pipe, int segment, bool automatic = true) { Sync(); return markers.IsVisible(pipe, segment, automatic); }
        public void HideMarker(WorkspacePipe pipe, int segment) { Sync(); markers.Hide(pipe, segment); }
        public void RestoreMarker(WorkspacePipe pipe, int segment) { Sync(); markers.Restore(pipe, segment); }
        public void Remap(WorkspacePipe pipe, PipeSectionPlan plan) { Sync(); markers.Remap(pipe, plan.Remap); }
    }
}
