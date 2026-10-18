[CmdletBinding()]
param([string] $MSBuild)
$ErrorActionPreference = 'Stop'
$projectDirectory = Join-Path $PSScriptRoot 'Invoicing.Mapping'
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
& $MSBuild (Join-Path $projectDirectory 'Invoicing.Mapping.csproj') /t:Build /p:Configuration=Release "/p:SolutionDir=$((Split-Path $PSScriptRoot -Parent))\" /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Mapping build failed: $LASTEXITCODE" }
& (Join-Path $projectDirectory 'bin/Release/Invoicing.Mapping.exe')
if ($LASTEXITCODE -ne 0) { throw "Mapping tests failed: $LASTEXITCODE" }
