USE [master]
GO
/****** Object:  Database [DatabaseName]    Script Date: 06/10/2014 00:20:01 ******/
CREATE DATABASE [DatabaseName] ON  PRIMARY 
( NAME = N'[DatabaseName]', FILENAME = N'[FileName].mdf' , SIZE = 10240KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'[DatabaseName]_log', FILENAME = N'[FileName]_log.ldf' , SIZE = 6272KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO
ALTER DATABASE [DatabaseName] SET COMPATIBILITY_LEVEL = 100
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [DatabaseName].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [DatabaseName] SET ANSI_NULL_DEFAULT OFF
GO
ALTER DATABASE [DatabaseName] SET ANSI_NULLS OFF
GO
ALTER DATABASE [DatabaseName] SET ANSI_PADDING OFF
GO
ALTER DATABASE [DatabaseName] SET ANSI_WARNINGS OFF
GO
ALTER DATABASE [DatabaseName] SET ARITHABORT OFF
GO
ALTER DATABASE [DatabaseName] SET AUTO_CLOSE OFF
GO
ALTER DATABASE [DatabaseName] SET AUTO_CREATE_STATISTICS ON
GO
ALTER DATABASE [DatabaseName] SET AUTO_SHRINK OFF
GO
ALTER DATABASE [DatabaseName] SET AUTO_UPDATE_STATISTICS ON
GO
ALTER DATABASE [DatabaseName] SET CURSOR_CLOSE_ON_COMMIT OFF
GO
ALTER DATABASE [DatabaseName] SET CURSOR_DEFAULT  GLOBAL
GO
ALTER DATABASE [DatabaseName] SET CONCAT_NULL_YIELDS_NULL OFF
GO
ALTER DATABASE [DatabaseName] SET NUMERIC_ROUNDABORT OFF
GO
ALTER DATABASE [DatabaseName] SET QUOTED_IDENTIFIER OFF
GO
ALTER DATABASE [DatabaseName] SET RECURSIVE_TRIGGERS OFF
GO
ALTER DATABASE [DatabaseName] SET  DISABLE_BROKER
GO
ALTER DATABASE [DatabaseName] SET AUTO_UPDATE_STATISTICS_ASYNC OFF
GO
ALTER DATABASE [DatabaseName] SET DATE_CORRELATION_OPTIMIZATION OFF
GO
ALTER DATABASE [DatabaseName] SET TRUSTWORTHY OFF
GO
ALTER DATABASE [DatabaseName] SET ALLOW_SNAPSHOT_ISOLATION OFF
GO
ALTER DATABASE [DatabaseName] SET PARAMETERIZATION SIMPLE
GO
ALTER DATABASE [DatabaseName] SET READ_COMMITTED_SNAPSHOT OFF
GO
ALTER DATABASE [DatabaseName] SET HONOR_BROKER_PRIORITY OFF
GO
ALTER DATABASE [DatabaseName] SET  READ_WRITE
GO
ALTER DATABASE [DatabaseName] SET RECOVERY SIMPLE
GO
ALTER DATABASE [DatabaseName] SET  MULTI_USER
GO
ALTER DATABASE [DatabaseName] SET PAGE_VERIFY CHECKSUM
GO
ALTER DATABASE [DatabaseName] SET DB_CHAINING OFF
GO
USE [DatabaseName]
GO
/****** Object:  Table [dbo].[UsagePeriod]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UsagePeriod](
	[UsagePeriodId] [uniqueidentifier] NOT NULL,
	[PeriodStart] [datetime] NOT NULL,
	[PeriodEnd] [datetime] NULL,
	[IsLocked] [bit] NOT NULL,
 CONSTRAINT [UsagePeriod_PK] PRIMARY KEY CLUSTERED 
(
	[UsagePeriodId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SendLog]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SendLog](
	[SendLogId] [uniqueidentifier] NOT NULL,
	[SendDate] [datetime] NOT NULL,
	[Action] [nvarchar](50) NOT NULL,
	[SendData] [ntext] NOT NULL,
 CONSTRAINT [PK_SendLog] PRIMARY KEY CLUSTERED 
(
	[SendLogId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DispenserProtocol]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DispenserProtocol](
	[DispenserProtocolId] [uniqueidentifier] NOT NULL,
	[ProtocolName] [nvarchar](255) NOT NULL,
	[EnumeratorValue] [int] NOT NULL,
 CONSTRAINT [DispenserProtocol_PK] PRIMARY KEY CLUSTERED 
(
	[DispenserProtocolId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FuelType]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FuelType](
	[FuelTypeId] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Code] [nvarchar](30) NOT NULL,
	[Color] [int] NULL,
	[ThermalCoeficient] [decimal](18, 9) NOT NULL,
	[EnumeratorValue] [int] NOT NULL,
	[BaseDensity] [decimal](18, 3) NOT NULL,
 CONSTRAINT [FuelType_PK] PRIMARY KEY CLUSTERED 
(
	[FuelTypeId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ApplicationUser]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ApplicationUser](
	[ApplicationUserId] [uniqueidentifier] NOT NULL,
	[UserName] [nvarchar](100) NOT NULL,
	[Password] [nvarchar](100) NOT NULL,
	[UserLevel] [int] NOT NULL,
 CONSTRAINT [PK_ApplicationUser] PRIMARY KEY CLUSTERED 
(
	[ApplicationUserId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AlertDefinition]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AlertDefinition](
	[AlertDefinitionId] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[AlertEnumValue] [int] NULL,
	[IsNozzleAlert] [bit] NOT NULL,
	[IsDispenserAlert] [bit] NOT NULL,
	[IsTankAlert] [bit] NOT NULL,
	[IsStationAlert] [bit] NOT NULL,
	[LockDevices] [bit] NOT NULL,
	[Expression] [ntext] NOT NULL,
	[AlertIsDisabled] [bit] NOT NULL,
	[ErrorThreshold] [decimal](19, 3) NOT NULL,
	[ResendAlerts] [bit] NOT NULL,
	[AlerttMessage] [ntext] NULL,
	[IsGeneric] [bit] NOT NULL,
	[ResendAlertsInterval] [int] NOT NULL,
	[AutoResolve] [bit] NOT NULL,
	[MethodForResolve] [nvarchar](150) NULL,
 CONSTRAINT [PK_AlertDefinition] PRIMARY KEY CLUSTERED 
(
	[AlertDefinitionId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CommunicationController]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CommunicationController](
	[CommunicationControllerId] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](100) NULL,
	[CommunicationPort] [nvarchar](30) NULL,
	[CommunicationProtocol] [int] NULL,
	[ControllerAssembly] [nvarchar](255) NULL,
 CONSTRAINT [PK_CommunicationController] PRIMARY KEY CLUSTERED 
(
	[CommunicationControllerId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AtgProbeProtocol]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AtgProbeProtocol](
	[AtgProbeProtocolId] [uniqueidentifier] NOT NULL,
	[ProtocolName] [nvarchar](255) NOT NULL,
	[EnumeratorValue] [int] NOT NULL,
 CONSTRAINT [AtgProbeProtocol_PK] PRIMARY KEY CLUSTERED 
(
	[AtgProbeProtocolId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PriceList]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PriceList](
	[PriceListId] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[StartDate] [date] NOT NULL,
	[EndDate] [datetime] NULL,
	[IsTraderPriceList] [bit] NOT NULL,
 CONSTRAINT [PriceList_PK] PRIMARY KEY CLUSTERED 
(
	[PriceListId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PendingSend]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PendingSend](
	[PendingSendId] [uniqueidentifier] NOT NULL,
	[PendingData] [ntext] NOT NULL,
	[DataType] [nvarchar](500) NOT NULL,
	[AssemblyFile] [nvarchar](500) NULL,
 CONSTRAINT [PK_PendingSend] PRIMARY KEY CLUSTERED 
(
	[PendingSendId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Option]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Option](
	[OptionId] [uniqueidentifier] NOT NULL,
	[OptionKey] [nvarchar](100) NULL,
	[OptionValue] [ntext] NULL,
	[Description] [nvarchar](100) NULL,
 CONSTRAINT [Option_PK] PRIMARY KEY CLUSTERED 
(
	[OptionId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[InvoiceForm]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[InvoiceForm](
	[InvoiceFormId] [uniqueidentifier] NOT NULL,
	[IsTextForm] [bit] NOT NULL,
	[Data] [ntext] NOT NULL,
 CONSTRAINT [PK_InvoiceForm] PRIMARY KEY CLUSTERED 
(
	[InvoiceFormId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Normalization]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Normalization](
	[NormalizationId] [uniqueidentifier] NOT NULL,
	[FuelTypeId] [uniqueidentifier] NOT NULL,
	[Density] [decimal](18, 0) NOT NULL,
	[Temperature] [decimal](18, 0) NOT NULL,
	[ThermalCoefficient] [decimal](18, 0) NOT NULL,
 CONSTRAINT [PK_Normalization] PRIMARY KEY CLUSTERED 
(
	[NormalizationId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[InvoiceType]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[InvoiceType](
	[InvoiceTypeId] [uniqueidentifier] NOT NULL,
	[Description] [nvarchar](100) NOT NULL,
	[Abbreviation] [nvarchar](10) NOT NULL,
	[LastNumber] [int] NOT NULL,
	[TransactionType] [int] NOT NULL,
	[Printable] [bit] NOT NULL,
	[OfficialEnumerator] [int] NOT NULL,
	[Printer] [nvarchar](150) NULL,
	[InvoiceFormId] [uniqueidentifier] NULL,
 CONSTRAINT [PK_InvoiceType] PRIMARY KEY CLUSTERED 
(
	[InvoiceTypeId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PriceListTimeSpan]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PriceListTimeSpan](
	[PriceListTimeSpanId] [uniqueidentifier] NOT NULL,
	[FromTime] [int] NOT NULL,
	[ToTime] [int] NOT NULL,
	[PriceListId] [uniqueidentifier] NULL,
 CONSTRAINT [PriceListTimeSpan_PK] PRIMARY KEY CLUSTERED 
(
	[PriceListTimeSpanId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FuelTypePrice]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FuelTypePrice](
	[FuelTypePriceId] [uniqueidentifier] NOT NULL,
	[FuelTypeId] [uniqueidentifier] NOT NULL,
	[Price] [decimal](18, 4) NOT NULL,
	[ChangeDate] [datetime] NOT NULL,
	[SentDateTime] [datetime] NULL,
	[ResponseCode] [nvarchar](500) NULL,
 CONSTRAINT [FuelTypePrice_PK] PRIMARY KEY CLUSTERED 
(
	[FuelTypePriceId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AtgProbeType]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AtgProbeType](
	[AtgProbeTypeId] [uniqueidentifier] NOT NULL,
	[AtgProbeProtocolId] [uniqueidentifier] NOT NULL,
	[BrandName] [nvarchar](255) NOT NULL,
 CONSTRAINT [AtgProbeType_PK] PRIMARY KEY CLUSTERED 
(
	[AtgProbeTypeId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ApplicationUserLoggon]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ApplicationUserLoggon](
	[ApplicationUserLoggonId] [uniqueidentifier] NOT NULL,
	[ApplicationUserId] [uniqueidentifier] NOT NULL,
	[LoggonTime] [datetime] NOT NULL,
	[LogoffTime] [datetime] NULL,
 CONSTRAINT [PK_ApplicationUserLoggon] PRIMARY KEY CLUSTERED 
(
	[ApplicationUserLoggonId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DispenserType]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DispenserType](
	[DispenserTypeId] [uniqueidentifier] NOT NULL,
	[DispenserProtocolId] [uniqueidentifier] NOT NULL,
	[BrandName] [nvarchar](255) NOT NULL,
 CONSTRAINT [DispenserType_PK] PRIMARY KEY CLUSTERED 
(
	[DispenserTypeId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Shift]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Shift](
	[ShiftId] [uniqueidentifier] NOT NULL,
	[ApplicationUserId] [uniqueidentifier] NOT NULL,
	[ShiftBegin] [datetime] NOT NULL,
	[ShiftEnd] [datetime] NULL,
 CONSTRAINT [PK_Shift] PRIMARY KEY CLUSTERED 
(
	[ShiftId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Tank]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tank](
	[TankId] [uniqueidentifier] NOT NULL,
	[FuelTypeId] [uniqueidentifier] NOT NULL,
	[TotalVolume] [decimal](19, 2) NOT NULL,
	[OffsetVolume] [decimal](19, 2) NOT NULL,
	[PhysicalState] [int] NOT NULL,
	[Channel] [int] NOT NULL,
	[Address] [int] NOT NULL,
	[AtgProbeTypeId] [uniqueidentifier] NOT NULL,
	[CommunicationControllerId] [uniqueidentifier] NOT NULL,
	[MaxWaterHeight] [decimal](18, 4) NOT NULL,
	[MinFuelHeight] [decimal](18, 4) NOT NULL,
	[MaxFuelHeight] [decimal](18, 4) NOT NULL,
	[TankNumber] [int] NOT NULL,
	[TankSerialNumber] [nvarchar](100) NOT NULL,
	[FuelLevel] [decimal](18, 3) NOT NULL,
	[WaterLevel] [decimal](18, 3) NOT NULL,
	[Temperatire] [decimal](18, 3) NOT NULL,
	[OffestWater] [decimal](19, 2) NOT NULL,
	[IsVirtual] [bit] NULL,
	[OrderLimit] [decimal](19, 2) NULL,
 CONSTRAINT [Tank_PK] PRIMARY KEY CLUSTERED 
(
	[TankId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Trader]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Trader](
	[TraderId] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[TaxRegistrationNumber] [nvarchar](40) NULL,
	[TaxRegistrationOffice] [nvarchar](100) NULL,
	[Address] [nvarchar](100) NULL,
	[City] [nvarchar](100) NULL,
	[Phone1] [nvarchar](30) NULL,
	[Phone2] [nvarchar](30) NULL,
	[Fax] [nvarchar](30) NULL,
	[Email] [nvarchar](100) NULL,
	[website] [nvarchar](100) NULL,
	[InvoiceTypeId] [uniqueidentifier] NULL,
	[PriceListId] [uniqueidentifier] NULL,
	[IsCustomer] [bit] NOT NULL,
	[IsSupplier] [bit] NOT NULL,
 CONSTRAINT [Trader_PK] PRIMARY KEY CLUSTERED 
(
	[TraderId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Dispenser]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Dispenser](
	[DispenserId] [uniqueidentifier] NOT NULL,
	[PhysicalState] [int] NOT NULL,
	[IsValid] [bit] NOT NULL,
	[InValidationDate] [datetime] NULL,
	[DispenserTypeId] [uniqueidentifier] NOT NULL,
	[CommunicationControllerId] [uniqueidentifier] NOT NULL,
	[Channel] [int] NOT NULL,
	[PhysicalAddress] [int] NOT NULL,
	[DispenserNumber] [int] NOT NULL,
	[PumpSerialNumber] [nvarchar](100) NOT NULL,
	[OfficialPumpNumber] [int] NOT NULL,
	[DecimalPlaces] [int] NULL,
	[UnitPriceDecimalPlaces] [int] NULL,
 CONSTRAINT [Dispenser_PK] PRIMARY KEY CLUSTERED 
(
	[DispenserId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[InvoicePrint]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[InvoicePrint](
	[InvoicePrintId] [uniqueidentifier] NOT NULL,
	[DispenserId] [uniqueidentifier] NOT NULL,
	[Printer] [nvarchar](150) NULL,
	[DefaultInvoiceType] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_InvoicePrint] PRIMARY KEY CLUSTERED 
(
	[InvoicePrintId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Nozzle]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Nozzle](
	[NozzleId] [uniqueidentifier] NOT NULL,
	[DispenserId] [uniqueidentifier] NOT NULL,
	[TotalCounter] [decimal](19, 4) NOT NULL,
	[Channel] [int] NOT NULL,
	[Address] [int] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[FuelTypeId] [uniqueidentifier] NOT NULL,
	[OrderId] [int] NOT NULL,
	[NozzleState] [int] NOT NULL,
	[SerialNumber] [nvarchar](50) NULL,
	[OfficialNozzleNumber] [int] NOT NULL,
 CONSTRAINT [Nozzle_PK] PRIMARY KEY CLUSTERED 
(
	[NozzleId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Vehicle]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Vehicle](
	[VehicleId] [uniqueidentifier] NOT NULL,
	[PlateNumber] [nvarchar](20) NOT NULL,
	[TraderId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_Vehicle] PRIMARY KEY CLUSTERED 
(
	[VehicleId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TankPrice]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TankPrice](
	[TankPriceId] [uniqueidentifier] NOT NULL,
	[TankId] [uniqueidentifier] NOT NULL,
	[Price] [decimal](18, 10) NOT NULL,
	[ChangeDate] [datetime] NOT NULL,
	[FuelDensity] [decimal](18, 4) NOT NULL,
 CONSTRAINT [TankPrice_PK] PRIMARY KEY CLUSTERED 
(
	[TankPriceId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Titrimetry]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Titrimetry](
	[TitrimetryId] [uniqueidentifier] NOT NULL,
	[TankId] [uniqueidentifier] NULL,
	[TitrationDate] [datetime] NULL,
 CONSTRAINT [Titrimetry_PK] PRIMARY KEY CLUSTERED 
(
	[TitrimetryId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TankUsagePeriod]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TankUsagePeriod](
	[TankUsagePeriodId] [uniqueidentifier] NOT NULL,
	[TankId] [uniqueidentifier] NOT NULL,
	[UsagePeriodId] [uniqueidentifier] NOT NULL,
	[MeasurementDateTime] [datetime] NOT NULL,
	[ExpirationDate] [datetime] NOT NULL,
	[TotalVolume] [decimal](19, 4) NOT NULL,
	[CRC] [bigint] NOT NULL,
 CONSTRAINT [TankUsagePeriod_PK] PRIMARY KEY CLUSTERED 
(
	[TankUsagePeriodId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TankSetting]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TankSetting](
	[TankSettingId] [uniqueidentifier] NOT NULL,
	[TankId] [uniqueidentifier] NOT NULL,
	[SettingKey] [nvarchar](100) NULL,
	[SettingValue] [ntext] NULL,
	[Description] [ntext] NULL,
 CONSTRAINT [TankSetting_PK] PRIMARY KEY CLUSTERED 
(
	[TankSettingId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TitrimetryLevel]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TitrimetryLevel](
	[TitrimetryLevelId] [uniqueidentifier] NOT NULL,
	[TitrimetryId] [uniqueidentifier] NULL,
	[Height] [decimal](18, 2) NULL,
	[Volume] [decimal](18, 2) NULL,
 CONSTRAINT [TitrimetryLevel_PK] PRIMARY KEY CLUSTERED 
(
	[TitrimetryLevelId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TankFilling]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TankFilling](
	[TankFillingId] [uniqueidentifier] NOT NULL,
	[UsagePeriodId] [uniqueidentifier] NOT NULL,
	[TankId] [uniqueidentifier] NOT NULL,
	[TankPriceId] [uniqueidentifier] NOT NULL,
	[TransactionTime] [datetime] NOT NULL,
	[Volume] [decimal](18, 2) NOT NULL,
	[VolumeNormalized] [decimal](18, 8) NOT NULL,
	[TankTemperatureStart] [decimal](18, 4) NOT NULL,
	[TankTemperatureEnd] [decimal](18, 3) NOT NULL,
	[IsLocked] [bit] NOT NULL,
	[CRC] [bigint] NOT NULL,
	[TransactionTimeEnd] [datetime] NOT NULL,
	[LevelStart] [decimal](18, 3) NOT NULL,
	[LevelEnd] [decimal](18, 3) NOT NULL,
	[FuelDensity] [decimal](18, 3) NOT NULL,
	[VolumeReal] [decimal](18, 2) NOT NULL,
	[VolumeRealNormalized] [decimal](18, 2) NOT NULL,
	[ApplicationUserId] [uniqueidentifier] NOT NULL,
	[SentDateTime] [datetime] NULL,
	[ResponseCode] [nvarchar](500) NULL,
 CONSTRAINT [TankFilling_PK] PRIMARY KEY CLUSTERED 
(
	[TankFillingId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SystemEvent]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SystemEvent](
	[EventId] [uniqueidentifier] NOT NULL,
	[EventType] [int] NOT NULL,
	[Message] [ntext] NOT NULL,
	[EventDate] [datetime] NOT NULL,
	[NozzleId] [uniqueidentifier] NULL,
	[TankId] [uniqueidentifier] NULL,
	[DispenserId] [uniqueidentifier] NULL,
	[AlertDefinitionId] [uniqueidentifier] NULL,
	[SentDate] [datetime] NULL,
	[ResolvedDate] [datetime] NULL,
	[ResolveMessage] [ntext] NULL,
	[CRC] [bigint] NOT NULL,
	[AlarmType] [int] NULL,
 CONSTRAINT [Event_PK] PRIMARY KEY CLUSTERED 
(
	[EventId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DispenserSetting]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DispenserSetting](
	[DispenserSettingId] [uniqueidentifier] NOT NULL,
	[DispenserId] [uniqueidentifier] NOT NULL,
	[SettingKey] [nvarchar](100) NULL,
	[SettingValue] [ntext] NULL,
	[Description] [ntext] NULL,
	[NozzleId] [uniqueidentifier] NULL,
 CONSTRAINT [DispenserSetting_PK] PRIMARY KEY CLUSTERED 
(
	[DispenserSettingId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Invoice]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Invoice](
	[InvoiceId] [uniqueidentifier] NOT NULL,
	[TraderId] [uniqueidentifier] NULL,
	[Number] [int] NOT NULL,
	[VehiclePlateNumber] [nvarchar](50) NULL,
	[InvoiceSignature] [nvarchar](255) NULL,
	[InvoiceTypeId] [uniqueidentifier] NOT NULL,
	[VehicleId] [uniqueidentifier] NULL,
	[TransactionDate] [datetime] NOT NULL,
	[NettoAmount] [decimal](18, 2) NULL,
	[VatAmount] [decimal](18, 2) NULL,
	[TotalAmount] [decimal](18, 2) NULL,
	[Printer] [nvarchar](150) NULL,
	[InvoiceFormId] [uniqueidentifier] NULL,
	[ApplicationUserId] [uniqueidentifier] NOT NULL,
	[IsPrinted] [bit] NULL,
	[Series] [nvarchar](10) NULL,
 CONSTRAINT [Invoice_PK] PRIMARY KEY CLUSTERED 
(
	[InvoiceId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NozzleUsagePeriod]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NozzleUsagePeriod](
	[NozzleUsagePeriodId] [uniqueidentifier] NOT NULL,
	[NozzleId] [uniqueidentifier] NOT NULL,
	[UsagePeriodId] [uniqueidentifier] NOT NULL,
	[MeasurementDateTime] [datetime] NOT NULL,
	[ExpirationDate] [datetime] NOT NULL,
	[TotalVolume] [decimal](19, 4) NOT NULL,
	[CRC] [bigint] NOT NULL,
 CONSTRAINT [NozzleUsagePeriod_PK] PRIMARY KEY CLUSTERED 
(
	[NozzleUsagePeriodId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NozzlePriceList]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NozzlePriceList](
	[NozzlePriceListId] [uniqueidentifier] NOT NULL,
	[PriceListId] [uniqueidentifier] NULL,
	[NozzleId] [uniqueidentifier] NULL,
	[Discount] [decimal](18, 2) NULL,
 CONSTRAINT [NozzlePriceList_PK] PRIMARY KEY CLUSTERED 
(
	[NozzlePriceListId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NozzleFlow]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NozzleFlow](
	[NozzleFlowId] [uniqueidentifier] NOT NULL,
	[NozzleId] [uniqueidentifier] NOT NULL,
	[TankId] [uniqueidentifier] NOT NULL,
	[FlowState] [smallint] NOT NULL,
 CONSTRAINT [PK_NozzleFlow] PRIMARY KEY CLUSTERED 
(
	[NozzleFlowId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SalesTransaction]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SalesTransaction](
	[SalesTransactionId] [uniqueidentifier] NOT NULL,
	[UsagePeriodId] [uniqueidentifier] NOT NULL,
	[IsLocked] [bit] NOT NULL,
	[CRC] [bigint] NOT NULL,
	[NozzleId] [uniqueidentifier] NOT NULL,
	[TotalizerStart] [decimal](18, 3) NOT NULL,
	[TotalizerEnd] [decimal](18, 3) NOT NULL,
	[TransactionTimeStamp] [datetime] NOT NULL,
	[Volume] [decimal](18, 3) NOT NULL,
	[VolumeNormalized] [decimal](18, 3) NOT NULL,
	[TemperatureStart] [decimal](18, 3) NOT NULL,
	[TemperatureEnd] [decimal](18, 3) NOT NULL,
	[UnitPrice] [decimal](18, 3) NOT NULL,
	[TotalPrice] [decimal](18, 3) NOT NULL,
	[ApplicationUserId] [uniqueidentifier] NOT NULL,
	[SentDateTime] [datetime] NULL,
	[ResponseCode] [nvarchar](500) NULL,
	[ShiftId] [uniqueidentifier] NULL,
	[IsErrorResolving] [bit] NULL,
 CONSTRAINT [SalesTransaction_PK] PRIMARY KEY CLUSTERED 
(
	[SalesTransactionId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[InvoiceLine]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[InvoiceLine](
	[InvoiceLineId] [uniqueidentifier] NOT NULL,
	[InvoiceId] [uniqueidentifier] NOT NULL,
	[Volume] [decimal](18, 3) NOT NULL,
	[VolumeNormalized] [decimal](18, 3) NOT NULL,
	[Temperature] [decimal](18, 3) NOT NULL,
	[FuelDensity] [decimal](18, 3) NOT NULL,
	[UnitPrice] [decimal](18, 5) NOT NULL,
	[TotalPrice] [decimal](18, 2) NOT NULL,
	[VatAmount] [decimal](18, 2) NOT NULL,
	[VatPercentage] [decimal](18, 2) NOT NULL,
	[SaleTransactionId] [uniqueidentifier] NULL,
	[TankFillingId] [uniqueidentifier] NULL,
	[FuelTypeId] [uniqueidentifier] NOT NULL,
	[DiscountAmount] [decimal](18, 2) NOT NULL,
	[TankId] [uniqueidentifier] NULL,
 CONSTRAINT [PK_InvoiceLine] PRIMARY KEY CLUSTERED 
(
	[InvoiceLineId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[InvoiceRelation]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[InvoiceRelation](
	[InvoiceRelationId] [uniqueidentifier] NOT NULL,
	[ParentInvoiceId] [uniqueidentifier] NOT NULL,
	[ChildInvoiceId] [uniqueidentifier] NOT NULL,
	[RelationType] [int] NOT NULL,
 CONSTRAINT [InvoiceRelation_PK] PRIMARY KEY CLUSTERED 
(
	[InvoiceRelationId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Balance]    Script Date: 06/20/2014 19:13:12 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Balance](
	[BalanceId] [uniqueidentifier] NOT NULL,
	[StartDate] [datetime] NOT NULL,
	[EndDate] [datetime] NOT NULL,
	[LastSale] [uniqueidentifier] NULL,
	[LastFilling] [uniqueidentifier] NULL,
	[BalanceText] [ntext] NOT NULL,
	[ApplicationUserId] [uniqueidentifier] NOT NULL,
	[SentDateTime] [datetime] NULL,
	[ResponseCode] [nvarchar](500) NULL,
 CONSTRAINT [PK_Balance] PRIMARY KEY CLUSTERED 
(
	[BalanceId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  View [dbo].[SalesView]    Script Date: 06/20/2014 19:13:13 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[SalesView]
AS
SELECT     NEWID() AS ViewId, dbo.SalesTransaction.Volume, dbo.SalesTransaction.VolumeNormalized, dbo.SalesTransaction.UnitPrice, dbo.SalesTransaction.TotalPrice, 
                      dbo.SalesTransaction.TotalizerStart, dbo.SalesTransaction.TotalizerEnd, dbo.FuelType.Name, dbo.Nozzle.OfficialNozzleNumber, dbo.Dispenser.OfficialPumpNumber, 
                      dbo.SalesTransaction.TransactionTimeStamp, dbo.ApplicationUser.UserName
FROM         dbo.SalesTransaction INNER JOIN
                      dbo.Nozzle ON dbo.SalesTransaction.NozzleId = dbo.Nozzle.NozzleId INNER JOIN
                      dbo.Dispenser ON dbo.Nozzle.DispenserId = dbo.Dispenser.DispenserId INNER JOIN
                      dbo.FuelType ON dbo.Nozzle.FuelTypeId = dbo.FuelType.FuelTypeId LEFT OUTER JOIN
                      dbo.ApplicationUser ON dbo.SalesTransaction.ApplicationUserId = dbo.ApplicationUser.ApplicationUserId
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = -96
         Left = 0
      End
      Begin Tables = 
         Begin Table = "SalesTransaction"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 125
               Right = 235
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "Nozzle"
            Begin Extent = 
               Top = 6
               Left = 273
               Bottom = 125
               Right = 463
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "Dispenser"
            Begin Extent = 
               Top = 126
               Left = 38
               Bottom = 245
               Right = 256
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "FuelType"
            Begin Extent = 
               Top = 184
               Left = 289
               Bottom = 303
               Right = 464
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "ApplicationUser"
            Begin Extent = 
               Top = 144
               Left = 708
               Bottom = 263
               Right = 881
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 11
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 13' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'SalesView'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane2', @value=N'50
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'SalesView'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=2 , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'SalesView'
GO
/****** Object:  Table [dbo].[SystemEventData]    Script Date: 06/20/2014 19:13:13 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SystemEventData](
	[SystemEventDataId] [uniqueidentifier] NOT NULL,
	[SystemEventId] [uniqueidentifier] NOT NULL,
	[PropertyName] [nvarchar](100) NOT NULL,
	[Value] [ntext] NOT NULL,
	[IsAlertTrigger] [bit] NOT NULL,
 CONSTRAINT [PK_SystemEventData] PRIMARY KEY CLUSTERED 
(
	[SystemEventDataId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TankSale]    Script Date: 06/20/2014 19:13:13 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TankSale](
	[TankSaleId] [uniqueidentifier] NOT NULL,
	[SalesTransactionId] [uniqueidentifier] NOT NULL,
	[TankId] [uniqueidentifier] NOT NULL,
	[StartVolume] [decimal](19, 4) NOT NULL,
	[EndVolume] [decimal](19, 4) NULL,
	[StartVolumeNormalized] [decimal](19, 4) NOT NULL,
	[EndVolumeNormalized] [decimal](19, 4) NULL,
	[StartTemperature] [decimal](19, 4) NULL,
	[EndTemperature] [decimal](19, 4) NOT NULL,
	[CRC] [bigint] NOT NULL,
	[StartLevel] [decimal](18, 3) NOT NULL,
	[EndLevel] [decimal](18, 3) NULL,
	[FuelDensity] [decimal](18, 3) NOT NULL,
 CONSTRAINT [PK_TankSale] PRIMARY KEY CLUSTERED 
(
	[TankSaleId] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[TankFillingView]    Script Date: 06/20/2014 19:13:13 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[TankFillingView]
AS
SELECT     NEWID() AS ViewId, dbo.TankFilling.TransactionTime AS StartTime, dbo.TankFilling.TransactionTimeEnd AS EndTime, dbo.TankFilling.LevelStart, 
                      dbo.TankFilling.LevelEnd, dbo.TankFilling.TankTemperatureStart AS TempStart, dbo.TankFilling.TankTemperatureEnd AS TempEnd, 
                      dbo.TankFilling.VolumeReal AS TFVolume, dbo.TankFilling.VolumeRealNormalized AS TFVolumeNormalized, dbo.InvoiceLine.Volume AS INVVolume, 
                      dbo.InvoiceLine.VolumeNormalized AS INVVolumeNormalized, dbo.InvoiceLine.Temperature AS INVTemp, dbo.InvoiceLine.FuelDensity, dbo.InvoiceLine.UnitPrice, 
                      dbo.Invoice.Number, dbo.Invoice.VehiclePlateNumber, dbo.InvoiceType.Description, dbo.FuelType.Name, dbo.Tank.TankNumber, 
                      dbo.ApplicationUser.UserName
FROM         dbo.InvoiceType RIGHT OUTER JOIN
                      dbo.Invoice ON dbo.InvoiceType.InvoiceTypeId = dbo.Invoice.InvoiceTypeId FULL OUTER JOIN
                      dbo.InvoiceLine FULL OUTER JOIN
                      dbo.FuelType RIGHT OUTER JOIN
                      dbo.Tank ON dbo.FuelType.FuelTypeId = dbo.Tank.FuelTypeId RIGHT OUTER JOIN
                      dbo.ApplicationUser RIGHT OUTER JOIN
                      dbo.TankFilling ON dbo.ApplicationUser.ApplicationUserId = dbo.TankFilling.ApplicationUserId ON dbo.Tank.TankId = dbo.TankFilling.TankId ON 
                      dbo.InvoiceLine.TankFillingId = dbo.TankFilling.TankFillingId ON dbo.Invoice.InvoiceId = dbo.InvoiceLine.InvoiceId
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "InvoiceLine"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 125
               Right = 213
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "FuelType"
            Begin Extent = 
               Top = 6
               Left = 251
               Bottom = 125
               Right = 426
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "Tank"
            Begin Extent = 
               Top = 126
               Left = 38
               Bottom = 245
               Right = 256
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "TankFilling"
            Begin Extent = 
               Top = 246
               Left = 38
               Bottom = 365
               Right = 236
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "Invoice"
            Begin Extent = 
               Top = 0
               Left = 451
               Bottom = 119
               Right = 634
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "InvoiceType"
            Begin Extent = 
               Top = 246
               Left = 274
               Bottom = 365
               Right = 452
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "ApplicationUser"
            Begin Extent = 
               Top = 41
               Left = 894
               Bottom = 160
               Right = 1067
            End
            Dis' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'TankFillingView'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane2', @value=N'playFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 11
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'TankFillingView'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=2 , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'TankFillingView'
GO

/****** Object:  View [dbo].[InvoiceGroupView]    Script Date: 06/20/2014 19:13:13 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[InvoiceGroupView]
AS
SELECT     TOP (100) PERCENT NEWID() AS ViewId, MIN(dbo.Invoice.Number) AS MinNumber, MAX(dbo.Invoice.Number) AS MaxNumber, CONVERT(nvarchar(10), 
                      dbo.Invoice.TransactionDate, 103) AS DateString, CONVERT(nvarchar(10), dbo.Invoice.TransactionDate, 112) AS DateString2, SUM(dbo.Invoice.NettoAmount) 
                      AS NettoAmount, SUM(dbo.Invoice.VatAmount) AS VatAmount, SUM(dbo.Invoice.TotalAmount) AS TotalAmount, dbo.InvoiceType.TransactionType, 
                      dbo.InvoiceType.Description, CONVERT(DateTime, CONVERT(nvarchar(10), dbo.Invoice.TransactionDate, 103), 103) AS TransactionDate
FROM         dbo.Invoice INNER JOIN
                      dbo.InvoiceType ON dbo.Invoice.InvoiceTypeId = dbo.InvoiceType.InvoiceTypeId
WHERE     (dbo.InvoiceType.TransactionType = 0)
GROUP BY CONVERT(nvarchar(10), dbo.Invoice.TransactionDate, 112), CONVERT(nvarchar(10), dbo.Invoice.TransactionDate, 103), dbo.InvoiceType.TransactionType, 
                      dbo.InvoiceType.Description
ORDER BY DateString2

GO

EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "Invoice"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 264
               Right = 462
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "InvoiceType"
            Begin Extent = 
               Top = 6
               Left = 500
               Bottom = 125
               Right = 678
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 12
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'InvoiceGroupView'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'VIEW',@level1name=N'InvoiceGroupView'
GO


/****** Object:  ForeignKey [FK_Normalization_FuelType]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Normalization]  WITH CHECK ADD  CONSTRAINT [FK_Normalization_FuelType] FOREIGN KEY([FuelTypeId])
REFERENCES [dbo].[FuelType] ([FuelTypeId])
GO
ALTER TABLE [dbo].[Normalization] CHECK CONSTRAINT [FK_Normalization_FuelType]
GO
/****** Object:  ForeignKey [FK_InvoiceType_InvoiceForm]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[InvoiceType]  WITH CHECK ADD  CONSTRAINT [FK_InvoiceType_InvoiceForm] FOREIGN KEY([InvoiceFormId])
REFERENCES [dbo].[InvoiceForm] ([InvoiceFormId])
GO
ALTER TABLE [dbo].[InvoiceType] CHECK CONSTRAINT [FK_InvoiceType_InvoiceForm]
GO
/****** Object:  ForeignKey [PriceList_PriceListTimeSpan_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[PriceListTimeSpan]  WITH CHECK ADD  CONSTRAINT [PriceList_PriceListTimeSpan_FK1] FOREIGN KEY([PriceListId])
REFERENCES [dbo].[PriceList] ([PriceListId])
GO
ALTER TABLE [dbo].[PriceListTimeSpan] CHECK CONSTRAINT [PriceList_PriceListTimeSpan_FK1]
GO
/****** Object:  ForeignKey [FuelType_FuelTypePrice_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[FuelTypePrice]  WITH CHECK ADD  CONSTRAINT [FuelType_FuelTypePrice_FK1] FOREIGN KEY([FuelTypeId])
REFERENCES [dbo].[FuelType] ([FuelTypeId])
GO
ALTER TABLE [dbo].[FuelTypePrice] CHECK CONSTRAINT [FuelType_FuelTypePrice_FK1]
GO
/****** Object:  ForeignKey [AtgProbeProtocol_AtgProbeType_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[AtgProbeType]  WITH CHECK ADD  CONSTRAINT [AtgProbeProtocol_AtgProbeType_FK1] FOREIGN KEY([AtgProbeProtocolId])
REFERENCES [dbo].[AtgProbeProtocol] ([AtgProbeProtocolId])
GO
ALTER TABLE [dbo].[AtgProbeType] CHECK CONSTRAINT [AtgProbeProtocol_AtgProbeType_FK1]
GO
/****** Object:  ForeignKey [FK_ApplicationUserLoggon_ApplicationUser]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[ApplicationUserLoggon]  WITH CHECK ADD  CONSTRAINT [FK_ApplicationUserLoggon_ApplicationUser] FOREIGN KEY([ApplicationUserId])
REFERENCES [dbo].[ApplicationUser] ([ApplicationUserId])
GO
ALTER TABLE [dbo].[ApplicationUserLoggon] CHECK CONSTRAINT [FK_ApplicationUserLoggon_ApplicationUser]
GO
/****** Object:  ForeignKey [DispenserProtocol_DispenserType_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[DispenserType]  WITH CHECK ADD  CONSTRAINT [DispenserProtocol_DispenserType_FK1] FOREIGN KEY([DispenserProtocolId])
REFERENCES [dbo].[DispenserProtocol] ([DispenserProtocolId])
GO
ALTER TABLE [dbo].[DispenserType] CHECK CONSTRAINT [DispenserProtocol_DispenserType_FK1]
GO
/****** Object:  ForeignKey [FK_Shift_ApplicationUser]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Shift]  WITH CHECK ADD  CONSTRAINT [FK_Shift_ApplicationUser] FOREIGN KEY([ApplicationUserId])
REFERENCES [dbo].[ApplicationUser] ([ApplicationUserId])
GO
ALTER TABLE [dbo].[Shift] CHECK CONSTRAINT [FK_Shift_ApplicationUser]
GO
/****** Object:  ForeignKey [AtgProbeType_Tank_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Tank]  WITH CHECK ADD  CONSTRAINT [AtgProbeType_Tank_FK1] FOREIGN KEY([AtgProbeTypeId])
REFERENCES [dbo].[AtgProbeType] ([AtgProbeTypeId])
GO
ALTER TABLE [dbo].[Tank] CHECK CONSTRAINT [AtgProbeType_Tank_FK1]
GO
/****** Object:  ForeignKey [FK_Tank_CommunicationController]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Tank]  WITH CHECK ADD  CONSTRAINT [FK_Tank_CommunicationController] FOREIGN KEY([CommunicationControllerId])
REFERENCES [dbo].[CommunicationController] ([CommunicationControllerId])
GO
ALTER TABLE [dbo].[Tank] CHECK CONSTRAINT [FK_Tank_CommunicationController]
GO
/****** Object:  ForeignKey [FuelType_Tank_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Tank]  WITH CHECK ADD  CONSTRAINT [FuelType_Tank_FK1] FOREIGN KEY([FuelTypeId])
REFERENCES [dbo].[FuelType] ([FuelTypeId])
GO
ALTER TABLE [dbo].[Tank] CHECK CONSTRAINT [FuelType_Tank_FK1]
GO
/****** Object:  ForeignKey [FK_Trader_InvoiceType]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Trader]  WITH CHECK ADD  CONSTRAINT [FK_Trader_InvoiceType] FOREIGN KEY([InvoiceTypeId])
REFERENCES [dbo].[InvoiceType] ([InvoiceTypeId])
GO
ALTER TABLE [dbo].[Trader] CHECK CONSTRAINT [FK_Trader_InvoiceType]
GO
/****** Object:  ForeignKey [PriceList_Trader_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Trader]  WITH CHECK ADD  CONSTRAINT [PriceList_Trader_FK1] FOREIGN KEY([PriceListId])
REFERENCES [dbo].[PriceList] ([PriceListId])
GO
ALTER TABLE [dbo].[Trader] CHECK CONSTRAINT [PriceList_Trader_FK1]
GO
/****** Object:  ForeignKey [DispenserType_Dispenser_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Dispenser]  WITH CHECK ADD  CONSTRAINT [DispenserType_Dispenser_FK1] FOREIGN KEY([DispenserTypeId])
REFERENCES [dbo].[DispenserType] ([DispenserTypeId])
GO
ALTER TABLE [dbo].[Dispenser] CHECK CONSTRAINT [DispenserType_Dispenser_FK1]
GO
/****** Object:  ForeignKey [FK_Dispenser_CommunicationController]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Dispenser]  WITH CHECK ADD  CONSTRAINT [FK_Dispenser_CommunicationController] FOREIGN KEY([CommunicationControllerId])
REFERENCES [dbo].[CommunicationController] ([CommunicationControllerId])
GO
ALTER TABLE [dbo].[Dispenser] CHECK CONSTRAINT [FK_Dispenser_CommunicationController]
GO
/****** Object:  ForeignKey [FK_InvoicePrint_Dispenser]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[InvoicePrint]  WITH CHECK ADD  CONSTRAINT [FK_InvoicePrint_Dispenser] FOREIGN KEY([DispenserId])
REFERENCES [dbo].[Dispenser] ([DispenserId])
GO
ALTER TABLE [dbo].[InvoicePrint] CHECK CONSTRAINT [FK_InvoicePrint_Dispenser]
GO
/****** Object:  ForeignKey [FK_InvoicePrint_InvoiceType]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[InvoicePrint]  WITH CHECK ADD  CONSTRAINT [FK_InvoicePrint_InvoiceType] FOREIGN KEY([DefaultInvoiceType])
REFERENCES [dbo].[InvoiceType] ([InvoiceTypeId])
GO
ALTER TABLE [dbo].[InvoicePrint] CHECK CONSTRAINT [FK_InvoicePrint_InvoiceType]
GO
/****** Object:  ForeignKey [Dispenser_Nozzle_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Nozzle]  WITH CHECK ADD  CONSTRAINT [Dispenser_Nozzle_FK1] FOREIGN KEY([DispenserId])
REFERENCES [dbo].[Dispenser] ([DispenserId])
GO
ALTER TABLE [dbo].[Nozzle] CHECK CONSTRAINT [Dispenser_Nozzle_FK1]
GO
/****** Object:  ForeignKey [FK_Nozzle_FuelType]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Nozzle]  WITH CHECK ADD  CONSTRAINT [FK_Nozzle_FuelType] FOREIGN KEY([FuelTypeId])
REFERENCES [dbo].[FuelType] ([FuelTypeId])
GO
ALTER TABLE [dbo].[Nozzle] CHECK CONSTRAINT [FK_Nozzle_FuelType]
GO
/****** Object:  ForeignKey [FK_Vehicle_Trader]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Vehicle]  WITH CHECK ADD  CONSTRAINT [FK_Vehicle_Trader] FOREIGN KEY([TraderId])
REFERENCES [dbo].[Trader] ([TraderId])
GO
ALTER TABLE [dbo].[Vehicle] CHECK CONSTRAINT [FK_Vehicle_Trader]
GO
/****** Object:  ForeignKey [Tank_TankPrice_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[TankPrice]  WITH CHECK ADD  CONSTRAINT [Tank_TankPrice_FK1] FOREIGN KEY([TankId])
REFERENCES [dbo].[Tank] ([TankId])
GO
ALTER TABLE [dbo].[TankPrice] CHECK CONSTRAINT [Tank_TankPrice_FK1]
GO
/****** Object:  ForeignKey [Tank_Titrimetry_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Titrimetry]  WITH CHECK ADD  CONSTRAINT [Tank_Titrimetry_FK1] FOREIGN KEY([TankId])
REFERENCES [dbo].[Tank] ([TankId])
GO
ALTER TABLE [dbo].[Titrimetry] CHECK CONSTRAINT [Tank_Titrimetry_FK1]
GO
/****** Object:  ForeignKey [Tank_TankUsagePeriod_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[TankUsagePeriod]  WITH CHECK ADD  CONSTRAINT [Tank_TankUsagePeriod_FK1] FOREIGN KEY([TankId])
REFERENCES [dbo].[Tank] ([TankId])
GO
ALTER TABLE [dbo].[TankUsagePeriod] CHECK CONSTRAINT [Tank_TankUsagePeriod_FK1]
GO
/****** Object:  ForeignKey [UsagePeriod_TankUsagePeriod_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[TankUsagePeriod]  WITH CHECK ADD  CONSTRAINT [UsagePeriod_TankUsagePeriod_FK1] FOREIGN KEY([UsagePeriodId])
REFERENCES [dbo].[UsagePeriod] ([UsagePeriodId])
GO
ALTER TABLE [dbo].[TankUsagePeriod] CHECK CONSTRAINT [UsagePeriod_TankUsagePeriod_FK1]
GO
/****** Object:  ForeignKey [Tank_TankSetting_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[TankSetting]  WITH CHECK ADD  CONSTRAINT [Tank_TankSetting_FK1] FOREIGN KEY([TankId])
REFERENCES [dbo].[Tank] ([TankId])
GO
ALTER TABLE [dbo].[TankSetting] CHECK CONSTRAINT [Tank_TankSetting_FK1]
GO
/****** Object:  ForeignKey [Titrimetry_TitrimetryLevel_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[TitrimetryLevel]  WITH CHECK ADD  CONSTRAINT [Titrimetry_TitrimetryLevel_FK1] FOREIGN KEY([TitrimetryId])
REFERENCES [dbo].[Titrimetry] ([TitrimetryId])
GO
ALTER TABLE [dbo].[TitrimetryLevel] CHECK CONSTRAINT [Titrimetry_TitrimetryLevel_FK1]
GO
/****** Object:  ForeignKey [FK_TankFilling_ApplicationUser]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[TankFilling]  WITH CHECK ADD  CONSTRAINT [FK_TankFilling_ApplicationUser] FOREIGN KEY([ApplicationUserId])
REFERENCES [dbo].[ApplicationUser] ([ApplicationUserId])
GO
ALTER TABLE [dbo].[TankFilling] CHECK CONSTRAINT [FK_TankFilling_ApplicationUser]
GO
/****** Object:  ForeignKey [Tank_TankFilling_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[TankFilling]  WITH CHECK ADD  CONSTRAINT [Tank_TankFilling_FK1] FOREIGN KEY([TankId])
REFERENCES [dbo].[Tank] ([TankId])
GO
ALTER TABLE [dbo].[TankFilling] CHECK CONSTRAINT [Tank_TankFilling_FK1]
GO
/****** Object:  ForeignKey [TankPrice_TankFilling_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[TankFilling]  WITH CHECK ADD  CONSTRAINT [TankPrice_TankFilling_FK1] FOREIGN KEY([TankPriceId])
REFERENCES [dbo].[TankPrice] ([TankPriceId])
GO
ALTER TABLE [dbo].[TankFilling] CHECK CONSTRAINT [TankPrice_TankFilling_FK1]
GO
/****** Object:  ForeignKey [UsagePeriod_TankFilling_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[TankFilling]  WITH CHECK ADD  CONSTRAINT [UsagePeriod_TankFilling_FK1] FOREIGN KEY([UsagePeriodId])
REFERENCES [dbo].[UsagePeriod] ([UsagePeriodId])
GO
ALTER TABLE [dbo].[TankFilling] CHECK CONSTRAINT [UsagePeriod_TankFilling_FK1]
GO
/****** Object:  ForeignKey [Dispenser_Event_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[SystemEvent]  WITH CHECK ADD  CONSTRAINT [Dispenser_Event_FK1] FOREIGN KEY([DispenserId])
REFERENCES [dbo].[Dispenser] ([DispenserId])
GO
ALTER TABLE [dbo].[SystemEvent] CHECK CONSTRAINT [Dispenser_Event_FK1]
GO
/****** Object:  ForeignKey [FK_Event_AlertDefinition]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[SystemEvent]  WITH CHECK ADD  CONSTRAINT [FK_Event_AlertDefinition] FOREIGN KEY([AlertDefinitionId])
REFERENCES [dbo].[AlertDefinition] ([AlertDefinitionId])
GO
ALTER TABLE [dbo].[SystemEvent] CHECK CONSTRAINT [FK_Event_AlertDefinition]
GO
/****** Object:  ForeignKey [Nozzle_Event_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[SystemEvent]  WITH CHECK ADD  CONSTRAINT [Nozzle_Event_FK1] FOREIGN KEY([NozzleId])
REFERENCES [dbo].[Nozzle] ([NozzleId])
GO
ALTER TABLE [dbo].[SystemEvent] CHECK CONSTRAINT [Nozzle_Event_FK1]
GO
/****** Object:  ForeignKey [Tank_Event_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[SystemEvent]  WITH CHECK ADD  CONSTRAINT [Tank_Event_FK1] FOREIGN KEY([TankId])
REFERENCES [dbo].[Tank] ([TankId])
GO
ALTER TABLE [dbo].[SystemEvent] CHECK CONSTRAINT [Tank_Event_FK1]
GO
/****** Object:  ForeignKey [Dispenser_DispenserSetting_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[DispenserSetting]  WITH CHECK ADD  CONSTRAINT [Dispenser_DispenserSetting_FK1] FOREIGN KEY([DispenserId])
REFERENCES [dbo].[Dispenser] ([DispenserId])
GO
ALTER TABLE [dbo].[DispenserSetting] CHECK CONSTRAINT [Dispenser_DispenserSetting_FK1]
GO
/****** Object:  ForeignKey [Nozzle_DispenserSetting_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[DispenserSetting]  WITH CHECK ADD  CONSTRAINT [Nozzle_DispenserSetting_FK1] FOREIGN KEY([NozzleId])
REFERENCES [dbo].[Nozzle] ([NozzleId])
GO
ALTER TABLE [dbo].[DispenserSetting] CHECK CONSTRAINT [Nozzle_DispenserSetting_FK1]
GO
/****** Object:  ForeignKey [FK_Invoice_ApplicationUser]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Invoice]  WITH CHECK ADD  CONSTRAINT [FK_Invoice_ApplicationUser] FOREIGN KEY([ApplicationUserId])
REFERENCES [dbo].[ApplicationUser] ([ApplicationUserId])
GO
ALTER TABLE [dbo].[Invoice] CHECK CONSTRAINT [FK_Invoice_ApplicationUser]
GO
/****** Object:  ForeignKey [FK_Invoice_InvoiceForm]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Invoice]  WITH CHECK ADD  CONSTRAINT [FK_Invoice_InvoiceForm] FOREIGN KEY([InvoiceFormId])
REFERENCES [dbo].[InvoiceForm] ([InvoiceFormId])
GO
ALTER TABLE [dbo].[Invoice] CHECK CONSTRAINT [FK_Invoice_InvoiceForm]
GO
/****** Object:  ForeignKey [FK_Invoice_InvoiceType]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Invoice]  WITH CHECK ADD  CONSTRAINT [FK_Invoice_InvoiceType] FOREIGN KEY([InvoiceTypeId])
REFERENCES [dbo].[InvoiceType] ([InvoiceTypeId])
GO
ALTER TABLE [dbo].[Invoice] CHECK CONSTRAINT [FK_Invoice_InvoiceType]
GO
/****** Object:  ForeignKey [FK_Invoice_Vehicle]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Invoice]  WITH CHECK ADD  CONSTRAINT [FK_Invoice_Vehicle] FOREIGN KEY([VehicleId])
REFERENCES [dbo].[Vehicle] ([VehicleId])
GO
ALTER TABLE [dbo].[Invoice] CHECK CONSTRAINT [FK_Invoice_Vehicle]
GO
/****** Object:  ForeignKey [Trader_Invoice_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Invoice]  WITH CHECK ADD  CONSTRAINT [Trader_Invoice_FK1] FOREIGN KEY([TraderId])
REFERENCES [dbo].[Trader] ([TraderId])
GO
ALTER TABLE [dbo].[Invoice] CHECK CONSTRAINT [Trader_Invoice_FK1]
GO
/****** Object:  ForeignKey [Nozzle_NozzleUsagePeriod_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[NozzleUsagePeriod]  WITH CHECK ADD  CONSTRAINT [Nozzle_NozzleUsagePeriod_FK1] FOREIGN KEY([NozzleId])
REFERENCES [dbo].[Nozzle] ([NozzleId])
GO
ALTER TABLE [dbo].[NozzleUsagePeriod] CHECK CONSTRAINT [Nozzle_NozzleUsagePeriod_FK1]
GO
/****** Object:  ForeignKey [UsagePeriod_NozzleUsagePeriod_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[NozzleUsagePeriod]  WITH CHECK ADD  CONSTRAINT [UsagePeriod_NozzleUsagePeriod_FK1] FOREIGN KEY([UsagePeriodId])
REFERENCES [dbo].[UsagePeriod] ([UsagePeriodId])
GO
ALTER TABLE [dbo].[NozzleUsagePeriod] CHECK CONSTRAINT [UsagePeriod_NozzleUsagePeriod_FK1]
GO
/****** Object:  ForeignKey [Nozzle_NozzlePriceList_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[NozzlePriceList]  WITH CHECK ADD  CONSTRAINT [Nozzle_NozzlePriceList_FK1] FOREIGN KEY([NozzleId])
REFERENCES [dbo].[Nozzle] ([NozzleId])
GO
ALTER TABLE [dbo].[NozzlePriceList] CHECK CONSTRAINT [Nozzle_NozzlePriceList_FK1]
GO
/****** Object:  ForeignKey [PriceList_NozzlePriceList_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[NozzlePriceList]  WITH CHECK ADD  CONSTRAINT [PriceList_NozzlePriceList_FK1] FOREIGN KEY([PriceListId])
REFERENCES [dbo].[PriceList] ([PriceListId])
GO
ALTER TABLE [dbo].[NozzlePriceList] CHECK CONSTRAINT [PriceList_NozzlePriceList_FK1]
GO
/****** Object:  ForeignKey [FK_NozzleFlow_Nozzle]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[NozzleFlow]  WITH CHECK ADD  CONSTRAINT [FK_NozzleFlow_Nozzle] FOREIGN KEY([NozzleId])
REFERENCES [dbo].[Nozzle] ([NozzleId])
GO
ALTER TABLE [dbo].[NozzleFlow] CHECK CONSTRAINT [FK_NozzleFlow_Nozzle]
GO
/****** Object:  ForeignKey [FK_NozzleFlow_Tank]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[NozzleFlow]  WITH CHECK ADD  CONSTRAINT [FK_NozzleFlow_Tank] FOREIGN KEY([TankId])
REFERENCES [dbo].[Tank] ([TankId])
GO
ALTER TABLE [dbo].[NozzleFlow] CHECK CONSTRAINT [FK_NozzleFlow_Tank]
GO
/****** Object:  ForeignKey [FK_SalesTransaction_ApplicationUser]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[SalesTransaction]  WITH CHECK ADD  CONSTRAINT [FK_SalesTransaction_ApplicationUser] FOREIGN KEY([ApplicationUserId])
REFERENCES [dbo].[ApplicationUser] ([ApplicationUserId])
GO
ALTER TABLE [dbo].[SalesTransaction] CHECK CONSTRAINT [FK_SalesTransaction_ApplicationUser]
GO
/****** Object:  ForeignKey [FK_SalesTransaction_Nozzle]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[SalesTransaction]  WITH CHECK ADD  CONSTRAINT [FK_SalesTransaction_Nozzle] FOREIGN KEY([NozzleId])
REFERENCES [dbo].[Nozzle] ([NozzleId])
GO
ALTER TABLE [dbo].[SalesTransaction] CHECK CONSTRAINT [FK_SalesTransaction_Nozzle]
GO
/****** Object:  ForeignKey [UsagePeriod_SalesTransaction_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[SalesTransaction]  WITH CHECK ADD  CONSTRAINT [UsagePeriod_SalesTransaction_FK1] FOREIGN KEY([UsagePeriodId])
REFERENCES [dbo].[UsagePeriod] ([UsagePeriodId])
GO
ALTER TABLE [dbo].[SalesTransaction] CHECK CONSTRAINT [UsagePeriod_SalesTransaction_FK1]
GO
/****** Object:  ForeignKey [FK_InvoiceLine_FuelType]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[InvoiceLine]  WITH CHECK ADD  CONSTRAINT [FK_InvoiceLine_FuelType] FOREIGN KEY([FuelTypeId])
REFERENCES [dbo].[FuelType] ([FuelTypeId])
GO
ALTER TABLE [dbo].[InvoiceLine] CHECK CONSTRAINT [FK_InvoiceLine_FuelType]
GO
/****** Object:  ForeignKey [FK_InvoiceLine_Invoice]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[InvoiceLine]  WITH CHECK ADD  CONSTRAINT [FK_InvoiceLine_Invoice] FOREIGN KEY([InvoiceId])
REFERENCES [dbo].[Invoice] ([InvoiceId])
GO
ALTER TABLE [dbo].[InvoiceLine] CHECK CONSTRAINT [FK_InvoiceLine_Invoice]
GO
/****** Object:  ForeignKey [FK_InvoiceLine_SalesTransaction]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[InvoiceLine]  WITH CHECK ADD  CONSTRAINT [FK_InvoiceLine_SalesTransaction] FOREIGN KEY([SaleTransactionId])
REFERENCES [dbo].[SalesTransaction] ([SalesTransactionId])
GO
ALTER TABLE [dbo].[InvoiceLine] CHECK CONSTRAINT [FK_InvoiceLine_SalesTransaction]
GO
/****** Object:  ForeignKey [FK_InvoiceLine_Tank]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[InvoiceLine]  WITH CHECK ADD  CONSTRAINT [FK_InvoiceLine_Tank] FOREIGN KEY([TankId])
REFERENCES [dbo].[Tank] ([TankId])
GO
ALTER TABLE [dbo].[InvoiceLine] CHECK CONSTRAINT [FK_InvoiceLine_Tank]
GO
/****** Object:  ForeignKey [FK_InvoiceLine_TankFilling]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[InvoiceLine]  WITH CHECK ADD  CONSTRAINT [FK_InvoiceLine_TankFilling] FOREIGN KEY([TankFillingId])
REFERENCES [dbo].[TankFilling] ([TankFillingId])
GO
ALTER TABLE [dbo].[InvoiceLine] CHECK CONSTRAINT [FK_InvoiceLine_TankFilling]
GO
/****** Object:  ForeignKey [Invoice_InvoiceRelation_FK1]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[InvoiceRelation]  WITH CHECK ADD  CONSTRAINT [Invoice_InvoiceRelation_FK1] FOREIGN KEY([ParentInvoiceId])
REFERENCES [dbo].[Invoice] ([InvoiceId])
GO
ALTER TABLE [dbo].[InvoiceRelation] CHECK CONSTRAINT [Invoice_InvoiceRelation_FK1]
GO
/****** Object:  ForeignKey [Invoice_InvoiceRelation_FK2]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[InvoiceRelation]  WITH CHECK ADD  CONSTRAINT [Invoice_InvoiceRelation_FK2] FOREIGN KEY([ChildInvoiceId])
REFERENCES [dbo].[Invoice] ([InvoiceId])
GO
ALTER TABLE [dbo].[InvoiceRelation] CHECK CONSTRAINT [Invoice_InvoiceRelation_FK2]
GO
/****** Object:  ForeignKey [FK_Balance_ApplicationUser]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Balance]  WITH CHECK ADD  CONSTRAINT [FK_Balance_ApplicationUser] FOREIGN KEY([ApplicationUserId])
REFERENCES [dbo].[ApplicationUser] ([ApplicationUserId])
GO
ALTER TABLE [dbo].[Balance] CHECK CONSTRAINT [FK_Balance_ApplicationUser]
GO
/****** Object:  ForeignKey [FK_Balance_SalesTransaction]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Balance]  WITH CHECK ADD  CONSTRAINT [FK_Balance_SalesTransaction] FOREIGN KEY([LastSale])
REFERENCES [dbo].[SalesTransaction] ([SalesTransactionId])
GO
ALTER TABLE [dbo].[Balance] CHECK CONSTRAINT [FK_Balance_SalesTransaction]
GO
/****** Object:  ForeignKey [FK_Balance_TankFilling]    Script Date: 06/20/2014 19:13:12 ******/
ALTER TABLE [dbo].[Balance]  WITH CHECK ADD  CONSTRAINT [FK_Balance_TankFilling] FOREIGN KEY([LastFilling])
REFERENCES [dbo].[TankFilling] ([TankFillingId])
GO
ALTER TABLE [dbo].[Balance] CHECK CONSTRAINT [FK_Balance_TankFilling]
GO
/****** Object:  ForeignKey [FK_SystemEventData_SystemEvent]    Script Date: 06/20/2014 19:13:13 ******/
ALTER TABLE [dbo].[SystemEventData]  WITH CHECK ADD  CONSTRAINT [FK_SystemEventData_SystemEvent] FOREIGN KEY([SystemEventId])
REFERENCES [dbo].[SystemEvent] ([EventId])
GO
ALTER TABLE [dbo].[SystemEventData] CHECK CONSTRAINT [FK_SystemEventData_SystemEvent]
GO
/****** Object:  ForeignKey [FK_TankSale_SalesTransaction]    Script Date: 06/20/2014 19:13:13 ******/
ALTER TABLE [dbo].[TankSale]  WITH CHECK ADD  CONSTRAINT [FK_TankSale_SalesTransaction] FOREIGN KEY([SalesTransactionId])
REFERENCES [dbo].[SalesTransaction] ([SalesTransactionId])
GO
ALTER TABLE [dbo].[TankSale] CHECK CONSTRAINT [FK_TankSale_SalesTransaction]
GO
/****** Object:  ForeignKey [FK_TankSale_Tank]    Script Date: 06/20/2014 19:13:13 ******/
ALTER TABLE [dbo].[TankSale]  WITH CHECK ADD  CONSTRAINT [FK_TankSale_Tank] FOREIGN KEY([TankId])
REFERENCES [dbo].[Tank] ([TankId])
GO
ALTER TABLE [dbo].[TankSale] CHECK CONSTRAINT [FK_TankSale_Tank]
GO

INSERT [dbo].[ApplicationUser] ([ApplicationUserId], [UserName], [Password], [UserLevel]) VALUES (N'1e92d2ab-2f7a-4e4e-ac16-e8a609fd3ebe', N'Admin', N'1234', 0)
INSERT [dbo].[ApplicationUser] ([ApplicationUserId], [UserName], [Password], [UserLevel]) VALUES (N'5a414e23-3987-4ce3-a413-e59241651154', N'Διευθυντής', N'1234', 1)
INSERT [dbo].[ApplicationUser] ([ApplicationUserId], [UserName], [Password], [UserLevel]) VALUES (N'9c884274-7822-4476-8dc0-bbac58a66aec', N'Χρήστης', N'111', 2)
GO

INSERT [dbo].[AtgProbeProtocol] ([AtgProbeProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'9de24082-7394-4e4f-9af7-1a1487d78c06', N'ENRAF Height-, Volume-protocol', 2)
INSERT [dbo].[AtgProbeProtocol] ([AtgProbeProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'615db050-5ca0-4427-a871-1abce2894530', N'HECTRONIC HLS', 5)
INSERT [dbo].[AtgProbeProtocol] ([AtgProbeProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'5be6bc74-677c-4b78-8698-2be825d12be8', N'Assytech', 1)
INSERT [dbo].[AtgProbeProtocol] ([AtgProbeProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'f4399d9a-d164-4aad-a161-4525209ad9fd', N'FAFNIR VISY-Quick', 3)
INSERT [dbo].[AtgProbeProtocol] ([AtgProbeProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'00a2e1a3-d6e8-4414-97ee-907f892e93bb', N'START ITALIANA SMT/XMT', 8)
INSERT [dbo].[AtgProbeProtocol] ([AtgProbeProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'e6fa9c69-c1e9-469b-8a44-a7a8716f00de', N'TiT UniProbe', 10)
INSERT [dbo].[AtgProbeProtocol] ([AtgProbeProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'14f1c568-2f3a-4348-a439-bab8e5d67f80', N'GILBARCO Veeder Root', 4)
INSERT [dbo].[AtgProbeProtocol] ([AtgProbeProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'7af48977-9559-4996-bba7-bfb5ac2475e5', N'MTS USTD', 6)
INSERT [dbo].[AtgProbeProtocol] ([AtgProbeProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'1b749d69-8cbe-48f4-b814-c3376362c277', N'PETROVEND4', 7)
INSERT [dbo].[AtgProbeProtocol] ([AtgProbeProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'd5b1652b-0443-45c0-8859-f8a48de49ca2', N'STRUNA Kedr spec. 1.4', 9)
GO

INSERT [dbo].[AtgProbeType] ([AtgProbeTypeId], [AtgProbeProtocolId], [BrandName]) VALUES (N'ac5ccb12-eed8-43fc-bc3f-06b53b8e5f6e', N'5be6bc74-677c-4b78-8698-2be825d12be8', N'ASSYTECH')
INSERT [dbo].[AtgProbeType] ([AtgProbeTypeId], [AtgProbeProtocolId], [BrandName]) VALUES (N'3b7c6297-214a-44b0-92b3-14e725e381d0', N'14f1c568-2f3a-4348-a439-bab8e5d67f80', N'FRANKLIN FUELING')
INSERT [dbo].[AtgProbeType] ([AtgProbeTypeId], [AtgProbeProtocolId], [BrandName]) VALUES (N'699cb098-de24-4545-bc2c-17526d9209f4', N'd5b1652b-0443-45c0-8859-f8a48de49ca2', N'STRUNA')
INSERT [dbo].[AtgProbeType] ([AtgProbeTypeId], [AtgProbeProtocolId], [BrandName]) VALUES (N'9f959ce7-e834-4083-aaac-455e82fa1def', N'615db050-5ca0-4427-a871-1abce2894530', N'HECTRONIC')
INSERT [dbo].[AtgProbeType] ([AtgProbeTypeId], [AtgProbeProtocolId], [BrandName]) VALUES (N'207f50bb-2ff6-4749-99e6-49d88d7f957f', N'14f1c568-2f3a-4348-a439-bab8e5d67f80', N'OPW')
INSERT [dbo].[AtgProbeType] ([AtgProbeTypeId], [AtgProbeProtocolId], [BrandName]) VALUES (N'f909cd10-a669-4a27-b4aa-5437d426e5c8', N'14f1c568-2f3a-4348-a439-bab8e5d67f80', N'LABKO')
INSERT [dbo].[AtgProbeType] ([AtgProbeTypeId], [AtgProbeProtocolId], [BrandName]) VALUES (N'6e276ee9-5d12-47c4-918a-580f44cf2676', N'14f1c568-2f3a-4348-a439-bab8e5d67f80', N'INCON')
INSERT [dbo].[AtgProbeType] ([AtgProbeTypeId], [AtgProbeProtocolId], [BrandName]) VALUES (N'548df453-db09-4122-ab7a-715ff2fdfcab', N'e6fa9c69-c1e9-469b-8a44-a7a8716f00de', N'UNIPROBE')
INSERT [dbo].[AtgProbeType] ([AtgProbeTypeId], [AtgProbeProtocolId], [BrandName]) VALUES (N'3bcc5060-2e5d-4668-a75d-839259b905e0', N'14f1c568-2f3a-4348-a439-bab8e5d67f80', N'OMNTEC')
INSERT [dbo].[AtgProbeType] ([AtgProbeTypeId], [AtgProbeProtocolId], [BrandName]) VALUES (N'9fec2811-5503-4ed1-8a91-93c871022465', N'14f1c568-2f3a-4348-a439-bab8e5d67f80', N'FAFNIR')
INSERT [dbo].[AtgProbeType] ([AtgProbeTypeId], [AtgProbeProtocolId], [BrandName]) VALUES (N'e73f56d6-900f-489f-9fab-a4be0b51ea2f', N'1b749d69-8cbe-48f4-b814-c3376362c277', N'PETRO VEND')
INSERT [dbo].[AtgProbeType] ([AtgProbeTypeId], [AtgProbeProtocolId], [BrandName]) VALUES (N'95b70408-f3f1-4c9e-b040-b9c4d0d6cdaf', N'9de24082-7394-4e4f-9af7-1a1487d78c06', N'ENRAF')
INSERT [dbo].[AtgProbeType] ([AtgProbeTypeId], [AtgProbeProtocolId], [BrandName]) VALUES (N'0bdd8e3e-d71b-4f69-a1d0-e204103a2727', N'00a2e1a3-d6e8-4414-97ee-907f892e93bb', N'START ITALIANA')
INSERT [dbo].[AtgProbeType] ([AtgProbeTypeId], [AtgProbeProtocolId], [BrandName]) VALUES (N'29c5c4a9-53bb-4f86-887d-eadaf8552926', N'7af48977-9559-4996-bba7-bfb5ac2475e5', N'MTS ATG SENSORS')
INSERT [dbo].[AtgProbeType] ([AtgProbeTypeId], [AtgProbeProtocolId], [BrandName]) VALUES (N'f02d57c8-396e-4203-ae2f-f3cf725a391f', N'f4399d9a-d164-4aad-a161-4525209ad9fd', N'FAFNIR')
INSERT [dbo].[AtgProbeType] ([AtgProbeTypeId], [AtgProbeProtocolId], [BrandName]) VALUES (N'838e2512-c4b4-4da9-a9af-ffaaa36cfc6e', N'14f1c568-2f3a-4348-a439-bab8e5d67f80', N'GILBARCO Veeder Root (TLS-2, TLS-300, TLS-350, TLS-450)')
GO

INSERT [dbo].[FuelType] ([FuelTypeId], [Name], [Code], [Color], [ThermalCoeficient], [EnumeratorValue], [BaseDensity]) VALUES (N'1ee40ff4-ce7c-41b0-8a89-117b4aa5c185', N'ΑΜΟΛΥΒΔΗ 100', N'UL100', -14761442, CAST(0.001381000 AS Decimal(18, 9)), 12, CAST(755.000 AS Decimal(18, 3)))
INSERT [dbo].[FuelType] ([FuelTypeId], [Name], [Code], [Color], [ThermalCoeficient], [EnumeratorValue], [BaseDensity]) VALUES (N'f56f5138-b7d7-4da0-8291-237789b61d10', N'ΑΜΟΛΥΒΔΗ 95', N'UL95', -15231977, CAST(0.001361000 AS Decimal(18, 9)), 10, CAST(744.000 AS Decimal(18, 3)))
INSERT [dbo].[FuelType] ([FuelTypeId], [Name], [Code], [Color], [ThermalCoeficient], [EnumeratorValue], [BaseDensity]) VALUES (N'3541aad8-5dd0-478f-810e-4153308fd40e', N'Diesel', N'DL', -13027015, CAST(0.000814000 AS Decimal(18, 9)), 20, CAST(835.000 AS Decimal(18, 3)))
INSERT [dbo].[FuelType] ([FuelTypeId], [Name], [Code], [Color], [ThermalCoeficient], [EnumeratorValue], [BaseDensity]) VALUES (N'7624b3ec-691f-48e5-a6d4-88ce635f4396', N'Diesel PREMIUM', N'DLP', -13092808, CAST(0.000814000 AS Decimal(18, 9)), 21, CAST(836.000 AS Decimal(18, 3)))
INSERT [dbo].[FuelType] ([FuelTypeId], [Name], [Code], [Color], [ThermalCoeficient], [EnumeratorValue], [BaseDensity]) VALUES (N'afd91459-d80f-476c-b909-9d6b6e63f45f', N'LRP', N'LRP', 16766976, CAST(0.001357000 AS Decimal(18, 9)), 13, CAST(742.000 AS Decimal(18, 3)))
INSERT [dbo].[FuelType] ([FuelTypeId], [Name], [Code], [Color], [ThermalCoeficient], [EnumeratorValue], [BaseDensity]) VALUES (N'827eb887-cf8a-48c5-a37b-af2c36cc1d49', N'ΠΕΤΡΕΛΑΙΟ ΘΕΡΜΑΝΣΗΣ', N'DLH', 16711680, CAST(0.000828000 AS Decimal(18, 9)), 30, CAST(850.000 AS Decimal(18, 3)))
INSERT [dbo].[FuelType] ([FuelTypeId], [Name], [Code], [Color], [ThermalCoeficient], [EnumeratorValue], [BaseDensity]) VALUES (N'90e5cd96-ab7b-4540-a573-b8951fca1435', N'ΑΜΟΛΥΒΔΗ 95+', N'UL95P', -15165928, CAST(0.001361000 AS Decimal(18, 9)), 11, CAST(744.000 AS Decimal(18, 3)))
INSERT [dbo].[FuelType] ([FuelTypeId], [Name], [Code], [Color], [ThermalCoeficient], [EnumeratorValue], [BaseDensity]) VALUES (N'5b205aaa-36ca-4bd5-aab5-e842ed5437e2', N'LPG', N'LPG', 180, CAST(0.000000000 AS Decimal(18, 9)), 40, CAST(0.000 AS Decimal(18, 3)))
INSERT [dbo].[FuelType] ([FuelTypeId], [Name], [Code], [Color], [ThermalCoeficient], [EnumeratorValue], [BaseDensity]) VALUES (N'5251b6be-d7ee-4977-a807-e94396c6cc12', N'ΦΩΤΙΣΤΙΚΟ ΠΕΤΡΕΛΑΙΟ', N'PO', 16711680, CAST(0.000000000 AS Decimal(18, 9)), 32, CAST(0.000 AS Decimal(18, 3)))
INSERT [dbo].[FuelType] ([FuelTypeId], [Name], [Code], [Color], [ThermalCoeficient], [EnumeratorValue], [BaseDensity]) VALUES (N'9227828e-1b48-4288-ab1c-f805c4ab873c', N'ΠΕΤΡΕΛΑΙΟ ΘΕΡΜΑΝΣΗΣ PREMIUM', N'DLHP', 16711680, CAST(0.000828000 AS Decimal(18, 9)), 31, CAST(850.000 AS Decimal(18, 3)))
GO

INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId]) VALUES (N'3f54a35b-01f6-43b8-a17e-2d4927e319b8', N'Παραστατικό Λιτρομέτρησης', N'ΠΛΜ', 0, 0, 1, 160, N'C:\ASFuelControl\Sign', NULL)
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId]) VALUES (N'25330fa4-dc1d-428b-bd8f-34d7cf00dda4', N'Τιμολόγιο Δελτίο Αποστολής (Αγορών)', N'ΤΠΥ ΑΓ', 0, 1, 0, 221, N'C:\ASFuelControl\Sign', NULL)
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId]) VALUES (N'e75c52e3-9207-4be3-96d0-56eb98d7b782', N'Δελτίο Εξαγωγής Εξυδάτωσης', N'ΔΕ. Εξυδ', 0, 0, 1, 501, N'C:\ASFuelControl\Sign', NULL)
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId]) VALUES (N'76f18cac-1f11-4b10-860f-72c6b1dac7ff', N'Δελτίο Εξαγωγής για άλλους λόγους', N'ΔΕ.', 0, 0, 1, 501, N'C:\ASFuelControl\Sign', NULL)
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId]) VALUES (N'718c7428-1b23-4c60-9635-81dc2fdcd4f4', N'Δελτίο Επιστροφής', N'Δ.Ε.', 0, 1, 1, 63, N'C:\ASFuelControl\Sign', NULL)
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId]) VALUES (N'394612c6-073c-4820-aa60-828aaecefcb7', N'Απόδειξη Εσόδου', N'ΑΕ', 0, 0, 1, 173, N'C:\ASFuelControl\Sign', NULL)
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId]) VALUES (N'58d7de2e-2648-4b34-acd4-bf4c0590caa3', N'Δελτίο Εξαγωγής για Διύληση', N'ΔΕ. Διυλ.', 0, 0, 1, 501, N'C:\ASFuelControl\Sign', NULL)
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId]) VALUES (N'89499fe5-f0fd-4de6-b239-c74df1b3fe41', N'Τιμολόγιο Δελτίο Αποστολής (Πωλήσεων)', N'ΤΠΥ', 0, 0, 1, 173, N'C:\ASFuelControl\Sign', NULL)
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId]) VALUES (N'fbe44bd8-d3b3-41b7-b454-daa3dcc9bb77', N'Δελτίο Αποστολής', N'Δ.Α. Απ', 0, 1, 1, 158, N'C:\ASFuelControl\Sign', NULL)
GO

INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'0dd314da-c7f2-4857-838b-1c01e1314432', N'CompanyTaxOffice', N'', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'138a1925-f817-4281-8d72-1d833f0c5248', N'CompanyName', N'-', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'ffc1829b-a6e7-43df-b62d-23713fee3dde', N'DeliveryCheckInvoiceType', N'25330fa4-dc1d-428b-bd8f-34d7cf00dda4', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'edc23f13-6d1a-4041-9682-330ce13afc42', N'SenderTIN', N'998549751', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'4ddf7bef-21e2-4272-af02-3e533ecec74e', N'CompanyPhone', N'-', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'2cba40f6-c244-4b4d-89a3-46e93233c788', N'CompanyOccupation', N'ΠΡΑΤΗΡΙΟ ΥΓΡΩΝ ΚΑΥΣΙΜΩΝ - ΛΙΠΑΝΤΙΚΑ', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'48a74c66-f80f-4e80-937b-4bb380c64d6e', N'IsFinalized', N'True', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'3a5f0b71-67d9-4381-80b7-51938d9742f1', N'SendData', N'False', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'0dcab9b6-26b0-4e89-89b0-553aea444d8c', N'CompanyPostalCode', N'-', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'3b41aaf4-3a82-4113-ac45-5d91fdf5fe71', N'TankCheckInterval', N'300', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'ff2864aa-8dba-45b9-b2ad-5f9bbea56d42', N'TailInvoiceLine', N'10', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'79974f90-acd0-41c3-b79d-6afba947ed7e', N'DBVersion', N'0', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'63d743a2-ba11-48a7-8733-6df5b9318357', N'LiterCheckWaitingTime', N'60', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'5124661d-ddc3-42da-9ad3-87cdd9911894', N'ReturnInvoiceType', N'718c7428-1b23-4c60-9635-81dc2fdcd4f4', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'12d474b2-6820-4b4b-8e54-9226abaf3322', N'SendCheckInvoiceType', N'fbe44bd8-d3b3-41b7-b454-daa3dcc9bb77', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'b8d6084c-1e51-4355-b384-94287e80ab74', N'DefaultTaxDevice', N'SamTec', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'bf8d29b9-1001-4055-8beb-a4e844a4d4bd', N'CompanyCity', N'-', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'dad31d8d-dca3-4519-9e3a-a8fd3705fa70', N'CompanyTIN', N'-', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'd9bcf645-24e0-414e-9ee1-b04e572cf160', N'CompanyAddress', N'-', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'89cad4b1-9d98-40c1-a1bc-b34701972fb2', N'Samtec_OutFolder', N'', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'2561de33-b919-4dfe-81dd-da675701027f', N'LiterCheckInvoiceType', N'3f54a35b-01f6-43b8-a17e-2d4927e319b8', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'2b84208e-adc6-443e-813b-db2072e5a248', N'SignA_SignFolder', N'', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'c65d5966-3665-4239-8433-1a631f95a4ab', N'Samtec_SignFolder', N'', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'd6061240-0e0e-484f-b168-ee60de584d06', N'CompanyFax', N'', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'30e4f02a-c89b-4580-b1c2-f1e5b95df176', N'AMDIKA', N'0', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'11dca8e9-8baa-4274-8205-f26028f089c7', N'BalanceAtMinute', N'1439', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'cff0e7a0-6bb8-432d-a8d6-f29e3b9acd72', N'SingLine', N'[<][AFM]/[CustomerAfm]//[Date]/[Description]/[Series]/[InvoiceNumber]/[AmountA]/[AmountB]/[AmountC]/[AmountD]/[AmountE]/[TaxA]/[TaxB]/[TaxC]/[TaxD]/[SUM]/[Currancy][>]', NULL)
INSERT [dbo].[Option] ([OptionId], [OptionKey], [OptionValue], [Description]) VALUES (N'595a28ff-da3c-45b0-a5c0-f9404df32f00', N'DeliveryWaitingTime', N'60', NULL)
GO

INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'2281da89-734d-4e4a-a125-01091992024c', N'Tokheim UDC', 33)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'f069b2e3-1508-49a3-9a70-10724d814d53', N'PumpControl GC21', 20)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'11131ce0-58d6-40ce-87fa-13d8ec926495', N'HongYang 886 communication protocol', 10)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'73099df7-5231-474a-8b07-15ca3f4bccaf', N'TATSUNO Benc PDE', 28)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'6c40da75-4bd0-4c50-9a4e-1d161a92cfbf', N'BENNETT pump dispenser protocol (RS-485)', 3)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'058edc36-9144-4238-83cd-2ba5538bc3ea', N'TIT UniGaz', 30)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'c3166ae2-3329-49b2-add8-33743c11a09b', N'SHELF', 25)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'f0889fbf-4025-49ee-9c57-36ac509b8750', N'Blue Sky', 4)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'52481695-50ee-4ae8-9c5f-3b4011c05300', N'MNET design specification', 15)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'72fea9d5-c3e5-49ec-b217-4cc703884ea4', N'Topaz Electro', 36)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'94b8d3da-6c5b-40ea-bcfc-536b3afade1d', N'SAFE Graf', 23)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'33602bf6-18b3-4b0b-9f76-6612b0901642', N'HongYang FZ-protocol', 11)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'872e9985-2724-4bf4-89e1-69cdeac0b244', N'GILBARCO Australia Two-Wire', 8)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'85cbc389-5763-407c-9344-6c33403354b3', N'RS–232 Protocol of Dispenser', 21)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'07204abe-24bc-4104-81a8-6e15df32c3dd', N'S4-Dart', 22)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'faf0468a-fb85-4f93-8500-6e8564a3589e', N'TATSUNO SS-LAN', 29)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'619b97b7-2eb1-4675-934f-71f14b8dc1ed', N'Tominaga SS-LAN', 35)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'dfe1ff5f-569b-43da-89a0-78395809068f', N'SPDC-1, MPDC-1', 27)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'0530d14f-afae-43bc-b9e4-79c63b756e8e', N'Prime pump interface', 19)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'c88dae3e-f302-4ec6-a3d7-7caaec93f674', N'ADAST EasyCall', 1)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'8b4b8e82-c8b7-45d2-89d8-8b674a57d2ee', N'SLAVUTICH FD-Link', 26)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'c73fc5bd-49ff-41cb-a26a-8c2ba0ea4bf7', N'DEVELCO', 5)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'1fbc026c-827f-4baf-b755-8fe337915eae', N'Marconi PumaLAN', 0)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'0430cfdb-01ac-41d5-89a0-9759df50df2e', N'WAYNE Dart', 37)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'd59b12ae-47f2-46f9-8c02-9918564df396', N'EnE Dispenser POS protocol', 6)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'9fdf3e82-a3e2-4884-a126-aefa4e2679d1', N'TIT UniPump', 31)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'cbcda1c9-d438-4b68-a8f9-cd8d2782720e', N'PEC Pump Communication Protocol', 17)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'2035ccb6-6270-4bf8-b0f9-cee4d7386d8f', N'Wayne US Current Loop', 38)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'0591baf3-4fce-450a-b97c-d5b2f04c3446', N'Tokheim Controller-Dispenser Communication protocol', 32)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'e99ba1c7-f983-450e-a0c6-daf4980fbd5d', N'Sanki communication protocol', 24)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'14875e68-5654-43d7-b5a7-dbff1cbe83b4', N'Nuovo Pignone', 16)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'5ce1be4a-a0d8-41fc-9c17-e2e4e0fce204', N'Kalvacha', 12)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'86e745e9-15b9-4ce8-984c-e7ce83fda619', N'GILBARCO Two-Wire', 9)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'14d2f53a-30e5-4e8c-9eea-ea817a9754d2', N'EPCO protocol specification', 7)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'5f1f0156-1a65-4fec-aa7a-ef6fcb827b19', N'Tokico SS-LAN', 34)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'66b45f22-745e-4f37-b9c2-ef7e4de9b733', N'POS protocol', 18)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'2e5b5639-c31b-4c31-a786-f4187cdc7a7d', N'MM PETRO ZAP RS-485', 14)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'6ac09a21-cba7-4b5d-9584-f86424b02f95', N'Pumb Simulator 10', 2)
INSERT [dbo].[DispenserProtocol] ([DispenserProtocolId], [ProtocolName], [EnumeratorValue]) VALUES (N'56a10849-3dab-4187-bf38-fc202d99dc9e', N'BENNETT pump dispenser protocol (current loop)', 13)
GO

INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'd8006ebc-9eef-4d51-8760-0565b94a1917', N'2281da89-734d-4e4a-a125-01091992024c', N'IMW')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'f71a754e-8a97-42fc-97a8-0a6960d7466a', N'0430cfdb-01ac-41d5-89a0-9759df50df2e', N'Wayne Pignone')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'3408f58e-4282-4aef-9cc4-0c38fd5b1854', N'c73fc5bd-49ff-41cb-a26a-8c2ba0ea4bf7', N'Develco')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'534b3a4f-2571-4828-b2fd-0d77b8705c82', N'619b97b7-2eb1-4675-934f-71f14b8dc1ed', N'Tominaga')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'e6222ba8-b3ff-4aa0-80da-0e8bd1dc2a0a', N'f0889fbf-4025-49ee-9c57-36ac509b8750', N'Blue Sky')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'ac0c21d8-4848-4e62-9d8d-1c405c1f21bb', N'52481695-50ee-4ae8-9c5f-3b4011c05300', N'Kraus')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'd2fcae0d-fecf-4f9a-8ada-1d10105e8929', N'872e9985-2724-4bf4-89e1-69cdeac0b244', N'Batchen')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'f1784cfd-a18e-429b-bf54-21e27b72ba1c', N'86e745e9-15b9-4ce8-984c-e7ce83fda619', N'Petrotec')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'8c20f593-4bec-4e0e-8319-21ff8abe7a03', N'1fbc026c-827f-4baf-b755-8fe337915eae', N'Logitron')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'69470ab8-8774-4608-80ae-2548f891689f', N'07204abe-24bc-4104-81a8-6e15df32c3dd', N'2A')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'ef80bc27-fca3-4b6d-bcaf-2701c212fa03', N'0530d14f-afae-43bc-b9e4-79c63b756e8e', N'Dong Hwa Prime')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'200a3743-abf5-44f9-9a88-2f67e517f0b3', N'33602bf6-18b3-4b0b-9f76-6612b0901642', N'HongYang')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'876ee960-701c-46af-92bb-3018f61c9276', N'f0889fbf-4025-49ee-9c57-36ac509b8750', N'Real-Tech')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'f9311eec-72fe-4b48-92c6-368128b42ee6', N'f069b2e3-1508-49a3-9a70-10724d814d53', N'Agira')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'3197df42-d402-4f52-b2b4-3694f8cce4c9', N'0430cfdb-01ac-41d5-89a0-9759df50df2e', N'Petrolmeccanica')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'a8a83315-27a5-481f-b2bd-39ab9ecbc46f', N'56a10849-3dab-4187-bf38-fc202d99dc9e', N'Bennett')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'94240d5c-cc2c-41e7-bde8-3da541afeac0', N'07204abe-24bc-4104-81a8-6e15df32c3dd', N'Petposan')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'22f10c4c-0418-4800-a13d-4508f6393767', N'f069b2e3-1508-49a3-9a70-10724d814d53', N'Aspro')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'47887003-2b24-4e83-bd30-493f4fb6766d', N'faf0468a-fb85-4f93-8500-6e8564a3589e', N'Tatsuno (Japan)')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'88f6e101-97ea-40bb-9a6f-4a65b5470cf6', N'1fbc026c-827f-4baf-b755-8fe337915eae', N'EMGAZ Dragon')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'604ef96f-27b1-414f-b84d-4aa7326ab36f', N'058edc36-9144-4238-83cd-2ba5538bc3ea', N'KPG-2')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'c42ea05e-2311-457c-bc08-4ebe45d52abd', N'14d2f53a-30e5-4e8c-9eea-ea817a9754d2', N'EPCO')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'6d31db1d-3289-417c-b5af-58afd7f6dc37', N'6ac09a21-cba7-4b5d-9584-f86424b02f95', N'Simulator')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'f899ce91-0427-4a4b-b69d-58ff0114ae08', N'86e745e9-15b9-4ce8-984c-e7ce83fda619', N'Falcon LPG')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'a80b4f83-ab08-4c2f-a4ec-5933861c24db', N'8b4b8e82-c8b7-45d2-89d8-8b674a57d2ee', N'Slavutich')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'8f18fe6b-8410-45ac-8399-59e4d57ce5f8', N'5f1f0156-1a65-4fec-aa7a-ef6fcb827b19', N'Tokico')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'0a52a336-1f62-4c60-aa97-5d35e037c37d', N'f069b2e3-1508-49a3-9a70-10724d814d53', N'IMW')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'8d494298-55d1-4882-80df-635f540d520c', N'c88dae3e-f302-4ec6-a3d7-7caaec93f674', N'ADAST (Adamov systems)')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'55b7eb55-a933-4d1e-9db4-79c6ac4db696', N'c3166ae2-3329-49b2-add8-33743c11a09b', N'Shelf')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'7ebb7d08-58a8-4ff8-87b1-79f755e8b77e', N'5ce1be4a-a0d8-41fc-9c17-e2e4e0fce204', N'Kalvacha')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'6a6dcbd5-b08e-4247-9fbd-7c50e092827d', N'11131ce0-58d6-40ce-87fa-13d8ec926495', N'HongYang')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'b55272fb-908d-419f-9ece-7ce99a02390b', N'07204abe-24bc-4104-81a8-6e15df32c3dd', N'EuroPump')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'0a778903-57de-4ee5-939b-7ebdab066014', N'e99ba1c7-f983-450e-a0c6-daf4980fbd5d', N'Sanki')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'209853bb-3f71-4a4e-b846-832ccb9fbbef', N'94b8d3da-6c5b-40ea-bcfc-536b3afade1d', N'SAFE')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'43d51fe8-fefe-4d9d-b7d3-882751f442a2', N'dfe1ff5f-569b-43da-89a0-78395809068f', N'Prowalco')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'e299b682-38ea-4c93-8d9d-8d6e55940318', N'14875e68-5654-43d7-b5a7-dbff1cbe83b4', N'Nuovo Pignone')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'08041fbf-737a-42b5-b029-8d9239edf64b', N'07204abe-24bc-4104-81a8-6e15df32c3dd', N'Mekser')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'4d376531-a53a-4b81-9fdf-8f9166cdc866', N'f069b2e3-1508-49a3-9a70-10724d814d53', N'Pump Control')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'f1e1a86f-aa7b-43df-a846-917ecc836660', N'86e745e9-15b9-4ce8-984c-e7ce83fda619', N'Salzkotten')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'6a4eb868-95ed-448e-a6db-95f1d0fa548b', N'2e5b5639-c31b-4c31-a786-f4187cdc7a7d', N'ZAP / MM Petro')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'd732c2a0-7526-4458-8b66-9ecd7eb933e0', N'cbcda1c9-d438-4b68-a8f9-cd8d2782720e', N'PEC (Gallagher Fuel Systems)')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'7eac5bbf-bdf5-4668-a589-a9692af6a315', N'0430cfdb-01ac-41d5-89a0-9759df50df2e', N'Meksan / Wayne SU86')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'11458fe7-1726-4642-8d57-acb938c19895', N'07204abe-24bc-4104-81a8-6e15df32c3dd', N'Mepsan')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'a1ee3ab3-3ac9-4072-abaf-adbee297df77', N'f069b2e3-1508-49a3-9a70-10724d814d53', N'Galileo')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'2d0f43eb-4134-4e5c-8064-bf4399bebc33', N'd59b12ae-47f2-46f9-8c02-9918564df396', N'LG EnE')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'e4feb4ce-7c4d-45d7-9de3-c1693e625254', N'0430cfdb-01ac-41d5-89a0-9759df50df2e', N'Wayne Dresser')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'bb1fef98-7d10-45de-8595-c5741c072c1f', N'872e9985-2724-4bf4-89e1-69cdeac0b244', N'Batchen')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'e532eba6-bded-4e54-8b2b-cbce0f23d8be', N'0591baf3-4fce-450a-b97c-d5b2f04c3446', N'Tokheim')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'06b98e40-6ee7-47e8-b8e6-cf79e4824e5c', N'07204abe-24bc-4104-81a8-6e15df32c3dd', N'Yenen')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'25d896a5-458f-407f-bfe6-d035a9d35588', N'058edc36-9144-4238-83cd-2ba5538bc3ea', N'KievNIIGaz')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'e1f56a1e-f6d2-4ced-a3dc-dc5d0ef676ed', N'2035ccb6-6270-4bf8-b0f9-cee4d7386d8f', N'Wayne Dresser')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'd6f15269-6510-4dbb-8cb4-ddf4679d929c', N'd59b12ae-47f2-46f9-8c02-9918564df396', N'Korea EnE')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'1ed4fd5c-7f5d-459c-8c1b-e2c3f012acc1', N'f0889fbf-4025-49ee-9c57-36ac509b8750', N'Sea Bird')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'c5a60040-bab9-411e-b8c8-e83a0a09eb1d', N'86e745e9-15b9-4ce8-984c-e7ce83fda619', N'Gilbarco')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'6250d367-38c7-430d-83ab-e9f7b42a8620', N'86e745e9-15b9-4ce8-984c-e7ce83fda619', N'Baransay')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'41d77b30-3d72-4c07-9c00-ea2004e54ea4', N'66b45f22-745e-4f37-b9c2-ef7e4de9b733', N'SOMO Petro')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'14907f6e-4b60-4dc4-86a5-ed77a9c734d7', N'85cbc389-5763-407c-9344-6c33403354b3', N'Lanfeng')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'95de53fa-5e9c-4a71-a4b0-ef5eca832e02', N'6c40da75-4bd0-4c50-9a4e-1d161a92cfbf', N'Bennett')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'0387d257-7c2c-4b99-a387-f70677715227', N'73099df7-5231-474a-8b07-15ca3f4bccaf', N'Tatsuno Europe (former Benc)')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'958ca354-b6a4-472f-a3b5-fc7a81c6f630', N'9fdf3e82-a3e2-4884-a126-aefa4e2679d1', N'UNICON-TiT')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'5264a16b-60d3-47d3-93e3-fc8e92f97b76', N'86e745e9-15b9-4ce8-984c-e7ce83fda619', N'GREENFIELD')
INSERT [dbo].[DispenserType] ([DispenserTypeId], [DispenserProtocolId], [BrandName]) VALUES (N'ce9ce340-f087-4dff-ad65-fee943230256', N'72fea9d5-c3e5-49ec-b217-4cc703884ea4', N'Topaz')
GO
