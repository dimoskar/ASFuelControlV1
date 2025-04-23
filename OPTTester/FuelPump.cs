namespace ASFuelControl.Unixfor
{
    public class FuelPump
    {
        public int PumpIndex { set; get; }
        public int PumpNumber { set; get; }
        public ProductInfo[] Products { set; get; }
    }

    public class ProductInfo
    {
        public int ProductIndex { set; get; }
        public int ProductNumber { set; get; }
        public string ProductCode { set; get; }
        public string ProductDescription { set; get; }
        public decimal ProductPrice { set; get; }
    }
}