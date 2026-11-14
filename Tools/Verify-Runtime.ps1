[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$modRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = Split-Path -Parent (Split-Path -Parent $modRoot)
$foundationRoot = Join-Path (Split-Path -Parent $modRoot) 'Foundation'
if (Get-Process KingdomInc -ErrorAction SilentlyContinue) { throw 'Close Kingdom Inc before running isolated runtime verification.' }
& dotnet build (Join-Path $foundationRoot 'PipeWrench.RuntimeVerification.csproj') -c Release --nologo '-p:BaseIntermediateOutputPath=_AI/Scratch/PipeToolbarRuntime/obj/'
if ($LASTEXITCODE -ne 0) { throw 'Runtime verification helper failed to build.' }
$version = (Get-Content -LiteralPath (Join-Path $modRoot 'mod.json') -Raw | ConvertFrom-Json).version
$output = Join-Path $modRoot "_AI/Notes/Runtime-$version"
New-Item -ItemType Directory -Force -Path $output | Out-Null
$config = Join-Path $gameRoot 'BepInEx/config/urakka.kingdominc.pipewrench.cfg'
$configBytes = [IO.File]::ReadAllBytes($config)
$saves = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/Leaping Turtle/KingdomInc/Saves'
$before = @{}
Get-ChildItem -LiteralPath $saves -File -Recurse | ForEach-Object { $before[$_.FullName] = (Get-FileHash -LiteralPath $_.FullName).Hash }
$helperFolder = Join-Path $gameRoot 'BepInEx/plugins/_PipeWrenchRuntimeVerification'
$helper = Join-Path $helperFolder 'PipeWrench.RuntimeVerification.dll'
$steamIdFile = Join-Path $gameRoot 'steam_appid.txt'
$originalSteamId = if (Test-Path -LiteralPath $steamIdFile) { [IO.File]::ReadAllBytes($steamIdFile) } else { $null }
New-Item -ItemType Directory -Force -Path $helperFolder | Out-Null
if (Test-Path -LiteralPath (Join-Path $output 'result.txt')) {
    Copy-Item -LiteralPath (Join-Path $output 'result.txt') -Destination (Join-Path $output ('result-before-' + (Get-Date -Format 'HHmmss') + '.txt'))
    Remove-Item -LiteralPath (Join-Path $output 'result.txt')
}
try {
    # Keep this developer test process attached rather than relaunching through Steam.
    [IO.File]::WriteAllText($steamIdFile, '4538080')
    Copy-Item -LiteralPath (Join-Path $foundationRoot '_AI/Scratch/PipeToolbarRuntime/bin/netstandard2.1/PipeWrench.RuntimeVerification.dll') -Destination $helper
    $logPath = Join-Path $output 'unity-runtime.log'
    $arguments = '-force-d3d11 -screen-fullscreen 0 -screen-width 1280 -screen-height 720 --pipewrench-verify-runtime -logFile "' + $logPath + '"'
    $process = Start-Process -FilePath (Join-Path $gameRoot 'KingdomInc.exe') -ArgumentList $arguments -WorkingDirectory $gameRoot -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(110000)) { throw 'The isolated Unity run did not exit within its timeout.' }
    $result = Join-Path $output 'result.txt'
    if (-not (Test-Path -LiteralPath $result)) { throw "Unity did not write a verification result. Inspect $logPath" }
    Get-Content -LiteralPath $result
    if ($process.ExitCode -ne 0 -or ([IO.File]::ReadAllText($result) -notmatch '(?m)^PASS: \d+ runtime checks\.')) { throw 'Unity runtime verification failed.' }
} finally {
    if ($process -and -not $process.HasExited) { Stop-Process -Id $process.Id; $process.WaitForExit(10000) | Out-Null }
    if ($null -ne $originalSteamId) { [IO.File]::WriteAllBytes($steamIdFile, $originalSteamId) }
    elseif (Test-Path -LiteralPath $steamIdFile) { Remove-Item -LiteralPath $steamIdFile }
    [IO.File]::WriteAllBytes($config, $configBytes)
    if (Test-Path -LiteralPath $helper) { Remove-Item -LiteralPath $helper }
    if ((Get-ChildItem -LiteralPath $helperFolder -Force).Count -eq 0) { Remove-Item -LiteralPath $helperFolder }
    $after = @{}
    Get-ChildItem -LiteralPath $saves -File -Recurse | ForEach-Object { $after[$_.FullName] = (Get-FileHash -LiteralPath $_.FullName).Hash }
    if ($before.Count -ne $after.Count) { throw 'The user save collection changed during verification.' }
    foreach ($path in $before.Keys) { if ($before[$path] -ne $after[$path]) { throw "A user save changed: $path" } }
    Write-Host 'User saves unchanged; Pipe Wrench configuration restored; temporary verification plugin removed.'
}
