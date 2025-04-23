namespace ASFuelControl.Reports
{
    partial class ReportViewerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.reportViewer1 = new Telerik.ReportViewer.WinForms.ReportViewer();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // reportViewer1
            // 
            this.reportViewer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.reportViewer1.Location = new System.Drawing.Point(0, 0);
            this.reportViewer1.Name = "reportViewer1";
            this.reportViewer1.Resources.AllFiles = "Όλα τα αρχεία";
            this.reportViewer1.Resources.BackToolTip = "Πίσω στο Ιστορικό";
            this.reportViewer1.Resources.CurrentPageToolTip = "Τρέχουσα σελίδα";
            this.reportViewer1.Resources.DocumentMapToolTip = "Κάντε κλικ για να κλείσετε το χάρτη εγγράφου | Κάντε κλικ για να ανοίξετε το χάρτ" +
    "η έγγραφου";
            this.reportViewer1.Resources.ExportError = "Σφάλμα Εξαγωγής";
            this.reportViewer1.Resources.ExportToolTip = "Εξαγωγή";
            this.reportViewer1.Resources.FirstPageToolTip = "Πρώτη Σελίδα";
            this.reportViewer1.Resources.ForwardToolTip = "Εμπρός στο Ιστορικό";
            this.reportViewer1.Resources.GenericMessageBoxCaption = "Προβολή Αναφοράς";
            this.reportViewer1.Resources.LabelOf = "από";
            this.reportViewer1.Resources.LastPageToolTip = "Τελευταία Σελίδα";
            this.reportViewer1.Resources.MissingReportSource = "Η πηγή του ορισμού της αναφοράς δεν έχει καθοριστεί.";
            this.reportViewer1.Resources.NextPageToolTip = "Επόμενη Σελίδα";
            this.reportViewer1.Resources.NoPageToDisplay = "Δεν υπάρχει σελίδα για εμφανιση.";
            this.reportViewer1.Resources.PageSetupToolTip = "Διάταξη Σελίδας";
            this.reportViewer1.Resources.ParametersToolTip = "Κάντε κλικ για να κλείσετε τη περιοχή παραμέτρων | Κάντε κλικ για να ανοίξετε τη " +
    "περιοχή παραμέτρων";
            this.reportViewer1.Resources.PreviousPageToolTip = "Προηγούμενη Σελίδα";
            this.reportViewer1.Resources.PrintPreviewToolTip = "Αλλαγή σε διαδραστική διαρρύθμιση | Αλλαγή σε προεπισκόπηση εκτύπωσης";
            this.reportViewer1.Resources.PrintToolTip = "Εκτύπωση Αναφοράς";
            this.reportViewer1.Resources.ProcessingReportMessage = "Δημιουργία αναφοράς...";
            this.reportViewer1.Resources.RefreshToolTip = "Ανανέωση";
            this.reportViewer1.Resources.ReportParametersEmptyStringError = "Δεν επιτρέπεται η κενή συμβολοσειρά";
            this.reportViewer1.Resources.ReportParametersFalseValueLabel = "Αρνητικό";
            this.reportViewer1.Resources.ReportParametersInputDataError = "Λείπει ή δεν είναι έγκυρη η τιμή παραμέτρου. Παρακαλώ εισάγετε έγκυρα στοιχεία γι" +
    "α όλες τις παραμέτρους.";
            this.reportViewer1.Resources.ReportParametersInvalidValueText = "Μη έγκυρη τιμή.";
            this.reportViewer1.Resources.ReportParametersNoValueText = "Απαιτούμενη τιμή.";
            this.reportViewer1.Resources.ReportParametersNullText = "Κενό";
            this.reportViewer1.Resources.ReportParametersPreviewButtonText = "Προεπισκόπηση";
            this.reportViewer1.Resources.ReportParametersSelectAllText = "<επιλογή όλων>";
            this.reportViewer1.Resources.ReportParametersSelectAValueText = "<επιλέξτε τιμή>";
            this.reportViewer1.Resources.ReportParametersTrueValueLabel = "Θετικό";
            this.reportViewer1.Resources.StopProcessing = "Η Επεξεργασία Αναφοράς ακυρώθηκε.";
            this.reportViewer1.Resources.StopToolTip = "Διακοπή";
            this.reportViewer1.Resources.TotalPagesToolTip = "Σύνολο Σελίδων";
            this.reportViewer1.Resources.ZoomToolTip = "Μεγένθυση";
            this.reportViewer1.Resources.ZoomToPageWidth = "Πλάτος Σελίδας";
            this.reportViewer1.Resources.ZoomToWholePage = "Ολόκληρη σελίδα";
            this.reportViewer1.Size = new System.Drawing.Size(1016, 575);
            this.reportViewer1.TabIndex = 1;
            // 
            // ReportViewerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1016, 575);
            this.Controls.Add(this.reportViewer1);
            this.Name = "ReportViewerForm";
            // 
            // 
            // 
            this.RootElement.ApplyShapeToControl = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ReportViewerForm";
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Telerik.ReportViewer.WinForms.ReportViewer reportViewer1;

    }
}