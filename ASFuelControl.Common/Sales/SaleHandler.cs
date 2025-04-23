//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace ASFuelControl.Common.Sales
//{
//    public class SaleHandler
//    {
//        private bool invalidTotals = false;
//        private string _case = "";
//        private int tries = 0;
//        public Common.Nozzle ParentNozzle
//        {
//            set;
//            get;
//        }

//        public List<TotalizerState> totalizers = new List<TotalizerState>();

//        public SaleData AddTotalizer(decimal nozzleTotalizer, decimal unitPrice, decimal dispensedVolume, decimal dispensedPrice)
//        {   
                   
//            int fpAdd = ParentNozzle.ParentFuelPoint.Address;
//            int fpChn = ParentNozzle.ParentFuelPoint.Channel;
//            decimal totalizerDiff = 0;
            
//            TotalizerState state = new TotalizerState();
//            try
//            {
//                state.Totalizer = nozzleTotalizer;
//                state.Sale = new SaleData();
//                state.Sale.TotalizerEnd = nozzleTotalizer;
              

//                if (this.totalizers.Count == 0)
//                {
//                    state.Sale.SaleCompleted = true;
//                    this.totalizers.Add(state);
//                    ParentNozzle.QueryTotals = false;
//                    return state.Sale;
//                }
                
//                TotalizerState lastState = this.totalizers.LastOrDefault();
//                if (lastState == null)
//                {
//                    state.Sale.SaleCompleted = true;
//                    this.totalizers.Add(state);
//                    ParentNozzle.QueryTotals = false;
//                    return state.Sale;
//                }
                
//                state.Sale.TotalizerStart = lastState.Totalizer;
//                state.Sale.TotalVolume = dispensedVolume;
//                state.Sale.TotalPrice = dispensedPrice;
//                state.Sale.UnitPrice = unitPrice;
//                totalizerDiff = (state.Sale.TotalizerEnd - state.Sale.TotalizerStart) / 100;

//                if (totalizerDiff > 0 && totalizerDiff != dispensedVolume)
//                {
//                    _case = "totalizerDiff > 0 && totalizerDiff != dispensedVolume";

//                    state.Sale.TotalVolume = totalizerDiff;//Litra=diafora apo totals
//                    decimal calcAmount = decimal.Round(state.Sale.TotalVolume * unitPrice, 2, MidpointRounding.AwayFromZero);
//                    decimal decimalPart = (int)(calcAmount * 100) - 100 * (int)calcAmount;

//                    //if((dispensedPrice - calcAmount) < 0.50m || (calcAmount - dispensedPrice) < 0.50m)
//                    if (Math.Abs((dispensedPrice - calcAmount)) < 0.50m)
//                    {   //an h diafora sta euro einai +-50cents
//                        //pare oti leei to display
//                        state.Sale.TotalPrice = dispensedPrice;
//                        state.Sale.TotalVolume = dispensedVolume;
//                    }
//                    else
//                    {
//                        //Litra=diafora apo totals
//                        //Euro=Litra*unitprice;
//                        state.Sale.TotalPrice = calcAmount;
//                    }

//                    state.Sale.SaleCompleted = true;
//                    this.totalizers.Add(state);
//                    ParentNozzle.QueryTotals = false;
//                    ParentNozzle.TotalMisfunction = false;
//                    invalidTotals = false;

//                }
//                else if (totalizerDiff > 0 && totalizerDiff == dispensedVolume)
//                {
//                    _case = "totalizerDiff > 0 && totalizerDiff == dispensedVolume";
//                    state.Sale.TotalVolume = totalizerDiff;
//                    state.Sale.TotalPrice = dispensedPrice;
//                    state.Sale.SaleCompleted = true;
//                    this.totalizers.Add(state);
//                    ParentNozzle.QueryTotals = false;
//                    ParentNozzle.TotalMisfunction = false;
//                    invalidTotals = false;
                    
//                }
//                else if (totalizerDiff < 0)
//                {
//                    _case = "totalizerDiff < 0";
//                    this.totalizers.Clear();
//                    state.Sale.SaleCompleted = true;
//                    this.totalizers.Add(state);
//                    ParentNozzle.TotalMisfunction = false;
//                    ParentNozzle.QueryTotals = false;
//                }
//                else if ((totalizerDiff == 0 && dispensedVolume > 0) )
//                {   
//                    //RequestTotals again and again
//                    tries = (int)ParentNozzle.ParentFuelPoint.GetExtendedProperty("tries", (int)0);
//                    tries++;
//                    ParentNozzle.ParentFuelPoint.SetExtendedProperty("tries", tries);
//                    if((int)ParentNozzle.ParentFuelPoint.GetExtendedProperty("tries", (int)0) < 150)
//                    {
//                        _case = "totalizerDiff == 0 && dispensedVolume > 0) || (totalizerDiff>0 && dispensedVolume==0";
//                        ParentNozzle.QueryTotals = true;
//                        ParentNozzle.TotalMisfunction = true;
//                        invalidTotals = true;
//                        ParentNozzle.ParentFuelPoint.SetExtendedProperty("iNeedDisplay", true);
//                    }
//                    else
//                    {
//                        _case = "too many tries";
//                        ParentNozzle.QueryTotals = false;
//                        ParentNozzle.TotalMisfunction = false;
//                        invalidTotals = false;
//                        ParentNozzle.ParentFuelPoint.SetExtendedProperty("iNeedDisplay", true);

//                        //INVOICE BASED 100% DISPLAY 
//                        state.Sale.TotalPrice = dispensedPrice;
//                        state.Sale.TotalVolume = dispensedVolume;
//                        state.Sale.SaleCompleted = true;
//                        this.totalizers.Add(state);
//                        ParentNozzle.QueryTotals = false;
//                        ParentNozzle.TotalMisfunction = false;
//                        invalidTotals = false;


//                     //   throw new Exception("Tried 150 times");

//                    }
//                }
//                else if(totalizerDiff == 0 && dispensedVolume == 0)
//                {
//                    _case = "totalizerDiff==0 && dispensedVolume ==0";
//                    ParentNozzle.QueryTotals = false;
//                    ParentNozzle.TotalMisfunction = false;
//                    invalidTotals = false;
                    
//                }
//                else
//                {
//                    _case = "else";
//                    if(!invalidTotals)
//                        ParentNozzle.QueryTotals = false;
//                }

//                if(Properties.Settings.Default.SalesLogger)
//                {
//                    fpAdd = ParentNozzle.ParentFuelPoint.Address;
//                    fpChn = ParentNozzle.ParentFuelPoint.Channel;
//                    //Address, Channel, TotalizerStart, TotalizerEnd, totalizerDiff, dispenseVolume, dispensedPrice, unitprice, case, invalidTotals,time
//                    System.IO.Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\logs" + "\\SalesHandler");
//                    System.IO.File.AppendAllText(System.Environment.CurrentDirectory+"\\logs"+"\\SalesHandler" + "\\SalesHandler" +"_"+ fpAdd + "_" + fpChn +".txt",
//                       string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\n",
//                       ParentNozzle.ParentFuelPoint.Address,//0
//                       ParentNozzle.ParentFuelPoint.Channel,//1
//                       lastState.Totalizer,                 //2 TotalizerStart
//                       state.Sale.TotalizerEnd,             //3
//                       totalizerDiff,                       //4 Start-End
//                       dispensedVolume,                     //5
//                       dispensedPrice,                      //6
//                       _case,                               //7
//                       invalidTotals,                       //8
//                       unitPrice,                           //9
//                       System.DateTime.Now));               //10
//                }

//                return state.Sale;
//            }

//            catch(Exception e)
//            {
//                if(Properties.Settings.Default.SalesLogger)
//                {
//                    System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\SalesHandler" + "\\SalesHandler" + "_" + fpAdd + "_" + fpChn + "_Ex.txt", "\n" + e.ToString());
//                }
//                return state.Sale;
//            }
//            finally
//            {
//                ParentNozzle.ParentFuelPoint.SetExtendedProperty("iNeedDisplay", false);
                              
//                if(! ((totalizerDiff == 0 && dispensedVolume > 0) && (int)ParentNozzle.ParentFuelPoint.GetExtendedProperty("tries", (int)0) < 150))
//                {
//                    ParentNozzle.ParentFuelPoint.DispensedVolume = 0;
//                    ParentNozzle.ParentFuelPoint.DispensedAmount = 0;
//                }
//                for (int i = 20; i < this.totalizers.Count; i++)
//                {
//                    this.totalizers.RemoveAt(0);
//                }   
//            }
//        }

      
//        public SaleData GetLastSale()
//        {
//            if(totalizers.Count == 0)
//                return null;
//            return this.totalizers.Last().Sale;
            
//        }
//    }

//    public class TotalizerState
//    {
//        public decimal Totalizer { set; get; }
//        public SaleData Sale { set; get; }
//    }
//}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.Common.Sales
{
    /// <summary>
    /// class that contains methods for sale processing
    /// </summary>
    public class SaleHandler
    {
        private bool invalidTotals = false;
        private string _case = "";
        private int tries = 0;
        public Common.Nozzle ParentNozzle
        {
            set;
            get;
        }

        public void ClearTotalizers()
        {
            this.totalizers.Clear();
        }

        /// <summary>
        /// List of the last totalizers of the nozzle attached
        /// </summary>
        public List<TotalizerState> totalizers = new List<TotalizerState>();

        /// <summary>
        /// Adds the first entry in the totalizers list
        /// </summary>
        /// <param name="nozzleTotalizer"></param>
        public void AddDummyTotalizer(decimal nozzleTotalizer)
        {
            TotalizerState state = new TotalizerState();
            state.Totalizer = nozzleTotalizer;
            state.Sale = new SaleData();
            state.Sale.TotalizerEnd = nozzleTotalizer;
            state.Sale.SaleCompleted = true;
            this.totalizers.Add(state);
        }

        /// <summary>
        /// Adds the last totalizer of the attached nozzle and hanldels the sale process.
        /// </summary>
        /// <param name="nozzleTotalizer"></param>
        /// <param name="unitPrice"></param>
        /// <param name="dispensedVolume"></param>
        /// <param name="dispensedPrice"></param>
        /// <returns></returns>
        public SaleData AddTotalizer(decimal nozzleTotalizer, decimal unitPrice, decimal dispensedVolume, decimal dispensedPrice)
        {

            int fpAdd = ParentNozzle.ParentFuelPoint.Address;
            int fpChn = ParentNozzle.ParentFuelPoint.Channel;
            decimal totalizerDiff = 0;

            TotalizerState state = new TotalizerState();
            try
            {
                state.Totalizer = nozzleTotalizer;
                state.Sale = new SaleData();
                state.Sale.TotalizerEnd = nozzleTotalizer;


                //Count of totalizers entries == 0. The nozzle is first time in use in the current session
                if (this.totalizers.Count == 0)
                {
                    //state.Sale.TotalizerStart = nozzleTotalizer - dispensedVolume * ;
                    //state.Sale.TotalVolume = dispensedVolume;
                    //state.Sale.TotalPrice = dispensedPrice;
                    //state.Sale.UnitPrice = unitPrice;
                    if (Properties.Settings.Default.SalesLogger)
                    {
                        if (!System.IO.Directory.Exists(System.Environment.CurrentDirectory + "\\logs" + "\\SalesHandler"))
                            System.IO.Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\logs" + "\\SalesHandler");
                        string msg = string.Format("Totalizer Count = 0 Parameters : Totalizer={0}, DispensedVolume={1}, DispensedPrice={2}, UnitPrice={3}", nozzleTotalizer, dispensedVolume, dispensedPrice, unitPrice);
                        System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\SalesHandler" + "\\SalesHandler" + "_" + fpAdd + "_" + fpChn + "_Ex.txt", msg +"\r\n");
                    }
                    state.Sale.SaleCompleted = true;
                    this.totalizers.Add(state);
                    ParentNozzle.QueryTotals = false;
                    return state.Sale;
                }

                TotalizerState lastState = this.totalizers.LastOrDefault();
                if (lastState == null)
                {
                    state.Sale.SaleCompleted = true;
                    this.totalizers.Add(state);
                    ParentNozzle.QueryTotals = false;
                    return state.Sale;
                }

                state.Sale.TotalizerStart = lastState.Totalizer;
                state.Sale.TotalVolume = dispensedVolume;
                state.Sale.TotalPrice = dispensedPrice;
                state.Sale.UnitPrice = unitPrice;
                totalizerDiff = (state.Sale.TotalizerEnd - state.Sale.TotalizerStart) / (decimal)Math.Pow(10, ParentNozzle.ParentFuelPoint.TotalDecimalPlaces);

                //There is a missmatch of the totalizers of the nozzle and the displayed values.
                if (totalizerDiff > 0 && totalizerDiff != dispensedVolume)
                {
                    _case = "totalizerDiff > 0 && totalizerDiff != dispensedVolume";

                    state.Sale.TotalPrice = dispensedPrice != 0 ? dispensedPrice : (decimal)0.01;
                    state.Sale.TotalVolume = dispensedVolume != 0 ? dispensedVolume : (decimal)0.01;

                    state.Sale.SaleCompleted = true;
                    this.totalizers.Add(state);
                    ParentNozzle.QueryTotals = false;
                    ParentNozzle.TotalMisfunction = false;
                    invalidTotals = false;

                }
                //Sale is OK. Totalizer and Display are OK
                else if (totalizerDiff > 0 && totalizerDiff == dispensedVolume)
                {
                    _case = "totalizerDiff > 0 && totalizerDiff == dispensedVolume";
                    state.Sale.TotalVolume = totalizerDiff;
                    state.Sale.TotalPrice = dispensedPrice;
                    state.Sale.SaleCompleted = true;
                    this.totalizers.Add(state);
                    ParentNozzle.QueryTotals = false;
                    ParentNozzle.TotalMisfunction = false;
                    invalidTotals = false;

                }
                ///Totalizer is less than previous. Clears all entries in totalizers and keeps last totalizer as reference.
                else if (totalizerDiff < 0)
                {
                    _case = "totalizerDiff < 0";
                    this.totalizers.Clear();
                    state.Sale.SaleCompleted = true;
                    this.totalizers.Add(state);
                    ParentNozzle.TotalMisfunction = false;
                    ParentNozzle.QueryTotals = false;

                }
                //Totalizer diff is 0 but there is an display value. 
                else if ((totalizerDiff == 0 && dispensedVolume > 0))
                {
                    //RequestTotals again and again
                    tries = (int)ParentNozzle.ParentFuelPoint.GetExtendedProperty("tries", (int)0);
                    tries++;
                    ParentNozzle.ParentFuelPoint.SetExtendedProperty("tries", tries);
                    if ((int)ParentNozzle.ParentFuelPoint.GetExtendedProperty("tries", (int)0) < 10)
                    {
                        _case = "totalizerDiff == 0 && dispensedVolume > 0) || (totalizerDiff>0 && dispensedVolume==0";
                        ParentNozzle.QueryTotals = true;
                        ParentNozzle.TotalMisfunction = true;
                        invalidTotals = true;
                        ParentNozzle.ParentFuelPoint.SetExtendedProperty("iNeedDisplay", true);


                    }
                    else
                    {
                        _case = "too many tries";
                        ParentNozzle.QueryTotals = false;
                        ParentNozzle.TotalMisfunction = false;
                        invalidTotals = false;
                        ParentNozzle.ParentFuelPoint.SetExtendedProperty("iNeedDisplay", true);

                        //INVOICE BASED 100% DISPLAY 
                        state.Sale.TotalPrice = dispensedPrice;
                        state.Sale.TotalVolume = dispensedVolume;
                        state.Sale.SaleCompleted = true;
                        this.totalizers.Add(state);
                        ParentNozzle.QueryTotals = false;
                        ParentNozzle.TotalMisfunction = false;
                        invalidTotals = false;


                        //   throw new Exception("Tried 150 times");

                    }
                }
                else if (totalizerDiff == 0 && dispensedVolume == 0)
                {
                    _case = "totalizerDiff==0 && dispensedVolume ==0";
                    ParentNozzle.QueryTotals = false;
                    ParentNozzle.TotalMisfunction = false;
                    invalidTotals = false;

                }
                else
                {
                    _case = "else";
                    if (!invalidTotals)
                        ParentNozzle.QueryTotals = false;
                }

                if (Properties.Settings.Default.SalesLogger)
                {
                    fpAdd = ParentNozzle.ParentFuelPoint.Address;
                    fpChn = ParentNozzle.ParentFuelPoint.Channel;
                    //Address, Channel, TotalizerStart, TotalizerEnd, totalizerDiff, dispenseVolume, dispensedPrice, unitprice, case, invalidTotals,time
                    if(!System.IO.Directory.Exists(System.Environment.CurrentDirectory + "\\logs" + "\\SalesHandler"))
                        System.IO.Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\logs" + "\\SalesHandler");
                    System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\SalesHandler" + "\\SalesHandler" + "_" + fpAdd + "_" + fpChn + ".txt",
                       string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\n",
                       ParentNozzle.ParentFuelPoint.Address,//0
                       ParentNozzle.ParentFuelPoint.Channel,//1
                       lastState.Totalizer,                 //2 TotalizerStart
                       state.Sale.TotalizerEnd,             //3
                       totalizerDiff,                       //4 Start-End
                       dispensedVolume,                     //5
                       dispensedPrice,                      //6
                       _case,                               //7
                       invalidTotals,                       //8
                       unitPrice,                           //9
                       System.DateTime.Now) + "\r\n");               //10
                }

                return state.Sale;
            }

            catch (Exception e)
            {
                if (Properties.Settings.Default.SalesLogger)
                {
                    if (!System.IO.Directory.Exists(System.Environment.CurrentDirectory + "\\logs" + "\\SalesHandler"))
                        System.IO.Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\logs" + "\\SalesHandler");
                    System.IO.File.AppendAllText(System.Environment.CurrentDirectory + "\\logs" + "\\SalesHandler" + "\\SalesHandler" + "_" + fpAdd + "_" + fpChn + "_Ex.txt", "\n" + e.ToString());
                }
                return state.Sale;
            }
            finally
            {
                ParentNozzle.ParentFuelPoint.SetExtendedProperty("iNeedDisplay", false);

                if (!((totalizerDiff == 0 && dispensedVolume > 0) && (int)ParentNozzle.ParentFuelPoint.GetExtendedProperty("tries", (int)0) < 150))
                {
                    ParentNozzle.ParentFuelPoint.DispensedVolume = 0;
                    ParentNozzle.ParentFuelPoint.DispensedAmount = 0;
                }
                for (int i = 20; i < this.totalizers.Count; i++)
                {
                    this.totalizers.RemoveAt(0);
                }
            }
        }


        public SaleData GetLastSale()
        {
            if (totalizers.Count == 0)
                return null;
            return this.totalizers.Last().Sale;

        }
    }

    public class TotalizerState
    {
        public decimal Totalizer { set; get; }
        public SaleData Sale { set; get; }
    }
}
