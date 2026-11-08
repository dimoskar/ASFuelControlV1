using System;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Threading;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using Exedron.Invoicing.Adapters;
using Exedron.Invoicing.Core;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    internal sealed class MyDieselCaptureInput
    {
        internal MyDieselCouponIdentity Identity { get; set; }
        internal MyDieselQrCoupon Qr { get; set; }
    }

    /// <summary>
    /// Shared camera/manual-entry dialog. A supplied trader VAT is authoritative
    /// and is therefore shown read-only and checked against a scanned QR payload.
    /// Camera frames are decoded in memory and are never stored.
    /// </summary>
    internal sealed class MyDieselCouponCaptureDialog : RadForm
    {
        private const string CameraOptionKey = "MyDieselCameraMoniker";
        private readonly RadDropDownList cameras = new RadDropDownList { Dock = DockStyle.Fill };
        private readonly PictureBox preview = new PictureBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            SizeMode = PictureBoxSizeMode.Zoom
        };
        private readonly Panel previewHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            Padding = new Padding(3)
        };
        private readonly Panel recognitionCard;
        private readonly RadLabel recognitionCode = RecognitionValue();
        private readonly RadLabel recognitionVat = RecognitionValue();
        private readonly RadLabel recognitionLitres = RecognitionValue();
        private readonly RadLabel recognitionExpiry = RecognitionValue();
        private readonly RadLabel state = new RadLabel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlignment = ContentAlignment.MiddleLeft
        };
        private readonly RadButton restart = new RadButton
        {
            Text = "Επανεκκίνηση κάμερας",
            Dock = DockStyle.Fill
        };
        private readonly RadTextBox code = new RadTextBox { Dock = DockStyle.Fill };
        private readonly RadTextBox vat = new RadTextBox { Dock = DockStyle.Fill };
        private readonly RadButton accept;
        private readonly RadButton rescan;
        private readonly string traderVat;
        private VideoCaptureDevice source;
        private volatile MyDieselQrCoupon scannedQr;
        private int decoding;
        private int lastDecodeTick;
        private int lastPreviewTick;
        private bool loadingCameras;
        private bool closing;

        internal MyDieselCaptureInput Value { get; private set; }

        internal MyDieselCouponCaptureDialog(string traderVat, bool reserveImmediately)
        {
            this.traderVat = string.IsNullOrWhiteSpace(traderVat)
                ? null
                : MyDieselCouponIdentity.NormalizeVat(traderVat, "ΑΦΜ πελάτη");

            Text = "Σάρωση κουπονιού myDiesel";
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            ClientSize = new Size(800, 650);
            MinimumSize = new Size(700, 560);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(12)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));

            var deviceRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            deviceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75));
            deviceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            deviceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 185));
            deviceRow.Controls.Add(new RadLabel
            {
                Text = "Κάμερα",
                Dock = DockStyle.Fill,
                TextAlignment = ContentAlignment.MiddleLeft
            }, 0, 0);
            deviceRow.Controls.Add(cameras, 1, 0);
            restart.Margin = new Padding(7, 0, 0, 3);
            restart.Click += RestartScanning;
            deviceRow.Controls.Add(restart, 2, 0);
            table.Controls.Add(deviceRow, 0, 0);
            table.SetColumnSpan(deviceRow, 2);

            recognitionCard = CreateRecognitionCard();
            previewHost.Controls.Add(preview);
            previewHost.Controls.Add(recognitionCard);
            previewHost.Layout += CenterRecognitionCard;
            table.Controls.Add(previewHost, 0, 1);
            table.SetColumnSpan(previewHost, 2);
            state.Text = "Τοποθετήστε το QR μέσα στο κάδρο. Η εικόνα χρησιμοποιείται μόνο για την ανάγνωση και δεν αποθηκεύεται.";
            state.Padding = new Padding(8, 4, 8, 4);
            state.BackColor = Color.FromArgb(238, 247, 253);
            state.ForeColor = Color.FromArgb(20, 86, 122);
            table.Controls.Add(state, 0, 2);
            table.SetColumnSpan(state, 2);

            table.Controls.Add(new RadLabel { Text = "ή χειροκίνητος κωδικός", Dock = DockStyle.Fill }, 0, 3);
            table.Controls.Add(code, 1, 3);

            bool hasTraderVat = this.traderVat != null;
            vat.Text = this.traderVat ?? "";
            vat.ReadOnly = hasTraderVat;
            vat.TabStop = !hasTraderVat;
            table.Controls.Add(new RadLabel
            {
                Text = hasTraderVat ? "ΑΦΜ δικαιούχου (από πελάτη)" : "ΑΦΜ δικαιούχου (9 ψηφία)",
                Dock = DockStyle.Fill
            }, 0, 4);
            table.Controls.Add(vat, 1, 4);

            var cancel = new RadButton
            {
                Text = "Άκυρο",
                Dock = DockStyle.Left,
                Width = 110,
                DialogResult = DialogResult.Cancel
            };
            accept = new RadButton
            {
                Text = reserveImmediately ? "Έλεγχος και δέσμευση" : "Χρήση κουπονιού",
                Dock = DockStyle.Right,
                Width = 190,
                Height = 34,
                Enabled = false
            };
            rescan = new RadButton
            {
                Text = "Σάρωση ξανά",
                Dock = DockStyle.Fill,
                Visible = false
            };
            rescan.Click += RestartScanning;
            accept.Click += Accept;
            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0)
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            cancel.Dock = DockStyle.Fill;
            cancel.Margin = new Padding(0, 2, 8, 2);
            rescan.Margin = new Padding(0, 2, 8, 2);
            accept.Margin = new Padding(0, 2, 0, 2);
            actions.Controls.Add(cancel, 0, 0);
            actions.Controls.Add(rescan, 1, 0);
            actions.Controls.Add(accept, 2, 0);
            table.Controls.Add(actions, 0, 5);
            table.SetColumnSpan(actions, 2);
            Controls.Add(table);
            AcceptButton = accept;
            CancelButton = cancel;

            cameras.DropDownStyle = RadDropDownStyle.DropDownList;
            cameras.SelectedIndexChanged += (s, e) =>
            {
                if (loadingCameras) return;
                SaveSelectedCamera();
                ClearScannedCoupon();
                StartSelectedCamera();
            };
            code.TextChanged += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(code.Text)) ClearScannedCoupon();
                UpdateAcceptState();
            };
            Shown += (s, e) => LoadCameras();
            FormClosing += (s, e) =>
            {
                closing = true;
                StopCamera();
                DisposePreviewImage();
            };
        }

        private Panel CreateRecognitionCard()
        {
            Color successColor = Color.FromArgb(35, 145, 55);
            var border = new Panel
            {
                BackColor = successColor,
                Padding = new Padding(3),
                Size = new Size(545, 255),
                Visible = false
            };
            var content = new TableLayoutPanel
            {
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(20, 12, 20, 16)
            };
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var heading = new RadLabel
            {
                Text = "✓  Το QR αναγνωρίστηκε επιτυχώς",
                Dock = DockStyle.Fill,
                AutoSize = false,
                ForeColor = successColor,
                TextAlignment = ContentAlignment.MiddleCenter,
                Font = new Font(Font.FontFamily, 17F, FontStyle.Bold)
            };
            var helper = new RadLabel
            {
                Text = "Ελέγξτε τα στοιχεία και συνεχίστε.",
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlignment = ContentAlignment.MiddleCenter,
                Font = new Font(Font.FontFamily, 10F, FontStyle.Regular)
            };
            var details = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                Margin = new Padding(14, 2, 14, 0)
            };
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43));
            details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57));
            for (int row = 0; row < 4; row++)
            {
                details.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            }
            RadLabel codeCaption = RecognitionCaption("Κωδικός κουπονιού");
            RadLabel vatCaption = RecognitionCaption("ΑΦΜ δικαιούχου");
            RadLabel litresCaption = RecognitionCaption("Διαθέσιμα λίτρα");
            RadLabel expiryCaption = RecognitionCaption("Ισχύει έως");
            details.Controls.Add(codeCaption, 0, 0);
            details.Controls.Add(recognitionCode, 1, 0);
            details.Controls.Add(vatCaption, 0, 1);
            details.Controls.Add(recognitionVat, 1, 1);
            details.Controls.Add(litresCaption, 0, 2);
            details.Controls.Add(recognitionLitres, 1, 2);
            details.Controls.Add(expiryCaption, 0, 3);
            details.Controls.Add(recognitionExpiry, 1, 3);

            content.Controls.Add(heading, 0, 0);
            content.Controls.Add(helper, 0, 1);
            content.Controls.Add(details, 0, 2);
            border.Controls.Add(content);
            return border;
        }

        private static RadLabel RecognitionCaption(string text)
        {
            return new RadLabel
            {
                Text = text,
                Dock = DockStyle.Fill,
                AutoSize = false,
                BackColor = Color.FromArgb(245, 247, 249),
                TextAlignment = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 4, 0),
                Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold)
            };
        }

        private static RadLabel RecognitionValue()
        {
            return new RadLabel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                BackColor = Color.White,
                TextAlignment = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 4, 0)
            };
        }

        private void CenterRecognitionCard(object sender, LayoutEventArgs e)
        {
            int left = Math.Max(0,
                (previewHost.ClientSize.Width - recognitionCard.Width) / 2);
            int top = Math.Max(0,
                (previewHost.ClientSize.Height - recognitionCard.Height) / 2);
            recognitionCard.Location = new Point(left, top);
        }

        private void RestartScanning(object sender, EventArgs e)
        {
            ClearScannedCoupon();
            bool cameraAvailable = cameras.Items.Count > 0 &&
                cameras.SelectedItem != null;
            cameras.Enabled = cameraAvailable;
            restart.Enabled = cameraAvailable;
            StartSelectedCamera();
        }

        private void LoadCameras()
        {
            try
            {
                loadingCameras = true;
                var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                cameras.Items.Clear();
                foreach (FilterInfo device in devices)
                    cameras.Items.Add(new RadListDataItem(device.Name) { Value = device.MonikerString });

                if (cameras.Items.Count == 0)
                {
                    loadingCameras = false;
                    cameras.Enabled = false;
                    restart.Enabled = false;
                    ShowState("Δεν βρέθηκε διαθέσιμη κάμερα. Πληκτρολογήστε τον κωδικό του κουπονιού.", true);
                    return;
                }

                cameras.Enabled = true;
                restart.Enabled = true;
                string savedMoniker = Data.Implementation.OptionHandler.Instance.GetOption(CameraOptionKey, null);
                int savedIndex = -1;
                if (!string.IsNullOrWhiteSpace(savedMoniker))
                {
                    for (int index = 0; index < cameras.Items.Count; index++)
                    {
                        if (string.Equals(cameras.Items[index].Value as string, savedMoniker,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            savedIndex = index;
                            break;
                        }
                    }
                }
                cameras.SelectedIndex = savedIndex >= 0 ? savedIndex : 0;
                loadingCameras = false;
                if (string.IsNullOrWhiteSpace(savedMoniker)) SaveSelectedCamera();
                StartSelectedCamera();
            }
            catch (Exception ex)
            {
                loadingCameras = false;
                cameras.Enabled = false;
                restart.Enabled = false;
                ShowState("Δεν ήταν δυνατή η αναζήτηση καμερών: " + ex.Message + " Πληκτρολογήστε τον κωδικό του κουπονιού.", true);
            }
        }

        private void SaveSelectedCamera()
        {
            if (cameras.SelectedItem == null) return;
            string moniker = cameras.SelectedItem.Value as string;
            if (string.IsNullOrWhiteSpace(moniker)) return;
            try
            {
                Data.Implementation.OptionHandler.Instance.SetOption(CameraOptionKey, moniker);
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.LogToFile("myDiesel camera selection", ex);
                ShowState("Η επιλογή κάμερας δεν αποθηκεύτηκε: " + ex.Message, true);
            }
        }

        private void StartSelectedCamera()
        {
            StopCamera();
            if (closing || cameras.SelectedItem == null) return;
            string moniker = cameras.SelectedItem.Value as string;
            if (string.IsNullOrWhiteSpace(moniker)) return;
            try
            {
                source = new VideoCaptureDevice(moniker);
                var capabilities = source.VideoCapabilities ?? new VideoCapabilities[0];
                var capability = capabilities
                    .Where(value => value != null && value.FrameSize.Width <= 1920 && value.FrameSize.Height <= 1080)
                    .OrderBy(value => Math.Abs(value.FrameSize.Width - 1280) + Math.Abs(value.FrameSize.Height - 720))
                    .FirstOrDefault();
                if (capability != null) source.VideoResolution = capability;
                source.NewFrame += SourceNewFrame;
                source.VideoSourceError += SourceVideoError;
                source.Start();
                restart.Enabled = true;
                ShowState("Τοποθετήστε το QR μέσα στο κάδρο. Η ανάγνωση ολοκληρώνεται αυτόματα.", false);
            }
            catch (Exception ex)
            {
                StopCamera();
                ShowState("Η κάμερα δεν μπόρεσε να ξεκινήσει: " + ex.Message + " Πληκτρολογήστε τον κωδικό του κουπονιού.", true);
            }
        }

        private void SourceNewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (closing || eventArgs.Frame == null) return;
            Bitmap frame = null;
            Bitmap display = null;
            try
            {
                frame = (Bitmap)eventArgs.Frame.Clone();
                int now = Environment.TickCount;
                if (unchecked(now - lastPreviewTick) >= 66)
                {
                    lastPreviewTick = now;
                    display = (Bitmap)frame.Clone();
                    Present(display);
                    display = null;
                }

                if (scannedQr != null || unchecked(now - lastDecodeTick) < 250 ||
                    Interlocked.Exchange(ref decoding, 1) != 0) return;
                lastDecodeTick = now;
                try
                {
                    string text = MyDieselQrImageDecoder.Decode(frame);
                    if (text == null) return;
                    MyDieselQrCoupon parsed;
                    try
                    {
                        parsed = MyDieselQrParser.Parse(text);
                        if (traderVat != null && parsed.Identity.BeneficiaryVat != traderVat)
                            throw new InvalidOperationException("Το ΑΦΜ δικαιούχου του κουπονιού δεν συμφωνεί με το ΑΦΜ του επιλεγμένου πελάτη.");
                    }
                    catch (Exception ex)
                    {
                        ShowState("Αναγνώστηκε QR, αλλά δεν είναι έγκυρο κουπόνι myDiesel: " + ex.Message, true);
                        return;
                    }
                    SetScannedCoupon(parsed);
                }
                finally
                {
                    Interlocked.Exchange(ref decoding, 0);
                }
            }
            catch (Exception ex)
            {
                ShowState("Σφάλμα ανάγνωσης εικόνας κάμερας: " + ex.Message, true);
            }
            finally
            {
                if (display != null) display.Dispose();
                if (frame != null) frame.Dispose();
            }
        }

        private void SourceVideoError(object sender, VideoSourceErrorEventArgs eventArgs)
        {
            ShowState("Η κάμερα ανέφερε σφάλμα: " + eventArgs.Description, true);
        }

        private void Present(Bitmap image)
        {
            if (closing || IsDisposed)
            {
                image.Dispose();
                return;
            }
            try
            {
                BeginInvoke((Action)(() =>
                {
                    if (closing || IsDisposed)
                    {
                        image.Dispose();
                        return;
                    }
                    var old = preview.Image;
                    preview.Image = image;
                    if (old != null) old.Dispose();
                }));
            }
            catch
            {
                image.Dispose();
            }
        }

        private void SetScannedCoupon(MyDieselQrCoupon parsed)
        {
            if (closing || IsDisposed) return;
            try
            {
                BeginInvoke((Action)(() =>
                {
                    if (closing || IsDisposed || !string.IsNullOrWhiteSpace(code.Text)) return;
                    scannedQr = parsed;
                    if (traderVat == null) vat.Text = parsed.Identity.BeneficiaryVat;
                    StopCamera();
                    cameras.Enabled = false;
                    restart.Enabled = false;
                    recognitionCode.Text = parsed.Identity.CouponCode;
                    recognitionVat.Text = parsed.Identity.BeneficiaryVat;
                    recognitionLitres.Text = parsed.IssuedLitres.ToString("0.00") + " L";
                    DateTimeOffset localExpiry = parsed.ValidUntilUtc.ToLocalTime();
                    recognitionExpiry.Text = localExpiry.ToString("dd/MM/yyyy HH:mm");
                    Color successColor = Color.FromArgb(35, 145, 55);
                    previewHost.BackColor = successColor;
                    recognitionCard.Visible = true;
                    recognitionCard.BringToFront();
                    rescan.Visible = true;
                    state.Text = "✓ Επιτυχής τοπική αναγνώριση QR";
                    state.ForeColor = Color.FromArgb(25, 115, 45);
                    state.BackColor = Color.FromArgb(232, 249, 235);
                    accept.BackColor = successColor;
                    accept.ForeColor = Color.White;
                    UpdateAcceptState();
                    accept.Select();
                    try
                    {
                        SystemSounds.Asterisk.Play();
                    }
                    catch
                    {
                    }
                }));
            }
            catch { }
        }

        private void ClearScannedCoupon()
        {
            scannedQr = null;
            recognitionCard.Visible = false;
            previewHost.BackColor = Color.Black;
            rescan.Visible = false;
            accept.BackColor = SystemColors.Control;
            accept.ForeColor = SystemColors.ControlText;
            UpdateAcceptState();
        }

        private void UpdateAcceptState()
        {
            accept.Enabled = scannedQr != null || !string.IsNullOrWhiteSpace(code.Text);
        }

        private void Accept(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(code.Text))
                {
                    Value = new MyDieselCaptureInput
                    {
                        Identity = new MyDieselCouponIdentity(code.Text, traderVat ?? vat.Text)
                    };
                }
                else if (scannedQr != null)
                {
                    Value = new MyDieselCaptureInput { Identity = scannedQr.Identity, Qr = scannedQr };
                }
                else
                {
                    return;
                }
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                RadMessageBox.Show(ex.Message, "Κουπόνι myDiesel", MessageBoxButtons.OK, RadMessageIcon.Exclamation);
            }
        }

        private void ShowState(string message, bool error)
        {
            if (closing || IsDisposed) return;
            Action update = () =>
            {
                if (closing || IsDisposed) return;
                state.Text = message;
                state.ForeColor = error ? Color.FromArgb(170, 60, 35) : Color.FromArgb(20, 86, 122);
                state.BackColor = error ? Color.FromArgb(255, 242, 235) : Color.FromArgb(238, 247, 253);
            };
            try
            {
                if (InvokeRequired) BeginInvoke(update); else update();
            }
            catch { }
        }

        private void StopCamera()
        {
            var current = source;
            source = null;
            if (current == null) return;
            current.NewFrame -= SourceNewFrame;
            current.VideoSourceError -= SourceVideoError;
            try
            {
                if (current.IsRunning)
                {
                    current.SignalToStop();
                    current.WaitForStop();
                }
            }
            catch
            {
                try
                {
                    if (current.IsRunning) current.Stop();
                }
                catch { }
            }
        }

        private void DisposePreviewImage()
        {
            var image = preview.Image;
            preview.Image = null;
            if (image != null) image.Dispose();
        }
    }
}
