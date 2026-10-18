$ErrorActionPreference = 'Stop'
$sourcePath = Join-Path $PSScriptRoot '../../ASFuelControl.Windows/Threads/PrintAgent.cs'
$source = [IO.File]::ReadAllText($sourcePath)

# Boundaries are deliberately explicit. Fail rather than silently using an old copy
# when production methods are renamed/moved. The C# compiler checks the excerpts.
function Read-Section([string] $start, [string] $end) {
    $a = $source.IndexOf($start, [StringComparison]::Ordinal)
    $b = $source.IndexOf($end, [StringComparison]::Ordinal)
    if ($a -lt 0 -or $b -le $a -or $source.IndexOf($start, $a + 1, [StringComparison]::Ordinal) -ge 0) {
        throw "Cannot uniquely locate legacy method section: $start"
    }
    return $source.Substring($a, $b - $a)
}

$mapping = Read-Section '        public static Exedron.MyData.InvoiceModels.Invoice CreateInvoice(' '        public static string GetInvoiceTypeContext('
$sending = Read-Section '        private static bool MyDataSendInvoice(' '        private static void LogProviderMemory('
$signing = Read-Section '        public void SignInvoice(' '        private void PrintInvoice('
$applying = Read-Section '        private bool ApplySign(' '        private string EvaluateSign('
$generated = @"
// Generated from ASFuelControl.Windows/Threads/PrintAgent.cs. DO NOT EDIT.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
namespace ASFuelControl.Windows.Threads
{
    public static class InvoiceHelper
    {
$mapping
    }
    public partial class PrintAgent
    {
$sending
$signing
$applying
    }
}
"@
$output = Join-Path $PSScriptRoot 'obj/LegacyMethods.g.cs'
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($output)) | Out-Null
[IO.File]::WriteAllText($output, $generated, [Text.UTF8Encoding]::new($false))
