[CmdletBinding()]
param([string] $MSBuild, [string] $LocalDB, [string] $NativeProbe, [string] $WindowsBinaryDirectory, [switch] $NativeOnly)
$ErrorActionPreference = 'Stop'
$taskRepo = Split-Path $PSScriptRoot -Parent
if (-not $MSBuild) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
    $MSBuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/MSBuild.exe' | Select-Object -First 1
}
if (-not $LocalDB) { $LocalDB = (Get-Command SqlLocalDB.exe -ErrorAction Stop).Source }
if ($WindowsBinaryDirectory) {
    # Supply a directory containing the rebuilt Windows executable and its dependencies.
    $taskBinary = (Resolve-Path -LiteralPath $WindowsBinaryDirectory).Path
    $taskCompiler = Join-Path (Split-Path $MSBuild -Parent) 'Roslyn/csc.exe'
    $taskReferences = @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Data.dll','System.Configuration.dll','System.Xml.dll') | ForEach-Object { '/r:' + $_ }
    $taskReferences += @('ASFuelControl.Windows.exe','ASFuelControl.Data.dll','ASFuelControl.Common.dll','Telerik.WinControls.dll','Telerik.WinControls.UI.dll','Telerik.WinControls.GridView.dll','TelerikCommon.dll','TelerikData.dll','Telerik.OpenAccess.dll','Telerik.OpenAccess.35.Extensions.dll') | ForEach-Object { '/r:' + (Join-Path $taskBinary $_) }
    $NativeProbe = Join-Path $taskBinary 'NativeProbe.exe'
    & $taskCompiler /nologo /langversion:7.3 /target:exe /platform:x86 "/out:$NativeProbe" @taskReferences (Join-Path $PSScriptRoot 'InvoiceCatalog/NativeProbe.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Native catalog probe compilation failed.' }
}
if ($NativeOnly -and -not $NativeProbe) { throw '-NativeOnly requires -NativeProbe or -WindowsBinaryDirectory.' }
& $MSBuild (Join-Path $PSScriptRoot 'InvoiceCatalog/InvoiceCatalog.csproj') /t:Build /p:Configuration=Release "/p:SolutionDir=$taskRepo\" /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Catalog regression build failed.' }
# Never inspect or use any application database or saved connection string.
$taskInstance = 'ASFC_Catalog_' + [Guid]::NewGuid().ToString('N')
$taskCreated = $false
try {
    & $LocalDB create $taskInstance
    if ($LASTEXITCODE -ne 0) { throw 'Cannot create private catalog test instance.' }
    $taskCreated = $true
    & $LocalDB start $taskInstance
    if ($LASTEXITCODE -ne 0) { throw 'Cannot start private catalog test instance.' }
    $taskArguments = @($taskInstance, $taskRepo)
    if ($NativeProbe) { $taskArguments += (Resolve-Path -LiteralPath $NativeProbe).Path }
    if ($NativeOnly) { $taskArguments += '--native-only' }
    & (Join-Path $PSScriptRoot 'InvoiceCatalog/bin/Release/InvoiceCatalog.Tests.exe') @taskArguments
    if ($LASTEXITCODE -ne 0) { throw 'Catalog regression tests failed.' }
}
finally {
    if ($taskCreated -and $taskInstance -match '^ASFC_Catalog_[0-9a-f]{32}$') {
        & $LocalDB stop $taskInstance -k
        & $LocalDB delete $taskInstance
    }
}
