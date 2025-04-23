using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.UniversalPump
{
    //[Serializable]
    //public class ControllerDescription
    //{
    //    [System.Xml.Serialization.XmlAttribute]
    //    public Common.Enumerators.ControllerTypeEnum Type { set; get; }

    //    [System.Xml.Serialization.XmlAttribute]
    //    public int BaudRate { set; get; }

    //    [System.Xml.Serialization.XmlAttribute]
    //    public int DataBits { set; get; }

    //    [System.Xml.Serialization.XmlAttribute]
    //    public System.IO.Ports.StopBits StopBits { set; get; }

    //    [System.Xml.Serialization.XmlAttribute]
    //    public System.IO.Ports.Parity Parity { set; get; }

    //    public WorkFlowDescription WorkFlow { set; get; }

    //    public StatusDescription[] Statuses { set; get; }
    //}

    //[Serializable]
    //public class WorkFlowDescription
    //{
    //    public WorkFlowStepDescription[] WorkFlowSteps { set; get; }
    //}

    //public class WorkFlowStepDescription
    //{
    //    [System.Xml.Serialization.XmlAttribute]
    //    public bool BreakLoop { set; get; }

    //    [System.Xml.Serialization.XmlAttribute]
    //    public ConditionEnum Condition { set; get; }

    //    public WriteDescription[] Writes { set; get; }

    //    #region Actions

    //    private bool GetStatus(Common.FuelPoint fp)
    //    {
    //        return false;
    //    }

    //    private bool Initialize(Common.FuelPoint fp)
    //    {
    //        return false;
    //    }

    //    private bool SetPrice(Common.FuelPoint fp)
    //    {
    //        return false;
    //    }

    //    private bool GetTotals(Common.FuelPoint fp)
    //    {
    //        return false;
    //    }

    //    private bool Halt(Common.FuelPoint fp)
    //    {
    //        return false;
    //    }

    //    private bool Authorize(Common.FuelPoint fp)
    //    {
    //        return false;
    //    }

    //    private bool GetDisplay(Common.FuelPoint fp)
    //    {
    //        return false;
    //    }

    //    #endregion

    //    #region Evaluation Methods

    //    private byte[] EvaluateData(Common.FuelPoint fp, byte[] response, string data)
    //    {
    //        int res = 0;
    //        List<Byte> buffer = new List<byte>();
    //        if (data.Length == 0)
    //            return buffer.ToArray();
    //        if (data.Length == 1)
    //        {
    //            if (int.TryParse(data.ToString(), out res))
    //                buffer.Add((byte)res);
    //            else
    //            {
    //                if (data.ToUpper() == "A")
    //                    buffer.Add(10);
    //                else if (data.ToUpper() == "B")
    //                    buffer.Add(11);
    //                else if (data.ToUpper() == "C")
    //                    buffer.Add(12);
    //                else if (data.ToUpper() == "D")
    //                    buffer.Add(13);
    //                else if (data.ToUpper() == "E")
    //                    buffer.Add(14);
    //                else if (data.ToUpper() == "F")
    //                    buffer.Add(15);
    //                else if (data.ToUpper() == "P")
    //                    buffer.Add(0);
    //            }
    //        }
    //        else
    //        {
    //            if (data[0] == '[')
    //            {
    //                if (data.Contains("EvaluateStatus"))
    //                    this.EvaluateStatus(fp, response, data);
    //                else if (data.Contains("EvaluateDisplay"))
    //                    this.EvaluateDisplay(fp, response, data);
    //                else if (data.Contains("EvaluateVolume"))
    //                    this.EvaluateVolume(fp, response, data);
    //                else if (data.Contains("EvaluateTotalVolume"))
    //                    this.EvaluateTotalVolume(fp, response, data);
    //                else if (data.Contains("EvaluateTotalAmount"))
    //                    this.EvaluateTotalAmount(fp, response, data);
    //            }
    //            else if (data[0] == '{')
    //            {
    //            }
    //            else if (data[0] == 'P')
    //            {
    //            }
    //            else if (data[0] == 'V')
    //            {
    //            }
    //        }
    //        return buffer.ToArray();
    //    }

    //    private void StripCommand()
    //    {

    //    }

    //    private void EvaluateStatus(Common.FuelPoint fp, byte[] response, string data)
    //    {
    //    }

    //    private void EvaluateDisplay(Common.FuelPoint fp, byte[] response, string data)
    //    {
    //    }

    //    private void EvaluateVolume(Common.FuelPoint fp, byte[] response, string data)
    //    {
    //    }

    //    private void EvaluateTotalVolume(Common.FuelPoint fp, byte[] response, string data)
    //    {
    //    }

    //    private void EvaluateTotalAmount(Common.FuelPoint fp, byte[] response, string data)
    //    {
    //    }

    //    #endregion

    //    #region WorkFlow Condition Methods

    //    private bool QueryTotals(Common.FuelPoint fp)
    //    {
    //        int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();
    //        return nozzleForTotals > 0;
    //    }

    //    private bool QuerySetPrice(Common.FuelPoint fp)
    //    {
    //        return fp.QuerySetPrice;
    //    }

    //    private bool QueryAuthorize(Common.FuelPoint fp)
    //    {
    //        return fp.QueryAuthorize;
    //    }

    //    private bool QueryHalt(Common.FuelPoint fp)
    //    {
    //        return fp.QueryHalt;
    //    }

    //    private bool IsWorking(Common.FuelPoint fp)
    //    {
    //        return fp.Status == Common.Enumerators.FuelPointStatusEnum.Work;
    //    }

    //    #endregion
    //}

    //[Serializable]
    //public class WriteDescription
    //{
    //    string data;

    //    [System.Xml.Serialization.XmlAttribute]
    //    public string Data
    //    {
    //        set { this.data = value; }
    //        get { return this.data; }
    //    }

    //    [System.Xml.Serialization.XmlAttribute]
    //    public CRCTypeEnum CRCType { set; get; }
    //}

    //[Serializable]
    //public class EvaluationDescription
    //{
    //    [System.Xml.Serialization.XmlAttribute]
    //    public MethodNameEnum Method { set; get; }

    //    [System.Xml.Serialization.XmlAttribute]
    //    public int Skip { set; get; }

    //    [System.Xml.Serialization.XmlAttribute]
    //    public int Take { set; get; }

    //    [System.Xml.Serialization.XmlAttribute]
    //    public CRCTypeEnum CRCType { set; get; }
    //}

    //[Serializable]
    //public class ActionDescription
    //{
    //    [System.Xml.Serialization.XmlAttribute]
    //    public string Data { set; get; }

    //    [System.Xml.Serialization.XmlAttribute]
    //    public int Delay { set; get; }

    //    [System.Xml.Serialization.XmlAttribute]
    //    public ActionTypeEnum Action { set; get; }

    //    [System.Xml.Serialization.XmlAttribute]
    //    public CRCTypeEnum CRCType { set; get; }

    //    public bool ExecuteAction(Common.FuelPoint fp)
    //    {
    //        if (this.Data.Length == 0)
    //            return true;

            
    //        string[] data = this.Data.Split(';');
            
    //        if (data.Length > 0)
    //        {
    //            for (int i = 0; i < data.Length; i++)
    //            {
    //            }
                
    //        }
    //        return false;
    //    }
    //}

    //[Serializable]
    //public class StatusDescription
    //{
    //    [System.Xml.Serialization.XmlAttribute]
    //    public Common.Enumerators.FuelPointStatusEnum Status { set; get; }

    //    [System.Xml.Serialization.XmlAttribute]
    //    public string Data { set; get; }
    //}

    #region Enumerators

    public enum ActionTypeEnum
    {
        Read,
        Write
    }

    public enum MethodNameEnum
    {
        GetStatus,
        GetTotals,
        AuthorizeFuelPoint,
        GetDisplay,
        SetPrice,
        GetPrice
    }

    public enum CRCTypeEnum
    {
        None,
        LRC,
        CRC16,
        Gilbarco,
        Supplementary
    }

    public enum ConditionEnum
    {
        QueryInitialize,
        QueryPrice,
        QueryTotals,
        QueryHalt,
        QueryAuthorize,
        IsWorking
    }

    #endregion
}
