[CmdletBinding()]
param([string] $MSBuild, [string] $LocalDB)
$ErrorActionPreference = 'Stop'
if (-not $MSBuild) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
    $MSBuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/MSBuild.exe' | Select-Object -First 1
}
if (-not $LocalDB) { $LocalDB = (Get-Command SqlLocalDB.exe -ErrorAction Stop).Source }
if (-not $MSBuild) { throw 'Visual Studio MSBuild with .NET Framework 4.6 targeting assemblies is required.' }
$taskProject = Join-Path $PSScriptRoot 'Invoicing.Providers/Invoicing.Providers.csproj'
& $MSBuild $taskProject /t:Build /p:Configuration=Release "/p:SolutionDir=$((Split-Path $PSScriptRoot -Parent))\" /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Provider build failed: $LASTEXITCODE" }
# A fresh private instance; never enumerate, connect to or delete existing application databases.
$taskInstance = 'ASFC_InvoicingStep5_' + [Guid]::NewGuid().ToString('N')
$taskCreated = $false
try {
    & $LocalDB create $taskInstance
    if ($LASTEXITCODE -ne 0) { throw 'Cannot create isolated LocalDB instance.' }
    $taskCreated = $true
    & $LocalDB start $taskInstance
    if ($LASTEXITCODE -ne 0) { throw 'Cannot start isolated LocalDB instance.' }
    & (Join-Path $PSScriptRoot 'Invoicing.Providers/bin/Release/Invoicing.Providers.exe') $taskInstance
    if ($LASTEXITCODE -ne 0) { throw "Provider tests failed: $LASTEXITCODE" }
}
finally {
    if ($taskCreated -and $taskInstance -match '^ASFC_InvoicingStep5_[0-9a-f]{32}$') {
        & $LocalDB stop $taskInstance -k
        & $LocalDB delete $taskInstance
    }
}
