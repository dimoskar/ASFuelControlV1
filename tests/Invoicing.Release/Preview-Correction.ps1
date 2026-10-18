param([Parameter(Mandatory=$true)][string] $MSBuild)
$ErrorActionPreference='Stop'
$taskRepo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$taskOutput = Join-Path $PSScriptRoot 'output'
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
$taskCompiler = Join-Path (Split-Path $MSBuild -Parent) 'Roslyn/csc.exe'
$taskBinary = Join-Path $taskRepo 'MyDataCorrection/bin/Release'
$taskReferences = @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll') + @('MyDataCorrection.exe','ASFuelControl.Invoicing.Persistence.dll') | ForEach-Object {
    if ($_ -like 'System*') { '/r:' + $_ } else { '/r:' + (Join-Path $taskBinary $_) }
}
& $taskCompiler /nologo /target:exe ('/out:' + (Join-Path $taskOutput 'CorrectionPreview.exe')) @taskReferences (Join-Path $PSScriptRoot 'CorrectionPreview.cs')
if ($LASTEXITCODE -ne 0) { throw 'Correction preview compile failed.' }
# A standalone form constructor only. No Program.Main, settings read, DB or transport.
& (Join-Path $taskOutput 'CorrectionPreview.exe') $taskBinary (Join-Path $taskOutput 'correction.png')
if ($LASTEXITCODE -ne 0) { throw 'Correction preview failed.' }
