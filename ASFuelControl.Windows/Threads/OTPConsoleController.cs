using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASFuel.AutoPay.Common;
using System.Collections.Concurrent;
using ASFuelControl.VirtualDevices;

namespace ASFuelControl.Windows.Threads
{
    public class OTPConsoleController : IConsoleInterface
    {
        public event EventHandler OTPStatusChanged;
        public bool OPTStatus { set; get; }
        public bool IsRunning
        {
            get
            {
                if (this.messenger == null)
                    return false;
                return this.messenger.IsRunning;
            }
        }
        private string clientIp;
        private int clientPort = 20000;
        private string serverIp;
        private int serverPort = 20000;
        private Guid[] nozzles = new Guid[] { };
        private Messenger messenger;
        private bool pendingDisableOTP = false;
        private bool pendingEnableOTP = false;
        //private SimpleTCP.SimpleTcpClient client = new SimpleTCP.SimpleTcpClient();
        //private SimpleTCP.SimpleTcpServer server = new SimpleTCP.SimpleTcpServer();
        private ConcurrentDictionary<Guid, OpenSale> openSales = new ConcurrentDictionary<Guid, OpenSale>();
        //private ConcurrentDictionary<Guid, BaseMessage> messages = new ConcurrentDictionary<Guid, BaseMessage>();
        private Dictionary<VirtualDevices.VirtualDispenser, Guid[]> dispNozzles = new Dictionary<VirtualDispenser, Guid[]>();
        

        private List<VirtualDevices.VirtualDispenser> dispensers = new List<VirtualDevices.VirtualDispenser>(); 

        public int VendingMachineId { get; set; }

        public OTPConsoleController(string serverIp, int serverPort, string clientIp, int clientPort)
        {
            this.clientIp = clientIp;
            this.clientPort = clientPort;
            this.serverIp = serverIp;
            this.serverPort = serverPort;
            this.messenger = new Messenger(this.serverIp, this.serverPort, EndPointEnum.Console, 1);
            this.messenger.MessageRecieved += Messenger_MessageRecieved;
            this.messenger.MessageResponsePrepared += Messenger_MessageResponsePrepared;
            this.messenger.MessageFailed += Messenger_MessageFailed;
            this.messenger.AddReciever(new ClientDescriptor() { EndPointType = EndPointEnum.OTP, Id = 1, Port = clientPort, Ip = System.Net.IPAddress.Parse(clientIp) });
            this.messenger.AddReciever(new ClientDescriptor() { EndPointType = EndPointEnum.Proxy, Id = 1, Port = clientPort, Ip = System.Net.IPAddress.Parse(clientIp) });
            this.messenger.Start();
        }

        public void CloseServer()
        {
            this.messenger.Stop();
            //if(this.server.IsStarted)
            //    this.server.Stop();
        }

        public void AddDispenser(VirtualDevices.VirtualDispenser dispenser, Guid[] nozzleIds)
        {
            if (this.dispensers.Where(d => d.DispenserId == dispenser.DispenserId).Count() > 0)
                return;
            dispensers.Add(dispenser);
            dispNozzles.Add(dispenser, nozzleIds);
        }

        public void GetNextSale(VirtualDevices.VirtualDispenser dispenser)
        {
            
            if (this.dispensers.Where(d=>d.DispenserId == dispenser.DispenserId).Count() == 0)
                return;
            if (dispenser.CurrentVendingMachineSale != null)
            {
                return;
            }
            int[] nozzles = dispenser.Nozzles.Select(n => n.NozzleOfficialNumber).ToArray();
            OpenSale sale = this.openSales.Values.Where(s => s.DispenserId == dispenser.OfficialNumber && nozzles.Contains(s.NozzleId)).
                OrderBy(s => s.DateTimeStarted).FirstOrDefault();

            if(sale != null)
            {
                if (this.openSales.TryGetValue(sale.SaleId, out sale) == false)
                    return;
                dispenser.CurrentVendingMachineSale = sale;
                sale.SaleState = SaleStateEnum.Started;
            }
            //dispenser.CurrentVendingMachineSale = this.GetNextSale(dispenser.DispenserNumber);
            //if (dispenser.CurrentVendingMachineSale != null)
            //    dispenser.CurrentVendingMachineSale.SaleState = SaleStateEnum.Started;
            
        }

        public void TranactionCompleted(VirtualDevices.VirtualNozzle nozzle, decimal totalPrice, decimal totalVolume)
        {
            if (nozzle.ParentDispenser.CurrentVendingMachineSale == null)
                return;
            if (totalPrice == 0 && totalVolume == 0 && nozzle.ParentDispenser.CurrentVendingMachineSale.NozzleId != nozzle.NozzleOfficialNumber)
                return;
            Guid saleId = nozzle.ParentDispenser.CurrentVendingMachineSale.SaleId;
            nozzle.ParentDispenser.CurrentVendingMachineSale.SaleState = SaleStateEnum.Finished;
            nozzle.ParentDispenser.CurrentVendingMachineSale.SalesAmount = totalPrice;
            nozzle.ParentDispenser.CurrentVendingMachineSale.SalesVolume = totalVolume;
            nozzle.ParentDispenser.CurrentVendingMachineSale.UnitPrice = nozzle.CurrentSaleUnitPrice;
            bool done = this.TranactionCompleted(nozzle.ParentDispenser.OfficialNumber, nozzle.NozzleOfficialNumber, nozzle.ParentDispenser.CurrentVendingMachineSale);
            if (!done)
            {
                System.Threading.Thread.Sleep(1000);
                done = this.TranactionCompleted(nozzle.ParentDispenser.OfficialNumber, nozzle.NozzleOfficialNumber, nozzle.ParentDispenser.CurrentVendingMachineSale);
            }
            OpenSale sale = nozzle.ParentDispenser.CurrentVendingMachineSale;
            this.openSales.TryRemove(sale.SaleId, out sale);
            nozzle.ParentDispenser.CurrentVendingMachineSale = null;
        }

        public OpenSale GetNextSale(int dispenserNumber)
        {
            if (this.dispensers.Where(d => d.OfficialNumber == dispenserNumber).Count() == 0)
                return null;
            VirtualDispenser disp = this.dispensers.Where(d => d.OfficialNumber == dispenserNumber).FirstOrDefault();
            this.GetNextSale(disp);
            return disp.CurrentVendingMachineSale;
        }

        //public OpenSale GetNextSale(int dispenserNumber)
        //{
        //    try
        //    {
        //        client.Connect(this.clientIp, this.clientPort);
        //        ASFuel.AutoPay.Common.GetNextSale msg = new ASFuel.AutoPay.Common.GetNextSale();
        //        msg.VendingMachineId = this.VendingMachineId;
        //        msg.DispenserId = dispenserNumber;

        //        string data = ASFuel.AutoPay.Common.BaseMessage.SerializeMessage(msg);
        //        SimpleTCP.Message mgsRet = client.WriteLineAndGetReply(data, TimeSpan.FromMilliseconds(200));
        //        client.Disconnect();

        //        //int posEnd = mgsRet.MessageString.LastIndexOf('>');
        //        data = mgsRet.MessageString;
        //        //if (posEnd > 0)
        //        //{
        //        //    data = data.Substring(0, posEnd + 1);
        //        //}
        //        BaseMessage msgRet = BaseMessage.Deserialize(data, msg.MessageId);
        //        GetNextSaleReturn sret = msgRet as GetNextSaleReturn;
        //        if (sret == null)
        //            return null;
        //        return sret.SaleData;
        //    }
        //    catch(Exception ex)
        //    {
        //        return null;
        //    }

        //}

        public bool TranactionCompleted(int dispenserNumber, int nozzleNumber, OpenSale saleData)
        {
            try
            {
                ClientDescriptor proxy = this.messenger.GetClient(EndPointEnum.Proxy);
                SendSaleMessage saleMsg = new SendSaleMessage();
                saleMsg.Transmitter = messenger.MessengerAsClient;
                saleMsg.Reciever = proxy;
                saleMsg.SaleData = saleData;
                messenger.SendMessage(saleMsg);
                return false;
            }
            catch(Exception ex)
            {
                return false;
            }
        }

        public void GetOTPStatus()
        {
            try
            {
                MessageDescriptor msg = new MessageDescriptor();
                msg.MessageKind = MessageKindEnum.GetStatus;
                msg.Route.Reciever = EndPointEnum.OTP;
                msg.Transmitter = this.messenger.MessengerAsClient;
                messenger.SendMessage(msg);
            }
            catch (Exception ex)
            {
                
            }
        }

        public void EnableOTP()
        {
            try
            {
                this.pendingEnableOTP = true;
                MessageDescriptor msg = messenger.CreateMessage(typeof(MessageDescriptor), MessageKindEnum.EnableOTP, this.messenger.MessengerAsClient, EndPointEnum.OTP, 1) as
                   MessageDescriptor;
                msg.ResponseMessageType = typeof(AckMessage);

                //MessageDescriptor msg = new MessageDescriptor();
                //msg.MessageKind = MessageKindEnum.EnableOTP;
                //msg.Route.Reciever = EndPointEnum.OTP;
                //msg.ResponseMessageType = typeof(AckMessage);
                //msg.Transmitter = this.messenger.MessengerAsClient;
                //msg.Route.TrasmitterAddress = System.Net.IPAddress.Parse(this.serverIp);
                //msg.Route.TrasmitterPort = this.serverPort;
                //msg.Route.Transmitter = EndPointEnum.Console;
                //msg.Route.RevieverId = 1;
                messenger.SendMessage(msg);
            }
            catch (Exception ex)
            {
            }
        }

        public void DisableOTP()
        {
            try
            {
                this.pendingDisableOTP = true;
                MessageDescriptor msg = messenger.CreateMessage(typeof(MessageDescriptor), MessageKindEnum.DisbleOTP, this.messenger.MessengerAsClient, EndPointEnum.OTP, 1) as
                    MessageDescriptor;
                msg.ResponseMessageType = typeof(AckMessage);

                //msg.MessageKind = MessageKindEnum.DisbleOTP;
                //msg.Route.Reciever = EndPointEnum.OTP;
                //msg.ResponseMessageType = typeof(AckMessage);
                //msg.Transmitter = this.messenger.MessengerAsClient;
                //msg.Route.TrasmitterAddress = System.Net.IPAddress.Parse(this.serverIp);
                //msg.Route.TrasmitterPort = this.serverPort;
                //msg.Route.Transmitter = EndPointEnum.Console;
                //msg.Route.RevieverId = 1;
                messenger.SendMessage(msg);
            }
            catch (Exception ex)
            {
            }
        }

        private FuelTypeModel[] GetFuelTypes()
        {
            var fts = this.dispensers.SelectMany(d => d.Nozzles).Where(n => this.dispNozzles[n.ParentDispenser].ToArray().
                            Contains(n.NozzleId)).
                        Select(n => n.FuelTypeDescription).Distinct();
            List<FuelTypeModel> fuelTypes = new List<FuelTypeModel>();
            foreach (string desc in fts)
            {
                ASFuel.AutoPay.Common.FuelTypeModel ft = new FuelTypeModel();
                ft.Description = desc;

                VirtualDevices.VirtualNozzle[] nzs = this.dispensers.SelectMany(d => d.Nozzles).Where(n => this.dispNozzles[n.ParentDispenser].ToArray().
                      Contains(n.NozzleId)).
                        Where(n => n.FuelTypeDescription == desc).ToArray();

                List<NozzleTypeModel> nzModels = new List<NozzleTypeModel>();
                foreach (VirtualDevices.VirtualNozzle nz in nzs)
                {
                    ft.FuelCode = nz.FuelCode;
                    NozzleTypeModel nzModel = new NozzleTypeModel();
                    nzModel.NozzleId = nz.NozzleOfficialNumber;
                    nzModel.DispenserId = nz.ParentDispenser.OfficialNumber;
                    nzModel.UnitPrice = nz.CurrentSaleUnitPrice;
                    nzModels.Add(nzModel);
                }
                ft.Nozzles = nzModels.OrderBy(n => n.DispenserId).ThenBy(n => n.NozzleId).ToArray();
                if (ft.Nozzles.Length > 0)
                    fuelTypes.Add(ft);
            }
            return fuelTypes.ToArray();
        }

        private void Messenger_MessageFailed(object sender, MessageEventArgs e)
        {

        }

        private void Messenger_MessageRecieved(object sender, MessageEventArgs e)
        {
            if (e.Message == null)
                return;

            if (e.Message.Route.Reciever == EndPointEnum.Console)
            {
                if (e.Message.MessageKind == MessageKindEnum.GetStatus)
                {
                    StatusResponseMessage res = e.Message as StatusResponseMessage;
                    if (res == null)
                        return;
                    res.Status = StatusEnum.Open;
                }
                else if (e.Message.MessageKind == MessageKindEnum.GetDeviceInfo)
                {
                    GetDeviceInfoResponseMessage msg = e.Message.CreateResponse() as GetDeviceInfoResponseMessage;
                    msg.FuelTypes = this.GetFuelTypes();
                    msg.MessageKind = MessageKindEnum.GetDeviceInfo;
                    messenger.SendMessage(msg);
                }
                else if (e.Message.MessageKind == MessageKindEnum.GetNozzleInfo)
                {
                    MessageDescriptor ms = e.Message as MessageDescriptor;
                    if (ms == null)
                        return;

                    GetNozzleInfoResponseMessage msg = ms.CreateResponse() as GetNozzleInfoResponseMessage;
                    if (msg == null)
                        return;

                    FuelTypeModel[] fuelTypes = this.GetFuelTypes();
                    NozzleTypeModel[] nozzles = fuelTypes.SelectMany(n => n.Nozzles).ToArray();
                    msg.Nozzles = nozzles.Select(n => new NozzleInfo() { DispenserNumber = n.DispenserId, NozzleNumber = n.NozzleId, UnitPrice = n.UnitPrice }).ToArray();
                    foreach (NozzleInfo ni in msg.Nozzles)
                    {
                        var q = dispensers.Where(d => d.OfficialNumber == ni.DispenserNumber);
                        VirtualDevices.VirtualDispenser dispenser = q.Where(d => d.Nozzles.Where(n => n.NozzleOfficialNumber == ni.NozzleNumber).Count() > 0).FirstOrDefault();
                        if (dispenser == null)
                            continue;
                        VirtualDevices.VirtualNozzle vnz = dispenser.Nozzles.Where(n => n.NozzleOfficialNumber == ni.NozzleNumber).FirstOrDefault();
                        ni.RemainingTankVolume = vnz.ConnectedTanks.Sum(t => t.CurrentVolume);
                        if (dispenser.Status == Common.Enumerators.FuelPointStatusEnum.Offline)
                            ni.Status = NozzleStatusEnum.Offline;
                        else if (dispenser.Status == Common.Enumerators.FuelPointStatusEnum.Idle)
                            ni.Status = NozzleStatusEnum.Idle;
                        else
                            ni.Status = NozzleStatusEnum.Working;
                        if (vnz.HasLockedTank)
                            ni.Status = NozzleStatusEnum.Offline;
                    }
                    messenger.SendMessage(msg);
                    //msg.Nozzles
                }
                else if (e.Message.MessageKind == MessageKindEnum.Sale)
                {
                    SendSaleMessage saleMsg = e.Message as SendSaleMessage;
                    if (saleMsg == null)
                        return;
                    if (saleMsg.SaleData == null)
                        return;
                    this.openSales.TryAdd(saleMsg.SaleData.SaleId, saleMsg.SaleData);
                }
                else if (e.Message.GetType() == typeof(AckMessage))
                {
                    AckMessage ack = e.Message as AckMessage;
                    if (ack.SourceMessageKind == MessageKindEnum.EnableOTP && this.pendingEnableOTP)
                    {
                        this.pendingEnableOTP = false;
                        this.OPTStatus = true;
                        if (this.OTPStatusChanged != null)
                            this.OTPStatusChanged(this, new EventArgs());
                    }
                    else if (ack.SourceMessageKind == MessageKindEnum.DisbleOTP && this.pendingDisableOTP)
                    {
                        this.pendingDisableOTP = false;
                        this.OPTStatus = false;
                        if (this.OTPStatusChanged != null)
                            this.OTPStatusChanged(this, new EventArgs());
                    }
                }
            }
            else
            {
                
            }

        }

        private void Messenger_MessageResponsePrepared(object sender, MessageEventArgs e)
        {
            
        }

        //private void Server_DataReceived(object sender, SimpleTCP.Message e)
        //{
        //    BaseMessage[] msgArr = BaseMessage.Deserialize(e.MessageString);
        //    foreach (BaseMessage bm in msgArr)
        //    {
        //        if (bm == null)
        //            continue;
        //        if (bm.MessageType == MessageTypeEnum.GetNextSaleReturn)
        //        {
        //            GetNextSaleReturn saleMsg = bm as GetNextSaleReturn;
        //            OpenSale sale = saleMsg.SaleData;
        //            if (openSales.Keys.Where(s => s == sale.SaleId).Count() > 0)
        //            {
        //                ReturnMessage emsg = new ReturnMessage();
        //                emsg.ErrorCode = 0;
        //                emsg.ErrorDescription = "";
        //                emsg.MessageId = bm.MessageId;
        //                this.messages.TryAdd(emsg.MessageId, emsg);
        //                continue;
        //            }
        //            while (!this.openSales.TryAdd(sale.SaleId, sale))
        //            {
        //                System.Threading.Thread.Sleep(100);
        //            }
        //            ReturnMessage emsg1 = new ReturnMessage();
        //            emsg1.ErrorCode = 0;
        //            emsg1.ErrorDescription = "";
        //            emsg1.MessageId = bm.MessageId;
        //            this.messages.TryAdd(emsg1.MessageId, emsg1);
        //        }
        //        else if (bm.MessageType == MessageTypeEnum.GetMultiNozzleInfo)
        //        {
        //            GetMultiNozzleInfo mni = bm as GetMultiNozzleInfo;
        //            foreach (NozzleInfo ni in mni.Nozzles)
        //            {
        //                var q = dispensers.Where(d => d.OfficialNumber == ni.DispenserNumber);
        //                VirtualDevices.VirtualDispenser dispenser = q.Where(d => d.Nozzles.Where(n => n.NozzleOfficialNumber == ni.NozzleNumber).Count() > 0).FirstOrDefault();
        //                if (dispenser == null)
        //                    continue;
        //                VirtualDevices.VirtualNozzle vnz = dispenser.Nozzles.Where(n => n.NozzleOfficialNumber == ni.NozzleNumber).FirstOrDefault();

        //                ni.UnitPrice = vnz.CurrentSaleUnitPrice;
        //                ni.RemainingTankVolume = vnz.ConnectedTanks.Sum(t => t.CurrentVolume);
        //                if (dispenser.Status == Common.Enumerators.FuelPointStatusEnum.Offline)
        //                    ni.Status = NozzleStatusEnum.Offline;
        //                else if (dispenser.Status == Common.Enumerators.FuelPointStatusEnum.Idle)
        //                    ni.Status = NozzleStatusEnum.Idle;
        //                else
        //                    ni.Status = NozzleStatusEnum.Working;
        //                if (vnz.HasLockedTank)
        //                    ni.Status = NozzleStatusEnum.Offline;

        //                //this.GetNozzleInfo(ni);
        //            }
        //            GetMultiNozzleInfoReturn rni = new GetMultiNozzleInfoReturn();
        //            rni.VendingMachineId = 1;
        //            rni.Nozzles = mni.Nozzles;
        //            rni.MessageId = bm.MessageId;
        //            this.messages.TryAdd(rni.MessageId, rni);
        //        }
        //        else if (bm.MessageType == MessageTypeEnum.GetDevices)
        //        {
        //            var fts = this.dispensers.SelectMany(d => d.Nozzles).Where(n => this.dispNozzles[n.ParentDispenser].ToArray().
        //                    Contains(n.NozzleId)).
        //                Select(n => n.FuelTypeDescription).Distinct();
        //            List<FuelTypeModel> fuelTypes = new List<FuelTypeModel>();
        //            foreach (string desc in fts)
        //            {
        //                ASFuel.AutoPay.Common.FuelTypeModel ft = new FuelTypeModel();
        //                ft.Description = desc;

        //                VirtualDevices.VirtualNozzle[] nzs = this.dispensers.SelectMany(d => d.Nozzles).Where(n => this.dispNozzles[n.ParentDispenser].ToArray().
        //                      Contains(n.NozzleId)).
        //                        Where(n => n.FuelTypeDescription == desc).ToArray();

        //                List<NozzleTypeModel> nzModels = new List<NozzleTypeModel>();
        //                foreach (VirtualDevices.VirtualNozzle nz in nzs)
        //                {
        //                    ft.FuelCode = nz.FuelCode;
        //                    NozzleTypeModel nzModel = new NozzleTypeModel();
        //                    nzModel.NozzleId = nz.NozzleOfficialNumber;
        //                    nzModel.DispenserId = nz.ParentDispenser.OfficialNumber;
        //                    nzModel.UnitPrice = nz.CurrentSaleUnitPrice;
        //                    nzModels.Add(nzModel);
        //                }
        //                ft.Nozzles = nzModels.OrderBy(n => n.DispenserId).ThenBy(n => n.NozzleId).ToArray();
        //                if (ft.Nozzles.Length > 0)
        //                    fuelTypes.Add(ft);
        //            }
        //            GetDevicesReturn msg = new GetDevicesReturn();
        //            msg.VendingMachineId = 1;
        //            msg.FuelTypes = fuelTypes.OrderBy(f => f.Description).ToArray();
        //            msg.MessageId = bm.MessageId;
        //            this.messages.TryAdd(msg.MessageId, msg);
        //        }
        //        else if (bm.MessageType == MessageTypeEnum.GetConsoleStatus)
        //        {
        //            ConsoleStatusMessage msg = bm as ConsoleStatusMessage;
        //            msg.ConsoleStatus = ConsoleStatusEnum.Open;
        //            msg.Direction = DirectionEnum.ConsoleToProxy;
        //            msg.MessageId = bm.MessageId;
        //            this.messages.TryAdd(msg.MessageId, msg);
        //        }
        //        else if(bm.MessageType == MessageTypeEnum.GetOPTStatusReturn)
        //        {
        //            GetOPTStatusReturnMessage gm = bm as GetOPTStatusReturnMessage;
        //            if (gm.Status == VendingMachineStatusEnum.Open)
        //            {
        //                this.OPTStatus = true;
        //                if (this.messages.Where(m => m.Value.MessageType == MessageTypeEnum.EnableOPT).Count() > 0)
        //                {
        //                    BaseMessage rm = this.messages.Where(m => m.Value.MessageType == MessageTypeEnum.EnableOPT && m.Value.VendingMachineId == bm.VendingMachineId).
        //                        Select(m => m.Value).FirstOrDefault();
        //                    this.messages.TryRemove(rm.MessageId, out rm);
        //                    this.OPTStatus = true;
        //                    if (this.OTPStatusChanged != null)
        //                        this.OTPStatusChanged(this, new EventArgs());
        //                }
        //            }
        //            else
        //            {
        //                this.OPTStatus = false;
        //                if (this.messages.Where(m => m.Value.MessageType == MessageTypeEnum.DisableOPT).Count() > 0)
        //                {
        //                    BaseMessage rm = this.messages.Where(m => m.Value.MessageType == MessageTypeEnum.DisableOPT && m.Value.VendingMachineId == bm.VendingMachineId).
        //                        Select(m => m.Value).FirstOrDefault();
        //                    this.messages.TryRemove(rm.MessageId, out rm);
        //                    this.OPTStatus = false;
        //                }
        //            }
        //            if (this.OTPStatusChanged != null)
        //                this.OTPStatusChanged(this, new EventArgs());

        //        }
        //        else if(bm.MessageType == MessageTypeEnum.ReturnMessage)
        //        {


        //            //{
        //            //    BaseMessage rm;
        //            //    this.messages.TryGetValue(bm.MessageId, out rm);
        //            //    if(rm != null && rm.MessageType == MessageTypeEnum.EnableOPT)
        //            //    {
        //            //        this.messages.TryRemove(bm.MessageId, out rm);
        //            //        this.OPTStatus = true;
        //            //        if (this.OTPStatusChanged != null)
        //            //            this.OTPStatusChanged(this, new EventArgs());
        //            //    }
        //            //    else if (rm != null && rm.MessageType == MessageTypeEnum.DisableOPT)
        //            //    {
        //            //        this.messages.TryRemove(bm.MessageId, out rm);
        //            //        this.OPTStatus = false;
        //            //        if (this.OTPStatusChanged != null)
        //            //            this.OTPStatusChanged(this, new EventArgs());
        //            //    }
        //            //}
        //        }
        //        BaseMessage foo = null;
        //        if(this.messages.ContainsKey(bm.MessageId))
        //            this.messages.TryRemove(bm.MessageId, out foo);
        //    }

        //}

        //private void TreadRun()
        //{
        //    while(true)
        //    {
        //        try
        //        {
        //            string message = "";
        //            List<BaseMessage> list = new List<BaseMessage>();
        //            foreach (Guid messageId in this.messages.Keys)
        //            {
        //                BaseMessage msg = this.messages[messageId];
        //                if (msg.Sent)
        //                    continue;
        //                if (message == "")
        //                    message = BaseMessage.SerializeMessage(msg);
        //                else
        //                    message = message + "\r\n" + BaseMessage.SerializeMessage(msg);
        //                list.Add(msg);
        //            }
        //            if (message == "")
        //                continue;
        //            this.client.Connect(this.clientIp, this.clientPort);
        //            this.client.Write(message);
        //            this.client.Disconnect();
        //            foreach (BaseMessage msg in list)
        //            {
        //                msg.Sent = true;
        //                BaseMessage foo;
        //                this.messages.TryRemove(msg.MessageId, out foo);
        //            }
        //            System.Threading.Thread.Sleep(50);
        //        }
        //        catch
        //        {
        //            System.Threading.Thread.Sleep(1000);
        //        }
        //        System.Threading.Thread.Sleep(250);
        //    }
        //}
    }
}
