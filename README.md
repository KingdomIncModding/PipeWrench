# Pipe Wrench

Draw tidy right-angle pipes and slide their straight sections with grab markers.

## How to use

1. **Start a pipe connection from a port.** Pipe Wrench immediately draws a centered right-angle preview and opens its toolbar at the top left, beneath the mod bar.
2. **Choose your route.** Press **P** to cycle Center, Near Start, Near End and Freeform. Press **= / −** to request more or fewer bends. Tap **Shift**, or click a toolbar button, to choose what the route should avoid.
3. **Connect to the destination port.** The tools stay open after connecting. Drag a grab marker sideways to slide its section; press **P** to try another route shape on the selected pipe. Click empty space to finish editing.
4. **Add a slider to an existing pipe.** Press **Alt+P**, then click the section you want to move. It can be a straight section of a bent vanilla pipe. A slider appears at that section's center without changing the pipe until you drag it.
5. **Remove a slider with right-click.** This removes only the marker. Sliders last for the current loaded session and are never saved; the pipe shapes you create are saved normally.

The four optional avoidance constraints are **Buildings** (avoid building bodies and their visible labels), **Overlap** (avoid shared pipe runs, allow crossings), **Both** (avoid buildings, their labels and shared runs, allow crossings), and **Strict** (avoid buildings, their labels, shared runs and crossings). By default, no avoidance constraints are active so connections stay at a clean two-elbow right-angle route (`output -> straight -> bend -> straight -> bend -> straight -> input`, or straight when aligned). Tap **Shift** or click a toolbar button to turn on constraints when you need multi-bend detours around obstacles; click an active button again (or tap Shift past Strict) to return to no constraints.

Routing, added bends and slider movement follow the game's active pipe snap grid. With **Snappy** installed and its research unlocked, Pipe Wrench automatically uses its finer grid. Before that unlock, or without Snappy, it uses the normal game grid. Existing ports and manual waypoints keep their exact positions. Hold **Ctrl** while moving a slider to bypass snapping, like the game's normal pipe-corner editor.

## Controls

- Start a pipe connection: Pipe Wrench defaults to no constraints and a centered two-elbow right-angle connection (`output -> straight -> bend -> straight -> bend -> straight -> input`, or straight when aligned), and immediately shows its toolbar beneath the mod bar.
- Press **P** to cycle **Center → Near Start → Near End → Freeform**, during placement or after connecting.
- While Pipe Wrench is active, press **=** to request more bends or **−** to request fewer on the current pipe. Numpad **+ / −** also work. New connections always start cleanly at two elbows.
- The status message shows the actual count and, when different, the requested count. Aligned ports stay straight at two elbows; request four bends to offset a straight run. Tight spaces may limit added bends, and enabled avoidance constraints may require extra bends.
- Tap **Shift** to cycle **None → Buildings → Overlap → Both → Strict**. **None** (default) uses clean two-elbow routing with no obstacle detours; **Buildings** avoids buildings and their labels; **Overlap** avoids shared pipe runs while allowing crossings; **Both** avoids buildings and shared runs while allowing crossings; **Strict** also avoids crossings. The active rule remains set after releasing Shift; holding Shift for port tooltips does not cycle it.
- The slim **Pipe Wrench toolbar**, at the top left beneath the mod bar, highlights the active constraint (none highlighted when no constraints are active). Click any of its four buttons to select a constraint, or click the active button again to clear constraints back to None. Hover a button for its full meaning. Changing rules updates a routed preview or selected connected pipe; toolbar clicks keep editing active. The toolbar stays visible after connecting until you click away, and is also shown while Grab Inserter is armed.
- Connect normally to keep the preview as editable pipe corners. The route snaps to a compatible port while hovering it. The connected pipe stays selected, with its sliders visible, until you click somewhere without a grab marker. Moving the pointer away does not end editing.
- A red preview means no clear avoidance route was found. Choose another endpoint or change the avoidance rule before connecting.
- Existing manually placed waypoints are preserved; Pipe Wrench shapes the remaining section. Ctrl-click can fix the current section as a waypoint and continue.
- Drag a grab marker to slide that section sideways while keeping the adjacent corners square. Vertical sections slide left/right; horizontal sections slide up/down. **= / −** also adjusts bends on the selected connected pipe.
- Slider moves snap to the active game grid, including Snappy's unlocked grid. **Ctrl** bypasses snapping during a slider move. A section needs enough space on the current grid to add local corners.
- Press **Alt+P** to activate **Grab Inserter**, then click the **section** of a pipe where you want a slider. The pipe can already have bends: the slider appears at the center of the clicked section. This works on vanilla pipes, sections beside a port, and diagonal sections. Inserting a slider keeps the pipe geometry unchanged; dragging slides that section or adds local square corners while keeping both ports and unrelated corners fixed. Alt+P again, right-click, or **Escape** cancels the inserter.
- **Right-click a slider to remove that marker**, keeping the pipe and its corners. **Escape** cancels an active marker move. Manual slider moves ignore building, overlap and crossing restrictions, while keeping the active snap grid.
- Sliders and their ownership exist only in memory during the current loaded game session. They are never written to your save or restored from configuration. Loading a save or restarting clears them; Alt+P can add new section sliders again. Pipe shapes themselves remain saved normally.

Building avoidance leaves a small clearance. Pipe overlap means a shared length of centerline; crossing means pipes intersect without sharing a run. Automatic routing allows pipe crossings, including diagonal native pipes, except in Strict mode. Shared connection terminals remain usable. Avoidance applies to automatic routing, route-shape changes and added bends. Manual slider moves and Freeform let you position pipes yourself, including through buildings and other pipes. Routing uses a bounded search; particularly crowded or enclosed layouts may have no available route. It does not move buildings.

## Requirements and Installation

For Kingdom Inc **1.0.14**, with **BepInEx 5** and [Foundation **0.14.4 or later**](https://www.nexusmods.com/kingdominc/mods/18). Snappy is optional; its finer grid is used after its research is unlocked. Port Layouts, Undo, and Right Click Fix are also optionally supported when installed.

Close the game before installing. Extract the Pipe Wrench download into your game folder so `PipeWrench.dll` and its `mod.json` are in `BepInEx/plugins/PipeWrench/`. Install or update Foundation separately in `BepInEx/plugins/Foundation/`.

## Settings

Options → Mod Settings → Pipe Wrench provides enable and grab-marker toggles. The configuration file also contains the mode key, bend adjustment keys, preferred bend count, selected avoidance rule and building clearance. Slider state is not stored in configuration or game saves.

## Compatibility

| Class.Method | Hook Type | Impact |
|---|---|---|
| WorldFlowIO.OnDragDropMove | Foundation hook (postfix) | Updates the pipe preview while a Pipe Wrench route is active. |
| WorldFlowIO.OnDragDropEnd | Foundation hook (prefix/postfix) | Finalizes corners, preserves manual waypoints, and keeps blocked avoidance routes in placement mode. |
| WorldFlowIO.OnDragDropCancel | Foundation hook (prefix) | Clears Pipe Wrench preview state when placement is cancelled. |

## Removal

Close the game and remove `BepInEx/plugins/PipeWrench/`. Existing pipe shapes remain in your saves and can still be edited using the game's normal corner controls.
