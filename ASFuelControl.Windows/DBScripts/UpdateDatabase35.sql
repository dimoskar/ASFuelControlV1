update Invoice set [PaymentType] = 1
Where 
(
	select count(*) 
	from [dbo].[FinancialTransaction] 
	where FinancialTransaction.InvoiceId = Invoice.InvoiceId
) = 0
GO

ALTER TABLE dbo.InvoiceType ADD
	AdminView bit NULL,
	Invalidated bit NULL,
	DispenserType bit NULL,
	ForcesDelivery bit NULL,
	RetailInvoice bit NULL,
	IncludeInBalance bit NULL
GO

ALTER TABLE dbo.Invoice ADD
	SupplyNumber nvarchar(100) NULL
GO

ALTER TABLE dbo.Trader ADD
	SupplyNumber nvarchar(100) NULL
GO

ALTER TABLE dbo.FuelType ADD
	SupportsSupplyNumber bit NULL
GO

CREATE TABLE [dbo].[FinTransaction](
	[FinTransactionId] [uniqueidentifier] NOT NULL,
	[ApplicationUserId] [uniqueidentifier] NOT NULL,
	[TraderId] [uniqueidentifier] NULL,
	[InvoiceId] [uniqueidentifier] NULL,
	[TransactionDate] [datetime] NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[CreditAmount] [decimal](18, 2) NOT NULL,
	[DebitAmount] [decimal](18, 2) NOT NULL,
	[TransactionType] [int] NOT NULL,
 CONSTRAINT [PK_FinTransaction] PRIMARY KEY CLUSTERED 
(
	[FinTransactionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[FinTransaction]  WITH CHECK ADD  CONSTRAINT [FK_FinTransaction_ApplicationUser] FOREIGN KEY([ApplicationUserId])
REFERENCES [dbo].[ApplicationUser] ([ApplicationUserId])
GO

ALTER TABLE [dbo].[FinTransaction]  WITH CHECK ADD  CONSTRAINT [FK_FinTransaction_Invoice] FOREIGN KEY([InvoiceId])
REFERENCES [dbo].[Invoice] ([InvoiceId])
ON DELETE CASCADE
GO

CREATE TABLE [dbo].[InvoiceTypeTransform](
	[InvoiceTypeTransformId] [uniqueidentifier] NOT NULL,
	[ParentInvoiceTypeId] [uniqueidentifier] NOT NULL,
	[ChildInvoiceTypeId] [uniqueidentifier] NOT NULL,
	[TransformationMode] [int] NOT NULL,
	[NotesAddition] [nvarchar](500) NOT NULL,
 CONSTRAINT [PK_InvoiceTypeTransform] PRIMARY KEY CLUSTERED 
(
	[InvoiceTypeTransformId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[InvoiceTypeTransform]  WITH CHECK ADD  CONSTRAINT [FK_InvoiceTypeTransform_ChildInvoiceType] FOREIGN KEY([ChildInvoiceTypeId])
REFERENCES [dbo].[InvoiceType] ([InvoiceTypeId])
GO

ALTER TABLE [dbo].[InvoiceTypeTransform]  WITH CHECK ADD  CONSTRAINT [FK_InvoiceTypeTransform_ParentInvoiceType] FOREIGN KEY([ParentInvoiceTypeId])
REFERENCES [dbo].[InvoiceType] ([InvoiceTypeId])
GO


ALTER TABLE dbo.InvoiceLine ADD
	InvoiceRelationId uniqueidentifier NULL
GO

ALTER TABLE dbo.InvoiceLine ADD CONSTRAINT
	FK_InvoiceLine_InvoiceRelation FOREIGN KEY
	(
		InvoiceRelationId
	) REFERENCES dbo.InvoiceRelation
	(
		InvoiceRelationId
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
GO

CREATE TABLE [dbo].[InvoiceLineRelation](
	[InvoiceLineRelationId] [uniqueidentifier] NOT NULL,
	[InvoiceRelationId] [uniqueidentifier] NOT NULL,
	[ParentLineId] [uniqueidentifier] NOT NULL,
	[ChildRelationId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_InvoiceLineRelation] PRIMARY KEY CLUSTERED 
(
	[InvoiceLineRelationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[InvoiceLineRelation]  WITH CHECK ADD  CONSTRAINT [FK_InvoiceLineRelation_ChildInvoiceLine] FOREIGN KEY([ChildRelationId])
REFERENCES [dbo].[InvoiceLine] ([InvoiceLineId])
GO

ALTER TABLE [dbo].[InvoiceLineRelation]  WITH CHECK ADD  CONSTRAINT [FK_InvoiceLineRelation_InvoiceRelation] FOREIGN KEY([InvoiceRelationId])
REFERENCES [dbo].[InvoiceRelation] ([InvoiceRelationId])
GO

ALTER TABLE [dbo].[InvoiceLineRelation]  WITH CHECK ADD  CONSTRAINT [FK_InvoiceLineRelation_ParentInvoiceLine] FOREIGN KEY([ParentLineId])
REFERENCES [dbo].[InvoiceLine] ([InvoiceLineId])
GO

CREATE FUNCTION GetInvoiceFuelTypes
(
	@InvoiceId uniqueidentifier
)
RETURNS nvarchar(1000)
AS
BEGIN
	declare @ret nvarchar(1000)
	set @ret = IsNull((SELECT STUFF
			(
				(
					SELECT DISTINCT ', ' + FuelType.Name
					FROM	InvoiceLine AS s INNER JOIN
								FuelType ON s.FuelTypeId = FuelType.FuelTypeId
					WHERE        (s.InvoiceId = @InvoiceId)
					FOR XML PATH('')
				),
					1, 1, ''
			)
		), '')
	return @ret
END
GO

CREATE FUNCTION dbo.HasLinesToTransform
(
	@InvoiceId uniqueidentifier
)
RETURNS bit
AS
BEGIN
	Declare @ret bit
	Declare @sumInvoice decimal(18,2)
	Declare @sumReplaced decimal(18,2)
	set @sumInvoice = (Select Sum(Volume) from InvoiceLine Where InvoiceId = @InvoiceId)
	set @sumReplaced = 
	IsNull((
		Select Sum(Volume) from InvoiceLine Where InvoiceId in 
		(
			Select [ChildInvoiceId] From InvoiceRelation Where ParentInvoiceId = @InvoiceId
		)
	), 0)
	if(@sumInvoice > @sumReplaced)
		set @ret = 1
	else
		set @ret = 0
	return @ret
END
GO

CREATE VIEW [dbo].[InvoiceCatalogView]
AS
SELECT	a.InvoiceId, 
		a.InvoiceTypeId, 
		a.TransactionDate, 
		ISNULL(a.DiscountAmount, 0) AS DiscountAmount, 
		ISNULL(a.NettoAmount, 0) AS NettoAmount, 
		a.Number, 
		ISNULL(a.PaymentType, 1) AS PaymentType, 
		ISNULL(a.TotalAmount, 0) AS TotalAmount, 
		a.TraderId, 
		c.Name AS TraderName, 
		a.VehicleId, 
		d.PlateNumber, 
		ISNULL(c.VatExemption, 0) AS VatExemption, 
		ISNULL(a.VatAmount, 0) AS VatAmount, 
		b.Abbreviation AS InvoiceTypeDesc, 
		ISNULL(b.IsLaserPrint, 0) AS IsLaserPrint, 
		ISNULL(a.IsPrinted, 0) AS IsPrinted, 
		ISNULL(b.TransactionSign, 1) AS TransactionSign, 
		ISNULL(
			(SELECT        COUNT(*) AS Expr1
                FROM            dbo.InvoiceLine AS il
                WHERE        (a.InvoiceId = InvoiceId)), 0) AS InvoiceLineCount, 
		ISNULL
            ((SELECT        COUNT(*) AS Expr1
                FROM            dbo.InvoiceRelation AS pr
                WHERE        (ParentInvoiceId = a.InvoiceId) AND (RelationType = 0)), 0) AS Cancelation, 
		ISNULL
            ((SELECT        COUNT(*) AS Expr1
                FROM            dbo.InvoiceRelation AS pr
                WHERE        (ChildInvoiceId = a.InvoiceId) AND (RelationType = 0)), 0) AS Canceled, 
		ISNULL
            ((SELECT        COUNT(*) AS Expr1
                FROM            dbo.InvoiceRelation AS pr
                WHERE        (ParentInvoiceId = a.InvoiceId) AND (RelationType = 1)), 0) AS Replaced, 
		ISNULL
            ((SELECT        COUNT(*) AS Expr1
                FROM            dbo.InvoiceRelation AS e
                WHERE        (a.InvoiceId = ParentInvoiceId) AND (RelationType = 1)), 0) AS ReplaceInvoicesCount, 
		ISNULL
            ((SELECT        SUM(DebitAmount) AS Expr1
                FROM            dbo.FinTransaction AS h
                WHERE        (a.InvoiceId IS NOT NULL) AND (a.InvoiceId = InvoiceId)), 0) AS DebitAmount, 
		ISNULL
            ((SELECT        SUM(CreditAmount) AS Expr1
                FROM            dbo.FinTransaction AS h
                WHERE        (a.InvoiceId IS NOT NULL) AND (a.InvoiceId = InvoiceId)), 0) AS CreditAmount,
		dbo.GetInvoiceFuelTypes(a.InvoiceId) as FuelTypes
			
FROM	dbo.Invoice AS a LEFT OUTER JOIN
			dbo.InvoiceType AS b ON a.InvoiceTypeId = b.InvoiceTypeId LEFT OUTER JOIN
			dbo.Trader AS c ON a.TraderId = c.TraderId LEFT OUTER JOIN
			dbo.Vehicle AS d ON a.VehicleId = d.VehicleId
GO

UPDATE [dbo].[InvoiceType] set [Invalidated] = 1, [IncludeInBalance] = 1
GO

INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId], [IsInternal], [InternalDeliveryDescription], [NeedsVehicle], [IsCancelation], [HasFinancialTransactions], [IsLaserPrint], [TransactionSign], [ShowFinancialData], [DeliveryType], [AdminView], [Invalidated], [DispenserType], [ForcesDelivery], [RetailInvoice], [IncludeInBalance]) VALUES (N'0248ad15-44dc-416a-a059-166f4ba26414', N'Τιμολόγιο Αγορών', N'ΤIM. ΑΓΟΡ.', 0, 1, 0, 270, N'C:\ASFuelControl\Sign', NULL, 0, N'', 0, 0, 1, 1, 1, 1, NULL, 0, 0, 0, 0, 0, 0)
GO
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId], [IsInternal], [InternalDeliveryDescription], [NeedsVehicle], [IsCancelation], [HasFinancialTransactions], [IsLaserPrint], [TransactionSign], [ShowFinancialData], [DeliveryType], [AdminView], [Invalidated], [DispenserType], [ForcesDelivery], [RetailInvoice], [IncludeInBalance]) VALUES (N'10efcdf0-4838-4b88-96fc-2fb8faf28849', N'Τιμολόγιο Πωλήσεων', N'ΤΙΜ', 0, 0, 1, 165, N'C:\ASFuelControl\Sign', NULL, 0, N'', 1, 0, 1, 1, 1, 1, NULL, 0, 0, 0, 0, 0, 0)
GO
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId], [IsInternal], [InternalDeliveryDescription], [NeedsVehicle], [IsCancelation], [HasFinancialTransactions], [IsLaserPrint], [TransactionSign], [ShowFinancialData], [DeliveryType], [AdminView], [Invalidated], [DispenserType], [ForcesDelivery], [RetailInvoice], [IncludeInBalance]) VALUES (N'6e7f8bf5-3952-4390-9651-5a09188cc3e0', N'Τιμολόγιο – Δελτίο Αποστολής Αγορών', N'Τ.Δ.Α.A.', 0, 1, 0, 40, N'C:\ASFuelControl\Sign', NULL, 0, N'Παραλαβή Καυσίμου', 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1)
GO
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId], [IsInternal], [InternalDeliveryDescription], [NeedsVehicle], [IsCancelation], [HasFinancialTransactions], [IsLaserPrint], [TransactionSign], [ShowFinancialData], [DeliveryType], [AdminView], [Invalidated], [DispenserType], [ForcesDelivery], [RetailInvoice], [IncludeInBalance]) VALUES (N'e568c652-2444-4297-9cae-67bb57dcf362', N'Δελτίο Εξαγωγής', N'Δ.ΕΞΑΓ.', 0, 0, 1, 181, N'C:\ASFuelControl\Sign', NULL, 1, N'Εξαγωγή Καυσίμου', 0, 0, 0, 1, 1, 0, 2, 0, 0, 0, 0, 0, 1)
GO
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId], [IsInternal], [InternalDeliveryDescription], [NeedsVehicle], [IsCancelation], [HasFinancialTransactions], [IsLaserPrint], [TransactionSign], [ShowFinancialData], [DeliveryType], [AdminView], [Invalidated], [DispenserType], [ForcesDelivery], [RetailInvoice], [IncludeInBalance]) VALUES (N'23ad17e7-0a59-4d97-8a6a-2dbbcac2db8d', N'Δελτίο Εξαγωγής για Διύληση', N'Δ.ΕΞΑΓ. Διυλ.', 0, 0, 1, 181, N'C:\ASFuelControl\Sign', NULL, 1, N'Εξαγωγή Καυσίμου για Διύληση', 0, 0, 0, 1, 1, 0, 2, 0, 0, 0, 0, 0, 1)
GO
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId], [IsInternal], [InternalDeliveryDescription], [NeedsVehicle], [IsCancelation], [HasFinancialTransactions], [IsLaserPrint], [TransactionSign], [ShowFinancialData], [DeliveryType], [AdminView], [Invalidated], [DispenserType], [ForcesDelivery], [RetailInvoice], [IncludeInBalance]) VALUES (N'e147a216-a3df-4214-902a-a1f9a78b45aa', N'Δελτίο Εξαγωγής για Εξυδάτωση', N'Δ.ΕΞΑΓ. Εξυδ.', 0, 0, 1, 181, N'C:\ASFuelControl\Sign', NULL, 1, N'Εξαγωγή Καυσίμου για Εξυδάτωση', 0, 0, 0, 1, 1, 0, 2, 0, 0, 0, 0, 0, 1)
GO
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId], [IsInternal], [InternalDeliveryDescription], [NeedsVehicle], [IsCancelation], [HasFinancialTransactions], [IsLaserPrint], [TransactionSign], [ShowFinancialData], [DeliveryType], [AdminView], [Invalidated], [DispenserType], [ForcesDelivery], [RetailInvoice], [IncludeInBalance]) VALUES (N'5fd053a5-84b1-4564-b445-6b0b23a359f1', N'Δελτίο Αποστολής Πωλήσεων', N'Δ.Α.', 0, 0, 1, 158, N'C:\ASFuelControl\Sign', NULL, 0, N'', 1, 0, 0, 1, 1, 0, NULL, 0, 0, 1, 0, 0, 1)
GO
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId], [IsInternal], [InternalDeliveryDescription], [NeedsVehicle], [IsCancelation], [HasFinancialTransactions], [IsLaserPrint], [TransactionSign], [ShowFinancialData], [DeliveryType], [AdminView], [Invalidated], [DispenserType], [ForcesDelivery], [RetailInvoice], [IncludeInBalance]) VALUES (N'78271fe2-3b97-4870-9d96-6ecbbd4b9ce0', N'Δελτίο Παράδοσης', N'Δ.ΠΑ.', 0, 0, 1, 160, N'C:\ASFuelControl\Sign', NULL, 0, N'Παράδοση Καυσίμου', 1, 0, 0, 1, 1, 0, NULL, 0, 0, 1, 0, 0, 1)
GO
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId], [IsInternal], [InternalDeliveryDescription], [NeedsVehicle], [IsCancelation], [HasFinancialTransactions], [IsLaserPrint], [TransactionSign], [ShowFinancialData], [DeliveryType], [AdminView], [Invalidated], [DispenserType], [ForcesDelivery], [RetailInvoice], [IncludeInBalance]) VALUES (N'38c58ed3-c69f-4f74-9ef5-77b951e91d9c', N'Απόδειξη Εσόδου', N'ΑΕ', 0, 0, 1, 173, N'C:\ASFuelControl\Sign', NULL, 0, N'', 0, 0, 1, 0, 1, 1, NULL, 1, 0, 1, 0, 1, 1)
GO
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId], [IsInternal], [InternalDeliveryDescription], [NeedsVehicle], [IsCancelation], [HasFinancialTransactions], [IsLaserPrint], [TransactionSign], [ShowFinancialData], [DeliveryType], [AdminView], [Invalidated], [DispenserType], [ForcesDelivery], [RetailInvoice], [IncludeInBalance]) VALUES (N'c754f26a-a35e-46f9-bbbf-82f223431508', N'Δελτίο Αποστολής Αγορών', N'Δ.Α.Α', 0, 1, 0, 40, N'C:\ASFuelControl\Sign', NULL, 0, N'Παραλαβή Καυσίμου', 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1)
GO
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId], [IsInternal], [InternalDeliveryDescription], [NeedsVehicle], [IsCancelation], [HasFinancialTransactions], [IsLaserPrint], [TransactionSign], [ShowFinancialData], [DeliveryType], [AdminView], [Invalidated], [DispenserType], [ForcesDelivery], [RetailInvoice], [IncludeInBalance]) VALUES (N'04c39f93-e648-4be0-8d06-a8e34f48839a', N'Ειδικό Ακυρωτικό Στοιχείο', N'ΑΚ', 0, 0, 1, 215, N'C:\ASFuelControl\Sign', NULL, 0, N'', 0, 1, 1, 0, 1, 1, NULL, 1, 0, 0, 0, 0, 1)
GO
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId], [IsInternal], [InternalDeliveryDescription], [NeedsVehicle], [IsCancelation], [HasFinancialTransactions], [IsLaserPrint], [TransactionSign], [ShowFinancialData], [DeliveryType], [AdminView], [Invalidated], [DispenserType], [ForcesDelivery], [RetailInvoice], [IncludeInBalance]) VALUES (N'a9bf8c4d-b056-4220-b53f-ad2bbfcdee1d', N'Πιστωτικό Δελτίο Λιανικών Συναλλαγών', N'Α.ΕΠΙΣΤΡ.', 0, 0, 1, 175, N'C:\ASFuelControl\Sign', NULL, 0, N'', 0, 0, 1, 1, 1, 1, NULL, 0, 0, 0, 1, 1, 1)
GO
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId], [IsInternal], [InternalDeliveryDescription], [NeedsVehicle], [IsCancelation], [HasFinancialTransactions], [IsLaserPrint], [TransactionSign], [ShowFinancialData], [DeliveryType], [AdminView], [Invalidated], [DispenserType], [ForcesDelivery], [RetailInvoice], [IncludeInBalance]) VALUES (N'f0405639-84ea-414d-b907-b143505ae7c7', N'Δελτίο Επιστροφής', N'Δ.ΕΠ.', 0, 1, 1, 63, N'C:\ASFuelControl\Sign', NULL, 0, N'Επιστροφή Καυσίμου', 0, 0, 0, 1, 1, 0, 1, 0, 0, 0, 0, 0, 1)
GO
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId], [IsInternal], [InternalDeliveryDescription], [NeedsVehicle], [IsCancelation], [HasFinancialTransactions], [IsLaserPrint], [TransactionSign], [ShowFinancialData], [DeliveryType], [AdminView], [Invalidated], [DispenserType], [ForcesDelivery], [RetailInvoice], [IncludeInBalance]) VALUES (N'04c3f1d4-437b-4874-be3c-c1238615a4a6', N' Δελτίο Λιτρομέτρησης', N'Δ.ΛΙΤΡ.', 0, 0, 1, 316, N'C:\ASFuelControl\Sign', NULL, 1, N'Παραλαβή Λιτρομέτρησης', 0, 0, 0, 0, 1, 0, NULL, 0, 0, 0, 0, 0, 1)
GO
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId], [IsInternal], [InternalDeliveryDescription], [NeedsVehicle], [IsCancelation], [HasFinancialTransactions], [IsLaserPrint], [TransactionSign], [ShowFinancialData], [DeliveryType], [AdminView], [Invalidated], [DispenserType], [ForcesDelivery], [RetailInvoice], [IncludeInBalance]) VALUES (N'c7d3d685-e4cf-4a98-a757-d4c12da411d3', N'Τιμολόγιο – Δελτίο Αποστολής Πωλήσεων', N'ΤΔΑΠ', 0, 0, 1, 222, N'C:\ASFuelControl\Sign', NULL, 0, N'', 1, 0, 1, 1, 1, 1, NULL, 0, 0, 1, 0, 0, 1)
GO
INSERT [dbo].[InvoiceType] ([InvoiceTypeId], [Description], [Abbreviation], [LastNumber], [TransactionType], [Printable], [OfficialEnumerator], [Printer], [InvoiceFormId], [IsInternal], [InternalDeliveryDescription], [NeedsVehicle], [IsCancelation], [HasFinancialTransactions], [IsLaserPrint], [TransactionSign], [ShowFinancialData], [DeliveryType], [AdminView], [Invalidated], [DispenserType], [ForcesDelivery], [RetailInvoice], [IncludeInBalance]) VALUES (N'a8184bb0-2368-4c9d-9e45-eeb7b45b2393', N'Πιστωτικό Τιμολόγιο', N'ΠΤ', 0, 0, 1, 168, N'C:\ASFuelControl\Sign', NULL, 0, N'', 1, 0, 1, 1, 1, 1, NULL, 0, 0, 0, 0, 0, 1)
GO

INSERT [dbo].[InvoiceTypeTransform] ([InvoiceTypeTransformId], [ParentInvoiceTypeId], [ChildInvoiceTypeId], [TransformationMode], [NotesAddition]) VALUES (N'4155a224-3b79-4125-aaf4-cbd093e31666', N'394612c6-073c-4820-aa60-828aaecefcb7', N'a9bf8c4d-b056-4220-b53f-ad2bbfcdee1d', 1, N'Επιστροφή του ')
GO
INSERT [dbo].[InvoiceTypeTransform] ([InvoiceTypeTransformId], [ParentInvoiceTypeId], [ChildInvoiceTypeId], [TransformationMode], [NotesAddition]) VALUES (N'1bf5c4ef-9767-4a10-845a-613517a4519b', N'394612c6-073c-4820-aa60-828aaecefcb7', N'04c39f93-e648-4be0-8d06-a8e34f48839a', 0, N'Ακύρωση των')
GO
INSERT [dbo].[InvoiceTypeTransform] ([InvoiceTypeTransformId], [ParentInvoiceTypeId], [ChildInvoiceTypeId], [TransformationMode], [NotesAddition]) VALUES (N'e7548eff-c4b2-4c77-b2bf-3a24291f2b30', N'394612c6-073c-4820-aa60-828aaecefcb7', N'10efcdf0-4838-4b88-96fc-2fb8faf28849', 3, N'Αντικατάσταση των')
GO

INSERT [dbo].[InvoiceTypeTransform] ([InvoiceTypeTransformId], [ParentInvoiceTypeId], [ChildInvoiceTypeId], [TransformationMode], [NotesAddition]) VALUES (N'98c9b124-f9f3-4803-b8d6-09db4d2ba6ad', N'38c58ed3-c69f-4f74-9ef5-77b951e91d9c', N'a9bf8c4d-b056-4220-b53f-ad2bbfcdee1d', 1, N'Επιστροφή του ')
GO
INSERT [dbo].[InvoiceTypeTransform] ([InvoiceTypeTransformId], [ParentInvoiceTypeId], [ChildInvoiceTypeId], [TransformationMode], [NotesAddition]) VALUES (N'8302d51e-5402-4bee-8b20-23b765fa6ee2', N'5fd053a5-84b1-4564-b445-6b0b23a359f1', N'04c39f93-e648-4be0-8d06-a8e34f48839a', 0, N'Ακύρωση του')
GO
INSERT [dbo].[InvoiceTypeTransform] ([InvoiceTypeTransformId], [ParentInvoiceTypeId], [ChildInvoiceTypeId], [TransformationMode], [NotesAddition]) VALUES (N'aebdfff3-ff56-4de1-b8b2-53c42d7af7bd', N'38c58ed3-c69f-4f74-9ef5-77b951e91d9c', N'04c39f93-e648-4be0-8d06-a8e34f48839a', 0, N'Ακύρωση των')
GO
INSERT [dbo].[InvoiceTypeTransform] ([InvoiceTypeTransformId], [ParentInvoiceTypeId], [ChildInvoiceTypeId], [TransformationMode], [NotesAddition]) VALUES (N'0e7f76eb-2d87-4a11-aa9e-60f1a3daa03e', N'10efcdf0-4838-4b88-96fc-2fb8faf28849', N'a8184bb0-2368-4c9d-9e45-eeb7b45b2393', 1, N'Πιστωτικό του')
GO
INSERT [dbo].[InvoiceTypeTransform] ([InvoiceTypeTransformId], [ParentInvoiceTypeId], [ChildInvoiceTypeId], [TransformationMode], [NotesAddition]) VALUES (N'b4463fea-089d-450b-91ed-738d05170803', N'38c58ed3-c69f-4f74-9ef5-77b951e91d9c', N'10efcdf0-4838-4b88-96fc-2fb8faf28849', 3, N'Αντικατάσταση των')
GO
INSERT [dbo].[InvoiceTypeTransform] ([InvoiceTypeTransformId], [ParentInvoiceTypeId], [ChildInvoiceTypeId], [TransformationMode], [NotesAddition]) VALUES (N'137e9ad0-5ee8-435c-967b-c53218fec964', N'5fd053a5-84b1-4564-b445-6b0b23a359f1', N'10efcdf0-4838-4b88-96fc-2fb8faf28849', 3, N'Μετασχηματισμός των')
GO

update [dbo].[trader] set InvoiceTypeId = 'c7d3d685-e4cf-4a98-a757-d4c12da411d3' where InvoiceTypeId is not null
GO

update dbo.FuelType set SupportsSupplyNumber = 1 where FuelTypeId = '827eb887-cf8a-48c5-a37b-af2c36cc1d49'
GO

update dbo.FuelType set SupportsSupplyNumber = 1 where FuelTypeId = '9227828e-1b48-4288-ab1c-f805c4ab873c'
GO
