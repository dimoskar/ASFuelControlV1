using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Windows.ViewModels
{
    public class EKOQRData
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
                this, ID);
            return str;
        }
    }
}
