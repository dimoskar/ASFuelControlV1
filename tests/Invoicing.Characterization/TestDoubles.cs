// Infrastructure substitutes only. Mapping, serialization and send decisions are
// compiled from production source. No SQL, sockets, HTTP client, printer or SDK.
using System;
using System.Collections.Generic;
using System.Linq;
using ASFuelControl.Data;
using Exedron.MyData.Models.Responses;

// This suite intentionally characterizes the retained legacy branch. Shared workflow
// behavior is exercised separately against real SQL stores in Invoicing.Workflow.
namespace ASFuelControl.Windows.Threads
{
    internal static class SharedInvoiceRuntime
    {
        internal static bool Enabled => false;
        internal static bool Handles(Guid id) => false;
        internal static bool HandlesDocument(string kind, Guid id) => false;
        internal static void ProcessDocument(string kind, Guid id) { throw new InvalidOperationException("Legacy fixture entered the shared document path."); }
        internal static SharedResult Process(Guid id, bool print = true) { throw new InvalidOperationException("Legacy fixture entered the shared path."); }
    }
    internal sealed class SharedResult { internal bool Accepted; }
}

namespace ASFuelControl.Data
{
    public sealed class DatabaseModel : IDisposable
    {
        public static List<Invoice> InvoiceRows = new List<Invoice>();
        public static List<MyDataInvoice> TransmissionRows = new List<MyDataInvoice>();
        public static List<string> SavedStates = new List<string>();
        public static List<Balance> BalanceRows = new List<Balance>();
        public static List<SystemEvent> AlertRows = new List<SystemEvent>();
        public static List<Titrimetry> TitrimetryRows = new List<Titrimetry>();
        public static List<TankFilling> FillingRows = new List<TankFilling>();
        public DatabaseModel(string ignored = null) { }
        public List<Invoice> Invoices => InvoiceRows;
        public List<MyDataInvoice> MyDataInvoices => TransmissionRows;
        public List<Balance> Balances => BalanceRows;
        public List<SystemEvent> SystemEvents => AlertRows;
        public List<Titrimetry> Titrimetries => TitrimetryRows;
        public List<TankFilling> TankFillings => FillingRows;
        public void Add(MyDataInvoice row) { TransmissionRows.Add(row); }
        public void SaveChanges() { SavedStates.Add(string.Join(",", TransmissionRows.Select(r => r.Status.ToString()))); }
        public void Dispose() { }
    }
    public sealed class Invoice
    {
        public Guid InvoiceId { get; set; }
        public InvoiceType InvoiceType { get; set; }
        public Trader Trader { get; set; }
        public Vehicle Vehicle { get; set; }
        public int Number { get; set; }
        public string Series { get; set; }
        public DateTime TransactionDate { get; set; }
        public int? PaymentType { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? VatAmount { get; set; }
        public string InvoiceSignature { get; set; }
        public string QRCodeData { get; set; }
        public bool? IsPrinted { get; set; }
        public Guid? VehicleId { get; set; }
        public Guid InvoiceTypeId { get; set; }
        public List<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();
        public List<InvoiceRelation> ParentInvoiceRelations { get; set; } = new List<InvoiceRelation>();
    }
    public sealed class InvoiceType
    {
        public int OfficialEnumerator { get; set; }
        public bool? IsCancelation { get; set; }
        public bool SendToMyData { get; set; } = true;
        public int LastNumber { get; set; }
        public string DefaultSeries { get; set; }
        public bool? ForcesDelivery { get; set; }
        public int TransactionType { get; set; }
        public bool? IsInternal { get; set; }
        public bool? IsLaserPrint { get; set; }
    }
    public sealed class InvoiceRelation { public Invoice ParentInvoice { get; set; } }
    public sealed class Vehicle { public string PlateNumber { get; set; } }
    public sealed class InvoiceLine
    {
        public string FuelTypeName { get; set; }
        public FuelType FuelType { get; set; }
        public decimal Volume { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal VatAmount { get; set; }
        public decimal VatPercentage { get; set; }
        public Guid? TankId { get; set; }
        public Guid InvoiceLineId { get; set; }
    }
    public sealed class FuelType { public int EnumeratorValue { get; set; } public string Name { get; set; } }
    public sealed class Trader
    {
        public bool? VatExemption { get; set; }
        public string Country { get; set; }
        public string TaxRegistrationNumber { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string ZipCode { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string TaxRegistrationOffice { get; set; }
        public string Occupation { get; set; }
        public string Phone1 { get; set; }
        public int Branch { get; set; }
    }
    public sealed class CompanyData
    {
        public string CompanyName => "SYNTHETIC FUEL COMPANY";
        public string CompanyCity => "Athens";
        public string CompanyPostalCode => "10000";
        public string CompanyAddress => "Example Street 10";
    }
    public sealed class MyDataInvoice
    {
        public Guid MyDataInvoiceId { get; set; }
        public Guid InvoiceId { get; set; }
        public int Status { get; set; }
        public DateTime? DateTimeSent { get; set; }
        public string Data { get; set; }
        public string Errors { get; set; }
        public string Uid { get; set; }
        public long? Mark { get; set; }
        public long? CanceledByMark { get; set; }
        public long? CancelationMark { get; set; }
        public string QrCodeUrl { get; set; }
        public string VerificationHash { get; set; }
        public string ProviderUrl { get; set; }
        public string InvoiceUrl { get; set; }
    }
    public sealed class Balance { public Guid BalanceId { get; set; } public string DocumentSign { get; set; } public DateTime? PrintDate { get; set; } }
    public sealed class SystemEvent { public Guid EventId { get; set; } public string DocumentSign { get; set; } public DateTime? PrintedDate { get; set; } }
    public sealed class Titrimetry { public Guid TitrimetryId { get; set; } public string DocumentSign { get; set; } public DateTime? PrintDate { get; set; } }
    public sealed class TankFilling { public Guid TankFillingId { get; set; } public string SignSignature { get; set; } }
}
namespace ASFuelControl.Data.Implementation
{
    public sealed class OptionHandler
    {
        public static readonly OptionHandler Instance = new OptionHandler();
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
        public string GetOption(string key, string fallback = "") => Values.ContainsKey(key) ? Values[key] : fallback;
        public int GetIntOption(string key, int fallback) { int n; return int.TryParse(GetOption(key), out n) ? n : fallback; }
        public bool GetBoolOption(string key, bool fallback) { bool b; return bool.TryParse(GetOption(key), out b) ? b : fallback; }
    }
    public static class Country
    {
        // Fixture lookup only; countries outside this explicit set fail closed.
        public static bool IsEuCountry(string country)
        {
            if (country == "DE") return true;
            if (country == "US") return false;
            throw new InvalidOperationException("Unconfigured fixture country: " + country);
        }
    }
}
namespace ASFuelControl.Common
{
    public sealed class Logger
    {
        public static readonly Logger Instance = new Logger();
        public Logger CurrentLogger => this;
        public void Debug(string ignored) { }
        public void Error(string ignored) { }
    }
}
namespace ASFuelControl.Windows.Properties
{
    public sealed class Settings
    {
        public static readonly Settings Default = new Settings();
        public string DBConnection => "IN_MEMORY_ONLY";
        public string[] WholeSaleCodes => new[] { "100", "158", "159" };
    }
}
namespace ASFuelControl.Windows.Threads
{
    public partial class PrintAgent
    {
        private static bool invoicingProviderEnabled;
        private const int MaxRememberedSigns = 1000;
        private readonly List<string> signsApplied = new List<string>();
        private readonly List<string> invoicesProcessed = new List<string>();
        private readonly List<Guid> invoiceProcessed = new List<Guid>();
        private readonly List<Guid> balancesProcessed = new List<Guid>();
        private readonly List<Guid> tankFillingsProcessed = new List<Guid>();
        private readonly string samtecWSUrl = "OFFLINE_DEVICE";
        public static Action<Invoice> PrintInvoiceBoundary;
        public static Action<string> PrintOtherBoundary;
        public static void SelectProvider(bool provider) { invoicingProviderEnabled = provider; }
        public void RunBalance(DatabaseModel db, ASFuelControl.Data.Balance value) { SignBalance(db, value, "synthetic balance"); }
        public void RunFilling(DatabaseModel db, ASFuelControl.Data.TankFilling value) { SignTankFilling(db, value, "synthetic filling", 40); }
        public void RunAlert(DatabaseModel db, ASFuelControl.Data.SystemEvent value) { SignAlert(db, value, "synthetic alert"); }
        public void RunTitrimetry(DatabaseModel db, ASFuelControl.Data.Titrimetry value) { SignTitrimetry(db, value, "synthetic titrimetry"); }
        // Request composition and printer execution are explicit test boundaries.
        // These stubs do not claim to test Samtec payload generation or a printer.
        private List<string> CreateInvoiceText(DatabaseModel db, Invoice invoice) => new List<string> { "synthetic invoice text" };
        private Samtec.WebService.Models.Request CreateInvoiceRequest(Invoice invoice) => new Samtec.WebService.Models.Request();
        private Samtec.WebService.Models.Request CreateBalanceRequest(ASFuelControl.Data.Balance value) => new Samtec.WebService.Models.Request();
        private Samtec.WebService.Models.Request CreateTankFillingRequest(DatabaseModel db, ASFuelControl.Data.TankFilling value, int code) => new Samtec.WebService.Models.Request();
        private void PrintToPrinter(DatabaseModel db, Invoice invoice) { PrintInvoiceBoundary(invoice); }
        private void PrintToPrinterInternalInvoice(Invoice invoice, bool ignored) { PrintInvoiceBoundary(invoice); }
        private void PrintToPrinter(ASFuelControl.Data.Balance value, bool ignored) { PrintOtherBoundary("Balance"); }
        private void PrintToPrinter(ASFuelControl.Data.SystemEvent value) { PrintOtherBoundary("Alert"); }
        private void PrintToPrinter(ASFuelControl.Data.Titrimetry value) { PrintOtherBoundary("Titrimetry"); }
        private void PrintToPrinter(ASFuelControl.Data.TankFilling value) { PrintOtherBoundary("TankFilling"); }
        public static void SetInvoicingParameters() { } // Fixture configuration replaces the singleton setup.
        private static void LogProviderMemory(string stage, Guid id, int mode) { }
        public static bool RunSend(Guid id, bool provider, int mode = 2)
        {
            invoicingProviderEnabled = provider;
            return MyDataSendInvoice(id, mode);
        }
    }
}
namespace ASFuelControl.Windows
{
    public static class Program { public static readonly MainFormStub ApplicationMainForm = new MainFormStub(); }
    public sealed class MainFormStub { public readonly TankControllerStub ThreadControllerInstance = new TankControllerStub(); }
    public sealed class TankControllerStub
    {
        public List<TankStub> Tanks = new List<TankStub>();
        public List<TankStub> GetTanks() => Tanks;
    }
    public sealed class TankStub
    {
        public Guid TankId, VehicleId, InvoiceTypeId, InvoiceLineId;
        public bool InitializeExtraction, InitializeFilling;
    }
}
namespace Samtec.WebService.Models
{
    public sealed class Request { public string AtxtContent { get; set; } }
    public sealed class Response { public string ResultCode { get; set; } }
}
namespace Samtec.WebService
{
    public static class HttpClient
    {
        public static int Calls;
        public static Func<Models.Response> Reply;
        public static Models.Response CallWS(Models.Request request, string endpoint)
        {
            Calls++;
            if (Reply == null) throw new InvalidOperationException("No offline device response configured");
            return Reply();
        }
    }
}
namespace Exedron.MyData
{
    public static class InvoiceSender
    {
        public static int Calls;
        public static string LastXml;
        public static Func<ResponseDoc> Reply;
        public static ResponseDoc MakeRequest(string url, string xml)
        {
            Calls++; LastXml = xml;
            if (Reply == null) throw new InvalidOperationException("No offline response configured");
            return Reply();
        }
    }
    public static class InvoiceCancelation
    {
        public static int Calls;
        public static long LastMark;
        public static Func<ResponseDoc> Reply;
        public static ResponseDoc MakeRequest(string url, long mark)
        {
            Calls++; LastMark = mark;
            if (Reply == null) throw new InvalidOperationException("No offline cancellation response configured");
            return Reply();
        }
    }
}
namespace Exedron.ProviderInvoicing
{
    public sealed class InvoiceHandler
    {
        public static readonly InvoiceHandler Instance = new InvoiceHandler();
        public string[] Errors = new string[0];
        public string CompanyName = "SYNTHETIC FUEL COMPANY";
        public string CompanyVAT = "000000000";
        public int CompanyBranch = 0;
        public string CompanyEmail = "issuer@example.invalid";
        public string CompanyTaxOffice = "TEST";
        public string CompanyOccupation = "FUEL";
        public string CompanyPhone = "0000000000";
        public string CompanyAddressStreet = "Example Street 10";
        public string CompanyPostalCode = "10000";
        public string CompanyCity = "Athens";
        public int Calls;
        public InvoiceModel LastInvoice;
        public Func<InvoiceResponse> Reply;
        public InvoiceResponse SendInvoice(InvoiceModel invoice)
        {
            Calls++; LastInvoice = invoice;
            if (Reply == null) throw new InvalidOperationException("No offline provider response configured");
            return Reply();
        }
    }
}
