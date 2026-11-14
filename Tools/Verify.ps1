$ErrorActionPreference = 'Stop'
$modRoot = Split-Path -Parent $PSScriptRoot
& dotnet run --project (Join-Path $modRoot 'PipeWrench.Verification.csproj') --configuration Release
if ($LASTEXITCODE -ne 0) { throw 'Pipe Wrench routing verification failed.' }
