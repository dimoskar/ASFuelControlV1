using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Models
{
    public class VATCategory
    {
        public int Code { set; get; }
        public string Description { set; get; }
        public decimal VATPercentage { set; get; }

        public static VATCategory[] Categories
        {
            get
            {
                return new VATCategory[]
                {
                    new VATCategory() { Code = 1, Description = "ΦΠΑ συντελεστής 24%", VATPercentage = 24 },
                    new VATCategory() { Code = 2, Description = "ΦΠΑ συντελεστής 13%", VATPercentage = 13 },
                    new VATCategory() { Code = 3, Description = "ΦΠΑ συντελεστής 6%", VATPercentage = 6 },
                    new VATCategory() { Code = 4, Description = "ΦΠΑ συντελεστής 17%", VATPercentage = 17 },
                    new VATCategory() { Code = 5, Description = "ΦΠΑ συντελεστής 9%", VATPercentage = 9 },
                    new VATCategory() { Code = 6, Description = "ΦΠΑ συντελεστής 4%", VATPercentage = 4 },
                    new VATCategory() { Code = 7, Description = "Άνευ Φ.Π.Α.0%", VATPercentage = 0 },
                    new VATCategory() { Code = 8, Description = "Εγγραφές χωρίς ΦΠΑ (πχ Μισθοδοσία, Αποσβέσεις)", VATPercentage = 24 }
                };
            }
        }        
    }

    public class WithheldTaxesCategory
    {
        public int Code { set; get; }
        public string Description { set; get; }
        public decimal Value { set; get; }
        public NumberValueTypeEnum ValueType { set; get; }

        public static WithheldTaxesCategory[] TaxesList
        {
            get
            {
                return new WithheldTaxesCategory[]
                {
                        new WithheldTaxesCategory() { Code=1, Description="Περιπτ. β’-Τόκοι -15%", Value=15},
                        new WithheldTaxesCategory() { Code=2, Description="Περιπτ. γ’ -Δικαιώματα -20%", Value=20},
                        new WithheldTaxesCategory() { Code=3, Description="Περιπτ. δ’ -Αμοιβές Συμβουλών Διοίκησης -20%", Value=20},
                        new WithheldTaxesCategory() { Code=4, Description="Περιπτ. δ’ -Τεχνικά Έργα -3%", Value=3},
                        new WithheldTaxesCategory() { Code=5, Description="Υγρά καύσιμα και προϊόντα καπνοβιομηχανίας 1%", Value=1},
                        new WithheldTaxesCategory() { Code=6, Description="Λοιπά Αγαθά 4%", Value=4},
                        new WithheldTaxesCategory() { Code=7, Description="Παροχή Υπηρεσιών 8%", Value=8},
                        new WithheldTaxesCategory() { Code=8, Description="Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, για Εκπόνηση Μελετών και Σχεδίων 4%", Value=4},
                        new WithheldTaxesCategory() { Code=9, Description="Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, που αφορούν οποιασδήποτε άλλης φύσης έργα 10%", Value=10},
                        new WithheldTaxesCategory() { Code=10, Description="Προκαταβλητέος Φόρος στις Αμοιβές Δικηγόρων 15%", Value=15},
                        new WithheldTaxesCategory() { Code=11, Description="Παρακράτηση Φόρου Μισθωτών Υπηρεσιών  παρ. 1 αρ. 15 ν. 4172/2013", Value=0, ValueType = NumberValueTypeEnum.Amount},
                        new WithheldTaxesCategory() { Code=12, Description="Παρακράτηση Φόρου Μισθωτών Υπηρεσιών  παρ. 2 αρ. 15 ν. 4172/2013 -Αξιωματικών Εμπορικού Ναυτικού", Value=15},
                        new WithheldTaxesCategory() { Code=13, Description="Παρακράτηση ΦόρουΜισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 -Κατώτερο Πλήρωμα Εμπορικού Ναυτικού", Value=10},
                        new WithheldTaxesCategory() { Code=14, Description="Παρακράτηση Ειδικής Εισφοράς Αλληλεγγύης", Value=0, ValueType = NumberValueTypeEnum.Amount},
                        new WithheldTaxesCategory() { Code=15, Description="Παρακράτηση Φόρου Αποζημίωσης λόγω Διακοπής Σχέσης Εργασίας  παρ. 3 αρ. 15 ν. 4172/2013", Value=0, ValueType = NumberValueTypeEnum.Amount}
                };
            }
        }

        //1 Περιπτ. β’-Τόκοι -15    %15%
        //2 Περιπτ. γ’ -Δικαιώματα -20% 20%
        //3 Περιπτ. δ’ -Αμοιβές Συμβουλών Διοίκησης -20%    20%
        //4 Περιπτ. δ’ -Τεχνικά Έργα -3%    3%
        //5 Υγρά καύσιμα και προϊόντα καπνοβιομηχανίας 1%   1%
        //6 Λοιπά Αγαθά 4%  4%
        //7 Παροχή Υπηρεσιών 8% 8%
        //8 Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, για Εκπόνηση Μελετών και Σχεδίων 4% 4%
        //9 Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, που αφορούν οποιασδήποτε άλλης φύσης έργα 10%   10%
        //10    Προκαταβλητέος Φόρος στις Αμοιβές Δικηγόρων 15% 15%
        //11    Παρακράτηση Φόρου Μισθωτών Υπηρεσιών  παρ. 1 αρ. 15 ν. 4172/2013    0
        //12    Παρακράτηση Φόρου Μισθωτών Υπηρεσιών  παρ. 2 αρ. 15 ν. 4172/2013 -Αξιωματικών Εμπορικού Ναυτικού    15%
        //13    Παρακράτηση ΦόρουΜισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 -Κατώτερο Πλήρωμα Εμπορικού Ναυτικού 10%
        //14    Παρακράτηση Ειδικής Εισφοράς Αλληλεγγύης    0
        //15    Παρακράτηση Φόρου Αποζημίωσης λόγω Διακοπής Σχέσης Εργασίας  παρ. 3 αρ. 15 ν. 4172/2013 0
    }

    public class OtherTaxesCategory
    {
        public int Code { set; get; }
        public string Description { set; get; }
        public decimal Value { set; get; }
        public NumberValueTypeEnum ValueType { set; get; }

        public static OtherTaxesCategory[] TaxesList
        {
            get
            {
                return new OtherTaxesCategory[]
                {
                        new OtherTaxesCategory() { Code=1, Description="α1) ασφάλιστρα κλάδου πυρός 20%", Value=15, ValueType = NumberValueTypeEnum.Percentage},
                        new OtherTaxesCategory() { Code=2, Description="α2) ασφάλιστρα κλάδου πυρός 20%", Value=5, ValueType = NumberValueTypeEnum.Percentage},
                        new OtherTaxesCategory() { Code=3, Description="β) ασφάλιστρα κλάδου ζωής 4%", Value=4, ValueType = NumberValueTypeEnum.Percentage},
                        new OtherTaxesCategory() { Code=4, Description="γ) ασφάλιστρα λοιπών κλάδων 15%.", Value=15, ValueType = NumberValueTypeEnum.Percentage},
                        new OtherTaxesCategory() { Code=5, Description="δ) απαλλασσόμενα φόρου ασφαλίστρων 0%.", Value=0, ValueType = NumberValueTypeEnum.Percentage},
                        new OtherTaxesCategory() { Code=6, Description="Ξενοδοχεία 1-2 αστέρων", Value=(decimal)0.5, ValueType = NumberValueTypeEnum.Amount},
                        new OtherTaxesCategory() { Code=7, Description="Ξενοδοχεία 3 αστέρων", Value=(decimal)1.5d, ValueType = NumberValueTypeEnum.Amount},
                        new OtherTaxesCategory() { Code=8, Description="Ξενοδοχεία 4 αστέρων", Value=(decimal)3, ValueType = NumberValueTypeEnum.Amount},
                        new OtherTaxesCategory() { Code=9, Description="Ξενοδοχεία 4 αστέρων", Value=(decimal)4, ValueType = NumberValueTypeEnum.Amount},
                        new OtherTaxesCategory() { Code=10, Description="Ενοικιαζόμενα -επιπλωμένα δωμάτια -διαμερίσματα", Value=1, ValueType = NumberValueTypeEnum.Percentage},
                        new OtherTaxesCategory() { Code=11, Description="Ειδικός Φόρος στις  διαφημίσεις που προβάλλονται από την τηλεόραση (ΕΦΤΔ) 5%", Value=5, ValueType = NumberValueTypeEnum.Percentage},
                        new OtherTaxesCategory() { Code=12, Description="3.1 Φόρος πολυτελείας 10% επί της φορολογητέας αξίας για τα ενδοκοινοτικώς αποκτούμενα και εισαγόμενα από τρίτες χώρες  10%", Value=10, ValueType = NumberValueTypeEnum.Percentage},
                        new OtherTaxesCategory() { Code=13, Description="3.2 Φόρος πολυτελείας 10% επί της τιμής πώλησης προ Φ.Π.Α. για τα εγχωρίωςπαραγόμενα είδη 10%", Value=10, ValueType = NumberValueTypeEnum.Percentage},
                        new OtherTaxesCategory() { Code=14, Description="Δικαίωμα του Δημοσίου στα εισιτήρια των καζίνο (80% επί του εισιτηρίου)", Value=80, ValueType = NumberValueTypeEnum.Percentage}
                };
            }
        }

        //1 Περιπτ. β’-Τόκοι -15    %15%
        //2 Περιπτ. γ’ -Δικαιώματα -20% 20%
        //3 Περιπτ. δ’ -Αμοιβές Συμβουλών Διοίκησης -20%    20%
        //4 Περιπτ. δ’ -Τεχνικά Έργα -3%    3%
        //5 Υγρά καύσιμα και προϊόντα καπνοβιομηχανίας 1%   1%
        //6 Λοιπά Αγαθά 4%  4%
        //7 Παροχή Υπηρεσιών 8% 8%
        //8 Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, για Εκπόνηση Μελετών και Σχεδίων 4% 4%
        //9 Προκαταβλητέος Φόρος Αρχιτεκτόνων και Μηχανικών επί Συμβατικών Αμοιβών, που αφορούν οποιασδήποτε άλλης φύσης έργα 10%   10%
        //10    Προκαταβλητέος Φόρος στις Αμοιβές Δικηγόρων 15% 15%
        //11    Παρακράτηση Φόρου Μισθωτών Υπηρεσιών  παρ. 1 αρ. 15 ν. 4172/2013    0
        //12    Παρακράτηση Φόρου Μισθωτών Υπηρεσιών  παρ. 2 αρ. 15 ν. 4172/2013 -Αξιωματικών Εμπορικού Ναυτικού    15%
        //13    Παρακράτηση ΦόρουΜισθωτών Υπηρεσιών παρ. 2 αρ. 15 ν. 4172/2013 -Κατώτερο Πλήρωμα Εμπορικού Ναυτικού 10%
        //14    Παρακράτηση Ειδικής Εισφοράς Αλληλεγγύης    0
        //15    Παρακράτηση Φόρου Αποζημίωσης λόγω Διακοπής Σχέσης Εργασίας  παρ. 3 αρ. 15 ν. 4172/2013 0
    }

    public class StampDutyCategory
    {
        public int Code { set; get; }
        public string Description { set; get; }
        public decimal Value { set; get; }
        public NumberValueTypeEnum ValueType { set; get; }

        public static StampDutyCategory[] TaxesList
        {
            get
            {
                return new StampDutyCategory[]
                {
                        new StampDutyCategory() { Code = 1, Description = "Συντελεστής  1,2 %", Value = (decimal)1.2 },
                        new StampDutyCategory() { Code = 2, Description = "Συντελεστής  2,4 %", Value = (decimal)2.4 },
                        new StampDutyCategory() { Code = 3, Description = "Συντελεστής  3,6 %", Value = (decimal)3.6 }
                };
            }
        }
    }

    public class Fees
    {
        public int Code { set; get; }
        public string Description { set; get; }
        public decimal Value { set; get; }
        public NumberValueTypeEnum ValueType { set; get; }

        public static Fees[] TaxesList
        {
            get
            {
                return new Fees[]
                {
                        new Fees() { Code=1, Description="Για μηνιαίο λογαριασμό μέχρι και 50 ευρώ 12%", Value=12, ValueType=NumberValueTypeEnum.Percentage},
                        new Fees() { Code=2, Description="Για μηνιαίο λογαριασμό από 50,01 μέχρι και 100 ευρώ 15%", Value=15, ValueType=NumberValueTypeEnum.Percentage},
                        new Fees() { Code=3, Description="Για μηνιαίο λογαριασμό από 100,01 μέχρι και 150 ευρώ 18%", Value=18, ValueType=NumberValueTypeEnum.Percentage},
                        new Fees() { Code=4, Description="Για μηνιαίο λογαριασμό από 150,01 ευρώ και άνω 20%", Value=20, ValueType=NumberValueTypeEnum.Percentage},
                        new Fees() { Code=5, Description="Τέλος καρτοκινητής επί της αξίας του χρόνου ομιλίας (12%)", Value=12, ValueType=NumberValueTypeEnum.Percentage},
                        new Fees() { Code=6, Description="Τέλος στη συνδρομητική τηλεόραση 10%", Value=10, ValueType=NumberValueTypeEnum.Percentage},
                        new Fees() { Code=7, Description="Τέλος συνδρομητών σταθερής τηλεφωνίας 5%", Value=5, ValueType=NumberValueTypeEnum.Percentage},
                        new Fees() { Code=8, Description="Περιβαλλοντικό Τέλος & πλαστικής σακούλας ν. 2339/2001 αρ. 6α 0,07 ευρώ ανά τεμάχιο", Value=0, ValueType=NumberValueTypeEnum.Amount},
                        new Fees() { Code=9, Description="Εισφορά δακοκτονίας 2%", Value=2, ValueType=NumberValueTypeEnum.Percentage}

                };
            }
        }
    }

    public class IncomeCategory
    {
        public string Code { set; get; }
        public string Description { set; get; }

        public static IncomeCategory[] IncomeCategories
        {
            get
            {
                return new IncomeCategory[]
                {
                        new IncomeCategory() { Code="category1_1", Description = "Έσοδα από Πώληση Εμπορευμάτων (+) / (-)" },
                        new IncomeCategory() { Code="category1_2", Description = "Έσοδα από Πώληση Προϊόντων (+)/ (-)" },
                        new IncomeCategory() { Code="category1_3", Description = "Έσοδα από Παροχή Υπηρεσιών (+) / (-)" },
                        new IncomeCategory() { Code="category1_4", Description = "Έσοδα από Πώληση Παγίων (+) /(-)" },
                        new IncomeCategory() { Code="category1_5", Description = "Λοιπά Έσοδα/ Κέρδη (+) / (-)" },
                        new IncomeCategory() { Code="category1_6", Description = "Αυτοπαραδόσεις /  Ιδιοχρησιμοποιήσεις (+)/ (-)" },
                        new IncomeCategory() { Code="category1_7", Description = "Έσοδα για λ/σμο τρίτων (+)/ (-)" },
                        new IncomeCategory() { Code="category1_8", Description = "Έσοδα προηγούμενων χρήσεων (+)/(-)" },
                        new IncomeCategory() { Code="category1_9", Description = "Έσοδα επομένων χρήσεων (+)/(-)" },
                        new IncomeCategory() { Code="category1_10", Description = "Λοιπές Εγγραφές Τακτοποίησης Εσόδων (+)/ (-)" },
                        new IncomeCategory() { Code="category1_95", Description = "Λοιπά Πληροφοριακά Στοιχεία Εσόδων (+)/ (-)" }
                };
            }
        }
    }

    public class ExpenseCategory
    {
        public string Code { set; get; }
        public string Description { set; get; }

        public static ExpenseCategory[] ExpenseCategories
        {
            get
            {
                return new ExpenseCategory[]
                {
                        new ExpenseCategory() { Code="category2_1", Description = "Αγορές Εμπορευμάτων(-) /(+)" },
                        new ExpenseCategory() { Code="category2_2", Description = "Αγορές Α'-Β' Υλών  (-) / (+)" },
                        new ExpenseCategory() { Code="category2_3", Description = "Λήψη Υπηρεσιών  (-)/ (+)" },
                        new ExpenseCategory() { Code="category2_4", Description = "Γενικά Έξοδα με δικαίωμα έκπτωσης ΦΠΑ  (-) / (+)" },
                        new ExpenseCategory() { Code="category2_5", Description = "Γενικά Έξοδα χωρίς δικαίωμα έκπτωσης ΦΠΑ(-) /(+)" },
                        new ExpenseCategory() { Code="category2_6", Description = "Αμοιβές και Παροχές προσωπικού(-)/(+)" },
                        new ExpenseCategory() { Code="category2_7", Description = "Αγορές Παγίων  (-)/ (+)" },
                        new ExpenseCategory() { Code="category2_8", Description = "Αποσβέσεις Παγίων  (-) / (+)" },
                        new ExpenseCategory() { Code="category2_9", Description = "Έξοδα για λ/σμο τρίτων (-) / (+)" },
                        new ExpenseCategory() { Code="category2_10", Description = "Έξοδα προηγούμενων χρήσεων  (-)/ (+)" },
                        new ExpenseCategory() { Code="category2_11", Description = "Έξοδα επομένων χρήσεων (-) /(+)" },
                        new ExpenseCategory() { Code="category2_12", Description = "Λοιπές Εγγραφές Τακτοποίησης Εξόδων (-)/ (+)" },
                        new ExpenseCategory() { Code="category2_13", Description = "Αποθέματα Έναρξης Περιόδου (-) / (+)" },
                        new ExpenseCategory() { Code="category2_14", Description = "Αποθέματα Λήξης Περιόδου (-) / (+)" },
                        new ExpenseCategory() { Code="category2_95", Description = "Λοιπά Πληροφοριακά Στοιχεία Εξόδων (-) / (+)" }

                };
            }
        }
    }

    public class IncomeType
    {
        public string Code { set; get; }
        public string Description { set; get; }

        public IncomeType[] IncomeTypes
        {
            get
            {
                return new IncomeType[]
                {
                        new IncomeType() { Code="E3_106", Description = "Ιδιοπαραγωγή παγίων -Αυτοπαραδόσεις -Καταστροφές αποθεμάτων/Εμπορεύματα" },
                        new IncomeType() { Code="E3_205", Description = "Ιδιοπαραγωγή παγίων -Αυτοπαραδόσεις -Καταστροφές αποθεμάτων/Πρώτες ύλες και λοιπά υλικά" },
                        new IncomeType() { Code="E3_210", Description = "Ιδιοπαραγωγή παγίων -Αυτοπαραδόσεις-Καταστροφές αποθεμάτων/Προϊόντα και παραγωγή σε εξέλιξη" },
                        new IncomeType() { Code="E3_305", Description = "Ιδιοπαραγωγή παγίων -Αυτοπαραδόσεις –Καταστροφές αποθεμάτων/Πρώτες ύλες και λοιπά υλικά" },
                        new IncomeType() { Code="E3_310", Description = "Ιδιοπαραγωγή παγίων -Αυτοπαραδόσεις -Καταστροφές αποθεμάτων/Προϊόντα και παραγωγή σε εξέλιξη" },
                        new IncomeType() { Code="E3_318", Description = "Ιδιοπαραγωγή παγίων -Αυτοπαραδόσεις -Καταστροφές αποθεμάτων/Έξοδα παραγωγής" },
                        new IncomeType() { Code="E3_561_001", Description = "Πωλήσεις αγαθών και υπηρεσιών Χονδρικές -Επιτηδευματιών" },
                        new IncomeType() { Code="E3_561_002", Description = "Πωλήσεις αγαθών και υπηρεσιών Χονδρικές βάσει άρθρου 39α παρ 5 του Κώδικα Φ.Π.Α. (Ν.2859/2000)" },
                        new IncomeType() { Code="E3_561_003", Description = "Πωλήσεις αγαθών και υπηρεσιών Λιανικές -Ιδιωτική Πελατεία" },
                        new IncomeType() { Code="E3_561_004", Description = "Πωλήσεις αγαθών και υπηρεσιών Λιανικές βάσει άρθρου 39α παρ 5 του Κώδικα Φ.Π.Α. (Ν.2859/2000)" },
                        new IncomeType() { Code="E3_561_005", Description = "Πωλήσεις αγαθών και υπηρεσιών Εξωτερικού Ενδοκοινοτικές" },
                        new IncomeType() { Code="E3_561_006", Description = "Πωλήσεις αγαθών και υπηρεσιών Εξωτερικού Τρίτες Χώρες" },
                        new IncomeType() { Code="E3_561_007", Description = "Πωλήσεις αγαθών και υπηρεσιών Λοιπά" },
                        new IncomeType() { Code="E3_562", Description = "Λοιπά συνήθη έσοδα" },
                        new IncomeType() { Code="E3_563", Description = "Πιστωτικοί τόκοι και συναφή έσοδα" },
                        new IncomeType() { Code="E3_564", Description = "Πιστωτικές συναλλαγματικές διαφορές" },
                        new IncomeType() { Code="E3_565", Description = "Έσοδα συμμετοχών" },
                        new IncomeType() { Code="E3_566", Description = "Κέρδη από διάθεση μη κυκλοφορούντων περιουσιακών στοιχείων" },
                        new IncomeType() { Code="E3_567", Description = "Κέρδη από αναστροφή προβλέψεων και απομειώσεων" },
                        new IncomeType() { Code="E3_568", Description = "Κέρδη από επιμέτρηση στην εύλογη αξία" },
                        new IncomeType() { Code="E3_570", Description = "Ασυνήθη έσοδα και κέρδη" },
                        new IncomeType() { Code="E3_595", Description = "Έξοδα σε ιδιοπαραγωγή" },
                        new IncomeType() { Code="E3_596", Description = "Επιδοτήσεις -Επιχορηγήσεις" },
                        new IncomeType() { Code="E3_597", Description = "Επιδοτήσεις -Επιχορηγήσεις για επενδυτικούς σκοπούς -κάλυψη δαπανών" },
                        new IncomeType() { Code="E3_880_001", Description = "Πωλήσεις Παγίων Χονδρικές" },
                        new IncomeType() { Code="E3_880_002", Description = "Πωλήσεις Παγίων Λιανικές" },
                        new IncomeType() { Code="E3_880_003", Description = "Πωλήσεις Παγίων Εξωτερικού Ενδοκοινοτικές" },
                        new IncomeType() { Code="E3_880_004", Description = "Πωλήσεις Παγίων Εξωτερικού Τρίτες Χώρες" },
                        new IncomeType() { Code="E3_881_001", Description = "Πωλήσεις για λογ/σμο Τρίτων Χονδρικές" },
                        new IncomeType() { Code="E3_881_002", Description = "Πωλήσεις για λογ/σμο Τρίτων Λιανικές" },
                        new IncomeType() { Code="E3_881_003", Description = "Πωλήσεις για λογ/σμο Τρίτων Εξωτερικού Ενδοκοινοτικές" },
                        new IncomeType() { Code="E3_881_004", Description = "Πωλήσεις για λογ/σμο Τρίτων Εξωτερικού Τρίτες Χώρες" }
                };
            }
        }
    }

    public class ExpenseType
    {
        public string Code { set; get; }
        public string Description { set; get; }

        public ExpenseType[] ExpenseTypes
        {
            get
            {
                return new ExpenseType[]
                {
                        new ExpenseType() { Code="E3_101", Description = "Εμπορεύματα έναρξης" },
                        new ExpenseType() { Code="E3_102_001", Description = "Αγορές εμπορευμάτων χρήσης (καθαρό ποσό)/Χονδρικές" },
                        new ExpenseType() { Code="E3_102_002", Description = "Αγορές εμπορευμάτων χρήσης (καθαρό ποσό)/Λιανικές" },
                        new ExpenseType() { Code="E3_102_003", Description = "Αγορές εμπορευμάτων χρήσης (καθαρό ποσό)/Αγαθών του άρθρου 39α παρ.5 του Κώδικα Φ.Π.Α. (ν.2859/2000)" },
                        new ExpenseType() { Code="E3_102_004", Description = "Αγορές εμπορευμάτων χρήσης (καθαρό ποσό)/Εξωτερικού Ενδοκοινοτικές" },
                        new ExpenseType() { Code="E3_102_005", Description = "Αγορές εμπορευμάτων χρήσης (καθαρό ποσό)/Εξωτερικού Τρίτες Χώρες" },
                        new ExpenseType() { Code="E3_102_006", Description = "Αγορές εμπορευμάτων χρήσης (καθαρό ποσό)Λοιπά" },
                        new ExpenseType() { Code="E3_104", Description = "Εμπορεύματα λήξης" },
                        new ExpenseType() { Code="E3_201", Description = "Πρώτες ύλες και υλικά έναρξης/Παραγωγή" },
                        new ExpenseType() { Code="E3_202_001", Description = "Αγορές πρώτων υλών και υλικών χρήσης  (καθαρό ποσό)/Χονδρικές" },
                        new ExpenseType() { Code="E3_202_002", Description = "Αγορές πρώτων υλών και υλικών χρήσης  (καθαρό ποσό)/Λιανικές" },
                        new ExpenseType() { Code="E3_202_003", Description = "Αγορές πρώτων υλών και υλικών χρήσης (καθαρό ποσό)/Εξωτερικού Ενδοκοινοτικές" },
                        new ExpenseType() { Code="E3_202_004", Description = "Αγορές πρώτων υλών και υλικών χρήσης  (καθαρό ποσό)/Εξωτερικού Τρίτες Χώρες" },
                        new ExpenseType() { Code="E3_202_005", Description = "Αγορές πρώτωνυλών και υλικών χρήσης (καθαρό ποσό)/Λοιπά" },
                        new ExpenseType() { Code="E3_204", Description = "Αποθέματα λήξης πρώτων υλών και υλικών/Παραγωγή" },
                        new ExpenseType() { Code="E3_207", Description = "Προϊόντα και παραγωγή σε εξέλιξη έναρξης/Παραγωγή" },
                        new ExpenseType() { Code="E3_209", Description = "Προϊόντα και παραγωγή σε εξέλιξη λήξης/Παραγωγή" },
                        new ExpenseType() { Code="E3_301", Description = "Πρώτες ύλες και υλικά έναρξης/Αγροτική" },
                        new ExpenseType() { Code="E3_302_001", Description = "Αγορές πρώτων υλών και υλικών χρήσης  (καθαρό ποσό)/Χονδρικές" },
                        new ExpenseType() { Code="E3_302_002", Description = "Αγορές πρώτων υλών και υλικών χρήσης  (καθαρό ποσό)/Λιανικές" },
                        new ExpenseType() { Code="E3_302_003", Description = "Αγορές πρώτων υλών και υλικών χρήσης (καθαρό ποσό)/Εξωτερικού Ενδοκοινοτικές" },
                        new ExpenseType() { Code="E3_302_004", Description = "Αγορές πρώτων υλών και υλικών χρήσης  (καθαρό ποσό)/Εξωτερικού Τρίτες Χώρες" },
                        new ExpenseType() { Code="E3_302_005", Description = "Αγορές πρώτων υλών και υλικών χρήσης (καθαρό ποσό)/Λοιπά" },
                        new ExpenseType() { Code="E3_304", Description = "Αποθέματα λήξης πρώτων υλών και υλικών/Αγροτική" },
                        new ExpenseType() { Code="E3_307", Description = "Προϊόντα και παραγωγή σε εξέλιξη έναρξης/Αγροτική" },
                        new ExpenseType() { Code="E3_309", Description = "Προϊόντα και παραγωγή σε εξέλιξη λήξης/Αγροτική" },
                        new ExpenseType() { Code="E3_312", Description = "Αποθέματα έναρξης (ζώων -φυτών)" },
                        new ExpenseType() { Code="E3_313_001", Description = "Αγορές ζώων -φυτών (καθαρό ποσό)/Χονδρικές" },
                        new ExpenseType() { Code="E3_313_002", Description = "Αγορές ζώων -φυτών (καθαρό ποσό)/Λιανικές" },
                        new ExpenseType() { Code="E3_313_003", Description = "Αγορές ζώων -φυτών(καθαρό ποσό)/Εξωτερικού Ενδοκοινοτικές" },
                        new ExpenseType() { Code="E3_313_004", Description = "Αγορές ζώων -φυτών (καθαρό ποσό)/Εξωτερικού Τρίτες Χώρες" },
                        new ExpenseType() { Code="E3_313_005", Description = "Αγορές ζώων -φυτών (καθαρό ποσό)/Λοιπά" },
                        new ExpenseType() { Code="E3_315", Description = "Αποθέματα τέλους (ζώων -φυτών)/Αγροτική" },
                        new ExpenseType() { Code="E3_581_001", Description = "Παροχές σε εργαζόμενους/Μικτές αποδοχές" },
                        new ExpenseType() { Code="E3_581_002", Description = "Παροχές σε εργαζόμενους/Εργοδοτικές εισφορές" },
                        new ExpenseType() { Code="E3_581_003", Description = "Παροχές σε εργαζόμενους/Λοιπές παροχές" },
                        new ExpenseType() { Code="E3_582", Description = "Ζημιές επιμέτρησης περιουσιακών στοιχείων" },
                        new ExpenseType() { Code="E3_583", Description = "Χρεωστικές συναλλαγματικές διαφορές" },
                        new ExpenseType() { Code="E3_584", Description = "Ζημιές από διάθεση-απόσυρση μη κυκλοφορούντων περιουσιακών στοιχείων" },
                        new ExpenseType() { Code="E3_585_001", Description = "Προμήθειες διαχείρισης ημεδαπής -αλλοδαπής (management fees)" },
                        new ExpenseType() { Code="E3_585_002", Description = "Δαπάνες από συνδεδεμένες επιχειρήσεις" },
                        new ExpenseType() { Code="E3_585_003", Description = "Δαπάνες από μη συνεργαζόμενα κράτη ή από κράτη με προνομιακό φορολογικό καθεστώς" },
                        new ExpenseType() { Code="E3_585_004", Description = "Δαπάνες για ενημερωτικές ημερίδες" },
                        new ExpenseType() { Code="E3_585_005", Description = "Έξοδα υποδοχής και φιλοξενίας" },
                        new ExpenseType() { Code="E3_585_006", Description = "Έξοδα ταξιδιών" },
                        new ExpenseType() { Code="E3_585_007", Description = "Ασφαλιστικές Εισφορές Αυτοαπασχολούμενων" },
                        new ExpenseType() { Code="E3_585_008", Description = "Έξοδα και προμήθειες παραγγελιοδόχου για λογαριασμό αγροτών" },
                        new ExpenseType() { Code="E3_585_009", Description = "Λοιπές Αμοιβές για υπηρεσίες ημεδαπής" },
                        new ExpenseType() { Code="E3_585_010", Description = "Λοιπές Αμοιβές για υπηρεσίες αλλοδαπής" },
                        new ExpenseType() { Code="E3_585_011", Description = "Ενέργεια" },
                        new ExpenseType() { Code="E3_585_012", Description = "Ύδρευση" },
                        new ExpenseType() { Code="E3_585_013", Description = "Τηλεπικοινωνίες" },
                        new ExpenseType() { Code="E3_585_014", Description = "Ενοίκια" },
                        new ExpenseType() { Code="E3_585_015", Description = "Διαφήμιση και προβολή" },
                        new ExpenseType() { Code="E3_585_016", Description = "Λοιπά έξοδα" },
                        new ExpenseType() { Code="E3_586", Description = "Χρεωστικοί τόκοι και συναφή έξοδα" },
                        new ExpenseType() { Code="E3_587", Description = "Αποσβέσεις" },
                        new ExpenseType() { Code="E3_588", Description = "Ασυνήθη έξοδα, ζημιές και πρόστιμα" },
                        new ExpenseType() { Code="E3_589", Description = "Προβλέψεις (εκτός από προβλέψεις για το προσωπικό)" },
                        new ExpenseType() { Code="E3_882_001", Description = "Αγορές ενσώματων παγίων χρήσης/Χονδρικές" },
                        new ExpenseType() { Code="E3_882_002", Description = "Αγορές ενσώματων παγίων χρήσης/Λιανικές" },
                        new ExpenseType() { Code="E3_882_003", Description = "Αγορές ενσώματων παγίων χρήσης/Εξωτερικού Ενδοκοινοτικές" },
                        new ExpenseType() { Code="E3_882_004", Description = "Αγορές ενσώματων παγίων χρήσης/Εξωτερικού Τρίτες Χώρες" },
                        new ExpenseType() { Code="E3_883_001", Description = "Αγορές μη ενσώματων παγίων χρήσης/Χονδρικές" },
                        new ExpenseType() { Code="E3_883_002", Description = "Αγορές μη ενσώματων παγίων χρήσης/Λιανικές" },
                        new ExpenseType() { Code="E3_883_003", Description = "Αγορές μη ενσώματων παγίων χρήσης/Εξωτερικού Ενδοκοινοτικές" },
                        new ExpenseType() { Code="E3_883_004", Description = "Αγορές μη ενσώματων παγίων χρήσης/Εξωτερικού Τρίτες Χώρες" },
                        new ExpenseType() { Code="VAT_361", Description = "Αγορές & δαπάνες στο εσωτερικό της χώρας" },
                        new ExpenseType() { Code="VAT_362", Description = "Αγορές & εισαγωγές επενδ. Αγαθών (πάγια)" },
                        new ExpenseType() { Code="VAT_363", Description = "Λοιπές εισαγωγές εκτός επενδ. Αγαθών (πάγια)" },
                        new ExpenseType() { Code="VAT_364", Description = "Ενδοκοινοτικές αποκτήσεις αγαθών" },
                        new ExpenseType() { Code="VAT_365", Description = "Ενδοκοινοτικές λήψεις υπηρεσιών άρθρ. 14.2.α" },
                        new ExpenseType() { Code="VAT_366", Description = "Λοιπές πράξεις λήπτη" }
                };
            }
        }
    }

    public class PaymentType
    {
        public int Code { set; get; }
        public string Description { set; get; }

        public PaymentType[] PaymentTypes
        {
            get
            {
                return new PaymentType[]
                {
                        new PaymentType(){ Code = 1, Description = "Επαγ. Λογαριασμός Πληρωμών Ημεδαπής" },
                        new PaymentType(){ Code = 2, Description = "Επαγ. Λογαριασμός Πληρωμών Αλλοδαπής" },
                        new PaymentType(){ Code = 3, Description = "Μετρητά" },
                        new PaymentType(){ Code = 4, Description = "Επιταγή" },
                        new PaymentType(){ Code = 5, Description = "Επί Πιστώσει" },
                };
            }
        }
    }

    public class QuantityType
    {
        public int Code { set; get; }
        public string Description { set; get; }

        public QuantityType[] QuantityTypes
        {
            get
            {
                return new QuantityType[]
                {
                        new QuantityType(){ Code = 1, Description = "Τεμάχια" },
                        new QuantityType(){ Code = 2, Description = "Κιλά" },
                        new QuantityType(){ Code = 3, Description = "Λίτρα" }
                };
            }
        }
    }

    public class MoveReasonType
    {
        public int Code { set; get; }
        public string Description { set; get; }

        public MoveReasonType[] MoveReasonTypes
        {
            get
            {
                return new MoveReasonType[]
                {
                        new MoveReasonType(){ Code = 1, Description = "Πώληση" },
                        new MoveReasonType(){ Code = 2, Description = "Πώληση για Λογαριασμό Τρίτων" },
                        new MoveReasonType(){ Code = 3, Description = "Δειγματισμός" },
                        new MoveReasonType(){ Code = 4, Description = "Έκθεση" },
                        new MoveReasonType(){ Code = 5, Description = "Επιστροφή" },
                        new MoveReasonType(){ Code = 6, Description = "Φύλαξη" },
                        new MoveReasonType(){ Code = 7, Description = "Επεξεργασία  Συναρμολόγηση" },
                        new MoveReasonType(){ Code = 8, Description = "Μεταξύ Εγκαταστάσεων Οντότητας" }
                };
            }
        }
    }

    public class LabelingType
    {
        public int Code { set; get; }
        public string Description { set; get; }

        public LabelingType[] LabelingTypes
        {
            get
            {
                return new LabelingType[]
                {
                        new LabelingType(){ Code = 1, Description = "Εκκαθάριση Πωλήσεων Τρίτων" },
                        new LabelingType(){ Code = 2, Description = "Αμοιβή από Πωλήσεις Τρίτων" }
                };
            }
        }
    }

    public enum NumberValueTypeEnum
    {
        Percentage = 0,
        Amount = 1
    }
}
