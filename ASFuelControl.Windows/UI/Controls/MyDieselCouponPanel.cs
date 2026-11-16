using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ASFuelControl.Invoicing;
using ASFuelControl.Invoicing.Persistence;
using ASFuelControl.Windows.Threads;
using ASFuelControl.Windows.UI.Forms;
using Exedron.Invoicing.Adapters;
using Exedron.Invoicing.Core;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Controls
{
    internal sealed class MyDieselCouponPanel : UserControl
    {
        private readonly Func<bool> prepareInvoice;
        private readonly Func<string> traderVat;
        private readonly Func<string> eligibilityProblem;
        private readonly Action<MyDieselCouponCaseRecord> applyDiscount;
        private readonly Action clearDiscount;
        private readonly Action invoiceValuesChanged;
        private readonly RadLabel state = new RadLabel { AutoSize = false,Dock = DockStyle.Fill,TextAlignment = ContentAlignment.MiddleLeft };
        private readonly RadLabel details = new RadLabel { AutoSize = false,Dock = DockStyle.Fill,TextAlignment = ContentAlignment.MiddleLeft };
        private readonly RadLabel warning = new RadLabel { AutoSize = false,Dock = DockStyle.Fill,ForeColor = Color.FromArgb(138,80,0),TextAlignment = ContentAlignment.MiddleLeft };
        private readonly FlowLayoutPanel actions = new FlowLayoutPanel { Dock = DockStyle.Fill,FlowDirection = FlowDirection.LeftToRight,WrapContents = false,AutoScroll = true,Padding = new Padding(0,4,0,0) };
        private readonly RadButton capture, reassign, reserve, release, dismiss, refresh, correctMark, changeBranch, evidence;
        private Guid invoiceId;
        private bool newUnsavedInvoice;
        private SharedMyDieselStatus status;
        internal bool Busy { get; private set; }
        internal bool HasBlockingCase => status?.HistoricalReassignmentSource != true &&
            status?.CouponCase?.BlocksInvoice == true;
        internal bool DiscountLocked
        {
            get
            {
                var value = status?.CouponCase;
                if (value == null || status?.HistoricalReassignmentSource == true) return false;
                return value.LocalStage == MyDieselCaseStage.Reserved || value.LocalStage == MyDieselCaseStage.InvoiceAccepted ||
                    value.LocalStage == MyDieselCaseStage.RedeemUnknown || value.LocalStage == MyDieselCaseStage.Redeemed ||
                    value.LocalStage == MyDieselCaseStage.VerificationPending || value.LocalStage == MyDieselCaseStage.Final ||
                    value.LocalStage == MyDieselCaseStage.ReassignmentPending;
            }
        }
        internal event EventHandler StateChanged;

        internal MyDieselCouponPanel(Func<bool> prepareInvoice,
            Func<string> traderVat, Func<string> eligibilityProblem,
            Action<MyDieselCouponCaseRecord> applyDiscount,
            Action clearDiscount, Action invoiceValuesChanged)
        {
            this.prepareInvoice = prepareInvoice ?? throw new ArgumentNullException(nameof(prepareInvoice));
            this.traderVat = traderVat ?? throw new ArgumentNullException(nameof(traderVat));
            this.eligibilityProblem = eligibilityProblem ?? throw new ArgumentNullException(nameof(eligibilityProblem));
            this.applyDiscount = applyDiscount ?? throw new ArgumentNullException(nameof(applyDiscount));
            this.clearDiscount = clearDiscount ?? throw new ArgumentNullException(nameof(clearDiscount));
            this.invoiceValuesChanged = invoiceValuesChanged ?? throw new ArgumentNullException(nameof(invoiceValuesChanged));
            Height = 132; MinimumSize = new Size(760,132); BackColor = SystemColors.ControlLightLight;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill,ColumnCount = 2,RowCount = 4,Padding = new Padding(9,6,9,5) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,130)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,24)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,25));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,27)); layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            layout.Controls.Add(new RadLabel { Text = "myDiesel",Font = new Font(Font,FontStyle.Bold),Dock = DockStyle.Fill },0,0);
            layout.Controls.Add(state,1,0); layout.Controls.Add(new RadLabel { Text = "Στοιχεία",Dock = DockStyle.Fill },0,1); layout.Controls.Add(details,1,1);
            layout.Controls.Add(new RadLabel { Text = "Προειδοποίηση",Dock = DockStyle.Fill },0,2); layout.Controls.Add(warning,1,2);
            layout.Controls.Add(actions,0,3); layout.SetColumnSpan(actions,2); Controls.Add(layout);

            capture = Button("Σάρωση / καταχώριση",CaptureAndReserve);
            reassign = Button("Μεταφορά εξαργυρωμένου", ReassignRedeemed);
            reserve = Button("Έλεγχος και δέσμευση",Reserve);
            release = Button("Αποδέσμευση",Release);
            dismiss = Button("Αφαίρεση άκυρου",DismissInvalid);
            refresh = Button("Ανανέωση κατάστασης",Refresh);
            correctMark = Button("Διόρθωση MARK",CorrectMark);
            changeBranch = Button("Αλλαγή πρατηρίου",ChangeBranch);
            evidence = Button("Πλήρη τεκμήρια…",ShowEvidence);
            Render();
        }

        internal void LoadInvoice(Guid id, bool isNewUnsavedInvoice = false)
        {
            if (invoiceId != id || isNewUnsavedInvoice)
            {
                newUnsavedInvoice = isNewUnsavedInvoice;
            }
            invoiceId = id;
            try
            {
                status = SharedMyDieselRuntime.ReadInvoice(id);
                if (newUnsavedInvoice)
                    status.CanAttachNewCoupon = true;
            }
            catch (Exception ex)
            {
                status = null; state.Text = "Δεν ήταν δυνατός ο έλεγχος myDiesel"; details.Text = ex.Message; warning.Text = "";
                Enable(false); StateChanged?.Invoke(this,EventArgs.Empty); return;
            }
            Render(); StateChanged?.Invoke(this,EventArgs.Empty);
        }

        internal void MarkInvoicePersisted()
        {
            newUnsavedInvoice = false;
            LoadInvoice(invoiceId);
        }

        internal void RefreshEligibility()
        {
            if (!IsDisposed) Render();
        }

        private void Render()
        {
            var value = status?.CouponCase;
            string currentEligibilityProblem = ReadEligibilityProblem();
            bool dismissed = value?.LocalStage == MyDieselCaseStage.Dismissed;
            bool released = value?.LocalStage == MyDieselCaseStage.Released;
            bool historicalReassignmentSource = status?.HistoricalReassignmentSource == true;
            if (value == null)
            {
                state.Text = "Δεν έχει συνδεθεί κουπόνι";
                details.Text = status?.CanAttachNewCoupon == true
                    ? "Σαρώστε το QR ή καταχωρίστε κωδικό και ΑΦΜ δικαιούχου."
                    : "Το παραστατικό έχει ήδη εκδοθεί ή εκτυπωθεί. Δεν μπορεί να προστεθεί κουπόνι myDiesel.";
                warning.Text = status?.CanAttachNewCoupon == true ? currentEligibilityProblem ?? "" : "";
            }
            else if (historicalReassignmentSource)
            {
                state.Text = "Μεταφέρθηκε σε νέο παραστατικό";
                details.Text = value.CouponCode + " · παλιό MARK " + status.HistoricalCouponMark +
                    " · τα τεκμήρια της αρχικής σύνδεσης διατηρούνται";
                warning.Text = "Το κουπόνι δεν ανήκει πλέον σε αυτό το παραστατικό.";
            }
            else if (dismissed)
            {
                state.Text = "Δεν έχει συνδεθεί κουπόνι";
                details.Text = "Το προηγούμενο άκυρο κουπόνι αφαιρέθηκε. Μπορείτε να σαρώσετε νέο κουπόνι.";
                warning.Text = currentEligibilityProblem ?? "";
            }
            else
            {
                state.Text = MyDieselEvidenceForm.StageLabel(value.LocalStage.ToString()) +
                    (string.IsNullOrWhiteSpace(value.RemoteStatus) ? "" : " · ΑΑΔΕ: " + value.RemoteStatus);
                details.Text = value.CouponCode + " · ΑΦΜ " + value.BeneficiaryVat + " · πρατήριο " +
                    (value.CurrentBranchNum ?? value.ReservedBranchNum) +
                    (value.IssuedLitres.HasValue ? " · " + value.IssuedLitres.Value.ToString("0.00") + " L" : "") +
                    " · ΕΦΚ χωρίς ΦΠΑ " + value.ExciseDiscountPerLitre.ToString("0.0000") + " €/L" +
                    (string.IsNullOrWhiteSpace(value.InvoiceMark) ? "" : " · MARK " + value.InvoiceMark);
                warning.Text = value.LowBalance ? value.LowBalanceMessageEl : value.LocalStage == MyDieselCaseStage.ReserveUnknown || value.LocalStage == MyDieselCaseStage.RedeemUnknown
                    ? "Η έκβαση είναι άγνωστη. Μην επαναλάβετε τυφλά την ενέργεια." : currentEligibilityProblem ?? "";
            }
            bool active = !Busy && invoiceId != Guid.Empty;
            bool showCapture = !historicalReassignmentSource &&
                (value == null || dismissed || released) && status?.CanAttachNewCoupon == true;
            capture.Text = released ? "Σάρωση νέου κουπονιού" : "Σάρωση / καταχώριση";
            capture.Visible = showCapture;
            capture.Enabled = active && showCapture && NewReservationsEnabled() && currentEligibilityProblem == null;
            bool showReassign = value == null && status?.CanAttachNewCoupon == true && currentEligibilityProblem == null;
            reassign.Visible = showReassign;
            reassign.Enabled = active && Program.AdminConnected && showReassign;
            bool showReserve = !historicalReassignmentSource && value != null &&
                (value.LocalStage == MyDieselCaseStage.Captured || value.LocalStage == MyDieselCaseStage.ReserveUnknown);
            reserve.Visible = showReserve;
            reserve.Enabled = active && showReserve && currentEligibilityProblem == null;
            bool showRelease = !historicalReassignmentSource && value != null &&
                value.LocalStage == MyDieselCaseStage.Reserved;
            release.Visible = showRelease;
            release.Enabled = active && showRelease;
            bool showDismiss = !historicalReassignmentSource && status?.CanAttachNewCoupon == true &&
                MyDieselCouponService.CanDismissInvalid(value, status.Attempts);
            dismiss.Visible = showDismiss;
            dismiss.Enabled = active && showDismiss;
            bool showRefresh = !historicalReassignmentSource && value != null && (value.LocalStage == MyDieselCaseStage.RedeemUnknown || value.LocalStage == MyDieselCaseStage.Redeemed ||
                value.LocalStage == MyDieselCaseStage.VerificationPending || value.LocalStage == MyDieselCaseStage.InterventionRequired);
            refresh.Visible = showRefresh;
            refresh.Enabled = active && showRefresh;
            var remote = value == null ? MyDieselCouponStatus.UnknownValue : MyDieselCouponStates.Parse(value.RemoteStatus);
            bool admin = Program.AdminConnected;
            bool showCorrectMark = !historicalReassignmentSource && value != null &&
                MyDieselCouponStates.CanRedeem(remote) && remote != MyDieselCouponStatus.Reserved;
            correctMark.Visible = showCorrectMark;
            correctMark.Enabled = active && admin && showCorrectMark;
            bool showChangeBranch = !historicalReassignmentSource && value != null &&
                MyDieselCouponStates.CanChangeBranch(remote);
            changeBranch.Visible = showChangeBranch;
            changeBranch.Enabled = active && admin && showChangeBranch;
            bool showEvidence = value != null;
            evidence.Visible = showEvidence;
            evidence.Enabled = active && showEvidence;
        }

        private bool NewReservationsEnabled()
        {
            try { return Data.Implementation.OptionHandler.Instance.MyDieselEnabled &&
                    SharedInvoicingSettings.LoadRuntime().MyDiesel?.Enabled == true; }
            catch { return false; }
        }

        private async void CaptureAndReserve(object sender, EventArgs e)
        {
            if (Busy) return;
            if (!EnsureEligible(true)) return;
            string initialInvoiceVat;
            try { initialInvoiceVat = ReadInvoiceTraderVat(); }
            catch (Exception ex) { RadMessageBox.Show(ex.Message,"myDiesel",MessageBoxButtons.OK,RadMessageIcon.Exclamation); return; }
            MyDieselCaptureInput input;
            using (var dialog = new MyDieselCouponCaptureDialog(initialInvoiceVat,true))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                input = dialog.Value;
            }
            if (!prepareInvoice()) return;
            if (!EnsureEligible(true)) return;
            string invoiceVat;
            try { invoiceVat = ReadInvoiceTraderVat(); }
            catch (Exception ex) { RadMessageBox.Show(ex.Message,"myDiesel",MessageBoxButtons.OK,RadMessageIcon.Exclamation); return; }
            if (initialInvoiceVat != null && invoiceVat == null)
            {
                RadMessageBox.Show("Ο πελάτης αφαιρέθηκε από το παραστατικό. Ανοίξτε ξανά τη σάρωση του κουπονιού.","myDiesel",MessageBoxButtons.OK,RadMessageIcon.Exclamation); return;
            }
            if (invoiceVat != null && invoiceVat != input.Identity.BeneficiaryVat)
            {
                RadMessageBox.Show("Το ΑΦΜ του πελάτη στο παραστατικό δεν συμφωνεί με το ΑΦΜ δικαιούχου του κουπονιού.","myDiesel",MessageBoxButtons.OK,RadMessageIcon.Exclamation); return;
            }
            MyDieselCouponCaseRecord reservedCoupon = null;
            await Run(() => {
                var configuration = SharedInvoicingSettings.LoadRuntime();
                var service = SharedMyDieselRuntime.CreateService(configuration);
                var scope = SharedMyDieselRuntime.NewReservationScope(configuration);
                decimal exciseRate = configuration.MyDiesel.ExciseDiscountPerLitre;
                var coupon = input.Qr == null
                    ? service.CaptureDraft(invoiceId, input.Identity, scope, null,
                        null, exciseRate)
                    : service.CaptureDraft(invoiceId, input.Qr, scope, exciseRate);
                reservedCoupon = ReserveCoupon(service, coupon.CouponCaseId);
            });
            bool discountApplied = await ApplyReservedDiscount(reservedCoupon);
            if (discountApplied && !IsDisposed)
            {
                invoiceValuesChanged();
            }
        }

        private string ReadInvoiceTraderVat()
        {
            string value = traderVat();
            return string.IsNullOrWhiteSpace(value) ? null : MyDieselCouponIdentity.NormalizeVat(value,"Invoice trader VAT");
        }

        private string ReadEligibilityProblem()
        {
            try { return eligibilityProblem(); }
            catch (Exception ex) { return "Δεν ήταν δυνατός ο έλεγχος επιλεξιμότητας myDiesel: " + ex.Message; }
        }

        private bool EnsureEligible(bool showMessage)
        {
            string problem = ReadEligibilityProblem();
            if (problem == null) return true;
            if (showMessage)
                RadMessageBox.Show(problem,"myDiesel",MessageBoxButtons.OK,RadMessageIcon.Exclamation);
            return false;
        }

        private async void Reserve(object sender, EventArgs e)
        {
            var value = status?.CouponCase; if (value == null) return;
            if (!EnsureEligible(true) || !prepareInvoice() || !EnsureEligible(true)) return;
            MyDieselCouponCaseRecord reservedCoupon = null;
            await Run(() => {
                var service = SharedMyDieselRuntime.CreateService();
                reservedCoupon = ReserveCoupon(service,value.CouponCaseId);
            });
            bool discountApplied = await ApplyReservedDiscount(reservedCoupon);
            if (discountApplied && !IsDisposed)
            {
                invoiceValuesChanged();
            }
        }

        private static MyDieselCouponCaseRecord ReserveCoupon(
            MyDieselCouponService service, Guid couponCaseId)
        {
            var result = service.Reserve(couponCaseId);
            var reserved = result == null ? null : result.CouponCase;
            MyDieselCouponStatus remoteStatus = reserved == null
                ? MyDieselCouponStatus.UnknownValue
                : MyDieselCouponStates.Parse(reserved.RemoteStatus);
            bool normalReservation = reserved != null && reserved.LocalStage == MyDieselCaseStage.Reserved &&
                remoteStatus == MyDieselCouponStatus.Reserved;
            bool reassignment = reserved != null && reserved.LocalStage == MyDieselCaseStage.ReassignmentPending &&
                MyDieselCouponStates.CanRedeem(remoteStatus) && remoteStatus != MyDieselCouponStatus.Reserved;
            if (!normalReservation && !reassignment)
                return null;
            if (!reserved.IssuedLitres.HasValue || reserved.IssuedLitres.Value <= 0)
            {
                if (normalReservation)
                {
                    service.Release(reserved.CouponCaseId);
                    throw new InvalidOperationException("Δεν επιστράφηκαν έγκυρα λίτρα κουπονιού. Το κουπόνι αποδεσμεύτηκε και δεν εφαρμόστηκε έκπτωση.");
                }

                throw new InvalidOperationException("Η μεταφορά δεν περιέχει έγκυρα λίτρα κουπονιού. Δεν εφαρμόστηκε έκπτωση.");
            }
            return reserved;
        }

        private async Task<bool> ApplyReservedDiscount(
            MyDieselCouponCaseRecord reservedCoupon)
        {
            if (reservedCoupon == null || IsDisposed)
            {
                return false;
            }

            string problem = ReadEligibilityProblem();
            try
            {
                if (problem != null)
                {
                    throw new InvalidOperationException(problem);
                }
                applyDiscount(reservedCoupon);
                return true;
            }
            catch (Exception exception)
            {
                MyDieselCouponStatus remoteStatus = MyDieselCouponStates.Parse(
                    reservedCoupon.RemoteStatus);
                bool normalReservation = reservedCoupon.LocalStage ==
                    MyDieselCaseStage.Reserved && remoteStatus ==
                    MyDieselCouponStatus.Reserved;
                if (normalReservation)
                {
                    bool released = false;
                    await Run(() =>
                    {
                        MyDieselServiceResult result =
                            SharedMyDieselRuntime.CreateService().Release(
                                reservedCoupon.CouponCaseId);
                        released = result != null && result.OperationState ==
                            MyDieselOperationState.Succeeded;
                    });
                    string releaseMessage = released
                        ? " Το κουπόνι αποδεσμεύτηκε και δεν εφαρμόστηκε έκπτωση."
                        : " Η έκπτωση δεν εφαρμόστηκε. Ελέγξτε την ενεργή δέσμευση myDiesel.";
                    RadMessageBox.Show(exception.Message + releaseMessage,
                        "myDiesel", MessageBoxButtons.OK,
                        RadMessageIcon.Exclamation);
                    return false;
                }

                RadMessageBox.Show(exception.Message +
                    " Η μεταφορά παραμένει συνδεδεμένη ώστε να διορθωθεί το νέο παραστατικό χωρίς απώλεια τεκμηρίων.",
                    "myDiesel", MessageBoxButtons.OK,
                    RadMessageIcon.Exclamation);
                return false;
            }
        }

        private async void ReassignRedeemed(object sender, EventArgs e)
        {
            if (Busy || !Program.AdminConnected || !EnsureEligible(true))
            {
                return;
            }
            if (!prepareInvoice() || !EnsureEligible(true))
            {
                return;
            }

            string invoiceVat;
            try
            {
                invoiceVat = ReadInvoiceTraderVat();
            }
            catch (Exception exception)
            {
                RadMessageBox.Show(exception.Message, "myDiesel", MessageBoxButtons.OK,
                    RadMessageIcon.Exclamation);
                return;
            }

            SharedMyDieselReassignmentCandidate[] candidates;
            try
            {
                candidates = SharedMyDieselRuntime.ReassignmentCandidates(invoiceId, invoiceVat);
            }
            catch (Exception exception)
            {
                RadMessageBox.Show(exception.Message, "myDiesel", MessageBoxButtons.OK,
                    RadMessageIcon.Exclamation);
                return;
            }
            if (candidates.Length == 0)
            {
                RadMessageBox.Show(
                    "Δεν βρέθηκε εξαργυρωμένο κουπόνι του ίδιου πελάτη σε παραστατικό με απευθείας διαβίβαση myDATA.",
                    "Μεταφορά myDiesel", MessageBoxButtons.OK, RadMessageIcon.Info);
                return;
            }

            SharedMyDieselReassignmentCandidate selectedCandidate;
            if (!ReassignmentDialog(candidates, out selectedCandidate))
            {
                return;
            }

            string reason;
            if (!ReasonDialog(out reason))
            {
                return;
            }
            DialogResult confirmation = RadMessageBox.Show(
                "Το κουπόνι θα αποσυνδεθεί από το παλιό παραστατικό απευθείας myDATA και θα συνδεθεί στο νέο παραστατικό παρόχου. " +
                "Μετά την αποδοχή του νέου παραστατικού, το νέο MARK θα σταλεί στο myDiesel ως διόρθωση. Συνέχεια;",
                "Μεταφορά myDiesel", MessageBoxButtons.YesNo, RadMessageIcon.Question);
            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            MyDieselCouponCaseRecord reservedCoupon = null;
            void ReassignOperation()
            {
                SharedInvoiceConfiguration configuration = SharedInvoicingSettings.LoadRuntime();
                MyDieselCouponService service = SharedMyDieselRuntime.CreateService(configuration);
                MyDieselCouponCaseRecord selectedCoupon = selectedCandidate.CouponCase;
                var identity = new MyDieselCouponIdentity(selectedCoupon.CouponCode,
                    selectedCoupon.BeneficiaryVat);
                var scope = new MyDieselScope(selectedCoupon.Environment,
                    selectedCoupon.CompanyVat, selectedCoupon.ReservedBranchNum,
                    selectedCoupon.CredentialProfileId, selectedCoupon.ReservedStationAmdika);
                string authorizedBy = string.IsNullOrWhiteSpace(Program.CurrentUserName)
                    ? Environment.UserName
                    : Program.CurrentUserName;
                MyDieselCouponCaseRecord reassigned = service.ReassignDirectMyDataToInvoice(
                    invoiceId, identity, scope, authorizedBy, reason);
                reservedCoupon = ReserveCoupon(service, reassigned.CouponCaseId);
            }

            await Run(ReassignOperation);
            bool discountApplied = await ApplyReservedDiscount(reservedCoupon);
            if (discountApplied && !IsDisposed)
            {
                invoiceValuesChanged();
            }
        }

        private static bool ReassignmentDialog(
            SharedMyDieselReassignmentCandidate[] candidates,
            out SharedMyDieselReassignmentCandidate selectedCandidate)
        {
            using (var dialog = new RadForm
            {
                Text = "Επιλογή εξαργυρωμένου κουπονιού myDiesel",
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(820, 320),
                MinimumSize = new Size(720, 280),
                ShowIcon = false
            })
            {
                var table = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 3,
                    Padding = new Padding(14)
                };
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
                var list = new ListBox
                {
                    Dock = DockStyle.Fill,
                    SelectionMode = SelectionMode.One,
                    HorizontalScrollbar = true
                };
                foreach (SharedMyDieselReassignmentCandidate candidate in candidates)
                {
                    list.Items.Add(candidate);
                }
                if (list.Items.Count > 0)
                {
                    list.SelectedIndex = 0;
                }

                var accept = new RadButton
                {
                    Text = "Επιλογή",
                    Dock = DockStyle.Right,
                    Width = 150
                };
                void AcceptSelection(object clickSender, EventArgs clickEvent)
                {
                    if (list.SelectedItem != null)
                    {
                        dialog.DialogResult = DialogResult.OK;
                    }
                }

                accept.Click += AcceptSelection;
                table.Controls.Add(new RadLabel
                {
                    Text = "Εμφανίζονται κουπόνια του ίδιου ΑΦΜ από οριστικοποιημένα ή μη παραστατικά με παγωμένη απευθείας διαβίβαση myDATA.",
                    Dock = DockStyle.Fill,
                    AutoSize = false
                }, 0, 0);
                table.Controls.Add(list, 0, 1);
                table.Controls.Add(accept, 0, 2);
                dialog.Controls.Add(table);
                DialogResult dialogResult = dialog.ShowDialog();
                selectedCandidate = list.SelectedItem as SharedMyDieselReassignmentCandidate;
                return dialogResult == DialogResult.OK && selectedCandidate != null;
            }
        }

        private static bool ReasonDialog(out string reason)
        {
            using (var dialog = new RadForm
            {
                Text = "Αιτιολογία μεταφοράς myDiesel",
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(580, 190),
                MinimumSize = new Size(580, 190),
                ShowIcon = false
            })
            {
                var table = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 3,
                    Padding = new Padding(14)
                };
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
                var note = new RadTextBox { Multiline = true, Dock = DockStyle.Fill };
                var accept = new RadButton { Text = "Συνέχεια", Dock = DockStyle.Right, Width = 150 };
                void AcceptReason(object clickSender, EventArgs clickEvent)
                {
                    if (!string.IsNullOrWhiteSpace(note.Text))
                    {
                        dialog.DialogResult = DialogResult.OK;
                    }
                }

                accept.Click += AcceptReason;
                table.Controls.Add(new RadLabel
                {
                    Text = "Αιτιολογία / τεκμήριο (υποχρεωτικό)",
                    Dock = DockStyle.Fill
                }, 0, 0);
                table.Controls.Add(note, 0, 1);
                table.Controls.Add(accept, 0, 2);
                dialog.Controls.Add(table);
                DialogResult dialogResult = dialog.ShowDialog();
                reason = note.Text.Trim();
                return dialogResult == DialogResult.OK;
            }
        }

        private async void Release(object sender, EventArgs e)
        {
            var value = status?.CouponCase; if (value == null) return;
            if (RadMessageBox.Show("Να αποδεσμευτεί το κουπόνι από το παραστατικό;","myDiesel",MessageBoxButtons.YesNo,RadMessageIcon.Question) != DialogResult.Yes) return;
            bool discountCleared = false;
            await Run(() =>
            {
                var result = SharedMyDieselRuntime.CreateService().Release(value.CouponCaseId);
                var released = result != null && result.OperationState == MyDieselOperationState.Succeeded &&
                    result.CouponCase != null &&
                    (result.CouponCase.LocalStage == MyDieselCaseStage.Released ||
                     result.CouponCase.LocalStage == MyDieselCaseStage.Final);
                if (!released) return;
                discountCleared = true;
            });
            if (discountCleared && !IsDisposed)
            {
                clearDiscount();
                invoiceValuesChanged();
            }
        }

        private async void DismissInvalid(object sender, EventArgs e)
        {
            var value = status?.CouponCase;
            if (status?.CanAttachNewCoupon != true ||
                !MyDieselCouponService.CanDismissInvalid(value, status.Attempts))
            {
                return;
            }
            DialogResult confirmation = RadMessageBox.Show(
                "Να αφαιρεθεί το άκυρο ή ανενεργό κουπόνι από το παραστατικό; Τα τεκμήρια θα διατηρηθούν.",
                "myDiesel", MessageBoxButtons.YesNo, RadMessageIcon.Question);
            if (confirmation != DialogResult.Yes)
            {
                return;
            }
            bool discountCleared = false;
            await Run(() =>
            {
                MyDieselCouponService service = SharedMyDieselRuntime.CreateService();
                service.DismissInvalid(value.CouponCaseId);
                discountCleared = true;
            });
            if (discountCleared && !IsDisposed)
            {
                clearDiscount();
                invoiceValuesChanged();
            }
        }

        private async void Refresh(object sender, EventArgs e)
        {
            var value = status?.CouponCase; if (value == null) return;
            bool reconcile = value.LocalStage == MyDieselCaseStage.RedeemUnknown || value.LocalStage == MyDieselCaseStage.InterventionRequired;
            if (reconcile && RadMessageBox.Show("Θα γίνει μόνο ασφαλής έλεγχος της τρέχουσας κατάστασης στην ΑΑΔΕ. Συνέχεια;","Συμφιλίωση myDiesel",MessageBoxButtons.YesNo,RadMessageIcon.Question) != DialogResult.Yes) return;
            await Run(() => SharedMyDieselRuntime.CreateService().RefreshStatus(value.CouponCaseId,reconcile));
        }

        private async void CorrectMark(object sender, EventArgs e)
        {
            var value = status?.CouponCase; if (value == null || !Program.AdminConnected) return;
            string mark, reason;
            if (!CorrectionDialog("Διόρθωση MARK","Νέο MARK (15 ψηφία)",value.InvoiceMark,out mark,out reason)) return;
            await Run(() => SharedMyDieselRuntime.CreateService().CorrectMark(value.CouponCaseId,value.InvoiceId,mark,reason));
        }

        private async void ChangeBranch(object sender, EventArgs e)
        {
            var value = status?.CouponCase; if (value == null || !Program.AdminConnected) return;
            string branch, reason;
            if (!CorrectionDialog("Αλλαγή πρατηρίου","Νέος αριθμός υποκαταστήματος",(value.CurrentBranchNum ?? value.ReservedBranchNum).ToString(),out branch,out reason)) return;
            int number; if (!int.TryParse(branch,out number) || number < 0) { RadMessageBox.Show("Ο αριθμός υποκαταστήματος δεν είναι έγκυρος."); return; }
            await Run(() => SharedMyDieselRuntime.CreateService().ChangeBranch(value.CouponCaseId,number,reason));
        }

        private void ShowEvidence(object sender, EventArgs e)
        {
            if (invoiceId == Guid.Empty) return;
            try { using (var form = new MyDieselEvidenceForm(invoiceId)) form.ShowDialog(this); }
            catch (Exception ex) { RadMessageBox.Show(ex.Message,"Τεκμήρια myDiesel",MessageBoxButtons.OK,RadMessageIcon.Error); }
        }

        private async Task Run(Action action)
        {
            if (Busy) return; Busy = true; Render(); StateChanged?.Invoke(this,EventArgs.Empty);
            try { await Task.Run(action); }
            catch (Exception ex) { if (!IsDisposed) RadMessageBox.Show(ex.Message,"myDiesel",MessageBoxButtons.OK,RadMessageIcon.Error); }
            finally { Busy = false; if (!IsDisposed) LoadInvoice(invoiceId); }
        }

        private RadButton Button(string text, EventHandler click)
        {
            var value = new RadButton { Text = text,Height = 30,AutoSize = true,MinimumSize = new Size(128,30),Margin = new Padding(0,0,7,0) };
            value.Click += click; actions.Controls.Add(value); return value;
        }

        private void Enable(bool enabled)
        {
            foreach (Control control in actions.Controls) control.Enabled = enabled;
        }

        private static bool CorrectionDialog(string title, string valueLabel, string initial, out string value, out string reason)
        {
            using (var dialog = new RadForm { Text = title,StartPosition = FormStartPosition.CenterParent,ClientSize = new Size(580,260),MinimumSize = new Size(580,260),ShowIcon = false })
            {
                var table = new TableLayoutPanel { Dock = DockStyle.Fill,ColumnCount = 1,RowCount = 5,Padding = new Padding(14) };
                table.RowStyles.Add(new RowStyle(SizeType.Absolute,28)); table.RowStyles.Add(new RowStyle(SizeType.Absolute,34));
                table.RowStyles.Add(new RowStyle(SizeType.Absolute,28)); table.RowStyles.Add(new RowStyle(SizeType.Percent,100)); table.RowStyles.Add(new RowStyle(SizeType.Absolute,38));
                var input = new RadTextBox { Text = initial ?? "",Dock = DockStyle.Fill };
                var note = new RadTextBox { Multiline = true,Dock = DockStyle.Fill };
                var accept = new RadButton { Text = "Καταχώριση",Dock = DockStyle.Right,Width = 150 };
                accept.Click += (s,e) => { if (!string.IsNullOrWhiteSpace(input.Text) && !string.IsNullOrWhiteSpace(note.Text)) dialog.DialogResult = DialogResult.OK; };
                table.Controls.Add(new RadLabel { Text = valueLabel,Dock = DockStyle.Fill },0,0); table.Controls.Add(input,0,1);
                table.Controls.Add(new RadLabel { Text = "Αιτιολογία / τεκμήριο (υποχρεωτικό)",Dock = DockStyle.Fill },0,2); table.Controls.Add(note,0,3); table.Controls.Add(accept,0,4);
                dialog.Controls.Add(table); bool ok = dialog.ShowDialog() == DialogResult.OK;
                value = input.Text.Trim(); reason = note.Text.Trim(); return ok;
            }
        }

    }
}
