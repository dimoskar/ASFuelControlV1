CREATE TABLE [dbo].[ShiftSetting](
	[ShiftSettingId] [uniqueidentifier] NOT NULL,
	[ShiftId] [uniqueidentifier] NOT NULL,
	[NozzleId] [uniqueidentifier] NOT NULL,
	[TotalizerStart] [decimal](18, 3) NOT NULL,
	[TotalizerEnd] [decimal](18, 3) NOT NULL,
	CONSTRAINT [PK_ShiftSetting] PRIMARY KEY CLUSTERED 
	(
		[ShiftSettingId] ASC
	)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[ShiftSetting]  WITH CHECK ADD  CONSTRAINT [FK_ShiftSetting_Nozzle] FOREIGN KEY([NozzleId])
REFERENCES [dbo].[Nozzle] ([NozzleId])
GO

ALTER TABLE [dbo].[ShiftSetting]  WITH CHECK ADD  CONSTRAINT [FK_ShiftSetting_Shift] FOREIGN KEY([ShiftId])
REFERENCES [dbo].[Shift] ([ShiftId])
GO

