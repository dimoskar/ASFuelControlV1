using System;
using System.Linq;
using Exedron.Invoicing.Core;

internal static partial class Program
{
    private static void MyDieselContracts()
    {
        Test("myDiesel normalizes Latin, Greek, spaces and hyphens to one coupon identity", () => {
            Equal("AYNY-1022-8625-7956-7356", MyDieselCouponIdentity.NormalizeCouponCode("  αυνυ 1022 8625-7956 7356 "));
            var identity = new MyDieselCouponIdentity("ayny1022862579567356", " 123456789 ");
            Equal("AYNY-1022-8625-7956-7356", identity.CouponCode);
            Equal("123456789", identity.BeneficiaryVat);
        });
        Test("myDiesel rejects malformed coupon, VAT, branch and MARK values", () => {
            Throws<ArgumentException>(() => MyDieselCouponIdentity.NormalizeCouponCode("ACYY-1022-8625-7956-7356"));
            Throws<ArgumentException>(() => MyDieselCouponIdentity.NormalizeCouponCode("AYNY-1022-8625-7956-735"));
            Throws<ArgumentException>(() => new MyDieselCouponIdentity("AYNY-1022-8625-7956-7356", "12345678A"));
            Throws<ArgumentOutOfRangeException>(() => new MyDieselLookupRequest("AYNY-1022-8625-7956-7356", "123456789", -1));
            Throws<ArgumentException>(() => new MyDieselRedeemRequest("AYNY-1022-8625-7956-7356", "123456789", 1, "40000123456789"));
            Equal("400001234567890", new MyDieselRedeemRequest("AYNY-1022-8625-7956-7356", "123456789", 1, " 400001234567890 ").Mark);
        });
        Test("myDiesel scope freezes environment, company, branch and credential profile", () => {
            var scope = new MyDieselScope(FiscalEnvironment.Test, "998877665", 0, " uat-1 ");
            Equal(FiscalEnvironment.Test, scope.Environment);
            Equal("998877665", scope.CompanyVat);
            Equal(0, scope.BranchNum);
            Equal("uat-1", scope.CredentialProfileId);
            Throws<ArgumentException>(() => new MyDieselScope(FiscalEnvironment.Unspecified, "998877665", 0, "uat-1"));
            Throws<ArgumentOutOfRangeException>(() => new MyDieselScope(FiscalEnvironment.Test, "998877665", -1, "uat-1"));
        });
        Test("myDiesel retains future remote status text without treating it as a known state", () => {
            var info = new MyDieselCouponInfo { CouponStatus = "FUTURE_AADE_STATE", CreatedAt = 1792648800, ValidUntil = 1792666800 };
            Equal(MyDieselCouponStatus.UnknownValue, info.KnownStatus);
            Equal("FUTURE_AADE_STATE", info.CouponStatus);
            Equal(DateTimeKind.Utc, info.CreatedUtc.UtcDateTime.Kind);
            Equal(new DateTime(2026, 10, 22, 6, 0, 0, DateTimeKind.Utc), info.CreatedUtc.UtcDateTime);
        });
        Test("myDiesel operation attempts use persist-before-dispatch transitions", () => {
            Matrix(Enum.GetValues(typeof(MyDieselOperationState)).Cast<MyDieselOperationState>().ToArray(),
                MyDieselOperationTransitions.CanTransition,
                "Prepared>Sending,Prepared>FailedBeforeDispatch,Sending>Succeeded,Sending>Rejected,Sending>Unknown,Sending>FailedBeforeDispatch,Unknown>Sending,Unknown>Succeeded,Unknown>Rejected,Unknown>Unknown");
        });
        Test("myDiesel coupon action rules match the documented lifecycle", () => {
            Check(MyDieselCouponStates.CanRedeem(MyDieselCouponStatus.Reserved), "Reserved coupon cannot redeem.");
            Check(MyDieselCouponStates.CanRedeem(MyDieselCouponStatus.VerificationInvalid), "Invalid verification cannot be corrected.");
            Check(!MyDieselCouponStates.CanRedeem(MyDieselCouponStatus.Verified), "Verified coupon can redeem again.");
            Check(MyDieselCouponStates.CanRelease(MyDieselCouponStatus.Reserved), "Reserved coupon cannot release.");
            Check(!MyDieselCouponStates.CanRelease(MyDieselCouponStatus.Active), "Active coupon can release.");
            Check(MyDieselCouponStates.CanChangeBranch(MyDieselCouponStatus.Redeemed), "Redeemed coupon cannot change branch.");
            Check(!MyDieselCouponStates.CanChangeBranch(MyDieselCouponStatus.Reserved), "Reserved coupon can change redeemed branch.");
            Check(!MyDieselCouponStates.SafeForBackgroundQuery(MyDieselCouponStatus.Active), "Background query may reserve an active coupon.");
            Check(!MyDieselCouponStates.SafeForBackgroundQuery(MyDieselCouponStatus.Reserved), "Background query may interfere with a reservation.");
            Check(MyDieselCouponStates.SafeForBackgroundQuery(MyDieselCouponStatus.Redeemed), "Redeemed coupon cannot be polled.");
            Check(MyDieselCouponStates.IsFinal(MyDieselCouponStatus.VerifiedPaid), "Paid verified state is not final.");
        });
    }
}
