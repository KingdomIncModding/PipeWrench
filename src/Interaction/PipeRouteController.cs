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
    internal sealed class PipeRouteController : IDisposable
    {
        private readonly PipeWrenchPlugin plugin;
        private readonly ConnectedPipeEditor editor;
        private WorkspacePipeDrag drag;
        private WorkspacePipe pipe;
        private List<RoutePoint> fixedNodes = new List<RoutePoint>();
        private RouteShape shape;
        private bool blocked;
        private int lastKeyFrame = -1;
        private int requestedBends = 2, actualBends;
        private bool announce;
        private bool startsAtInput;
        private Color nativeStart, nativeEnd;
        private RouteConstraints constraints;
        private AvoidanceMode constraintMode;
        private string cacheKey;
        private List<RoutePoint> cachedRoute;
        private List<RoutePoint> previewNodes;
        private Vector2Int previewEndpoint;
        public bool IsRouting => drag != null && drag.IsActive && shape != RouteShape.Freeform;
        public bool HasActiveConnection => drag != null && drag.IsActive;
        public PipeRouteController(PipeWrenchPlugin plugin, ConnectedPipeEditor editor) { this.plugin = plugin; this.editor = editor; }

        public void Move(GameHookCall call)
        {
            if (!call.OriginalRan || !plugin.Enabled.Value) return;
            Capture(FoundationApi.PipeDrag.FromView(call.View));
            if (drag == null) return;
            ReadHotkey();
            Apply(false);
            if (announce)
            {
                announce = false;
                FoundationApi.UiAccess.ShowStatusMessage(shape == RouteShape.Freeform ? "Pipe Wrench: Freeform"
                    : "Pipe Wrench: " + RouteModes.Label(shape) + " — " + actualBends + " bends" + (actualBends != requestedBends ? " (requested " + requestedBends + ")" : "")
                    + " — = / - adjust; Shift cycles avoidance");
            }
        }

        private void Capture(WorkspacePipeDrag current)
        {
            if (current == null || current.Pipe == pipe) return;
            Reset(); editor.Dismiss(); drag = current; pipe = current.Pipe; shape = RouteShape.Center;
            startsAtInput = drag.Start.IsInput;
            requestedBends = RouteBendPlanner.Normalize(plugin.PreferredBends.Value);
            fixedNodes = pipe.Nodes.Select(WorkspaceGeometry.Point).ToList();
            if (drag.Start.IsInput) fixedNodes.Reverse();
            var line = FoundationApi.WorkspaceVisuals.PipeLine(pipe.World);
            nativeStart = line.startColor; nativeEnd = line.endColor;
        }

        private void ReadHotkey()
        {
            plugin.ReadAvoidanceInput();
            var keyboard = Keyboard.current;
            if (keyboard == null || lastKeyFrame == Time.frameCount || keyboard.ctrlKey.isPressed || keyboard.altKey.isPressed
                || !FoundationApi.UiAccess.IsGameInputEnabled || FoundationApi.UiAccess.IsMenuVisible) return;
            bool toggle = Pressed(keyboard, plugin.RouteKey.Value);
            bool increase = Pressed(keyboard, plugin.IncreaseBendsKey.Value) || keyboard.numpadPlusKey.wasPressedThisFrame;
            bool decrease = Pressed(keyboard, plugin.DecreaseBendsKey.Value) || keyboard.numpadMinusKey.wasPressedThisFrame;
            if (toggle) shape = RouteModes.Next(shape);
            else if (shape != RouteShape.Freeform && increase != decrease)
            {
                requestedBends = RouteBendPlanner.Normalize(requestedBends + (increase ? 2 : -2));
                plugin.PreferredBends.Value = requestedBends;
            }
            else return;
            lastKeyFrame = Time.frameCount; cacheKey = null; announce = true;
        }

        private static bool Pressed(Keyboard keyboard, Key key) => key != Key.None && keyboard[key].wasPressedThisFrame;

        private void Apply(bool committing)
        {
            if (drag == null || !drag.IsAlive) return;
            if (plugin.IsPointerOverToolbar)
            { if (previewNodes != null) { WriteNodes(previewNodes, previewEndpoint); Tint(blocked); } return; }
            var target = drag.HoveredPort;
            var endpoint = target != null ? OptionalIntegrations.ConnectionPoint(target) : FoundationApi.UiAccess.MouseWorld;
            if (target == null && shape != RouteShape.Freeform && Keyboard.current?.ctrlKey.isPressed == true)
                endpoint = FoundationApi.NativeIO.SnapRouteNode(endpoint);
            if (shape == RouteShape.Freeform)
            {
                WriteNodes(fixedNodes, endpoint); Tint(false); blocked = false; return;
            }
            var start = fixedNodes.Count == 0 ? WorkspaceGeometry.Point(OptionalIntegrations.ConnectionPoint(drag.Start)) : fixedNodes[fixedNodes.Count - 1];
            var end = WorkspaceGeometry.Point(endpoint);
            int startDirection = fixedNodes.Count == 0 ? OptionalIntegrations.OutwardDirection(drag.Start) : 0;
            int endDirection = target != null ? OptionalIntegrations.OutwardDirection(target) : 0;
            var mode = plugin.Avoidance.Value;
            var terminalStart = WorkspaceGeometry.Point(OptionalIntegrations.ConnectionPoint(drag.Start));
            if (constraints == null || constraintMode != mode || committing)
            { constraints = WorkspaceGeometry.Constraints(pipe, mode, plugin.Clearance.Value, terminalStart, end); constraintMode = mode; cacheKey = null; }
            else if (constraints.End != end)
                constraints = new RouteConstraints(constraints.Buildings, constraints.Pipes, constraints.AvoidCrossings, terminalStart, end, constraints.Snap);
            string key = start + "/" + end + "/" + startDirection + "/" + endDirection + "/" + shape + "/" + requestedBends + "/" + mode + "/" + constraints.Snap.Signature;
            if (key != cacheKey)
            {
                cachedRoute = ObstacleRouter.Find(start, end, startDirection, endDirection, shape, constraints);
                if (cachedRoute != null)
                    cachedRoute = RouteBendPlanner.Plan(cachedRoute, requestedBends, constraints);
                cacheKey = key;
            }
            blocked = cachedRoute == null;
            var route = cachedRoute ?? RouteGeometry.Basic(start, end, startDirection, endDirection, shape, constraints.Snap);
            actualBends = RouteBendPlanner.Count(route);
            var nodes = new List<RoutePoint>(fixedNodes);
            for (int i = 1; i < route.Count - 1; i++) nodes.Add(route[i]);
            // Only the remaining section is routed; hand-placed waypoints retain their positions.
            if (!blocked && fixedNodes.Count > 0)
            {
                var full = new List<RoutePoint> { terminalStart }; full.AddRange(nodes); full.Add(end);
                blocked = !constraints.IsClear(full);
            }
            WriteNodes(nodes, endpoint); Tint(blocked);
        }

        private void WriteNodes(IEnumerable<RoutePoint> nodes, Vector2Int endpoint)
        {
            previewNodes = nodes.ToList(); previewEndpoint = endpoint;
            var values = previewNodes.Select(WorkspaceGeometry.Vector).ToList();
            if (drag.Start.IsInput) values.Reverse();
            drag.SetPreview(values, endpoint);
        }
        private void Tint(bool failed)
        {
            if (pipe?.World == null) return;
            var line = FoundationApi.WorkspaceVisuals.PipeLine(pipe.World);
            line.startColor = failed ? Color.red : nativeStart;
            line.endColor = failed ? Color.red : nativeEnd;
        }

        public void BeforeEnd(GameHookCall call)
        {
            if (!plugin.Enabled.Value) return;
            Capture(FoundationApi.PipeDrag.FromView(call.View));
            if (drag == null) return;
            if (plugin.IsPointerOverToolbar) { call.Cancel(); drag.Continue(); return; }
            if (shape == RouteShape.Freeform) return;
            Apply(true);
            if (blocked && (drag.HoveredPort != null || Keyboard.current?.ctrlKey.isPressed == true))
            {
                call.Cancel(); drag.Continue();
                FoundationApi.UiAccess.ShowPointerWarning("Pipe Wrench: no clear route. Move the endpoint or change the avoidance rule.");
            }
        }

        public void AfterEnd(GameHookCall call)
        {
            if (drag == null || !call.OriginalRan) return;
            if (drag.IsActive)
            {
                // Native Ctrl-click commits a waypoint and continues the same preview.
                fixedNodes = pipe.Nodes.Select(WorkspaceGeometry.Point).ToList();
                if (drag.Start.IsInput) fixedNodes.Reverse();
                cacheKey = null; return;
            }
            if (pipe.ConnectedInput != null && pipe.ConnectedOutput != null)
            {
                plugin.Managed.Add(pipe);
                editor.Select(pipe, shape, requestedBends, fixedNodes, startsAtInput);
            }
            Reset();
        }

        public void Tick()
        {
            if (plugin.Enabled.Value) Capture(FoundationApi.PipeDrag.Active);
            if (drag != null && (!drag.IsActive || !plugin.Enabled.Value))
            {
                if (drag.IsActive) { WriteNodes(fixedNodes, FoundationApi.UiAccess.MouseWorld); Tint(false); }
                Reset();
            }
            if (drag != null) { ReadHotkey(); Apply(false); }
        }
        public void Cancel(GameHookCall call) => Reset();
        private void Reset()
        {
            Tint(false); drag = null; pipe = null; shape = RouteShape.Freeform;
            fixedNodes.Clear(); constraints = null; cacheKey = null; cachedRoute = null; previewNodes = null; blocked = false;
            announce = false; actualBends = 0;
        }
        public void Dispose()
        {
            if (drag != null && drag.IsActive) WriteNodes(fixedNodes, FoundationApi.UiAccess.MouseWorld);
            Reset();
        }
    }
}
