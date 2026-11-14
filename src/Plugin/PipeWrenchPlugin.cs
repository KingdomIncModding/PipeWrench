using BepInEx;
using BepInEx.Configuration;
using KingdomFoundation.Api;
using KingdomPipeWrench.Handles;
using KingdomPipeWrench.Interaction;
using KingdomPipeWrench.Routing;
using KingdomPipeWrench.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KingdomPipeWrench.Plugin
{
    [BepInPlugin(Id, "Pipe Wrench", Version)]
    [BepInDependency("urakka.kingdominc.foundation", "0.14.3")]
    public sealed class PipeWrenchPlugin : BaseUnityPlugin
    {
        public const string Id = "urakka.kingdominc.pipewrench", Version = "0.1.8";
        internal ConfigEntry<bool> Enabled, ShowHandles;
        internal ConfigEntry<Key> RouteKey, IncreaseBendsKey, DecreaseBendsKey;
        internal ConfigEntry<int> Clearance, PreferredBends;
        internal ConfigEntry<AvoidanceMode> Avoidance;
        private PipeToolbar toolbar;
        private int avoidanceKeyFrame = -1;
        internal bool IsPointerOverToolbar => toolbar?.IsPointerOver == true;
        internal bool ShowTools => PipeToolVisibility.Show(Enabled.Value, FoundationApi.UiAccess.IsWorkspaceVisible,
            FoundationApi.UiAccess.IsMenuVisible, routing?.HasActiveConnection == true, editor?.Selected != null, inserter?.IsActive == true);
        internal ManagedPipes Managed;
        private FoundationModContext foundation;
        private PipeRouteController routing;
        private PipeHandleManager handles;
        private ConnectedPipeEditor editor;
        private GrabInserter inserter;

        private void Awake()
        {
            Enabled = Config.Bind("General", "Enabled", true, "Enable Pipe Wrench.");
            ShowHandles = Config.Bind("General", "ShowGrabMarkers", true, "Show sliding handles when hovering a Pipe Wrench pipe.");
            RouteKey = Config.Bind("Input", "CycleRoute", Key.P, "Cycle Center, Near Start, Near End and Freeform while dragging or editing the selected pipe. Alt+P activates Grab Inserter.");
            IncreaseBendsKey = Config.Bind("Input", "IncreaseBends", Key.Equals, "Add two bends while Pipe Wrench is active. Numpad + also works.");
            DecreaseBendsKey = Config.Bind("Input", "DecreaseBends", Key.Minus, "Remove two requested bends while Pipe Wrench is active. Numpad - also works.");
            PreferredBends = Config.Bind("Routing", "PreferredBends", 2, new ConfigDescription("Requested bend count, adjusted in pairs with = and -. Geometry or avoidance may require a different count.", new AcceptableValueRange<int>(2, 12)));
            Clearance = Config.Bind("Routing", "BuildingClearance", 8, new ConfigDescription("Clearance around buildings in world units.", new AcceptableValueRange<int>(0, 12)));
            Avoidance = Config.Bind("Routing", "Avoidance", AvoidanceMode.Buildings, "Shift cycles Buildings, Overlap (crossing allowed), BuildingsAndOverlap (crossing allowed), Strict (also avoids crossings).");
            Managed = new ManagedPipes();
            foundation = FoundationApi.ForMod(Id);
            var settings = foundation.Settings("Pipe Wrench", "Right angles by default. P cycles modes; = / - adjust bends; Shift cycles avoidance rules. Alt+P inserts section sliders; right-click removes them.");
            settings.AddToggle("Enabled", Enabled.Value, value => Enabled.Value = value, getter: () => Enabled.Value);
            settings.AddToggle("Grab markers", ShowHandles.Value, value => ShowHandles.Value = value, getter: () => ShowHandles.Value);
            editor = new ConnectedPipeEditor(this);
            routing = new PipeRouteController(this, editor);
            foundation.Own(FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.PipeDragMove, after: routing.Move));
            foundation.Own(FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.PipeDragEnd, before: routing.BeforeEnd, after: routing.AfterEnd));
            foundation.Own(FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.PipeDragCancel, before: routing.Cancel));
            handles = new PipeHandleManager(this, editor);
            inserter = new GrabInserter(this, handles, editor);
            toolbar = new PipeToolbar(this);
            Logger.LogInfo("Pipe Wrench " + Version + " loaded: default right angles; persistent pipe editing; P cycles modes; Alt+P inserts session-only sliders.");
        }
        private void LateUpdate()
        {
            ReadAvoidanceInput();
            routing?.Tick();
            inserter?.Tick();
            if (editor != null && handles != null) editor.Tick(handles.IsPointerOverMarker() || IsPointerOverToolbar, handles.Active != null);
            handles?.Tick();
            toolbar?.Tick();
        }
        private void OnDestroy()
        {
            toolbar?.Dispose(); handles?.Dispose(); routing?.Dispose(); editor?.Dismiss(); foundation?.Dispose();
        }
        internal void ReadAvoidanceInput()
        {
            var keys = Keyboard.current;
            if (keys == null || avoidanceKeyFrame == Time.frameCount || !Enabled.Value
                || !FoundationApi.UiAccess.IsWorkspaceVisible || !FoundationApi.UiAccess.IsGameInputEnabled
                || FoundationApi.UiAccess.IsMenuVisible || keys.ctrlKey.isPressed || keys.altKey.isPressed) return;
            if (!keys.leftShiftKey.wasPressedThisFrame && !keys.rightShiftKey.wasPressedThisFrame) return;
            avoidanceKeyFrame = Time.frameCount;
            SetAvoidance(AvoidanceRules.Next(Avoidance.Value));
        }
        internal void SetAvoidance(AvoidanceMode mode)
        {
            Avoidance.Value = mode;
            FoundationApi.UiAccess.ShowStatusMessage("Pipe Wrench: " + AvoidanceRules.Label(mode));
        }
        internal void Report(string message) => Logger.LogWarning(message);
    }
}
