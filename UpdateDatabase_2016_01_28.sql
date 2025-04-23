CREATE TABLE [dbo].[FleetManagmentCotroller]
(
	[FleetManagmentCotrollerId] [uniqueidentifier] NOT NULL,
	[ComPort] [nvarchar](20) NOT NULL,
	[BaudRate] [int] NOT NULL,
	[Parity] [int] NOT NULL,
	[DataBits] [int] NOT NULL,
	[StopBits] [int] NOT NULL,
	CONSTRAINT [PK_FleetManagmentCotroller] PRIMARY KEY CLUSTERED 
	(
		[FleetManagmentCotrollerId] ASC
	)
)
GO


CREATE TABLE [dbo].[FleetManagerDispenser]
(
	[FleetManagerDispenserId] [uniqueidentifier] NOT NULL,
	[FleetManagmentCotrollerId] [uniqueidentifier] NOT NULL,
	[DispenserId] [uniqueidentifier] NOT NULL,
	CONSTRAINT [PK_FleetManagerDispenser] PRIMARY KEY CLUSTERED 
	(
		[FleetManagerDispenserId] ASC
	)
)

GO

ALTER TABLE [dbo].[FleetManagerDispenser]  WITH CHECK ADD  CONSTRAINT [FK_FleetManagerDispenser_Dispenser] FOREIGN KEY([DispenserId])
REFERENCES [dbo].[Dispenser] ([DispenserId])
GO

ALTER TABLE [dbo].[FleetManagerDispenser]  WITH CHECK ADD  CONSTRAINT [FK_FleetManagerDispenser_FleetManagmentCotroller] FOREIGN KEY([FleetManagmentCotrollerId])
REFERENCES [dbo].[FleetManagmentCotroller] ([FleetManagmentCotrollerId])
GO


CREATE TABLE [dbo].[FleetManagmentSchedule]
(
	FleetManagmentScheduleId] [uniqueidentifier] NOT NULL,
	[FleetManagerDispenserId] [uniqueidentifier] NOT NULL,
	[TimeFrom] [int] NOT NULL,
	[TimeTo] [int] NOT NULL,
	[DayMask] [int] NOT NULL,
	CONSTRAINT [PK_FleetManagmentSchedule] PRIMARY KEY CLUSTERED 
	(
		[FleetManagmentScheduleId] ASC
	)
)

GO

ALTER TABLE [dbo].[FleetManagmentSchedule]  WITH CHECK ADD  CONSTRAINT [FK_FleetManagmentSchedule_FleetManagerDispenser] FOREIGN KEY([FleetManagerDispenserId])
REFERENCES [dbo].[FleetManagerDispenser] ([FleetManagerDispenserId])
GO

ALTER TABLE dbo.Vehicle ADD
	CardId nvarchar(100) NULL
GO


ALTER TABLE dbo.FleetManagerDispenser ADD
	InvoiceTypeId uniqueidentifier NOT NULL
GO

ALTER TABLE dbo.FleetManagerDispenser ADD CONSTRAINT
	FK_FleetManagerDispenser_InvoiceType FOREIGN KEY
	(
	InvoiceTypeId
	) REFERENCES dbo.InvoiceType
	(
	InvoiceTypeId
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO






