using System;
using System.Collections.Generic;
using KingdomFoundation.Api;
using KingdomPipeWrench.Interaction;
using KingdomPipeWrench.Plugin;
using KingdomPipeWrench.Routing;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace KingdomPipeWrench.Handles
{
    internal sealed class PipeHandleManager : IDisposable
    {
        private readonly PipeWrenchPlugin plugin;
        private readonly ConnectedPipeEditor editor;
        private readonly List<PipeGrabMarker> markers = new List<PipeGrabMarker>();
        private WorkspacePipe shown;
        private Component shownWorld;
        private int nodeCount, pathHash;
        private float nextSearch;
        private bool claimingRightClickUntilRelease;
        internal PipeGrabMarker Active;
        internal PipeWrenchPlugin Plugin => plugin;
        public PipeHandleManager(PipeWrenchPlugin plugin, ConnectedPipeEditor editor) { this.plugin = plugin; this.editor = editor; }

        internal bool IsPointerOverMarker()
        {
            if (Mouse.current == null
                || !Mouse.current.leftButton.wasPressedThisFrame && !Mouse.current.rightButton.wasPressedThisFrame) return false;
            return IsPointerOverActiveMarker();
        }

        internal bool IsPointerOverActiveMarker()
        {
            if (EventSystem.current == null || Mouse.current == null || markers.Count == 0) return false;
            var hits = new List<RaycastResult>();
            var pointer = new PointerEventData(EventSystem.current) { position = Mouse.current.position.ReadValue() };
            EventSystem.current.RaycastAll(pointer, hits);
            foreach (var hit in hits)
            {
                var marker = hit.gameObject ? hit.gameObject.GetComponentInParent<PipeGrabMarker>() : null;
                if (marker && markers.Contains(marker)) return true;
            }
            return false;
        }

        internal bool ShouldClaimRightClick() => claimingRightClickUntilRelease || IsPointerOverActiveMarker();

        public void Tick()
        {
            if (claimingRightClickUntilRelease && (Mouse.current == null || (!Mouse.current.rightButton.isPressed && !Mouse.current.rightButton.wasReleasedThisFrame)))
                claimingRightClickUntilRelease = false;
            if (!plugin.Enabled.Value || !plugin.ShowHandles.Value || !FoundationApi.UiAccess.IsWorkspaceVisible
                || !FoundationApi.UiAccess.IsGameInputEnabled || FoundationApi.UiAccess.IsMenuVisible)
            { Clear(); return; }
            if (Active != null) { UpdatePositions(); return; }
            if (FoundationApi.UiAccess.IsDragDropBusy) { Clear(); return; }
            if (editor.Selected != null)
            {
                var selected = editor.Selected;
                if (NeedsRefresh(selected)) Show(selected);
                UpdatePositions(); return;
            }
            if (Time.unscaledTime >= nextSearch)
            {
                nextSearch = Time.unscaledTime + 0.06f;
                if (shown != null && IsPointerOverActiveMarker())
                {
                    if (shown.World == null) Clear();
                    else if (NeedsRefresh(shown)) Show(shown);
                }
                else
                {
                    var pipe = NearestPipe(false, out _);
                    if (pipe == null) { if (shown != null) Clear(); }
                    else if (NeedsRefresh(pipe)) Show(pipe);
                }
            }
            UpdatePositions();
        }

        private WorkspacePipe NearestPipe(bool inserting, out int markerKey)
        {
            markerKey = -1;
            var workspace = FoundationApi.Kingdom?.ActiveWorkspace;
            if (workspace == null || FoundationApi.UiAccess.IsPointerOverNonWorkspaceUi) return null;
            var mouse = WorkspaceGeometry.Point(FoundationApi.UiAccess.MouseWorld);
            double nearest = 22 * 22;
            WorkspacePipe result = null;
            foreach (var pipe in workspace.Pipes)
            {
                if (pipe.World == null || !pipe.World.gameObject.activeInHierarchy || !inserting && !plugin.Managed.Contains(pipe)) continue;
                var path = WorkspaceGeometry.FullPath(pipe);
                int segment = PipeSectionPicker.Nearest(path, mouse, nearest, out double distance);
                if (segment < 0) continue;
                nearest = distance; result = pipe;
                markerKey = RouteGeometry.IsStraight(path) ? -1 : segment;
            }
            return result;
        }
        internal WorkspacePipe NearestSection(out int markerKey) => NearestPipe(true, out markerKey);
        internal void Invalidate() => nodeCount = -1;

        private bool NeedsRefresh(WorkspacePipe pipe)
        {
            if (pipe != shown || pipe.World != shownWorld || pipe.Nodes.Count != nodeCount || PathHash(pipe) != pathHash) return true;
            for (int i = 0; i < markers.Count; i++) if (!markers[i]) return true;
            return false;
        }

        private static int PathHash(WorkspacePipe pipe)
        {
            var nodes = pipe.Nodes;
            int hash = nodes.Count;
            for (int i = 0; i < nodes.Count; i++)
                hash = unchecked(hash * 397 ^ nodes[i].x * 31 ^ nodes[i].y);
            return hash;
        }

        private void Show(WorkspacePipe pipe)
        {
            Clear(); shown = pipe;
            if (pipe?.World == null) return;
            pipe.World.transform.SetAsLastSibling();
            shownWorld = pipe.World;
            var nodes = pipe.Nodes; nodeCount = nodes.Count; pathHash = PathHash(pipe);
            var full = WorkspaceGeometry.FullPath(pipe);
            bool straight = RouteGeometry.IsStraight(full);
            if (straight)
            {
                if (!plugin.Managed.IsMarkerVisible(pipe, -1) && plugin.Managed.ShouldRestoreWholeStraight(pipe))
                    plugin.Managed.RestoreMarker(pipe, -1);
                if (plugin.Managed.IsMarkerVisible(pipe, -1)) CreateMarker(pipe, -1);
            }
            else
                for (int i = 0; i + 1 < full.Count; i++)
                {
                    bool internalRun = !straight && i > 0 && i + 2 < full.Count;
                    if (full[i] != full[i + 1] && plugin.Managed.IsMarkerVisible(pipe, i, internalRun)) CreateMarker(pipe, i);
                }
            UpdatePositions();
        }

        private void CreateMarker(WorkspacePipe pipe, int segment)
        {
            var obj = new GameObject("PipeWrenchGrabMarker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(PipeGrabMarker));
            obj.transform.SetParent(pipe.World.transform, false);
            var rect = (RectTransform)obj.transform; rect.sizeDelta = new Vector2(18, 18);
            var image = obj.GetComponent<Image>(); image.color = new Color(0.24f, 0.19f, 0.09f, 0.95f); image.raycastTarget = true;
            var dot = new GameObject("Grip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dot.transform.SetParent(obj.transform, false);
            var gripRect = (RectTransform)dot.transform;
            gripRect.sizeDelta = new Vector2(8, 8); gripRect.localRotation = Quaternion.Euler(0, 0, 45);
            var grip = dot.GetComponent<Image>(); grip.color = new Color(0.3f, 0.95f, 1, 1); grip.raycastTarget = false;
            var marker = obj.GetComponent<PipeGrabMarker>(); marker.Initialize(this, pipe, segment); markers.Add(marker);
        }

        private void UpdatePositions()
        {
            if (shown?.World == null) { Clear(); return; }
            var path = WorkspaceGeometry.FullPath(shown);
            foreach (var marker in markers)
                if (marker)
                {
                    marker.UpdatePosition(path);
                    marker.transform.SetAsLastSibling();
                }
        }

        private void Clear()
        {
            if (Active != null) Active.OnDragDropCancel();
            foreach (var marker in markers) if (marker) UnityEngine.Object.Destroy(marker.gameObject);
            markers.Clear(); shown = null; shownWorld = null; nodeCount = 0; pathHash = 0;
        }
        internal void BeginEdit(WorkspacePipe pipe) => editor.SelectExisting(pipe);
        internal void NotifyManualEdit(WorkspacePipe pipe) { editor.MarkManual(); plugin.Managed.ClearAutoNodes(pipe?.ConnectedOutput); }
        internal void Remove(PipeGrabMarker marker, WorkspacePipe pipe)
        {
            claimingRightClickUntilRelease = true;
            marker.OnDragDropCancel();
            plugin.Managed.HideMarker(pipe, marker.Segment);
            markers.Remove(marker); marker.gameObject.SetActive(false); UnityEngine.Object.Destroy(marker.gameObject);
            editor.Touch();
        }
        internal void Finished() { Active = null; nodeCount = -1; nextSearch = 0; editor.Touch(); }
        public void Dispose() => Clear();
    }
}
