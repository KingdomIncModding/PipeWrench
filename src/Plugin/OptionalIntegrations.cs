using System;
using BepInEx.Bootstrap;
using KingdomFoundation.Api;
using UnityEngine;
#if HAS_PORTLAYOUTS
using KingdomPortLayouts;
#endif
#if HAS_RIGHTCLICKFIX
using KingdomRightClickFix;
#endif
#if HAS_UNDO
using KingdomIncUndo;
#endif

namespace KingdomPipeWrench.Plugin
{
    internal static class OptionalIntegrations
    {
        const string UndoPluginId = "urakka.kingdominc.undo";
        const string PortLayoutsPluginId = "urakka.kingdominc.portlayouts";
        const string RightClickFixPluginId = "urakka.kingdominc.rightclickfix";

        static bool HasUndo => Chainloader.PluginInfos.ContainsKey(UndoPluginId);
        static bool HasPortLayouts => Chainloader.PluginInfos.ContainsKey(PortLayoutsPluginId);
        static bool HasRightClickFix => Chainloader.PluginInfos.ContainsKey(RightClickFixPluginId);

        internal static Vector2Int ConnectionPoint(WorkspacePort port)
        {
            if (port?.World == null)
                return default;
#if HAS_PORTLAYOUTS
            if (HasPortLayouts && TryGetPortLayoutsEndpoint(port, out var endpoint))
                return endpoint;
#endif
            return FoundationApi.PipeDrag.ConnectionPoint(port);
        }

        internal static int OutwardDirection(WorkspacePort port)
        {
            if (port?.World == null)
                return 0;
#if HAS_PORTLAYOUTS
            if (HasPortLayouts && TryGetPortLayoutsDirection(port, out int direction))
                return direction;
#endif
            return FoundationApi.PipeDrag.OutwardDirection(port);
        }

        internal static Vector2 BuildingSize(WorkspaceObject building)
        {
            if (building == null)
                return Vector2.zero;
#if HAS_PORTLAYOUTS
            if (HasPortLayouts && TryGetPortLayoutsBuildingSize(building, out var size))
                return size;
#endif
            return building.World ? FoundationApi.WorkspaceVisuals.Size(building.World) : building.Size;
        }

        internal static void RunUndoEdit(Action action)
        {
#if HAS_UNDO
            if (HasUndo)
            {
                RunUndoEditCore(action);
                return;
            }
#endif
            action();
        }

        internal static IDisposable OpenUndoEditScope()
        {
#if HAS_UNDO
            if (HasUndo)
                return OpenUndoEditScopeCore();
#endif
            return null;
        }

        internal static IDisposable RegisterRightClickConsumer(string ownerId, Func<bool> predicate)
        {
#if HAS_RIGHTCLICKFIX
            if (HasRightClickFix)
                return RegisterRightClickConsumerCore(ownerId, predicate);
#endif
            return null;
        }

#if HAS_PORTLAYOUTS
        static bool TryGetPortLayoutsEndpoint(WorkspacePort port, out Vector2Int endpoint)
        {
            if (PortLayoutsApi.TryGetPortEndpoint(port, out var raw))
            {
                endpoint = Vector2Int.RoundToInt((Vector2)raw);
                return true;
            }

            endpoint = default;
            return false;
        }

        static bool TryGetPortLayoutsDirection(WorkspacePort port, out int direction)
        {
            if (PortLayoutsApi.TryGetPortEndpoint(port, out var raw))
            {
                float dx = raw.x - port.World.transform.position.x;
                direction = Mathf.Abs(dx) >= 8f ? Math.Sign(dx) : 0;
                return true;
            }

            direction = 0;
            return false;
        }

        static bool TryGetPortLayoutsBuildingSize(WorkspaceObject building, out Vector2 size)
        {
            if (PortLayoutsApi.GetLayout(building) != 0)
            {
                var rotated = PortLayoutsApi.GetBuildingSize(building);
                if (rotated.x > 0 && rotated.y > 0)
                {
                    size = rotated;
                    return true;
                }
            }

            size = Vector2.zero;
            return false;
        }
#endif

#if HAS_UNDO
        static void RunUndoEditCore(Action action) => UndoApi.RunEdit(action);
        static IDisposable OpenUndoEditScopeCore() => UndoApi.OpenEditScope();
#endif

#if HAS_RIGHTCLICKFIX
        static IDisposable RegisterRightClickConsumerCore(string ownerId, Func<bool> predicate) => RightClickFixApi.RegisterConsumer(ownerId, predicate);
#endif
    }
}

