using Exedron.Invoicing.Core;
using Exedron.Invoicing.Core.Models;
using System;
using System.Globalization;
using System.Linq;

namespace Exedron.Invoicing.Adapters
{
    /// <summary>Compatibility POS entry point. Explicit reversals belong to the durable host workflow.</summary>
    public sealed class GBridgePOSSupport : IPOSSupport
    {
        private readonly IPaymentGateway paymentGateway;
        private readonly IProviderPaymentAttemptStore paymentAttemptStore;

        public GBridgePOSSupport(IPaymentGateway paymentGateway, IProviderPaymentAttemptStore paymentAttemptStore = null)
        {
            this.paymentGateway = paymentGateway;
            this.paymentAttemptStore = paymentAttemptStore;
        }

        public InvoiceResponse SendInvoiceWithPOS(IInvoiceProvider provider, InvoiceModel invoice, Guid invoiceId)
        {
            var lifecycle = provider as IInvoiceProviderPaymentLifecycle;
            try
            {
                using (lifecycle == null ? null : lifecycle.AcquirePayment(invoiceId))
                    return SendInvoiceWithPOSCore(provider, invoice, invoiceId);
            }
            catch (Exception ex) { return Error(ex.Message); }
        }

        private InvoiceResponse SendInvoiceWithPOSCore(IInvoiceProvider provider, InvoiceModel invoice, Guid invoiceId)
        {
            IInvoiceProviderPaymentSigner signer = provider as IInvoiceProviderPaymentSigner;
            var lifecycle = provider as IInvoiceProviderPaymentLifecycle;

            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (!HasPosPayment(invoice)) return provider.SendInvoice(invoice, invoiceId, null);
            if (paymentAttemptStore == null)
                return Error("POS requires host-owned durable payment-attempt storage.");
            if (signer == null)
                return Error("The selected invoice provider does not support the signature required by GBridge.");

            if (!provider.Descriptor.AadeProviderId.HasValue)
            {
                return Error("The selected invoice provider is not mapped to a GBridge provider ID.");
            }
            if (paymentGateway == null)
            {
                return Error("POS/IRIS invoices require GBridge to be selected in POS configuration.");
            }

            string gatewayError = paymentGateway.ValidateConfiguration();
            if (!string.IsNullOrWhiteSpace(gatewayError))
            {
                return Error(gatewayError);
            }

            PaymentMethod[] posPayments = invoice.PaymentMethods.Where(IsPosPayment).ToArray();
            if (posPayments.Length != 1)
            {
                return Error("An invoice using GBridge must contain exactly one POS or IRIS payment method.");
            }
            PaymentMethod sourcePayment = posPayments[0];
            int paymentType = Convert.ToInt32(sourcePayment.Type, CultureInfo.InvariantCulture) == 8 ? 1 : 0;
            ProviderPaymentOperation operation = IsRefundInvoice(invoice)
                ? ProviderPaymentOperation.Refund
                : ProviderPaymentOperation.Sale;
            if (operation == ProviderPaymentOperation.Refund)
                return Error("Refunds require an explicit original payment reference through the durable host workflow.");
            decimal paymentAmount = Math.Abs(sourcePayment.Amount);
            int amountMinorUnits;
            try
            {
                amountMinorUnits = checked((int)decimal.Round(
                    paymentAmount * 100m,
                    0,
                    MidpointRounding.AwayFromZero));
            }
            catch (OverflowException)
            {
                return Error("The POS payment amount is outside the range supported by GBridge.");
            }
            paymentAmount = amountMinorUnits / 100m;

            ProviderPaymentAttempt attempt = null;
            string transactionUid = invoiceId.ToString("N", CultureInfo.InvariantCulture);
            ProviderPaymentContext payment = null;
            try
            {
                if (paymentAttemptStore != null)
                {
                    var prior = (paymentAttemptStore as IProviderPaymentAttemptControl)?.LoadLatest(invoiceId);
                    if (prior != null && new[] { "Declined", "Failed", "Voided", "SignaturePending" }.Contains(prior.State))
                        return Error("Continue this payment through the durable host workflow; no automatic payment retry is permitted.");
                    var attemptRequest = new ProviderPaymentAttemptRequest
                    {
                        InvoiceId = invoiceId,
                        Gateway = "GBridge",
                        InvoiceProvider = provider.Descriptor.Key,
                        ProviderId = provider.Descriptor.AadeProviderId.Value,
                        Operation = operation,
                        PaymentType = paymentType,
                        TerminalId = paymentGateway.TerminalId,
                        Amount = paymentAmount,
                        AmountMinorUnits = amountMinorUnits,
                        CurrencyCode = 978
                    };
                    attempt = paymentAttemptStore.GetOrCreate(attemptRequest);
                    transactionUid = attempt.TransactionUid;

                    if (attempt.State == ProviderPaymentAttemptStates.Mismatch)
                    {
                        return Error("The previous GBridge response did not match the payment request. Manual reconciliation is required.");
                    }
                    if (attempt.State == ProviderPaymentAttemptStates.VoidPending ||
                        attempt.State == ProviderPaymentAttemptStates.VoidFailed)
                    {
                        InvoiceResponse voidError = CompleteOutstandingVoid(attempt);
                        if (voidError != null)
                        {
                            return voidError;
                        }

                        attempt = paymentAttemptStore.GetOrCreate(attemptRequest);
                        transactionUid = attempt.TransactionUid;
                    }
                }

                ProviderPaymentSignature signature;
                if (lifecycle != null)
                {
                    bool mayAuthorize = attempt == null || attempt.State == ProviderPaymentAttemptStates.Prepared ||
                        attempt.State == ProviderPaymentAttemptStates.SignatureReady;
                    signature = lifecycle.PreparePaymentSignature(invoice, invoiceId, mayAuthorize);
                    if (paymentAttemptStore != null && mayAuthorize)
                    {
                        paymentAttemptStore.SaveSignature(attempt.Id, signature);
                        attempt.ProviderUid = signature.ProviderUid;
                        attempt.ProviderInput = signature.ProviderInput;
                        attempt.ProviderSignature = signature.ProviderSignature;
                        attempt.State = ProviderPaymentAttemptStates.SignatureReady;
                    }
                }
                else if (attempt != null &&
                    !string.IsNullOrWhiteSpace(attempt.ProviderInput) &&
                    !string.IsNullOrWhiteSpace(attempt.ProviderSignature))
                {
                    signature = new ProviderPaymentSignature
                    {
                        SourcePayment = sourcePayment,
                        ProviderUid = attempt.ProviderUid,
                        ProviderInput = attempt.ProviderInput,
                        ProviderSignature = attempt.ProviderSignature
                    };
                }
                else
                {
                    (paymentAttemptStore as IProviderPaymentWorkflowStore)?.MarkSignaturePending(attempt.Id);
                    var diagnosticSigner = provider as ITimologiaInvoiceProvider;
                    var eventSink = paymentAttemptStore as IPaymentGatewayEventSink;
                    signature = diagnosticSigner != null && eventSink != null && attempt != null
                        ? diagnosticSigner.CreatePaymentSignature(invoice, exchange => {
                            exchange.TransactionUid = transactionUid;
                            eventSink.Record(exchange);
                        })
                        : signer.CreatePaymentSignature(invoice);
                    if (paymentAttemptStore != null)
                    {
                        paymentAttemptStore.SaveSignature(attempt.Id, signature);
                        attempt.ProviderUid = signature.ProviderUid;
                        attempt.ProviderInput = signature.ProviderInput;
                        attempt.ProviderSignature = signature.ProviderSignature;
                        attempt.State = ProviderPaymentAttemptStates.SignatureReady;
                    }
                }

                PaymentAuthorizationResult authorization;
                bool paymentWasAlreadyInvoiced = attempt != null &&
                    attempt.State == ProviderPaymentAttemptStates.Invoiced;
                bool recoverPaymentOnly = attempt != null &&
                    (attempt.State == ProviderPaymentAttemptStates.Pending ||
                     attempt.State == ProviderPaymentAttemptStates.Unknown);
                if (attempt != null &&
                    (attempt.State == ProviderPaymentAttemptStates.Approved || paymentWasAlreadyInvoiced))
                {
                    authorization = AuthorizationFromAttempt(attempt);
                }
                else
                {
                    var ilydaSigner = provider as IlydaInvoiceProvider;
                    string gatewaySignature = ilydaSigner == null || recoverPaymentOnly
                        ? signature.ProviderSignature
                        : ilydaSigner.GBridgeSignature(invoiceId, signature.ProviderSignature);
                    if (paymentAttemptStore != null)
                    {
                        paymentAttemptStore.MarkPending(attempt.Id);
                    }

                    try
                    {
                        authorization = paymentGateway.Authorize(
                            new PaymentAuthorizationRequest
                            {
                                TransactionUid = transactionUid,
                                Operation = operation,
                                PaymentType = paymentType,
                                ProviderId = provider.Descriptor.AadeProviderId.Value,
                                ProviderInput = signature.ProviderInput,
                                ProviderSignature = gatewaySignature,
                                ExpectedTerminalId = attempt == null ? null : attempt.TerminalId,
                                ExpectedAmountMinorUnits = amountMinorUnits,
                                RecoverOnly = recoverPaymentOnly
                            });
                    }
                    catch (GBridgeException ex)
                    {
                        if (paymentAttemptStore != null)
                        {
                            string state = ex.FailureKind == GBridgeFailureKind.Unknown ||
                                (lifecycle != null && ex.FailureKind == GBridgeFailureKind.Failed)
                                ? ProviderPaymentAttemptStates.Unknown
                                : ex.FailureKind == GBridgeFailureKind.Mismatch
                                    ? ProviderPaymentAttemptStates.Mismatch
                                    : ProviderPaymentAttemptStates.Declined;
                            paymentAttemptStore.MarkFailure(attempt.Id, state, ex.FailureKind.ToString(), ex.Message);
                        }
                        if (lifecycle != null && (ex.FailureKind == GBridgeFailureKind.Declined ||
                            ex.FailureKind == GBridgeFailureKind.SignatureRejected || ex.FailureKind == GBridgeFailureKind.CredentialsRejected))
                            lifecycle.CancelUnusedPaymentSignature(invoiceId, "TRANSACTION_FAILED");
                        var iTimologia = provider as ITimologiaInvoiceProvider;
                        if (ex.FailureKind == GBridgeFailureKind.SignatureRejected && iTimologia != null)
                            throw new GBridgeException(ex.Message + " Active iTimologia signature format: acq_id=" +
                                iTimologia.PosAcquirerId + (iTimologia.PosSignatureFormat == 1
                                ? " (EDPS / Viva). Check the signed values and provider/POS signing environment."
                                : " (Mellon / ePay / Cardlink / Nexi). For EDPS, select EDPS / Viva and save the provider settings before Resend."),
                                ex.FailureKind, ex);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        if (paymentAttemptStore != null)
                        {
                            paymentAttemptStore.MarkFailure(
                                attempt.Id,
                                lifecycle == null ? ProviderPaymentAttemptStates.Failed : ProviderPaymentAttemptStates.Unknown,
                                ex.GetType().Name,
                                ex.Message);
                        }
                        throw;
                    }

                    if (paymentAttemptStore != null)
                    {
                        paymentAttemptStore.MarkApproved(attempt.Id, authorization);
                        ApplyAuthorization(attempt, authorization);
                        attempt.State = ProviderPaymentAttemptStates.Approved;
                    }
                }

                payment = new ProviderPaymentContext
                {
                    SourcePayment = signature.SourcePayment,
                    ProviderUid = signature.ProviderUid,
                    ProviderSignature = signature.ProviderSignature,
                    TransactionId = authorization.TransactionId,
                    TerminalId = authorization.TerminalId
                };

                InvoiceResponse response = provider.SendInvoice(invoice, invoiceId, payment);
                if (response != null && !response.HasErrors && paymentAttemptStore != null)
                {
                    paymentAttemptStore.MarkInvoiceSubmitted(attempt.Id);
                }
                // Ilyda may have queued/issued the invoice even when the response is uncertain.
                // Retain the approved payment for recovery; never void or charge it again here.
                if (lifecycle != null) return response;
                return CompleteAttempt(response, payment, transactionUid, attempt, paymentWasAlreadyInvoiced);
            }
            catch (Exception ex)
            {
                if (lifecycle != null) return Error(ex.Message);
                return CompleteAttempt(Error(ex.Message), payment, transactionUid, attempt, false);
            }
        }

        public void Dispose()
        {
            paymentGateway?.Dispose();
        }

        private InvoiceResponse CompleteAttempt(
            InvoiceResponse response,
            ProviderPaymentContext payment,
            string transactionUid,
            ProviderPaymentAttempt attempt,
            bool paymentWasAlreadyInvoiced)
        {
            if (payment == null || (response != null && !response.HasErrors))
            {
                return response;
            }

            if (paymentWasAlreadyInvoiced)
            {
                response.Errors = Append(
                    response.Errors,
                    "The payment was already attached to this invoice and was not voided.");
                return response;
            }

            if (response == null)
            {
                response = Error("The invoice provider returned no response after payment approval.");
            }

            response.Errors = Append(response.Errors,
                "The approved payment is retained. Reconcile invoice issuance before requesting an explicit void or refund.");
            return response;
        }

        private InvoiceResponse CompleteOutstandingVoid(ProviderPaymentAttempt attempt)
        {
            return Error("A previous void is unresolved. Use the explicit lookup-only void recovery workflow; no new payment was sent.");
        }
        private static PaymentAuthorizationResult AuthorizationFromAttempt(ProviderPaymentAttempt attempt)
        {
            return new PaymentAuthorizationResult
            {
                TransactionId = attempt.TransactionId,
                TerminalId = FirstNonEmpty(attempt.ResponseTerminalId, attempt.TerminalId),
                Rrn = attempt.Rrn,
                AuthorizationCode = attempt.AuthorizationCode,
                AmountMinorUnits = attempt.ResponseAmountMinorUnits ?? attempt.AmountMinorUnits,
                TransactionType = attempt.ResponseTransactionType ?? (int)attempt.Operation,
                PaymentType = attempt.ResponsePaymentType ?? attempt.PaymentType,
                ResponseTransactionUid = attempt.TransactionUid
            };
        }

        private static void ApplyAuthorization(
            ProviderPaymentAttempt attempt,
            PaymentAuthorizationResult authorization)
        {
            attempt.TransactionId = authorization.TransactionId;
            attempt.ResponseTerminalId = authorization.TerminalId;
            attempt.Rrn = authorization.Rrn;
            attempt.AuthorizationCode = authorization.AuthorizationCode;
            attempt.ResponseAmountMinorUnits = authorization.AmountMinorUnits;
            attempt.ResponseTransactionType = authorization.TransactionType;
            attempt.ResponsePaymentType = authorization.PaymentType;
        }

        private static bool IsRefundInvoice(InvoiceModel invoice)
        {
            string invoiceType = invoice == null || invoice.InvoiceHeader == null
                ? null
                : invoice.InvoiceHeader.InvoiceType;
            return string.Equals(invoiceType, "5.1", StringComparison.Ordinal) ||
                string.Equals(invoiceType, "5.2", StringComparison.Ordinal) ||
                string.Equals(invoiceType, "11.4", StringComparison.Ordinal);
        }

        private static bool IsPosPayment(PaymentMethod value)
        {
            return value != null &&
                (Convert.ToInt32(value.Type, CultureInfo.InvariantCulture) == 7 ||
                 Convert.ToInt32(value.Type, CultureInfo.InvariantCulture) == 8);
        }

        private static bool HasPosPayment(InvoiceModel invoice)
        {
            return invoice != null &&
                invoice.PaymentMethods != null &&
                invoice.PaymentMethods.Any(IsPosPayment);
        }

        private static InvoiceResponse Error(string message)
        {
            return new InvoiceResponse
            {
                Errors = new[] { string.IsNullOrWhiteSpace(message) ? "Invoice-provider issuance failed." : message },
                Warnings = new string[0]
            };
        }

        private static string[] Append(string[] existing, string message)
        {
            return (existing ?? new string[0]).Concat(new[] { message }).ToArray();
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }
    }

}
