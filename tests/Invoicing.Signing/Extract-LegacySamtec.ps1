$ErrorActionPreference = 'Stop'
$source = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '../../ASFuelControl.Windows/Threads/PrintAgent.cs'))
$start = '        private void BuildInvoiceDetails(Samtec.WebService.Models.Request req, Data.Invoice invoice)'
$end = '        private Samtec.WebService.Models.Request CreateTankFillingRequest('
$a = $source.IndexOf($start, [StringComparison]::Ordinal)
$b = $source.IndexOf($end, [StringComparison]::Ordinal)
if ($a -lt 0 -or $b -le $a -or $source.IndexOf($start, $a + 1, [StringComparison]::Ordinal) -ge 0) { throw 'Cannot locate the legacy Samtec builders uniquely.' }
# Only the enum reference is substituted; its reviewed wire value is 11.
$methods = $source.Substring($a, $b - $a).Replace('(int)Exedron.MyData.Interfaces.VATExemptionCategoryEnum.Article27','11')
$generated = @"
using System;
using System.Linq;
using System.Collections.Generic;
namespace ASFuelControl.Invoicing.Tests {
internal sealed class LegacySamtec {
    private string samtecWSUrl = "unused";
    private string eftPosTid = "unused";
    private Samtec.WebService.Models.Request GetInvoiceInfoRequest(Data.Invoice invoice) { throw new NotSupportedException(); }
    public string Build(Data.Invoice invoice, string text) {
        var request = CreateInvoiceRequest(invoice); request.AtxtContent = text;
        return Newtonsoft.Json.JsonConvert.SerializeObject(request);
    }
$methods
}}
namespace Samtec.WebService {
internal static class HttpClient {
    public static Models.Response CallWS(Models.Request request, string url) { throw new InvalidOperationException("Legacy transport is forbidden in offline tests."); }
}}
"@
$output = Join-Path $PSScriptRoot 'obj/LegacySamtec.g.cs'
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($output)) | Out-Null
[IO.File]::WriteAllText($output, $generated, [Text.UTF8Encoding]::new($false))
