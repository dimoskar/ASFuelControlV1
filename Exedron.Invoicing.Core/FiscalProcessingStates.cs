using System;

namespace Exedron.Invoicing.Core
{
    // Values are stable persistence contracts. A default (Unspecified) is never success.
    public enum FiscalSigningState
    {
        Unspecified = 0, NotRequired = 1, Prepared = 2, Signing = 3,
        Signed = 4, Rejected = 5, Unknown = 6, FailedBeforeDispatch = 7
    }
    public enum FiscalTransmissionState
    {
        Unspecified = 0, NotRequired = 1, Prepared = 2, Sending = 3,
        Accepted = 4, Rejected = 5, Unknown = 6, FailedBeforeDispatch = 7
    }
    // Cancellation has its own journal entry; it does not reset an accepted issuance.
    public enum FiscalTransmissionOperation { Unspecified = 0, Issue = 1, Cancel = 2 }
    // State of one authorization, void or refund operation, not the invoice's paid balance.
    public enum FiscalPaymentState
    {
        Unspecified = 0, NotRequired = 1, Prepared = 2, Processing = 3,
        Accepted = 4, Rejected = 5, Unknown = 6, FailedBeforeDispatch = 7
    }
    public enum FiscalPaymentOperation { Unspecified = 0, Authorize = 1, Void = 2, Refund = 3 }
    public enum FiscalPrintState
    {
        Unspecified = 0, NotRequired = 1, Prepared = 2, Dispatching = 3,
        HandedOff = 4, Confirmed = 5, Unknown = 6, FailedBeforeDispatch = 7, Rejected = 8
    }

    // Four separate observations: signing/transmission/payment do not imply printing.
    // Persistence and orchestration belong to the host. These rules concern ONE attempt.
    public static class FiscalStateTransitions
    {
        public static bool CanTransition(FiscalSigningState from, FiscalSigningState to)
        {
            return Enum.IsDefined(typeof(FiscalSigningState), from) && Enum.IsDefined(typeof(FiscalSigningState), to) &&
                CanTransitionExternal((int)from, (int)to);
        }
        public static bool CanTransition(FiscalTransmissionState from, FiscalTransmissionState to)
        {
            return Enum.IsDefined(typeof(FiscalTransmissionState), from) && Enum.IsDefined(typeof(FiscalTransmissionState), to) &&
                CanTransitionExternal((int)from, (int)to);
        }
        public static bool CanTransition(FiscalPaymentState from, FiscalPaymentState to)
        {
            return Enum.IsDefined(typeof(FiscalPaymentState), from) && Enum.IsDefined(typeof(FiscalPaymentState), to) &&
                CanTransitionExternal((int)from, (int)to);
        }
        // The three external-operation enums intentionally share these stable numeric states.
        // Reconciliation may resolve Unknown only with authoritative evidence of acceptance/rejection.
        private static bool CanTransitionExternal(int from, int to)
        {
            switch (from)
            {
                case 2: return to == 3 || to == 7;
                case 3: return to == 4 || to == 5 || to == 6;
                case 6: return to == 4 || to == 5 || to == 6; // Persist another inconclusive lookup, not another send.
                default: return false;
            }
        }
        public static bool CanTransition(FiscalPrintState from, FiscalPrintState to)
        {
            switch (from)
            {
                case FiscalPrintState.Prepared:
                    return to == FiscalPrintState.Dispatching || to == FiscalPrintState.FailedBeforeDispatch;
                case FiscalPrintState.Dispatching:
                    return to == FiscalPrintState.HandedOff || to == FiscalPrintState.Confirmed ||
                        to == FiscalPrintState.Rejected || to == FiscalPrintState.Unknown;
                case FiscalPrintState.HandedOff:
                    return to == FiscalPrintState.Confirmed || to == FiscalPrintState.Unknown;
                case FiscalPrintState.Unknown:
                    return to == FiscalPrintState.Confirmed || to == FiscalPrintState.Rejected || to == FiscalPrintState.Unknown;
                default: return false;
            }
        }
    }
}
