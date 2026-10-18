using System;
using System.Collections.Generic;
using Exedron.Invoicing.Core.Models;

namespace Exedron.Invoicing.Core
{
    public enum InvoiceMode { Normal = 0, Cancel = 1, Credit = 2 }
    public enum InvoicePaymentType { Credit = 0, Check = 1, Bank = 2, Card = 3, Cash = 4, IRIS = 5 }

    // Snapshots supplied by the host. No entity tracking, lazy loading, SQL or UI dependencies.
    public sealed class InvoiceBuildRequest
    {
        public InvoiceInput Invoice { get; set; }
        public CompanySettings Company { get; set; }
        public CountryInput Country { get; set; }
        public IEnumerable<InvoiceParent> Parents { get; set; }
        public MovementContext Movement { get; set; }
        // Ordinary delivery, separate from the tanker's allocation/parent workflow.
        public DeliveryContext Delivery { get; set; }
        // Explicit normal-sale correlations; credit parents still use Parents.
        public IEnumerable<InvoiceParent> SaleCorrelations { get; set; }
        public bool RequireIssuedIdentity { get; set; } = true;
    }
    public sealed class InvoiceInput
    {
        public Guid InvoiceId { get; set; }
        public Guid TraderId { get; set; }
        public InvoiceSeriesInput InvoiceSeries { get; set; }
        public TraderInput Trader { get; set; }
        public int Number { get; set; }
        // Greek fiscal wall-clock time, NOT UTC. Audit timestamps are separate.
        public DateTime PrintedDateTime { get; set; }
        public string Series { get; set; }
        public string SupplyCode { get; set; }
        public bool VATPaymentSuspension { get; set; }
        public IList<InvoiceLineInput> InvoiceLines { get; set; } = new List<InvoiceLineInput>();
        // Before discount. NettoAmount - DiscountAmount is the taxable net total.
        public decimal NettoAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VATAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public InvoicePaymentType PaymentType { get; set; }
    }
    public sealed class InvoiceSeriesInput
    {
        public string Series { get; set; }
        public bool IsDeliveryNote { get; set; }
        public InvoiceTypeInput InvoiceType { get; set; }
    }
    public sealed class InvoiceTypeInput
    {
        public InvoiceMode InvoiceTypeMode { get; set; }
        public int MyDataDocumentKind { get; set; }
        public bool IsRetail { get; set; }
    }
    public sealed class TraderInput
    {
        public string TaxRegistrationId { get; set; }
        public string TaxRegistrationOffice { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public int Branch { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string StringField1 { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Job { get; set; }
    }
    public sealed class InvoiceLineInput
    {
        public SalesItemInput SalesItem { get; set; }
        public MeasurementUnitInput MeasurementUnit { get; set; }
        public string ItemDescription { get; set; }
        public string ItemCode { get; set; }
        public int? OfficialFuelCode { get; set; }
        public decimal Quantity { get; set; }
        // Before discount; the builder subtracts DiscountAmount exactly once.
        public decimal? NetAmount { get; set; }
        public decimal? VatAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal VATPercentage { get; set; }
    }
    public sealed class SalesItemInput
    {
        public int? OfficialFuelCode { get; set; }
        public string FactoryCode { get; set; }
        public int? VATExcemptionArticle { get; set; }
        public int SalesItemType { get; set; }
    }
    public sealed class MeasurementUnitInput { public int OfficialType { get; set; } }
    public sealed class CountryInput
    {
        public string CountryCode { get; set; }
        public string DisplayMember { get; set; }
        public bool IsEu { get; set; }
    }
    public sealed class CompanySettings
    {
        public string CompanyTaxId { get; set; }
        public string CompanyBranch { get; set; }
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyCity { get; set; }
        public string CompanyZipCode { get; set; }
        public string CompanyTaxOffice { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhone { get; set; }
        public string CompanyJob { get; set; }
        public int ProviderBankPaymentType { get; set; }
    }
    public sealed class TransmissionEvidence
    {
        public int Status { get; set; }
        public long? Mark { get; set; }
        public long? CancelationMark { get; set; }
        public long? CanceledByMark { get; set; }
    }
    public sealed class DeliveryContext
    {
        public string Vehicle { get; set; }
        // Fiscal wall-clock time, matching InvoiceInput.PrintedDateTime.
        public DateTime Departure { get; set; }
        public Address LoadingAddress { get; set; }
        public Address DeliveryAddress { get; set; }
    }
    public sealed class MovementContext
    {
        public string Role { get; set; }
        public string Vehicle { get; set; }
        public DateTime Departure { get; set; }
        public decimal Quantity { get; set; }
        public decimal Quantity15 { get; set; }
        public long? ParentMark { get; set; }
        public Address LoadingAddress { get; set; }
        public Address DeliveryAddress { get; set; }
    }
}
