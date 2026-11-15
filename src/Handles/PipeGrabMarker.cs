using System;
using System.Collections.Generic;
using System.Linq;
using KingdomFoundation.Api;
using KingdomPipeWrench.Interaction;
using KingdomPipeWrench.Plugin;
using KingdomPipeWrench.Routing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace KingdomPipeWrench.Handles
{
    public sealed class PipeGrabMarker : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerClickHandler, IPointerDownHandler,
        IPointerEnterHandler, IPointerExitHandler, IGameDragDropTarget
    {
        private PipeHandleManager manager;
        private WorkspacePipe pipe;
        private List<RoutePoint> original;
        private PipeSectionPlan plan;
        private bool moved;
        private bool dragging;
        private bool removed;
        private IDisposable undoScope;
        internal int Segment { get; private set; }
        internal void Initialize(PipeHandleManager manager, WorkspacePipe pipe, int segment)
        { this.manager = manager; this.pipe = pipe; Segment = segment; }

        public void OnBeginDrag(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) Begin(true); }
        public void OnDrag(PointerEventData data) { }
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Right) RemoveMarker();
            else if (data.button == PointerEventData.InputButton.Left && !dragging) Begin(false);
        }
        public void OnPointerDown(PointerEventData data)
        { if (data.button == PointerEventData.InputButton.Right) RemoveMarker(); }
        private void RemoveMarker()
        { if (!removed) { removed = true; manager.Remove(this, pipe); } }

        private void Begin(bool hold)
        {
            if (dragging || pipe?.World == null || FoundationApi.UiAccess.IsDragDropBusy) return;
            original = pipe.Nodes.Select(WorkspaceGeometry.Point).ToList();
            plan = PipeSectionPlan.Create(WorkspaceGeometry.FullPath(pipe), Segment, WorkspaceGeometry.NativeSnap());
            if (plan == null) { FoundationApi.UiAccess.ShowPointerWarning("Pipe Wrench: this section has no room for corners on the current snap grid."); return; }
            moved = false;
            dragging = true; manager.Active = this;
            undoScope = OptionalIntegrations.OpenUndoEditScope();
            if (!FoundationApi.DragDrop.SetTarget(this, hold))
            {
                undoScope?.Dispose(); undoScope = null;
                dragging = false; manager.Active = null; return;
            }
            manager.BeginEdit(pipe);
            FoundationApi.UiAccess.ShowStatusMessage("Pipe Wrench: slide " + (plan.Vertical ? "left / right" : "up / down") + " — Escape cancels; right-click removes the slider");
        }

        public Vector2Int GetGrabOffset(Vector2Int mousePosition)
            => mousePosition - Vector2Int.RoundToInt((Vector2)transform.position);

        public void OnDragDropMove(Vector2Int mousePosition, Vector2Int grabOffset)
        {
            if (!dragging || pipe?.World == null) return;
            int raw = plan.Vertical ? mousePosition.x - grabOffset.x : mousePosition.y - grabOffset.y;
            // Keep a click motionless; once moved, use the same live snap as vanilla nodes and Snappy.
            var snapped = FoundationApi.NativeIO.SnapRouteNode(plan.Vertical ? new Vector2Int(raw, 0) : new Vector2Int(0, raw));
            int value = raw == plan.Coordinate || Keyboard.current?.ctrlKey.isPressed == true ? raw : plan.Vertical ? snapped.x : snapped.y;
            if (value == plan.Coordinate)
            {
                if (moved) Write(original);
                moved = false; return;
            }
            var full = plan.Move(value);
            manager.Plugin.ReadAvoidanceInput();
            Write(full.Skip(1).Take(full.Count - 2)); moved = true;
        }

        private void Write(IEnumerable<RoutePoint> nodes)
        {
            pipe.SetNodes(nodes.Select(WorkspaceGeometry.Vector));
            FoundationApi.WorkspaceVisuals.RefreshPipe(pipe.World);
        }

        public void OnDragDropEnd()
        {
            if (!dragging) return;
            if (moved && pipe?.World != null)
            { manager.Plugin.Managed.Remap(pipe, plan); Segment = plan.Segment; }
            undoScope?.Dispose(); undoScope = null;
            dragging = false; manager.Finished();
        }

        public void OnDragDropCancel()
        {
            if (!dragging) return;
            if (pipe?.World != null)
            {
                pipe.SetNodes(original.Select(WorkspaceGeometry.Vector)); FoundationApi.WorkspaceVisuals.RefreshPipe(pipe.World);
            }
            undoScope?.Dispose(); undoScope = null;
            if (ReferenceEquals(FoundationApi.DragDrop.Target, this)) FoundationApi.DragDrop.Clear();
            dragging = false; manager.Finished();
        }

        private void OnDestroy()
        {
            undoScope?.Dispose();
            undoScope = null;
        }
        public void OnPointerEnter(PointerEventData data) => GetComponent<Image>().color = new Color(0.75f, 0.52f, 0.15f, 1);
        public void OnPointerExit(PointerEventData data) => GetComponent<Image>().color = new Color(0.24f, 0.19f, 0.09f, 0.95f);
        internal void UpdatePosition(IReadOnlyList<RoutePoint> path)
        {
            int index = dragging && moved ? plan.Segment : Segment;
            if (path.Count < 2 || index + 1 >= path.Count) return;
            Vector2 a = WorkspaceGeometry.Vector(path[index < 0 ? 0 : index]);
            Vector2 b = WorkspaceGeometry.Vector(path[index < 0 ? path.Count - 1 : index + 1]);
            transform.position = new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, 0);
        }
    }
}
