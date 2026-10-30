using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ASFuelControl.Windows.Threads;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    internal sealed class OutboundSubmissionAuditForm : RadForm
    {
        private readonly OutboundSubmissionAuditStore store;
        private readonly RadDateTimePicker fromDate = new RadDateTimePicker();
        private readonly RadDateTimePicker toDate = new RadDateTimePicker();
        private readonly RadDropDownList statusFilter = new RadDropDownList();
        private readonly RadSpinEditor maximumRows = new RadSpinEditor();
        private readonly RadGridView submissionsGrid = new RadGridView();
        private readonly RadGridView attemptsGrid = new RadGridView();
        private readonly RadLabel resultCount = new RadLabel();
        private readonly RadLabel storageInfo = new RadLabel();
        private readonly RadLabel selectionInfo = new RadLabel();
        private readonly RadButton pinButton = new RadButton();
        private readonly RadButton exportButton = new RadButton();
        private readonly RadButton cleanupButton = new RadButton();
        private readonly RadTextBox requestText = CreatePayloadBox();
        private readonly RadTextBox responseText = CreatePayloadBox();
        private readonly RadTextBox errorText = CreatePayloadBox();

        public OutboundSubmissionAuditForm()
        {
            this.store = new OutboundSubmissionAuditStore(Properties.Settings.Default.DBConnection);
            this.Text = "Έλεγχος Εξερχόμενων Αποστολών";
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(960, 650);
            this.Size = new Size(1250, 780);
            this.ThemeName = "TelerikMetroBlue";
            this.Font = new Font("Segoe UI", 9F);
            this.BackColor = Color.White;
            this.ShowIcon = false;

            BuildLayout();
            ConfigureSubmissionGrid();
            ConfigureAttemptGrid();

            fromDate.Value = DateTime.Today.AddDays(-7);
            toDate.Value = DateTime.Today;
            statusFilter.DataSource = new[]
            {
                new StatusOption("Problems", "Προβλήματα"),
                new StatusOption("Failed", "Αποτυχία"),
                new StatusOption("RetryScheduled", "Προγραμματισμένη επανάληψη"),
                new StatusOption("Unknown", "Άγνωστη έκβαση"),
                new StatusOption("Sending", "Σε αποστολή"),
                new StatusOption("Completed", "Ολοκληρωμένες"),
                new StatusOption("All", "Όλες")
            };
            statusFilter.DisplayMember = "Text";
            statusFilter.ValueMember = "Value";
            statusFilter.SelectedIndex = 0;
            maximumRows.Minimum = 1;
            maximumRows.Maximum = 5000;
            maximumRows.Value = 500;

            this.Shown += delegate { RefreshSubmissions(); };
        }

        private void BuildLayout()
        {
            var filterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(10, 10, 10, 7),
                WrapContents = false,
                BackColor = Color.FromArgb(247, 248, 250)
            };
            filterPanel.Controls.Add(CreateFilterLabel("Από"));
            ConfigureDatePicker(fromDate);
            filterPanel.Controls.Add(fromDate);
            filterPanel.Controls.Add(CreateFilterLabel("Έως", 15));
            ConfigureDatePicker(toDate);
            filterPanel.Controls.Add(toDate);
            filterPanel.Controls.Add(CreateFilterLabel("Κατάσταση", 15));
            statusFilter.DropDownStyle = RadDropDownStyle.DropDownList;
            statusFilter.AutoSize = false;
            statusFilter.Size = new Size(205, 28);
            filterPanel.Controls.Add(statusFilter);
            filterPanel.Controls.Add(CreateFilterLabel("Μέγιστες εγγραφές", 15));
            maximumRows.AutoSize = false;
            maximumRows.Size = new Size(80, 28);
            filterPanel.Controls.Add(maximumRows);

            var refresh = new RadButton
            {
                Text = "Ανανέωση",
                Size = new Size(115, 29),
                Margin = new Padding(15, 0, 3, 0)
            };
            refresh.Click += delegate { RefreshSubmissions(); };
            filterPanel.Controls.Add(refresh);
            resultCount.Text = "Εγγραφές: 0";
            resultCount.AutoSize = true;
            resultCount.Margin = new Padding(15, 5, 3, 0);
            filterPanel.Controls.Add(resultCount);

            var mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 6,
                BackColor = Color.FromArgb(220, 224, 230)
            };
            submissionsGrid.Dock = DockStyle.Fill;
            mainSplit.Panel1.Controls.Add(CreateSection("Αποστολές", submissionsGrid));

            var detailSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 6,
                BackColor = Color.FromArgb(220, 224, 230)
            };
            attemptsGrid.Dock = DockStyle.Fill;
            detailSplit.Panel1.Controls.Add(CreateSection("Προσπάθειες αποστολής", attemptsGrid));

            var payloadTabs = new RadPageView { Dock = DockStyle.Fill, ThemeName = "TelerikMetroBlue" };
            payloadTabs.Pages.Add(CreatePayloadPage("Αίτημα", requestText));
            payloadTabs.Pages.Add(CreatePayloadPage("Απάντηση", responseText));
            payloadTabs.Pages.Add(CreatePayloadPage("Σφάλμα", errorText));
            detailSplit.Panel2.Controls.Add(payloadTabs);
            mainSplit.Panel2.Controls.Add(detailSplit);

            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                Padding = new Padding(8),
                BackColor = Color.White
            };
            var close = new RadButton { Text = "Κλείσιμο", Dock = DockStyle.Right, Width = 125 };
            close.Click += delegate { this.Close(); };
            storageInfo.Dock = DockStyle.Fill;
            storageInfo.Padding = new Padding(2, 7, 0, 0);
            storageInfo.ForeColor = Color.DimGray;
            footer.Controls.Add(storageInfo);
            footer.Controls.Add(close);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                Padding = new Padding(10, 7, 10, 6),
                WrapContents = false,
                BackColor = Color.White
            };
            ConfigureActionButton(pinButton, "Διατήρηση υπόθεσης", 165);
            ConfigureActionButton(exportButton, "Εξαγωγή υπόθεσης", 165);
            ConfigureActionButton(cleanupButton, "Εκτέλεση εκκαθάρισης", 175);
            var settings = new RadButton { Text = "Πολιτική διατήρησης", Size = new Size(170, 30), Margin = new Padding(6, 0, 0, 0) };
            var openArchive = new RadButton { Text = "Φάκελος αρχείου", Size = new Size(145, 30), Margin = new Padding(6, 0, 0, 0) };
            pinButton.Click += delegate { TogglePinned(); };
            exportButton.Click += delegate { ExportSelectedSubmission(); };
            cleanupButton.Click += async delegate { await RunCleanup(); };
            settings.Click += delegate { ConfigureRetention(); };
            openArchive.Click += delegate { OpenArchiveDirectory(); };
            selectionInfo.AutoSize = true;
            selectionInfo.Margin = new Padding(15, 6, 3, 0);
            actions.Controls.Add(pinButton);
            actions.Controls.Add(exportButton);
            actions.Controls.Add(cleanupButton);
            actions.Controls.Add(settings);
            actions.Controls.Add(openArchive);
            actions.Controls.Add(selectionInfo);
            cleanupButton.Enabled = true;

            this.Controls.Add(mainSplit);
            this.Controls.Add(footer);
            this.Controls.Add(actions);
            this.Controls.Add(filterPanel);
            this.Shown += delegate
            {
                mainSplit.SplitterDistance = Math.Max(180, mainSplit.Height * 2 / 5);
                detailSplit.SplitterDistance = Math.Max(100, detailSplit.Height / 3);
            };
        }

        private static void ConfigureActionButton(RadButton button, string text, int width)
        {
            button.Text = text;
            button.Size = new Size(width, 30);
            button.Margin = new Padding(0, 0, 6, 0);
            button.Enabled = false;
        }

        private static RadLabel CreateFilterLabel(string text, int leftMargin = 3)
        {
            return new RadLabel
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(leftMargin, 5, 4, 0)
            };
        }

        private static Control CreateSection(string title, Control content)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var header = new RadLabel
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(8, 5, 0, 0),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(54, 66, 150)
            };
            content.Dock = DockStyle.Fill;
            panel.Controls.Add(content);
            panel.Controls.Add(header);
            return panel;
        }

        private static void ConfigureDatePicker(RadDateTimePicker picker)
        {
            picker.Format = DateTimePickerFormat.Custom;
            picker.CustomFormat = "dd/MM/yyyy";
            picker.AutoSize = false;
            picker.Size = new Size(112, 28);
        }

        private static RadPageViewPage CreatePayloadPage(string title, Control content)
        {
            var page = new RadPageViewPage { Text = title };
            content.Dock = DockStyle.Fill;
            content.Margin = new Padding(8);
            page.Controls.Add(content);
            return page;
        }

        private static RadTextBox CreatePayloadBox()
        {
            return new RadTextBox
            {
                ReadOnly = true,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 9F),
                BackColor = Color.White,
                ThemeName = "TelerikMetroBlue"
            };
        }

        private void ConfigureSubmissionGrid()
        {
            ConfigureGrid(submissionsGrid);
            AddTextColumn(submissionsGrid, "EntityType", "Αντικείμενο", 105);
            AddTextColumn(submissionsGrid, "EntityId", "ID αντικειμένου", 210);
            AddTextColumn(submissionsGrid, "Destination", "Προορισμός", 135);
            AddTextColumn(submissionsGrid, "Status", "Κατάσταση", 125);
            AddTextColumn(submissionsGrid, "AttemptCount", "Προσπάθειες", 85);
            AddDateColumn(submissionsGrid, "LastAttemptUtc", "Τελευταία προσπάθεια (UTC)", 165);
            AddDateColumn(submissionsGrid, "NextAttemptUtc", "Επόμενη προσπάθεια (UTC)", 165);
            AddTextColumn(submissionsGrid, "LastErrorCategory", "Κατηγορία", 115);
            AddTextColumn(submissionsGrid, "LastErrorSummary", "Τελευταίο σφάλμα", 300);
            submissionsGrid.Columns.Add(new GridViewCheckBoxColumn("IsPinned")
            {
                FieldName = "IsPinned",
                HeaderText = "Διατήρηση",
                Width = 80,
                ReadOnly = true
            });
            submissionsGrid.SelectionChanged += delegate
            {
                UpdateSelectedSubmissionActions();
                LoadSelectedSubmission();
            };
        }

        private void ConfigureAttemptGrid()
        {
            ConfigureGrid(attemptsGrid);
            AddTextColumn(attemptsGrid, "AttemptNumber", "#", 45);
            AddDateColumn(attemptsGrid, "StartedUtc", "Έναρξη (UTC)", 150);
            AddDateColumn(attemptsGrid, "FinishedUtc", "Ολοκλήρωση (UTC)", 150);
            AddTextColumn(attemptsGrid, "Outcome", "Έκβαση", 110);
            AddTextColumn(attemptsGrid, "DurationMilliseconds", "Διάρκεια ms", 95);
            AddTextColumn(attemptsGrid, "ResponseStatusCode", "HTTP", 55);
            AddTextColumn(attemptsGrid, "CorrelationId", "Correlation ID", 210);
            AddTextColumn(attemptsGrid, "MachineName", "Υπολογιστής", 130);
            AddTextColumn(attemptsGrid, "InitiatedBy", "Χρήστης", 110);
            attemptsGrid.SelectionChanged += delegate { ShowSelectedAttempt(); };
        }

        private static void ConfigureGrid(RadGridView grid)
        {
            grid.AutoGenerateColumns = false;
            grid.AllowAddNewRow = false;
            grid.AllowDeleteRow = false;
            grid.AllowEditRow = false;
            grid.MultiSelect = false;
            grid.ReadOnly = true;
            grid.ShowGroupPanel = false;
            grid.MasterTemplate.EnableFiltering = true;
            grid.MasterTemplate.AutoSizeColumnsMode = GridViewAutoSizeColumnsMode.None;
            grid.ThemeName = "TelerikMetroBlue";
            grid.Font = new Font("Segoe UI", 9F);
        }

        private static void AddTextColumn(RadGridView grid, string property, string caption, int width)
        {
            grid.Columns.Add(new GridViewTextBoxColumn(property)
            {
                FieldName = property,
                HeaderText = caption,
                Width = width,
                ReadOnly = true
            });
        }

        private static void AddDateColumn(RadGridView grid, string property, string caption, int width)
        {
            var column = new GridViewDateTimeColumn(property)
            {
                FieldName = property,
                HeaderText = caption,
                Width = width,
                FormatString = "{0:dd/MM/yyyy HH:mm:ss}",
                ReadOnly = true
            };
            grid.Columns.Add(column);
        }

        private void RefreshSubmissions()
        {
            try
            {
                DateTime fromUtc = fromDate.Value.Date.ToUniversalTime();
                DateTime toUtc = toDate.Value.Date.AddDays(1).ToUniversalTime();
                string status = Convert.ToString(statusFilter.SelectedValue);
                if (string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
                    status = null;
                IList<OutboundSubmissionAuditEntry> entries = store.LoadSubmissions(
                    fromUtc,
                    toUtc,
                    status,
                    Decimal.ToInt32(maximumRows.Value));
                submissionsGrid.DataSource = entries.ToList();
                resultCount.Text = "Εγγραφές: " + entries.Count;
                if (submissionsGrid.Rows.Count > 0)
                    submissionsGrid.CurrentRow = submissionsGrid.Rows[0];
                else
                {
                    ClearDetails();
                    UpdateSelectedSubmissionActions();
                }
                RefreshStorageStatistics();
            }
            catch (Exception ex)
            {
                resultCount.Text = "Αποτυχία φόρτωσης";
                RadMessageBox.Show(this, ex.Message, "Αποτυχία φόρτωσης ελέγχου αποστολών", MessageBoxButtons.OK, RadMessageIcon.Error);
            }
        }

        private void LoadSelectedSubmission()
        {
            var entry = GetSelectedItem<OutboundSubmissionAuditEntry>(submissionsGrid);
            if (entry == null)
            {
                ClearDetails();
                return;
            }
            try
            {
                attemptsGrid.DataSource = store.LoadAttempts(entry.SubmissionId).ToList();
                if (attemptsGrid.Rows.Count > 0)
                {
                    attemptsGrid.CurrentRow = attemptsGrid.Rows[0];
                    this.BeginInvoke(new MethodInvoker(ShowSelectedAttempt));
                }
                else
                    ClearPayloads();
            }
            catch (Exception ex)
            {
                ClearDetails();
                RadMessageBox.Show(this, ex.Message, "Αποτυχία φόρτωσης προσπαθειών", MessageBoxButtons.OK, RadMessageIcon.Error);
            }
        }

        private void ShowSelectedAttempt()
        {
            var summary = GetSelectedItem<OutboundSubmissionAttemptEntry>(attemptsGrid);
            if (summary == null)
            {
                ClearPayloads();
                return;
            }
            try
            {
                OutboundSubmissionAttemptEntry attempt = store.LoadAttempt(summary.AttemptId);
                requestText.Text = attempt == null ? string.Empty : attempt.RequestPayload ?? string.Empty;
                responseText.Text = attempt == null ? string.Empty : attempt.ResponsePayload ?? string.Empty;
                var error = new StringBuilder();
                if (attempt != null && !string.IsNullOrWhiteSpace(attempt.ErrorType))
                    error.AppendLine(attempt.ErrorType);
                if (attempt != null && !string.IsNullOrWhiteSpace(attempt.ErrorMessage))
                    error.AppendLine(attempt.ErrorMessage);
                if (attempt != null && !string.IsNullOrWhiteSpace(attempt.StackTrace))
                    error.AppendLine().Append(attempt.StackTrace);
                errorText.Text = error.ToString();
                selectionInfo.Text = attempt != null && attempt.PayloadPurgedUtc.HasValue
                    ? "Από επαληθευμένο αρχείο: " + System.IO.Path.GetFileName(attempt.ArchiveFilePath)
                    : string.Empty;
            }
            catch (Exception ex)
            {
                ClearPayloads();
                errorText.Text = ex.ToString();
                selectionInfo.Text = "Αποτυχία ανάκτησης αρχειοθετημένου payload";
            }
        }

        private void UpdateSelectedSubmissionActions()
        {
            OutboundSubmissionAuditEntry entry = GetSelectedItem<OutboundSubmissionAuditEntry>(submissionsGrid);
            pinButton.Enabled = entry != null;
            exportButton.Enabled = entry != null;
            pinButton.Text = entry != null && entry.IsPinned
                ? "Λήξη διατήρησης"
                : "Διατήρηση υπόθεσης";
            if (entry == null)
                selectionInfo.Text = string.Empty;
        }

        private void TogglePinned()
        {
            OutboundSubmissionAuditEntry entry = GetSelectedItem<OutboundSubmissionAuditEntry>(submissionsGrid);
            if (entry == null)
                return;
            try
            {
                bool pinned = !entry.IsPinned;
                store.SetPinned(entry.SubmissionId, pinned, Program.CurrentUserName);
                RefreshSubmissions();
                RadMessageBox.Show(
                    pinned
                        ? "Η υπόθεση εξαιρέθηκε από την αυτόματη εκκαθάριση."
                        : "Η υπόθεση ακολουθεί ξανά την πολιτική διατήρησης.",
                    "Έλεγχος αποστολών",
                    MessageBoxButtons.OK,
                    RadMessageIcon.Info);
            }
            catch (Exception ex)
            {
                RadMessageBox.Show(this, ex.Message, "Αποτυχία αλλαγής διατήρησης", MessageBoxButtons.OK, RadMessageIcon.Error);
            }
        }

        private void ExportSelectedSubmission()
        {
            OutboundSubmissionAuditEntry entry = GetSelectedItem<OutboundSubmissionAuditEntry>(submissionsGrid);
            if (entry == null)
                return;
            using (var dialog = new SaveFileDialog
            {
                Filter = "JSON (*.json)|*.json",
                FileName = "outbound-audit-" + entry.EntityType + "-" + entry.EntityId.ToString("N") + ".json"
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    store.ExportSubmission(entry.SubmissionId, dialog.FileName);
                    RadMessageBox.Show(this, "Η υπόθεση εξήχθη επιτυχώς.", "Εξαγωγή ελέγχου αποστολών", MessageBoxButtons.OK, RadMessageIcon.Info);
                }
                catch (Exception ex)
                {
                    RadMessageBox.Show(this, ex.Message, "Αποτυχία εξαγωγής", MessageBoxButtons.OK, RadMessageIcon.Error);
                }
            }
        }

        private async Task RunCleanup()
        {
            cleanupButton.Enabled = false;
            selectionInfo.Text = "Εκτελείται επαληθευμένη αρχειοθέτηση...";
            try
            {
                OutboundAuditCleanupResult result = await Task.Run(delegate
                {
                    return OutboundAuditArchiveWorker.RunNow(Properties.Settings.Default.DBConnection);
                });
                RefreshSubmissions();
                RadMessageBox.Show(
                    this,
                    result.Message + Environment.NewLine +
                    "Αρχειοθετημένες προσπάθειες: " + result.ArchivedAttempts + Environment.NewLine +
                    "Διαγραμμένες εγγραφές metadata: " + result.DeletedMetadataRows + Environment.NewLine +
                    "Διαγραμμένα ληγμένα αρχεία: " + result.DeletedArchiveFiles,
                    "Εκκαθάριση ελέγχου αποστολών",
                    MessageBoxButtons.OK,
                    RadMessageIcon.Info);
            }
            catch (Exception ex)
            {
                RadMessageBox.Show(this, ex.Message, "Αποτυχία εκκαθάρισης", MessageBoxButtons.OK, RadMessageIcon.Error);
            }
            finally
            {
                cleanupButton.Enabled = true;
                UpdateSelectedSubmissionActions();
            }
        }

        private void ConfigureRetention()
        {
            try
            {
                using (var form = new OutboundAuditRetentionSettingsForm(OutboundAuditRetentionPolicy.Load()))
                {
                    if (form.ShowDialog(this) != DialogResult.OK)
                        return;
                }
                RefreshStorageStatistics();
            }
            catch (Exception ex)
            {
                RadMessageBox.Show(this, ex.Message, "Αποτυχία αποθήκευσης πολιτικής", MessageBoxButtons.OK, RadMessageIcon.Error);
            }
        }

        private void OpenArchiveDirectory()
        {
            try
            {
                string path = OutboundAuditRetentionPolicy.Load().ArchiveDirectory;
                System.IO.Directory.CreateDirectory(path);
                Process.Start(path);
            }
            catch (Exception ex)
            {
                RadMessageBox.Show(this, ex.Message, "Αποτυχία ανοίγματος φακέλου", MessageBoxButtons.OK, RadMessageIcon.Error);
            }
        }

        private void RefreshStorageStatistics()
        {
            try
            {
                OutboundAuditStorageStatistics statistics = store.GetStorageStatistics();
                storageInfo.Text = "Payload στη βάση: " + FormatBytes(statistics.DatabasePayloadBytes) +
                    "   ·   Αρχεία: " + statistics.VerifiedArchiveCount + " / " + FormatBytes(statistics.ArchiveBytes) +
                    "   ·   Προσπάθειες: " + statistics.AttemptCount +
                    "   ·   Αρχειοθετημένες: " + statistics.PurgedAttemptCount;
            }
            catch
            {
                storageInfo.Text = "Τα στοιχεία μεγέθους δεν είναι διαθέσιμα.";
            }
        }

        private static string FormatBytes(long value)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = Math.Max(0, value);
            int unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return size.ToString(unit == 0 ? "0" : "0.0") + " " + units[unit];
        }

        private static T GetSelectedItem<T>(RadGridView grid) where T : class
        {
            if (grid.CurrentRow == null)
                return null;
            return grid.CurrentRow.DataBoundItem as T;
        }

        private void ClearDetails()
        {
            attemptsGrid.DataSource = null;
            ClearPayloads();
        }

        private void ClearPayloads()
        {
            requestText.Text = string.Empty;
            responseText.Text = string.Empty;
            errorText.Text = string.Empty;
        }

        private sealed class StatusOption
        {
            public StatusOption(string value, string text)
            {
                this.Value = value;
                this.Text = text;
            }

            public string Value { get; private set; }
            public string Text { get; private set; }
        }
    }

    internal sealed class OutboundAuditRetentionSettingsForm : RadForm
    {
        private readonly OutboundAuditRetentionPolicy policy;
        private readonly RadCheckBox enabled = new RadCheckBox();
        private readonly RadTextBox archiveDirectory = new RadTextBox();
        private readonly RadSpinEditor completedDays;
        private readonly RadSpinEditor failedDays;
        private readonly RadSpinEditor unknownDays;
        private readonly RadSpinEditor metadataDays;
        private readonly RadSpinEditor archiveFileDays;
        private readonly RadSpinEditor batchSize;
        private readonly RadSpinEditor runHour;
        private readonly RadSpinEditor minimumFreeSpace;

        public OutboundAuditRetentionSettingsForm(OutboundAuditRetentionPolicy policy)
        {
            if (policy == null)
                throw new ArgumentNullException("policy");
            this.policy = policy;
            this.completedDays = CreateSpin(1, 36500, policy.CompletedPayloadDays);
            this.failedDays = CreateSpin(1, 36500, policy.FailedPayloadDays);
            this.unknownDays = CreateSpin(1, 36500, policy.UnknownPayloadDays);
            this.metadataDays = CreateSpin(0, 36500, policy.MetadataDays);
            this.archiveFileDays = CreateSpin(0, 36500, policy.ArchiveFileDays);
            this.batchSize = CreateSpin(10, 500, policy.BatchSize);
            this.runHour = CreateSpin(0, 23, policy.RunHour);
            this.minimumFreeSpace = CreateSpin(100, 1000000, policy.MinimumFreeSpaceMb);

            this.Text = "Πολιτική διατήρησης ελέγχου αποστολών";
            this.ThemeName = "TelerikMetroBlue";
            this.Font = new Font("Segoe UI", 9F);
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(720, 590);
            this.MinimumSize = new Size(680, 570);
            this.ShowIcon = false;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 12,
                Padding = new Padding(16),
                BackColor = Color.White
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 235));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            for (int index = 0; index < 10; index++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

            enabled.Text = "Ενεργή αυτόματη αρχειοθέτηση";
            enabled.Checked = policy.Enabled;
            enabled.Dock = DockStyle.Fill;
            layout.Controls.Add(enabled, 0, 0);
            layout.SetColumnSpan(enabled, 3);

            archiveDirectory.Text = policy.ArchiveDirectory;
            archiveDirectory.Dock = DockStyle.Fill;
            var browse = new RadButton { Text = "Επιλογή φακέλου", Dock = DockStyle.Fill };
            browse.Click += delegate { SelectArchiveDirectory(); };
            AddRow(layout, 1, "Φάκελος αρχείου", archiveDirectory, browse);
            AddRow(layout, 2, "Payload ολοκληρωμένων", completedDays, CreateHint("ημέρες"));
            AddRow(layout, 3, "Payload αποτυχημένων", failedDays, CreateHint("ημέρες"));
            AddRow(layout, 4, "Payload άγνωστης έκβασης", unknownDays, CreateHint("ημέρες"));
            AddRow(layout, 5, "Metadata στη βάση", metadataDays, CreateHint("0 = χωρίς διαγραφή"));
            AddRow(layout, 6, "Αρχεία στο δίσκο", archiveFileDays, CreateHint("0 = χωρίς διαγραφή"));
            AddRow(layout, 7, "Μέγεθος παρτίδας", batchSize, CreateHint("10–500 προσπάθειες"));
            AddRow(layout, 8, "Ώρα ημερήσιας εκτέλεσης", runHour, CreateHint("0–23"));
            AddRow(layout, 9, "Ελάχιστος ελεύθερος χώρος", minimumFreeSpace, CreateHint("MB"));

            var explanation = new RadLabel
            {
                Text = "Τα payload διαγράφονται από τη βάση μόνο αφού το συμπιεσμένο αρχείο επαληθευτεί με SHA-256 και έλεγχο όλων των Attempt ID. Οι διατηρημένες υποθέσεις εξαιρούνται.",
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextWrap = true,
                ForeColor = Color.DimGray,
                Padding = new Padding(4, 10, 4, 4)
            };
            layout.Controls.Add(explanation, 0, 10);
            layout.SetColumnSpan(explanation, 3);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            var save = new RadButton { Text = "Αποθήκευση", Size = new Size(130, 32) };
            var cancel = new RadButton { Text = "Άκυρο", Size = new Size(110, 32) };
            save.Click += delegate { SavePolicy(); };
            cancel.Click += delegate { this.DialogResult = DialogResult.Cancel; this.Close(); };
            actions.Controls.Add(save);
            actions.Controls.Add(cancel);
            layout.Controls.Add(actions, 0, 11);
            layout.SetColumnSpan(actions, 3);
            this.Controls.Add(layout);
        }

        private static RadSpinEditor CreateSpin(decimal minimum, decimal maximum, decimal value)
        {
            return new RadSpinEditor
            {
                Minimum = minimum,
                Maximum = maximum,
                Value = Math.Max(minimum, Math.Min(maximum, value)),
                Dock = DockStyle.Fill
            };
        }

        private static RadLabel CreateHint(string text)
        {
            return new RadLabel { Text = text, Dock = DockStyle.Fill, Padding = new Padding(5, 6, 0, 0), ForeColor = Color.DimGray };
        }

        private static void AddRow(TableLayoutPanel layout, int row, string label, Control input, Control hint)
        {
            var title = new RadLabel { Text = label, Dock = DockStyle.Fill, Padding = new Padding(4, 6, 0, 0) };
            input.Margin = new Padding(3, 5, 3, 5);
            layout.Controls.Add(title, 0, row);
            layout.Controls.Add(input, 1, row);
            layout.Controls.Add(hint, 2, row);
        }

        private void SelectArchiveDirectory()
        {
            using (var dialog = new FolderBrowserDialog
            {
                Description = "Επιλέξτε φάκελο για τα συμπιεσμένα αρχεία ελέγχου αποστολών",
                SelectedPath = archiveDirectory.Text
            })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    archiveDirectory.Text = dialog.SelectedPath;
            }
        }

        private void SavePolicy()
        {
            if (string.IsNullOrWhiteSpace(archiveDirectory.Text))
            {
                RadMessageBox.Show(this, "Ο φάκελος αρχείου είναι υποχρεωτικός.", this.Text, MessageBoxButtons.OK, RadMessageIcon.Exclamation);
                return;
            }
            if ((metadataDays.Value > 0 || archiveFileDays.Value > 0) &&
                RadMessageBox.Show(
                    this,
                    "Η επιλεγμένη πολιτική επιτρέπει οριστική διαγραφή ληγμένων metadata ή αρχείων. Θέλετε να συνεχίσετε;",
                    this.Text,
                    MessageBoxButtons.YesNo,
                    RadMessageIcon.Exclamation) != DialogResult.Yes)
                return;

            policy.Enabled = enabled.Checked;
            policy.ArchiveDirectory = archiveDirectory.Text.Trim();
            policy.CompletedPayloadDays = Decimal.ToInt32(completedDays.Value);
            policy.FailedPayloadDays = Decimal.ToInt32(failedDays.Value);
            policy.UnknownPayloadDays = Decimal.ToInt32(unknownDays.Value);
            policy.MetadataDays = Decimal.ToInt32(metadataDays.Value);
            policy.ArchiveFileDays = Decimal.ToInt32(archiveFileDays.Value);
            policy.BatchSize = Decimal.ToInt32(batchSize.Value);
            policy.RunHour = Decimal.ToInt32(runHour.Value);
            policy.MinimumFreeSpaceMb = Decimal.ToInt32(minimumFreeSpace.Value);
            policy.Save();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
