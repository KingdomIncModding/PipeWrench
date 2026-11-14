$ErrorActionPreference = 'Stop'
$modRoot = Split-Path -Parent $PSScriptRoot
$gameRoot = Split-Path -Parent (Split-Path -Parent $modRoot)
$output = Join-Path $modRoot '_AI/Gen/Assets/Promotions'
New-Item -ItemType Directory -Force -Path (Join-Path $output 'Frames') | Out-Null
& dotnet run --project (Join-Path $modRoot 'PipeWrench.Verification.csproj') -c Release -- --presentation-routes (Join-Path $output 'routes.json')
if ($LASTEXITCODE -ne 0) { throw 'Could not export presentation routes.' }
& node (Join-Path $modRoot 'src/Presentation/render.mjs') $gameRoot $output
if ($LASTEXITCODE -ne 0) { throw 'Presentation rendering failed.' }
& python (Join-Path $modRoot 'src/Presentation/encode_gif.py') $output
if ($LASTEXITCODE -ne 0) { throw 'GIF encoding failed.' }
