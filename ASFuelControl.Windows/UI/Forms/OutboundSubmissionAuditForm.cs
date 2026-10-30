using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ASFuelControl.Windows.Threads;

namespace ASFuelControl.Windows.UI.Forms
{
    internal sealed class OutboundSubmissionAuditForm : Form
    {
        private readonly OutboundSubmissionAuditStore store;
        private readonly DateTimePicker fromDate = new DateTimePicker();
        private readonly DateTimePicker toDate = new DateTimePicker();
        private readonly ComboBox statusFilter = new ComboBox();
        private readonly NumericUpDown maximumRows = new NumericUpDown();
        private readonly DataGridView submissionsGrid = new DataGridView();
        private readonly DataGridView attemptsGrid = new DataGridView();
        private readonly RichTextBox requestText = CreatePayloadBox();
        private readonly RichTextBox responseText = CreatePayloadBox();
        private readonly RichTextBox errorText = CreatePayloadBox();

        public OutboundSubmissionAuditForm()
        {
            this.store = new OutboundSubmissionAuditStore(Properties.Settings.Default.DBConnection);
            this.Text = "Έλεγχος Εξερχόμενων Αποστολών";
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(960, 650);
            this.Size = new Size(1250, 780);

            BuildLayout();
            ConfigureSubmissionGrid();
            ConfigureAttemptGrid();

            fromDate.Value = DateTime.Today.AddDays(-7);
            toDate.Value = DateTime.Today;
            statusFilter.Items.AddRange(new object[]
            {
                "Problems", "Failed", "RetryScheduled", "Unknown", "Sending", "Completed", "All"
            });
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
                Height = 38,
                Padding = new Padding(6, 6, 6, 3),
                WrapContents = false
            };
            filterPanel.Controls.Add(new Label { Text = "Από", AutoSize = true, Margin = new Padding(3, 5, 3, 0) });
            ConfigureDatePicker(fromDate);
            filterPanel.Controls.Add(fromDate);
            filterPanel.Controls.Add(new Label { Text = "Έως", AutoSize = true, Margin = new Padding(12, 5, 3, 0) });
            ConfigureDatePicker(toDate);
            filterPanel.Controls.Add(toDate);
            filterPanel.Controls.Add(new Label { Text = "Κατάσταση", AutoSize = true, Margin = new Padding(12, 5, 3, 0) });
            statusFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            statusFilter.Width = 140;
            filterPanel.Controls.Add(statusFilter);
            filterPanel.Controls.Add(new Label { Text = "Μέγιστες", AutoSize = true, Margin = new Padding(12, 5, 3, 0) });
            maximumRows.Width = 75;
            filterPanel.Controls.Add(maximumRows);

            var refresh = new Button { Text = "Ανανέωση", AutoSize = true, Margin = new Padding(12, 0, 3, 0) };
            refresh.Click += delegate { RefreshSubmissions(); };
            filterPanel.Controls.Add(refresh);

            var mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal
            };
            submissionsGrid.Dock = DockStyle.Fill;
            mainSplit.Panel1.Controls.Add(submissionsGrid);

            var detailSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal
            };
            attemptsGrid.Dock = DockStyle.Fill;
            detailSplit.Panel1.Controls.Add(attemptsGrid);

            var payloadTabs = new TabControl { Dock = DockStyle.Fill };
            payloadTabs.TabPages.Add(CreatePayloadPage("Request", requestText));
            payloadTabs.TabPages.Add(CreatePayloadPage("Response", responseText));
            payloadTabs.TabPages.Add(CreatePayloadPage("Error", errorText));
            detailSplit.Panel2.Controls.Add(payloadTabs);
            mainSplit.Panel2.Controls.Add(detailSplit);

            this.Controls.Add(mainSplit);
            this.Controls.Add(filterPanel);
            this.Shown += delegate
            {
                mainSplit.SplitterDistance = Math.Max(180, mainSplit.Height * 2 / 5);
                detailSplit.SplitterDistance = Math.Max(100, detailSplit.Height / 3);
            };
        }

        private static void ConfigureDatePicker(DateTimePicker picker)
        {
            picker.Format = DateTimePickerFormat.Custom;
            picker.CustomFormat = "dd/MM/yyyy";
            picker.Width = 105;
        }

        private static TabPage CreatePayloadPage(string title, Control content)
        {
            var page = new TabPage(title);
            content.Dock = DockStyle.Fill;
            page.Controls.Add(content);
            return page;
        }

        private static RichTextBox CreatePayloadBox()
        {
            return new RichTextBox
            {
                ReadOnly = true,
                WordWrap = false,
                DetectUrls = false,
                Font = new Font(FontFamily.GenericMonospace, 9F),
                BackColor = SystemColors.Window
            };
        }

        private void ConfigureSubmissionGrid()
        {
            ConfigureGrid(submissionsGrid);
            AddTextColumn(submissionsGrid, "EntityType", "Object", 95);
            AddTextColumn(submissionsGrid, "EntityId", "Object ID", 190);
            AddTextColumn(submissionsGrid, "Destination", "Destination", 120);
            AddTextColumn(submissionsGrid, "Status", "Status", 105);
            AddTextColumn(submissionsGrid, "AttemptCount", "Attempts", 65);
            AddDateColumn(submissionsGrid, "LastAttemptUtc", "Last attempt (UTC)", 135);
            AddDateColumn(submissionsGrid, "NextAttemptUtc", "Next attempt (UTC)", 135);
            AddTextColumn(submissionsGrid, "LastErrorCategory", "Category", 105);
            AddTextColumn(submissionsGrid, "LastErrorSummary", "Last error", 260);
            submissionsGrid.SelectionChanged += delegate { LoadSelectedSubmission(); };
        }

        private void ConfigureAttemptGrid()
        {
            ConfigureGrid(attemptsGrid);
            AddTextColumn(attemptsGrid, "AttemptNumber", "#", 45);
            AddDateColumn(attemptsGrid, "StartedUtc", "Started (UTC)", 140);
            AddDateColumn(attemptsGrid, "FinishedUtc", "Finished (UTC)", 140);
            AddTextColumn(attemptsGrid, "Outcome", "Outcome", 90);
            AddTextColumn(attemptsGrid, "DurationMilliseconds", "Duration ms", 85);
            AddTextColumn(attemptsGrid, "ResponseStatusCode", "HTTP", 55);
            AddTextColumn(attemptsGrid, "CorrelationId", "Correlation ID", 210);
            AddTextColumn(attemptsGrid, "MachineName", "Machine", 120);
            AddTextColumn(attemptsGrid, "InitiatedBy", "User", 100);
            attemptsGrid.SelectionChanged += delegate { ShowSelectedAttempt(); };
        }

        private static void ConfigureGrid(DataGridView grid)
        {
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AutoGenerateColumns = false;
            grid.MultiSelect = false;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        }

        private static void AddTextColumn(DataGridView grid, string property, string caption, int width)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = property,
                HeaderText = caption,
                Name = property,
                Width = width,
                SortMode = DataGridViewColumnSortMode.Automatic
            });
        }

        private static void AddDateColumn(DataGridView grid, string property, string caption, int width)
        {
            var column = new DataGridViewTextBoxColumn
            {
                DataPropertyName = property,
                HeaderText = caption,
                Name = property,
                Width = width,
                SortMode = DataGridViewColumnSortMode.Automatic
            };
            column.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";
            grid.Columns.Add(column);
        }

        private void RefreshSubmissions()
        {
            try
            {
                DateTime fromUtc = fromDate.Value.Date.ToUniversalTime();
                DateTime toUtc = toDate.Value.Date.AddDays(1).ToUniversalTime();
                string status = Convert.ToString(statusFilter.SelectedItem);
                if (string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
                    status = null;
                IList<OutboundSubmissionAuditEntry> entries = store.LoadSubmissions(
                    fromUtc,
                    toUtc,
                    status,
                    Decimal.ToInt32(maximumRows.Value));
                submissionsGrid.DataSource = entries.ToList();
                if (submissionsGrid.Rows.Count > 0)
                    submissionsGrid.Rows[0].Selected = true;
                else
                    ClearDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Audit load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    attemptsGrid.Rows[0].Selected = true;
                else
                    ClearPayloads();
            }
            catch (Exception ex)
            {
                ClearDetails();
                MessageBox.Show(this, ex.Message, "Attempt load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private static T GetSelectedItem<T>(DataGridView grid) where T : class
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
            requestText.Clear();
            responseText.Clear();
            errorText.Clear();
        }
    }
}
