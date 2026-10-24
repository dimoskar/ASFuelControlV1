using System;
using System.Collections.Generic;
using System.Linq;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;

internal static partial class Program
{
    private static void MyDieselServiceTests()
    {
        Test("myDiesel reservation retries the same unknown request and attempt", () => {
            var factory = new DieselFactory();
            factory.Results.Enqueue((operation, caseRow) => Unknown(operation));
            factory.Results.Enqueue((operation, caseRow) => Success(operation, caseRow, "RESERVED", true));
            var service = new MyDieselCouponService(database, factory);
            var row = service.Capture(DraftInvoice(), Identity("AYNY-1022-8625-7956-7356"), Scope(), null, null, 0.375m);
            Equal(0.375m, row.ExciseDiscountPerLitre);
            var first = service.Reserve(row.CouponCaseId); Equal(MyDieselOperationState.Unknown, first.OperationState);
            Equal(MyDieselCaseStage.ReserveUnknown, first.CouponCase.LocalStage);
            var second = service.Reserve(row.CouponCaseId); Equal(MyDieselOperationState.Succeeded, second.OperationState);
            Equal(first.AttemptId, second.AttemptId); Equal(MyDieselCaseStage.Reserved, second.CouponCase.LocalStage);
            using (var lease = new SqlMyDieselStore(database).Acquire(row.CouponCaseId))
            {
                var attempts = new SqlMyDieselStore(database).LoadAttempts(lease, first.AttemptId, true);
                Equal(5, attempts.Count); Equal(MyDieselOperationState.Succeeded, attempts.Last().State);
                Check(attempts.All(a => a.RequestJson == attempts[0].RequestJson), "Unknown retry changed its exact request.");
            }
            Equal(2, factory.Calls);
        });
        Test("myDiesel failure before dispatch remains captured and permits a clean retry", () => {
            var factory = new DieselFactory { FailCreate = true }; var service = new MyDieselCouponService(database, factory);
            var row = service.Capture(DraftInvoice(), Identity("ABEZ-1022-8625-7956-7356"), Scope());
            var failed = service.Reserve(row.CouponCaseId); Equal(MyDieselOperationState.FailedBeforeDispatch, failed.OperationState);
            Equal(MyDieselCaseStage.Captured, failed.CouponCase.LocalStage); Equal(0, factory.Calls);
            factory.FailCreate = false; factory.Results.Enqueue((operation, value) => Success(operation, value, "RESERVED", true));
            var accepted = service.Reserve(row.CouponCaseId); Equal(MyDieselCaseStage.Reserved, accepted.CouponCase.LocalStage);
            Check(accepted.AttemptId != failed.AttemptId, "A failed-before-dispatch attempt was overwritten.");
        });
        Test("myDiesel authoritative release clears both local uniqueness guards", () => {
            var factory = new DieselFactory();
            factory.Results.Enqueue((operation, caseRow) => Success(operation, caseRow, "RESERVED", true));
            factory.Results.Enqueue((operation, caseRow) => Success(operation, caseRow, "ACTIVE", true));
            var service = new MyDieselCouponService(database, factory);
            var row = service.Capture(DraftInvoice(), Identity("AHIK-1022-8625-7956-7356"), Scope());
            service.Reserve(row.CouponCaseId); var released = service.Release(row.CouponCaseId);
            Equal(MyDieselCaseStage.Released, released.CouponCase.LocalStage);
            Check(!released.CouponCase.BlocksInvoice && !released.CouponCase.BlocksCouponReuse, "Released coupon is still blocking.");
            Equal(null, new SqlMyDieselStore(database).FindBlockingByInvoice(row.InvoiceId));
        });
        Test("myDiesel redeem timeout never repeats redemption and reconciles with explicit query", () => {
            var factory = new DieselFactory();
            factory.Results.Enqueue((operation, caseRow) => Success(operation, caseRow, "RESERVED", true));
            factory.Results.Enqueue((operation, caseRow) => Unknown(operation));
            factory.Results.Enqueue((operation, caseRow) => Success(operation, caseRow, "REDEEMED", false));
            var service = new MyDieselCouponService(database, factory); var invoice = DraftInvoice();
            var row = service.Capture(invoice, Identity("AMNO-1022-8625-7956-7356"), Scope()); service.Reserve(row.CouponCaseId);
            var unknown = service.Redeem(row.CouponCaseId, invoice, "400001234567890");
            Equal(MyDieselCaseStage.RedeemUnknown, unknown.CouponCase.LocalStage);
            Throws(() => service.Redeem(row.CouponCaseId, invoice, "400001234567890"));
            var reconciled = service.RefreshStatus(row.CouponCaseId, true);
            Equal(MyDieselCaseStage.Redeemed, reconciled.CouponCase.LocalStage);
            Equal("400001234567890", reconciled.CouponCase.InvoiceMark); Equal(3, factory.Calls);
        });
        Test("myDiesel rejects a successful envelope for a different coupon identity", () => {
            var factory = new DieselFactory();
            factory.Results.Enqueue((operation, caseRow) => {
                var reply = Success(operation, caseRow, "RESERVED", true); reply.Data.Code = "APTY-1022-8625-7956-7356"; return reply;
            });
            var service = new MyDieselCouponService(database, factory);
            var row = service.Capture(DraftInvoice(), Identity("AOPX-1022-8625-7956-7356"), Scope());
            var result = service.Reserve(row.CouponCaseId);
            Equal(MyDieselOperationState.Unknown, result.OperationState); Equal(MyDieselCaseStage.ReserveUnknown, result.CouponCase.LocalStage);
            Equal("RESPONSE_IDENTITY_MISMATCH", result.ErrorCode);
        });
        Test("myDiesel administrator corrections retain reason and original redemption link", () => {
            var factory = new DieselFactory();
            factory.Results.Enqueue((operation, caseRow) => Success(operation, caseRow, "RESERVED", true));
            factory.Results.Enqueue((operation, caseRow) => Success(operation, caseRow, "REDEEMED", false));
            factory.Results.Enqueue((operation, caseRow) => Success(operation, caseRow, "REDEEMED", false));
            factory.Results.Enqueue((operation, caseRow) => Success(operation, caseRow, "REDEEMED", false));
            var service = new MyDieselCouponService(database,factory); var invoice = DraftInvoice();
            var row = service.Capture(invoice,Identity("ANTY-1022-8625-7956-7356"),Scope()); service.Reserve(row.CouponCaseId);
            var redeemed = service.Redeem(row.CouponCaseId,invoice,"400001234567890");
            bool missingReasonRejected = false;
            try { service.CorrectMark(row.CouponCaseId,invoice,"400001234567891",""); }
            catch (ArgumentException) { missingReasonRejected = true; }
            Check(missingReasonRejected,"MARK correction accepted an empty administrator reason.");
            var corrected = service.CorrectMark(row.CouponCaseId,invoice,"400001234567891","Διόρθωση αποδεκτού παραστατικού");
            Equal("400001234567891",corrected.CouponCase.InvoiceMark);
            var branch = service.ChangeBranch(row.CouponCaseId,7,"Διόρθωση επιλεγμένου πρατηρίου");
            Equal(7,branch.CouponCase.CurrentBranchNum);
            using (var lease = new SqlMyDieselStore(database).Acquire(row.CouponCaseId))
            {
                var attempts = new SqlMyDieselStore(database).LoadAttempts(lease);
                var redeems = attempts.Where(a => a.Operation == MyDieselOperation.Redeem).ToArray();
                Equal(2,redeems.Length); Equal(redeemed.AttemptId,redeems[1].OriginalAttemptId);
                Equal("Διόρθωση αποδεκτού παραστατικού",redeems[1].OperatorReason);
                Equal("Διόρθωση επιλεγμένου πρατηρίου",attempts.Last().OperatorReason);
            }
        });
        Test("accepted invoice redeems myDiesel before print and freezes the coupon in snapshot v2", () => {
            var source = new WorkflowSource(); var invoice = source.Source.Id;
            DraftInvoice(invoice);
            var factory = new DieselFactory();
            factory.Results.Enqueue((operation, caseRow) => Success(operation,caseRow,"RESERVED",true));
            factory.Results.Enqueue((operation, caseRow) => Success(operation,caseRow,"REDEEMED",false));
            var couponService = new MyDieselCouponService(database,factory);
            var scope = new MyDieselScope(FiscalEnvironment.Test,"000000000",0,"uat-fixture");
            var coupon = couponService.Capture(invoice,new MyDieselCouponIdentity("AEMN-1022-8625-7956-7356","111111111"),scope,null,null,0.375m);
            couponService.Reserve(coupon.CouponCaseId);
            source.Snapshot = new InvoiceWorkflowSnapshot { Version = 2,InvoiceId = invoice,Provider = Route(InvoiceProviderCatalog.MyDataKey),
                Invoice = source.Source.Prepare(invoice,Route(InvoiceProviderCatalog.MyDataKey)),PrintDestination = "test",PrintFormat = "test/v1",
                PrintContent = System.Text.Encoding.UTF8.GetBytes("invoice"),MyDieselCouponCaseId = coupon.CouponCaseId,MyDieselScope = scope,
                MyDieselExciseDiscountPerLitre = coupon.ExciseDiscountPerLitre };
            var http = new Http(invoice,InvoiceProviderCatalog.MyDataKey) {
                InvoicePost = body => Reply("<ResponseDoc><response><statusCode>Success</statusCode><invoiceMark>400001234567890</invoiceMark><invoiceUid>UID</invoiceUid></response></ResponseDoc>") };
            var printer = new Printer(); var workflow = Workflow(http,printer,myDiesel:new DieselCoordinator(couponService));
            var result = workflow.Process(invoice,source);
            Equal("Redeemed",result.MyDieselState); Equal("HandedOff",result.Print.State); Equal(1,printer.Calls);
            using (var lease = database.EnterInvoice(invoice))
            {
                var saved = Newtonsoft.Json.JsonConvert.DeserializeObject<InvoiceWorkflowSnapshot>(System.Text.Encoding.UTF8.GetString(new SqlInvoiceWorkflowStore(database).Load(lease)));
                Equal(2,saved.Version); Equal(coupon.CouponCaseId,saved.MyDieselCouponCaseId); Equal(0.375m,saved.MyDieselExciseDiscountPerLitre);
            }
        });
        Test("myDiesel rejects a new coupon after the invoice is printed", () => {
            var invoice = DraftInvoice(null,true);
            var service = new MyDieselCouponService(database,new DieselFactory());
            Throws(() => service.Capture(invoice,Identity("APTY-1022-8625-7956-7356"),Scope()));
            Check(!new SqlMyDieselStore(database).CanAttachNewCoupon(invoice),"Printed invoice still accepts a new coupon.");
        });
    }

    private static Guid DraftInvoice(Guid? requestedId = null, bool printed = false)
    {
        var invoice = requestedId ?? Guid.NewGuid(); var type = Guid.NewGuid();
        Execute(connection,"INSERT dbo.InvoiceType(InvoiceTypeId,LastNumber,DefaultSeries) VALUES('" + type + "',0,'MD'); " +
            "INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate,IsPrinted) VALUES('" + invoice + "','" + type + "',0,'MD','20261025'," + (printed ? "1" : "0") + ");");
        return invoice;
    }

    private static MyDieselCouponIdentity Identity(string code) { return new MyDieselCouponIdentity(code, "123456789"); }
    private static MyDieselScope Scope() { return new MyDieselScope(FiscalEnvironment.Test, "998877665", 1, "uat-fixture"); }
    private static MyDieselCallResult<MyDieselCouponInfo> Unknown(MyDieselOperation operation)
    { return new MyDieselCallResult<MyDieselCouponInfo> { Operation = operation, State = MyDieselOperationState.Unknown, CommunicationError = "fixture.timeout" }; }
    private static MyDieselCallResult<MyDieselCouponInfo> Success(MyDieselOperation operation, MyDieselCouponCaseRecord row, string status, bool redeemable)
    {
        return new MyDieselCallResult<MyDieselCouponInfo> { Operation = operation, State = MyDieselOperationState.Succeeded, HttpStatus = 200,
            ResponseBody = "{\"success\":true}", Data = new MyDieselCouponInfo { Code = row.CouponCode, CouponStatus = status,
                Redeemable = redeemable, CreatedAt = 1792648800, ValidUntil = 1792666800, IssuedLitres = 150m,
                FuelType = "DIESEL", FuelCode = 20, LowBalance = false } };
    }

    private sealed class DieselFactory : IMyDieselClientFactory
    {
        internal readonly Queue<Func<MyDieselOperation, MyDieselCouponCaseRecord, MyDieselCallResult<MyDieselCouponInfo>>> Results =
            new Queue<Func<MyDieselOperation, MyDieselCouponCaseRecord, MyDieselCallResult<MyDieselCouponInfo>>>();
        internal bool FailCreate; internal int Calls;
        public IMyDieselClient Create(MyDieselCouponCaseRecord couponCase)
        {
            if (FailCreate) throw new InvalidOperationException("Missing fixture credential.");
            return new DieselClient(this, couponCase);
        }
        private MyDieselCallResult<MyDieselCouponInfo> Next(MyDieselOperation operation, MyDieselCouponCaseRecord row)
        { Calls++; return Results.Dequeue()(operation, row); }
        private sealed class DieselClient : IMyDieselClient
        {
            private readonly DieselFactory owner; private readonly MyDieselCouponCaseRecord row;
            internal DieselClient(DieselFactory owner, MyDieselCouponCaseRecord row) { this.owner = owner; this.row = row; }
            public MyDieselCallResult<MyDieselCompany> GetCompany(string companyVat) { throw new NotSupportedException(); }
            public MyDieselCallResult<MyDieselFuelType[]> GetFuelTypes() { throw new NotSupportedException(); }
            public MyDieselCallResult<MyDieselCouponInfo> Query(MyDieselLookupRequest request) { return owner.Next(MyDieselOperation.Query, row); }
            public MyDieselCallResult<MyDieselCouponInfo> Redeem(MyDieselRedeemRequest request) { return owner.Next(MyDieselOperation.Redeem, row); }
            public MyDieselCallResult<MyDieselCouponInfo> Release(MyDieselLookupRequest request) { return owner.Next(MyDieselOperation.Release, row); }
            public MyDieselCallResult<MyDieselCouponInfo> ChangeBranch(MyDieselLookupRequest request) { return owner.Next(MyDieselOperation.ChangeBranch, row); }
            public void Dispose() { }
        }
    }

    private sealed class DieselCoordinator : IMyDieselRedemptionCoordinator
    {
        private readonly MyDieselCouponService service;
        internal DieselCoordinator(MyDieselCouponService service) { this.service = service; }
        public MyDieselServiceResult RedeemOrRecover(Guid couponCaseId, Guid invoiceId, string mark)
        { return service.RedeemOrRecover(couponCaseId,invoiceId,mark); }
    }
}
