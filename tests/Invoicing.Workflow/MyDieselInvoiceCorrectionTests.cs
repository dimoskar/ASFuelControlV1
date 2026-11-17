using System;
using System.Reflection;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using Exedron.Invoicing.Core;

internal static partial class Program
{
    private static InvoiceWorkflowResult MyDieselCorrectionResult(
        bool accepted, MyDieselCaseStage? myDieselStage)
    {
        var result = new InvoiceWorkflowResult();
        Type resultType = typeof(InvoiceWorkflowResult);
        PropertyInfo fiscalState = resultType.GetProperty("FiscalState");
        fiscalState.SetValue(result, accepted ? "Accepted" : "Rejected", null);
        if (accepted)
        {
            PropertyInfo attemptId = resultType.GetProperty("AcceptedAttemptId");
            Guid acceptedAttemptId = Guid.NewGuid();
            attemptId.SetValue(result, (Guid?)acceptedAttemptId, null);
        }
        if (myDieselStage.HasValue)
        {
            PropertyInfo myDieselState = resultType.GetProperty("MyDieselState");
            string stage = myDieselStage.Value.ToString();
            myDieselState.SetValue(result, stage, null);
        }
        return result;
    }

    private static MyDieselServiceResult MyDieselCorrectionServiceResult(
        MyDieselOperationState operationState)
    {
        var result = new MyDieselServiceResult();
        Type resultType = typeof(MyDieselServiceResult);
        PropertyInfo stateProperty = resultType.GetProperty("OperationState");
        stateProperty.SetValue(result, operationState, null);
        return result;
    }

    private static void MyDieselInvoiceCorrectionTests()
    {
        Test("myDiesel credit route ignores credential-profile revisions but retains transmission identity",
            delegate
            {
                var original = new ProviderRoute(
                    InvoiceProviderCatalog.MyDataKey, false, "165368890", 0,
                    "6d392615cdd94522a2d8d130c84e783a",
                    "tsakalidisaxilleas");
                var current = new ProviderRoute(
                    InvoiceProviderCatalog.MyDataKey, false, "165368890", 0,
                    "a30f4848227d40e2914bf94d670922ef",
                    "tsakalidisaxilleas");
                bool sameRoute =
                    MyDieselInvoiceCorrectionPlan.SameCreditProviderRoute(
                        original, current);
                Check(sameRoute,
                    "A credential-profile revision changed the correction route.");

                var differentAccount = new ProviderRoute(
                    InvoiceProviderCatalog.MyDataKey, false, "165368890", 0,
                    "a30f4848227d40e2914bf94d670922ef",
                    "different-account");
                bool sameDifferentAccount =
                    MyDieselInvoiceCorrectionPlan.SameCreditProviderRoute(
                        original, differentAccount);
                Check(!sameDifferentAccount,
                    "A different provider account was treated as the same route.");
            });

        Test("myDiesel corrective invoices retain payment except card becomes credit",
            delegate
            {
                InvoicePaymentType[] sourcePayments =
                    (InvoicePaymentType[])Enum.GetValues(
                        typeof(InvoicePaymentType));
                foreach (InvoicePaymentType sourcePayment in sourcePayments)
                {
                    var plan = new MyDieselInvoiceCorrectionPlan
                    {
                        Original = new InvoiceWorkflowSnapshot
                        {
                            Invoice = new InvoiceBuildRequest
                            {
                                Invoice = new InvoiceInput
                                {
                                    PaymentType = sourcePayment
                                }
                            }
                        }
                    };
                    InvoicePaymentType expected = sourcePayment ==
                        InvoicePaymentType.Card ? InvoicePaymentType.Credit :
                        sourcePayment;
                    InvoicePaymentType actual = plan.CorrectivePaymentType();
                    Equal(expected, actual);
                }
            });

        Test("myDiesel correction does not credit when replacement is rejected",
            delegate
            {
                int corrections = 0;
                int credits = 0;
                string state = string.Empty;
                Func<InvoiceWorkflowResult> issueReplacement = delegate
                {
                    return MyDieselCorrectionResult(false, null);
                };
                Func<InvoiceWorkflowResult, MyDieselServiceResult>
                    correctCoupon = delegate(InvoiceWorkflowResult replacement)
                {
                    corrections++;
                    return MyDieselCorrectionServiceResult(
                        MyDieselOperationState.Succeeded);
                };
                Func<bool, InvoiceWorkflowResult> issueCredit =
                    delegate(bool creditReplacement)
                {
                    credits++;
                    return MyDieselCorrectionResult(true, null);
                };
                Action<bool> verify = delegate(bool couponOnReplacement)
                {
                    throw new InvalidOperationException(
                        "Verification must not run.");
                };
                Action<string, string> save = delegate(string savedState,
                    string message)
                {
                    state = savedState;
                };
                Func<bool> paused = delegate
                {
                    return false;
                };

                MyDieselInvoiceCorrectionRunner.Run(issueReplacement,
                    correctCoupon, issueCredit, verify, save, paused);

                Equal("ReplacementPending", state);
                Equal(0, corrections);
                Equal(0, credits);
            });

        Test("myDiesel correction rejection credits replacement and keeps coupon on original",
            delegate
            {
                bool? creditedReplacement = null;
                bool? couponOnReplacement = null;
                string state = string.Empty;
                Func<InvoiceWorkflowResult> issueReplacement = delegate
                {
                    return MyDieselCorrectionResult(true, null);
                };
                Func<InvoiceWorkflowResult, MyDieselServiceResult>
                    correctCoupon = delegate(InvoiceWorkflowResult replacement)
                {
                    return MyDieselCorrectionServiceResult(
                        MyDieselOperationState.Rejected);
                };
                Func<bool, InvoiceWorkflowResult> issueCredit =
                    delegate(bool replacement)
                {
                    creditedReplacement = replacement;
                    return MyDieselCorrectionResult(true, null);
                };
                Action<bool> verify = delegate(bool replacement)
                {
                    couponOnReplacement = replacement;
                };
                Action<string, string> save = delegate(string savedState,
                    string message)
                {
                    state = savedState;
                };
                Func<bool> paused = delegate
                {
                    return false;
                };

                MyDieselInvoiceCorrectionRunner.Run(issueReplacement,
                    correctCoupon, issueCredit, verify, save, paused);

                Equal("Completed", state);
                Equal(true, creditedReplacement);
                Equal(false, couponOnReplacement);
            });

        Test("myDiesel correction success credits original and verifies coupon on replacement",
            delegate
            {
                bool? creditedReplacement = null;
                bool? couponOnReplacement = null;
                string state = string.Empty;
                Func<InvoiceWorkflowResult> issueReplacement = delegate
                {
                    return MyDieselCorrectionResult(true, null);
                };
                Func<InvoiceWorkflowResult, MyDieselServiceResult>
                    correctCoupon = delegate(InvoiceWorkflowResult replacement)
                {
                    return MyDieselCorrectionServiceResult(
                        MyDieselOperationState.Succeeded);
                };
                Func<bool, InvoiceWorkflowResult> issueCredit =
                    delegate(bool replacement)
                {
                    creditedReplacement = replacement;
                    return MyDieselCorrectionResult(true, null);
                };
                Action<bool> verify = delegate(bool replacement)
                {
                    couponOnReplacement = replacement;
                };
                Action<string, string> save = delegate(string savedState,
                    string message)
                {
                    state = savedState;
                };
                Func<bool> paused = delegate
                {
                    return false;
                };

                MyDieselInvoiceCorrectionRunner.Run(issueReplacement,
                    correctCoupon, issueCredit, verify, save, paused);

                Equal("Completed", state);
                Equal(false, creditedReplacement);
                Equal(true, couponOnReplacement);
            });

        Test("myDiesel correction unknown outcome issues no credit",
            delegate
            {
                int credits = 0;
                string state = string.Empty;
                Func<InvoiceWorkflowResult> issueReplacement = delegate
                {
                    return MyDieselCorrectionResult(true, null);
                };
                Func<InvoiceWorkflowResult, MyDieselServiceResult>
                    correctCoupon = delegate(InvoiceWorkflowResult replacement)
                {
                    return MyDieselCorrectionServiceResult(
                        MyDieselOperationState.Unknown);
                };
                Func<bool, InvoiceWorkflowResult> issueCredit =
                    delegate(bool replacement)
                {
                    credits++;
                    return MyDieselCorrectionResult(true, null);
                };
                Action<bool> verify = delegate(bool replacement)
                {
                    throw new InvalidOperationException(
                        "Verification must not run.");
                };
                Action<string, string> save = delegate(string savedState,
                    string message)
                {
                    state = savedState;
                };
                Func<bool> paused = delegate
                {
                    return false;
                };

                MyDieselInvoiceCorrectionRunner.Run(issueReplacement,
                    correctCoupon, issueCredit, verify, save, paused);

                Equal("CouponCorrectionPending", state);
                Equal(0, credits);
            });

        Test("myDiesel correction reservation persists and protects both child invoices",
            delegate
            {
                Guid originalId = Guid.NewGuid();
                Guid creditId = Guid.NewGuid();
                Guid replacementId = Guid.NewGuid();
                Guid typeId = Guid.NewGuid();
                Guid traderId = Guid.NewGuid();
                Guid fuelTypeId = Guid.NewGuid();
                Guid userId = Guid.NewGuid();
                Guid couponCaseId = Guid.NewGuid();
                string insert =
                    "INSERT dbo.InvoiceType(InvoiceTypeId,LastNumber,DefaultSeries) VALUES('" +
                    typeId + "',70,'MD'); " +
                    "INSERT dbo.Invoice(InvoiceId,InvoiceTypeId,Number,Series,TransactionDate,TraderId,NettoAmount,VatAmount,TotalAmount,SupplyNumber,PaymentType,IsPrinted) VALUES('" +
                    originalId + "','" + typeId +
                    "',70,'MD','20261116','" + traderId +
                    "',100,24,124,'123456789',1,1); " +
                    "INSERT dbo.InvoiceLine(InvoiceId,InvoiceLineId,FuelTypeId,Volume,TotalPrice,VatAmount) VALUES('" +
                    originalId + "','" + Guid.NewGuid() + "','" +
                    fuelTypeId + "',50,124,24);";
                Execute(connection, insert);
                var store = new SqlMyDieselInvoiceCorrectionStore(database);
                Newtonsoft.Json.Linq.JObject frozenHost =
                    store.ReadHost(originalId);
                var record = new MyDieselInvoiceCorrectionRecord
                {
                    OriginalInvoiceId = originalId,
                    BatchId = Guid.NewGuid(),
                    CouponCaseId = couponCaseId,
                    CreditInvoiceId = creditId,
                    ReplacementInvoiceId = replacementId,
                    PlanJson = "{}",
                    CreatedBy = userId
                };
                using (IFiscalDocumentLease lease =
                    database.EnterInvoice(originalId))
                {
                    DateTime issuedAt = new DateTime(2026, 11, 16);
                    store.ReserveAndCreateChildren(lease, record, frozenHost,
                        typeId, "CR", "credit", typeId, "NEW",
                        "replacement", issuedAt);
                }

                Check(store.IsChild(creditId),
                    "credit child was not protected");
                Check(store.IsChild(replacementId),
                    "replacement child was not protected");
                MyDieselInvoiceCorrectionRecord saved = store.Load()[0];
                Equal(couponCaseId, saved.CouponCaseId);
                Equal("Prepared", saved.State);
                Newtonsoft.Json.Linq.JObject savedCredit =
                    store.ReadHost(creditId);
                Newtonsoft.Json.Linq.JObject savedReplacement =
                    store.ReadHost(replacementId);
                int creditNumber =
                    (int)savedCredit["Invoice"]["Number"];
                int replacementNumber =
                    (int)savedReplacement["Invoice"]["Number"];
                Equal(71, creditNumber);
                Equal(72, replacementNumber);
            });
    }
}
