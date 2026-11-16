using System;
using System.Collections.Generic;
using System.Linq;
using KingdomFoundation.Api;
using KingdomPipeWrench.Plugin;
using KingdomPipeWrench.Routing;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KingdomPipeWrench.Interaction
{
    /// <summary>Keeps the just-connected pipe selected for mode changes and sliding handles.</summary>
    internal sealed class ConnectedPipeEditor
    {
        private readonly PipeWrenchPlugin plugin;
        private readonly PipeEditState state = new PipeEditState();
        private WorkspacePipe pipe;
        private Component knownWorld;
        private int knownPathHash;
        private List<RoutePoint> fixedNodes = new List<RoutePoint>();
        private bool startsAtInput;
        private AvoidanceMode appliedMode;
        private string appliedSnap;
        public WorkspacePipe Selected => state.Active ? pipe : null;
        public ConnectedPipeEditor(PipeWrenchPlugin plugin) { this.plugin = plugin; }

        public void Select(WorkspacePipe value, RouteShape shape, int bends, IEnumerable<RoutePoint> prefix, bool fromInput)
        {
            pipe = value; fixedNodes = prefix.ToList(); startsAtInput = fromInput;
            state.Begin(Time.frameCount, shape, bends); appliedMode = plugin.Avoidance.Value; appliedSnap = WorkspaceGeometry.NativeSnap().Signature;
            RecordKnownGeometry();
        }
        public void SelectExisting(WorkspacePipe value)
        {
            if (Selected != value) Select(value, RouteShape.Freeform, 2, Array.Empty<RoutePoint>(), false);
            else state.Touch(Time.frameCount);
        }
        public void Touch() { state.Touch(Time.frameCount); appliedMode = plugin.Avoidance.Value; RecordKnownGeometry(); }
        public void MarkManual() { state.MarkManual(); plugin.Managed.ClearAutoNodes(pipe?.ConnectedOutput); RecordKnownGeometry(); }
        public void Dismiss() { state.Dismiss(); pipe = null; knownWorld = null; knownPathHash = 0; fixedNodes.Clear(); }

        public void Tick(bool overGrabber, bool draggingGrabber)
        {
            if (!state.Active) return;
            if (!IsAvailable() || !plugin.Enabled.Value || !FoundationApi.UiAccess.IsWorkspaceVisible
                || !FoundationApi.UiAccess.IsGameInputEnabled || FoundationApi.UiAccess.IsMenuVisible)
            { Dismiss(); return; }
            var mouse = Mouse.current;
            bool clicked = mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame);
            if (state.ShouldDismiss(Time.frameCount, clicked, overGrabber, draggingGrabber)) { Dismiss(); return; }
            if (draggingGrabber || !state.CanReadKeys(Time.frameCount) || FoundationApi.UiAccess.IsDragDropBusy) return;
            if (pipe.World != knownWorld || PathHash(pipe) != knownPathHash)
                SyncFromExternal();
            var snap = WorkspaceGeometry.NativeSnap().Signature;
            if (appliedMode != plugin.Avoidance.Value || appliedSnap != snap)
            { if (state.Shape != RouteShape.Freeform) Apply(); appliedMode = plugin.Avoidance.Value; appliedSnap = snap; }
            ReadKeys();
        }

        private void RecordKnownGeometry()
        {
            knownWorld = pipe?.World;
            knownPathHash = pipe != null ? PathHash(pipe) : 0;
        }

        private static int PathHash(WorkspacePipe value)
        {
            var nodes = value.Nodes;
            int hash = nodes.Count;
            for (int i = 0; i < nodes.Count; i++)
                hash = unchecked(hash * 397 ^ nodes[i].x * 31 ^ nodes[i].y);
            return hash;
        }

        private void SyncFromExternal()
        {
            RecordKnownGeometry();
            fixedNodes.Clear();
            appliedMode = plugin.Avoidance.Value;
            appliedSnap = WorkspaceGeometry.NativeSnap().Signature;
            var full = WorkspaceGeometry.FullPath(pipe);
            int bends = Math.Max(2, RouteBendPlanner.Count(full));
            if (pipe.Nodes.Count == 0)
            {
                state.Sync(RouteShape.Freeform, 2);
                return;
            }
            var start = full[0];
            var end = full[full.Count - 1];
            int from = OptionalIntegrations.OutwardDirection(pipe.ConnectedOutput);
            int to = OptionalIntegrations.OutwardDirection(pipe.ConnectedInput);
            var snap = WorkspaceGeometry.NativeSnap();
            foreach (var candidate in new[] { RouteShape.Center, RouteShape.NearStart, RouteShape.NearEnd })
            {
                var basic = RouteGeometry.Basic(start, end, from, to, candidate, snap);
                if (basic.SequenceEqual(full))
                {
                    state.Sync(candidate, bends);
                    return;
                }
            }
            state.Sync(RouteShape.Freeform, bends);
        }

        private bool IsAvailable()
            => pipe?.World != null && pipe.World.gameObject.activeInHierarchy && pipe.ConnectedInput?.World != null
            && pipe.ConnectedOutput?.World != null && pipe.ConnectedOutput.Parent?.ParentWorkspace == FoundationApi.Kingdom?.ActiveWorkspace;

        private void ReadKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || keyboard.ctrlKey.isPressed || keyboard.altKey.isPressed) return;
            bool increase = Pressed(keyboard, plugin.IncreaseBendsKey.Value) || keyboard.numpadPlusKey.wasPressedThisFrame;
            bool decrease = Pressed(keyboard, plugin.DecreaseBendsKey.Value) || keyboard.numpadMinusKey.wasPressedThisFrame;
            if (Pressed(keyboard, plugin.RouteKey.Value)) state.Cycle();
            else if (increase != decrease && state.Shape != RouteShape.Freeform)
                state.Adjust(increase ? 1 : -1);
            else return;
            Apply();
        }
        private static bool Pressed(Keyboard keyboard, Key key) => key != Key.None && keyboard[key].wasPressedThisFrame;

        private void Apply()
        {
            var startPort = startsAtInput ? pipe.ConnectedInput : pipe.ConnectedOutput;
            var endPort = startsAtInput ? pipe.ConnectedOutput : pipe.ConnectedInput;
            var start = fixedNodes.Count > 0 ? fixedNodes[fixedNodes.Count - 1] : WorkspaceGeometry.Point(OptionalIntegrations.ConnectionPoint(startPort));
            var end = WorkspaceGeometry.Point(OptionalIntegrations.ConnectionPoint(endPort));
            var nodes = new List<RoutePoint>(fixedNodes);
            int count = 0;
            if (state.Shape != RouteShape.Freeform)
            {
                int from = fixedNodes.Count == 0 ? OptionalIntegrations.OutwardDirection(startPort) : 0;
                int to = OptionalIntegrations.OutwardDirection(endPort);
                var terminalStart = WorkspaceGeometry.Point(OptionalIntegrations.ConnectionPoint(startPort));
                var constraints = WorkspaceGeometry.Constraints(pipe, plugin.Avoidance.Value, plugin.Clearance.Value, terminalStart, end);
                var route = ObstacleRouter.Find(start, end, from, to, state.Shape, constraints);
                if (route == null) { FoundationApi.UiAccess.ShowPointerWarning("Pipe Wrench: no clear route. The current pipe was kept."); return; }
                route = RouteBendPlanner.Plan(route, state.Bends, constraints); count = RouteBendPlanner.Count(route);
                nodes.AddRange(route.Skip(1).Take(Math.Max(0, route.Count - 2)));
                var full = new List<RoutePoint> { terminalStart }; full.AddRange(nodes); full.Add(end);
                if (plugin.Avoidance.Value != AvoidanceMode.None && !constraints.IsClear(full))
                { FoundationApi.UiAccess.ShowPointerWarning("Pipe Wrench: a fixed waypoint conflicts with this rule. The current pipe was kept."); return; }
            }
            var values = nodes.Select(WorkspaceGeometry.Vector).ToList();
            if (startsAtInput) values.Reverse();
            OptionalIntegrations.RunUndoEdit(() =>
            {
                pipe.SetNodes(values);
                FoundationApi.WorkspaceVisuals.RefreshPipe(pipe.World);
            });
            plugin.Managed.Add(pipe);
            if (state.Shape != RouteShape.Freeform && fixedNodes.Count == 0)
                plugin.Managed.RecordAutoNodes(pipe.ConnectedOutput, values.Select(WorkspaceGeometry.Point));
            else
                plugin.Managed.ClearAutoNodes(pipe.ConnectedOutput);
            RecordKnownGeometry();
            FoundationApi.UiAccess.ShowStatusMessage("Pipe Wrench: " + RouteModes.Label(state.Shape)
                + (state.Shape == RouteShape.Freeform ? "" : " — " + count + " bends") + "; P cycles modes; click away to finish");
        }
    }
}
