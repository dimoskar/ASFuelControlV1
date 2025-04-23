using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.WinControls.UI;
using Telerik.WinControls.UI.Localization;

namespace ASFuelControl.Windows
{
    public class GridViewLocalization : RadGridLocalizationProvider
    {
        public override string GetLocalizedString(string id)
        {
            switch (id)
            {
                case RadGridStringId.ConditionalFormattingPleaseSelectValidCellValue: return "Παρακαλώ επιλέξτε μια έγκυρη τιμή";
                case RadGridStringId.ConditionalFormattingPleaseSetValidCellValue: return "Παρακαλώ εισάγετε μια έγκυρη τιμή";
                case RadGridStringId.ConditionalFormattingPleaseSetValidCellValues: return "Παρακαλώ εισάγετε μια έγκυρη τιμή";
                case RadGridStringId.ConditionalFormattingPleaseSetValidExpression: return "Παρακαλώ εισάγετε μια έγκυρη έκφραση";
                case RadGridStringId.ConditionalFormattingItem: return "Τμήμα";

                case RadGridStringId.ConditionalFormattingInvalidParameters: return "Ακυροι παράμετροι";
                case RadGridStringId.FilterFunctionBetween: return "Μεταξύ";
                case RadGridStringId.FilterFunctionContains: return "Περιέχει";
                case RadGridStringId.FilterFunctionDoesNotContain: return "Δεν περιέχει";
                case RadGridStringId.FilterFunctionEndsWith: return "Τελειώνει με";
                case RadGridStringId.FilterFunctionEqualTo: return "Ισούτε";
                case RadGridStringId.FilterFunctionGreaterThan: return "Μεγαλύτερο από";
                case RadGridStringId.FilterFunctionGreaterThanOrEqualTo: return "Μεγαλύτερο ίσο από";
                case RadGridStringId.FilterFunctionIsEmpty: return "Κενό";
                case RadGridStringId.FilterFunctionIsNull: return "Είναι αδιευκρίνιστο";
                case RadGridStringId.FilterFunctionLessThan: return "Μικρότερο απο";
                case RadGridStringId.FilterFunctionLessThanOrEqualTo: return "Μικρότερο ίσο από";
                case RadGridStringId.FilterFunctionNoFilter: return "Χωρίς Φίλτρο";
                case RadGridStringId.FilterFunctionNotBetween: return "Όχι ανάμεσα";
                case RadGridStringId.FilterFunctionNotEqualTo: return "Δεν ισούτε με";
                case RadGridStringId.FilterFunctionNotIsEmpty: return "Είναι κενό";
                case RadGridStringId.FilterFunctionNotIsNull: return "Εχει τιμή";
                case RadGridStringId.FilterFunctionStartsWith: return "Αρχίζει με ";
                case RadGridStringId.FilterFunctionCustom: return "Προσαρμοσμένο";

                case RadGridStringId.FilterOperatorBetween: return "Ανάμεα";
                case RadGridStringId.FilterOperatorContains: return "Περιέχει";
                case RadGridStringId.FilterOperatorDoesNotContain: return "Δεν Περιέχει";
                case RadGridStringId.FilterOperatorEndsWith: return "Τελιώνει με";
                case RadGridStringId.FilterOperatorEqualTo: return "Ισούται";
                case RadGridStringId.FilterOperatorGreaterThan: return "Μεγαλύτερο από";
                case RadGridStringId.FilterOperatorGreaterThanOrEqualTo: return "Μεγαλύτερο ίσο";
                case RadGridStringId.FilterOperatorIsEmpty: return "Κενό";
                case RadGridStringId.FilterOperatorIsNull: return "Χωρίς Τιμή";
                case RadGridStringId.FilterOperatorLessThan: return "Μικρότερο από";
                case RadGridStringId.FilterOperatorLessThanOrEqualTo: return "Μικρότερο ίσο";
                case RadGridStringId.FilterOperatorNoFilter: return "Χωρίς Φίλτρο";
                case RadGridStringId.FilterOperatorNotBetween: return "NotBetween";
                case RadGridStringId.FilterOperatorNotEqualTo: return "Δεν ισούτε";
                case RadGridStringId.FilterOperatorNotIsEmpty: return "Οχι Κενό";
                case RadGridStringId.FilterOperatorNotIsNull: return "Όχι Αδιευκρίνιστο";
                case RadGridStringId.FilterOperatorStartsWith: return "Αρχίζει με";
                case RadGridStringId.FilterOperatorIsLike: return "Μοιάζει";
                case RadGridStringId.FilterOperatorNotIsLike: return "Δεν Μοιάζει";
                case RadGridStringId.FilterOperatorIsContainedIn: return "Περιέχεται στο";
                case RadGridStringId.FilterOperatorNotIsContainedIn: return "Δεν περιέχεται στο";
                case RadGridStringId.FilterOperatorCustom: return "Προσαρμοσμένο";

                case RadGridStringId.CustomFilterMenuItem: return "Προσαρμοσμένο";
                case RadGridStringId.CustomFilterDialogCaption: return "Παράθυρο φίλτρων [{0}]";
                case RadGridStringId.CustomFilterDialogLabel: return "Εμφάνιση εγγραφών που:";
                case RadGridStringId.CustomFilterDialogRbAnd: return "ΚΑΙ";
                case RadGridStringId.CustomFilterDialogRbOr: return "Η";
                case RadGridStringId.CustomFilterDialogBtnOk: return "Εντάξει";
                case RadGridStringId.CustomFilterDialogBtnCancel: return "Άκυρο";
                case RadGridStringId.CustomFilterDialogCheckBoxNot: return "ΔΕΝ";
                case RadGridStringId.CustomFilterDialogTrue: return "Αληθές";
                case RadGridStringId.CustomFilterDialogFalse: return "Ψευδές";

                case RadGridStringId.FilterMenuAvailableFilters: return "Διαθέσιμα Φίλτρα";
                case RadGridStringId.FilterMenuSearchBoxText: return "Ευρεση...";
                case RadGridStringId.FilterMenuClearFilters: return "Καθαρισμός Φίλτρου";
                case RadGridStringId.FilterMenuButtonOK: return "Εντάξει";
                case RadGridStringId.FilterMenuButtonCancel: return "Ακυρο";
                case RadGridStringId.FilterMenuSelectionAll: return "Ολα";
                case RadGridStringId.FilterMenuSelectionAllSearched: return "Αποτέλεσμα";
                case RadGridStringId.FilterMenuSelectionNull: return "Αδιευκρίνιστο";
                case RadGridStringId.FilterMenuSelectionNotNull: return "Συγκεκριμένο";

                case RadGridStringId.FilterFunctionSelectedDates: return "Φίλτρο Ημερομηνιών:";
                case RadGridStringId.FilterFunctionToday: return "Σήμερα";
                case RadGridStringId.FilterFunctionYesterday: return "Εχθές";
                case RadGridStringId.FilterFunctionDuringLast7days: return "Προηγούμενες 7 ημέρες";

                case RadGridStringId.FilterLogicalOperatorAnd: return "ΚΑΙ";
                case RadGridStringId.FilterLogicalOperatorOr: return "Ή";
                case RadGridStringId.FilterCompositeNotOperator: return "ΔΕΝ";

                case RadGridStringId.DeleteRowMenuItem: return "Διαγραφλη Εγγραφής";
                case RadGridStringId.SortAscendingMenuItem: return "Αύξουσα Ταξινόμηση";
                case RadGridStringId.SortDescendingMenuItem: return "Φθίνουσα Ταξινόμηση";
                case RadGridStringId.ClearSortingMenuItem: return "Καθαρισμός Ταξινόμησης";
                case RadGridStringId.ConditionalFormattingMenuItem: return "Μορφοποίηση με συνθήκες";
                case RadGridStringId.GroupByThisColumnMenuItem: return "Ομαδοποίηση με αυτή τη στήλη";
                case RadGridStringId.UngroupThisColumn: return "Αρση ομαδοπίησης στήλης";
                case RadGridStringId.ColumnChooserMenuItem: return "Επιλογέας στηλών";
                case RadGridStringId.HideMenuItem: return "Απόκρυψη στήλης";
                case RadGridStringId.HideGroupMenuItem: return "Απόκρηψη Ομάδας";
                case RadGridStringId.UnpinMenuItem: return "Ξεκαρφίτσωμα Στήλης";
                case RadGridStringId.UnpinRowMenuItem: return "Ξεκαρφίτσωμα Εγγραφής";
                case RadGridStringId.PinMenuItem: return "Κατάσταση Καρφιτσώματος";
                case RadGridStringId.PinAtLeftMenuItem: return "Καρφίτσωμα στα αριστερά";
                case RadGridStringId.PinAtRightMenuItem: return "Καρφίτσωμα στα δεξιά";
                case RadGridStringId.PinAtBottomMenuItem: return "Καρφίτσωμα στο τέλος";
                case RadGridStringId.PinAtTopMenuItem: return "Καρφίτσωμα στην κορυφή";
                case RadGridStringId.BestFitMenuItem: return "Βέλτιστη Διάταξη";
                case RadGridStringId.PasteMenuItem: return "Επικόληση";
                case RadGridStringId.EditMenuItem: return "Επεξεργασία";
                case RadGridStringId.ClearValueMenuItem: return "Καθαρισμός Τιμής";
                case RadGridStringId.CopyMenuItem: return "Αντιγραφή";
                case RadGridStringId.CutMenuItem: return "Αποκοπή";
                case RadGridStringId.AddNewRowString: return "Επιλέξτε για προσθήκη εγγραφής";

                case RadGridStringId.ConditionalFormattingSortAlphabetically: return "Ταξινόμηση στηλών αλφαβητικά";
                case RadGridStringId.ConditionalFormattingCaption: return "Διαχείρηση συνθηκών μορφοποίησης";
                case RadGridStringId.ConditionalFormattingLblColumn: return "Μορφοποίση κελιών με";
                case RadGridStringId.ConditionalFormattingLblName: return "Όνομα συνθήκης";
                case RadGridStringId.ConditionalFormattingLblType: return "Τιμή Πεδίου";
                case RadGridStringId.ConditionalFormattingLblValue1: return "Τιμή 1";
                case RadGridStringId.ConditionalFormattingLblValue2: return "Τιμή 2";
                case RadGridStringId.ConditionalFormattingGrpConditions: return "Συνθήκες";
                case RadGridStringId.ConditionalFormattingGrpProperties: return "Ιδιότητες Συνθήκης";
                case RadGridStringId.ConditionalFormattingChkApplyToRow: return "Εφαρμογή μορφοποίησης σε ολή την εγγραφή";
                case RadGridStringId.ConditionalFormattingChkApplyOnSelectedRows: return "Εφαρμογή μορφοποίησης όταν η εγγραφή είναι επιλεγμένη";
                case RadGridStringId.ConditionalFormattingBtnAdd: return "Προσθήκη Συνθήκης";
                case RadGridStringId.ConditionalFormattingBtnRemove: return "Διαγραφή";
                case RadGridStringId.ConditionalFormattingBtnOK: return "Εντάξει";
                case RadGridStringId.ConditionalFormattingBtnCancel: return "Άκυρο";
                case RadGridStringId.ConditionalFormattingBtnApply: return "Εφαρμογή";
                case RadGridStringId.ConditionalFormattingRuleAppliesOn: return "Συνθήκη εφαρμόζεται σε";
                case RadGridStringId.ConditionalFormattingCondition: return "Συνθήκη";
                case RadGridStringId.ConditionalFormattingExpression: return "Έκφραση";
                case RadGridStringId.ConditionalFormattingChooseOne: return "[Επιλέξτε]";
                case RadGridStringId.ConditionalFormattingEqualsTo: return "ισούται με [Value1]";
                case RadGridStringId.ConditionalFormattingIsNotEqualTo: return "δεν ισούτε με [Value1]";
                case RadGridStringId.ConditionalFormattingStartsWith: return "ξεκινάει με [Value1]";
                case RadGridStringId.ConditionalFormattingEndsWith: return "τελειώνει με [Value1]";
                case RadGridStringId.ConditionalFormattingContains: return "περιέχει [Value1]";
                case RadGridStringId.ConditionalFormattingDoesNotContain: return "δεν περιέχει [Value1]";
                case RadGridStringId.ConditionalFormattingIsGreaterThan: return "μεγαλύτερο από [Value1]";
                case RadGridStringId.ConditionalFormattingIsGreaterThanOrEqual: return "μεγαλύτερο ίσο με [Value1]";
                case RadGridStringId.ConditionalFormattingIsLessThan: return "μικρότερο από [Value1]";
                case RadGridStringId.ConditionalFormattingIsLessThanOrEqual: return "μικρότερο ίσο με [Value1]";
                case RadGridStringId.ConditionalFormattingIsBetween: return "ανάμεσα από [Value1] και [Value2]";
                case RadGridStringId.ConditionalFormattingIsNotBetween: return "δεν είναι ανάμεσα από [Value1] και [Value2]";
                case RadGridStringId.ConditionalFormattingLblFormat: return "Μορφοποίηση";

                case RadGridStringId.ConditionalFormattingBtnExpression: return "Επεξεργασία Εκφρασής";
                case RadGridStringId.ConditionalFormattingTextBoxExpression: return "Εκφραση";

                case RadGridStringId.ConditionalFormattingPropertyGridCaseSensitive: return "ΠεζαΚεφαλαία";
                case RadGridStringId.ConditionalFormattingPropertyGridCellBackColor: return "ΧρώμαΠεδίου";
                case RadGridStringId.ConditionalFormattingPropertyGridCellForeColor: return "ΧρώμαΚειμένουΠεδίου";
                case RadGridStringId.ConditionalFormattingPropertyGridEnabled: return "Ενεργό";
                case RadGridStringId.ConditionalFormattingPropertyGridRowBackColor: return "ΧρώμαΕγγραφής";
                case RadGridStringId.ConditionalFormattingPropertyGridRowForeColor: return "ΧρώμαΚειμένουΠεδίου";
                case RadGridStringId.ConditionalFormattingPropertyGridRowTextAlignment: return "ΣτοίχησηΕγγραφής";
                case RadGridStringId.ConditionalFormattingPropertyGridTextAlignment: return "ΣτοιχησηΠεδίου";

                case RadGridStringId.ConditionalFormattingPropertyGridCaseSensitiveDescription: return "Καθορίζει αν θα γίνει συγκρίση πεζών-κεφαλαίων κατά την αξιολόγηση τιμών.";
                case RadGridStringId.ConditionalFormattingPropertyGridCellBackColorDescription: return "Εισάγετε το χρώμα του Πεδίου";
                case RadGridStringId.ConditionalFormattingPropertyGridCellForeColorDescription: return "Εισάγετε το χρώμα του κειμένου στο Πεδίου.";
                case RadGridStringId.ConditionalFormattingPropertyGridEnabledDescription: return "Κοθορίζει αν η συνθήκη είναι ενεργή";
                case RadGridStringId.ConditionalFormattingPropertyGridRowBackColorDescription: return "Εισάγετε το χρώμα ολόκληρης της εγραφής";
                case RadGridStringId.ConditionalFormattingPropertyGridRowForeColorDescription: return "Εισάγετε το χρώμα κειμένου ολόκληρης της εγραφής";
                case RadGridStringId.ConditionalFormattingPropertyGridRowTextAlignmentDescription: return "Εισάετε την στοίχηση όλων των πεδίων (εαν είναι απιλεγμένη η εφαρμογή μορφοποίησης σε ολή την εγγραφή )";
                case RadGridStringId.ConditionalFormattingPropertyGridTextAlignmentDescription: return "Εισάγετε τη στοίχηση του πεδιού";

                case RadGridStringId.ColumnChooserFormCaption: return "Επιλογή Στηλών";
                case RadGridStringId.ColumnChooserFormMessage: return "Σύρτε την επικεφαλίδα της στήλης του\nπίνακα εδώ για να αφαιρεθεί από την\nthe τρέχουσα προβολή.";
                case RadGridStringId.GroupingPanelDefaultMessage: return "Σύρτε την επικεφαλίδα της στήλης εδώ για να γίνει ομαδοποίηση με άυτή";
                case RadGridStringId.GroupingPanelHeader: return "Ομαδοποίση κατά:";

                case RadGridStringId.NoDataText: return "Δεν υπάρχουν δεδομένα";
                case RadGridStringId.CompositeFilterFormErrorCaption: return "Σφ'αλμα Φίλτρου";
                case RadGridStringId.CompositeFilterFormInvalidFilter: return "Η σύνθετη περιγραφή του φίλτρου δεν είναι έγκυρη";

                case RadGridStringId.ExpressionMenuItem: return "Έκφραση";
                case RadGridStringId.ExpressionFormTitle: return "Διαχείρηση Εκφρασης";
                case RadGridStringId.ExpressionFormFunctions: return "Συναρτήσεις";
                case RadGridStringId.ExpressionFormFunctionsText: return "Κείμενου";
                case RadGridStringId.ExpressionFormFunctionsAggregate: return "Συνόλων";
                case RadGridStringId.ExpressionFormFunctionsDateTime: return "Ημερομηνίας - Ώρας";
                case RadGridStringId.ExpressionFormFunctionsLogical: return "Λογικές";
                case RadGridStringId.ExpressionFormFunctionsMath: return "Μαθηματικές";
                case RadGridStringId.ExpressionFormFunctionsOther: return "Άλλες";
                case RadGridStringId.ExpressionFormOperators: return "Τελεστές";
                case RadGridStringId.ExpressionFormConstants: return "Σταθερές";
                case RadGridStringId.ExpressionFormFields: return "Πεδία";
                case RadGridStringId.ExpressionFormDescription: return "Περιγραφή";
                case RadGridStringId.ExpressionFormResultPreview: return "Προεπισκόπηση Αποτελέσματος";
                case RadGridStringId.ExpressionFormTooltipPlus: return "Συν";
                case RadGridStringId.ExpressionFormTooltipMinus: return "Μείων";
                case RadGridStringId.ExpressionFormTooltipMultiply: return "Επί";
                case RadGridStringId.ExpressionFormTooltipDivide: return "Διά";
                case RadGridStringId.ExpressionFormTooltipModulo: return "Υπόλοιπο";
                case RadGridStringId.ExpressionFormTooltipEqual: return "Ισον";
                case RadGridStringId.ExpressionFormTooltipNotEqual: return "Διάφορο";
                case RadGridStringId.ExpressionFormTooltipLess: return "Μικρότερο";
                case RadGridStringId.ExpressionFormTooltipLessOrEqual: return "Μικότερο Ισο";
                case RadGridStringId.ExpressionFormTooltipGreaterOrEqual: return "Μεγαλύτερο Ίσο";
                case RadGridStringId.ExpressionFormTooltipGreater: return "Μεγαλύτερο";
                case RadGridStringId.ExpressionFormTooltipAnd: return "Λογικό \"ΚΑΙ\"";
                case RadGridStringId.ExpressionFormTooltipOr: return "Λογικό \"Η\"";
                case RadGridStringId.ExpressionFormTooltipNot: return "Λογικό \"ΔΕΝ\"";
                case RadGridStringId.ExpressionFormAndButton: return string.Empty; //if empty, default button image is used
                case RadGridStringId.ExpressionFormOrButton: return string.Empty; //if empty, default button image is used
                case RadGridStringId.ExpressionFormNotButton: return string.Empty; //if empty, default button image is used
                case RadGridStringId.ExpressionFormOKButton: return "Εντάξει";
                case RadGridStringId.ExpressionFormCancelButton: return "Άκυρο";
            }

            return string.Empty;
        }
    }

    public class PrintPreviewLocalizationProvider : Telerik.WinControls.UI.PrintDialogsLocalizationProvider
    {
        public override string GetLocalizedString(string id)
        {
            switch (id)
            {
                case PrintDialogsStringId.PreviewDialogTitle: return "Προεπισκόπιση";
                case PrintDialogsStringId.PreviewDialogPrint: return "Εκτύπωση...";
                case PrintDialogsStringId.PreviewDialogPrintSettings: return "Επιλογές Εκτύπωσης...";
                case PrintDialogsStringId.PreviewDialogWatermark: return "Watermark...";
                case PrintDialogsStringId.PreviewDialogPreviousPage: return "Προηγούμενη Σελίδα";
                case PrintDialogsStringId.PreviewDialogNextPage: return "Επόμενη Σελίδα";
                case PrintDialogsStringId.PreviewDialogZoomIn: return "Μεγένθυση";
                case PrintDialogsStringId.PreviewDialogZoomOut: return "Σμίκρυνση";
                case PrintDialogsStringId.PreviewDialogZoom: return "Zoom";
                case PrintDialogsStringId.PreviewDialogAuto: return "Auto";
                case PrintDialogsStringId.PreviewDialogLayout: return "Layout";
                case PrintDialogsStringId.PreviewDialogFile: return "Αρχείο";
                case PrintDialogsStringId.PreviewDialogView: return "View";
                case PrintDialogsStringId.PreviewDialogTools: return "Εργαλεία";
                case PrintDialogsStringId.PreviewDialogExit: return "Έξοδος";
                case PrintDialogsStringId.PreviewDialogStripTools: return "Εργαλεία";
                case PrintDialogsStringId.PreviewDialogStripNavigation: return "Πλοήγηση";
                case PrintDialogsStringId.WatermarkDialogTitle: return "Watermark Settings";
                case PrintDialogsStringId.WatermarkDialogButtonOK: return "Εντάξει";
                case PrintDialogsStringId.WatermarkDialogButtonCancel: return "Ακυρο";
                case PrintDialogsStringId.WatermarkDialogLabelPreview: return "Προεπισκόπηση";
                case PrintDialogsStringId.WatermarkDialogButtonRemove: return "Remove watermark";
                case PrintDialogsStringId.WatermarkDialogLabelPosition: return "Θέση";
                case PrintDialogsStringId.WatermarkDialogRadioInFront: return "In Front";
                case PrintDialogsStringId.WatermarkDialogRadioBehind: return "Behind";
                case PrintDialogsStringId.WatermarkDialogLabelPageRange: return "Φάσμα Σελίδων";
                case PrintDialogsStringId.WatermarkDialogRadioAll: return "All";
                case PrintDialogsStringId.WatermarkDialogRadioPages: return "Σελίδες";
                case PrintDialogsStringId.WatermarkDialogLabelPagesDescription: return "(π.χ. 1,3,5-12)";
                case PrintDialogsStringId.WatermarkDialogTabText: return "Κείμενο";
                case PrintDialogsStringId.WatermarkDialogTabPicture: return "Εικόνα";
                case PrintDialogsStringId.WatermarkDialogLabelText: return "Κείμενο";
                case PrintDialogsStringId.WatermarkDialogWatermarkText: return "Watermark text";
                case PrintDialogsStringId.WatermarkDialogLabelHOffset: return "Oριζόντια Mετατόπιση";
                case PrintDialogsStringId.WatermarkDialogLabelVOffset: return "Κάθετη Mετατόπιση";
                case PrintDialogsStringId.WatermarkDialogLabelRotation: return "Περιστροφή";
                case PrintDialogsStringId.WatermarkDialogLabelFont: return "Γραμματοσειρά:";
                case PrintDialogsStringId.WatermarkDialogLabelSize: return "Μέγεθος:";
                case PrintDialogsStringId.WatermarkDialogLabelColor: return "Χρώμα:";
                case PrintDialogsStringId.WatermarkDialogLabelOpacity: return "Διαφάνεια:";
                case PrintDialogsStringId.WatermarkDialogLabelLoadImage: return "Επιλογή Εικόνας:";
                case PrintDialogsStringId.WatermarkDialogCheckboxTiling: return "Tiling";
                case PrintDialogsStringId.SettingDialogTitle: return "Επιλογές Εκτύπωσης";
                case PrintDialogsStringId.SettingDialogButtonPrint: return "Εκτύπωση";
                case PrintDialogsStringId.SettingDialogButtonPreview: return "Προεπισκόπιση";
                case PrintDialogsStringId.SettingDialogButtonCancel: return "Ακυρο";
                case PrintDialogsStringId.SettingDialogButtonOK: return "Εντάξει";
                case PrintDialogsStringId.SettingDialogPageFormat: return "Μορφοποίηση";
                case PrintDialogsStringId.SettingDialogPagePaper: return "Χαρτί";
                case PrintDialogsStringId.SettingDialogPageHeaderFooter: return "Επικεφαλίδα/Υποσέλιδο";
                case PrintDialogsStringId.SettingDialogButtonPageNumber: return "Αριθμ. Σελίδας";
                case PrintDialogsStringId.SettingDialogButtonTotalPages: return "Συνολικές Σελίδες";
                case PrintDialogsStringId.SettingDialogButtonCurrentDate: return "Τρέχουσα Ημερομηνία";
                case PrintDialogsStringId.SettingDialogButtonCurrentTime: return "Τρέχουσα Ώρα";
                case PrintDialogsStringId.SettingDialogButtonUserName: return "Όνομα χρήστη";
                case PrintDialogsStringId.SettingDialogButtonFont: return "Γραμματοσειρά...";
                case PrintDialogsStringId.SettingDialogLabelHeader: return "Επικεφαλίδα";
                case PrintDialogsStringId.SettingDialogLabelFooter: return "Υποσέλιδο";
                case PrintDialogsStringId.SettingDialogCheckboxReverse: return "Reverse on even pages";
                case PrintDialogsStringId.SettingDialogLabelPage: return "Σελίδα";
                case PrintDialogsStringId.SettingDialogLabelType: return "Τύπος";
                case PrintDialogsStringId.SettingDialogLabelPageSource: return "Πηγή Χαρτιού";
                case PrintDialogsStringId.SettingDialogLabelMargins: return "Περιθώρια (ιντσες)";
                case PrintDialogsStringId.SettingDialogLabelOrientation: return "Προσανατολισμός";
                case PrintDialogsStringId.SettingDialogLabelTop: return "Επάνω:";
                case PrintDialogsStringId.SettingDialogLabelBottom: return "Κάτω:";
                case PrintDialogsStringId.SettingDialogLabelLeft: return "Αριστερά:";
                case PrintDialogsStringId.SettingDialogLabelRight: return "Δεξιά:";
                case PrintDialogsStringId.SettingDialogRadioPortrait: return "Κατακόρυφος";
                case PrintDialogsStringId.SettingDialogRadioLandscape: return "Οριζόντιος";
                case PrintDialogsStringId.SchedulerSettingsLabelPrintStyle: return "Στυλ Εκτύπωσης";
                case PrintDialogsStringId.SchedulerSettingsDailyStyle: return "Daily Style";
                case PrintDialogsStringId.SchedulerSettingsWeeklyStyle: return "Weekly Style";
                case PrintDialogsStringId.SchedulerSettingsMonthlyStyle: return "Monthly Style";
                case PrintDialogsStringId.SchedulerSettingsDetailStyle: return "Details Style";
                case PrintDialogsStringId.SchedulerSettingsButtonWatermark: return "Watermark...";
                case PrintDialogsStringId.SchedulerSettingsButtonFont: return "Γραμματοσειρά...";
                case PrintDialogsStringId.SchedulerSettingsLabelPrintRange: return "Print range";
                case PrintDialogsStringId.SchedulerSettingsLabelStyleSettings: return "Επιλογές Εμφάνισης";
                case PrintDialogsStringId.SchedulerSettingsLabelPrintSettings: return "Επιλογές Εκτύπωσης";
                case PrintDialogsStringId.SchedulerSettingsLabelStartDate: return "Ημερ. Έναρξη";
                case PrintDialogsStringId.SchedulerSettingsLabelEndDate: return "Ημερ. Λήξης";
                case PrintDialogsStringId.SchedulerSettingsLabelStartTime: return "Ώρα Έναρξης";
                case PrintDialogsStringId.SchedulerSettingsLabelEndTime: return "Ώρα Λήξης";
                case PrintDialogsStringId.SchedulerSettingsLabelDateFont: return "Date heading font";
                case PrintDialogsStringId.SchedulerSettingsLabelAppointmentFont: return "Appointment font";
                case PrintDialogsStringId.SchedulerSettingsLabelLayout: return "Layout";
                case PrintDialogsStringId.SchedulerSettingsPrintPageTitle: return "Print page title";
                case PrintDialogsStringId.SchedulerSettingsPrintCalendar: return "Include calendar in the title";
                case PrintDialogsStringId.SchedulerSettingsPrintTimezone: return "Print the selected timezone";
                case PrintDialogsStringId.SchedulerSettingsPrintNotesBlank: return "Notes area (blank)";
                case PrintDialogsStringId.SchedulerSettingsPrintNotesLined: return "Notes area (lined)";
                case PrintDialogsStringId.SchedulerSettingsNonworkingDays: return "Exclude non-working days";
                case PrintDialogsStringId.SchedulerSettingsExactlyOneMonth: return "Print exactly one month";
                case PrintDialogsStringId.SchedulerSettingsLabelWeeksPerPage: return "Weeks per page";
                case PrintDialogsStringId.SchedulerSettingsNewPageEach: return "Start new page each";
                case PrintDialogsStringId.SchedulerSettingsStringDay: return "Ημέρα";
                case PrintDialogsStringId.SchedulerSettingsStringMonth: return "Μήνας";
                case PrintDialogsStringId.SchedulerSettingsStringWeek: return "Εβδομάδα";
                case PrintDialogsStringId.SchedulerSettingsStringPage: return "Σελίδα";
                case PrintDialogsStringId.SchedulerSettingsStringPages: return "Σελίδες";
                case PrintDialogsStringId.SchedulerSettingsLabelGroupBy: return "Ομαδοποίηση κατά:";
                case PrintDialogsStringId.SchedulerSettingsGroupByNone: return "None";
                case PrintDialogsStringId.SchedulerSettingsGroupByResource: return "Resource";
                case PrintDialogsStringId.SchedulerSettingsGroupByDate: return "Ημερομηνία";
                case PrintDialogsStringId.GridSettingsLabelPreview: return "Προεπισκόπιση";
                case PrintDialogsStringId.GridSettingsLabelStyleSettings: return "Style Settings";
                case PrintDialogsStringId.GridSettingsLabelFitMode: return "Page fit mode:";
                case PrintDialogsStringId.GridSettingsLabelHeaderCells: return "Header cells";
                case PrintDialogsStringId.GridSettingsLabelGroupCells: return "Group cells";
                case PrintDialogsStringId.GridSettingsLabelDataCells: return "Data cells";
                case PrintDialogsStringId.GridSettingsLabelSummaryCells: return "Summary cells";
                case PrintDialogsStringId.GridSettingsLabelBackground: return "Background";
                case PrintDialogsStringId.GridSettingsLabelBorderColor: return "Border color";
                case PrintDialogsStringId.GridSettingsLabelAlternatingRowColor: return "Alternating row color";
                case PrintDialogsStringId.GridSettingsLabelPadding: return "Padding";
                case PrintDialogsStringId.GridSettingsPrintGrouping: return "Print grouping";
                case PrintDialogsStringId.GridSettingsPrintSummaries: return "Print summaries";
                case PrintDialogsStringId.GridSettingsPrintHiddenRows: return "Print hidden rows";
                case PrintDialogsStringId.GridSettingsPrintHiddenColumns: return "Print hidden columns";
                case PrintDialogsStringId.GridSettingsPrintHeader: return "Print header on each page";
                case PrintDialogsStringId.GridSettingsButtonWatermark: return "Watermark...";
                case PrintDialogsStringId.GridSettingsButtonFont: return "Font...";
                case PrintDialogsStringId.GridSettingsFitPageWidth: return "FitPageWidth";
                case PrintDialogsStringId.GridSettingsNoFit: return "NoFit";
                case PrintDialogsStringId.GridSettingsNoFitCentered: return "NoFitCentered";
                case PrintDialogsStringId.GridSettingsLabelPrint: return "Print";
            }
            return String.Empty;
        }
    }
}

    //public class GreekRadDockLocalizationProvider : RadDockLocalizationProvider
    //{
    //    public override string GetLocalizedString(string id)
    //    {
    //        switch (id)
    //        {
    //            case RadDockStringId.ContextMenuFloating:
    //                return "Ελεύθερο";
    //            case RadDockStringId.ContextMenuDockable:
    //                return "Συνδέσιμο";
    //            case RadDockStringId.ContextMenuTabbedDocument:
    //                return "Εγγραφο σε Καρτέλα";
    //            case RadDockStringId.ContextMenuAutoHide:
    //                return "Αυτόματη απόκρυψη";
    //            case RadDockStringId.ContextMenuHide:
    //                return "Απόκρυψη";
    //            case RadDockStringId.ContextMenuClose:
    //                return "Κλείσιμο";
    //            case RadDockStringId.ContextMenuCloseAll:
    //                return "Κλείσιμο όλων";
    //            case RadDockStringId.ContextMenuCloseAllButThis:
    //                return "Κλείσιμο όλων εκτός Επιλεγμένου";
    //            case RadDockStringId.ContextMenuMoveToPreviousTabGroup:
    //                return "Μετακίνηση στην Προηγούμενη Ομάδα Καρτελών";
    //            case RadDockStringId.ContextMenuMoveToNextTabGroup:
    //                return "Μετακίνηση στην Επόμενη Ομάδα Καρτελών";
    //            case RadDockStringId.ContextMenuNewHorizontalTabGroup:
    //                return "Νέα Οριζόντια Ομάδα Καρτελών";
    //            case RadDockStringId.ContextMenuNewVerticalTabGroup:
    //                return "Νέα Κάθετη Ομάδα Καρτελών";
    //            case RadDockStringId.ToolTabStripCloseButton:
    //                return "Κλείσιμο Παραθύρου";
    //            case RadDockStringId.ToolTabStripDockStateButton:
    //                return "Κατάσταση Παραθύρου";
    //            case RadDockStringId.ToolTabStripUnpinButton:
    //                return "Αυτόματη Απόκρυψη";
    //            case RadDockStringId.ToolTabStripPinButton:
    //                return "Συνδεδεμένη";
    //            case RadDockStringId.DocumentTabStripCloseButton:
    //                return "Κλέισιμο Καρτέλας";
    //            case RadDockStringId.DocumentTabStripListButton:
    //                return "Ενεργές Καρτέλες";

    //        }

    //        return string.Empty;
    //    }
    //}
    
//}