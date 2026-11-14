using KingdomFoundation.Api;
using KingdomPipeWrench.Interaction;
using KingdomPipeWrench.Plugin;
using UnityEngine.InputSystem;

namespace KingdomPipeWrench.Handles
{
    internal sealed class GrabInserter
    {
        private readonly PipeWrenchPlugin plugin;
        private readonly PipeHandleManager handles;
        private readonly ConnectedPipeEditor editor;
        private bool active;
        public bool IsActive => active;
        public GrabInserter(PipeWrenchPlugin plugin, PipeHandleManager handles, ConnectedPipeEditor editor)
        { this.plugin = plugin; this.handles = handles; this.editor = editor; }

        public void Tick()
        {
            if (!plugin.Enabled.Value || !plugin.ShowHandles.Value || !FoundationApi.UiAccess.IsWorkspaceVisible
                || !FoundationApi.UiAccess.IsGameInputEnabled || FoundationApi.UiAccess.IsMenuVisible || FoundationApi.UiAccess.IsDragDropBusy)
            { active = false; return; }
            var keyboard = Keyboard.current; var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;
            if (keyboard.altKey.isPressed && keyboard.pKey.wasPressedThisFrame && !keyboard.ctrlKey.isPressed)
            {
                active = !active;
                if (active) editor.Dismiss();
                FoundationApi.UiAccess.ShowStatusMessage(active ? "Grab Inserter: click a pipe section to add a center slider. Alt+P or right-click cancels." : "Grab Inserter: off");
            }
            if (!active) return;
            if (mouse.rightButton.wasPressedThisFrame) { active = false; return; }
            if (!mouse.leftButton.wasPressedThisFrame || keyboard.ctrlKey.isPressed || plugin.IsPointerOverToolbar || FoundationApi.UiAccess.IsPointerOverPort) return;
            var pipe = handles.NearestSection(out int segment);
            if (pipe == null)
            { FoundationApi.UiAccess.ShowPointerWarning("Grab Inserter: click directly on a pipe section."); return; }
            plugin.Managed.RestoreMarker(pipe, segment);
            handles.Invalidate();
            editor.SelectExisting(pipe);
            active = false;
            FoundationApi.UiAccess.ShowStatusMessage("Section slider added. Drag it to shape this section; right-click a slider to remove it.");
        }
    }
}
