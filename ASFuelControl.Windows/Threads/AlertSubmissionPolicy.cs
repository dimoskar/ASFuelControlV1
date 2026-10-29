using ASFuelControl.Data;
using System;
using System.Linq.Expressions;

namespace ASFuelControl.Windows.Threads
{
    /// <summary>
    /// Defines which alerts must be signed and submitted to the ministry.
    /// Automatically resolved alerts are deliberately suppressed; an alert
    /// resolved by an operator remains eligible for submission.
    /// </summary>
    internal static class AlertSubmissionPolicy
    {
        internal const string SystemResolvedMarker = "[System Resolved]";
        internal const string DateResolvedMarker = "[Date Resolved]";
        internal const string LegacyAutoResolvedMarker = "Auto Resolved";

        internal static Expression<Func<SystemEvent, bool>> IsEligibleForMinistry
        {
            get
            {
                return alert =>
                    !alert.ResolvedDate.HasValue ||
                    (
                        alert.ResolveMessage != null &&
                        alert.ResolveMessage != "" &&
                        !alert.ResolveMessage.Contains(SystemResolvedMarker) &&
                        !alert.ResolveMessage.Contains(DateResolvedMarker) &&
                        !alert.ResolveMessage.Contains(LegacyAutoResolvedMarker)
                    );
            }
        }
    }
}
