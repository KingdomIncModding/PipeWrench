[CmdletBinding()]
param([switch]$Install)
$ErrorActionPreference = 'Stop'
$modRoot = Split-Path -Parent $PSScriptRoot
$modsRoot = Split-Path -Parent $modRoot
$gameRoot = Split-Path -Parent $modsRoot
$checker = Join-Path $modsRoot '_AI/_Workflows/Tools/SourceLayout/Check-SourceLayout.ps1'
foreach ($name in @('Foundation', 'PipeWrench')) {
    $root = Join-Path $modsRoot $name
    & $checker -ModFolder $root
    if ($LASTEXITCODE -ne 0) { throw "Source layout failed: $name" }
    & dotnet build (Join-Path $root "$name.csproj") -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $name" }
    $stage = Join-Path $root 'Release'
    New-Item -ItemType Directory -Force -Path $stage | Out-Null
    Copy-Item -LiteralPath (Join-Path $root "bin/Release/netstandard2.1/$name.dll") -Destination $stage -Force
    Copy-Item -LiteralPath (Join-Path $root 'mod.json') -Destination $stage -Force
}
& (Join-Path $PSScriptRoot 'Verify.ps1')
if ($Install) {
    if (Get-Process -Name KingdomInc -ErrorAction SilentlyContinue) { throw 'Close Kingdom Inc before replacing its local plugins.' }
    $configPath = Join-Path $gameRoot 'BepInEx/config/urakka.kingdominc.pipewrench.cfg'
    if (Test-Path -LiteralPath $configPath) {
        $text = [IO.File]::ReadAllText($configPath)
        $updated = [regex]::Replace($text, '(?ms)^\[State\]\r?\n.*?(?=^\[|\z)', {
            param($match)
            $entries = @($match.Value -split '\r?\n' | Where-Object { $_ -match '^\s*[^#;\[\s][^=]*=' })
            if ($entries.Count -eq 1 -and $entries[0] -match '^ManagedConnections\s*=') { return '' }
            return [regex]::Replace($match.Value, '(?m)^ManagedConnections\s*=.*\r?\n?', '')
        })
        if ($updated -ne $text) {
            $backup = Join-Path $modRoot '_AI/Notes/Legacy-marker-config-before-migration.cfg'
            if (-not (Test-Path -LiteralPath $backup)) { Copy-Item -LiteralPath $configPath -Destination $backup }
            [IO.File]::WriteAllText($configPath, $updated, [Text.UTF8Encoding]::new($false))
            Write-Host 'Removed legacy persisted marker identities; other settings are preserved.'
        }
    }
    foreach ($name in @('Foundation', 'PipeWrench')) {
        $stage = Join-Path $modsRoot "$name/Release"
        $destination = Join-Path $gameRoot "BepInEx/plugins/$name"
        New-Item -ItemType Directory -Force -Path $destination | Out-Null
        foreach ($file in @("$name.dll", 'mod.json')) {
            Copy-Item -LiteralPath (Join-Path $stage $file) -Destination $destination -Force
            if ((Get-FileHash -LiteralPath (Join-Path $stage $file)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $destination $file)).Hash) {
                throw "Installed file differs: $name/$file"
            }
        }
        Write-Host "Installed and hash-verified: $name"
    }
}
Write-Host 'Local build complete. No publication performed.'
