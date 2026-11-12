using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ASFuelControl.Windows.Threads;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Forms
{
    // One-off, administrator-only batch surface. No individual correction action is exposed.
    internal sealed class HeatingCorrectionBatchForm : RadForm
    {
        private readonly RadDateTimePicker from=new RadDateTimePicker(),to=new RadDateTimePicker();
        private readonly RadDropDownList retail=new RadDropDownList(),wholesale=new RadDropDownList(),
            retailReplacement=new RadDropDownList(),wholesaleReplacement=new RadDropDownList();
        private readonly RadDropDownList exceptionalPayment=new RadDropDownList();
        private readonly RadDropDownList invoiceTypeFilter=new RadDropDownList(),
            statusFilter=new RadDropDownList();
        private readonly RadGridView grid=new RadGridView();
        private readonly Label summary=new Label(),detail=new Label();
        private readonly RadButton scan=new RadButton(),select=new RadButton(),clear=new RadButton(),start=new RadButton(),resume=new RadButton(),pause=new RadButton(),export=new RadButton(),print=new RadButton();
        private BindingList<HeatingBatchRow> rows=new BindingList<HeatingBatchRow>();
        private BindingList<HeatingBatchRow> visibleRows=
            new BindingList<HeatingBatchRow>();
        private volatile bool stopRequested;
        private bool busy,scanned,refreshingFilters;
        private sealed class HeatingPaymentChoice
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        private sealed class HeatingInvoiceTypeChoice
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
        }
        private sealed class HeatingStatusChoice
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        internal HeatingCorrectionBatchForm(DateTime startDate,DateTime endDate)
        {
            HeatingCreditType[] creditTypes=
                SharedInvoiceRuntime.HeatingCreditTypes();
            HeatingCreditType[] replacementTypes=
                SharedInvoiceRuntime.HeatingReplacementTypes();
            Initialize(startDate,endDate,creditTypes,replacementTypes);
        }
        // The data-only constructor also permits native layout verification without a database.
        internal HeatingCorrectionBatchForm(DateTime startDate,DateTime endDate,
            HeatingCreditType[] creditTypes,HeatingCreditType[] replacementTypes)
        {
            Initialize(startDate,endDate,creditTypes,replacementTypes);
        }
        private void Initialize(DateTime startDate,DateTime endDate,
            HeatingCreditType[] creditTypes,HeatingCreditType[] replacementTypes)
        {
            Text="Εφάπαξ διόρθωση επιδόματος θέρμανσης — Μαζική πίστωση και επανέκδοση";
            ThemeName="TelerikMetroBlue";ClientSize=new Size(1230,810);MinimumSize=new Size(1040,760);StartPosition=FormStartPosition.CenterParent;
            RadDropDownList[] dropDownLists={retail,wholesale,
                retailReplacement,wholesaleReplacement,exceptionalPayment,
                invoiceTypeFilter,statusFilter};
            foreach(RadDropDownList dropDownList in dropDownLists)
            {
                dropDownList.DropDownStyle=RadDropDownStyle.DropDownList;
            }
            var top=new Panel{Dock=DockStyle.Top,Height=261};Controls.Add(top);
            LabelAt(top,"Από",16,17,40);InputAt(top,from,58,16,127);from.Value=startDate;
            LabelAt(top,"Έως",200,17,40);InputAt(top,to,242,16,127);to.Value=endDate;
            from.Format=to.Format=DateTimePickerFormat.Custom;from.CustomFormat=to.CustomFormat="dd/MM/yyyy";
            LabelAt(top,"Πληρωμή POS/IRIS/στόλου",399,17,180);
            InputAt(top,exceptionalPayment,582,16,150);
            Button(top,scan,"Έλεγχος παραστατικών",955,13,255);scan.Anchor=AnchorStyles.Top|AnchorStyles.Right;
            LabelAt(top,"Πιστωτικό λιανικής",16,65,155);InputAt(top,retail,176,64,334);
            LabelAt(top,"Πιστωτικό τιμολόγιο",533,65,170);InputAt(top,wholesale,709,64,501);wholesale.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
            LabelAt(top,"Επανέκδοση λιανικής",16,105,155);InputAt(top,retailReplacement,176,104,334);
            LabelAt(top,"Επανέκδοση τιμολογίου",533,105,170);InputAt(top,wholesaleReplacement,709,104,501);wholesaleReplacement.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
            LabelAt(top,"Τύπος αρχικού παραστατικού",16,145,155);
            InputAt(top,invoiceTypeFilter,176,144,334);
            LabelAt(top,"Κατάσταση επεξεργασίας",533,145,170);
            InputAt(top,statusFilter,709,144,501);
            statusFilter.Anchor=AnchorStyles.Top|AnchorStyles.Left|
                AnchorStyles.Right;
            Button(top,select,"Επιλογή όλων των επιλέξιμων",16,206,290);Button(top,clear,"Καθαρισμός επιλογής",319,206,227);
            summary.SetBounds(565,206,645,35);summary.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;summary.Font=new Font(Font,FontStyle.Bold);summary.TextAlign=ContentAlignment.MiddleLeft;top.Controls.Add(summary);
            var bottom=new Panel{Dock=DockStyle.Bottom,Height=210};Controls.Add(bottom);
            detail.SetBounds(16,8,1194,65);detail.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;bottom.Controls.Add(detail);
            var explanation=LabelAt(bottom,"Πιστωτικό → αποδοχή παρόχου → επανέκδοση. Διορθωτικά επί πιστώσει / συμψηφισμός, χωρίς νέα είσπραξη ή παράδοση καυσίμου.",16,79,1194);explanation.Height=32;
            var actions=new FlowLayoutPanel{Left=13,Top=117,Width=1200,Height=87,Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom};bottom.Controls.Add(actions);
            Button(actions,start,"Έναρξη παρτίδας",0,0,254);Button(actions,resume,"Συνέχιση εκκρεμοτήτων",0,0,242);
            Button(actions,pause,"Παύση",0,0,100);pause.Margin=new Padding(3,3,20,3);
            Button(actions,print,"Εκτύπωση ολοκληρωμένων",0,0,246);Button(actions,export,"Εξαγωγή αναφοράς",0,0,177);
            grid.Dock=DockStyle.Fill;grid.ShowGroupPanel=false;grid.AutoGenerateColumns=false;grid.AllowAddNewRow=false;grid.AllowDeleteRow=false;
            grid.AutoSizeColumnsMode=GridViewAutoSizeColumnsMode.Fill;grid.EnableFiltering=true;grid.AutoSizeRows=true;
            grid.Columns.Add(new GridViewCheckBoxColumn("Selected"){HeaderText="",Width=35});
            string[] fields={"Document","Provider","Supply","Amount","Credit","Replacement","Status"};
            string[] labels={"Αρχικό","Πάροχος / περιβάλλον","Παροχή","Σύνολο €","Πιστωτικό","Επανέκδοση","Κατάσταση / αιτία εξαίρεσης"};
            int[] widths={125,160,115,90,130,130,380};
            for(int i=0;i<fields.Length;i++)grid.Columns.Add(new GridViewTextBoxColumn(fields[i]){HeaderText=labels[i],Width=widths[i],ReadOnly=true,WrapText=true});
            grid.Columns["Amount"].FormatString="{0:N2}";
            Controls.Add(grid);grid.BringToFront();grid.DataSource=visibleRows;
            grid.CellBeginEdit+=(s,e)=>{var row=grid.CurrentRow?.DataBoundItem as HeatingBatchRow;if(busy||row?.Eligible!=true)e.Cancel=true;};
            grid.CellValueChanged+=(s,e)=>UpdateButtons();
            scan.Click+=async(s,e)=>await Scan();
            select.Click+=(s,e)=>{foreach(var r in visibleRows)r.Selected=r.Eligible;visibleRows.ResetBindings();UpdateButtons();};
            clear.Click+=(s,e)=>{foreach(var r in rows)r.Selected=false;visibleRows.ResetBindings();UpdateButtons();};
            start.Click+=async(s,e)=>await Run(false);resume.Click+=async(s,e)=>await Run(true);
            pause.Click+=(s,e)=>{stopRequested=true;detail.Text="Ζητήθηκε παύση. Ολοκληρώνεται η τρέχουσα επικοινωνία και αποθηκεύεται η πρόοδος.";pause.Enabled=false;};
            export.Click+=(s,e)=>Export();print.Click+=async(s,e)=>await Print();
            from.ValueChanged+=(s,e)=>InvalidateScan();to.ValueChanged+=(s,e)=>InvalidateScan();
            retail.SelectedIndexChanged+=(s,e)=>InvalidateScan();wholesale.SelectedIndexChanged+=(s,e)=>InvalidateScan();
            retailReplacement.SelectedIndexChanged+=(s,e)=>InvalidateScan();wholesaleReplacement.SelectedIndexChanged+=(s,e)=>InvalidateScan();
            exceptionalPayment.SelectedIndexChanged+=(s,e)=>InvalidateScan();
            invoiceTypeFilter.SelectedIndexChanged+=(s,e)=>ApplyCatalogFilters();
            statusFilter.SelectedIndexChanged+=(s,e)=>ApplyCatalogFilters();
            FormClosing+=(s,e)=>{if(busy){stopRequested=true;e.Cancel=true;detail.Text="Η παρτίδα σταματά μετά την τρέχουσα επικοινωνία. Κλείστε το παράθυρο όταν ολοκληρωθεί η παύση.";}};
            retail.DataSource=creditTypes.Where(t=>t.Retail).ToArray();retail.DisplayMember="Name";retail.ValueMember="Id";
            wholesale.DataSource=creditTypes.Where(t=>!t.Retail).ToArray();wholesale.DisplayMember="Name";wholesale.ValueMember="Id";
            retailReplacement.DataSource=replacementTypes.Where(t=>t.Retail).ToArray();retailReplacement.DisplayMember="Name";retailReplacement.ValueMember="Id";
            wholesaleReplacement.DataSource=replacementTypes.Where(t=>!t.Retail).ToArray();wholesaleReplacement.DisplayMember="Name";wholesaleReplacement.ValueMember="Id";
            exceptionalPayment.DataSource=new[]{
                new HeatingPaymentChoice{Id=1,Name="Μετρητά"},
                new HeatingPaymentChoice{Id=0,Name="Επί πιστώσει"}};
            exceptionalPayment.DisplayMember="Name";exceptionalPayment.ValueMember="Id";
            invoiceTypeFilter.DisplayMember="Name";
            invoiceTypeFilter.ValueMember="Id";
            statusFilter.DisplayMember="Name";
            statusFilter.ValueMember="Id";
            statusFilter.DataSource=new[]{
                new HeatingStatusChoice{Id=0,Name="Όλες οι καταστάσεις"},
                new HeatingStatusChoice{Id=1,Name="Μόνο επιτυχώς ολοκληρωμένα"},
                new HeatingStatusChoice{Id=2,Name="Μόνο μη ολοκληρωμένα"}};
            RefreshCatalogFilters();
            detail.Text="Επιλέξτε ημερομηνίες και τύπους πιστωτικών / επανέκδοσης. Η συνέχεια ανακτά όλες τις αποθηκευμένες εκκρεμότητες, ακόμη και εκτός του διαστήματος.";UpdateButtons();
        }
        private static Label LabelAt(Control p,string text,int x,int y,int width){var l=new Label{Text=text,Bounds=new Rectangle(x,y,width,27),TextAlign=ContentAlignment.MiddleLeft};p.Controls.Add(l);return l;}
        private static void InputAt(Control p,Control input,int x,int y,int width){input.AutoSize=false;input.SetBounds(x,y,width,28);p.Controls.Add(input);}
        private static void Button(Control p,RadButton b,string text,int x,int y,int width){b.Text=text;b.SetBounds(x,y,width,35);p.Controls.Add(b);}
        private void InvalidateScan(){if(busy)return;scanned=false;foreach(var r in rows){r.Selected=false;r.Eligible=false;}visibleRows.ResetBindings();UpdateButtons();}
        private void UpdateButtons()
        {
            int count=visibleRows.Count(r=>r.Eligible&&r.Selected);decimal total=visibleRows.Where(r=>r.Eligible&&r.Selected).Sum(r=>r.Amount);
            summary.Text="Επιλεγμένα: "+count+"   ·   "+total.ToString("N2",CultureInfo.GetCultureInfo("el-GR"))+" €";
            scan.Enabled=select.Enabled=clear.Enabled=from.Enabled=to.Enabled=
                retail.Enabled=wholesale.Enabled=retailReplacement.Enabled=
                wholesaleReplacement.Enabled=exceptionalPayment.Enabled=!busy;
            invoiceTypeFilter.Enabled=statusFilter.Enabled=!busy&&rows.Count>0;
            start.Enabled=!busy&&scanned&&count>0;resume.Enabled=!busy;export.Enabled=!busy&&visibleRows.Count>0;print.Enabled=!busy;pause.Enabled=busy&&!stopRequested;
        }
        private void Merge(HeatingBatchRow[] updated)
        {
            foreach(var row in updated)
            {
                var existing=rows.FirstOrDefault(value=>value.Id==row.Id);
                if(existing!=null)
                {
                    rows[rows.IndexOf(existing)]=row;
                }
                else
                {
                    rows.Add(row);
                }
            }
            RefreshCatalogFilters();
        }
        private void RefreshCatalogFilters()
        {
            Guid selectedType=invoiceTypeFilter.SelectedValue is Guid?
                (Guid)invoiceTypeFilter.SelectedValue:Guid.Empty;
            var choices=rows.Where(row=>row.InvoiceTypeId!=Guid.Empty)
                .GroupBy(row=>row.InvoiceTypeId)
                .Select(group=>new HeatingInvoiceTypeChoice
                {
                    Id=group.Key,
                    Name=group.Select(row=>row.InvoiceType)
                        .FirstOrDefault(name=>!string.IsNullOrWhiteSpace(name))??
                        group.Key.ToString()
                })
                .OrderBy(choice=>choice.Name)
                .ToList();
            choices.Insert(0,new HeatingInvoiceTypeChoice
            {
                Id=Guid.Empty,
                Name="Όλοι οι υπάρχοντες τύποι"
            });
            refreshingFilters=true;
            invoiceTypeFilter.DataSource=choices;
            bool selectedTypeExists=choices.Any(choice=>choice.Id==selectedType);
            invoiceTypeFilter.SelectedValue=selectedTypeExists?selectedType:
                Guid.Empty;
            refreshingFilters=false;
            ApplyCatalogFilters();
        }
        private void ApplyCatalogFilters()
        {
            if(refreshingFilters)
            {
                return;
            }
            Guid selectedType=invoiceTypeFilter.SelectedValue is Guid?
                (Guid)invoiceTypeFilter.SelectedValue:Guid.Empty;
            int selectedStatus=statusFilter.SelectedValue is int?
                (int)statusFilter.SelectedValue:0;
            var filtered=rows.Where(row=>(selectedType==Guid.Empty||
                    row.InvoiceTypeId==selectedType)&&
                (selectedStatus==0||(selectedStatus==1&&row.Completed)||
                    (selectedStatus==2&&!row.Completed))).ToList();
            foreach(var row in rows.Except(filtered))
            {
                row.Selected=false;
            }
            visibleRows=new BindingList<HeatingBatchRow>(filtered);
            grid.DataSource=visibleRows;
            UpdateButtons();
        }
        private async Task Scan()
        {
            var first=from.Value.Date;var last=to.Value.Date;var r=retail.SelectedValue is Guid?(Guid)retail.SelectedValue:Guid.Empty;var w=wholesale.SelectedValue is Guid?(Guid)wholesale.SelectedValue:Guid.Empty;
            var rr=retailReplacement.SelectedValue is Guid?(Guid)retailReplacement.SelectedValue:Guid.Empty;var wr=wholesaleReplacement.SelectedValue is Guid?(Guid)wholesaleReplacement.SelectedValue:Guid.Empty;
            int ep=exceptionalPayment.SelectedValue is int?
                (int)exceptionalPayment.SelectedValue:1;
            busy=true;scanned=false;stopRequested=false;UpdateButtons();
            try{var values=await Task.Run(()=>SharedInvoiceRuntime.ScanHeating(first,last,r,w,rr,wr,ep,()=>stopRequested));rows=new BindingList<HeatingBatchRow>(values.ToList());RefreshCatalogFilters();scanned=!stopRequested;detail.Text=stopRequested?"Ο έλεγχος διακόπηκε. Εκτελέστε νέο έλεγχο πριν ξεκινήσετε παρτίδα.":"Ο έλεγχος ολοκληρώθηκε. Δεν έγινε έκδοση, πληρωμή ή εκτύπωση. Οι εξαιρέσεις δεν μπορούν να επιλεγούν.";}
            catch(Exception ex){Error(ex);}finally{busy=false;UpdateButtons();}
        }
        private async Task Run(bool continuing)
        {
            grid.EndEdit();var selected=rows.ToArray();busy=true;stopRequested=false;UpdateButtons();
            IProgress<string> progress=new Progress<string>(text=>detail.Text=text);
            try{var updated=await Task.Run(()=>SharedInvoiceRuntime.RunHeating(selected,continuing,()=>stopRequested,text=>progress.Report(text)));Merge(updated);scanned=false;detail.Text=stopRequested?"Η παρτίδα τέθηκε σε παύση. Η πρόοδος αποθηκεύτηκε.":"Η εκτέλεση ολοκληρώθηκε. Ελέγξτε την κατάσταση κάθε γραμμής. Η συνέχεια χειρίζεται μόνο τις εκκρεμότητες.";}
            catch(Exception ex){scanned=false;Error(ex);}finally{busy=false;UpdateButtons();}
        }
        private async Task Print()
        {busy=true;stopRequested=false;UpdateButtons();try{await Task.Run(()=>SharedInvoiceRuntime.PrintHeatingCompleted(()=>stopRequested));detail.Text=stopRequested?"Η εκτύπωση τέθηκε σε παύση. Η επόμενη εκτύπωση συνεχίζει τα υπόλοιπα έγγραφα.":"Ολοκληρώθηκε η αποστολή των διαθέσιμων εγγράφων στην εκτύπωση. Ελέγξτε τον εκτυπωτή.";}catch(Exception ex){Error(ex);}finally{busy=false;UpdateButtons();}}
        private void Export()
        {
            SharedInvoiceRuntime.RequireHeatingAdmin();
            using(var d=new SaveFileDialog{Filter="CSV (*.csv)|*.csv",FileName="Διόρθωση επιδόματος θέρμανσης.csv"})if(d.ShowDialog(this)==DialogResult.OK){
                Func<string,string> q=v=>"\""+((v??"").Length>0&&"=+-@".Contains((v??"")[0])?"'":"")+(v??"").Replace("\"","\"\"")+"\"";
                var lines=new[]{"Αρχικό;Πάροχος;Παροχή;Σύνολο;Πιστωτικό;Επανέκδοση;Κατάσταση"}.Concat(visibleRows.Select(r=>string.Join(";",new[]{r.Document,r.Provider,r.Supply,r.Amount.ToString(CultureInfo.GetCultureInfo("el-GR")),r.Credit,r.Replacement,r.Status}.Select(q))));
                File.WriteAllLines(d.FileName,lines,new UTF8Encoding(true));
            }
        }
        private void Error(Exception ex){detail.Text=ex.Message;RadMessageBox.Show(this,ex.Message,"Διόρθωση επιδόματος θέρμανσης",MessageBoxButtons.OK,RadMessageIcon.Error);}
    }
}
