[CmdletBinding()]
param([string] $MSBuild, [string] $LocalDB, [string] $ScriptDomPath)
$ErrorActionPreference = 'Stop'
if (-not $MSBuild) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
    $MSBuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/MSBuild.exe' | Select-Object -First 1
}
if (-not $LocalDB) { $LocalDB = (Get-Command SqlLocalDB.exe -ErrorAction Stop).Source }
if (-not $MSBuild) { throw 'Visual Studio MSBuild with .NET Framework 4.6 targeting assemblies is required.' }
if (-not $ScriptDomPath) {
    $taskVsRoot = Split-Path (Split-Path (Split-Path (Split-Path $MSBuild -Parent) -Parent) -Parent) -Parent
    $ScriptDomPath = Join-Path $taskVsRoot 'Common7/IDE/Extensions/Microsoft/SQLDB/DAC/Microsoft.SqlServer.TransactSql.ScriptDom.dll'
}
if (-not (Test-Path -LiteralPath $ScriptDomPath)) { throw 'SQL ScriptDom is required for SQL Server 2008 grammar checks. Pass -ScriptDomPath.' }
$taskProject = Join-Path $PSScriptRoot 'Invoicing.Persistence/Invoicing.Persistence.csproj'
& $MSBuild $taskProject /t:Rebuild /p:Configuration=Release "/p:ScriptDomPath=$ScriptDomPath" /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Persistence build failed: $LASTEXITCODE" }
# A fresh private instance; never enumerate, connect to or delete existing application databases.
$taskInstance = 'ASFC_InvoicingStep3_' + [Guid]::NewGuid().ToString('N')
$taskCreated = $false
try {
    & $LocalDB create $taskInstance
    if ($LASTEXITCODE -ne 0) { throw 'Cannot create isolated LocalDB instance.' }
    $taskCreated = $true
    & $LocalDB start $taskInstance
    if ($LASTEXITCODE -ne 0) { throw 'Cannot start isolated LocalDB instance.' }
    & (Join-Path $PSScriptRoot 'Invoicing.Persistence/bin/Release/Invoicing.Persistence.exe') $taskInstance
    if ($LASTEXITCODE -ne 0) { throw "Persistence tests failed: $LASTEXITCODE" }
}
finally {
    if ($taskCreated -and $taskInstance -match '^ASFC_InvoicingStep3_[0-9a-f]{32}$') {
        & $LocalDB stop $taskInstance -k
        & $LocalDB delete $taskInstance
    }
}
