using System;
using System.Linq;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ASFuelControl.Invoicing
{
    public interface IMyDieselClientFactory
    {
        IMyDieselClient Create(MyDieselCouponCaseRecord couponCase);
    }

    public sealed class MyDieselServiceResult
    {
        public MyDieselCouponCaseRecord CouponCase { get; internal set; }
        public Guid? AttemptId { get; internal set; }
        public MyDieselOperationState OperationState { get; internal set; }
        public string ErrorCode { get; internal set; }
        public bool RequiresIntervention { get { return CouponCase != null && CouponCase.LocalStage == MyDieselCaseStage.InterventionRequired; } }
    }

    // Owns lifecycle policy. The adapter only transports; the store owns durable evidence.
    public sealed class MyDieselCouponService
    {
        private readonly SqlMyDieselStore store;
        private readonly IMyDieselClientFactory clients;
        private readonly Func<DateTime> utcNow;

        public MyDieselCouponService(SqlFiscalDatabase database, IMyDieselClientFactory clientFactory, Func<DateTime> utcClock = null)
        {
            store = new SqlMyDieselStore(database ?? throw new ArgumentNullException(nameof(database)));
            clients = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            utcNow = utcClock ?? (() => DateTime.UtcNow);
        }

        public MyDieselCouponCaseRecord Capture(Guid invoiceId, MyDieselCouponIdentity identity, MyDieselScope scope,
            DateTime? validUntilUtc = null, decimal? issuedLitres = null,
            decimal exciseDiscountPerLitre = MyDieselTaxRules.DefaultExciseDiscountPerLitre)
        {
            if (invoiceId == Guid.Empty) throw new ArgumentException("Invoice ID is required.", nameof(invoiceId));
            if (identity == null) throw new ArgumentNullException(nameof(identity));
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            if (validUntilUtc.HasValue && validUntilUtc.Value.Kind != DateTimeKind.Utc) throw new ArgumentException("Coupon validity must be UTC.", nameof(validUntilUtc));
            if (issuedLitres <= 0) throw new ArgumentException("Issued litres must be positive when present.", nameof(issuedLitres));
            exciseDiscountPerLitre = MyDieselTaxRules.NormalizeExciseDiscountPerLitre(exciseDiscountPerLitre);
            using (store.AcquireInvoiceForNewCoupon(invoiceId))
            {
                var now = Now();
                var value = new MyDieselCouponCaseRecord {
                    CouponCaseId = Guid.NewGuid(), InvoiceId = invoiceId, CouponCode = identity.CouponCode,
                    BeneficiaryVat = identity.BeneficiaryVat, CompanyVat = scope.CompanyVat, Environment = scope.Environment,
                    CredentialProfileId = scope.CredentialProfileId, ReservedBranchNum = scope.BranchNum,
                    ExciseDiscountPerLitre = exciseDiscountPerLitre,
                    LocalStage = MyDieselCaseStage.Captured, ValidUntilUtc = validUntilUtc, IssuedLitres = issuedLitres,
                    BlocksInvoice = true, BlocksCouponReuse = true, CreatedUtc = now, UpdatedUtc = now
                };
                using (var lease = store.Acquire(value.CouponCaseId)) store.Create(lease, value);
                return value;
            }
        }

        public MyDieselCouponCaseRecord Capture(Guid invoiceId, MyDieselQrCoupon qr, MyDieselScope scope,
            decimal exciseDiscountPerLitre = MyDieselTaxRules.DefaultExciseDiscountPerLitre)
        {
            if (qr == null) throw new ArgumentNullException(nameof(qr));
            return Capture(invoiceId, qr.Identity, scope, qr.ValidUntilUtc.UtcDateTime, qr.IssuedLitres, exciseDiscountPerLitre);
        }

        public MyDieselServiceResult Reserve(Guid couponCaseId)
        {
            using (var lease = store.Acquire(couponCaseId))
            {
                var value = Required(store.Load(lease));
                if (value.LocalStage != MyDieselCaseStage.Captured && value.LocalStage != MyDieselCaseStage.ReserveUnknown)
                    throw new InvalidOperationException("Only a captured or reservation-unknown coupon can be reserved.");
                var request = new MyDieselLookupRequest(value.CouponCode, value.CompanyVat, value.ReservedBranchNum);
                MyDieselAttemptRecord retry = null;
                if (value.LocalStage == MyDieselCaseStage.ReserveUnknown)
                {
                    retry = store.LoadAttempts(lease).LastOrDefault(a => a.Operation == MyDieselOperation.Query && a.State == MyDieselOperationState.Unknown);
                    if (retry == null) throw new InvalidOperationException("Reservation is unknown but its blocking query evidence is missing.");
                }
                return Execute(lease, value, MyDieselOperation.Query, LookupJson(request), client => client.Query(request), ApplyReserve, retry);
            }
        }

        public MyDieselServiceResult Release(Guid couponCaseId)
        {
            using (var lease = store.Acquire(couponCaseId))
            {
                var value = Required(store.Load(lease));
                if (value.LocalStage != MyDieselCaseStage.Reserved || MyDieselCouponStates.Parse(value.RemoteStatus) != MyDieselCouponStatus.Reserved)
                    throw new InvalidOperationException("Only an authoritatively reserved coupon can be released.");
                var request = new MyDieselLookupRequest(value.CouponCode, value.CompanyVat, value.ReservedBranchNum);
                return Execute(lease, value, MyDieselOperation.Release, LookupJson(request), client => client.Release(request), ApplyRelease);
            }
        }

        public MyDieselCouponCaseRecord DismissRejected(Guid couponCaseId)
        {
            using (var lease = store.Acquire(couponCaseId))
            {
                var value = Required(store.Load(lease));
                var query = store.LoadAttempts(lease).LastOrDefault(a => a.Operation == MyDieselOperation.Query);
                if (!CanDismissRejected(value) || query == null || query.State != MyDieselOperationState.Rejected)
                    throw new InvalidOperationException("Only a definitively rejected, never-reserved coupon can be removed from an invoice.");
                value.LocalStage = MyDieselCaseStage.Dismissed;
                Advance(lease,value);
                return value;
            }
        }

        public static bool CanDismissRejected(MyDieselCouponCaseRecord value)
        {
            return value != null && value.LocalStage == MyDieselCaseStage.InterventionRequired &&
                !value.BlocksInvoice && !value.BlocksCouponReuse && string.IsNullOrWhiteSpace(value.RemoteStatus) &&
                string.IsNullOrWhiteSpace(value.InvoiceMark) && !value.MarkSourceInvoiceId.HasValue;
        }

        public MyDieselServiceResult Redeem(Guid couponCaseId, Guid invoiceId, string mark)
        {
            using (var lease = store.Acquire(couponCaseId))
            {
                var value = Required(store.Load(lease));
                if (value.InvoiceId != invoiceId) throw new ArgumentException("The accepted invoice does not own this coupon case.", nameof(invoiceId));
                if (value.LocalStage != MyDieselCaseStage.Reserved && value.LocalStage != MyDieselCaseStage.InvoiceAccepted)
                    throw new InvalidOperationException("Coupon redemption cannot be repeated from its current state.");
                mark = MyDieselRedeemRequest.NormalizeMark(mark);
                if (value.LocalStage == MyDieselCaseStage.Reserved)
                {
                    value.LocalStage = MyDieselCaseStage.InvoiceAccepted; value.InvoiceMark = mark; value.MarkSourceInvoiceId = invoiceId;
                    Advance(lease, value);
                }
                else if (value.InvoiceMark != mark || value.MarkSourceInvoiceId != invoiceId)
                    throw new InvalidOperationException("The accepted invoice MARK differs from the frozen redemption request.");
                var request = new MyDieselRedeemRequest(value.CouponCode, value.CompanyVat, value.ReservedBranchNum, mark);
                return Execute(lease, value, MyDieselOperation.Redeem, RedeemJson(request), client => client.Redeem(request), ApplyRedeem);
            }
        }

        public MyDieselServiceResult RedeemOrRecover(Guid couponCaseId, Guid invoiceId, string mark)
        {
            MyDieselCouponCaseRecord value;
            using (var lease = store.Acquire(couponCaseId)) value = Required(store.Load(lease));
            mark = MyDieselRedeemRequest.NormalizeMark(mark);
            if (value.InvoiceId != invoiceId) throw new ArgumentException("The accepted invoice does not own this coupon case.", nameof(invoiceId));
            if (value.LocalStage == MyDieselCaseStage.Reserved || value.LocalStage == MyDieselCaseStage.InvoiceAccepted)
                return Redeem(couponCaseId,invoiceId,mark);
            if (value.InvoiceMark != mark || value.MarkSourceInvoiceId != invoiceId)
                throw new InvalidOperationException("The accepted invoice MARK differs from the frozen myDiesel redemption.");
            if (value.LocalStage == MyDieselCaseStage.RedeemUnknown || value.LocalStage == MyDieselCaseStage.InterventionRequired)
                return RefreshStatus(couponCaseId,true);
            if (value.LocalStage == MyDieselCaseStage.Redeemed || value.LocalStage == MyDieselCaseStage.VerificationPending || value.LocalStage == MyDieselCaseStage.Final)
                return new MyDieselServiceResult { CouponCase = value,OperationState = MyDieselOperationState.Succeeded };
            throw new InvalidOperationException("The coupon is not reserved for this invoice.");
        }

        public MyDieselServiceResult RefreshStatus(Guid couponCaseId, bool explicitUnknownReconciliation = false)
        {
            using (var lease = store.Acquire(couponCaseId))
            {
                var value = Required(store.Load(lease));
                var status = MyDieselCouponStates.Parse(value.RemoteStatus);
                bool unknown = value.LocalStage == MyDieselCaseStage.RedeemUnknown || value.LocalStage == MyDieselCaseStage.InterventionRequired;
                if (!MyDieselCouponStates.SafeForBackgroundQuery(status) && !(explicitUnknownReconciliation && unknown))
                    throw new InvalidOperationException("Query could reserve this coupon; unattended refresh is not safe in its current state.");
                var request = new MyDieselLookupRequest(value.CouponCode, value.CompanyVat, value.CurrentBranchNum ?? value.ReservedBranchNum);
                return Execute(lease, value, MyDieselOperation.Query, LookupJson(request), client => client.Query(request), ApplyRefresh);
            }
        }

        public MyDieselServiceResult ChangeBranch(Guid couponCaseId, int newBranchNum, string operatorReason)
        {
            if (string.IsNullOrWhiteSpace(operatorReason)) throw new ArgumentException("A station-correction reason is required.", nameof(operatorReason));
            using (var lease = store.Acquire(couponCaseId))
            {
                var value = Required(store.Load(lease)); var status = MyDieselCouponStates.Parse(value.RemoteStatus);
                if (!MyDieselCouponStates.CanChangeBranch(status)) throw new InvalidOperationException("Station correction is not allowed for this coupon state.");
                var request = new MyDieselLookupRequest(value.CouponCode, value.CompanyVat, newBranchNum);
                return Execute(lease, value, MyDieselOperation.ChangeBranch, LookupJson(request), client => client.ChangeBranch(request),
                    (row, result) => { ApplyRedeem(row, result); if (result.Succeeded) row.CurrentBranchNum = newBranchNum; },null,null,operatorReason);
            }
        }

        public MyDieselServiceResult CorrectMark(Guid couponCaseId, Guid invoiceId, string mark, string operatorReason)
        {
            if (invoiceId == Guid.Empty) throw new ArgumentException("Source invoice ID is required.", nameof(invoiceId));
            if (string.IsNullOrWhiteSpace(operatorReason)) throw new ArgumentException("A MARK-correction reason is required.", nameof(operatorReason));
            mark = MyDieselRedeemRequest.NormalizeMark(mark);
            using (var lease = store.Acquire(couponCaseId))
            {
                var value = Required(store.Load(lease));
                if (value.InvoiceId != invoiceId) throw new ArgumentException("The accepted invoice does not own this coupon case.", nameof(invoiceId));
                var remote = MyDieselCouponStates.Parse(value.RemoteStatus);
                if (!MyDieselCouponStates.CanRedeem(remote) || remote == MyDieselCouponStatus.Reserved)
                    throw new InvalidOperationException("MARK correction is not allowed for this coupon state.");
                var original = store.LoadAttempts(lease).LastOrDefault(a => a.Operation == MyDieselOperation.Redeem);
                if (original == null) throw new InvalidOperationException("The original redemption attempt is missing.");
                var request = new MyDieselRedeemRequest(value.CouponCode,value.CompanyVat,value.CurrentBranchNum ?? value.ReservedBranchNum,mark);
                return Execute(lease,value,MyDieselOperation.Redeem,RedeemJson(request),client => client.Redeem(request),
                    (row,result) => {
                        if (result.State != MyDieselOperationState.FailedBeforeDispatch)
                        { row.InvoiceMark = mark; row.MarkSourceInvoiceId = invoiceId; }
                        ApplyRedeem(row,result);
                    },null,original.AttemptId,operatorReason);
            }
        }

        private MyDieselServiceResult Execute(IFiscalDocumentLease lease, MyDieselCouponCaseRecord value, MyDieselOperation operation,
            string requestJson, Func<IMyDieselClient, MyDieselCallResult<MyDieselCouponInfo>> send,
            Action<MyDieselCouponCaseRecord, MyDieselCallResult<MyDieselCouponInfo>> project, MyDieselAttemptRecord retry = null,
            Guid? originalAttemptId = null, string operatorReason = null)
        {
            MyDieselAttemptRecord attempt;
            long sendingRevision;
            if (retry == null)
            {
                var now = Now();
                attempt = new MyDieselAttemptRecord { AttemptId = Guid.NewGuid(), OriginalAttemptId = originalAttemptId,
                    Operation = operation, Scope = Scope(value), OperatorReason = operatorReason,
                    RequestJson = requestJson, State = MyDieselOperationState.Prepared, CreatedUtc = now, UpdatedUtc = now };
                store.BeginAttempt(lease, attempt); sendingRevision = 1;
                attempt.State = MyDieselOperationState.Sending; attempt.Revision = sendingRevision; attempt.UpdatedUtc = Now();
                store.SaveAttempt(lease, 0, attempt);
            }
            else
            {
                attempt = retry;
                if (attempt.Operation != operation || attempt.State != MyDieselOperationState.Unknown || attempt.RequestJson != requestJson ||
                    JsonConvert.SerializeObject(attempt.Scope) != JsonConvert.SerializeObject(Scope(value)))
                    throw new InvalidOperationException("The unknown reservation cannot be retried with changed request or scope.");
                sendingRevision = attempt.Revision + 1; attempt.State = MyDieselOperationState.Sending;
                attempt.Revision = sendingRevision; attempt.UpdatedUtc = Now(); attempt.HttpStatus = null; attempt.ResponseBody = null;
                attempt.ErrorCode = null; attempt.Diagnostic = null; store.SaveAttempt(lease, sendingRevision - 1, attempt);
            }

            MyDieselCallResult<MyDieselCouponInfo> result = null;
            IMyDieselClient client = null;
            try
            {
                client = clients.Create(value);
                if (client == null) throw new InvalidOperationException("The myDiesel client factory returned no client.");
            }
            catch (Exception ex)
            {
                result = Failure(operation, MyDieselOperationState.FailedBeforeDispatch, ex);
            }
            if (client != null)
            {
                using (client)
                {
                    try { result = send(client); }
                    catch (Exception ex) { result = Failure(operation, MyDieselOperationState.Unknown, ex); }
                }
            }
            result = ValidateIdentity(value, result);
            attempt.State = result.State; attempt.Revision = sendingRevision + 1; attempt.UpdatedUtc = Now(); attempt.HttpStatus = result.HttpStatus;
            attempt.ResponseBody = result.ResponseBody; attempt.ErrorCode = result.Error == null ? null : result.Error.Code;
            attempt.Diagnostic = result.Error == null ? result.CommunicationError : result.Error.Diagnostic;
            store.SaveAttempt(lease, sendingRevision, attempt);

            project(value, result); if (result.Succeeded) value.LastCheckedUtc = attempt.UpdatedUtc;
            value.UpdatedUtc = Now(); value.Revision++;
            store.Save(lease, value.Revision - 1, value);
            return new MyDieselServiceResult { CouponCase = value, AttemptId = attempt.AttemptId,
                OperationState = result.State, ErrorCode = attempt.ErrorCode };
        }

        private static MyDieselCallResult<MyDieselCouponInfo> ValidateIdentity(MyDieselCouponCaseRecord value, MyDieselCallResult<MyDieselCouponInfo> result)
        {
            if (result == null) return Failure(MyDieselOperation.Unspecified, MyDieselOperationState.Unknown, new InvalidOperationException("No myDiesel result was returned."));
            if (result.Succeeded && (result.Data == null || result.Data.Code != value.CouponCode))
                return new MyDieselCallResult<MyDieselCouponInfo> { Operation = result.Operation, State = MyDieselOperationState.Unknown,
                    HttpStatus = result.HttpStatus, ResponseBody = result.ResponseBody,
                    Error = new MyDieselError { HttpStatus = result.HttpStatus, Code = "RESPONSE_IDENTITY_MISMATCH", Diagnostic = "Response coupon does not match the request." } };
            return result;
        }

        private static void ApplyReserve(MyDieselCouponCaseRecord value, MyDieselCallResult<MyDieselCouponInfo> result)
        {
            if (result.State == MyDieselOperationState.FailedBeforeDispatch) return;
            if (result.State == MyDieselOperationState.Unknown) { value.LocalStage = MyDieselCaseStage.ReserveUnknown; value.BlocksInvoice = value.BlocksCouponReuse = true; return; }
            if (result.State == MyDieselOperationState.Rejected) { value.LocalStage = MyDieselCaseStage.InterventionRequired; value.BlocksInvoice = value.BlocksCouponReuse = false; return; }
            ApplyInfo(value, result.Data);
            var status = result.Data.KnownStatus;
            if (status == MyDieselCouponStatus.Reserved && result.Data.Redeemable)
            { value.LocalStage = MyDieselCaseStage.Reserved; value.CurrentBranchNum = value.ReservedBranchNum; value.BlocksInvoice = value.BlocksCouponReuse = true; }
            else if (status == MyDieselCouponStatus.Active)
            { value.LocalStage = MyDieselCaseStage.Released; value.BlocksInvoice = value.BlocksCouponReuse = false; }
            else if (status == MyDieselCouponStatus.Expired || status == MyDieselCouponStatus.Cancelled)
            { value.LocalStage = MyDieselCaseStage.Final; value.BlocksInvoice = value.BlocksCouponReuse = false; }
            else { value.LocalStage = MyDieselCaseStage.InterventionRequired; value.BlocksInvoice = value.BlocksCouponReuse = true; }
        }

        private static void ApplyRelease(MyDieselCouponCaseRecord value, MyDieselCallResult<MyDieselCouponInfo> result)
        {
            if (result.State == MyDieselOperationState.FailedBeforeDispatch) return;
            if (!result.Succeeded) { value.LocalStage = MyDieselCaseStage.InterventionRequired; return; }
            ApplyInfo(value, result.Data);
            if (result.Data.KnownStatus == MyDieselCouponStatus.Active)
            { value.LocalStage = MyDieselCaseStage.Released; value.BlocksInvoice = value.BlocksCouponReuse = false; }
            else if (result.Data.KnownStatus == MyDieselCouponStatus.Expired || result.Data.KnownStatus == MyDieselCouponStatus.Cancelled)
            { value.LocalStage = MyDieselCaseStage.Final; value.BlocksInvoice = value.BlocksCouponReuse = false; }
            else value.LocalStage = MyDieselCaseStage.InterventionRequired;
        }

        private static void ApplyRedeem(MyDieselCouponCaseRecord value, MyDieselCallResult<MyDieselCouponInfo> result)
        {
            if (result.State == MyDieselOperationState.FailedBeforeDispatch) return;
            if (result.State == MyDieselOperationState.Unknown) { value.LocalStage = MyDieselCaseStage.RedeemUnknown; return; }
            if (result.State == MyDieselOperationState.Rejected) { value.LocalStage = MyDieselCaseStage.InterventionRequired; return; }
            ApplyInfo(value, result.Data); ApplyRemoteStage(value, result.Data.KnownStatus);
        }

        private static void ApplyRefresh(MyDieselCouponCaseRecord value, MyDieselCallResult<MyDieselCouponInfo> result)
        {
            if (!result.Succeeded) { if (result.State != MyDieselOperationState.FailedBeforeDispatch) value.LocalStage = MyDieselCaseStage.InterventionRequired; return; }
            ApplyInfo(value, result.Data); ApplyRemoteStage(value, result.Data.KnownStatus);
        }

        private static void ApplyRemoteStage(MyDieselCouponCaseRecord value, MyDieselCouponStatus status)
        {
            if (status == MyDieselCouponStatus.Redeemed) value.LocalStage = MyDieselCaseStage.Redeemed;
            else if (status == MyDieselCouponStatus.VerificationFailed || status == MyDieselCouponStatus.VerificationExhausted || status == MyDieselCouponStatus.VerificationInvalid)
                value.LocalStage = MyDieselCaseStage.VerificationPending;
            else if (MyDieselCouponStates.IsFinal(status))
            {
                value.LocalStage = MyDieselCaseStage.Final;
                if (status == MyDieselCouponStatus.Expired || status == MyDieselCouponStatus.Cancelled) value.BlocksInvoice = value.BlocksCouponReuse = false;
            }
            else value.LocalStage = MyDieselCaseStage.InterventionRequired;
        }

        private static void ApplyInfo(MyDieselCouponCaseRecord value, MyDieselCouponInfo info)
        {
            value.RemoteStatus = info.CouponStatus; value.Redeemable = info.Redeemable; value.IssuedLitres = info.IssuedLitres;
            value.CouponCreatedUtc = info.CreatedUtc.UtcDateTime; value.ValidUntilUtc = info.ValidUntilUtc.UtcDateTime;
            value.FuelType = info.FuelType; value.FuelCode = info.FuelCode; value.LowBalance = info.LowBalance;
            value.LowBalanceMessageEl = info.LowBalanceMessage == null ? null : info.LowBalanceMessage.El;
        }

        private void Advance(IFiscalDocumentLease lease, MyDieselCouponCaseRecord value)
        {
            value.UpdatedUtc = Now(); value.Revision++; store.Save(lease, value.Revision - 1, value);
        }

        private DateTime Now()
        {
            var value = utcNow();
            if (value.Kind != DateTimeKind.Utc) throw new InvalidOperationException("The myDiesel clock must return UTC values.");
            return value;
        }

        private static MyDieselScope Scope(MyDieselCouponCaseRecord value)
        { return new MyDieselScope(value.Environment, value.CompanyVat, value.ReservedBranchNum, value.CredentialProfileId); }

        private static MyDieselCouponCaseRecord Required(MyDieselCouponCaseRecord value)
        { return value ?? throw new InvalidOperationException("The myDiesel coupon case does not exist."); }

        private static string LookupJson(MyDieselLookupRequest value)
        { return new JObject { ["couponCode"] = value.CouponCode, ["companyVat"] = value.CompanyVat, ["branchNum"] = value.BranchNum }.ToString(Formatting.None); }

        private static string RedeemJson(MyDieselRedeemRequest value)
        { return new JObject { ["couponCode"] = value.CouponCode, ["companyVat"] = value.CompanyVat, ["branchNum"] = value.BranchNum,
            ["invoiceType"] = value.InvoiceType, ["mark"] = value.Mark }.ToString(Formatting.None); }

        private static MyDieselCallResult<MyDieselCouponInfo> Failure(MyDieselOperation operation, MyDieselOperationState state, Exception error)
        { return new MyDieselCallResult<MyDieselCouponInfo> { Operation = operation, State = state, CommunicationError = error.GetType().FullName }; }
    }
}
