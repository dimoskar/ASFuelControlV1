[CmdletBinding()]
param([string] $MSBuild, [switch] $RecordSnapshots)
$ErrorActionPreference = 'Stop'
$projectDirectory = Join-Path $PSScriptRoot 'Invoicing.Characterization'
if (-not $MSBuild) {
    $command = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
    if ($command) { $MSBuild = $command.Source }
    else {
        $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
        if (Test-Path -LiteralPath $vswhere) {
            $MSBuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/MSBuild.exe' | Select-Object -First 1
        }
    }
}
if (-not $MSBuild) { throw 'MSBuild not found. Install Visual Studio Build Tools with .NET Framework 4.6 targeting assemblies, or pass -MSBuild.' }
& $MSBuild (Join-Path $projectDirectory 'Invoicing.Characterization.csproj') /t:Rebuild /p:Configuration=Release /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Characterization build failed: $LASTEXITCODE" }
$arguments = @('--fixtures', (Join-Path $projectDirectory 'Fixtures'))
if ($RecordSnapshots) { $arguments += '--record-snapshots' }
& (Join-Path $projectDirectory 'bin/Release/Invoicing.Characterization.exe') @arguments
if ($LASTEXITCODE -ne 0) { throw "Characterization tests failed: $LASTEXITCODE" }
