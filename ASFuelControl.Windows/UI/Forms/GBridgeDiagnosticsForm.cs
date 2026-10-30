using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    internal sealed class GBridgeDiagnosticsForm : RadForm
    {
        private readonly DateTimePicker from = new DateTimePicker { Width = 125 };
        private readonly DateTimePicker to = new DateTimePicker { Width = 125 };
        private readonly RadTextBox state = new RadTextBox { Width = 105 };
        private readonly RadTextBox invoice = new RadTextBox { Width = 235 };
        private readonly RadTextBox uid = new RadTextBox { Width = 150 };
        private readonly RadTextBox transaction = new RadTextBox { Width = 140 };
        private readonly RadTextBox rrn = new RadTextBox { Width = 115 };
        private readonly RadTextBox tid = new RadTextBox { Width = 105 };
        private readonly DataGridView attempts = new DataGridView { Dock = DockStyle.Fill,ReadOnly = true,
            AutoGenerateColumns = true,AllowUserToAddRows = false,AllowUserToDeleteRows = false,SelectionMode = DataGridViewSelectionMode.FullRowSelect };
        private readonly DataGridView events = new DataGridView { Dock = DockStyle.Fill,ReadOnly = true,
            AutoGenerateColumns = true,AllowUserToAddRows = false,AllowUserToDeleteRows = false,SelectionMode = DataGridViewSelectionMode.FullRowSelect };
        private readonly RadTextBox evidence = new RadTextBox { Dock = DockStyle.Fill,Multiline = true,ReadOnly = true,ScrollBars = ScrollBars.Both };
        private List<AttemptRow> loaded = new List<AttemptRow>();

        internal GBridgeDiagnosticsForm(Guid? initialInvoiceId = null)
        {
            Text = "Διαγνωστικά πληρωμών GBridge";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1280,760);
            MinimumSize = new Size(980,600);
            from.Value = initialInvoiceId.HasValue && initialInvoiceId != Guid.Empty ? new DateTime(2000,1,1) : DateTime.Today.AddDays(-7);
            to.Value = DateTime.Today;
            if (initialInvoiceId.HasValue && initialInvoiceId != Guid.Empty) invoice.Text = initialInvoiceId.Value.ToString();

            var filters = new FlowLayoutPanel { Dock = DockStyle.Fill,WrapContents = true,AutoScroll = true,Padding = new Padding(4) };
            AddFilter(filters,"Από",from); AddFilter(filters,"Έως",to); AddFilter(filters,"Κατάσταση",state);
            AddFilter(filters,"Παραστατικό",invoice); AddFilter(filters,"UID",uid); AddFilter(filters,"Συναλλαγή",transaction);
            AddFilter(filters,"RRN",rrn); AddFilter(filters,"TID",tid);
            var search = new RadButton { Text = "Αναζήτηση",Width = 110,Height = 28 };
            var open = new RadButton { Text = "Άνοιγμα παραστατικού",Width = 155,Height = 28 };
            search.Click += (s,e) => Reload(); open.Click += (s,e) => OpenInvoice();
            filters.Controls.Add(search); filters.Controls.Add(open);

            var details = new SplitContainer { Dock = DockStyle.Fill,Orientation = Orientation.Vertical,SplitterDistance = 560 };
            details.Panel1.Controls.Add(events); details.Panel2.Controls.Add(evidence);
            var body = new SplitContainer { Dock = DockStyle.Fill,Orientation = Orientation.Horizontal,SplitterDistance = 310 };
            body.Panel1.Controls.Add(attempts); body.Panel2.Controls.Add(details);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill,ColumnCount = 1,RowCount = 2 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,96)); layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            layout.Controls.Add(filters,0,0); layout.Controls.Add(body,0,1); Controls.Add(layout);
            attempts.SelectionChanged += (s,e) => LoadEvidence();
            attempts.CellDoubleClick += (s,e) => OpenInvoice();
            Shown += (s,e) => Reload();
        }
        private static void AddFilter(FlowLayoutPanel panel,string label,Control control)
        {
            panel.Controls.Add(new Label { Text = label,AutoSize = true,Padding = new Padding(5,7,0,0) });
            panel.Controls.Add(control);
        }
        private void Reload()
        {
            try
            {
                loaded = ReadAttempts(from.Value.Date,to.Value.Date.AddDays(1));
                Guid invoiceFilter;
                bool hasInvoice = Guid.TryParse(invoice.Text.Trim(),out invoiceFilter);
                loaded = loaded.Where(a => (!hasInvoice || a.InvoiceId == invoiceFilter) && Contains(a.State,state.Text) &&
                    Contains(a.TransactionUid,uid.Text) && Contains(a.TransactionId,transaction.Text) &&
                    Contains(a.Rrn,rrn.Text) && Contains(a.TerminalId,tid.Text)).ToList();
                attempts.DataSource = loaded;
                if (attempts.Columns[nameof(AttemptRow.RequestJson)] != null) attempts.Columns[nameof(AttemptRow.RequestJson)].Visible = false;
                if (attempts.Columns[nameof(AttemptRow.SnapshotJson)] != null) attempts.Columns[nameof(AttemptRow.SnapshotJson)].Visible = false;
                if (attempts.Rows.Count > 0) attempts.Rows[0].Selected = true; else { events.DataSource = null; evidence.Text = "Δεν βρέθηκαν προσπάθειες."; }
            }
            catch (Exception ex) { RadMessageBox.Show(ex.Message,"Διαγνωστικά GBridge",MessageBoxButtons.OK,RadMessageIcon.Error); }
        }
        private static bool Contains(string value,string filter)
        { return string.IsNullOrWhiteSpace(filter) || (value ?? "").IndexOf(filter.Trim(),StringComparison.OrdinalIgnoreCase) >= 0; }
        private List<AttemptRow> ReadAttempts(DateTime start,DateTime end)
        {
            const string sql = @"SELECT a.AttemptId,a.DocumentId,a.Operation,a.CreatedUtc,a.OriginalAttemptId,r.State,r.RecordedUtc,r.SnapshotJson,a.RequestPayload
FROM dbo.FiscalAttempt a JOIN dbo.FiscalAttemptRevision r ON r.AttemptId=a.AttemptId AND r.Revision=a.CurrentRevision
WHERE a.DocumentKind=1 AND a.Family='Payment' AND a.CreatedUtc>=@from AND a.CreatedUtc<@to
ORDER BY a.CreatedUtc DESC,a.AttemptSequence DESC;";
            var result = new List<AttemptRow>();
            using (var connection = new SqlConnection(Properties.Settings.Default.DBConnection))
            using (var command = new SqlCommand(sql,connection))
            {
                command.Parameters.AddWithValue("@from",start); command.Parameters.AddWithValue("@to",end);
                connection.Open();
                using (var reader = command.ExecuteReader()) while (reader.Read())
                {
                    var row = new AttemptRow { AttemptId = reader.GetGuid(0),InvoiceId = reader.GetGuid(1),Operation = reader.GetString(2),
                        Created = DateTime.SpecifyKind(reader.GetDateTime(3),DateTimeKind.Utc).ToLocalTime(),
                        OriginalAttemptId = reader.IsDBNull(4) ? (Guid?)null : reader.GetGuid(4),State = reader.GetString(5),
                        Updated = DateTime.SpecifyKind(reader.GetDateTime(6),DateTimeKind.Utc).ToLocalTime(),SnapshotJson = reader.GetString(7),
                        RequestJson = Encoding.UTF8.GetString((byte[])reader[8]) };
                    ReadIdentifiers(row); result.Add(row);
                }
            }
            return result;
        }
        private static void ReadIdentifiers(AttemptRow row)
        {
            try
            {
                var snapshot = JObject.Parse(row.SnapshotJson);
                var attempt = snapshot["Attempt"] ?? snapshot;
                row.TransactionUid = (string)attempt["TransactionUid"];
                row.TransactionId = (string)attempt["TransactionId"];
                row.Rrn = (string)attempt["Rrn"];
                row.TerminalId = (string)attempt["ResponseTerminalId"] ?? (string)attempt["TerminalId"];
            }
            catch { }
            if (row.Operation == "Refund" || row.Operation == "Void")
            {
                try
                {
                    var request = JObject.Parse(row.RequestJson);
                    var value = request["Request"] ?? request;
                    row.TransactionUid = row.TransactionUid ?? (string)value["TransactionUid"];
                    row.TransactionId = row.TransactionId ?? (string)value["OriginalTransactionId"];
                    row.TerminalId = row.TerminalId ?? (string)value["TerminalId"];
                }
                catch { }
            }
        }
        private void LoadEvidence()
        {
            var row = attempts.CurrentRow?.DataBoundItem as AttemptRow;
            if (row == null) return;
            try
            {
                var eventRows = new List<EventRow>();
                var raw = new JArray { new JObject { ["AttemptRequest"] = ParseOrString(row.RequestJson),["AttemptProjection"] = ParseOrString(row.SnapshotJson) } };
                using (var connection = new SqlConnection(Properties.Settings.Default.DBConnection))
                {
                    connection.Open();
                    if (!string.IsNullOrWhiteSpace(row.TransactionUid))
                    using (var command = new SqlCommand("SELECT RecordedUtc,EventJson FROM dbo.FiscalPaymentEvent WHERE TransactionUid=@uid ORDER BY RecordedUtc,EventId",connection))
                    {
                        command.Parameters.AddWithValue("@uid",row.TransactionUid);
                        using (var reader = command.ExecuteReader()) while (reader.Read())
                        {
                            var json = reader.GetString(1); var token = JObject.Parse(json);
                            eventRows.Add(new EventRow { Recorded = DateTime.SpecifyKind(reader.GetDateTime(0),DateTimeKind.Utc).ToLocalTime(),
                                InputType = (string)token["InputType"],Status = (string)token["Status"],HttpStatus = (int?)token["HttpStatus"],
                                Reply = (string)token["ReplyCode"] ?? (string)token["ErrorCode"],Description = (string)token["ReplyDescription"] ?? (string)token["ErrorMessage"] });
                            raw.Add(new JObject { ["PaymentEvent"] = token });
                        }
                    }
                    using (var command = new SqlCommand("SELECT StartedUtc,RequestJson,CompletedUtc,ResponseJson FROM dbo.FiscalHttpExchange WHERE InvoiceId=@invoice ORDER BY StartedUtc,ExchangeId",connection))
                    {
                        command.Parameters.AddWithValue("@invoice",row.InvoiceId);
                        using (var reader = command.ExecuteReader()) while (reader.Read()) raw.Add(new JObject {
                            ["StartedUtc"] = reader.GetDateTime(0),["Request"] = ParseOrString(reader.GetString(1)),
                            ["CompletedUtc"] = reader.IsDBNull(2) ? null : JToken.FromObject(reader.GetDateTime(2)),
                            ["Response"] = reader.IsDBNull(3) ? null : ParseOrString(reader.GetString(3)) });
                    }
                }
                events.DataSource = eventRows;
                evidence.Text = GBridgeEvidenceRedactor.Redact(raw).ToString(Formatting.Indented);
            }
            catch (Exception ex) { events.DataSource = null; evidence.Text = ex.Message; }
        }
        private static JToken ParseOrString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return JValue.CreateNull();
            try { return JToken.Parse(value); } catch { return new JValue(value); }
        }
        private void OpenInvoice()
        {
            var row = attempts.CurrentRow?.DataBoundItem as AttemptRow;
            if (row == null) return;
            try { var form = InvoiceEditorFormFactory.CreateForInvoice(row.InvoiceId); form.LoadInvoice(row.InvoiceId); form.Show(this); }
            catch (Exception ex) { RadMessageBox.Show(ex.Message,"Άνοιγμα παραστατικού",MessageBoxButtons.OK,RadMessageIcon.Error); }
        }
        private sealed class AttemptRow
        {
            public DateTime Created { get; set; }
            public DateTime Updated { get; set; }
            public Guid InvoiceId { get; set; }
            public Guid AttemptId { get; set; }
            public Guid? OriginalAttemptId { get; set; }
            public string Operation { get; set; }
            public string State { get; set; }
            public string TransactionUid { get; set; }
            public string TransactionId { get; set; }
            public string Rrn { get; set; }
            public string TerminalId { get; set; }
            public string RequestJson { get; set; }
            public string SnapshotJson { get; set; }
        }
        private sealed class EventRow
        {
            public DateTime Recorded { get; set; }
            public string InputType { get; set; }
            public int? HttpStatus { get; set; }
            public string Status { get; set; }
            public string Reply { get; set; }
            public string Description { get; set; }
        }
    }

    internal static class GBridgeEvidenceRedactor
    {
        private static readonly Regex Sensitive = new Regex("password|secret|token|authorization|signature|providerinput|receipt|cardpan|pan|apikey|api_key",RegexOptions.IgnoreCase | RegexOptions.Compiled);
        internal static JToken Redact(JToken value)
        {
            if (value == null) return JValue.CreateNull();
            var clone = value.DeepClone(); RedactNode(clone); return clone;
        }
        private static void RedactNode(JToken value)
        {
            var container = value as JContainer;
            if (container == null) return;
            foreach (var property in container.DescendantsAndSelf().OfType<JProperty>().ToArray())
            {
                if (property.Name == "InputType") continue;
                if (Sensitive.IsMatch(property.Name)) { property.Value = "[REDACTED]"; continue; }
                var text = property.Value.Type == JTokenType.String ? (string)property.Value : null;
                if (!string.IsNullOrWhiteSpace(text) && (text.TrimStart().StartsWith("{") || text.TrimStart().StartsWith("[")))
                {
                    try { var nested = JToken.Parse(text); RedactNode(nested); property.Value = nested; } catch { }
                }
            }
        }
    }
}
