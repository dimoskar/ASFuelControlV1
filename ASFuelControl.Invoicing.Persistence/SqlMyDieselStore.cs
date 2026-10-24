using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using Exedron.Invoicing.Core;

namespace ASFuelControl.Invoicing.Persistence
{
    public sealed class MyDieselCouponCaseRecord
    {
        public Guid CouponCaseId { get; set; }
        public Guid InvoiceId { get; set; }
        public string CouponCode { get; set; }
        public string BeneficiaryVat { get; set; }
        public string CompanyVat { get; set; }
        public FiscalEnvironment Environment { get; set; }
        public string CredentialProfileId { get; set; }
        public int ReservedBranchNum { get; set; }
        public decimal ExciseDiscountPerLitre { get; set; }
        public int? CurrentBranchNum { get; set; }
        public MyDieselCaseStage LocalStage { get; set; }
        public string RemoteStatus { get; set; }
        public bool? Redeemable { get; set; }
        public decimal? IssuedLitres { get; set; }
        public DateTime? CouponCreatedUtc { get; set; }
        public DateTime? ValidUntilUtc { get; set; }
        public string FuelType { get; set; }
        public int? FuelCode { get; set; }
        public bool LowBalance { get; set; }
        public string LowBalanceMessageEl { get; set; }
        public string InvoiceMark { get; set; }
        public Guid? MarkSourceInvoiceId { get; set; }
        public bool BlocksInvoice { get; set; }
        public bool BlocksCouponReuse { get; set; }
        public DateTime? LastCheckedUtc { get; set; }
        public long Revision { get; set; }
        public long FencingToken { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }

    public sealed class MyDieselAttemptRecord
    {
        public Guid AttemptId { get; set; }
        public Guid? OriginalAttemptId { get; set; }
        public MyDieselOperation Operation { get; set; }
        public MyDieselScope Scope { get; set; }
        public string RequestJson { get; set; }
        public string RequestSha256 { get; internal set; }
        public MyDieselOperationState State { get; set; }
        public long Revision { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public int? HttpStatus { get; set; }
        public string ResponseBody { get; set; }
        public string ErrorCode { get; set; }
        public string Diagnostic { get; set; }
        public string OperatorReason { get; set; }
    }

    public sealed class MyDieselReferenceCacheRecord
    {
        public FiscalEnvironment Environment { get; set; }
        public string CompanyVat { get; set; }
        public string CompanyJson { get; set; }
        public string CompanySha256 { get; internal set; }
        public DateTime? CompanyRefreshedUtc { get; set; }
        public string FuelTypesJson { get; set; }
        public string FuelTypesSha256 { get; internal set; }
        public DateTime? FuelTypesRefreshedUtc { get; set; }
        public string ContractVersion { get; set; }
    }

    public sealed class MyDieselHttpEvidenceRecord
    {
        public InvoiceRequestLog Request { get; set; }
        public InvoiceResponseLog Response { get; set; }
    }

    public sealed class SqlMyDieselStore
    {
        private readonly SqlFiscalDatabase db;
        private const string CaseColumns = "CouponCaseId,InvoiceId,CouponCode,BeneficiaryVat,CompanyVat,Environment,CredentialProfileId,ReservedBranchNum,CurrentBranchNum,LocalStage,RemoteStatus,Redeemable,IssuedLitres,CouponCreatedUtc,ValidUntilUtc,FuelType,FuelCode,LowBalance,LowBalanceMessageEl,InvoiceMark,MarkSourceInvoiceId,BlocksInvoice,BlocksCouponReuse,LastCheckedUtc,Revision,FencingToken,CreatedUtc,UpdatedUtc,ExciseDiscountPerLitre";
        internal const string InsertCase = @"INSERT dbo.MyDieselCouponCase
(CouponCaseId,InvoiceId,CouponCode,BeneficiaryVat,CompanyVat,Environment,CredentialProfileId,ReservedBranchNum,CurrentBranchNum,LocalStage,RemoteStatus,Redeemable,IssuedLitres,CouponCreatedUtc,ValidUntilUtc,FuelType,FuelCode,LowBalance,LowBalanceMessageEl,InvoiceMark,MarkSourceInvoiceId,BlocksInvoice,BlocksCouponReuse,LastCheckedUtc,Revision,FencingToken,CreatedUtc,UpdatedUtc,ExciseDiscountPerLitre)
VALUES(@case,@invoice,@code,@beneficiary,@company,@environment,@credential,@reservedBranch,@currentBranch,@stage,@remote,@redeemable,@litres,@couponCreated,@validUntil,@fuelType,@fuelCode,@lowBalance,@lowBalanceEl,@mark,@markInvoice,@blocksInvoice,@blocksCoupon,@lastChecked,0,@token,@created,@updated,@exciseRate);";
        internal const string UpdateCase = @"UPDATE dbo.MyDieselCouponCase SET CurrentBranchNum=@currentBranch,LocalStage=@stage,RemoteStatus=@remote,Redeemable=@redeemable,
IssuedLitres=@litres,CouponCreatedUtc=@couponCreated,ValidUntilUtc=@validUntil,FuelType=@fuelType,FuelCode=@fuelCode,LowBalance=@lowBalance,
LowBalanceMessageEl=@lowBalanceEl,InvoiceMark=@mark,MarkSourceInvoiceId=@markInvoice,BlocksInvoice=@blocksInvoice,BlocksCouponReuse=@blocksCoupon,
LastCheckedUtc=@lastChecked,Revision=Revision+1,FencingToken=@token,UpdatedUtc=@updated
WHERE CouponCaseId=@case AND Revision=@expected AND FencingToken<=@token;";
        internal const string ReadCase = "SELECT " + CaseColumns + " FROM dbo.MyDieselCouponCase WHERE CouponCaseId=@case;";
        internal const string ReadBlockingInvoice = "SELECT " + CaseColumns + " FROM dbo.MyDieselCouponCase WHERE InvoiceId=@invoice AND BlocksInvoice=1;";
        internal const string ReadLatestInvoice = "SELECT TOP 1 " + CaseColumns + " FROM dbo.MyDieselCouponCase WHERE InvoiceId=@invoice ORDER BY CreatedUtc DESC,CouponCaseId DESC;";
        internal const string ReadBlockingCoupon = "SELECT " + CaseColumns + " FROM dbo.MyDieselCouponCase WHERE CouponCode=@code AND BlocksCouponReuse=1;";
        internal const string ReadInvoiceAllowsNewCoupon = @"SELECT CASE WHEN EXISTS(
SELECT 1 FROM dbo.Invoice i
WHERE i.InvoiceId=@invoice
AND ISNULL(i.IsPrinted,0)=0
AND ISNULL(i.InvoiceSignature,'')=''
AND ISNULL(i.QRCodeData,'')=''
AND NOT EXISTS(SELECT 1 FROM dbo.MyDataInvoice m WHERE m.InvoiceId=i.InvoiceId))
THEN 1 ELSE 0 END;";
        internal const string ReadHttpEvidence = "SELECT RequestJson,ResponseJson FROM dbo.FiscalHttpExchange WHERE DocumentKind=6 AND DocumentId=@case ORDER BY StartedUtc,ExchangeId;";
        internal const string ReadPollCandidates = @"SELECT TOP (@maximum) CouponCaseId FROM dbo.MyDieselCouponCase
WHERE LocalStage IN ('Redeemed','VerificationPending')
AND RemoteStatus IN ('REDEEMED','VERIFICATION_FAILED','VERIFICATION_EXHAUSTED','VERIFICATION_INVALID')
AND (LastCheckedUtc IS NULL OR LastCheckedUtc<@cutoff)
ORDER BY ISNULL(LastCheckedUtc,'19000101'),CreatedUtc,CouponCaseId;";
        internal const string ReadCache = "SELECT Environment,CompanyVat,CompanyJson,CompanySha256,CompanyRefreshedUtc,FuelTypesJson,FuelTypesSha256,FuelTypesRefreshedUtc,ContractVersion FROM dbo.MyDieselReferenceCache WHERE Environment=@environment AND CompanyVat=@company;";
        internal const string UpsertCompanyCache = @"IF EXISTS(SELECT 1 FROM dbo.MyDieselReferenceCache WITH(UPDLOCK,HOLDLOCK) WHERE Environment=@environment AND CompanyVat=@company)
UPDATE dbo.MyDieselReferenceCache SET CompanyJson=@json,CompanySha256=@hash,CompanyRefreshedUtc=@refreshed,ContractVersion=@contract WHERE Environment=@environment AND CompanyVat=@company;
ELSE INSERT dbo.MyDieselReferenceCache(Environment,CompanyVat,CompanyJson,CompanySha256,CompanyRefreshedUtc,ContractVersion) VALUES(@environment,@company,@json,@hash,@refreshed,@contract);";
        internal const string UpsertFuelCache = @"IF EXISTS(SELECT 1 FROM dbo.MyDieselReferenceCache WITH(UPDLOCK,HOLDLOCK) WHERE Environment=@environment AND CompanyVat=@company)
UPDATE dbo.MyDieselReferenceCache SET FuelTypesJson=@json,FuelTypesSha256=@hash,FuelTypesRefreshedUtc=@refreshed,ContractVersion=@contract WHERE Environment=@environment AND CompanyVat=@company;
ELSE INSERT dbo.MyDieselReferenceCache(Environment,CompanyVat,FuelTypesJson,FuelTypesSha256,FuelTypesRefreshedUtc,ContractVersion) VALUES(@environment,@company,@json,@hash,@refreshed,@contract);";

        public SqlMyDieselStore(SqlFiscalDatabase database) { db = database ?? throw new ArgumentNullException(nameof(database)); }

        public IFiscalDocumentLease Acquire(Guid couponCaseId)
        {
            return db.Acquire(new FiscalDocumentReference(FiscalDocumentKind.MyDieselCoupon, couponCaseId));
        }

        public bool CanAttachNewCoupon(Guid invoiceId)
        {
            if (invoiceId == Guid.Empty) return false;
            using (var lease = db.EnterInvoice(invoiceId)) return InvoiceAllowsNewCoupon(lease);
        }

        public IDisposable AcquireInvoiceForNewCoupon(Guid invoiceId)
        {
            if (invoiceId == Guid.Empty) throw new ArgumentException("Invoice ID is required.", nameof(invoiceId));
            var lease = db.EnterInvoice(invoiceId);
            try
            {
                if (!InvoiceAllowsNewCoupon(lease))
                    throw new InvalidOperationException("Δεν μπορεί να προστεθεί κουπόνι myDiesel σε παραστατικό που έχει ήδη εκδοθεί ή εκτυπωθεί.");
                return lease;
            }
            catch { lease.Dispose(); throw; }
        }

        private bool InvoiceAllowsNewCoupon(IFiscalDocumentLease lease)
        {
            if (new SqlFiscalOwnershipStore(db).Owns(lease)) return false;
            return db.Locked(lease, (held, transaction) =>
            {
                using (var command = SqlFiscalDatabase.Command(held.Connection, transaction, ReadInvoiceAllowsNewCoupon,
                    "@invoice", held.Document.DocumentId))
                    return Convert.ToInt32(command.ExecuteScalar()) != 0;
            });
        }

        public void Create(IFiscalDocumentLease lease, MyDieselCouponCaseRecord value)
        {
            ValidateLease(lease, value == null ? Guid.Empty : value.CouponCaseId);
            ValidateCase(value, true);
            db.Locked(lease, (held, transaction) =>
            {
                value.Revision = 0; value.FencingToken = held.FencingToken;
                using (var command = CaseCommand(held.Connection, transaction, InsertCase, value)) command.ExecuteNonQuery();
                return true;
            });
        }

        public MyDieselCouponCaseRecord Load(IFiscalDocumentLease lease)
        {
            ValidateLease(lease, lease == null ? Guid.Empty : lease.Document.DocumentId);
            return db.Locked(lease, (held, transaction) => ReadOne(held.Connection, transaction, ReadCase, "@case", held.Document.DocumentId));
        }

        public MyDieselCouponCaseRecord FindBlockingByInvoice(Guid invoiceId)
        {
            if (invoiceId == Guid.Empty) throw new ArgumentException("Invoice ID is required.", nameof(invoiceId));
            using (var connection = db.Open()) return ReadOne(connection, null, ReadBlockingInvoice, "@invoice", invoiceId);
        }

        public MyDieselCouponCaseRecord FindLatestByInvoice(Guid invoiceId)
        {
            if (invoiceId == Guid.Empty) throw new ArgumentException("Invoice ID is required.", nameof(invoiceId));
            using (var connection = db.Open()) return ReadOne(connection, null, ReadLatestInvoice, "@invoice", invoiceId);
        }

        public MyDieselCouponCaseRecord FindBlockingByCoupon(string couponCode)
        {
            string normalized = MyDieselCouponIdentity.NormalizeCouponCode(couponCode);
            using (var connection = db.Open()) return ReadOne(connection, null, ReadBlockingCoupon, "@code", normalized);
        }

        public void Save(IFiscalDocumentLease lease, long expectedRevision, MyDieselCouponCaseRecord value)
        {
            ValidateLease(lease, value == null ? Guid.Empty : value.CouponCaseId);
            ValidateCase(value, false);
            db.Locked(lease, (held, transaction) =>
            {
                var old = ReadOne(held.Connection, transaction, ReadCase, "@case", value.CouponCaseId);
                if (old == null || old.Revision != expectedRevision || value.Revision != expectedRevision + 1 ||
                    old.InvoiceId != value.InvoiceId || old.CouponCode != value.CouponCode || old.BeneficiaryVat != value.BeneficiaryVat ||
                    old.CompanyVat != value.CompanyVat || old.Environment != value.Environment || old.CredentialProfileId != value.CredentialProfileId || old.ReservedBranchNum != value.ReservedBranchNum ||
                    old.ExciseDiscountPerLitre != value.ExciseDiscountPerLitre ||
                    old.CreatedUtc != value.CreatedUtc || !MyDieselCaseTransitions.CanTransition(old.LocalStage, value.LocalStage) ||
                    value.UpdatedUtc < old.UpdatedUtc)
                    throw new InvalidOperationException("Stale revision, changed coupon identity, or forbidden myDiesel transition.");
                value.FencingToken = held.FencingToken;
                using (var command = CaseCommand(held.Connection, transaction, UpdateCase, value))
                {
                    command.Parameters.AddWithValue("@expected", expectedRevision);
                    if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("myDiesel coupon revision conflict.");
                }
                return true;
            });
        }

        public MyDieselAttemptRecord BeginAttempt(IFiscalDocumentLease lease, MyDieselAttemptRecord attempt)
        {
            ValidateAttempt(attempt, true);
            ValidateLease(lease, lease == null ? Guid.Empty : lease.Document.DocumentId);
            var payload = Encoding.UTF8.GetBytes(attempt.RequestJson);
            var entry = Entry(attempt, payload);
            db.Write(lease, entry, null, true, _ => false);
            attempt.RequestSha256 = SqlFiscalDatabase.Hash(payload);
            return attempt;
        }

        public void SaveAttempt(IFiscalDocumentLease lease, long expectedRevision, MyDieselAttemptRecord attempt)
        {
            ValidateAttempt(attempt, false);
            ValidateLease(lease, lease == null ? Guid.Empty : lease.Document.DocumentId);
            var payload = Encoding.UTF8.GetBytes(attempt.RequestJson);
            bool blocking = attempt.State == MyDieselOperationState.Prepared || attempt.State == MyDieselOperationState.Sending ||
                attempt.State == MyDieselOperationState.Unknown;
            db.Write(lease, Entry(attempt, payload), expectedRevision, blocking, old =>
            {
                MyDieselAttemptRecord prior;
                try { prior = SqlFiscalDatabase.Read<MyDieselAttemptRecord>(old.Snapshot); }
                catch { return false; }
                return prior != null && MyDieselOperationTransitions.CanTransition(prior.State, attempt.State);
            });
            attempt.RequestSha256 = SqlFiscalDatabase.Hash(payload);
        }

        public IReadOnlyList<MyDieselAttemptRecord> LoadAttempts(IFiscalDocumentLease lease, Guid? attemptId = null, bool history = false)
        {
            ValidateLease(lease, lease == null ? Guid.Empty : lease.Document.DocumentId);
            return db.Load(lease, FiscalJournalFamily.MyDiesel.ToString(), attemptId, history).Select(entry =>
            {
                var value = SqlFiscalDatabase.Read<MyDieselAttemptRecord>(entry.Snapshot);
                value.RequestJson = Encoding.UTF8.GetString(entry.Payload);
                value.RequestSha256 = entry.Hash;
                value.OriginalAttemptId = entry.OriginalId;
                return value;
            }).ToArray();
        }

        public IReadOnlyList<MyDieselHttpEvidenceRecord> LoadHttpEvidence(Guid couponCaseId)
        {
            if (couponCaseId == Guid.Empty) throw new ArgumentException("Coupon case ID is required.", nameof(couponCaseId));
            var result = new List<MyDieselHttpEvidenceRecord>();
            using (var connection = db.Open())
            using (var command = SqlFiscalDatabase.Command(connection, null, ReadHttpEvidence, "@case", couponCaseId))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var request = SqlFiscalDatabase.Read<InvoiceRequestLog>(reader.GetString(0));
                    var response = reader.IsDBNull(1) ? null : SqlFiscalDatabase.Read<InvoiceResponseLog>(reader.GetString(1));
                    result.Add(new MyDieselHttpEvidenceRecord { Request = request, Response = response });
                }
            }
            return result;
        }

        public IReadOnlyList<Guid> LoadPollCandidates(DateTime cutoffUtc, int maximum)
        {
            if (cutoffUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Polling cutoff must be UTC.", nameof(cutoffUtc));
            if (maximum < 1 || maximum > 1000) throw new ArgumentOutOfRangeException(nameof(maximum));
            var result = new List<Guid>();
            using (var connection = db.Open())
            using (var command = SqlFiscalDatabase.Command(connection,null,ReadPollCandidates,"@maximum",maximum,"@cutoff",cutoffUtc))
            using (var reader = command.ExecuteReader()) while (reader.Read()) result.Add(reader.GetGuid(0));
            return result;
        }

        public void SaveCompanyCache(FiscalEnvironment environment, string companyVat, string json, DateTime refreshedUtc, string contractVersion)
        { SaveCache(environment, companyVat, json, refreshedUtc, contractVersion, true); }

        public void SaveFuelTypesCache(FiscalEnvironment environment, string companyVat, string json, DateTime refreshedUtc, string contractVersion)
        { SaveCache(environment, companyVat, json, refreshedUtc, contractVersion, false); }

        public MyDieselReferenceCacheRecord LoadCache(FiscalEnvironment environment, string companyVat)
        {
            ValidateCacheKey(environment, ref companyVat);
            using (var connection = db.Open())
            using (var command = SqlFiscalDatabase.Command(connection, null, ReadCache, "@environment", (int)environment, "@company", companyVat))
            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read()) return null;
                var value = new MyDieselReferenceCacheRecord {
                    Environment = (FiscalEnvironment)reader.GetByte(0), CompanyVat = reader.GetString(1).Trim(),
                    CompanyJson = DbString(reader, 2), CompanySha256 = DbString(reader, 3), CompanyRefreshedUtc = DbDate(reader, 4),
                    FuelTypesJson = DbString(reader, 5), FuelTypesSha256 = DbString(reader, 6), FuelTypesRefreshedUtc = DbDate(reader, 7),
                    ContractVersion = reader.GetString(8)
                };
                Verify(value.CompanyJson, value.CompanySha256, "company");
                Verify(value.FuelTypesJson, value.FuelTypesSha256, "fuel-types");
                return value;
            }
        }

        private void SaveCache(FiscalEnvironment environment, string companyVat, string json, DateTime refreshedUtc, string contractVersion, bool company)
        {
            ValidateCacheKey(environment, ref companyVat);
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Reference JSON is required.", nameof(json));
            if (refreshedUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Reference refresh time must be UTC.", nameof(refreshedUtc));
            if (string.IsNullOrWhiteSpace(contractVersion)) throw new ArgumentException("Contract version is required.", nameof(contractVersion));
            string hash = SqlFiscalDatabase.Hash(Encoding.UTF8.GetBytes(json));
            using (var connection = db.Open())
            using (var transaction = connection.BeginTransaction())
            using (var command = SqlFiscalDatabase.Command(connection, transaction, company ? UpsertCompanyCache : UpsertFuelCache,
                "@environment", (int)environment, "@company", companyVat, "@json", json, "@hash", hash,
                "@refreshed", refreshedUtc, "@contract", contractVersion.Trim()))
            { command.ExecuteNonQuery(); transaction.Commit(); }
        }

        private static SqlFiscalDatabase.JournalEntry Entry(MyDieselAttemptRecord attempt, byte[] payload)
        {
            return new SqlFiscalDatabase.JournalEntry {
                Id = attempt.AttemptId, OriginalId = attempt.OriginalAttemptId, Family = FiscalJournalFamily.MyDiesel.ToString(),
                Operation = attempt.Operation.ToString(), Scope = SqlFiscalDatabase.Json(attempt.Scope), Format = "myDiesel.erp.v1+json",
                Payload = payload, CreatedUtc = attempt.CreatedUtc, UpdatedUtc = attempt.UpdatedUtc, Revision = attempt.Revision,
                State = attempt.State.ToString(), Snapshot = SqlFiscalDatabase.Json(attempt)
            };
        }

        private static SqlCommand CaseCommand(SqlConnection connection, SqlTransaction transaction, string sql, MyDieselCouponCaseRecord value)
        {
            return SqlFiscalDatabase.Command(connection, transaction, sql,
                "@case", value.CouponCaseId, "@invoice", value.InvoiceId, "@code", value.CouponCode,
                "@beneficiary", value.BeneficiaryVat, "@company", value.CompanyVat, "@environment", (int)value.Environment, "@credential", value.CredentialProfileId,
                "@reservedBranch", value.ReservedBranchNum, "@currentBranch", value.CurrentBranchNum, "@stage", value.LocalStage.ToString(),
                "@remote", value.RemoteStatus, "@redeemable", value.Redeemable, "@litres", value.IssuedLitres,
                "@couponCreated", value.CouponCreatedUtc, "@validUntil", value.ValidUntilUtc, "@fuelType", value.FuelType,
                "@fuelCode", value.FuelCode, "@lowBalance", value.LowBalance, "@lowBalanceEl", value.LowBalanceMessageEl,
                "@mark", value.InvoiceMark, "@markInvoice", value.MarkSourceInvoiceId, "@blocksInvoice", value.BlocksInvoice,
                "@blocksCoupon", value.BlocksCouponReuse, "@lastChecked", value.LastCheckedUtc, "@token", value.FencingToken,
                "@created", value.CreatedUtc, "@updated", value.UpdatedUtc, "@exciseRate", value.ExciseDiscountPerLitre);
        }

        private static MyDieselCouponCaseRecord ReadOne(SqlConnection connection, SqlTransaction transaction, string sql, string parameter, object value)
        {
            using (var command = SqlFiscalDatabase.Command(connection, transaction, sql, parameter, value))
            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read()) return null;
                return new MyDieselCouponCaseRecord {
                    CouponCaseId = reader.GetGuid(0), InvoiceId = reader.GetGuid(1), CouponCode = reader.GetString(2),
                    BeneficiaryVat = reader.GetString(3).Trim(), CompanyVat = reader.GetString(4).Trim(), Environment = (FiscalEnvironment)reader.GetByte(5),
                    CredentialProfileId = reader.GetString(6), ReservedBranchNum = reader.GetInt32(7), CurrentBranchNum = DbInt(reader, 8),
                    LocalStage = (MyDieselCaseStage)Enum.Parse(typeof(MyDieselCaseStage), reader.GetString(9)), RemoteStatus = DbString(reader, 10),
                    Redeemable = DbBool(reader, 11), IssuedLitres = DbDecimal(reader, 12), CouponCreatedUtc = DbDate(reader, 13),
                    ValidUntilUtc = DbDate(reader, 14), FuelType = DbString(reader, 15), FuelCode = DbInt(reader, 16),
                    LowBalance = reader.GetBoolean(17), LowBalanceMessageEl = DbString(reader, 18), InvoiceMark = DbString(reader, 19),
                    MarkSourceInvoiceId = DbGuid(reader, 20), BlocksInvoice = reader.GetBoolean(21), BlocksCouponReuse = reader.GetBoolean(22),
                    LastCheckedUtc = DbDate(reader, 23), Revision = reader.GetInt64(24), FencingToken = reader.GetInt64(25),
                    CreatedUtc = SqlFiscalDatabase.Utc(reader.GetDateTime(26)), UpdatedUtc = SqlFiscalDatabase.Utc(reader.GetDateTime(27)),
                    ExciseDiscountPerLitre = reader.GetDecimal(28)
                };
            }
        }

        private static void ValidateLease(IFiscalDocumentLease lease, Guid caseId)
        {
            if (lease == null) throw new ArgumentNullException(nameof(lease));
            if (lease.Document.Kind != FiscalDocumentKind.MyDieselCoupon || caseId == Guid.Empty || lease.Document.DocumentId != caseId)
                throw new ArgumentException("A matching myDiesel coupon lease is required.", nameof(lease));
        }

        private static void ValidateCase(MyDieselCouponCaseRecord value, bool creating)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.CouponCaseId == Guid.Empty || value.InvoiceId == Guid.Empty) throw new ArgumentException("Coupon case and invoice IDs are required.");
            value.CouponCode = MyDieselCouponIdentity.NormalizeCouponCode(value.CouponCode);
            value.BeneficiaryVat = MyDieselCouponIdentity.NormalizeVat(value.BeneficiaryVat, "Beneficiary VAT");
            value.CompanyVat = MyDieselCouponIdentity.NormalizeVat(value.CompanyVat, "Company VAT");
            if (value.Environment != FiscalEnvironment.Test && value.Environment != FiscalEnvironment.Production) throw new ArgumentException("Invalid myDiesel environment.");
            if (string.IsNullOrWhiteSpace(value.CredentialProfileId) || value.CredentialProfileId.Length > 128) throw new ArgumentException("Credential profile ID is required.");
            if (value.ReservedBranchNum < 0 || value.CurrentBranchNum < 0) throw new ArgumentException("Branch numbers cannot be negative.");
            MyDieselTaxRules.NormalizeExciseDiscountPerLitre(value.ExciseDiscountPerLitre);
            if (value.LocalStage == MyDieselCaseStage.Unspecified || !Enum.IsDefined(typeof(MyDieselCaseStage), value.LocalStage)) throw new ArgumentException("A local myDiesel stage is required.");
            if (value.RemoteStatus != null && value.RemoteStatus.Length > 32) throw new ArgumentException("Remote status is too long.");
            if (value.IssuedLitres <= 0 || value.FuelCode <= 0) throw new ArgumentException("Litres and fuel code must be positive when present.");
            if (value.InvoiceMark != null) value.InvoiceMark = MyDieselRedeemRequest.NormalizeMark(value.InvoiceMark);
            if ((value.InvoiceMark == null) != !value.MarkSourceInvoiceId.HasValue) throw new ArgumentException("MARK and source invoice must be stored together.");
            if (value.LowBalance && string.IsNullOrWhiteSpace(value.LowBalanceMessageEl)) throw new ArgumentException("The Greek low-balance warning is required.");
            if (value.CreatedUtc.Kind != DateTimeKind.Utc || value.UpdatedUtc.Kind != DateTimeKind.Utc || value.UpdatedUtc < value.CreatedUtc)
                throw new ArgumentException("myDiesel case timestamps must be ordered UTC values.");
            if (creating && (value.LocalStage != MyDieselCaseStage.Captured || value.Revision != 0)) throw new ArgumentException("A new coupon case must be captured at revision zero.");
            var remote = MyDieselCouponStates.Parse(value.RemoteStatus);
            if ((value.LocalStage == MyDieselCaseStage.Released || remote == MyDieselCouponStatus.Expired || remote == MyDieselCouponStatus.Cancelled) &&
                (value.BlocksInvoice || value.BlocksCouponReuse)) throw new ArgumentException("Released or unavailable coupons cannot remain locally blocking.");
            if (value.LocalStage == MyDieselCaseStage.Final && !MyDieselCouponStates.IsFinal(remote)) throw new ArgumentException("A final local case requires a final remote status.");
            if (value.LocalStage == MyDieselCaseStage.Dismissed && (value.BlocksInvoice || value.BlocksCouponReuse ||
                !string.IsNullOrWhiteSpace(value.RemoteStatus) || !string.IsNullOrWhiteSpace(value.InvoiceMark) || value.MarkSourceInvoiceId.HasValue))
                throw new ArgumentException("Only a rejected, never-reserved coupon can be dismissed.");
        }

        private static void ValidateAttempt(MyDieselAttemptRecord value, bool creating)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value.AttemptId == Guid.Empty || value.Operation == MyDieselOperation.Unspecified || !Enum.IsDefined(typeof(MyDieselOperation), value.Operation) || value.Scope == null)
                throw new ArgumentException("Attempt identity, operation and scope are required.");
            if (string.IsNullOrWhiteSpace(value.RequestJson)) throw new ArgumentException("Exact request JSON is required.");
            if (value.State == MyDieselOperationState.Unspecified || !Enum.IsDefined(typeof(MyDieselOperationState), value.State)) throw new ArgumentException("Attempt state is required.");
            if (value.CreatedUtc.Kind != DateTimeKind.Utc || value.UpdatedUtc.Kind != DateTimeKind.Utc || value.UpdatedUtc < value.CreatedUtc)
                throw new ArgumentException("Attempt timestamps must be ordered UTC values.");
            if (creating && (value.State != MyDieselOperationState.Prepared || value.Revision != 0)) throw new ArgumentException("A new attempt must be prepared at revision zero.");
        }

        private static void ValidateCacheKey(FiscalEnvironment environment, ref string companyVat)
        {
            if (environment != FiscalEnvironment.Test && environment != FiscalEnvironment.Production) throw new ArgumentException("Invalid cache environment.");
            companyVat = MyDieselCouponIdentity.NormalizeVat(companyVat, "Company VAT");
        }

        private static void Verify(string json, string expected, string name)
        {
            if (json == null && expected == null) return;
            if (json == null || expected == null || SqlFiscalDatabase.Hash(Encoding.UTF8.GetBytes(json)) != expected)
                throw new InvalidDataException("myDiesel " + name + " cache fingerprint mismatch.");
        }

        private static string DbString(SqlDataReader reader, int ordinal) { return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal); }
        private static DateTime? DbDate(SqlDataReader reader, int ordinal) { return reader.IsDBNull(ordinal) ? (DateTime?)null : SqlFiscalDatabase.Utc(reader.GetDateTime(ordinal)); }
        private static int? DbInt(SqlDataReader reader, int ordinal) { return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal); }
        private static bool? DbBool(SqlDataReader reader, int ordinal) { return reader.IsDBNull(ordinal) ? (bool?)null : reader.GetBoolean(ordinal); }
        private static decimal? DbDecimal(SqlDataReader reader, int ordinal) { return reader.IsDBNull(ordinal) ? (decimal?)null : reader.GetDecimal(ordinal); }
        private static Guid? DbGuid(SqlDataReader reader, int ordinal) { return reader.IsDBNull(ordinal) ? (Guid?)null : reader.GetGuid(ordinal); }
    }
}
