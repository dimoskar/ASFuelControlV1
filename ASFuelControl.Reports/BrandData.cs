using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Reports
{
    public class BrandData
    {
        public static string SetBrandData(Data.Invoice inv)
        {
            try
            {
                if (!Data.Implementation.OptionHandler.Instance.GetBoolOption("HasBrandQRCode", false))
                    return "";
                //if (!inv.InvoiceType.DispenserType.HasValue || !inv.InvoiceType.DispenserType.Value)
                //    return "";
                if (inv.InvoiceLines.Count != 1)
                    return "";
                var invLine = inv.InvoiceLines[0];
                int brand = Data.Implementation.OptionHandler.Instance.GetIntOption("Brand", 1);
                if (brand == 1 || brand == 2)
                {
                    BrandQRData ekoData = new BrandQRData();
                    ekoData.AMDIKA = Data.Implementation.OptionHandler.Instance.GetOption("AMDIKA");
                    ekoData.Brand = brand;
                    ekoData.DocDate = inv.TransactionDate;
                    ekoData.DocNo = inv.Number;
                    ekoData.DocTime = inv.TransactionDate;
                    ekoData.DocType = inv.InvoiceType.OfficialEnumerator == 316 ? "B" : "A";
                    ekoData.ID = inv.Number;
                    ekoData.PayCode = inv.PaymentType.HasValue ? inv.PaymentType.Value : 1;
                    ekoData.Price = invLine.UnitPrice;
                    ekoData.Product = invLine.FuelType.Name;
                    string enumValue = invLine.FuelType.EnumeratorValue.ToString();
                    ekoData.ProductType = enumValue;
                    ekoData.Ser = (inv.Series == null || inv.Series == "") ? "A" : inv.Series;
                    ekoData.Store = Data.Implementation.OptionHandler.Instance.GetOption("StoreId");
                    ekoData.Value = inv.TotalAmount.HasValue ? inv.TotalAmount.Value : (decimal)0;
                    ekoData.Volume = invLine.Volume;
                    string brandQRCode = ekoData.GetQData();
                    return brandQRCode;
                }
                return "";
            }
            catch
            {
                return "";
            }
        }
    }

    public class BrandQRData
    {
        public int Brand { set; get; }
        public string Store { set; get; }
        public string AMDIKA { set; get; }
        public DateTime DocDate { set; get; }
        public DateTime DocTime { set; get; }
        public string Ser { set; get; } = "A";
        public int DocNo { set; get; }
        public string DocType { set; get; }
        public int PayCode { set; get; } = 1;
        public string ProductType { set; get; }
        public string Product { set; get; }
        public decimal Price { set; get; }
        public decimal Volume { set; get; }
        public decimal Value { set; get; }
        public int ID { set; get; }

        public string GetQData()
        {
            var str = string.Format("{0};{1};{2};{3:yyyy-MM-dd};{4:HH:mm:ss};{5};{6};{7};{8};{9};{10};{11:N3};{12:N2};{13:N2};{14}",
                this.Brand,
                this.Store,
                this.AMDIKA,
                this.DocDate,
                this.DocTime,
                this.Ser,
                this.DocNo,
                this.DocType,
                this.PayCode,
                this.ProductType,
                this.Product,
                this.Price,
                this.Volume,
                this.Value,
                this.ID);
            return str;
        }
    }
}
