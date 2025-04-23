using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASFuelControl.UniversalPump
{
    using System.Xml.Serialization;

    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class ControllerDescription
    {
        private System.IO.Ports.SerialPort serialPort = new System.IO.Ports.SerialPort();

        private WorkFlowStepDesc[] workFlowField;

        private StatusDesc[] statusesField;

        private Common.Enumerators.ControllerTypeEnum typeField;

        private ushort baudRateField;

        private System.IO.Ports.Parity parityField;

        private System.IO.Ports.StopBits stopBitsField;

        private byte dataBitsField;

        public System.IO.Ports.SerialPort SerialPort
        {
            get { return this.serialPort; }
            set { this.serialPort = value; }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("WorkFlowStep", IsNullable = false)]
        public WorkFlowStepDesc[] WorkFlow
        {
            get
            {
                return this.workFlowField;
            }
            set
            {
                this.workFlowField = value;
                foreach (WorkFlowStepDesc wfs in this.workFlowField)
                    wfs.ParentController = this;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Status", IsNullable = false)]
        public StatusDesc[] Statuses
        {
            get
            {
                return this.statusesField;
            }
            set
            {
                this.statusesField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public Common.Enumerators.ControllerTypeEnum Type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort BaudRate
        {
            get
            {
                return this.baudRateField;
            }
            set
            {
                this.baudRateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public System.IO.Ports.Parity Parity
        {
            get
            {
                return this.parityField;
            }
            set
            {
                this.parityField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public System.IO.Ports.StopBits StopBits
        {
            get
            {
                return this.stopBitsField;
            }
            set
            {
                this.stopBitsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte DataBits
        {
            get
            {
                return this.dataBitsField;
            }
            set
            {
                this.dataBitsField = value;
            }
        }
    }

    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class WorkFlowStepDesc
    {

        private MethodDesc[] methodField;

        private ConditionEnum conditionField;

        private bool breakLoopField;

        private ControllerDescription parentController;

        [System.Xml.Serialization.XmlIgnore]
        public ControllerDescription ParentController 
        {
            set 
            { 
                this.parentController = value; 

            }
            get { return this.parentController; }
        }


        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Method")]
        public MethodDesc[] Method
        {
            get
            {
                return this.methodField;
            }
            set
            {
                this.methodField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ConditionEnum Condition
        {
            get
            {
                return this.conditionField;
            }
            set
            {
                this.conditionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool BreakLoop
        {
            get
            {
                return this.breakLoopField;
            }
            set
            {
                this.breakLoopField = value;
            }
        }

        public bool ExecuteStep(Common.FuelPoint fp)
        {
            bool shouldExecute = false;
            switch (this.Condition)
            {
                case ConditionEnum.IsWorking:
                    shouldExecute = this.IsWorking(fp);
                    break;
                case ConditionEnum.QueryAuthorize:
                    shouldExecute = this.QueryAuthorize(fp);
                    break;
                case ConditionEnum.QueryHalt:
                    shouldExecute = this.QueryHalt(fp);
                    break;
                case ConditionEnum.QueryPrice:
                    shouldExecute = this.QuerySetPrice(fp);
                    break;
                case ConditionEnum.QueryTotals:
                    shouldExecute = this.QueryTotals(fp);
                    break;
                default:
                    shouldExecute = true;
                    break;
            }
            if (!shouldExecute)
                return this.BreakLoop;
            foreach (MethodDesc method in this.Method)
            {
                if (method.ParentController == null)
                    method.ParentController = this.parentController;
            }
            return this.BreakLoop;
        }

        #region WorkFlow Condition Methods

        private bool QueryTotals(Common.FuelPoint fp)
        {
            int nozzleForTotals = fp.Nozzles.Where(n => n.QueryTotals).Count();
            return nozzleForTotals > 0;
        }

        private bool QuerySetPrice(Common.FuelPoint fp)
        {
            return fp.QuerySetPrice;
        }

        private bool QueryAuthorize(Common.FuelPoint fp)
        {
            return fp.QueryAuthorize;
        }

        private bool QueryHalt(Common.FuelPoint fp)
        {
            return fp.QueryHalt;
        }

        private bool IsWorking(Common.FuelPoint fp)
        {
            return fp.Status == Common.Enumerators.FuelPointStatusEnum.Work;
        }

        #endregion
    }

    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class MethodDesc
    {

        private WriteDesc[] writeField;

        private MethodNameEnum nameField;

        private ControllerDescription parentController;

        [System.Xml.Serialization.XmlIgnore]
        public ControllerDescription ParentController
        {
            set
            {
                this.parentController = value;

            }
            get { return this.parentController; }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Write")]
        public WriteDesc[] Write
        {
            get
            {
                return this.writeField;
            }
            set
            {
                this.writeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public MethodNameEnum Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        public bool ExecuteMethod(Common.FuelPoint fp)
        {
            foreach (WriteDesc write in this.Write)
            {
                if (write.ParentController == null)
                    write.ParentController = this.parentController;

                bool success = write.ExecuteWrite(fp);


            }
            return true;
        }

        #region Actions

        private bool GetStatus(Common.FuelPoint fp)
        {
            return false;
        }

        private bool Initialize(Common.FuelPoint fp)
        {
            return false;
        }

        private bool SetPrice(Common.FuelPoint fp)
        {
            return false;
        }

        private bool GetTotals(Common.FuelPoint fp)
        {
            return false;
        }

        private bool Halt(Common.FuelPoint fp)
        {
            return false;
        }

        private bool Authorize(Common.FuelPoint fp)
        {
            return false;
        }

        private bool GetDisplay(Common.FuelPoint fp)
        {
            return false;
        }

        #endregion

        #region Evaluation Methods

        private byte[] EvaluateData(Common.FuelPoint fp, byte[] response, string data)
        {
            int res = 0;
            List<Byte> buffer = new List<byte>();
            if (data.Length == 0)
                return buffer.ToArray();
            if (data.Length == 1)
            {
                if (int.TryParse(data.ToString(), out res))
                    buffer.Add((byte)res);
                else
                {
                    if (data.ToUpper() == "A")
                        buffer.Add(10);
                    else if (data.ToUpper() == "B")
                        buffer.Add(11);
                    else if (data.ToUpper() == "C")
                        buffer.Add(12);
                    else if (data.ToUpper() == "D")
                        buffer.Add(13);
                    else if (data.ToUpper() == "E")
                        buffer.Add(14);
                    else if (data.ToUpper() == "F")
                        buffer.Add(15);
                    else if (data.ToUpper() == "P")
                        buffer.Add(0);
                }
            }
            else
            {
                if (data[0] == '[')
                {
                    if (data.Contains("EvaluateStatus"))
                        this.EvaluateStatus(fp, response, data);
                    else if (data.Contains("EvaluateDisplay"))
                        this.EvaluateDisplay(fp, response, data);
                    else if (data.Contains("EvaluateVolume"))
                        this.EvaluateVolume(fp, response, data);
                    else if (data.Contains("EvaluateTotalVolume"))
                        this.EvaluateTotalVolume(fp, response, data);
                    else if (data.Contains("EvaluateTotalAmount"))
                        this.EvaluateTotalAmount(fp, response, data);
                }
                else if (data[0] == '{')
                {
                }
                else if (data[0] == 'P')
                {
                }
                else if (data[0] == 'V')
                {
                }
            }
            return buffer.ToArray();
        }

        private void StripCommand()
        {

        }

        private void EvaluateStatus(Common.FuelPoint fp, byte[] response, string data)
        {
        }

        private void EvaluateDisplay(Common.FuelPoint fp, byte[] response, string data)
        {
        }

        private void EvaluateVolume(Common.FuelPoint fp, byte[] response, string data)
        {
        }

        private void EvaluateTotalVolume(Common.FuelPoint fp, byte[] response, string data)
        {
        }

        private void EvaluateTotalAmount(Common.FuelPoint fp, byte[] response, string data)
        {
        }

        #endregion
    }

    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class WriteDesc
    {

        private GetMethodsDesc[] getMethodsField;

        private EvaluationDesc[] evaluationField;

        private string dataField;

        private ControllerDescription parentController;

        [System.Xml.Serialization.XmlIgnore]
        public ControllerDescription ParentController
        {
            set
            {
                this.parentController = value;

            }
            get { return this.parentController; }
        }

        /// <remarks/>
        public GetMethodsDesc[] GetMethods
        {
            get
            {
                return this.getMethodsField;
            }
            set
            {
                this.getMethodsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Evaluation")]
        public EvaluationDesc[] Evaluation
        {
            get
            {
                return this.evaluationField;
            }
            set
            {
                this.evaluationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Data
        {
            get
            {
                return this.dataField;
            }
            set
            {
                this.dataField = value;
            }
        }

        public byte[] CreateCommand(Common.FuelPoint fp)
        {
            string data = this.dataField;
            foreach (GetMethodsDesc getMethod in this.GetMethods)
            {
                if (getMethod.GetAddress != null)
                {

                }
            }
            return new byte[0];
        }

        public bool ExecuteWrite(Common.FuelPoint fp)
        {
            byte[] cmd = this.CreateCommand(fp);

            if (this.parentController.SerialPort == null)
                return false;
            if (!this.parentController.SerialPort.IsOpen)
                return false;

            this.parentController.SerialPort.Write(cmd, 0, cmd.Length);

            return false;
        }
    }

    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class GetPriceBufferDesc
    {

        private int returnIndexField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int ReturnIndex
        {
            get
            {
                return this.returnIndexField;
            }
            set
            {
                this.returnIndexField = value;
            }
        }
    }

    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class EvaluationDesc
    {

        private GetMethodsDesc[] getMethodsField;

        private string responseField;

        private int skipField;

        private int takeField;

        private string methodField;

        /// <remarks/>
        public GetMethodsDesc[] GetMethods
        {
            get
            {
                return this.getMethodsField;
            }
            set
            {
                this.getMethodsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Response
        {
            get
            {
                return this.responseField;
            }
            set
            {
                this.responseField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int Skip
        {
            get
            {
                return this.skipField;
            }
            set
            {
                this.skipField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int Take
        {
            get
            {
                return this.takeField;
            }
            set
            {
                this.takeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Method
        {
            get
            {
                return this.methodField;
            }
            set
            {
                this.methodField = value;
            }
        }
    }

    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class GetMethodsDesc
    {

        private GetAddressDesc getAddressField;
        private GetPriceBufferDesc getPriceBufferField;

        /// <remarks/>
        public GetAddressDesc GetAddress
        {
            get
            {
                return this.getAddressField;
            }
            set
            {
                this.getAddressField = value;
            }
        }

        /// <remarks/>
        public GetPriceBufferDesc GetPriceBuffer
        {
            get
            {
                return this.getPriceBufferField;
            }
            set
            {
                this.getPriceBufferField = value;
            }
        }
    }

    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class GetAddressDesc
    {

        private int param1Field;

        private int param2Field;

        private int returnIndexField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int Param1
        {
            get
            {
                return this.param1Field;
            }
            set
            {
                this.param1Field = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int Param2
        {
            get
            {
                return this.param2Field;
            }
            set
            {
                this.param2Field = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int ReturnIndex
        {
            get
            {
                return this.returnIndexField;
            }
            set
            {
                this.returnIndexField = value;
            }
        }

        public byte[] Evaluate(Common.FuelPoint fp)
        {

        }
    }

    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class StatusDesc
    {

        private string dataField;

        private Common.Enumerators.FuelPointStatusEnum valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Data
        {
            get
            {
                return this.dataField;
            }
            set
            {
                this.dataField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public Common.Enumerators.FuelPointStatusEnum Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

}
