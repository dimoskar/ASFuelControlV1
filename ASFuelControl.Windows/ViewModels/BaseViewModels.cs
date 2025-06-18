using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ASFuelControl.Windows.ViewModels
{
    public partial class AlertDefinitionViewModel : BaseViewModel<Data.AlertDefinition>
    {
        private Guid alertdefinitionid;
        [PrimaryKey]
        public Guid AlertDefinitionId
        {
            set
            {
                if (this.alertdefinitionid == value)
                    return;
                this.alertdefinitionid = value;
                this.OnPropertyChanged("AlertDefinitionId");
            }
            get { return this.alertdefinitionid; }
        }

        private string name;
        public string Name
        {
            set
            {
                if (this.name == value)
                    return;
                this.name = value;
                this.OnPropertyChanged("Name");
            }
            get { return this.name; }
        }

        private int? alertenumvalue;
        public int? AlertEnumValue
        {
            set
            {
                if (this.alertenumvalue == value)
                    return;
                this.alertenumvalue = value;
                this.OnPropertyChanged("AlertEnumValue");
            }
            get { return this.alertenumvalue; }
        }

        private bool isnozzlealert;
        public bool IsNozzleAlert
        {
            set
            {
                if (this.isnozzlealert == value)
                    return;
                this.isnozzlealert = value;
                this.OnPropertyChanged("IsNozzleAlert");
            }
            get { return this.isnozzlealert; }
        }

        private bool isdispenseralert;
        public bool IsDispenserAlert
        {
            set
            {
                if (this.isdispenseralert == value)
                    return;
                this.isdispenseralert = value;
                this.OnPropertyChanged("IsDispenserAlert");
            }
            get { return this.isdispenseralert; }
        }

        private bool istankalert;
        public bool IsTankAlert
        {
            set
            {
                if (this.istankalert == value)
                    return;
                this.istankalert = value;
                this.OnPropertyChanged("IsTankAlert");
            }
            get { return this.istankalert; }
        }

        private bool isstationalert;
        public bool IsStationAlert
        {
            set
            {
                if (this.isstationalert == value)
                    return;
                this.isstationalert = value;
                this.OnPropertyChanged("IsStationAlert");
            }
            get { return this.isstationalert; }
        }

        private bool lockdevices;
        public bool LockDevices
        {
            set
            {
                if (this.lockdevices == value)
                    return;
                this.lockdevices = value;
                this.OnPropertyChanged("LockDevices");
            }
            get { return this.lockdevices; }
        }

        private string expression;
        public string Expression
        {
            set
            {
                if (this.expression == value)
                    return;
                this.expression = value;
                this.OnPropertyChanged("Expression");
            }
            get { return this.expression; }
        }

        private bool alertisdisabled;
        public bool AlertIsDisabled
        {
            set
            {
                if (this.alertisdisabled == value)
                    return;
                this.alertisdisabled = value;
                this.OnPropertyChanged("AlertIsDisabled");
            }
            get { return this.alertisdisabled; }
        }

        private decimal errorthreshold;
        public decimal ErrorThreshold
        {
            set
            {
                if (this.errorthreshold == value)
                    return;
                this.errorthreshold = value;
                this.OnPropertyChanged("ErrorThreshold");
            }
            get { return this.errorthreshold; }
        }

        private bool resendalerts;
        public bool ResendAlerts
        {
            set
            {
                if (this.resendalerts == value)
                    return;
                this.resendalerts = value;
                this.OnPropertyChanged("ResendAlerts");
            }
            get { return this.resendalerts; }
        }

        private string alerttmessage;
        public string AlerttMessage
        {
            set
            {
                if (this.alerttmessage == value)
                    return;
                this.alerttmessage = value;
                this.OnPropertyChanged("AlerttMessage");
            }
            get { return this.alerttmessage; }
        }

        private bool isgeneric;
        public bool IsGeneric
        {
            set
            {
                if (this.isgeneric == value)
                    return;
                this.isgeneric = value;
                this.OnPropertyChanged("IsGeneric");
            }
            get { return this.isgeneric; }
        }

        private int resendalertsinterval;
        public int ResendAlertsInterval
        {
            set
            {
                if (this.resendalertsinterval == value)
                    return;
                this.resendalertsinterval = value;
                this.OnPropertyChanged("ResendAlertsInterval");
            }
            get { return this.resendalertsinterval; }
        }

        private bool autoresolve;
        public bool AutoResolve
        {
            set
            {
                if (this.autoresolve == value)
                    return;
                this.autoresolve = value;
                this.OnPropertyChanged("AutoResolve");
            }
            get { return this.autoresolve; }
        }

        private string methodforresolve;
        public string MethodForResolve
        {
            set
            {
                if (this.methodforresolve == value)
                    return;
                this.methodforresolve = value;
                this.OnPropertyChanged("MethodForResolve");
            }
            get { return this.methodforresolve; }
        }

    }

    public partial class ApplicationUserViewModel : BaseViewModel<Data.ApplicationUser>
    {
        private Guid applicationuserid;
        [PrimaryKey]
        public Guid ApplicationUserId
        {
            set
            {
                if (this.applicationuserid == value)
                    return;
                this.applicationuserid = value;
                this.OnPropertyChanged("ApplicationUserId");
            }
            get { return this.applicationuserid; }
        }

        private string username;
        public string UserName
        {
            set
            {
                if (this.username == value)
                    return;
                this.username = value;
                this.OnPropertyChanged("UserName");
            }
            get { return this.username; }
        }

        private string password;
        public string Password
        {
            set
            {
                if (this.password == value)
                    return;
                this.password = value;
                this.OnPropertyChanged("Password");
            }
            get { return this.password; }
        }

        private int userlevel;
        public int UserLevel
        {
            set
            {
                if (this.userlevel == value)
                    return;
                this.userlevel = value;
                this.OnPropertyChanged("UserLevel");
            }
            get { return this.userlevel; }
        }

        private string passwordencrypted;
        public string PasswordEncrypted
        {
            set
            {
                if (this.passwordencrypted == value)
                    return;
                this.passwordencrypted = value;
                this.OnPropertyChanged("PasswordEncrypted");
            }
            get { return this.passwordencrypted; }
        }

    }

    public partial class ApplicationUserLoggonViewModel : BaseViewModel<Data.ApplicationUserLoggon>
    {
        private Guid applicationuserloggonid;
        [PrimaryKey]
        public Guid ApplicationUserLoggonId
        {
            set
            {
                if (this.applicationuserloggonid == value)
                    return;
                this.applicationuserloggonid = value;
                this.OnPropertyChanged("ApplicationUserLoggonId");
            }
            get { return this.applicationuserloggonid; }
        }

        private Guid applicationuserid;
        public Guid ApplicationUserId
        {
            set
            {
                if (this.applicationuserid == value)
                    return;
                this.applicationuserid = value;
                this.OnPropertyChanged("ApplicationUserId");
            }
            get { return this.applicationuserid; }
        }

        private DateTime loggontime;
        public DateTime LoggonTime
        {
            set
            {
                if (this.loggontime == value)
                    return;
                this.loggontime = value;
                this.OnPropertyChanged("LoggonTime");
            }
            get { return this.loggontime; }
        }

        private DateTime? logofftime;
        public DateTime? LogoffTime
        {
            set
            {
                if (this.logofftime == value)
                    return;
                this.logofftime = value;
                this.OnPropertyChanged("LogoffTime");
            }
            get { return this.logofftime; }
        }

    }

    public partial class AtgProbeProtocolViewModel : BaseViewModel<Data.AtgProbeProtocol>
    {
        private Guid atgprobeprotocolid;
        [PrimaryKey]
        public Guid AtgProbeProtocolId
        {
            set
            {
                if (this.atgprobeprotocolid == value)
                    return;
                this.atgprobeprotocolid = value;
                this.OnPropertyChanged("AtgProbeProtocolId");
            }
            get { return this.atgprobeprotocolid; }
        }

        private string protocolname;
        public string ProtocolName
        {
            set
            {
                if (this.protocolname == value)
                    return;
                this.protocolname = value;
                this.OnPropertyChanged("ProtocolName");
            }
            get { return this.protocolname; }
        }

        private int enumeratorvalue;
        public int EnumeratorValue
        {
            set
            {
                if (this.enumeratorvalue == value)
                    return;
                this.enumeratorvalue = value;
                this.OnPropertyChanged("EnumeratorValue");
            }
            get { return this.enumeratorvalue; }
        }

    }

    public partial class AtgProbeTypeViewModel : BaseViewModel<Data.AtgProbeType>
    {
        private Guid atgprobetypeid;
        [PrimaryKey]
        public Guid AtgProbeTypeId
        {
            set
            {
                if (this.atgprobetypeid == value)
                    return;
                this.atgprobetypeid = value;
                this.OnPropertyChanged("AtgProbeTypeId");
            }
            get { return this.atgprobetypeid; }
        }

        private Guid atgprobeprotocolid;
        public Guid AtgProbeProtocolId
        {
            set
            {
                if (this.atgprobeprotocolid == value)
                    return;
                this.atgprobeprotocolid = value;
                this.OnPropertyChanged("AtgProbeProtocolId");
            }
            get { return this.atgprobeprotocolid; }
        }

        private string brandname;
        public string BrandName
        {
            set
            {
                if (this.brandname == value)
                    return;
                this.brandname = value;
                this.OnPropertyChanged("BrandName");
            }
            get { return this.brandname; }
        }

    }

    public partial class BalanceViewModel : BaseViewModel<Data.Balance>
    {
        private Guid balanceid;
        [PrimaryKey]
        public Guid BalanceId
        {
            set
            {
                if (this.balanceid == value)
                    return;
                this.balanceid = value;
                this.OnPropertyChanged("BalanceId");
            }
            get { return this.balanceid; }
        }

        private DateTime startdate;
        public DateTime StartDate
        {
            set
            {
                if (this.startdate == value)
                    return;
                this.startdate = value;
                this.OnPropertyChanged("StartDate");
            }
            get { return this.startdate; }
        }

        private DateTime enddate;
        public DateTime EndDate
        {
            set
            {
                if (this.enddate == value)
                    return;
                this.enddate = value;
                this.OnPropertyChanged("EndDate");
            }
            get { return this.enddate; }
        }

        private Guid? lastsale;
        public Guid? LastSale
        {
            set
            {
                if (this.lastsale == value)
                    return;
                this.lastsale = value;
                this.OnPropertyChanged("LastSale");
            }
            get { return this.lastsale; }
        }

        private Guid? lastfilling;
        public Guid? LastFilling
        {
            set
            {
                if (this.lastfilling == value)
                    return;
                this.lastfilling = value;
                this.OnPropertyChanged("LastFilling");
            }
            get { return this.lastfilling; }
        }

        private string balancetext;
        public string BalanceText
        {
            set
            {
                if (this.balancetext == value)
                    return;
                this.balancetext = value;
                this.OnPropertyChanged("BalanceText");
            }
            get { return this.balancetext; }
        }

        private Guid applicationuserid;
        public Guid ApplicationUserId
        {
            set
            {
                if (this.applicationuserid == value)
                    return;
                this.applicationuserid = value;
                this.OnPropertyChanged("ApplicationUserId");
            }
            get { return this.applicationuserid; }
        }

        private DateTime? sentdatetime;
        public DateTime? SentDateTime
        {
            set
            {
                if (this.sentdatetime == value)
                    return;
                this.sentdatetime = value;
                this.OnPropertyChanged("SentDateTime");
            }
            get { return this.sentdatetime; }
        }

        private string responsecode;
        public string ResponseCode
        {
            set
            {
                if (this.responsecode == value)
                    return;
                this.responsecode = value;
                this.OnPropertyChanged("ResponseCode");
            }
            get { return this.responsecode; }
        }

        private DateTime? printdate;
        public DateTime? PrintDate
        {
            set
            {
                if (this.printdate == value)
                    return;
                this.printdate = value;
                this.OnPropertyChanged("PrintDate");
            }
            get { return this.printdate; }
        }

        private string documentsign;
        public string DocumentSign
        {
            set
            {
                if (this.documentsign == value)
                    return;
                this.documentsign = value;
                this.OnPropertyChanged("DocumentSign");
            }
            get { return this.documentsign; }
        }

    }

    public partial class ChangeLogViewModel : BaseViewModel<Data.ChangeLog>
    {
        private Guid changelogid;
        [PrimaryKey]
        public Guid ChangeLogId
        {
            set
            {
                if (this.changelogid == value)
                    return;
                this.changelogid = value;
                this.OnPropertyChanged("ChangeLogId");
            }
            get { return this.changelogid; }
        }

        private DateTime datetimestamp;
        public DateTime DateTimeStamp
        {
            set
            {
                if (this.datetimestamp == value)
                    return;
                this.datetimestamp = value;
                this.OnPropertyChanged("DateTimeStamp");
            }
            get { return this.datetimestamp; }
        }

        private string tablename;
        public string TableName
        {
            set
            {
                if (this.tablename == value)
                    return;
                this.tablename = value;
                this.OnPropertyChanged("TableName");
            }
            get { return this.tablename; }
        }

        private string columnname;
        public string ColumnName
        {
            set
            {
                if (this.columnname == value)
                    return;
                this.columnname = value;
                this.OnPropertyChanged("ColumnName");
            }
            get { return this.columnname; }
        }

        private string additionaldescription;
        public string AdditionalDescription
        {
            set
            {
                if (this.additionaldescription == value)
                    return;
                this.additionaldescription = value;
                this.OnPropertyChanged("AdditionalDescription");
            }
            get { return this.additionaldescription; }
        }

        private string oldvalue;
        public string OldValue
        {
            set
            {
                if (this.oldvalue == value)
                    return;
                this.oldvalue = value;
                this.OnPropertyChanged("OldValue");
            }
            get { return this.oldvalue; }
        }

        private string newvalue;
        public string NewValue
        {
            set
            {
                if (this.newvalue == value)
                    return;
                this.newvalue = value;
                this.OnPropertyChanged("NewValue");
            }
            get { return this.newvalue; }
        }

        private string applicationusername;
        public string ApplicationUserName
        {
            set
            {
                if (this.applicationusername == value)
                    return;
                this.applicationusername = value;
                this.OnPropertyChanged("ApplicationUserName");
            }
            get { return this.applicationusername; }
        }

        private string primarykey;
        public string PrimaryKey
        {
            set
            {
                if (this.primarykey == value)
                    return;
                this.primarykey = value;
                this.OnPropertyChanged("PrimaryKey");
            }
            get { return this.primarykey; }
        }

    }

    public partial class CommunicationControllerViewModel : BaseViewModel<Data.CommunicationController>
    {
        private Guid communicationcontrollerid;
        [PrimaryKey]
        public Guid CommunicationControllerId
        {
            set
            {
                if (this.communicationcontrollerid == value)
                    return;
                this.communicationcontrollerid = value;
                this.OnPropertyChanged("CommunicationControllerId");
            }
            get { return this.communicationcontrollerid; }
        }

        private string name;
        public string Name
        {
            set
            {
                if (this.name == value)
                    return;
                this.name = value;
                this.OnPropertyChanged("Name");
            }
            get { return this.name; }
        }

        private string communicationport;
        public string CommunicationPort
        {
            set
            {
                if (this.communicationport == value)
                    return;
                this.communicationport = value;
                this.OnPropertyChanged("CommunicationPort");
            }
            get { return this.communicationport; }
        }

        private int? communicationprotocol;
        public int? CommunicationProtocol
        {
            set
            {
                if (this.communicationprotocol == value)
                    return;
                this.communicationprotocol = value;
                this.OnPropertyChanged("CommunicationProtocol");
            }
            get { return this.communicationprotocol; }
        }

        private string controllerassembly;
        public string ControllerAssembly
        {
            set
            {
                if (this.controllerassembly == value)
                    return;
                this.controllerassembly = value;
                this.OnPropertyChanged("ControllerAssembly");
            }
            get { return this.controllerassembly; }
        }

        private bool? euromatenabled;
        public bool? EuromatEnabled
        {
            set
            {
                if (this.euromatenabled == value)
                    return;
                this.euromatenabled = value;
                this.OnPropertyChanged("EuromatEnabled");
            }
            get { return this.euromatenabled; }
        }

        private string euromatip;
        public string EuromatIp
        {
            set
            {
                if (this.euromatip == value)
                    return;
                this.euromatip = value;
                this.OnPropertyChanged("EuromatIp");
            }
            get { return this.euromatip; }
        }

        private int? euromatport;
        public int? EuromatPort
        {
            set
            {
                if (this.euromatport == value)
                    return;
                this.euromatport = value;
                this.OnPropertyChanged("EuromatPort");
            }
            get { return this.euromatport; }
        }

    }

    public partial class DeviceSettingViewModel : BaseViewModel<Data.DeviceSetting>
    {
        private Guid devicesettingid;
        [PrimaryKey]
        public Guid DeviceSettingId
        {
            set
            {
                if (this.devicesettingid == value)
                    return;
                this.devicesettingid = value;
                this.OnPropertyChanged("DeviceSettingId");
            }
            get { return this.devicesettingid; }
        }

        private Guid deviceid;
        public Guid DeviceId
        {
            set
            {
                if (this.deviceid == value)
                    return;
                this.deviceid = value;
                this.OnPropertyChanged("DeviceId");
            }
            get { return this.deviceid; }
        }

        private string devicetype;
        public string DeviceType
        {
            set
            {
                if (this.devicetype == value)
                    return;
                this.devicetype = value;
                this.OnPropertyChanged("DeviceType");
            }
            get { return this.devicetype; }
        }

        private string settingkey;
        public string SettingKey
        {
            set
            {
                if (this.settingkey == value)
                    return;
                this.settingkey = value;
                this.OnPropertyChanged("SettingKey");
            }
            get { return this.settingkey; }
        }

        private string settingvalue;
        public string SettingValue
        {
            set
            {
                if (this.settingvalue == value)
                    return;
                this.settingvalue = value;
                this.OnPropertyChanged("SettingValue");
            }
            get { return this.settingvalue; }
        }

        private string description;
        public string Description
        {
            set
            {
                if (this.description == value)
                    return;
                this.description = value;
                this.OnPropertyChanged("Description");
            }
            get { return this.description; }
        }

        private bool isserialnumber;
        public bool IsSerialNumber
        {
            set
            {
                if (this.isserialnumber == value)
                    return;
                this.isserialnumber = value;
                this.OnPropertyChanged("IsSerialNumber");
            }
            get { return this.isserialnumber; }
        }

    }

    public partial class DispenserViewModel : BaseViewModel<Data.Dispenser>
    {
        private Guid dispenserid;
        [PrimaryKey]
        public Guid DispenserId
        {
            set
            {
                if (this.dispenserid == value)
                    return;
                this.dispenserid = value;
                this.OnPropertyChanged("DispenserId");
            }
            get { return this.dispenserid; }
        }

        private int physicalstate;
        public int PhysicalState
        {
            set
            {
                if (this.physicalstate == value)
                    return;
                this.physicalstate = value;
                this.OnPropertyChanged("PhysicalState");
            }
            get { return this.physicalstate; }
        }

        private bool isvalid;
        public bool IsValid
        {
            set
            {
                if (this.isvalid == value)
                    return;
                this.isvalid = value;
                this.OnPropertyChanged("IsValid");
            }
            get { return this.isvalid; }
        }

        private DateTime? invalidationdate;
        public DateTime? InValidationDate
        {
            set
            {
                if (this.invalidationdate == value)
                    return;
                this.invalidationdate = value;
                this.OnPropertyChanged("InValidationDate");
            }
            get { return this.invalidationdate; }
        }

        private Guid dispensertypeid;
        public Guid DispenserTypeId
        {
            set
            {
                if (this.dispensertypeid == value)
                    return;
                this.dispensertypeid = value;
                this.OnPropertyChanged("DispenserTypeId");
            }
            get { return this.dispensertypeid; }
        }

        private Guid communicationcontrollerid;
        public Guid CommunicationControllerId
        {
            set
            {
                if (this.communicationcontrollerid == value)
                    return;
                this.communicationcontrollerid = value;
                this.OnPropertyChanged("CommunicationControllerId");
            }
            get { return this.communicationcontrollerid; }
        }

        private int channel;
        public int Channel
        {
            set
            {
                if (this.channel == value)
                    return;
                this.channel = value;
                this.OnPropertyChanged("Channel");
            }
            get { return this.channel; }
        }

        private int physicaladdress;
        public int PhysicalAddress
        {
            set
            {
                if (this.physicaladdress == value)
                    return;
                this.physicaladdress = value;
                this.OnPropertyChanged("PhysicalAddress");
            }
            get { return this.physicaladdress; }
        }

        private int dispensernumber;
        public int DispenserNumber
        {
            set
            {
                if (this.dispensernumber == value)
                    return;
                this.dispensernumber = value;
                this.OnPropertyChanged("DispenserNumber");
            }
            get { return this.dispensernumber; }
        }

        private string pumpserialnumber;
        public string PumpSerialNumber
        {
            set
            {
                if (this.pumpserialnumber == value)
                    return;
                this.pumpserialnumber = value;
                this.OnPropertyChanged("PumpSerialNumber");
            }
            get { return this.pumpserialnumber; }
        }

        private int officialpumpnumber;
        public int OfficialPumpNumber
        {
            set
            {
                if (this.officialpumpnumber == value)
                    return;
                this.officialpumpnumber = value;
                this.OnPropertyChanged("OfficialPumpNumber");
            }
            get { return this.officialpumpnumber; }
        }

        private int? decimalplaces;
        public int? DecimalPlaces
        {
            set
            {
                if (this.decimalplaces == value)
                    return;
                this.decimalplaces = value;
                this.OnPropertyChanged("DecimalPlaces");
            }
            get { return this.decimalplaces; }
        }

        private int? unitpricedecimalplaces;
        public int? UnitPriceDecimalPlaces
        {
            set
            {
                if (this.unitpricedecimalplaces == value)
                    return;
                this.unitpricedecimalplaces = value;
                this.OnPropertyChanged("UnitPriceDecimalPlaces");
            }
            get { return this.unitpricedecimalplaces; }
        }

        private int? volumedecimalplaces;
        public int? VolumeDecimalPlaces
        {
            set
            {
                if (this.volumedecimalplaces == value)
                    return;
                this.volumedecimalplaces = value;
                this.OnPropertyChanged("VolumeDecimalPlaces");
            }
            get { return this.volumedecimalplaces; }
        }

    }

    public partial class DispenserProtocolViewModel : BaseViewModel<Data.DispenserProtocol>
    {
        private Guid dispenserprotocolid;
        [PrimaryKey]
        public Guid DispenserProtocolId
        {
            set
            {
                if (this.dispenserprotocolid == value)
                    return;
                this.dispenserprotocolid = value;
                this.OnPropertyChanged("DispenserProtocolId");
            }
            get { return this.dispenserprotocolid; }
        }

        private string protocolname;
        public string ProtocolName
        {
            set
            {
                if (this.protocolname == value)
                    return;
                this.protocolname = value;
                this.OnPropertyChanged("ProtocolName");
            }
            get { return this.protocolname; }
        }

        private int enumeratorvalue;
        public int EnumeratorValue
        {
            set
            {
                if (this.enumeratorvalue == value)
                    return;
                this.enumeratorvalue = value;
                this.OnPropertyChanged("EnumeratorValue");
            }
            get { return this.enumeratorvalue; }
        }

    }

    public partial class DispenserSettingViewModel : BaseViewModel<Data.DispenserSetting>
    {
        private Guid dispensersettingid;
        [PrimaryKey]
        public Guid DispenserSettingId
        {
            set
            {
                if (this.dispensersettingid == value)
                    return;
                this.dispensersettingid = value;
                this.OnPropertyChanged("DispenserSettingId");
            }
            get { return this.dispensersettingid; }
        }

        private Guid dispenserid;
        public Guid DispenserId
        {
            set
            {
                if (this.dispenserid == value)
                    return;
                this.dispenserid = value;
                this.OnPropertyChanged("DispenserId");
            }
            get { return this.dispenserid; }
        }

        private string settingkey;
        public string SettingKey
        {
            set
            {
                if (this.settingkey == value)
                    return;
                this.settingkey = value;
                this.OnPropertyChanged("SettingKey");
            }
            get { return this.settingkey; }
        }

        private string settingvalue;
        public string SettingValue
        {
            set
            {
                if (this.settingvalue == value)
                    return;
                this.settingvalue = value;
                this.OnPropertyChanged("SettingValue");
            }
            get { return this.settingvalue; }
        }

        private string description;
        public string Description
        {
            set
            {
                if (this.description == value)
                    return;
                this.description = value;
                this.OnPropertyChanged("Description");
            }
            get { return this.description; }
        }

        private Guid? nozzleid;
        public Guid? NozzleId
        {
            set
            {
                if (this.nozzleid == value)
                    return;
                this.nozzleid = value;
                this.OnPropertyChanged("NozzleId");
            }
            get { return this.nozzleid; }
        }

    }

    public partial class DispenserTypeViewModel : BaseViewModel<Data.DispenserType>
    {
        private Guid dispensertypeid;
        [PrimaryKey]
        public Guid DispenserTypeId
        {
            set
            {
                if (this.dispensertypeid == value)
                    return;
                this.dispensertypeid = value;
                this.OnPropertyChanged("DispenserTypeId");
            }
            get { return this.dispensertypeid; }
        }

        private Guid dispenserprotocolid;
        public Guid DispenserProtocolId
        {
            set
            {
                if (this.dispenserprotocolid == value)
                    return;
                this.dispenserprotocolid = value;
                this.OnPropertyChanged("DispenserProtocolId");
            }
            get { return this.dispenserprotocolid; }
        }

        private string brandname;
        public string BrandName
        {
            set
            {
                if (this.brandname == value)
                    return;
                this.brandname = value;
                this.OnPropertyChanged("BrandName");
            }
            get { return this.brandname; }
        }

    }

    public partial class FinancialTransactionViewModel : BaseViewModel<Data.FinancialTransaction>
    {
        private Guid financialtransactionid;
        [PrimaryKey]
        public Guid FinancialTransactionId
        {
            set
            {
                if (this.financialtransactionid == value)
                    return;
                this.financialtransactionid = value;
                this.OnPropertyChanged("FinancialTransactionId");
            }
            get { return this.financialtransactionid; }
        }

        private Guid invoiceid;
        public Guid InvoiceId
        {
            set
            {
                if (this.invoiceid == value)
                    return;
                this.invoiceid = value;
                this.OnPropertyChanged("InvoiceId");
            }
            get { return this.invoiceid; }
        }

        private DateTime transactiondate;
        public DateTime TransactionDate
        {
            set
            {
                if (this.transactiondate == value)
                    return;
                this.transactiondate = value;
                this.OnPropertyChanged("TransactionDate");
            }
            get { return this.transactiondate; }
        }

        private decimal amount;
        public decimal Amount
        {
            set
            {
                if (this.amount == value)
                    return;
                this.amount = value;
                this.OnPropertyChanged("Amount");
            }
            get { return this.amount; }
        }

        private int transactiontype;
        public int TransactionTYpe
        {
            set
            {
                if (this.transactiontype == value)
                    return;
                this.transactiontype = value;
                this.OnPropertyChanged("TransactionTYpe");
            }
            get { return this.transactiontype; }
        }

        private string notes;
        public string Notes
        {
            set
            {
                if (this.notes == value)
                    return;
                this.notes = value;
                this.OnPropertyChanged("Notes");
            }
            get { return this.notes; }
        }

    }

    public partial class FinTransactionViewModel : BaseViewModel<Data.FinTransaction>
    {
        private Guid fintransactionid;
        [PrimaryKey]
        public Guid FinTransactionId
        {
            set
            {
                if (this.fintransactionid == value)
                    return;
                this.fintransactionid = value;
                this.OnPropertyChanged("FinTransactionId");
            }
            get { return this.fintransactionid; }
        }

        private Guid applicationuserid;
        public Guid ApplicationUserId
        {
            set
            {
                if (this.applicationuserid == value)
                    return;
                this.applicationuserid = value;
                this.OnPropertyChanged("ApplicationUserId");
            }
            get { return this.applicationuserid; }
        }

        private Guid? traderid;
        public Guid? TraderId
        {
            set
            {
                if (this.traderid == value)
                    return;
                this.traderid = value;
                this.OnPropertyChanged("TraderId");
            }
            get { return this.traderid; }
        }

        private Guid? invoiceid;
        public Guid? InvoiceId
        {
            set
            {
                if (this.invoiceid == value)
                    return;
                this.invoiceid = value;
                this.OnPropertyChanged("InvoiceId");
            }
            get { return this.invoiceid; }
        }

        private DateTime transactiondate;
        public DateTime TransactionDate
        {
            set
            {
                if (this.transactiondate == value)
                    return;
                this.transactiondate = value;
                this.OnPropertyChanged("TransactionDate");
            }
            get { return this.transactiondate; }
        }

        private decimal amount;
        public decimal Amount
        {
            set
            {
                if (this.amount == value)
                    return;
                this.amount = value;
                this.OnPropertyChanged("Amount");
            }
            get { return this.amount; }
        }

        private decimal creditamount;
        public decimal CreditAmount
        {
            set
            {
                if (this.creditamount == value)
                    return;
                this.creditamount = value;
                this.OnPropertyChanged("CreditAmount");
            }
            get { return this.creditamount; }
        }

        private decimal debitamount;
        public decimal DebitAmount
        {
            set
            {
                if (this.debitamount == value)
                    return;
                this.debitamount = value;
                this.OnPropertyChanged("DebitAmount");
            }
            get { return this.debitamount; }
        }

        private int transactiontype;
        public int TransactionType
        {
            set
            {
                if (this.transactiontype == value)
                    return;
                this.transactiontype = value;
                this.OnPropertyChanged("TransactionType");
            }
            get { return this.transactiontype; }
        }

        private string notes;
        public string Notes
        {
            set
            {
                if (this.notes == value)
                    return;
                this.notes = value;
                this.OnPropertyChanged("Notes");
            }
            get { return this.notes; }
        }

    }

    public partial class FleetManagerDispenserViewModel : BaseViewModel<Data.FleetManagerDispenser>
    {
        private Guid fleetmanagerdispenserid;
        [PrimaryKey]
        public Guid FleetManagerDispenserId
        {
            set
            {
                if (this.fleetmanagerdispenserid == value)
                    return;
                this.fleetmanagerdispenserid = value;
                this.OnPropertyChanged("FleetManagerDispenserId");
            }
            get { return this.fleetmanagerdispenserid; }
        }

        private Guid fleetmanagmentcotrollerid;
        public Guid FleetManagmentCotrollerId
        {
            set
            {
                if (this.fleetmanagmentcotrollerid == value)
                    return;
                this.fleetmanagmentcotrollerid = value;
                this.OnPropertyChanged("FleetManagmentCotrollerId");
            }
            get { return this.fleetmanagmentcotrollerid; }
        }

        private Guid dispenserid;
        public Guid DispenserId
        {
            set
            {
                if (this.dispenserid == value)
                    return;
                this.dispenserid = value;
                this.OnPropertyChanged("DispenserId");
            }
            get { return this.dispenserid; }
        }

        private Guid invoicetypeid;
        public Guid InvoiceTypeId
        {
            set
            {
                if (this.invoicetypeid == value)
                    return;
                this.invoicetypeid = value;
                this.OnPropertyChanged("InvoiceTypeId");
            }
            get { return this.invoicetypeid; }
        }

    }

    public partial class FleetManagmentCotrollerViewModel : BaseViewModel<Data.FleetManagmentCotroller>
    {
        private Guid fleetmanagmentcotrollerid;
        [PrimaryKey]
        public Guid FleetManagmentCotrollerId
        {
            set
            {
                if (this.fleetmanagmentcotrollerid == value)
                    return;
                this.fleetmanagmentcotrollerid = value;
                this.OnPropertyChanged("FleetManagmentCotrollerId");
            }
            get { return this.fleetmanagmentcotrollerid; }
        }

        private string comport;
        public string ComPort
        {
            set
            {
                if (this.comport == value)
                    return;
                this.comport = value;
                this.OnPropertyChanged("ComPort");
            }
            get { return this.comport; }
        }

        private int baudrate;
        public int BaudRate
        {
            set
            {
                if (this.baudrate == value)
                    return;
                this.baudrate = value;
                this.OnPropertyChanged("BaudRate");
            }
            get { return this.baudrate; }
        }

        private int parity;
        public int Parity
        {
            set
            {
                if (this.parity == value)
                    return;
                this.parity = value;
                this.OnPropertyChanged("Parity");
            }
            get { return this.parity; }
        }

        private int databits;
        public int DataBits
        {
            set
            {
                if (this.databits == value)
                    return;
                this.databits = value;
                this.OnPropertyChanged("DataBits");
            }
            get { return this.databits; }
        }

        private int stopbits;
        public int StopBits
        {
            set
            {
                if (this.stopbits == value)
                    return;
                this.stopbits = value;
                this.OnPropertyChanged("StopBits");
            }
            get { return this.stopbits; }
        }

        private int? controlertype;
        public int? ControlerType
        {
            set
            {
                if (this.controlertype == value)
                    return;
                this.controlertype = value;
                this.OnPropertyChanged("ControlerType");
            }
            get { return this.controlertype; }
        }

        private string deviceip;
        public string DeviceIp
        {
            set
            {
                if (this.deviceip == value)
                    return;
                this.deviceip = value;
                this.OnPropertyChanged("DeviceIp");
            }
            get { return this.deviceip; }
        }

        private int? deviceport;
        public int? DevicePort
        {
            set
            {
                if (this.deviceport == value)
                    return;
                this.deviceport = value;
                this.OnPropertyChanged("DevicePort");
            }
            get { return this.deviceport; }
        }

        private int? deviceindex;
        public int? DeviceIndex
        {
            set
            {
                if (this.deviceindex == value)
                    return;
                this.deviceindex = value;
                this.OnPropertyChanged("DeviceIndex");
            }
            get { return this.deviceindex; }
        }

    }

    public partial class FleetManagmentScheduleViewModel : BaseViewModel<Data.FleetManagmentSchedule>
    {
        private Guid fleetmanagmentscheduleid;
        [PrimaryKey]
        public Guid FleetManagmentScheduleId
        {
            set
            {
                if (this.fleetmanagmentscheduleid == value)
                    return;
                this.fleetmanagmentscheduleid = value;
                this.OnPropertyChanged("FleetManagmentScheduleId");
            }
            get { return this.fleetmanagmentscheduleid; }
        }

        private Guid fleetmanagerdispenserid;
        public Guid FleetManagerDispenserId
        {
            set
            {
                if (this.fleetmanagerdispenserid == value)
                    return;
                this.fleetmanagerdispenserid = value;
                this.OnPropertyChanged("FleetManagerDispenserId");
            }
            get { return this.fleetmanagerdispenserid; }
        }

        private int timefrom;
        public int TimeFrom
        {
            set
            {
                if (this.timefrom == value)
                    return;
                this.timefrom = value;
                this.OnPropertyChanged("TimeFrom");
            }
            get { return this.timefrom; }
        }

        private int timeto;
        public int TimeTo
        {
            set
            {
                if (this.timeto == value)
                    return;
                this.timeto = value;
                this.OnPropertyChanged("TimeTo");
            }
            get { return this.timeto; }
        }

        private int daymask;
        public int DayMask
        {
            set
            {
                if (this.daymask == value)
                    return;
                this.daymask = value;
                this.OnPropertyChanged("DayMask");
            }
            get { return this.daymask; }
        }

    }

    public partial class FuelTypeViewModel : BaseViewModel<Data.FuelType>
    {
        private Guid fueltypeid;
        [PrimaryKey]
        public Guid FuelTypeId
        {
            set
            {
                if (this.fueltypeid == value)
                    return;
                this.fueltypeid = value;
                this.OnPropertyChanged("FuelTypeId");
            }
            get { return this.fueltypeid; }
        }

        private string name;
        public string Name
        {
            set
            {
                if (this.name == value)
                    return;
                this.name = value;
                this.OnPropertyChanged("Name");
            }
            get { return this.name; }
        }

        private string code;
        public string Code
        {
            set
            {
                if (this.code == value)
                    return;
                this.code = value;
                this.OnPropertyChanged("Code");
            }
            get { return this.code; }
        }

        private int? color;
        public int? Color
        {
            set
            {
                if (this.color == value)
                    return;
                this.color = value;
                this.OnPropertyChanged("Color");
            }
            get { return this.color; }
        }

        private decimal thermalcoeficient;
        public decimal ThermalCoeficient
        {
            set
            {
                if (this.thermalcoeficient == value)
                    return;
                this.thermalcoeficient = value;
                this.OnPropertyChanged("ThermalCoeficient");
            }
            get { return this.thermalcoeficient; }
        }

        private int enumeratorvalue;
        public int EnumeratorValue
        {
            set
            {
                if (this.enumeratorvalue == value)
                    return;
                this.enumeratorvalue = value;
                this.OnPropertyChanged("EnumeratorValue");
            }
            get { return this.enumeratorvalue; }
        }

        private decimal basedensity;
        public decimal BaseDensity
        {
            set
            {
                if (this.basedensity == value)
                    return;
                this.basedensity = value;
                this.OnPropertyChanged("BaseDensity");
            }
            get { return this.basedensity; }
        }

        private bool? supportsSupplyNumber;
        public bool? SupportsSupplyNumber
        {
            set
            {
                if (this.supportsSupplyNumber == value)
                    return;
                this.supportsSupplyNumber = value;
                this.OnPropertyChanged("SupportsSupplyNumber");
            }
            get { return this.supportsSupplyNumber; }
        }
    }

    public partial class FuelTypePriceViewModel : BaseViewModel<Data.FuelTypePrice>
    {
        private Guid fueltypepriceid;
        [PrimaryKey]
        public Guid FuelTypePriceId
        {
            set
            {
                if (this.fueltypepriceid == value)
                    return;
                this.fueltypepriceid = value;
                this.OnPropertyChanged("FuelTypePriceId");
            }
            get { return this.fueltypepriceid; }
        }

        private Guid fueltypeid;
        public Guid FuelTypeId
        {
            set
            {
                if (this.fueltypeid == value)
                    return;
                this.fueltypeid = value;
                this.OnPropertyChanged("FuelTypeId");
            }
            get { return this.fueltypeid; }
        }

        private decimal price;
        public decimal Price
        {
            set
            {
                if (this.price == value)
                    return;
                this.price = value;
                this.OnPropertyChanged("Price");
            }
            get { return this.price; }
        }

        private DateTime changedate;
        public DateTime ChangeDate
        {
            set
            {
                if (this.changedate == value)
                    return;
                this.changedate = value;
                this.OnPropertyChanged("ChangeDate");
            }
            get { return this.changedate; }
        }

        private DateTime? sentdatetime;
        public DateTime? SentDateTime
        {
            set
            {
                if (this.sentdatetime == value)
                    return;
                this.sentdatetime = value;
                this.OnPropertyChanged("SentDateTime");
            }
            get { return this.sentdatetime; }
        }

        private string responsecode;
        public string ResponseCode
        {
            set
            {
                if (this.responsecode == value)
                    return;
                this.responsecode = value;
                this.OnPropertyChanged("ResponseCode");
            }
            get { return this.responsecode; }
        }

    }

    public partial class InvoiceViewModel : BaseViewModel<Data.Invoice>
    {
        private Guid invoiceid;
        [PrimaryKey]
        public Guid InvoiceId
        {
            set
            {
                if (this.invoiceid == value)
                    return;
                this.invoiceid = value;
                this.OnPropertyChanged("InvoiceId");
            }
            get { return this.invoiceid; }
        }

        private Guid? traderid;
        public Guid? TraderId
        {
            set
            {
                if (this.traderid == value)
                    return;
                this.traderid = value;
                this.OnPropertyChanged("TraderId");
            }
            get { return this.traderid; }
        }

        private int number;
        public int Number
        {
            set
            {
                if (this.number == value)
                    return;
                this.number = value;
                this.OnPropertyChanged("Number");
            }
            get { return this.number; }
        }

        private string vehicleplatenumber;
        public string VehiclePlateNumber
        {
            set
            {
                if (this.vehicleplatenumber == value)
                    return;
                this.vehicleplatenumber = value;
                this.OnPropertyChanged("VehiclePlateNumber");
            }
            get { return this.vehicleplatenumber; }
        }

        private string invoicesignature;
        public string InvoiceSignature
        {
            set
            {
                if (this.invoicesignature == value)
                    return;
                this.invoicesignature = value;
                this.OnPropertyChanged("InvoiceSignature");
            }
            get { return this.invoicesignature; }
        }

        private Guid invoicetypeid;
        public Guid InvoiceTypeId
        {
            set
            {
                if (this.invoicetypeid == value)
                    return;
                this.invoicetypeid = value;
                this.OnPropertyChanged("InvoiceTypeId");
            }
            get { return this.invoicetypeid; }
        }

        private Guid? vehicleid;
        public Guid? VehicleId
        {
            set
            {
                if (this.vehicleid == value)
                    return;
                this.vehicleid = value;
                this.OnPropertyChanged("VehicleId");
            }
            get { return this.vehicleid; }
        }

        private DateTime transactiondate;
        public DateTime TransactionDate
        {
            set
            {
                if (this.transactiondate == value)
                    return;
                this.transactiondate = value;
                this.OnPropertyChanged("TransactionDate");
            }
            get { return this.transactiondate; }
        }

        private decimal? nettoamount;
        public decimal? NettoAmount
        {
            set
            {
                if (this.nettoamount == value)
                    return;
                this.nettoamount = value;
                this.OnPropertyChanged("NettoAmount");
            }
            get { return this.nettoamount; }
        }

        private decimal? nettoAfterDiscount;
        public decimal? NettoAfterDiscount
        {
            set
            {
                if (this.nettoAfterDiscount == value)
                    return;
                this.nettoAfterDiscount = value;
                this.OnPropertyChanged("NettoAfterDiscount");
            }
            get { return this.nettoAfterDiscount; }
        }

        private decimal? vatamount;
        public decimal? VatAmount
        {
            set
            {
                if (this.vatamount == value)
                    return;
                this.vatamount = value;
                this.OnPropertyChanged("VatAmount");
            }
            get { return this.vatamount; }
        }

        private decimal? totalamount;
        public decimal? TotalAmount
        {
            set
            {
                if (this.totalamount == value)
                    return;
                this.totalamount = value;
                this.OnPropertyChanged("TotalAmount");
            }
            get { return this.totalamount; }
        }

        private string printer;
        public string Printer
        {
            set
            {
                if (this.printer == value)
                    return;
                this.printer = value;
                this.OnPropertyChanged("Printer");
            }
            get { return this.printer; }
        }

        private Guid? invoiceformid;
        public Guid? InvoiceFormId
        {
            set
            {
                if (this.invoiceformid == value)
                    return;
                this.invoiceformid = value;
                this.OnPropertyChanged("InvoiceFormId");
            }
            get { return this.invoiceformid; }
        }

        private Guid applicationuserid;
        public Guid ApplicationUserId
        {
            set
            {
                if (this.applicationuserid == value)
                    return;
                this.applicationuserid = value;
                this.OnPropertyChanged("ApplicationUserId");
            }
            get { return this.applicationuserid; }
        }

        private bool? isprinted;
        public bool? IsPrinted
        {
            set
            {
                if (this.isprinted == value)
                    return;
                this.isprinted = value;
                this.OnPropertyChanged("IsPrinted");
            }
            get { return this.isprinted; }
        }

        private string series;
        public string Series
        {
            set
            {
                if (this.series == value)
                    return;
                this.series = value;
                this.OnPropertyChanged("Series");
            }
            get { return this.series; }
        }

        private string notes;
        public string Notes
        {
            set
            {
                if (this.notes == value)
                    return;
                this.notes = value;
                this.OnPropertyChanged("Notes");
            }
            get { return this.notes; }
        }

        private int? paymenttype;
        public int? PaymentType
        {
            set
            {
                if (this.paymenttype == value)
                    return;
                this.paymenttype = value;
                this.OnPropertyChanged("PaymentType");
            }
            get { return this.paymenttype; }
        }

        private bool? iseuromat;
        public bool? IsEuromat
        {
            set
            {
                if (this.iseuromat == value)
                    return;
                this.iseuromat = value;
                this.OnPropertyChanged("IsEuromat");
            }
            get { return this.iseuromat; }
        }

        private decimal discountamount;
        public decimal DiscountAmount
        {
            set
            {
                if (this.discountamount == value)
                    return;
                this.discountamount = value;
                this.OnPropertyChanged("DiscountAmount");
            }
            get { return this.discountamount; }
        }

        private string supplyNumber;
        public string SupplyNumber
        {
            set
            {
                if (this.supplyNumber == value)
                    return;
                this.supplyNumber = value;
                this.OnPropertyChanged("SupplyNumber");
            }
            get { return this.supplyNumber; }
        }

        private decimal? lastrestamount;
        public decimal? LastRestAmount
        {
            set
            {
                if (this.lastrestamount == value)
                    return;
                this.lastrestamount = value;
                this.OnPropertyChanged("LastRestAmount");
            }
            get { return this.lastrestamount; }
        }

        private bool? allowEdit;
        public bool? AllowEdit
        {
            set
            {
                if (this.allowEdit == value)
                    return;
                this.allowEdit = value;
                this.OnPropertyChanged("AllowEdit");
            }
            get { return this.allowEdit; }
        }

        private int? vehicleOdometer;
        public int? VehicleOdometer
        {
            set
            {
                if (this.vehicleOdometer == value)
                    return;
                this.vehicleOdometer = value;
                this.OnPropertyChanged("VehicleOdometer");
            }
            get { return this.vehicleOdometer; }
        }

        private string deliveryAddress;
        public string DeliveryAddress
        {
            set
            {
                if (this.deliveryAddress == value)
                    return;
                this.deliveryAddress = value;
                this.OnPropertyChanged("DeliveryAddress");
            }
            get { return this.deliveryAddress; }
        }

        private decimal discountamountRetail;
        public decimal DiscountAmountRetail
        {
            set
            {
                if (this.discountamountRetail == value)
                    return;
                this.discountamountRetail = value;
                this.OnPropertyChanged("DiscountAmountRetail");
            }
            get { return this.discountamountRetail; }
        }

        private decimal discountamountWhole;
        public decimal DiscountAmountWhole
        {
            set
            {
                if (this.discountamountWhole == value)
                    return;
                this.discountamountWhole = value;
                this.OnPropertyChanged("DiscountAmountWhole");
            }
            get { return this.discountamountWhole; }
        }
        private string mark;
        public string Mark
        {
            set
            {
                if (this.mark == value)
                    return;
                this.mark = value;
                this.OnPropertyChanged("Mark");
            }
            get { return this.mark; }
        }
    }

    public partial class InvoiceCatalogViewViewModel : BaseViewModel<Data.InvoiceCatalogView>
    {
    }

    public partial class InvoiceFormViewModel : BaseViewModel<Data.InvoiceForm>
    {
        private Guid invoiceformid;
        [PrimaryKey]
        public Guid InvoiceFormId
        {
            set
            {
                if (this.invoiceformid == value)
                    return;
                this.invoiceformid = value;
                this.OnPropertyChanged("InvoiceFormId");
            }
            get { return this.invoiceformid; }
        }

        private bool istextform;
        public bool IsTextForm
        {
            set
            {
                if (this.istextform == value)
                    return;
                this.istextform = value;
                this.OnPropertyChanged("IsTextForm");
            }
            get { return this.istextform; }
        }

        private string data;
        public string Data
        {
            set
            {
                if (this.data == value)
                    return;
                this.data = value;
                this.OnPropertyChanged("Data");
            }
            get { return this.data; }
        }

    }

    public partial class InvoiceGroupViewViewModel : BaseViewModel<Data.InvoiceGroupView>
    {
    }

    public partial class InvoiceLineViewModel : BaseViewModel<Data.InvoiceLine>
    {
        private Guid invoicelineid;
        [PrimaryKey]
        public Guid InvoiceLineId
        {
            set
            {
                if (this.invoicelineid == value)
                    return;
                this.invoicelineid = value;
                this.OnPropertyChanged("InvoiceLineId");
            }
            get { return this.invoicelineid; }
        }

        private Guid invoiceid;
        public Guid InvoiceId
        {
            set
            {
                if (this.invoiceid == value)
                    return;
                this.invoiceid = value;
                this.OnPropertyChanged("InvoiceId");
            }
            get { return this.invoiceid; }
        }

        private decimal volume;
        public decimal Volume
        {
            set
            {
                if (this.volume == value)
                    return;
                this.volume = value;
                this.OnPropertyChanged("Volume");
            }
            get { return this.volume; }
        }

        private decimal volumenormalized;
        public decimal VolumeNormalized
        {
            set
            {
                if (this.volumenormalized == value)
                    return;
                this.volumenormalized = value;
                this.OnPropertyChanged("VolumeNormalized");
            }
            get { return this.volumenormalized; }
        }

        private decimal temperature;
        public decimal Temperature
        {
            set
            {
                if (this.temperature == value)
                    return;
                this.temperature = value;
                this.OnPropertyChanged("Temperature");
            }
            get { return this.temperature; }
        }

        private decimal fueldensity;
        public decimal FuelDensity
        {
            set
            {
                if (this.fueldensity == value)
                    return;
                this.fueldensity = value;
                this.OnPropertyChanged("FuelDensity");
            }
            get { return this.fueldensity; }
        }

        private decimal unitprice;
        public decimal UnitPrice
        {
            set
            {
                if (this.unitprice == value)
                    return;
                this.unitprice = value;
                this.OnPropertyChanged("UnitPrice");
            }
            get { return this.unitprice; }
        }

        private decimal totalprice;
        public decimal TotalPrice
        {
            set
            {
                if (this.totalprice == value)
                    return;
                this.totalprice = value;
                this.OnPropertyChanged("TotalPrice");
            }
            get { return this.totalprice; }
        }

        private decimal vatamount;
        public decimal VatAmount
        {
            set
            {
                if (this.vatamount == value)
                    return;
                this.vatamount = value;
                this.OnPropertyChanged("VatAmount");
            }
            get { return this.vatamount; }
        }

        private decimal vatpercentage;
        public decimal VatPercentage
        {
            set
            {
                if (this.vatpercentage == value)
                    return;
                this.vatpercentage = value;
                this.OnPropertyChanged("VatPercentage");
            }
            get
            {
                if(this.vatpercentage == 0)
                {
                    decimal vat = Data.Implementation.OptionHandler.Instance.GetDecimalOption("VATValue", 24);
                    return vat;
                }
                return this.vatpercentage;
            }
        }

        private Guid? saletransactionid;
        public Guid? SaleTransactionId
        {
            set
            {
                if (this.saletransactionid == value)
                    return;
                this.saletransactionid = value;
                this.OnPropertyChanged("SaleTransactionId");
            }
            get { return this.saletransactionid; }
        }

        private Guid? tankfillingid;
        public Guid? TankFillingId
        {
            set
            {
                if (this.tankfillingid == value)
                    return;
                this.tankfillingid = value;
                this.OnPropertyChanged("TankFillingId");
            }
            get { return this.tankfillingid; }
        }

        private Guid fueltypeid;
        public Guid FuelTypeId
        {
            set
            {
                if (this.fueltypeid == value)
                    return;
                this.fueltypeid = value;
                this.OnPropertyChanged("FuelTypeId");
            }
            get { return this.fueltypeid; }
        }

        private decimal discountamount;
        public decimal DiscountAmount
        {
            set
            {
                if (this.discountamount == value)
                    return;
                this.discountamount = value;
                this.OnPropertyChanged("DiscountAmount");
            }
            get { return this.discountamount; }
        }

        private Guid? tankid;
        public Guid? TankId
        {
            set
            {
                if (this.tankid == value)
                    return;
                this.tankid = value;
                this.OnPropertyChanged("TankId");
            }
            get { return this.tankid; }
        }

        private Guid? invoicerelationid;
        public Guid? InvoiceRelationId
        {
            set
            {
                if (this.invoicerelationid == value)
                    return;
                this.invoicerelationid = value;
                this.OnPropertyChanged("InvoiceRelationId");
            }
            get { return this.invoicerelationid; }
        }


        private decimal unitPriceRetail;
        public decimal UnitPriceRetail
        {
            set
            {
                if (this.unitPriceRetail == value)
                    return;
                this.unitPriceRetail = value;
                this.unitPriceWhole = value / ((100 + this.VatPercentageView) / 100);
                this.unitprice = this.unitPriceRetail;
                //this.OnPropertyChanged("UnitPriceRetail");
                this.OnPropertyChanged("UnitPriceView");
            }
            get { return this.unitPriceRetail; }
        }

        private decimal unitPriceWhole;
        public decimal UnitPriceWhole
        {
            set
            {
                if (this.unitPriceWhole == value)
                    return;
                this.unitPriceWhole = value;
                this.unitPriceRetail = value * ((100 + this.VatPercentageView) / 100);
                this.unitprice = this.unitPriceRetail;
                //this.OnPropertyChanged("UnitPriceWhole");
                this.OnPropertyChanged("UnitPriceView");
            }
            get { return this.unitPriceWhole; }
        }

        private decimal discountamountRetail;
        public decimal DiscountAmountRetail
        {
            set
            {
                if (this.discountamountRetail == value)
                    return;
                this.discountamountRetail = value;
                this.OnPropertyChanged("DiscountAmountRetail");
            }
            get { return this.discountamountRetail; }
        }

        private decimal discountamountWhole;
        public decimal DiscountAmountWhole
        {
            set
            {
                if (this.discountamountWhole == value)
                    return;
                this.discountamountWhole = value;
                this.OnPropertyChanged("DiscountAmountWhole");
            }
            get { return this.discountamountWhole; }
        }

        private decimal discountPercentage;
        public decimal DiscountPercentage
        {
            set
            {
                if (this.discountPercentage == value)
                    return;
                this.discountPercentage = value;
                this.OnPropertyChanged("DiscountPercentage");
            }
            get { return this.discountPercentage; }
        }
    }

    public partial class InvoiceLineRelationViewModel : BaseViewModel<Data.InvoiceLineRelation>
    {
        private Guid invoicelinerelationid;
        [PrimaryKey]
        public Guid InvoiceLineRelationId
        {
            set
            {
                if (this.invoicelinerelationid == value)
                    return;
                this.invoicelinerelationid = value;
                this.OnPropertyChanged("InvoiceLineRelationId");
            }
            get { return this.invoicelinerelationid; }
        }

        private Guid invoicerelationid;
        public Guid InvoiceRelationId
        {
            set
            {
                if (this.invoicerelationid == value)
                    return;
                this.invoicerelationid = value;
                this.OnPropertyChanged("InvoiceRelationId");
            }
            get { return this.invoicerelationid; }
        }

        private Guid parentlineid;
        public Guid ParentLineId
        {
            set
            {
                if (this.parentlineid == value)
                    return;
                this.parentlineid = value;
                this.OnPropertyChanged("ParentLineId");
            }
            get { return this.parentlineid; }
        }

        private Guid childrelationid;
        public Guid ChildRelationId
        {
            set
            {
                if (this.childrelationid == value)
                    return;
                this.childrelationid = value;
                this.OnPropertyChanged("ChildRelationId");
            }
            get { return this.childrelationid; }
        }

    }

    public partial class InvoicePrintViewModel : BaseViewModel<Data.InvoicePrint>
    {
        private Guid invoiceprintid;
        [PrimaryKey]
        public Guid InvoicePrintId
        {
            set
            {
                if (this.invoiceprintid == value)
                    return;
                this.invoiceprintid = value;
                this.OnPropertyChanged("InvoicePrintId");
            }
            get { return this.invoiceprintid; }
        }

        private Guid dispenserid;
        public Guid DispenserId
        {
            set
            {
                if (this.dispenserid == value)
                    return;
                this.dispenserid = value;
                this.OnPropertyChanged("DispenserId");
            }
            get { return this.dispenserid; }
        }

        private string printer;
        public string Printer
        {
            set
            {
                if (this.printer == value)
                    return;
                this.printer = value;
                this.OnPropertyChanged("Printer");
            }
            get { return this.printer; }
        }

        private Guid defaultinvoicetype;
        public Guid DefaultInvoiceType
        {
            set
            {
                if (this.defaultinvoicetype == value)
                    return;
                this.defaultinvoicetype = value;
                this.OnPropertyChanged("DefaultInvoiceType");
            }
            get { return this.defaultinvoicetype; }
        }

    }

    public partial class InvoicePrintViewViewModel : BaseViewModel<Data.InvoicePrintView>
    {
    }

    public partial class InvoiceRelationViewModel : BaseViewModel<Data.InvoiceRelation>
    {
        private Guid invoicerelationid;
        [PrimaryKey]
        public Guid InvoiceRelationId
        {
            set
            {
                if (this.invoicerelationid == value)
                    return;
                this.invoicerelationid = value;
                this.OnPropertyChanged("InvoiceRelationId");
            }
            get { return this.invoicerelationid; }
        }

        private Guid parentinvoiceid;
        public Guid ParentInvoiceId
        {
            set
            {
                if (this.parentinvoiceid == value)
                    return;
                this.parentinvoiceid = value;
                this.OnPropertyChanged("ParentInvoiceId");
            }
            get { return this.parentinvoiceid; }
        }

        private Guid childinvoiceid;
        public Guid ChildInvoiceId
        {
            set
            {
                if (this.childinvoiceid == value)
                    return;
                this.childinvoiceid = value;
                this.OnPropertyChanged("ChildInvoiceId");
            }
            get { return this.childinvoiceid; }
        }

        private int relationtype;
        public int RelationType
        {
            set
            {
                if (this.relationtype == value)
                    return;
                this.relationtype = value;
                this.OnPropertyChanged("RelationType");
            }
            get { return this.relationtype; }
        }

    }

    public partial class InvoiceTypeViewModel : BaseViewModel<Data.InvoiceType>
    {
        private Guid invoicetypeid;
        [PrimaryKey]
        public Guid InvoiceTypeId
        {
            set
            {
                if (this.invoicetypeid == value)
                    return;
                this.invoicetypeid = value;
                this.OnPropertyChanged("InvoiceTypeId");
            }
            get { return this.invoicetypeid; }
        }

        private string description;
        public string Description
        {
            set
            {
                if (this.description == value)
                    return;
                this.description = value;
                this.OnPropertyChanged("Description");
            }
            get { return this.description; }
        }

        private string abbreviation;
        public string Abbreviation
        {
            set
            {
                if (this.abbreviation == value)
                    return;
                this.abbreviation = value;
                this.OnPropertyChanged("Abbreviation");
            }
            get { return this.abbreviation; }
        }

        private int lastnumber;
        public int LastNumber
        {
            set
            {
                if (this.lastnumber == value)
                    return;
                this.lastnumber = value;
                this.OnPropertyChanged("LastNumber");
            }
            get { return this.lastnumber; }
        }

        private int transactiontype;
        public int TransactionType
        {
            set
            {
                if (this.transactiontype == value)
                    return;
                this.transactiontype = value;
                this.OnPropertyChanged("TransactionType");
            }
            get { return this.transactiontype; }
        }

        private bool printable;
        public bool Printable
        {
            set
            {
                if (this.printable == value)
                    return;
                this.printable = value;
                this.OnPropertyChanged("Printable");
            }
            get { return this.printable; }
        }

        private int officialenumerator;
        public int OfficialEnumerator
        {
            set
            {
                if (this.officialenumerator == value)
                    return;
                this.officialenumerator = value;
                this.OnPropertyChanged("OfficialEnumerator");
            }
            get { return this.officialenumerator; }
        }

        private string printer;
        public string Printer
        {
            set
            {
                if (this.printer == value)
                    return;
                this.printer = value;
                this.OnPropertyChanged("Printer");
            }
            get { return this.printer; }
        }

        private Guid? invoiceformid;
        public Guid? InvoiceFormId
        {
            set
            {
                if (this.invoiceformid == value)
                    return;
                this.invoiceformid = value;
                this.OnPropertyChanged("InvoiceFormId");
            }
            get { return this.invoiceformid; }
        }

        private bool? isinternal;
        public bool? IsInternal
        {
            set
            {
                if (this.isinternal == value)
                    return;
                this.isinternal = value;
                this.OnPropertyChanged("IsInternal");
            }
            get { return this.isinternal; }
        }

        private string internaldeliverydescription;
        public string InternalDeliveryDescription
        {
            set
            {
                if (this.internaldeliverydescription == value)
                    return;
                this.internaldeliverydescription = value;
                this.OnPropertyChanged("InternalDeliveryDescription");
            }
            get { return this.internaldeliverydescription; }
        }

        private bool? needsvehicle;
        public bool? NeedsVehicle
        {
            set
            {
                if (this.needsvehicle == value)
                    return;
                this.needsvehicle = value;
                this.OnPropertyChanged("NeedsVehicle");
            }
            get { return this.needsvehicle; }
        }

        private bool? iscancelation;
        public bool? IsCancelation
        {
            set
            {
                if (this.iscancelation == value)
                    return;
                this.iscancelation = value;
                this.OnPropertyChanged("IsCancelation");
            }
            get { return this.iscancelation; }
        }

        private bool? hasfinancialtransactions;
        public bool? HasFinancialTransactions
        {
            set
            {
                if (this.hasfinancialtransactions == value)
                    return;
                this.hasfinancialtransactions = value;
                this.OnPropertyChanged("HasFinancialTransactions");
            }
            get { return this.hasfinancialtransactions; }
        }

        private bool? islaserprint;
        public bool? IsLaserPrint
        {
            set
            {
                if (this.islaserprint == value)
                    return;
                this.islaserprint = value;
                this.OnPropertyChanged("IsLaserPrint");
            }
            get { return this.islaserprint; }
        }

        private int? transactionsign;
        public int? TransactionSign
        {
            set
            {
                if (this.transactionsign == value)
                    return;
                this.transactionsign = value;
                this.OnPropertyChanged("TransactionSign");
            }
            get { return this.transactionsign; }
        }

        private bool? showfinancialdata;
        public bool? ShowFinancialData
        {
            set
            {
                if (this.showfinancialdata == value)
                    return;
                this.showfinancialdata = value;
                this.OnPropertyChanged("ShowFinancialData");
            }
            get { return this.showfinancialdata; }
        }

        private int? deliverytype;
        public int? DeliveryType
        {
            set
            {
                if (this.deliverytype == value)
                    return;
                this.deliverytype = value;
                this.OnPropertyChanged("DeliveryType");
            }
            get { return this.deliverytype; }
        }

        private bool? adminview;
        public bool? AdminView
        {
            set
            {
                if (this.adminview == value)
                    return;
                this.adminview = value;
                this.OnPropertyChanged("AdminView");
            }
            get { return this.adminview; }
        }

        private bool? invalidated;
        public bool? Invalidated
        {
            set
            {
                if (this.invalidated == value)
                    return;
                this.invalidated = value;
                this.OnPropertyChanged("Invalidated");
            }
            get { return this.invalidated; }
        }

        private bool? dispensertype;
        public bool? DispenserType
        {
            set
            {
                if (this.dispensertype == value)
                    return;
                this.dispensertype = value;
                this.OnPropertyChanged("DispenserType");
            }
            get { return this.dispensertype; }
        }

        private bool? forcesdelivery;
        public bool? ForcesDelivery
        {
            set
            {
                if (this.forcesdelivery == value)
                    return;
                this.forcesdelivery = value;
                this.OnPropertyChanged("ForcesDelivery");
            }
            get { return this.forcesdelivery; }
        }

        private bool? retailInvoice;
        public bool? RetailInvoice
        {
            set
            {
                if (this.retailInvoice == value)
                    return;
                this.retailInvoice = value;
                this.OnPropertyChanged("RetailInvoice");
            }
            get { return this.retailInvoice; }
        }

        private bool? includeInBalance;
        public bool? IncludeInBalance
        {
            set
            {
                if (this.includeInBalance == value)
                    return;
                this.includeInBalance = value;
                this.OnPropertyChanged("IncludeInBalance");
            }
            get { return this.includeInBalance; }
        }

        private string defaultSeries;
        public string DefaultSeries
        {
            set
            {
                if (this.defaultSeries == value)
                    return;
                this.defaultSeries = value;
                this.OnPropertyChanged("DefaultSeries");
            }
            get { return this.defaultSeries; }
        }

        private bool sendToMyData;
        public bool SendToMyData
        {
            set
            {
                if (this.sendToMyData == value)
                    return;
                this.sendToMyData = value;
                this.OnPropertyChanged("SendToMyData");
            }
            get { return this.sendToMyData; }
        }
    }

    public partial class InvoiceTypeTransformViewModel : BaseViewModel<Data.InvoiceTypeTransform>
    {
        private Guid invoicetypetransformid;
        [PrimaryKey]
        public Guid InvoiceTypeTransformId
        {
            set
            {
                if (this.invoicetypetransformid == value)
                    return;
                this.invoicetypetransformid = value;
                this.OnPropertyChanged("InvoiceTypeTransformId");
            }
            get { return this.invoicetypetransformid; }
        }

        private Guid parentinvoicetypeid;
        public Guid ParentInvoiceTypeId
        {
            set
            {
                if (this.parentinvoicetypeid == value)
                    return;
                this.parentinvoicetypeid = value;
                this.OnPropertyChanged("ParentInvoiceTypeId");
            }
            get { return this.parentinvoicetypeid; }
        }

        private Guid childinvoicetypeid;
        public Guid ChildInvoiceTypeId
        {
            set
            {
                if (this.childinvoicetypeid == value)
                    return;
                this.childinvoicetypeid = value;
                this.OnPropertyChanged("ChildInvoiceTypeId");
            }
            get { return this.childinvoicetypeid; }
        }

        private int transformationmode;
        public int TransformationMode
        {
            set
            {
                if (this.transformationmode == value)
                    return;
                this.transformationmode = value;
                this.OnPropertyChanged("TransformationMode");
            }
            get { return this.transformationmode; }
        }

        private string notesAddition;
        public string NotesAddition
        {
            set
            {
                if (this.notesAddition == value)
                    return;
                this.notesAddition = value;
                this.OnPropertyChanged("NotesAddition");
            }
            get { return this.notesAddition; }
        }

        private Guid? creationInvoiceTypeId;
        public Guid? CreationInvoiceTypeId
        {
            set
            {
                if (this.creationInvoiceTypeId == value)
                    return;
                this.creationInvoiceTypeId = value;
                this.OnPropertyChanged("CreationInvoiceTypeId");
            }
            get { return this.creationInvoiceTypeId; }
        }

        private int? creationType;
        public int? CreationType
        {
            set
            {
                if (this.creationType == value)
                    return;
                this.creationType = value;
                this.OnPropertyChanged("CreationType");
            }
            get { return this.creationType; }
        }

        private string creationNotesAddition;
        public string CreationNotesAddition
        {
            set
            {
                if (this.creationNotesAddition == value)
                    return;
                this.creationNotesAddition = value;
                this.OnPropertyChanged("CreationNotesAddition");
            }
            get { return this.creationNotesAddition; }
        }
    }

    public partial class NormalizationViewModel : BaseViewModel<Data.Normalization>
    {
        private Guid normalizationid;
        [PrimaryKey]
        public Guid NormalizationId
        {
            set
            {
                if (this.normalizationid == value)
                    return;
                this.normalizationid = value;
                this.OnPropertyChanged("NormalizationId");
            }
            get { return this.normalizationid; }
        }

        private Guid fueltypeid;
        public Guid FuelTypeId
        {
            set
            {
                if (this.fueltypeid == value)
                    return;
                this.fueltypeid = value;
                this.OnPropertyChanged("FuelTypeId");
            }
            get { return this.fueltypeid; }
        }

        private decimal density;
        public decimal Density
        {
            set
            {
                if (this.density == value)
                    return;
                this.density = value;
                this.OnPropertyChanged("Density");
            }
            get { return this.density; }
        }

        private decimal temperature;
        public decimal Temperature
        {
            set
            {
                if (this.temperature == value)
                    return;
                this.temperature = value;
                this.OnPropertyChanged("Temperature");
            }
            get { return this.temperature; }
        }

        private decimal thermalcoefficient;
        public decimal ThermalCoefficient
        {
            set
            {
                if (this.thermalcoefficient == value)
                    return;
                this.thermalcoefficient = value;
                this.OnPropertyChanged("ThermalCoefficient");
            }
            get { return this.thermalcoefficient; }
        }

    }

    public partial class NozzleViewModel : BaseViewModel<Data.Nozzle>
    {
        private Guid nozzleid;
        [PrimaryKey]
        public Guid NozzleId
        {
            set
            {
                if (this.nozzleid == value)
                    return;
                this.nozzleid = value;
                this.OnPropertyChanged("NozzleId");
            }
            get { return this.nozzleid; }
        }

        private Guid dispenserid;
        public Guid DispenserId
        {
            set
            {
                if (this.dispenserid == value)
                    return;
                this.dispenserid = value;
                this.OnPropertyChanged("DispenserId");
            }
            get { return this.dispenserid; }
        }

        private decimal totalcounter;
        public decimal TotalCounter
        {
            set
            {
                if (this.totalcounter == value)
                    return;
                this.totalcounter = value;
                this.OnPropertyChanged("TotalCounter");
            }
            get { return this.totalcounter; }
        }

        private int channel;
        public int Channel
        {
            set
            {
                if (this.channel == value)
                    return;
                this.channel = value;
                this.OnPropertyChanged("Channel");
            }
            get { return this.channel; }
        }

        private int address;
        public int Address
        {
            set
            {
                if (this.address == value)
                    return;
                this.address = value;
                this.OnPropertyChanged("Address");
            }
            get { return this.address; }
        }

        private string name;
        public string Name
        {
            set
            {
                if (this.name == value)
                    return;
                this.name = value;
                this.OnPropertyChanged("Name");
            }
            get { return this.name; }
        }

        private Guid fueltypeid;
        public Guid FuelTypeId
        {
            set
            {
                if (this.fueltypeid == value)
                    return;
                this.fueltypeid = value;
                this.OnPropertyChanged("FuelTypeId");
            }
            get { return this.fueltypeid; }
        }

        private int orderid;
        public int OrderId
        {
            set
            {
                if (this.orderid == value)
                    return;
                this.orderid = value;
                this.OnPropertyChanged("OrderId");
            }
            get { return this.orderid; }
        }

        private int nozzlestate;
        public int NozzleState
        {
            set
            {
                if (this.nozzlestate == value)
                    return;
                this.nozzlestate = value;
                this.OnPropertyChanged("NozzleState");
            }
            get { return this.nozzlestate; }
        }

        private string serialnumber;
        public string SerialNumber
        {
            set
            {
                if (this.serialnumber == value)
                    return;
                this.serialnumber = value;
                this.OnPropertyChanged("SerialNumber");
            }
            get { return this.serialnumber; }
        }

        private int officialnozzlenumber;
        public int OfficialNozzleNumber
        {
            set
            {
                if (this.officialnozzlenumber == value)
                    return;
                this.officialnozzlenumber = value;
                this.OnPropertyChanged("OfficialNozzleNumber");
            }
            get { return this.officialnozzlenumber; }
        }

        private int? nozzleindex;
        public int? NozzleIndex
        {
            set
            {
                if (this.nozzleindex == value)
                    return;
                this.nozzleindex = value;
                this.OnPropertyChanged("NozzleIndex");
            }
            get { return this.nozzleindex; }
        }

    }

    public partial class NozzleFlowViewModel : BaseViewModel<Data.NozzleFlow>
    {
        private Guid nozzleflowid;
        [PrimaryKey]
        public Guid NozzleFlowId
        {
            set
            {
                if (this.nozzleflowid == value)
                    return;
                this.nozzleflowid = value;
                this.OnPropertyChanged("NozzleFlowId");
            }
            get { return this.nozzleflowid; }
        }

        private Guid nozzleid;
        public Guid NozzleId
        {
            set
            {
                if (this.nozzleid == value)
                    return;
                this.nozzleid = value;
                this.OnPropertyChanged("NozzleId");
            }
            get { return this.nozzleid; }
        }

        private Guid tankid;
        public Guid TankId
        {
            set
            {
                if (this.tankid == value)
                    return;
                this.tankid = value;
                this.OnPropertyChanged("TankId");
            }
            get { return this.tankid; }
        }

        private Int16 flowstate;
        public Int16 FlowState
        {
            set
            {
                if (this.flowstate == value)
                    return;
                this.flowstate = value;
                this.OnPropertyChanged("FlowState");
            }
            get { return this.flowstate; }
        }

    }

    public partial class NozzlePriceListViewModel : BaseViewModel<Data.NozzlePriceList>
    {
        private Guid nozzlepricelistid;
        [PrimaryKey]
        public Guid NozzlePriceListId
        {
            set
            {
                if (this.nozzlepricelistid == value)
                    return;
                this.nozzlepricelistid = value;
                this.OnPropertyChanged("NozzlePriceListId");
            }
            get { return this.nozzlepricelistid; }
        }

        private Guid? pricelistid;
        public Guid? PriceListId
        {
            set
            {
                if (this.pricelistid == value)
                    return;
                this.pricelistid = value;
                this.OnPropertyChanged("PriceListId");
            }
            get { return this.pricelistid; }
        }

        private Guid? nozzleid;
        public Guid? NozzleId
        {
            set
            {
                if (this.nozzleid == value)
                    return;
                this.nozzleid = value;
                this.OnPropertyChanged("NozzleId");
            }
            get { return this.nozzleid; }
        }

        private decimal? discount;
        public decimal? Discount
        {
            set
            {
                if (this.discount == value)
                    return;
                this.discount = value;
                this.OnPropertyChanged("Discount");
            }
            get { return this.discount; }
        }

    }

    public partial class NozzleUsagePeriodViewModel : BaseViewModel<Data.NozzleUsagePeriod>
    {
        private Guid nozzleusageperiodid;
        [PrimaryKey]
        public Guid NozzleUsagePeriodId
        {
            set
            {
                if (this.nozzleusageperiodid == value)
                    return;
                this.nozzleusageperiodid = value;
                this.OnPropertyChanged("NozzleUsagePeriodId");
            }
            get { return this.nozzleusageperiodid; }
        }

        private Guid nozzleid;
        public Guid NozzleId
        {
            set
            {
                if (this.nozzleid == value)
                    return;
                this.nozzleid = value;
                this.OnPropertyChanged("NozzleId");
            }
            get { return this.nozzleid; }
        }

        private Guid usageperiodid;
        public Guid UsagePeriodId
        {
            set
            {
                if (this.usageperiodid == value)
                    return;
                this.usageperiodid = value;
                this.OnPropertyChanged("UsagePeriodId");
            }
            get { return this.usageperiodid; }
        }

        private DateTime measurementdatetime;
        public DateTime MeasurementDateTime
        {
            set
            {
                if (this.measurementdatetime == value)
                    return;
                this.measurementdatetime = value;
                this.OnPropertyChanged("MeasurementDateTime");
            }
            get { return this.measurementdatetime; }
        }

        private DateTime expirationdate;
        public DateTime ExpirationDate
        {
            set
            {
                if (this.expirationdate == value)
                    return;
                this.expirationdate = value;
                this.OnPropertyChanged("ExpirationDate");
            }
            get { return this.expirationdate; }
        }

        private decimal totalvolume;
        public decimal TotalVolume
        {
            set
            {
                if (this.totalvolume == value)
                    return;
                this.totalvolume = value;
                this.OnPropertyChanged("TotalVolume");
            }
            get { return this.totalvolume; }
        }

        private Int64 crc;
        public Int64 CRC
        {
            set
            {
                if (this.crc == value)
                    return;
                this.crc = value;
                this.OnPropertyChanged("CRC");
            }
            get { return this.crc; }
        }

    }

    public partial class OilCompanyViewModel : BaseViewModel<Data.OilCompany>
    {
        private Guid oilcompanyid;
        [PrimaryKey]
        public Guid OilCompanyId
        {
            set
            {
                if (this.oilcompanyid == value)
                    return;
                this.oilcompanyid = value;
                this.OnPropertyChanged("OilCompanyId");
            }
            get { return this.oilcompanyid; }
        }

        private string name;
        public string Name
        {
            set
            {
                if (this.name == value)
                    return;
                this.name = value;
                this.OnPropertyChanged("Name");
            }
            get { return this.name; }
        }

        private byte[] logo;
        public byte[] Logo
        {
            set
            {
                if (this.logo == value)
                    return;
                this.logo = value;
                this.OnPropertyChanged("Logo");
            }
            get { return this.logo; }
        }

    }

    public partial class OptionViewModel : BaseViewModel<Data.Option>
    {
        private Guid optionid;
        [PrimaryKey]
        public Guid OptionId
        {
            set
            {
                if (this.optionid == value)
                    return;
                this.optionid = value;
                this.OnPropertyChanged("OptionId");
            }
            get { return this.optionid; }
        }

        private string optionkey;
        public string OptionKey
        {
            set
            {
                if (this.optionkey == value)
                    return;
                this.optionkey = value;
                this.OnPropertyChanged("OptionKey");
            }
            get { return this.optionkey; }
        }

        private string optionvalue;
        public string OptionValue
        {
            set
            {
                if (this.optionvalue == value)
                    return;
                this.optionvalue = value;
                this.OnPropertyChanged("OptionValue");
            }
            get { return this.optionvalue; }
        }

        private string description;
        public string Description
        {
            set
            {
                if (this.description == value)
                    return;
                this.description = value;
                this.OnPropertyChanged("Description");
            }
            get { return this.description; }
        }

    }

    public partial class OutdoorPaymentTerminalViewModel : BaseViewModel<Data.OutdoorPaymentTerminal>
    {
        private Guid outdoorpaymentterminalid;
        [PrimaryKey]
        public Guid OutdoorPaymentTerminalId
        {
            set
            {
                if (this.outdoorpaymentterminalid == value)
                    return;
                this.outdoorpaymentterminalid = value;
                this.OnPropertyChanged("OutdoorPaymentTerminalId");
            }
            get { return this.outdoorpaymentterminalid; }
        }

        private string serverip;
        public string ServerIp
        {
            set
            {
                if (this.serverip == value)
                    return;
                this.serverip = value;
                this.OnPropertyChanged("ServerIp");
            }
            get { return this.serverip; }
        }

        private int serverport;
        public int ServerPort
        {
            set
            {
                if (this.serverport == value)
                    return;
                this.serverport = value;
                this.OnPropertyChanged("ServerPort");
            }
            get { return this.serverport; }
        }

        private string clientip;
        public string ClientIp
        {
            set
            {
                if (this.clientip == value)
                    return;
                this.clientip = value;
                this.OnPropertyChanged("ClientIp");
            }
            get { return this.clientip; }
        }

        private int clientport;
        public int ClientPort
        {
            set
            {
                if (this.clientport == value)
                    return;
                this.clientport = value;
                this.OnPropertyChanged("ClientPort");
            }
            get { return this.clientport; }
        }

        private string name;
        public string Name
        {
            set
            {
                if (this.name == value)
                    return;
                this.name = value;
                this.OnPropertyChanged("Name");
            }
            get { return this.name; }
        }

        private int terminalid;
        public int TerminalId
        {
            set
            {
                if (this.terminalid == value)
                    return;
                this.terminalid = value;
                this.OnPropertyChanged("TerminalId");
            }
            get { return this.terminalid; }
        }

        private string terminalassembly;
        public string TerminalAssembly
        {
            set
            {
                if (this.terminalassembly == value)
                    return;
                this.terminalassembly = value;
                this.OnPropertyChanged("TerminalAssembly");
            }
            get { return this.terminalassembly; }
        }

        private bool isdisabled;
        public bool IsDisabled
        {
            set
            {
                if (this.isdisabled == value)
                    return;
                this.isdisabled = value;
                this.OnPropertyChanged("IsDisabled");
            }
            get { return this.isdisabled; }
        }

        private int terminaltype;
        public int TerminalType
        {
            set
            {
                if (this.terminaltype == value)
                    return;
                this.terminaltype = value;
                this.OnPropertyChanged("TerminalType");
            }
            get { return this.terminaltype; }
        }

        private int connectiontype;
        public int ConnectionType
        {
            set
            {
                if (this.connectiontype == value)
                    return;
                this.connectiontype = value;
                this.OnPropertyChanged("ConnectionType");
            }
            get { return this.connectiontype; }
        }

    }

    public partial class OutdoorPaymentTerminalControllerViewModel : BaseViewModel<Data.OutdoorPaymentTerminalController>
    {
        private Guid outdoorpaymentterminalcontrollerid;
        [PrimaryKey]
        public Guid OutdoorPaymentTerminalControllerId
        {
            set
            {
                if (this.outdoorpaymentterminalcontrollerid == value)
                    return;
                this.outdoorpaymentterminalcontrollerid = value;
                this.OnPropertyChanged("OutdoorPaymentTerminalControllerId");
            }
            get { return this.outdoorpaymentterminalcontrollerid; }
        }

        private Guid outdoorpaymentterminalid;
        public Guid OutdoorPaymentTerminalId
        {
            set
            {
                if (this.outdoorpaymentterminalid == value)
                    return;
                this.outdoorpaymentterminalid = value;
                this.OnPropertyChanged("OutdoorPaymentTerminalId");
            }
            get { return this.outdoorpaymentterminalid; }
        }

        private Guid communicationcontrollerid;
        public Guid CommunicationControllerId
        {
            set
            {
                if (this.communicationcontrollerid == value)
                    return;
                this.communicationcontrollerid = value;
                this.OnPropertyChanged("CommunicationControllerId");
            }
            get { return this.communicationcontrollerid; }
        }

    }

    public partial class OutdoorPaymentTerminalNozzleViewModel : BaseViewModel<Data.OutdoorPaymentTerminalNozzle>
    {
        private Guid outdoorpaymentterminalnozzleid;
        [PrimaryKey]
        public Guid OutdoorPaymentTerminalNozzleId
        {
            set
            {
                if (this.outdoorpaymentterminalnozzleid == value)
                    return;
                this.outdoorpaymentterminalnozzleid = value;
                this.OnPropertyChanged("OutdoorPaymentTerminalNozzleId");
            }
            get { return this.outdoorpaymentterminalnozzleid; }
        }

        private Guid? outdoorpaymentterminalid;
        public Guid? OutdoorPaymentTerminalId
        {
            set
            {
                if (this.outdoorpaymentterminalid == value)
                    return;
                this.outdoorpaymentterminalid = value;
                this.OnPropertyChanged("OutdoorPaymentTerminalId");
            }
            get { return this.outdoorpaymentterminalid; }
        }

        private Guid? nozzleid;
        public Guid? NozzleId
        {
            set
            {
                if (this.nozzleid == value)
                    return;
                this.nozzleid = value;
                this.OnPropertyChanged("NozzleId");
            }
            get { return this.nozzleid; }
        }

        private bool isdisabled;
        public bool IsDisabled
        {
            set
            {
                if (this.isdisabled == value)
                    return;
                this.isdisabled = value;
                this.OnPropertyChanged("IsDisabled");
            }
            get { return this.isdisabled; }
        }

    }

    public partial class OutdoorPaymentTerminalScheduleViewModel : BaseViewModel<Data.OutdoorPaymentTerminalSchedule>
    {
        private Guid outdoorpaymentterminalscheduleid;
        [PrimaryKey]
        public Guid OutdoorPaymentTerminalScheduleId
        {
            set
            {
                if (this.outdoorpaymentterminalscheduleid == value)
                    return;
                this.outdoorpaymentterminalscheduleid = value;
                this.OnPropertyChanged("OutdoorPaymentTerminalScheduleId");
            }
            get { return this.outdoorpaymentterminalscheduleid; }
        }

        private Guid outdoorpaymentterminalid;
        public Guid OutdoorPaymentTerminalId
        {
            set
            {
                if (this.outdoorpaymentterminalid == value)
                    return;
                this.outdoorpaymentterminalid = value;
                this.OnPropertyChanged("OutdoorPaymentTerminalId");
            }
            get { return this.outdoorpaymentterminalid; }
        }

        private int? dayofweek;
        public int? DayOfWeek
        {
            set
            {
                if (this.dayofweek == value)
                    return;
                this.dayofweek = value;
                this.OnPropertyChanged("DayOfWeek");
            }
            get { return this.dayofweek; }
        }

        private bool? isdayoff;
        public bool? IsDayOff
        {
            set
            {
                if (this.isdayoff == value)
                    return;
                this.isdayoff = value;
                this.OnPropertyChanged("IsDayOff");
            }
            get { return this.isdayoff; }
        }

        private DateTime? scheduledate;
        public DateTime? ScheduleDate
        {
            set
            {
                if (this.scheduledate == value)
                    return;
                this.scheduledate = value;
                this.OnPropertyChanged("ScheduleDate");
            }
            get { return this.scheduledate; }
        }

        private int scheduletype;
        public int ScheduleType
        {
            set
            {
                if (this.scheduletype == value)
                    return;
                this.scheduletype = value;
                this.OnPropertyChanged("ScheduleType");
            }
            get { return this.scheduletype; }
        }

        private Guid? outdoorpaymentterminalnozzleid;
        public Guid? OutdoorPaymentTerminalNozzleId
        {
            set
            {
                if (this.outdoorpaymentterminalnozzleid == value)
                    return;
                this.outdoorpaymentterminalnozzleid = value;
                this.OnPropertyChanged("OutdoorPaymentTerminalNozzleId");
            }
            get { return this.outdoorpaymentterminalnozzleid; }
        }

    }

    public partial class OutdoorPaymentTerminalTimeScheduleViewModel : BaseViewModel<Data.OutdoorPaymentTerminalTimeSchedule>
    {
        private Guid outdoorpaymentterminaltimescheduleid;
        [PrimaryKey]
        public Guid OutdoorPaymentTerminalTimeScheduleId
        {
            set
            {
                if (this.outdoorpaymentterminaltimescheduleid == value)
                    return;
                this.outdoorpaymentterminaltimescheduleid = value;
                this.OnPropertyChanged("OutdoorPaymentTerminalTimeScheduleId");
            }
            get { return this.outdoorpaymentterminaltimescheduleid; }
        }

        private Guid outdoorpaymentterminalscheduleid;
        public Guid OutdoorPaymentTerminalScheduleId
        {
            set
            {
                if (this.outdoorpaymentterminalscheduleid == value)
                    return;
                this.outdoorpaymentterminalscheduleid = value;
                this.OnPropertyChanged("OutdoorPaymentTerminalScheduleId");
            }
            get { return this.outdoorpaymentterminalscheduleid; }
        }

        private DateTime timefrom;
        public DateTime TimeFrom
        {
            set
            {
                if (this.timefrom == value)
                    return;
                this.timefrom = value;
                this.OnPropertyChanged("TimeFrom");
            }
            get { return this.timefrom; }
        }

        private int duration;
        public int Duration
        {
            set
            {
                if (this.duration == value)
                    return;
                this.duration = value;
                this.OnPropertyChanged("Duration");
            }
            get { return this.duration; }
        }

    }

    public partial class PendingSendViewModel : BaseViewModel<Data.PendingSend>
    {
        private Guid pendingsendid;
        [PrimaryKey]
        public Guid PendingSendId
        {
            set
            {
                if (this.pendingsendid == value)
                    return;
                this.pendingsendid = value;
                this.OnPropertyChanged("PendingSendId");
            }
            get { return this.pendingsendid; }
        }

        private string pendingdata;
        public string PendingData
        {
            set
            {
                if (this.pendingdata == value)
                    return;
                this.pendingdata = value;
                this.OnPropertyChanged("PendingData");
            }
            get { return this.pendingdata; }
        }

        private string datatype;
        public string DataType
        {
            set
            {
                if (this.datatype == value)
                    return;
                this.datatype = value;
                this.OnPropertyChanged("DataType");
            }
            get { return this.datatype; }
        }

        private string assemblyfile;
        public string AssemblyFile
        {
            set
            {
                if (this.assemblyfile == value)
                    return;
                this.assemblyfile = value;
                this.OnPropertyChanged("AssemblyFile");
            }
            get { return this.assemblyfile; }
        }

    }

    public partial class PriceListViewModel : BaseViewModel<Data.PriceList>
    {
        private Guid pricelistid;
        [PrimaryKey]
        public Guid PriceListId
        {
            set
            {
                if (this.pricelistid == value)
                    return;
                this.pricelistid = value;
                this.OnPropertyChanged("PriceListId");
            }
            get { return this.pricelistid; }
        }

        private string name;
        public string Name
        {
            set
            {
                if (this.name == value)
                    return;
                this.name = value;
                this.OnPropertyChanged("Name");
            }
            get { return this.name; }
        }

        private DateTime startdate;
        public DateTime StartDate
        {
            set
            {
                if (this.startdate == value)
                    return;
                this.startdate = value;
                this.OnPropertyChanged("StartDate");
            }
            get { return this.startdate; }
        }

        private DateTime? enddate;
        public DateTime? EndDate
        {
            set
            {
                if (this.enddate == value)
                    return;
                this.enddate = value;
                this.OnPropertyChanged("EndDate");
            }
            get { return this.enddate; }
        }

        private bool istraderpricelist;
        public bool IsTraderPriceList
        {
            set
            {
                if (this.istraderpricelist == value)
                    return;
                this.istraderpricelist = value;
                this.OnPropertyChanged("IsTraderPriceList");
            }
            get { return this.istraderpricelist; }
        }

    }

    public partial class PriceListTimeSpanViewModel : BaseViewModel<Data.PriceListTimeSpan>
    {
        private Guid pricelisttimespanid;
        [PrimaryKey]
        public Guid PriceListTimeSpanId
        {
            set
            {
                if (this.pricelisttimespanid == value)
                    return;
                this.pricelisttimespanid = value;
                this.OnPropertyChanged("PriceListTimeSpanId");
            }
            get { return this.pricelisttimespanid; }
        }

        private int fromtime;
        public int FromTime
        {
            set
            {
                if (this.fromtime == value)
                    return;
                this.fromtime = value;
                this.OnPropertyChanged("FromTime");
            }
            get { return this.fromtime; }
        }

        private int totime;
        public int ToTime
        {
            set
            {
                if (this.totime == value)
                    return;
                this.totime = value;
                this.OnPropertyChanged("ToTime");
            }
            get { return this.totime; }
        }

        private Guid? pricelistid;
        public Guid? PriceListId
        {
            set
            {
                if (this.pricelistid == value)
                    return;
                this.pricelistid = value;
                this.OnPropertyChanged("PriceListId");
            }
            get { return this.pricelistid; }
        }

    }

    public partial class SaleDataViewViewModel : BaseViewModel<Data.SaleDataView>
    {
    }

    public partial class SalesTransactionViewModel : BaseViewModel<Data.SalesTransaction>
    {
        private Guid salestransactionid;
        [PrimaryKey]
        public Guid SalesTransactionId
        {
            set
            {
                if (this.salestransactionid == value)
                    return;
                this.salestransactionid = value;
                this.OnPropertyChanged("SalesTransactionId");
            }
            get { return this.salestransactionid; }
        }

        private Guid usageperiodid;
        public Guid UsagePeriodId
        {
            set
            {
                if (this.usageperiodid == value)
                    return;
                this.usageperiodid = value;
                this.OnPropertyChanged("UsagePeriodId");
            }
            get { return this.usageperiodid; }
        }

        private bool islocked;
        public bool IsLocked
        {
            set
            {
                if (this.islocked == value)
                    return;
                this.islocked = value;
                this.OnPropertyChanged("IsLocked");
            }
            get { return this.islocked; }
        }

        private Int64 crc;
        public Int64 CRC
        {
            set
            {
                if (this.crc == value)
                    return;
                this.crc = value;
                this.OnPropertyChanged("CRC");
            }
            get { return this.crc; }
        }

        private Guid nozzleid;
        public Guid NozzleId
        {
            set
            {
                if (this.nozzleid == value)
                    return;
                this.nozzleid = value;
                this.OnPropertyChanged("NozzleId");
            }
            get { return this.nozzleid; }
        }

        private decimal totalizerstart;
        public decimal TotalizerStart
        {
            set
            {
                if (this.totalizerstart == value)
                    return;
                this.totalizerstart = value;
                this.OnPropertyChanged("TotalizerStart");
            }
            get { return this.totalizerstart; }
        }

        private decimal totalizerend;
        public decimal TotalizerEnd
        {
            set
            {
                if (this.totalizerend == value)
                    return;
                this.totalizerend = value;
                this.OnPropertyChanged("TotalizerEnd");
            }
            get { return this.totalizerend; }
        }

        private DateTime transactiontimestamp;
        public DateTime TransactionTimeStamp
        {
            set
            {
                if (this.transactiontimestamp == value)
                    return;
                this.transactiontimestamp = value;
                this.OnPropertyChanged("TransactionTimeStamp");
            }
            get { return this.transactiontimestamp; }
        }

        private decimal volume;
        public decimal Volume
        {
            set
            {
                if (this.volume == value)
                    return;
                this.volume = value;
                this.OnPropertyChanged("Volume");
            }
            get { return this.volume; }
        }

        private decimal volumenormalized;
        public decimal VolumeNormalized
        {
            set
            {
                if (this.volumenormalized == value)
                    return;
                this.volumenormalized = value;
                this.OnPropertyChanged("VolumeNormalized");
            }
            get { return this.volumenormalized; }
        }

        private decimal temperaturestart;
        public decimal TemperatureStart
        {
            set
            {
                if (this.temperaturestart == value)
                    return;
                this.temperaturestart = value;
                this.OnPropertyChanged("TemperatureStart");
            }
            get { return this.temperaturestart; }
        }

        private decimal temperatureend;
        public decimal TemperatureEnd
        {
            set
            {
                if (this.temperatureend == value)
                    return;
                this.temperatureend = value;
                this.OnPropertyChanged("TemperatureEnd");
            }
            get { return this.temperatureend; }
        }

        private decimal unitprice;
        public decimal UnitPrice
        {
            set
            {
                if (this.unitprice == value)
                    return;
                this.unitprice = value;
                this.OnPropertyChanged("UnitPrice");
            }
            get { return this.unitprice; }
        }

        private decimal totalprice;
        public decimal TotalPrice
        {
            set
            {
                if (this.totalprice == value)
                    return;
                this.totalprice = value;
                this.OnPropertyChanged("TotalPrice");
            }
            get { return this.totalprice; }
        }

        private Guid applicationuserid;
        public Guid ApplicationUserId
        {
            set
            {
                if (this.applicationuserid == value)
                    return;
                this.applicationuserid = value;
                this.OnPropertyChanged("ApplicationUserId");
            }
            get { return this.applicationuserid; }
        }

        private DateTime? sentdatetime;
        public DateTime? SentDateTime
        {
            set
            {
                if (this.sentdatetime == value)
                    return;
                this.sentdatetime = value;
                this.OnPropertyChanged("SentDateTime");
            }
            get { return this.sentdatetime; }
        }

        private string responsecode;
        public string ResponseCode
        {
            set
            {
                if (this.responsecode == value)
                    return;
                this.responsecode = value;
                this.OnPropertyChanged("ResponseCode");
            }
            get { return this.responsecode; }
        }

        private Guid? shiftid;
        public Guid? ShiftId
        {
            set
            {
                if (this.shiftid == value)
                    return;
                this.shiftid = value;
                this.OnPropertyChanged("ShiftId");
            }
            get { return this.shiftid; }
        }

        private bool? iserrorresolving;
        public bool? IsErrorResolving
        {
            set
            {
                if (this.iserrorresolving == value)
                    return;
                this.iserrorresolving = value;
                this.OnPropertyChanged("IsErrorResolving");
            }
            get { return this.iserrorresolving; }
        }

        private bool? invalidsale;
        public bool? InvalidSale
        {
            set
            {
                if (this.invalidsale == value)
                    return;
                this.invalidsale = value;
                this.OnPropertyChanged("InvalidSale");
            }
            get { return this.invalidsale; }
        }

        private decimal? discountpercentage;
        public decimal? DiscountPercentage
        {
            set
            {
                if (this.discountpercentage == value)
                    return;
                this.discountpercentage = value;
                this.OnPropertyChanged("DiscountPercentage");
            }
            get { return this.discountpercentage; }
        }

    }

    public partial class SalesViewViewModel : BaseViewModel<Data.SalesView>
    {
    }

    public partial class SendLogViewModel : BaseViewModel<Data.SendLog>
    {
        private Guid sendlogid;
        [PrimaryKey]
        public Guid SendLogId
        {
            set
            {
                if (this.sendlogid == value)
                    return;
                this.sendlogid = value;
                this.OnPropertyChanged("SendLogId");
            }
            get { return this.sendlogid; }
        }

        private DateTime senddate;
        public DateTime SendDate
        {
            set
            {
                if (this.senddate == value)
                    return;
                this.senddate = value;
                this.OnPropertyChanged("SendDate");
            }
            get { return this.senddate; }
        }

        private string action;
        public string Action
        {
            set
            {
                if (this.action == value)
                    return;
                this.action = value;
                this.OnPropertyChanged("Action");
            }
            get { return this.action; }
        }

        private string senddata;
        public string SendData
        {
            set
            {
                if (this.senddata == value)
                    return;
                this.senddata = value;
                this.OnPropertyChanged("SendData");
            }
            get { return this.senddata; }
        }

        private int? sentstatus;
        public int? SentStatus
        {
            set
            {
                if (this.sentstatus == value)
                    return;
                this.sentstatus = value;
                this.OnPropertyChanged("SentStatus");
            }
            get { return this.sentstatus; }
        }

        private DateTime? lastsent;
        public DateTime? LastSent
        {
            set
            {
                if (this.lastsent == value)
                    return;
                this.lastsent = value;
                this.OnPropertyChanged("LastSent");
            }
            get { return this.lastsent; }
        }

        private string entityidentity;
        public string EntityIdentity
        {
            set
            {
                if (this.entityidentity == value)
                    return;
                this.entityidentity = value;
                this.OnPropertyChanged("EntityIdentity");
            }
            get { return this.entityidentity; }
        }

    }

    public partial class ShiftViewModel : BaseViewModel<Data.Shift>
    {
        private Guid shiftid;
        [PrimaryKey]
        public Guid ShiftId
        {
            set
            {
                if (this.shiftid == value)
                    return;
                this.shiftid = value;
                this.OnPropertyChanged("ShiftId");
            }
            get { return this.shiftid; }
        }

        private Guid applicationuserid;
        public Guid ApplicationUserId
        {
            set
            {
                if (this.applicationuserid == value)
                    return;
                this.applicationuserid = value;
                this.OnPropertyChanged("ApplicationUserId");
            }
            get { return this.applicationuserid; }
        }

        private DateTime shiftbegin;
        public DateTime ShiftBegin
        {
            set
            {
                if (this.shiftbegin == value)
                    return;
                this.shiftbegin = value;
                this.OnPropertyChanged("ShiftBegin");
            }
            get { return this.shiftbegin; }
        }

        private DateTime? shiftend;
        public DateTime? ShiftEnd
        {
            set
            {
                if (this.shiftend == value)
                    return;
                this.shiftend = value;
                this.OnPropertyChanged("ShiftEnd");
            }
            get { return this.shiftend; }
        }

    }

    public partial class SystemEventViewModel : BaseViewModel<Data.SystemEvent>
    {
        private Guid eventid;
        [PrimaryKey]
        public Guid EventId
        {
            set
            {
                if (this.eventid == value)
                    return;
                this.eventid = value;
                this.OnPropertyChanged("EventId");
            }
            get { return this.eventid; }
        }

        private int eventtype;
        public int EventType
        {
            set
            {
                if (this.eventtype == value)
                    return;
                this.eventtype = value;
                this.OnPropertyChanged("EventType");
            }
            get { return this.eventtype; }
        }

        private string message;
        public string Message
        {
            set
            {
                if (this.message == value)
                    return;
                this.message = value;
                this.OnPropertyChanged("Message");
            }
            get { return this.message; }
        }

        private DateTime eventdate;
        public DateTime EventDate
        {
            set
            {
                if (this.eventdate == value)
                    return;
                this.eventdate = value;
                this.OnPropertyChanged("EventDate");
            }
            get { return this.eventdate; }
        }

        private Guid? nozzleid;
        public Guid? NozzleId
        {
            set
            {
                if (this.nozzleid == value)
                    return;
                this.nozzleid = value;
                this.OnPropertyChanged("NozzleId");
            }
            get { return this.nozzleid; }
        }

        private Guid? tankid;
        public Guid? TankId
        {
            set
            {
                if (this.tankid == value)
                    return;
                this.tankid = value;
                this.OnPropertyChanged("TankId");
            }
            get { return this.tankid; }
        }

        private Guid? dispenserid;
        public Guid? DispenserId
        {
            set
            {
                if (this.dispenserid == value)
                    return;
                this.dispenserid = value;
                this.OnPropertyChanged("DispenserId");
            }
            get { return this.dispenserid; }
        }

        private Guid? alertdefinitionid;
        public Guid? AlertDefinitionId
        {
            set
            {
                if (this.alertdefinitionid == value)
                    return;
                this.alertdefinitionid = value;
                this.OnPropertyChanged("AlertDefinitionId");
            }
            get { return this.alertdefinitionid; }
        }

        private DateTime? sentdate;
        public DateTime? SentDate
        {
            set
            {
                if (this.sentdate == value)
                    return;
                this.sentdate = value;
                this.OnPropertyChanged("SentDate");
            }
            get { return this.sentdate; }
        }

        private DateTime? resolveddate;
        public DateTime? ResolvedDate
        {
            set
            {
                if (this.resolveddate == value)
                    return;
                this.resolveddate = value;
                this.OnPropertyChanged("ResolvedDate");
            }
            get { return this.resolveddate; }
        }

        private string resolvemessage;
        public string ResolveMessage
        {
            set
            {
                if (this.resolvemessage == value)
                    return;
                this.resolvemessage = value;
                this.OnPropertyChanged("ResolveMessage");
            }
            get { return this.resolvemessage; }
        }

        private Int64 crc;
        public Int64 CRC
        {
            set
            {
                if (this.crc == value)
                    return;
                this.crc = value;
                this.OnPropertyChanged("CRC");
            }
            get { return this.crc; }
        }

        private int? alarmtype;
        public int? AlarmType
        {
            set
            {
                if (this.alarmtype == value)
                    return;
                this.alarmtype = value;
                this.OnPropertyChanged("AlarmType");
            }
            get { return this.alarmtype; }
        }

        private DateTime? printeddate;
        public DateTime? PrintedDate
        {
            set
            {
                if (this.printeddate == value)
                    return;
                this.printeddate = value;
                this.OnPropertyChanged("PrintedDate");
            }
            get { return this.printeddate; }
        }

        private string documentsign;
        public string DocumentSign
        {
            set
            {
                if (this.documentsign == value)
                    return;
                this.documentsign = value;
                this.OnPropertyChanged("DocumentSign");
            }
            get { return this.documentsign; }
        }

    }

    public partial class SystemEventDataViewModel : BaseViewModel<Data.SystemEventDatum>
    {
        private Guid systemeventdataid;
        [PrimaryKey]
        public Guid SystemEventDataId
        {
            set
            {
                if (this.systemeventdataid == value)
                    return;
                this.systemeventdataid = value;
                this.OnPropertyChanged("SystemEventDataId");
            }
            get { return this.systemeventdataid; }
        }

        private Guid systemeventid;
        public Guid SystemEventId
        {
            set
            {
                if (this.systemeventid == value)
                    return;
                this.systemeventid = value;
                this.OnPropertyChanged("SystemEventId");
            }
            get { return this.systemeventid; }
        }

        private string propertyname;
        public string PropertyName
        {
            set
            {
                if (this.propertyname == value)
                    return;
                this.propertyname = value;
                this.OnPropertyChanged("PropertyName");
            }
            get { return this.propertyname; }
        }

        private string value;
        public string Value
        {
            set
            {
                if (this.value == value)
                    return;
                this.value = value;
                this.OnPropertyChanged("Value");
            }
            get { return this.value; }
        }

        private bool isalerttrigger;
        public bool IsAlertTrigger
        {
            set
            {
                if (this.isalerttrigger == value)
                    return;
                this.isalerttrigger = value;
                this.OnPropertyChanged("IsAlertTrigger");
            }
            get { return this.isalerttrigger; }
        }

    }

    public partial class TankViewModel : BaseViewModel<Data.Tank>
    {
        private Guid tankid;
        [PrimaryKey]
        public Guid TankId
        {
            set
            {
                if (this.tankid == value)
                    return;
                this.tankid = value;
                this.OnPropertyChanged("TankId");
            }
            get { return this.tankid; }
        }

        private Guid fueltypeid;
        public Guid FuelTypeId
        {
            set
            {
                if (this.fueltypeid == value)
                    return;
                this.fueltypeid = value;
                this.OnPropertyChanged("FuelTypeId");
            }
            get { return this.fueltypeid; }
        }

        private decimal totalvolume;
        public decimal TotalVolume
        {
            set
            {
                if (this.totalvolume == value)
                    return;
                this.totalvolume = value;
                this.OnPropertyChanged("TotalVolume");
            }
            get { return this.totalvolume; }
        }

        private decimal offsetvolume;
        public decimal OffsetVolume
        {
            set
            {
                if (this.offsetvolume == value)
                    return;
                this.offsetvolume = value;
                this.OnPropertyChanged("OffsetVolume");
            }
            get { return this.offsetvolume; }
        }

        private int physicalstate;
        public int PhysicalState
        {
            set
            {
                if (this.physicalstate == value)
                    return;
                this.physicalstate = value;
                this.OnPropertyChanged("PhysicalState");
            }
            get { return this.physicalstate; }
        }

        private int channel;
        public int Channel
        {
            set
            {
                if (this.channel == value)
                    return;
                this.channel = value;
                this.OnPropertyChanged("Channel");
            }
            get { return this.channel; }
        }

        private int address;
        public int Address
        {
            set
            {
                if (this.address == value)
                    return;
                this.address = value;
                this.OnPropertyChanged("Address");
            }
            get { return this.address; }
        }

        private Guid atgprobetypeid;
        public Guid AtgProbeTypeId
        {
            set
            {
                if (this.atgprobetypeid == value)
                    return;
                this.atgprobetypeid = value;
                this.OnPropertyChanged("AtgProbeTypeId");
            }
            get { return this.atgprobetypeid; }
        }

        private Guid communicationcontrollerid;
        public Guid CommunicationControllerId
        {
            set
            {
                if (this.communicationcontrollerid == value)
                    return;
                this.communicationcontrollerid = value;
                this.OnPropertyChanged("CommunicationControllerId");
            }
            get { return this.communicationcontrollerid; }
        }

        private decimal maxwaterheight;
        public decimal MaxWaterHeight
        {
            set
            {
                if (this.maxwaterheight == value)
                    return;
                this.maxwaterheight = value;
                this.OnPropertyChanged("MaxWaterHeight");
            }
            get { return this.maxwaterheight; }
        }

        private decimal minfuelheight;
        public decimal MinFuelHeight
        {
            set
            {
                if (this.minfuelheight == value)
                    return;
                this.minfuelheight = value;
                this.OnPropertyChanged("MinFuelHeight");
            }
            get { return this.minfuelheight; }
        }

        private decimal maxfuelheight;
        public decimal MaxFuelHeight
        {
            set
            {
                if (this.maxfuelheight == value)
                    return;
                this.maxfuelheight = value;
                this.OnPropertyChanged("MaxFuelHeight");
            }
            get { return this.maxfuelheight; }
        }

        private int tanknumber;
        public int TankNumber
        {
            set
            {
                if (this.tanknumber == value)
                    return;
                this.tanknumber = value;
                this.OnPropertyChanged("TankNumber");
            }
            get { return this.tanknumber; }
        }

        private string tankserialnumber;
        public string TankSerialNumber
        {
            set
            {
                if (this.tankserialnumber == value)
                    return;
                this.tankserialnumber = value;
                this.OnPropertyChanged("TankSerialNumber");
            }
            get { return this.tankserialnumber; }
        }

        private decimal fuellevel;
        public decimal FuelLevel
        {
            set
            {
                if (this.fuellevel == value)
                    return;
                this.fuellevel = value;
                this.OnPropertyChanged("FuelLevel");
            }
            get { return this.fuellevel; }
        }

        private decimal waterlevel;
        public decimal WaterLevel
        {
            set
            {
                if (this.waterlevel == value)
                    return;
                this.waterlevel = value;
                this.OnPropertyChanged("WaterLevel");
            }
            get { return this.waterlevel; }
        }

        private decimal temperatire;
        public decimal Temperatire
        {
            set
            {
                if (this.temperatire == value)
                    return;
                this.temperatire = value;
                this.OnPropertyChanged("Temperatire");
            }
            get { return this.temperatire; }
        }

        private decimal offestwater;
        public decimal OffestWater
        {
            set
            {
                if (this.offestwater == value)
                    return;
                this.offestwater = value;
                this.OnPropertyChanged("OffestWater");
            }
            get { return this.offestwater; }
        }

        private bool? isvirtual;
        public bool? IsVirtual
        {
            set
            {
                if (this.isvirtual == value)
                    return;
                this.isvirtual = value;
                this.OnPropertyChanged("IsVirtual");
            }
            get { return this.isvirtual; }
        }

        private decimal? orderlimit;
        public decimal? OrderLimit
        {
            set
            {
                if (this.orderlimit == value)
                    return;
                this.orderlimit = value;
                this.OnPropertyChanged("OrderLimit");
            }
            get { return this.orderlimit; }
        }

        private int? alarmthreshold;
        public int? AlarmThreshold
        {
            set
            {
                if (this.alarmthreshold == value)
                    return;
                this.alarmthreshold = value;
                this.OnPropertyChanged("AlarmThreshold");
            }
            get { return this.alarmthreshold; }
        }

    }

    public partial class TankCheckViewModel : BaseViewModel<Data.TankCheck>
    {
        private Guid tankcheckid;
        [PrimaryKey]
        public Guid TankCheckId
        {
            set
            {
                if (this.tankcheckid == value)
                    return;
                this.tankcheckid = value;
                this.OnPropertyChanged("TankCheckId");
            }
            get { return this.tankcheckid; }
        }

        private Guid tankid;
        public Guid TankId
        {
            set
            {
                if (this.tankid == value)
                    return;
                this.tankid = value;
                this.OnPropertyChanged("TankId");
            }
            get { return this.tankid; }
        }

        private decimal tanklevel;
        public decimal TankLevel
        {
            set
            {
                if (this.tanklevel == value)
                    return;
                this.tanklevel = value;
                this.OnPropertyChanged("TankLevel");
            }
            get { return this.tanklevel; }
        }

        private DateTime checkdate;
        public DateTime CheckDate
        {
            set
            {
                if (this.checkdate == value)
                    return;
                this.checkdate = value;
                this.OnPropertyChanged("CheckDate");
            }
            get { return this.checkdate; }
        }

        private decimal? temperature;
        public decimal? Temperature
        {
            set
            {
                if (this.temperature == value)
                    return;
                this.temperature = value;
                this.OnPropertyChanged("Temperature");
            }
            get { return this.temperature; }
        }

        private DateTime? sentdatetime;
        public DateTime? SentDatetime
        {
            set
            {
                if (this.sentdatetime == value)
                    return;
                this.sentdatetime = value;
                this.OnPropertyChanged("SentDatetime");
            }
            get { return this.sentdatetime; }
        }

    }

    public partial class TankFillingViewModel : BaseViewModel<Data.TankFilling>
    {
        private Guid tankfillingid;
        [PrimaryKey]
        public Guid TankFillingId
        {
            set
            {
                if (this.tankfillingid == value)
                    return;
                this.tankfillingid = value;
                this.OnPropertyChanged("TankFillingId");
            }
            get { return this.tankfillingid; }
        }

        private Guid usageperiodid;
        public Guid UsagePeriodId
        {
            set
            {
                if (this.usageperiodid == value)
                    return;
                this.usageperiodid = value;
                this.OnPropertyChanged("UsagePeriodId");
            }
            get { return this.usageperiodid; }
        }

        private Guid tankid;
        public Guid TankId
        {
            set
            {
                if (this.tankid == value)
                    return;
                this.tankid = value;
                this.OnPropertyChanged("TankId");
            }
            get { return this.tankid; }
        }

        private Guid tankpriceid;
        public Guid TankPriceId
        {
            set
            {
                if (this.tankpriceid == value)
                    return;
                this.tankpriceid = value;
                this.OnPropertyChanged("TankPriceId");
            }
            get { return this.tankpriceid; }
        }

        private DateTime transactiontime;
        public DateTime TransactionTime
        {
            set
            {
                if (this.transactiontime == value)
                    return;
                this.transactiontime = value;
                this.OnPropertyChanged("TransactionTime");
            }
            get { return this.transactiontime; }
        }

        private decimal volume;
        public decimal Volume
        {
            set
            {
                if (this.volume == value)
                    return;
                this.volume = value;
                this.OnPropertyChanged("Volume");
            }
            get { return this.volume; }
        }

        private decimal volumenormalized;
        public decimal VolumeNormalized
        {
            set
            {
                if (this.volumenormalized == value)
                    return;
                this.volumenormalized = value;
                this.OnPropertyChanged("VolumeNormalized");
            }
            get { return this.volumenormalized; }
        }

        private decimal tanktemperaturestart;
        public decimal TankTemperatureStart
        {
            set
            {
                if (this.tanktemperaturestart == value)
                    return;
                this.tanktemperaturestart = value;
                this.OnPropertyChanged("TankTemperatureStart");
            }
            get { return this.tanktemperaturestart; }
        }

        private decimal tanktemperatureend;
        public decimal TankTemperatureEnd
        {
            set
            {
                if (this.tanktemperatureend == value)
                    return;
                this.tanktemperatureend = value;
                this.OnPropertyChanged("TankTemperatureEnd");
            }
            get { return this.tanktemperatureend; }
        }

        private bool islocked;
        public bool IsLocked
        {
            set
            {
                if (this.islocked == value)
                    return;
                this.islocked = value;
                this.OnPropertyChanged("IsLocked");
            }
            get { return this.islocked; }
        }

        private Int64 crc;
        public Int64 CRC
        {
            set
            {
                if (this.crc == value)
                    return;
                this.crc = value;
                this.OnPropertyChanged("CRC");
            }
            get { return this.crc; }
        }

        private DateTime transactiontimeend;
        public DateTime TransactionTimeEnd
        {
            set
            {
                if (this.transactiontimeend == value)
                    return;
                this.transactiontimeend = value;
                this.OnPropertyChanged("TransactionTimeEnd");
            }
            get { return this.transactiontimeend; }
        }

        private decimal levelstart;
        public decimal LevelStart
        {
            set
            {
                if (this.levelstart == value)
                    return;
                this.levelstart = value;
                this.OnPropertyChanged("LevelStart");
            }
            get { return this.levelstart; }
        }

        private decimal levelend;
        public decimal LevelEnd
        {
            set
            {
                if (this.levelend == value)
                    return;
                this.levelend = value;
                this.OnPropertyChanged("LevelEnd");
            }
            get { return this.levelend; }
        }

        private decimal fueldensity;
        public decimal FuelDensity
        {
            set
            {
                if (this.fueldensity == value)
                    return;
                this.fueldensity = value;
                this.OnPropertyChanged("FuelDensity");
            }
            get { return this.fueldensity; }
        }

        private decimal volumereal;
        public decimal VolumeReal
        {
            set
            {
                if (this.volumereal == value)
                    return;
                this.volumereal = value;
                this.OnPropertyChanged("VolumeReal");
            }
            get { return this.volumereal; }
        }

        private decimal volumerealnormalized;
        public decimal VolumeRealNormalized
        {
            set
            {
                if (this.volumerealnormalized == value)
                    return;
                this.volumerealnormalized = value;
                this.OnPropertyChanged("VolumeRealNormalized");
            }
            get { return this.volumerealnormalized; }
        }

        private Guid applicationuserid;
        public Guid ApplicationUserId
        {
            set
            {
                if (this.applicationuserid == value)
                    return;
                this.applicationuserid = value;
                this.OnPropertyChanged("ApplicationUserId");
            }
            get { return this.applicationuserid; }
        }

        private DateTime? sentdatetime;
        public DateTime? SentDateTime
        {
            set
            {
                if (this.sentdatetime == value)
                    return;
                this.sentdatetime = value;
                this.OnPropertyChanged("SentDateTime");
            }
            get { return this.sentdatetime; }
        }

        private string responsecode;
        public string ResponseCode
        {
            set
            {
                if (this.responsecode == value)
                    return;
                this.responsecode = value;
                this.OnPropertyChanged("ResponseCode");
            }
            get { return this.responsecode; }
        }

        private string signsignature;
        public string SignSignature
        {
            set
            {
                if (this.signsignature == value)
                    return;
                this.signsignature = value;
                this.OnPropertyChanged("SignSignature");
            }
            get { return this.signsignature; }
        }

    }

    public partial class TankFillingInvoiceViewViewModel : BaseViewModel<Data.TankFillingInvoiceView>
    {
    }

    public partial class TankFillingViewViewModel : BaseViewModel<Data.TankFillingView>
    {
    }

    public partial class TankLevelEndViewViewModel : BaseViewModel<Data.TankLevelEndView>
    {
    }

    public partial class TankLevelStartViewViewModel : BaseViewModel<Data.TankLevelStartView>
    {
    }

    public partial class TankPriceViewModel : BaseViewModel<Data.TankPrice>
    {
        private Guid tankpriceid;
        [PrimaryKey]
        public Guid TankPriceId
        {
            set
            {
                if (this.tankpriceid == value)
                    return;
                this.tankpriceid = value;
                this.OnPropertyChanged("TankPriceId");
            }
            get { return this.tankpriceid; }
        }

        private Guid tankid;
        public Guid TankId
        {
            set
            {
                if (this.tankid == value)
                    return;
                this.tankid = value;
                this.OnPropertyChanged("TankId");
            }
            get { return this.tankid; }
        }

        private decimal price;
        public decimal Price
        {
            set
            {
                if (this.price == value)
                    return;
                this.price = value;
                this.OnPropertyChanged("Price");
            }
            get { return this.price; }
        }

        private DateTime changedate;
        public DateTime ChangeDate
        {
            set
            {
                if (this.changedate == value)
                    return;
                this.changedate = value;
                this.OnPropertyChanged("ChangeDate");
            }
            get { return this.changedate; }
        }

        private decimal fueldensity;
        public decimal FuelDensity
        {
            set
            {
                if (this.fueldensity == value)
                    return;
                this.fueldensity = value;
                this.OnPropertyChanged("FuelDensity");
            }
            get { return this.fueldensity; }
        }

    }

    public partial class TankSaleViewModel : BaseViewModel<Data.TankSale>
    {
        private Guid tanksaleid;
        [PrimaryKey]
        public Guid TankSaleId
        {
            set
            {
                if (this.tanksaleid == value)
                    return;
                this.tanksaleid = value;
                this.OnPropertyChanged("TankSaleId");
            }
            get { return this.tanksaleid; }
        }

        private Guid salestransactionid;
        public Guid SalesTransactionId
        {
            set
            {
                if (this.salestransactionid == value)
                    return;
                this.salestransactionid = value;
                this.OnPropertyChanged("SalesTransactionId");
            }
            get { return this.salestransactionid; }
        }

        private Guid tankid;
        public Guid TankId
        {
            set
            {
                if (this.tankid == value)
                    return;
                this.tankid = value;
                this.OnPropertyChanged("TankId");
            }
            get { return this.tankid; }
        }

        private decimal startvolume;
        public decimal StartVolume
        {
            set
            {
                if (this.startvolume == value)
                    return;
                this.startvolume = value;
                this.OnPropertyChanged("StartVolume");
            }
            get { return this.startvolume; }
        }

        private decimal? endvolume;
        public decimal? EndVolume
        {
            set
            {
                if (this.endvolume == value)
                    return;
                this.endvolume = value;
                this.OnPropertyChanged("EndVolume");
            }
            get { return this.endvolume; }
        }

        private decimal startvolumenormalized;
        public decimal StartVolumeNormalized
        {
            set
            {
                if (this.startvolumenormalized == value)
                    return;
                this.startvolumenormalized = value;
                this.OnPropertyChanged("StartVolumeNormalized");
            }
            get { return this.startvolumenormalized; }
        }

        private decimal? endvolumenormalized;
        public decimal? EndVolumeNormalized
        {
            set
            {
                if (this.endvolumenormalized == value)
                    return;
                this.endvolumenormalized = value;
                this.OnPropertyChanged("EndVolumeNormalized");
            }
            get { return this.endvolumenormalized; }
        }

        private decimal? starttemperature;
        public decimal? StartTemperature
        {
            set
            {
                if (this.starttemperature == value)
                    return;
                this.starttemperature = value;
                this.OnPropertyChanged("StartTemperature");
            }
            get { return this.starttemperature; }
        }

        private decimal endtemperature;
        public decimal EndTemperature
        {
            set
            {
                if (this.endtemperature == value)
                    return;
                this.endtemperature = value;
                this.OnPropertyChanged("EndTemperature");
            }
            get { return this.endtemperature; }
        }

        private Int64 crc;
        public Int64 CRC
        {
            set
            {
                if (this.crc == value)
                    return;
                this.crc = value;
                this.OnPropertyChanged("CRC");
            }
            get { return this.crc; }
        }

        private decimal startlevel;
        public decimal StartLevel
        {
            set
            {
                if (this.startlevel == value)
                    return;
                this.startlevel = value;
                this.OnPropertyChanged("StartLevel");
            }
            get { return this.startlevel; }
        }

        private decimal? endlevel;
        public decimal? EndLevel
        {
            set
            {
                if (this.endlevel == value)
                    return;
                this.endlevel = value;
                this.OnPropertyChanged("EndLevel");
            }
            get { return this.endlevel; }
        }

        private decimal fueldensity;
        public decimal FuelDensity
        {
            set
            {
                if (this.fueldensity == value)
                    return;
                this.fueldensity = value;
                this.OnPropertyChanged("FuelDensity");
            }
            get { return this.fueldensity; }
        }

    }

    public partial class TankSaleViewViewModel : BaseViewModel<Data.TankSaleView>
    {
    }

    public partial class TankSettingViewModel : BaseViewModel<Data.TankSetting>
    {
        private Guid tanksettingid;
        [PrimaryKey]
        public Guid TankSettingId
        {
            set
            {
                if (this.tanksettingid == value)
                    return;
                this.tanksettingid = value;
                this.OnPropertyChanged("TankSettingId");
            }
            get { return this.tanksettingid; }
        }

        private Guid tankid;
        public Guid TankId
        {
            set
            {
                if (this.tankid == value)
                    return;
                this.tankid = value;
                this.OnPropertyChanged("TankId");
            }
            get { return this.tankid; }
        }

        private string settingkey;
        public string SettingKey
        {
            set
            {
                if (this.settingkey == value)
                    return;
                this.settingkey = value;
                this.OnPropertyChanged("SettingKey");
            }
            get { return this.settingkey; }
        }

        private string settingvalue;
        public string SettingValue
        {
            set
            {
                if (this.settingvalue == value)
                    return;
                this.settingvalue = value;
                this.OnPropertyChanged("SettingValue");
            }
            get { return this.settingvalue; }
        }

        private string description;
        public string Description
        {
            set
            {
                if (this.description == value)
                    return;
                this.description = value;
                this.OnPropertyChanged("Description");
            }
            get { return this.description; }
        }

    }

    public partial class TankUsagePeriodViewModel : BaseViewModel<Data.TankUsagePeriod>
    {
        private Guid tankusageperiodid;
        [PrimaryKey]
        public Guid TankUsagePeriodId
        {
            set
            {
                if (this.tankusageperiodid == value)
                    return;
                this.tankusageperiodid = value;
                this.OnPropertyChanged("TankUsagePeriodId");
            }
            get { return this.tankusageperiodid; }
        }

        private Guid tankid;
        public Guid TankId
        {
            set
            {
                if (this.tankid == value)
                    return;
                this.tankid = value;
                this.OnPropertyChanged("TankId");
            }
            get { return this.tankid; }
        }

        private Guid usageperiodid;
        public Guid UsagePeriodId
        {
            set
            {
                if (this.usageperiodid == value)
                    return;
                this.usageperiodid = value;
                this.OnPropertyChanged("UsagePeriodId");
            }
            get { return this.usageperiodid; }
        }

        private DateTime measurementdatetime;
        public DateTime MeasurementDateTime
        {
            set
            {
                if (this.measurementdatetime == value)
                    return;
                this.measurementdatetime = value;
                this.OnPropertyChanged("MeasurementDateTime");
            }
            get { return this.measurementdatetime; }
        }

        private DateTime expirationdate;
        public DateTime ExpirationDate
        {
            set
            {
                if (this.expirationdate == value)
                    return;
                this.expirationdate = value;
                this.OnPropertyChanged("ExpirationDate");
            }
            get { return this.expirationdate; }
        }

        private decimal totalvolume;
        public decimal TotalVolume
        {
            set
            {
                if (this.totalvolume == value)
                    return;
                this.totalvolume = value;
                this.OnPropertyChanged("TotalVolume");
            }
            get { return this.totalvolume; }
        }

        private Int64 crc;
        public Int64 CRC
        {
            set
            {
                if (this.crc == value)
                    return;
                this.crc = value;
                this.OnPropertyChanged("CRC");
            }
            get { return this.crc; }
        }

    }

    public partial class TitrimetryViewModel : BaseViewModel<Data.Titrimetry>
    {
        private Guid titrimetryid;
        [PrimaryKey]
        public Guid TitrimetryId
        {
            set
            {
                if (this.titrimetryid == value)
                    return;
                this.titrimetryid = value;
                this.OnPropertyChanged("TitrimetryId");
            }
            get { return this.titrimetryid; }
        }

        private Guid? tankid;
        public Guid? TankId
        {
            set
            {
                if (this.tankid == value)
                    return;
                this.tankid = value;
                this.OnPropertyChanged("TankId");
            }
            get { return this.tankid; }
        }

        private DateTime? titrationdate;
        public DateTime? TitrationDate
        {
            set
            {
                if (this.titrationdate == value)
                    return;
                this.titrationdate = value;
                this.OnPropertyChanged("TitrationDate");
            }
            get { return this.titrationdate; }
        }

        private DateTime? printdate;
        public DateTime? PrintDate
        {
            set
            {
                if (this.printdate == value)
                    return;
                this.printdate = value;
                this.OnPropertyChanged("PrintDate");
            }
            get { return this.printdate; }
        }

        private string documentsign;
        public string DocumentSign
        {
            set
            {
                if (this.documentsign == value)
                    return;
                this.documentsign = value;
                this.OnPropertyChanged("DocumentSign");
            }
            get { return this.documentsign; }
        }

        private decimal? uncertaintylevel;
        public decimal? UncertaintyLevel
        {
            set
            {
                if (this.uncertaintylevel == value)
                    return;
                this.uncertaintylevel = value;
                this.OnPropertyChanged("UncertaintyLevel");
            }
            get { return this.uncertaintylevel; }
        }

    }

    public partial class TitrimetryLevelViewModel : BaseViewModel<Data.TitrimetryLevel>
    {
        private Guid titrimetrylevelid;
        [PrimaryKey]
        public Guid TitrimetryLevelId
        {
            set
            {
                if (this.titrimetrylevelid == value)
                    return;
                this.titrimetrylevelid = value;
                this.OnPropertyChanged("TitrimetryLevelId");
            }
            get { return this.titrimetrylevelid; }
        }

        private Guid? titrimetryid;
        public Guid? TitrimetryId
        {
            set
            {
                if (this.titrimetryid == value)
                    return;
                this.titrimetryid = value;
                this.OnPropertyChanged("TitrimetryId");
            }
            get { return this.titrimetryid; }
        }

        private decimal? height;
        public decimal? Height
        {
            set
            {
                if (this.height == value)
                    return;
                this.height = value;
                this.OnPropertyChanged("Height");
            }
            get { return this.height; }
        }

        private decimal? volume;
        public decimal? Volume
        {
            set
            {
                if (this.volume == value)
                    return;
                this.volume = value;
                this.OnPropertyChanged("Volume");
            }
            get { return this.volume; }
        }

        private decimal? uncertaintyvolume;
        public decimal? UncertaintyVolume
        {
            set
            {
                if (this.uncertaintyvolume == value)
                    return;
                this.uncertaintyvolume = value;
                this.OnPropertyChanged("UncertaintyVolume");
            }
            get { return this.uncertaintyvolume; }
        }

        private decimal? uncertaintypercent;
        public decimal? UncertaintyPercent
        {
            set
            {
                if (this.uncertaintypercent == value)
                    return;
                this.uncertaintypercent = value;
                this.OnPropertyChanged("UncertaintyPercent");
            }
            get { return this.uncertaintypercent; }
        }

    }

    public partial class TraderViewModel : BaseViewModel<Data.Trader>
    {
        private Guid traderid;
        [PrimaryKey]
        public Guid TraderId
        {
            set
            {
                if (this.traderid == value)
                    return;
                this.traderid = value;
                this.OnPropertyChanged("TraderId");
            }
            get { return this.traderid; }
        }

        private string name;
        public string Name
        {
            set
            {
                if (this.name == value)
                    return;
                this.name = value;
                this.OnPropertyChanged("Name");
            }
            get { return this.name; }
        }

        private string taxregistrationnumber;
        public string TaxRegistrationNumber
        {
            set
            {
                if (this.taxregistrationnumber == value)
                    return;
                this.taxregistrationnumber = value;
                this.OnPropertyChanged("TaxRegistrationNumber");
            }
            get { return this.taxregistrationnumber; }
        }

        private string taxregistrationoffice;
        public string TaxRegistrationOffice
        {
            set
            {
                if (this.taxregistrationoffice == value)
                    return;
                this.taxregistrationoffice = value;
                this.OnPropertyChanged("TaxRegistrationOffice");
            }
            get { return this.taxregistrationoffice; }
        }

        private string address;
        public string Address
        {
            set
            {
                if (this.address == value)
                    return;
                this.address = value;
                this.OnPropertyChanged("Address");
            }
            get { return this.address; }
        }

        private string city;
        public string City
        {
            set
            {
                if (this.city == value)
                    return;
                this.city = value;
                this.OnPropertyChanged("City");
            }
            get { return this.city; }
        }

        private string zipCode;
        public string ZipCode
        {
            set
            {
                if (this.zipCode == value)
                    return;
                this.zipCode = value;
                this.OnPropertyChanged("ZipCode");
            }
            get { return this.zipCode; }
        }

        private string country;
        public string Country
        {
            set
            {
                if (this.country == value)
                    return;
                this.country = value;
                this.OnPropertyChanged("Country");
            }
            get { return this.country; }
        }

        private string phone1;
        public string Phone1
        {
            set
            {
                if (this.phone1 == value)
                    return;
                this.phone1 = value;
                this.OnPropertyChanged("Phone1");
            }
            get { return this.phone1; }
        }

        private string phone2;
        public string Phone2
        {
            set
            {
                if (this.phone2 == value)
                    return;
                this.phone2 = value;
                this.OnPropertyChanged("Phone2");
            }
            get { return this.phone2; }
        }

        private string fax;
        public string Fax
        {
            set
            {
                if (this.fax == value)
                    return;
                this.fax = value;
                this.OnPropertyChanged("Fax");
            }
            get { return this.fax; }
        }

        private string email;
        public string Email
        {
            set
            {
                if (this.email == value)
                    return;
                this.email = value;
                this.OnPropertyChanged("Email");
            }
            get { return this.email; }
        }

        private string _website;
        public string website
        {
            set
            {
                if (this._website == value)
                    return;
                this._website = value;
                this.OnPropertyChanged("website");
            }
            get { return this._website; }
        }

        private Guid? invoicetypeid;
        public Guid? InvoiceTypeId
        {
            set
            {
                if (this.invoicetypeid == value)
                    return;
                this.invoicetypeid = value;
                this.OnPropertyChanged("InvoiceTypeId");
            }
            get { return this.invoicetypeid; }
        }

        private Guid? pricelistid;
        public Guid? PriceListId
        {
            set
            {
                if (this.pricelistid == value)
                    return;
                this.pricelistid = value;
                this.OnPropertyChanged("PriceListId");
            }
            get { return this.pricelistid; }
        }

        private bool iscustomer;
        public bool IsCustomer
        {
            set
            {
                if (this.iscustomer == value)
                    return;
                this.iscustomer = value;
                this.OnPropertyChanged("IsCustomer");
            }
            get { return this.iscustomer; }
        }

        private bool issupplier;
        public bool IsSupplier
        {
            set
            {
                if (this.issupplier == value)
                    return;
                this.issupplier = value;
                this.OnPropertyChanged("IsSupplier");
            }
            get { return this.issupplier; }
        }

        private bool? vatexemption;
        public bool? VatExemption
        {
            set
            {
                if (this.vatexemption == value)
                    return;
                this.vatexemption = value;
                this.OnPropertyChanged("VatExemption");
            }
            get { return this.vatexemption; }
        }

        private int? paymenttype;
        public int? PaymentType
        {
            set
            {
                if (this.paymenttype == value)
                    return;
                this.paymenttype = value;
                this.OnPropertyChanged("PaymentType");
            }
            get { return this.paymenttype; }
        }

        private string vatexemptionreason;
        public string VatExemptionReason
        {
            set
            {
                if (this.vatexemptionreason == value)
                    return;
                this.vatexemptionreason = value;
                this.OnPropertyChanged("VatExemptionReason");
            }
            get { return this.vatexemptionreason; }
        }

        private string occupation;
        public string Occupation
        {
            set
            {
                if (this.occupation == value)
                    return;
                this.occupation = value;
                this.OnPropertyChanged("Occupation");
            }
            get { return this.occupation; }
        }

        private string supplyNumber;
        public string SupplyNumber
        {
            set
            {
                if (this.supplyNumber == value)
                    return;
                this.supplyNumber = value;
                this.OnPropertyChanged("SupplyNumber");
            }
            get { return this.supplyNumber; }
        }

        private bool? printdebtoninvoice;
        public bool? PrintDebtOnInvoice
        {
            set
            {
                if (this.printdebtoninvoice == value)
                    return;
                this.printdebtoninvoice = value;
                this.OnPropertyChanged("PrintDebtOnInvoice");
            }
            get { return this.printdebtoninvoice; }
        }

        private string deliveryAddress;
        public string DeliveryAddress
        {
            set
            {
                if (this.deliveryAddress == value)
                    return;
                this.deliveryAddress = value;
                this.OnPropertyChanged("DeliveryAddress");
            }
            get { return this.deliveryAddress; }
        }
    }

    public partial class UsagePeriodViewModel : BaseViewModel<Data.UsagePeriod>
    {
        private Guid usageperiodid;
        [PrimaryKey]
        public Guid UsagePeriodId
        {
            set
            {
                if (this.usageperiodid == value)
                    return;
                this.usageperiodid = value;
                this.OnPropertyChanged("UsagePeriodId");
            }
            get { return this.usageperiodid; }
        }

        private DateTime periodstart;
        public DateTime PeriodStart
        {
            set
            {
                if (this.periodstart == value)
                    return;
                this.periodstart = value;
                this.OnPropertyChanged("PeriodStart");
            }
            get { return this.periodstart; }
        }

        private DateTime? periodend;
        public DateTime? PeriodEnd
        {
            set
            {
                if (this.periodend == value)
                    return;
                this.periodend = value;
                this.OnPropertyChanged("PeriodEnd");
            }
            get { return this.periodend; }
        }

        private bool islocked;
        public bool IsLocked
        {
            set
            {
                if (this.islocked == value)
                    return;
                this.islocked = value;
                this.OnPropertyChanged("IsLocked");
            }
            get { return this.islocked; }
        }

    }

    public partial class VehicleViewModel : BaseViewModel<Data.Vehicle>
    {
        private Guid vehicleid;
        [PrimaryKey]
        public Guid VehicleId
        {
            set
            {
                if (this.vehicleid == value)
                    return;
                this.vehicleid = value;
                this.OnPropertyChanged("VehicleId");
            }
            get { return this.vehicleid; }
        }

        private string platenumber;
        public string PlateNumber
        {
            set
            {
                if (this.platenumber == value)
                    return;
                this.platenumber = value;
                this.OnPropertyChanged("PlateNumber");
            }
            get { return this.platenumber; }
        }

        private Guid traderid;
        public Guid TraderId
        {
            set
            {
                if (this.traderid == value)
                    return;
                this.traderid = value;
                this.OnPropertyChanged("TraderId");
            }
            get { return this.traderid; }
        }

        private string cardid;
        public string CardId
        {
            set
            {
                if (this.cardid == value)
                    return;
                this.cardid = value;
                this.OnPropertyChanged("CardId");
            }
            get { return this.cardid; }
        }

    }

}
