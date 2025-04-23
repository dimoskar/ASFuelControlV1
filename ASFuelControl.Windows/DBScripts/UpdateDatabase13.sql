CREATE TABLE [dbo].[OutdoorPaymentTerminal](
	[OutdoorPaymentTerminalId] [uniqueidentifier] NOT NULL,
	[ServerIp] [nvarchar](20) NOT NULL,
	[ServerPort] [int] NOT NULL,
	[ClientIp] [nvarchar](20) NOT NULL,
	[ClientPort] [int] NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[TerminalId] [int] NOT NULL,
	[TerminalAssembly] [nvarchar](250) NOT NULL,
	[IsDisabled] [bit] NOT NULL,
 CONSTRAINT [PK_OutdoorPaymentTerminal] PRIMARY KEY CLUSTERED 
 (
	[OutdoorPaymentTerminalId] ASC
 )
)
GO

CREATE TABLE [dbo].[OutdoorPaymentTerminalNozzle](
	[OutdoorPaymentTerminalNozzleId] [uniqueidentifier] NOT NULL,
	[OutdoorPaymentTerminalId] [uniqueidentifier] NULL,
	[NozzleId] [uniqueidentifier] NULL,
	[IsDisabled] [bit] NOT NULL,
 CONSTRAINT [PK_OutdoorPaymentTerminalNozzle] PRIMARY KEY CLUSTERED 
 (
	[OutdoorPaymentTerminalNozzleId] ASC
 )
)
GO

ALTER TABLE [dbo].[OutdoorPaymentTerminalNozzle]  WITH CHECK ADD  CONSTRAINT [FK_OutdoorPaymentTerminalNozzle_Nozzle] FOREIGN KEY([NozzleId])
REFERENCES [dbo].[Nozzle] ([NozzleId])
GO

ALTER TABLE [dbo].[OutdoorPaymentTerminalNozzle]  WITH CHECK ADD  CONSTRAINT [FK_OutdoorPaymentTerminalNozzle_OutdoorPaymentTerminal] FOREIGN KEY([OutdoorPaymentTerminalId])
REFERENCES [dbo].[OutdoorPaymentTerminal] ([OutdoorPaymentTerminalId])
GO

CREATE TABLE [dbo].[OutdoorPaymentTerminalSchedule](
	[OutdoorPaymentTerminalScheduleId] [uniqueidentifier] NOT NULL,
	[OutdoorPaymentTerminalId] [uniqueidentifier] NOT NULL,
	[DayOfWeek] [int] NULL,
	[IsDayOff] [bit] NULL,
	[ScheduleDate] [datetime] NULL,
	[ScheduleType] [int] NOT NULL,
	[OutdoorPaymentTerminalNozzleId] [uniqueidentifier] NULL,
 CONSTRAINT [PK_OutdoorPaymentTerminalSchedule] PRIMARY KEY CLUSTERED 
 (
	[OutdoorPaymentTerminalScheduleId] ASC
 )
)

GO

ALTER TABLE [dbo].[OutdoorPaymentTerminalSchedule]  WITH CHECK ADD  CONSTRAINT [FK_OutdoorPaymentTerminalSchedule_OutdoorPaymentTerminal] FOREIGN KEY([OutdoorPaymentTerminalId])
REFERENCES [dbo].[OutdoorPaymentTerminal] ([OutdoorPaymentTerminalId])
GO

ALTER TABLE [dbo].[OutdoorPaymentTerminalSchedule]  WITH CHECK ADD  CONSTRAINT [FK_OutdoorPaymentTerminalSchedule_OutdoorPaymentTerminalNozzle] FOREIGN KEY([OutdoorPaymentTerminalNozzleId])
REFERENCES [dbo].[OutdoorPaymentTerminalNozzle] ([OutdoorPaymentTerminalNozzleId])
GO

CREATE TABLE [dbo].[OutdoorPaymentTerminalTimeSchedule](
	[OutdoorPaymentTerminalTimeScheduleId] [uniqueidentifier] NOT NULL,
	[OutdoorPaymentTerminalScheduleId] [uniqueidentifier] NOT NULL,
	[TimeFrom] [time](7) NOT NULL,
	[Duration] [int] NOT NULL,
 CONSTRAINT [PK_OutdoorPaymentTerminalTimeSchedule] PRIMARY KEY CLUSTERED 
 (
	[OutdoorPaymentTerminalTimeScheduleId] ASC
 )
)

GO

ALTER TABLE [dbo].[OutdoorPaymentTerminalTimeSchedule]  WITH CHECK ADD  CONSTRAINT [FK_OutdoorPaymentTerminalTimeSchedule_OutdoorPaymentTerminalSchedule] FOREIGN KEY([OutdoorPaymentTerminalScheduleId])
REFERENCES [dbo].[OutdoorPaymentTerminalSchedule] ([OutdoorPaymentTerminalScheduleId])
GO

CREATE TABLE [dbo].[OutdoorPaymentTerminalController](
	[OutdoorPaymentTerminalControllerId] [uniqueidentifier] NOT NULL,
	[OutdoorPaymentTerminalId] [uniqueidentifier] NOT NULL,
	[CommunicationControllerId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_OutdoorPaymentTerminalController] PRIMARY KEY CLUSTERED 
 (
	[OutdoorPaymentTerminalControllerId] ASC
 )
)

GO

ALTER TABLE [dbo].[OutdoorPaymentTerminalController]  WITH CHECK ADD  CONSTRAINT [FK_OutdoorPaymentTerminalController_CommunicationController] FOREIGN KEY([CommunicationControllerId])
REFERENCES [dbo].[CommunicationController] ([CommunicationControllerId])
GO

ALTER TABLE [dbo].[OutdoorPaymentTerminalController]  WITH CHECK ADD  CONSTRAINT [FK_OutdoorPaymentTerminalController_OutdoorPaymentTerminalController] FOREIGN KEY([OutdoorPaymentTerminalId])
REFERENCES [dbo].[OutdoorPaymentTerminal] ([OutdoorPaymentTerminalId])
GO


CREATE TABLE [dbo].[TankCheck](
	[TankCheckId] [uniqueidentifier] NOT NULL,
	[TankId] [uniqueidentifier] NOT NULL,
	[TankLevel] [decimal](18, 2) NOT NULL,
	[CheckDate] [datetime] NOT NULL,
 CONSTRAINT [PK_TankCheck] PRIMARY KEY CLUSTERED 
 (
	[TankCheckId] ASC
 )
)

GO

ALTER TABLE [dbo].[TankCheck]  WITH CHECK ADD  CONSTRAINT [FK_TankCheck_Tank] FOREIGN KEY([TankId])
REFERENCES [dbo].[Tank] ([TankId])
GO
