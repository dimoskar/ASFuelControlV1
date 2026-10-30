using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
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

            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8),
                WrapContents = false,
                BackColor = Color.White
            };
            var close = new RadButton { Text = "Κλείσιμο", Size = new Size(125, 30) };
            close.Click += delegate { this.Close(); };
            footer.Controls.Add(close);

            this.Controls.Add(mainSplit);
            this.Controls.Add(footer);
            this.Controls.Add(filterPanel);
            this.Shown += delegate
            {
                mainSplit.SplitterDistance = Math.Max(180, mainSplit.Height * 2 / 5);
                detailSplit.SplitterDistance = Math.Max(100, detailSplit.Height / 3);
            };
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
            submissionsGrid.SelectionChanged += delegate { LoadSelectedSubmission(); };
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
                    ClearDetails();
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
            }
            catch (Exception ex)
            {
                ClearPayloads();
                errorText.Text = ex.ToString();
            }
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
}
