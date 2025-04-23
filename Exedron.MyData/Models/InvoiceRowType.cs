using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Exedron.MyData.Models
{
    public class InvoiceRowType
    {
        public int lineNumber { set; get; }
        public decimal? quantity { set; get; }
        public int? measurementUnit { set; get; } //1 Temaxia, 2 Kila, 3 Litra
        public int? invoiceDetailType { set; get; }
        public decimal netValue { set; get; }
        public int vatCategory { set; get; }
        public decimal vatAmount { set; get; }
        public int? vatExemptionCategory { set; get; }
        public ShipType dienergia { set; get; }
        public bool? discountOption { set; get; }
        public decimal? withheldAmount { set; get; }
        public int? withheldPercentCategory { set; get; }
        public decimal? stampDutyAmount { set; get; }
        public int? stampDutyPercentCategory { set; get; }
        public decimal? feesAmount { set; get; }
        public int? feesPercentCategory { set; get; }
        public int? otherTaxesPercentCategory { set; get; }
        public decimal? otherTaxesAmount { set; get; }
        public decimal? deductionsAmount { set; get; }
        public string lineComments { set; get; }
        
        public IncomeClassificationType incomeClassification {set;get;}
        public ExpensesClassificationType expensesClassification { set; get; }

        public InvoiceRowType()
        { }

        public class ShipType
        {
            public string applicationId { set; get; }
            public DateTime applicationDate { set; get; }
            public string doy { set; get; }
            public string shipID { set; get; }
        }

        public class IncomeClassificationType
        {
            [XmlElement(Namespace = "https://www.aade.gr/myDATA/incomeClassificaton/v1.0")]
            public string classificationType { set; get; }
            [XmlElement(Namespace = "https://www.aade.gr/myDATA/incomeClassificaton/v1.0")]
            public string classificationCategory { set; get; }
            [XmlElement(Namespace = "https://www.aade.gr/myDATA/incomeClassificaton/v1.0")]
            public decimal? amount { set; get; }
            [XmlElement(Namespace = "https://www.aade.gr/myDATA/incomeClassificaton/v1.0")]
            public byte id { set; get; }

            //E3_106, 
            //E3_205, 
            //E3_210,
            //E3_305,
            //E3_310,
            //E3_318,
            //E3_561_001,
            //E3_561_002,
            //E3_561_003, 
            //E3_561_004, 
            //E3_561_005, 
            //E3_561_006,
            //E3_561_007,
            //E3_562, 
            //E3_563, 
            //E3_564, 
            //E3_565,
            //E3_566, 
            //E3_567,  
            //E3_568, 
            //E3_569,  
            //E3_570, 
            //E3_595, 
            //E3_596, 
            //E3_597,
            //E3_880_001, 
            //E3_880_002,
            //E3_880_003, 
            //E3_880_004,
            //E3_881_001, 
            //E3_881_002,
            //E3_881_003, 
            //E3_881_004

            //category1_1,  
            //category1_2, 
            //category1_3,  
            //category1_4, 
            //category1_5,  
            //category1_6, 
            //category1_7,  
            //category1_8, 
            //category1_9,  
            //category1_10,
            //category1_95
        }

        public class ExpensesClassificationType
        {
            [XmlElement(Namespace = "https://www.aade.gr/myDATA/expensesClassificaton/v1.0")]
            public string classificationType { set; get; }
            [XmlElement(Namespace = "https://www.aade.gr/myDATA/expensesClassificaton/v1.0")]
            public string classificationCategory { set; get; }
            [XmlElement(Namespace = "https://www.aade.gr/myDATA/expensesClassificaton/v1.0")]
            public decimal? amount { set; get; }
            [XmlElement(Namespace = "https://www.aade.gr/myDATA/expensesClassificaton/v1.0")]
            public byte id { set; get; }

            //E3_101, 
            //E3_102_001, 
            //E3_102_002, 
            //E3_102_003, 
            //E3_102_004, 
            //E3_102_005, 
            //E3_102_006, 
            //E3_104, 
            //E3_201, 
            //E3_202_001, 
            //E3_202_002, 
            //E3_202_003, 
            //E3_202_004, 
            //E3_202_005, 
            //E3_204, 
            //E3_207, 
            //E3_209, 
            //E3_301, 
            //E3_302_001,  
            //E3_302_002, 
            //E3_302_003,  
            //E3_302_004, 
            //E3_302_005,  
            //E3_304,  
            //E3_307,  
            //E3_309,  
            //E3_312, 
            //E3_313_001,  
            //E3_313_002, 
            //E3_313_003,  
            //E3_313_004, 
            //E3_313_005,  
            //E3_315,  
            //E3_581_001,  
            //E3_581_002, 
            //E3_581_003,  
            //E3_582,  
            //E3_583,  
            //E3_584, 
            //E3_585_001,  
            //E3_585_002, 
            //E3_585_003,  
            //E3_585_004, 
            //E3_585_005,  
            //E3_585_006, 
            //E3_585_007,  
            //E3_585_008, 
            //E3_585_009,  
            //E3_585_010, 
            //E3_585_011,  
            //E3_585_012, 
            //E3_585_013,  
            //E3_585_014, 
            //E3_585_015,  
            //E3_585_016, 
            //E3_586,  
            //E3_587,  
            //E3_588,  
            //E3_589,  
            //E3_590,  
            //E3_596, 
            //E3_597, 
            //E3_882_001,     
            //E3_882_002,     
            //E3_882_003,  
            //E3_882_004,     
            //E3_883_001,     
            //E3_883_002,  
            //E3_883_003,  
            //E3_883_004, 
            //VAT_361,  
            //VAT_362, 
            //VAT_363,  
            //VAT_364, 
            //VAT_365,  
            //VAT_366

            //category2_1, 
            //category2_2,
            //category2_3, 
            //category2_4,
            //category2_5, 
            //category2_6,
            //category2_7, 
            //category2_8, 
            //category2_9, 
            //category2_10,
            //category2_11,
            //category2_12,
            //category2_13,
            //category2_14,
            //category2_95
        }
    }

    
}
