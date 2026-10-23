[CmdletBinding()]
param([string] $MSBuild)
$ErrorActionPreference = 'Stop'
$taskRepo = Split-Path $PSScriptRoot -Parent
if (-not $MSBuild) {
    $MSBuild = & "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/MSBuild.exe' | Select-Object -First 1
}
$taskBinary = Join-Path $taskRepo 'ASFuelControl.Windows/bin/Release'
$taskCompiler = Join-Path (Split-Path $MSBuild -Parent) 'Roslyn/csc.exe'
$taskReferences = @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Data.dll','System.Configuration.dll','System.Xml.dll') | ForEach-Object { '/r:' + $_ }
$taskReferences += @('ASFuelControl.Windows.exe','ASFuelControl.Data.dll','ASFuelControl.Common.dll','ASFuelControl.Invoicing.Persistence.dll','Exedron.Invoicing.Core.dll','Telerik.WinControls.dll','Telerik.WinControls.UI.dll','Telerik.WinControls.GridView.dll','TelerikCommon.dll','TelerikData.dll','Telerik.OpenAccess.dll','Telerik.OpenAccess.35.Extensions.dll') | ForEach-Object { '/r:' + (Join-Path $taskBinary $_) }
$taskProbe = Join-Path $taskBinary 'HeatingNativeProbe.exe'
& $taskCompiler /nologo /langversion:7.3 /target:exe /platform:x86 "/out:$taskProbe" @taskReferences (Join-Path $PSScriptRoot 'HeatingBatch/NativeProbe.cs')
if ($LASTEXITCODE -ne 0) { throw 'Heating batch native probe compilation failed.' }
$taskInstance = 'ASFC_HeatingBatch_' + [Guid]::NewGuid().ToString('N')
$taskCreated = $false
try {
    & SqlLocalDB.exe create $taskInstance
    if ($LASTEXITCODE -ne 0) { throw 'Cannot create private test instance.' }
    $taskCreated = $true
    & SqlLocalDB.exe start $taskInstance
    if ($LASTEXITCODE -ne 0) { throw 'Cannot start private test instance.' }
    & $taskProbe $taskInstance
    if ($LASTEXITCODE -ne 0) { throw 'Heating batch native/accounting tests failed.' }
} finally {
    if ($taskCreated -and $taskInstance -match '^ASFC_HeatingBatch_[0-9a-f]{32}$') {
        & SqlLocalDB.exe stop $taskInstance -k
        & SqlLocalDB.exe delete $taskInstance
    }
}
