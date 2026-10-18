[CmdletBinding()]
param([string] $MSBuild, [string] $LocalDB, [string] $Python = 'python')
$ErrorActionPreference = 'Stop'
$taskRepo = Split-Path $PSScriptRoot -Parent
$taskOutput = Join-Path $PSScriptRoot 'Invoicing.Release/output'
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
if (-not $MSBuild) {
    $taskVswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
    $MSBuild = & $taskVswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/MSBuild.exe' | Select-Object -First 1
}
if (-not $MSBuild) { throw 'Visual Studio MSBuild is required.' }
if (-not $LocalDB) { $LocalDB = (Get-Command SqlLocalDB.exe -ErrorAction Stop).Source }
$taskChecks = [System.Collections.Generic.List[object]]::new()
function Invoke-ReleaseCheck([string] $Name, [scriptblock] $Action) {
    $taskLog = Join-Path $taskOutput ($Name + '.log')
    $taskStarted = [DateTime]::UtcNow
    $taskSuccess = $false
    try { & $Action *> $taskLog; $taskSuccess = $true }
    catch { $_ | Out-String | Add-Content -LiteralPath $taskLog }
    $taskChecks.Add([pscustomobject]@{ Name=$Name; Passed=$taskSuccess; StartedUtc=$taskStarted; FinishedUtc=[DateTime]::UtcNow; Log=$taskLog })
    Write-Output ((@{ $true='PASS'; $false='FAIL' })[$taskSuccess] + ' ' + $Name)
}
Push-Location $taskRepo
try {
    # Sequential: the suites share build output, but each SQL suite creates and
    # removes its own private instance. No application config/database is loaded.
    foreach ($taskSuite in @('Contracts','Mapping','Providers','Payments','Persistence','Signing','Workflow','Characterization')) {
        $taskScript = Join-Path $PSScriptRoot ('Verify-Invoicing' + $taskSuite + '.ps1')
        Invoke-ReleaseCheck $taskSuite {
            $taskArguments = @{ MSBuild=$MSBuild }
            if ($taskSuite -in @('Providers','Payments','Persistence','Signing','Workflow')) { $taskArguments.LocalDB=$LocalDB }
            & $taskScript @taskArguments
        }
    }
    Invoke-ReleaseCheck 'Conformance' {
        & $Python -X utf8 tests/Invoicing.ConformanceReview/run.py
        if ($LASTEXITCODE -ne 0) { throw 'Offline conformance failed.' }
    }
    Invoke-ReleaseCheck 'CorrectionBuild' {
        & $MSBuild MyDataCorrection/MyDataCorrection.csproj /t:Build /p:Configuration=Release /p:RegisterForComInterop=false /nologo /v:minimal
        if ($LASTEXITCODE -ne 0) { throw 'Evidence utility build failed.' }
    }
    Invoke-ReleaseCheck 'SolutionBuild' {
        & $MSBuild ASFuelControl.Windows.sln /t:Build /p:Configuration=Release '/p:Platform=Any CPU' /p:RegisterForComInterop=false /nologo /v:minimal
        if ($LASTEXITCODE -ne 0) { throw 'Full solution build failed: release is blocked.' }
    }
    Invoke-ReleaseCheck 'WindowsDiagnostic' {
        $taskTargets = Join-Path $PSScriptRoot 'Invoicing.Workflow/WindowsCompile.targets'
        & $MSBuild ASFuelControl.Windows/ASFuelControl.Windows.csproj /t:Build /p:Configuration=Release /p:Platform=x86 '/p:OutputPath=bin\Release\' /p:BuildProjectReferences=false /p:RegisterForComInterop=false "/p:CustomAfterMicrosoftCommonTargets=$taskTargets" "/p:SolutionDir=$taskRepo\" /nologo /v:minimal
        if ($LASTEXITCODE -ne 0) { throw 'Windows diagnostic compilation failed.' }
    }
    Invoke-ReleaseCheck 'NativeUI' {
        & $Python -X utf8 tests/Invoicing.UI/build-preview.py
        if ($LASTEXITCODE -ne 0) { throw 'Native UI verification failed.' }
        & (Join-Path $PSScriptRoot 'Invoicing.Release/Preview-Correction.ps1') -MSBuild $MSBuild
    }
    $taskReport = [ordered]@{
        GeneratedUtc=[DateTime]::UtcNow
        Checks=$taskChecks.ToArray()
        AutomatedChecksPassed=(@($taskChecks | Where-Object { -not $_.Passed }).Count -eq 0)
        ProductionCutoverReady=$false
        PendingInstallationChecks=@('Native SQL Server 2008 R2 restore and migrations','Deployed OpenAccess application workflows','Selected provider sandbox and approved POS/device/printer validation','Historical evidence reconciliation','Recorded pilot acceptance and backup/rollback rehearsal')
    }
    $taskReport | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $taskOutput 'verification.json') -Encoding UTF8
    Write-Output ('Report: ' + (Join-Path $taskOutput 'verification.json'))
    if (-not $taskReport.AutomatedChecksPassed) { throw 'Release verification contains failures. See verification.json and individual logs.' }
}
finally { Pop-Location }
