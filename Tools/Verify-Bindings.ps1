$ErrorActionPreference = 'Stop'
$modRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = Split-Path -Parent (Split-Path -Parent $modRoot)
Add-Type -Path (Join-Path $gameRoot 'BepInEx/core/Mono.Cecil.dll')
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $gameRoot 'KingdomInc_Data/Managed/Assembly-CSharp.dll'))
$foundation = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $gameRoot '_Mods/Foundation/Release/Foundation.dll'))
$baseline = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $modRoot '_AI/Notes/FoundationBefore/Foundation.dll'))
$consumer = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $modRoot 'Release/PipeWrench.dll'))
try {
    $io = $game.MainModule.GetType('Assets.Behaviour.Flow.WorldFlowIO')
    foreach ($method in @('OnDragDropMove', 'OnDragDropEnd', 'OnDragDropCancel')) {
        $matches = @($io.Methods | Where-Object Name -eq $method)
        if ($matches.Count -ne 1 -or $matches[0].ReturnType.FullName -ne 'System.Void') { throw "Invalid native hook: $method" }
    }
    $move = @($io.Methods | Where-Object Name -eq 'OnDragDropMove')[0]
    if (($move.Parameters.ParameterType.FullName -join ',') -ne 'UnityEngine.Vector2Int,UnityEngine.Vector2Int') { throw 'Pipe move signature changed.' }
    $ghost = @($io.Fields | Where-Object Name -eq '_pipeGhost')[0]
    if ($ghost.FieldType.FullName -ne 'Assets.Behaviour.Flow.WorldFlowPipe') { throw 'Pipe ghost field changed.' }
    $api = $foundation.MainModule.GetType('KingdomFoundation.Api.FoundationApi')
    if (-not ($api.Properties | Where-Object Name -eq 'PipeDrag')) { throw 'PipeDrag entry point missing.' }
    $ui = $foundation.MainModule.GetType('KingdomFoundation.Api.IGameUiAccess')
    $bounds = @($ui.Properties | Where-Object Name -eq 'ModBarBounds')
    if ($bounds.Count -ne 1 -or $bounds[0].PropertyType.FullName -ne 'UnityEngine.RectTransform') { throw 'Mod-bar HUD anchor missing.' }
    $factory = $foundation.MainModule.GetType('KingdomFoundation.Api.IFoundationNativeUiFactory')
    $blocker = @($factory.Methods | Where-Object Name -eq 'BlockWorkspaceZoom')
    if ($blocker.Count -ne 1 -or ($blocker[0].Parameters.ParameterType.FullName -join ',') -ne 'UnityEngine.GameObject') { throw 'Native zoom blocker bridge missing.' }
    if ($consumer.MainModule.AssemblyReferences.Name -contains 'Assembly-CSharp') { throw 'Consumer bypasses Foundation.' }
    $resource = @($consumer.MainModule.Resources | Where-Object Name -eq 'KingdomPipeWrench.Toolbar.json')
    if ($resource.Count -ne 1) { throw 'Designer toolbar layout missing from DLL.' }
    $embedded = [Text.Encoding]::UTF8.GetString($resource[0].GetResourceData())
    $layoutPath = Join-Path $modRoot 'Assets/UI/PipeWrenchToolbar.kingdom-ui.json'
    if ($embedded -cne [IO.File]::ReadAllText($layoutPath)) { throw 'Embedded toolbar differs from the user design.' }
    $publicBefore = @{}
    foreach ($type in $baseline.MainModule.GetTypes()) {
        if (-not ($type.IsPublic -or $type.IsNestedPublic)) { continue }
        $publicBefore[$type.FullName] = @($type.Methods | Where-Object IsPublic | ForEach-Object FullName)
    }
    $missing = @()
    foreach ($name in $publicBefore.Keys) {
        $type = $foundation.MainModule.GetType($name)
        if (-not $type) { $missing += $name; continue }
        $current = @($type.Methods | Where-Object IsPublic | ForEach-Object FullName)
        foreach ($method in $publicBefore[$name]) { if ($method -notin $current) { $missing += $method } }
    }
    if ($missing.Count -gt 0) { throw ('Existing Foundation public methods removed: ' + ($missing -join '; ')) }
    Write-Host "PASS: native drag hooks/ghost field match game 1.0.14; consumer has no game-assembly reference; $($publicBefore.Count) existing public Foundation types retain their public methods."
} finally { $game.Dispose(); $foundation.Dispose(); $baseline.Dispose(); $consumer.Dispose() }
