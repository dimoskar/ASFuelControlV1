using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Exedron.Invoicing.Core
{
    // Stable persistence values. Unknown remote values are retained as text and map to UnknownValue.
    public enum MyDieselCouponStatus
    {
        UnknownValue = 0,
        Active = 1,
        Reserved = 2,
        Redeemed = 3,
        Verified = 4,
        VerificationFailed = 5,
        VerificationExhausted = 6,
        VerificationInvalid = 7,
        VerifiedPaid = 8,
        VerifiedUnpayable = 9,
        Expired = 10,
        Cancelled = 11
    }

    public enum MyDieselOperation
    {
        Unspecified = 0,
        CompanyLookup = 1,
        FuelTypes = 2,
        Query = 3,
        Redeem = 4,
        Release = 5,
        ChangeBranch = 6
    }

    // One persisted external-call attempt. Unknown is reconciled; it is never permission to resend blindly.
    public enum MyDieselOperationState
    {
        Unspecified = 0,
        Prepared = 1,
        Sending = 2,
        Succeeded = 3,
        Rejected = 4,
        Unknown = 5,
        FailedBeforeDispatch = 6
    }

    public enum MyDieselCaseStage
    {
        Unspecified = 0,
        Captured = 1,
        ReserveUnknown = 2,
        Reserved = 3,
        Released = 4,
        InvoiceAccepted = 5,
        RedeemUnknown = 6,
        Redeemed = 7,
        VerificationPending = 8,
        Final = 9,
        InterventionRequired = 10
    }

    public static class MyDieselCaseTransitions
    {
        public static bool CanTransition(MyDieselCaseStage from, MyDieselCaseStage to)
        {
            if (from == to) return from != MyDieselCaseStage.Unspecified;
            switch (from)
            {
                case MyDieselCaseStage.Captured:
                    return to == MyDieselCaseStage.ReserveUnknown || to == MyDieselCaseStage.Reserved ||
                        to == MyDieselCaseStage.Released || to == MyDieselCaseStage.InterventionRequired;
                case MyDieselCaseStage.ReserveUnknown:
                    return to == MyDieselCaseStage.Reserved || to == MyDieselCaseStage.Released ||
                        to == MyDieselCaseStage.InterventionRequired;
                case MyDieselCaseStage.Reserved:
                    return to == MyDieselCaseStage.Released || to == MyDieselCaseStage.InvoiceAccepted ||
                        to == MyDieselCaseStage.RedeemUnknown || to == MyDieselCaseStage.Redeemed ||
                        to == MyDieselCaseStage.InterventionRequired;
                case MyDieselCaseStage.InvoiceAccepted:
                    return to == MyDieselCaseStage.RedeemUnknown || to == MyDieselCaseStage.Redeemed ||
                        to == MyDieselCaseStage.InterventionRequired;
                case MyDieselCaseStage.RedeemUnknown:
                    return to == MyDieselCaseStage.Redeemed || to == MyDieselCaseStage.VerificationPending ||
                        to == MyDieselCaseStage.Final || to == MyDieselCaseStage.InterventionRequired;
                case MyDieselCaseStage.Redeemed:
                    return to == MyDieselCaseStage.VerificationPending || to == MyDieselCaseStage.Final ||
                        to == MyDieselCaseStage.InterventionRequired;
                case MyDieselCaseStage.VerificationPending:
                    return to == MyDieselCaseStage.Redeemed || to == MyDieselCaseStage.Final ||
                        to == MyDieselCaseStage.InterventionRequired;
                case MyDieselCaseStage.InterventionRequired:
                    return to == MyDieselCaseStage.Reserved || to == MyDieselCaseStage.Released ||
                        to == MyDieselCaseStage.RedeemUnknown || to == MyDieselCaseStage.Redeemed ||
                        to == MyDieselCaseStage.VerificationPending || to == MyDieselCaseStage.Final;
                default:
                    return false;
            }
        }
    }

    public static class MyDieselOperationTransitions
    {
        public static bool CanTransition(MyDieselOperationState from, MyDieselOperationState to)
        {
            if (!Enum.IsDefined(typeof(MyDieselOperationState), from) || !Enum.IsDefined(typeof(MyDieselOperationState), to)) return false;
            switch (from)
            {
                case MyDieselOperationState.Prepared:
                    return to == MyDieselOperationState.Sending || to == MyDieselOperationState.FailedBeforeDispatch;
                case MyDieselOperationState.Sending:
                    return to == MyDieselOperationState.Succeeded || to == MyDieselOperationState.Rejected ||
                        to == MyDieselOperationState.Unknown || to == MyDieselOperationState.FailedBeforeDispatch;
                case MyDieselOperationState.Unknown:
                    return to == MyDieselOperationState.Sending || to == MyDieselOperationState.Succeeded ||
                        to == MyDieselOperationState.Rejected || to == MyDieselOperationState.Unknown;
                default:
                    return false;
            }
        }
    }

    public sealed class MyDieselScope
    {
        public MyDieselScope(FiscalEnvironment environment, string companyVat, int branchNum, string credentialProfileId)
        {
            if (environment != FiscalEnvironment.Test && environment != FiscalEnvironment.Production)
                throw new ArgumentException("A myDiesel test or production environment is required.", nameof(environment));
            Environment = environment;
            CompanyVat = MyDieselCouponIdentity.NormalizeVat(companyVat, "Company VAT");
            if (branchNum < 0) throw new ArgumentOutOfRangeException(nameof(branchNum), "Branch number cannot be negative.");
            BranchNum = branchNum;
            CredentialProfileId = Required(credentialProfileId, "Credential profile ID");
        }

        public FiscalEnvironment Environment { get; }
        public string CompanyVat { get; }
        public int BranchNum { get; }
        public string CredentialProfileId { get; }

        private static string Required(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name + " is required.", nameof(value));
            return value.Trim();
        }
    }

    public sealed class MyDieselCouponIdentity
    {
        private const string AllowedLetters = "ABEZHIKMNOPTYX";
        private static readonly IDictionary<char, char> GreekToLatin = new Dictionary<char, char>
        {
            { 'Α', 'A' }, { 'Β', 'B' }, { 'Ε', 'E' }, { 'Ζ', 'Z' }, { 'Η', 'H' }, { 'Ι', 'I' }, { 'Κ', 'K' },
            { 'Μ', 'M' }, { 'Ν', 'N' }, { 'Ο', 'O' }, { 'Ρ', 'P' }, { 'Τ', 'T' }, { 'Υ', 'Y' }, { 'Χ', 'X' }
        };

        public MyDieselCouponIdentity(string couponCode, string beneficiaryVat)
        {
            CouponCode = NormalizeCouponCode(couponCode);
            BeneficiaryVat = NormalizeVat(beneficiaryVat, "Beneficiary VAT");
        }

        public string CouponCode { get; }
        public string BeneficiaryVat { get; }

        public static string NormalizeVat(string value, string name)
        {
            string vat = (value ?? "").Trim();
            if (vat.Length != 9 || vat.Any(c => c < '0' || c > '9'))
                throw new ArgumentException((name ?? "VAT") + " must contain exactly nine digits.", nameof(value));
            return vat;
        }

        public static string NormalizeCouponCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Coupon code is required.", nameof(value));
            var compact = new List<char>();
            foreach (char source in value.Trim())
            {
                if (char.IsWhiteSpace(source) || source == '-') continue;
                char upper = char.ToUpperInvariant(source);
                char mapped;
                compact.Add(GreekToLatin.TryGetValue(upper, out mapped) ? mapped : upper);
            }
            if (compact.Count != 20 || compact[0] != 'A' || compact.Skip(1).Take(3).Any(c => AllowedLetters.IndexOf(c) < 0) ||
                compact.Skip(4).Any(c => c < '0' || c > '9'))
                throw new ArgumentException("Coupon code must match AXXX-MMDD-NNNN-NNNN-NNNN.", nameof(value));
            string text = new string(compact.ToArray());
            return string.Join("-", Enumerable.Range(0, 5).Select(index => text.Substring(index * 4, 4)));
        }
    }

    // Parsed local capture. The raw QR text is deliberately not retained.
    public sealed class MyDieselQrCoupon
    {
        public MyDieselQrCoupon(MyDieselCouponIdentity identity, long validUntil, decimal issuedLitres)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            if (validUntil <= 0) throw new ArgumentOutOfRangeException(nameof(validUntil));
            if (issuedLitres <= 0 || decimal.Round(issuedLitres, 2) != issuedLitres)
                throw new ArgumentOutOfRangeException(nameof(issuedLitres), "Issued litres must be positive with at most two decimals.");
            ValidUntil = validUntil;
            IssuedLitres = issuedLitres;
        }
        public MyDieselCouponIdentity Identity { get; }
        public long ValidUntil { get; }
        public DateTimeOffset ValidUntilUtc => DateTimeOffset.FromUnixTimeSeconds(ValidUntil);
        public decimal IssuedLitres { get; }
    }

    public sealed class MyDieselLookupRequest
    {
        public MyDieselLookupRequest(string couponCode, string companyVat, int branchNum)
        {
            CouponCode = MyDieselCouponIdentity.NormalizeCouponCode(couponCode);
            CompanyVat = MyDieselCouponIdentity.NormalizeVat(companyVat, "Company VAT");
            if (branchNum < 0) throw new ArgumentOutOfRangeException(nameof(branchNum));
            BranchNum = branchNum;
        }
        public string CouponCode { get; }
        public string CompanyVat { get; }
        public int BranchNum { get; }
    }

    public sealed class MyDieselRedeemRequest
    {
        public MyDieselRedeemRequest(string couponCode, string companyVat, int branchNum, string mark)
        {
            CouponCode = MyDieselCouponIdentity.NormalizeCouponCode(couponCode);
            CompanyVat = MyDieselCouponIdentity.NormalizeVat(companyVat, "Company VAT");
            if (branchNum < 0) throw new ArgumentOutOfRangeException(nameof(branchNum));
            BranchNum = branchNum;
            Mark = NormalizeMark(mark);
        }
        public string CouponCode { get; }
        public string CompanyVat { get; }
        public int BranchNum { get; }
        public string InvoiceType => "INVOICE";
        public string Mark { get; }

        public static string NormalizeMark(string value)
        {
            string mark = (value ?? "").Trim();
            if (mark.Length != 15 || mark.Any(c => c < '0' || c > '9'))
                throw new ArgumentException("myDiesel MARK must contain exactly fifteen digits.", nameof(value));
            return mark;
        }
    }

    public sealed class MyDieselCompany
    {
        public string VatNumber { get; set; }
        public string CompanyName { get; set; }
        public bool Active { get; set; }
        public MyDieselGasStation[] GasStations { get; set; } = new MyDieselGasStation[0];
    }

    public sealed class MyDieselGasStation
    {
        public string Name { get; set; }
        public int BranchNum { get; set; }
        public string Amdika { get; set; }
        public string OilCompany { get; set; }
        public string Address { get; set; }
        public string Municipality { get; set; }
    }

    public sealed class MyDieselFuelType
    {
        public string Key { get; set; }
        public int Code { get; set; }
    }

    public sealed class MyDieselLocalizedMessage
    {
        public string En { get; set; }
        public string El { get; set; }
    }

    public sealed class MyDieselCouponInfo
    {
        public string Code { get; set; }
        public string CouponStatus { get; set; }
        public bool Redeemable { get; set; }
        public long CreatedAt { get; set; }
        public long ValidUntil { get; set; }
        public decimal IssuedLitres { get; set; }
        public string FuelType { get; set; }
        public int? FuelCode { get; set; }
        public bool LowBalance { get; set; }
        public MyDieselLocalizedMessage LowBalanceMessage { get; set; }

        public MyDieselCouponStatus KnownStatus => MyDieselCouponStates.Parse(CouponStatus);
        public DateTimeOffset CreatedUtc => UnixTime(CreatedAt, "createdAt");
        public DateTimeOffset ValidUntilUtc => UnixTime(ValidUntil, "validUntil");

        private static DateTimeOffset UnixTime(long seconds, string field)
        {
            try { return DateTimeOffset.FromUnixTimeSeconds(seconds); }
            catch (ArgumentOutOfRangeException ex) { throw new InvalidOperationException("Invalid myDiesel " + field + " value.", ex); }
        }
    }

    public static class MyDieselCouponStates
    {
        public static MyDieselCouponStatus Parse(string value)
        {
            switch ((value ?? "").Trim().ToUpperInvariant())
            {
                case "ACTIVE": return MyDieselCouponStatus.Active;
                case "RESERVED": return MyDieselCouponStatus.Reserved;
                case "REDEEMED": return MyDieselCouponStatus.Redeemed;
                case "VERIFIED": return MyDieselCouponStatus.Verified;
                case "VERIFICATION_FAILED": return MyDieselCouponStatus.VerificationFailed;
                case "VERIFICATION_EXHAUSTED": return MyDieselCouponStatus.VerificationExhausted;
                case "VERIFICATION_INVALID": return MyDieselCouponStatus.VerificationInvalid;
                case "VERIFIED_PAID": return MyDieselCouponStatus.VerifiedPaid;
                case "VERIFIED_UNPAYABLE": return MyDieselCouponStatus.VerifiedUnpayable;
                case "EXPIRED": return MyDieselCouponStatus.Expired;
                case "CANCELLED": return MyDieselCouponStatus.Cancelled;
                default: return MyDieselCouponStatus.UnknownValue;
            }
        }

        public static bool CanRedeem(MyDieselCouponStatus status)
        {
            return status == MyDieselCouponStatus.Reserved || status == MyDieselCouponStatus.Redeemed ||
                status == MyDieselCouponStatus.VerificationFailed || status == MyDieselCouponStatus.VerificationExhausted ||
                status == MyDieselCouponStatus.VerificationInvalid;
        }

        public static bool CanRelease(MyDieselCouponStatus status) => status == MyDieselCouponStatus.Reserved;
        public static bool CanChangeBranch(MyDieselCouponStatus status) => CanRedeem(status) && status != MyDieselCouponStatus.Reserved;

        public static bool IsFinal(MyDieselCouponStatus status)
        {
            return status == MyDieselCouponStatus.Verified || status == MyDieselCouponStatus.VerifiedPaid ||
                status == MyDieselCouponStatus.VerifiedUnpayable || status == MyDieselCouponStatus.Expired ||
                status == MyDieselCouponStatus.Cancelled;
        }

        // Query reserves ACTIVE coupons. Only these states are safe for unattended status polling.
        public static bool SafeForBackgroundQuery(MyDieselCouponStatus status)
        {
            return status == MyDieselCouponStatus.Redeemed || status == MyDieselCouponStatus.VerificationFailed ||
                status == MyDieselCouponStatus.VerificationExhausted || status == MyDieselCouponStatus.VerificationInvalid ||
                status == MyDieselCouponStatus.Verified || status == MyDieselCouponStatus.VerifiedPaid ||
                status == MyDieselCouponStatus.VerifiedUnpayable;
        }
    }

    public sealed class MyDieselError
    {
        public int? HttpStatus { get; set; }
        public string Code { get; set; }
        public string Path { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public string Diagnostic { get; set; }
    }

    public sealed class MyDieselCallResult<T>
    {
        public MyDieselOperation Operation { get; set; }
        public MyDieselOperationState State { get; set; }
        public int? HttpStatus { get; set; }
        public T Data { get; set; }
        public MyDieselError Error { get; set; }
        public string ResponseBody { get; set; }
        public string CommunicationError { get; set; }
        public bool Succeeded => State == MyDieselOperationState.Succeeded;
    }

    public interface IMyDieselClient : IDisposable
    {
        MyDieselCallResult<MyDieselCompany> GetCompany(string companyVat);
        MyDieselCallResult<MyDieselFuelType[]> GetFuelTypes();
        MyDieselCallResult<MyDieselCouponInfo> Query(MyDieselLookupRequest request);
        MyDieselCallResult<MyDieselCouponInfo> Redeem(MyDieselRedeemRequest request);
        MyDieselCallResult<MyDieselCouponInfo> Release(MyDieselLookupRequest request);
        MyDieselCallResult<MyDieselCouponInfo> ChangeBranch(MyDieselLookupRequest request);
    }
}
