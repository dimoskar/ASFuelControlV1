CREATE TABLE [dbo].[MyDataInvoice]
(
	[MyDataInvoiceId] [uniqueidentifier] NOT NULL,
	[Uid] nvarchar(100) NULL,
	[Mark] bigint NULL,
	[CanceledByMark] bigint NULL,
	[CancelationMark] bigint NULL,
	[InvoiceId] [uniqueidentifier] Not NULL,
	[DateTimeSent] DateTime NULL,
	[Status] int not null,
	[Data] ntext null,
	CONSTRAINT [PK_MyDataInvoice] PRIMARY KEY CLUSTERED 
	(
		[MyDataInvoiceId] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE dbo.InvoiceType ADD
	SendToMyData bit NULL
GO

if COLUMNPROPERTY( OBJECT_ID('dbo.InvoiceType'),'SendToMyData','AllowsNull') = 1
	update dbo.InvoiceType set SendToMyData = 0
GO

if COLUMNPROPERTY( OBJECT_ID('dbo.InvoiceType'),'SendToMyData','AllowsNull') = 1
	update dbo.InvoiceType set SendToMyData = 1 Where OfficialEnumerator = 173 or OfficialEnumerator = 215
GO

if COLUMNPROPERTY( OBJECT_ID('dbo.InvoiceType'),'SendToMyData','AllowsNull') = 1
	ALTER TABLE InvoiceType
		ALTER COLUMN SendToMyData bit NOT NULL
GO

ALTER TABLE dbo.Trader ADD
	Country nvarchar(50) NULL,
	ZipCode nvarchar(50) NULL
GO

if COLUMNPROPERTY( OBJECT_ID('dbo.Trader'),'Country','AllowsNull') = 1
	update dbo.Trader set Country = 'GR'
GO 

if COLUMNPROPERTY( OBJECT_ID('dbo.Trader'),'Country','AllowsNull') = 1
	ALTER TABLE .Trader
		ALTER COLUMN Country nvarchar(50) NOT NULL
GO

CREATE TABLE [dbo].[Country](
	[CountryId] [uniqueidentifier] NOT NULL,
	[CountryCode] [nvarchar](50) NOT NULL,
	[DisplayMember] [nvarchar](250) NOT NULL,
	[IsEu] [bit] NOT NULL,
    CONSTRAINT [PK_Country] PRIMARY KEY CLUSTERED 
    (
		[CountryId] ASC
	) ON [PRIMARY]
) ON [PRIMARY]
GO

INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'ebd0a4ec-fd10-48b9-b989-001874a605f7', N'GP', N'Γουαδελούπη', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'7208fcbd-b17a-4676-ac1d-008b11487137', N'SH', N'Νήσος Αγίας Ελένης', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'7293b8cc-08a7-44e3-8823-0372edeea9ac', N'MU', N'Μαυρίκιος', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'97cdd362-8bc0-43e9-ab26-04d77524207c', N'SC', N'Σεϋχέλλες', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e59b05be-3651-4d58-b7a3-05bd890bfb3d', N'KZ', N'Καζακστάν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e014b5b3-3504-4cc3-b026-068e66c8d0c1', N'NC', N'Νέα Καληδονία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e5fb1c9e-8828-4976-aee5-06b4265dd2ea', N'TZ', N'Τανζανία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'b7eec8c3-6fa6-4139-9167-081bbfdf0a78', N'NA', N'Ναμίμπια', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'90de63fb-2b4a-4b56-a6f9-09c679324a30', N'ZW', N'Ζιμπάμπουε', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'ba8fe2ff-d286-46a0-81e9-09e15e3a5269', N'DZ', N'Αλγερία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'd40179ef-273c-4300-96e9-09ff5e6465ce', N'IE', N'Ιρλανδία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'dace58d4-7b33-4621-b356-0a6f56189ddf', N'EC', N'Ισημερινός', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'4fcdfc2c-37ca-4f08-bd40-0c6d39172a3a', N'NR', N'Ναουρού', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'14f4bb0c-7b5a-4acb-849f-0ce6c8e3c6be', N'AR', N'Αργεντινή', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'322fb5d0-e34f-41a4-979b-0d4d468e968d', N'GA', N'Γκαμπόν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'c5e629a3-b8d7-408a-9792-0df72a78ad18', N'ML', N'Μάλι', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'2ee7e027-c276-4ff9-95e1-0e1cb8dbced4', N'CI', N'Ακτή Ελεφαντοστού', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e8bd141d-3af0-4206-9476-0f422555eb6c', N'NL', N'Ολλανδία (Κάτω Χώρες)', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'7cd4942a-b621-4e7b-a306-10872f409e32', N'GR', N'Ελλάδα', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'c9991248-96ee-49df-90f6-10c1c1aa94e8', N'JO', N'Ιορδανία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'3a1e0724-ec20-4590-8027-125accd59510', N'JP', N'Ιαπωνία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'c8c955dd-6958-4681-b896-1269b7e600df', N'ST', N'Σάο Τομέ και Πρίνσιπε', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'795ead10-16bf-4ab7-8595-12e641180f2d', N'AZ', N'Αζερμπαϊτζάν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'd3ba01bd-31e5-4df8-b3a5-13178a7ea2bd', N'GB', N'Ηνωμένο Βασίλειο', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'560546ca-f923-4648-aff0-14d7db3380bf', N'GW', N'Γουινέα-Μπισσάου', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'0101c6d0-f6f0-44dc-8c26-15cf0740b7d8', N'AE', N'Ηνωμένα Αραβικά Εμιράτα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'6359d550-6fe5-4bec-865b-16e9296a220f', N'CZ', N'Τσεχία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'83a7d96d-94dd-4fbb-a963-174790f5188a', N'DE', N'Γερμανία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'90410019-bf85-400e-b8cc-18f41d8ad03f', N'IL', N'Ισραήλ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'd17fd7ec-8346-4f54-86b4-194b602c977d', N'UY', N'Ουρουγουάη', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'add588b1-0a51-47aa-9241-1a07cacb1636', N'CK', N'Νήσοι Κουκ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'7e1653ed-d04b-4d23-812f-1a4199de90ac', N'MA', N'Μαρόκο', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'0e31a6c9-2b40-43ea-8ad3-1acf153a523f', N'AW', N'Αρούμπα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'beeaf46b-6bc8-41e9-af7c-1ad640256ccc', N'MZ', N'Μοζαμβίκη', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'084204f0-392b-40f8-b875-1bba412b3dac', N'GG', N'Γκέρνσεϊ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'0c865b5d-721c-4c52-bd0d-20a1d9bb4c19', N'CM', N'Καμερούν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'8a47253f-77f7-49a1-8de6-23c63e0e2854', N'BL', N'Άγιος Βαρθολομαίος', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'65056a10-d74b-43b9-8f7d-250de8482b4a', N'TC', N'Τερκς και Κέικος', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'9ba938ee-383c-4961-9d1f-261cc8e306ae', N'IR', N'Ιράν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'85a7dca6-b12d-4ede-bbca-26bcac0cae84', N'HM', N'Νήσοι Χερντ και Μακντόναλντ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'194bb28f-ff1a-4eda-abb0-26f5b2d60ce2', N'BG', N'Βουλγαρία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'179e6740-e6ea-4836-a16c-27bcc261b6d5', N'IN', N'Ινδία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'3dae64b7-e5b7-4a84-b53a-292d5e69a543', N'ET', N'Αιθιοπία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'ce646881-7c8c-4161-9cb1-293ea103b2c2', N'BR', N'Βραζιλία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'8a84e935-cc2e-42de-a130-29d2b6852dc1', N'EE', N'Εσθονία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'14a56177-00ae-4def-810c-2b8e1be81848', N'BS', N'Μπαχάμες', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'57360f54-1704-4aea-8b61-2bb9193d9c71', N'VN', N'Βιετνάμ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e92e9ce7-a5ad-4004-81a6-2cb3bb73c756', N'AT', N'Αυστρία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'346e6371-ee11-447b-880a-2d1f3d09e764', N'LY', N'Λιβύη', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'0384c7b3-ee60-44ae-844e-2e91e60bad68', N'SZ', N'Εσουατίνι', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'3f632c18-a7bc-47bc-af57-2eb484f559bc', N'UM', N'Απομακρυσμένες Νησίδες των Ηνωμένων Πολιτειών', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'0b958488-ed56-48c8-8236-2f3ffb04eb88', N'VC', N'Άγιος Βικέντιος και Γρεναδίνες', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'adecdd2f-71b6-4111-9f4c-2fe69ecd22e3', N'AX', N'Ώλαντ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'19a8326f-9a89-4d46-8e1c-301ce81c11c1', N'SX', N'Άγιος Μαρτίνος (Ολλανδία)', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'1dca5010-4d90-4aef-a12d-3270082b7629', N'GN', N'Γουινέα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'76c30612-4664-4516-a473-32a40405b90d', N'IO', N'Βρετανικό Έδαφος Ινδικού Ωκεανού', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e4bfa3d0-ab1c-4534-bb64-357d4dfb0cb5', N'KH', N'Καμπότζη', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'24ffcd3b-e729-402b-b081-35ab714fa2de', N'PW', N'Παλάου', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'dc9caa7e-a7b0-4c3b-a359-35d325f29b46', N'MC', N'Μονακό', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e8df3efe-a79e-45db-a34a-35daaf375dba', N'AN', N'Ολλανδικές Αντίλλες', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'01f2b569-aaaf-47cc-bd29-381e20787203', N'DK', N'Δανία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'6f9261a1-f544-4a3f-86e7-384a48b32bcb', N'SV', N'Ελ Σαλβαδόρ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'67275f70-8449-458b-8136-39394d0e6510', N'SY', N'Συρία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'afd46da3-1475-4874-943f-39a9128e19e8', N'AD', N'Ανδόρρα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'71c52c7f-220b-4527-963f-3aa4bea0151a', N'SE', N'Σουηδία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'5418ca63-866b-41c5-87b7-3be7d8028e20', N'CW', N'Κουρασάο', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'a5b279a4-dcfe-4fe8-a8b1-3df3e4d1fadc', N'LK', N'Σρι Λάνκα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'54f120f6-680c-4746-bd37-3f636a2874bc', N'BQ', N'Μποναίρ, Άγιος Ευστάθιος και Σάμπα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'3718bb5a-029e-4ab5-9b18-414c0ae33a0b', N'EH', N'Δυτική Σαχάρα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'f2bf0384-26f8-495d-b0de-41b9a4c60f51', N'MN', N'Μογγολία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'7adffa34-545a-46a0-a354-4231ddc7e710', N'GD', N'Γρενάδα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'1ac5f2bf-0aec-485d-9d36-43c8a47d1aec', N'GE', N'Γεωργία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'89e3dbe1-5d1e-405f-9e6a-468a1681e7e8', N'BF', N'Μπουρκίνα Φάσο', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'2613da8b-404d-4af5-8486-4732bd747366', N'BH', N'Μπαχρέιν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'fa01bdc4-6426-46fb-84a6-47f3d3952344', N'VE', N'Βενεζουέλα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'0af0713f-f240-48a6-849b-48159930148d', N'RS', N'Σερβία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'8226765d-3237-4a75-b197-493792a702ce', N'TK', N'Τοκελάου', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'fe7a82ff-69cb-44fa-8652-49855f4af43b', N'KM', N'Κομόρες', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e6ae7dc4-2c41-41fd-ba53-4a2f59af45d6', N'MD', N'Μολδαβία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'302372ba-e881-44a4-a1b4-4b191b0e6d9b', N'DJ', N'Τζιμπουτί', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'be1491f0-ddf9-48e3-845f-4b534a0c4f3e', N'KP', N'Βόρεια Κορέα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'96dd67d6-4ec8-47f6-a43a-4b9e7fdaebbe', N'KR', N'Νότια Κορέα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'd65709b3-d019-45fa-b0fd-4ba1e95a094b', N'CF', N'Κεντροαφρικανική Δημοκρατία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'fc0c318e-9064-460c-9357-4bddd68c2c1a', N'TJ', N'Τατζικιστάν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'8f69208a-c6eb-4572-a76d-4c4663b4981c', N'TW', N'Ταϊβάν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'd35811e6-8c6e-4b62-90a8-4da4e5121e11', N'NE', N'Νίγηρας', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'f366fec1-3a4f-418d-9054-4df7afed042d', N'CC', N'Νησιά Κόκος (Keeling)', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'b3eb5867-6b6c-4834-ba61-4e9995c463bc', N'SJ', N'Σβάλμπαρντ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'82458e92-aa32-45ba-b72d-505b582ae28a', N'BO', N'Βολιβία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'6ec9a91f-9c46-486e-a764-526b34d3e9a0', N'GH', N'Γκάνα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'1abdd30e-e09c-4f11-8798-5374d485a5c9', N'RU', N'Ρωσία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'95bb9ab6-2ab0-4122-aaac-53b853009b85', N'BI', N'Μπουρούντι', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'621b3358-9e93-448f-a858-558c71a7522b', N'MX', N'Μεξικό', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'38bf73bf-1025-49d4-823c-576508446722', N'MW', N'Μαλάουι', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'66715cb0-cc44-4745-beeb-581b6bb8aa5b', N'CH', N'Ελβετία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'2eb9923f-d229-4504-be2b-596da43d5efb', N'BZ', N'Μπελίζ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'39a1962e-3655-44c1-b6e3-598a5ab17ca1', N'MV', N'Μαλδίβες', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'49b8de61-e874-45a8-ab6f-59cee16b338f', N'AO', N'Ανγκόλα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'3d5d5aaa-39bf-4f53-a708-5a95fea36165', N'ZA', N'Νότια Αφρική', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'3dd102fc-cc99-4534-b0a3-5ac0be5978a6', N'FR', N'Γαλλία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'4e05dfd8-c0f4-462d-833d-5b106dcf7f74', N'MO', N'Μακάο', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'd1795d9f-4e0a-44d8-b39d-5c5613267186', N'TH', N'Ταϊλάνδη', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'aed0df3f-1a55-4a88-907f-5cc43255c761', N'LV', N'Λεττονία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'a5053026-0f8d-4870-ae74-5fc53ae27bd2', N'FJ', N'Φίτζι', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'1b67c55e-37ca-416e-9246-60ae78223ab4', N'GQ', N'Ισημερινή Γουινέα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'74af795a-c8bf-4361-bf29-620d6f86fea6', N'BE', N'Βέλγιο', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'9b16eb2a-bb54-44f9-860b-6253c24da812', N'GS', N'Νήσοι Νότια Γεωργία και Νότιες Σάντουιτς', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'5561a832-6759-4958-bae8-626c42703199', N'WF', N'Ουαλίς και Φουτουνά', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'dfd391d7-4b97-4ee6-88dc-6328768f7fdd', N'KN', N'Άγιος Χριστόφορος και Νέβις', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'1eb79011-cde2-4015-8b9c-67207cfa47a7', N'SK', N'Σλοβακία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'567fc50d-8c22-4118-a0db-68a1295aea3a', N'CY', N'Κύπρος', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'ac2659b2-16bf-4bac-96f0-6b75562194ce', N'GT', N'Γουατεμάλα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'1cbfa613-f506-4d99-a713-6be95c7135a2', N'GM', N'Γκάμπια', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'518930db-e24a-48ff-ab7a-6c64deef6488', N'MG', N'Μαδαγασκάρη', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'8c2ba26d-c0af-4776-91cd-6e422ad1f835', N'TF', N'Γαλλικά Νότια και Ανταρκτικά Εδάφη', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'7b553634-3345-4eac-b6b8-7118b9fe702c', N'TT', N'Τρινιντάντ και Τομπάγκο', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'2800d1b1-9dd2-4383-891c-71300baf1c13', N'SR', N'Σουρινάμ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'315f1956-2c6a-47d2-8caf-726a40cd7e9a', N'MS', N'Μοντσερράτ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'b988f99f-731a-4876-b433-730897db5af2', N'SA', N'Σαουδική Αραβία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'f44c4ee3-c555-44bd-9798-754db5838cd4', N'YT', N'Μαγιότ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'98dca30a-4dc1-49dc-9c58-76cca8450de6', N'VG', N'Βρετανικές Παρθένοι Νήσοι', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'bbadbd05-ce8e-4792-9276-7a166d694b7c', N'HU', N'Ουγγαρία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'0bf429fd-0307-45c6-9570-7de1788df918', N'CN', N'Κίνα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'b286d975-4ea0-478a-8881-809d0364823d', N'BD', N'Μπανγκλαντές', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'cfc50318-a0a7-45a8-b1ca-80ee43b3cfb7', N'HK', N'Χονγκ Κονγκ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'6a5bdae4-efdf-46b7-92ae-811ceb7012af', N'CX', N'Νήσος των Χριστουγέννων', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'f78774d1-5a9c-4d1a-b6a7-8132f96d7773', N'CL', N'Χιλή', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'8c43884d-5bf4-4279-b7bc-82d0aed28795', N'TO', N'Τόγκα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'7c6c6d91-db34-4315-8d1d-834ff4183de4', N'LB', N'Λίβανος', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'91ffa043-94cf-4592-b8fb-8354c3155c14', N'SL', N'Σιέρα Λεόνε', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'047c1973-2ef5-447b-aa27-837299b99f0e', N'PE', N'Περού', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'54d335c5-cb72-4f7d-8839-83a5817be3df', N'SS', N'Νότιο Σουδάν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'978589e9-74a5-464f-9e13-84440ac07d39', N'VU', N'Βανουάτου', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e6b1be97-798f-4ee1-9c1e-885bd31ed394', N'AS', N'Αμερικανική Σαμόα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'cb3d265d-44ec-4e09-bfe7-8902f9ed87ce', N'MR', N'Μαυριτανία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'ba18c335-11b7-4a17-8562-8b037bc46167', N'RW', N'Ρουάντα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'7c80f66c-8913-450a-a819-8c06fefbe990', N'IQ', N'Ιράκ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e97b4197-e320-49e9-81dd-8e7a97926efb', N'SD', N'Σουδάν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'9cff2039-1534-4c55-aebc-8fd6ff58a240', N'TR', N'Τουρκία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'0b4270f8-6cb5-409b-87e9-9278ff3859c4', N'JE', N'Τζέρσεϊ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e644ebb1-16b5-4614-b49e-92ee13a42fb4', N'BW', N'Μποτσουάνα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'1b9521fe-99ac-4633-9646-95d51b1134b6', N'JM', N'Τζαμάικα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'17a5fc2f-4330-43ff-8ad4-97ab41886124', N'MK', N'Βόρεια Μακεδονία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'9c7b47e2-fc33-4ff1-9259-98156e5556a2', N'LS', N'Λεσότο', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'0dbb12f6-d2e6-4dee-9452-98b7818f0fb9', N'FO', N'Νήσοι Φερόες', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'5942fb2d-27e8-4c83-a100-98d7899228e1', N'MT', N'Μάλτα', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'f34c4494-275b-4051-8b88-98fa73e4e055', N'PG', N'Παπούα Νέα Γουινέα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'704519da-53c7-4d00-81ff-99aed1fd198e', N'IT', N'Ιταλία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'8ca3169b-b801-41c4-b205-9a319b849631', N'MH', N'Νήσοι Μάρσαλ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'86f27e2c-e88b-41e4-b89c-9bf216628161', N'FI', N'Φινλανδία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'bda483b2-332e-4b0a-bdbb-9c01eed87330', N'ZM', N'Ζάμπια', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'511e8cf0-7038-4ace-a2f7-9d08d35d6a67', N'YE', N'Υεμένη', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'04772ca7-db13-4ac7-b55c-9e272fd00d07', N'LU', N'Λουξεμβούργο', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'f2904482-dbae-4e2c-b343-a124ad5f835c', N'PT', N'Πορτογαλία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'6d48b31b-d1d0-4775-8b5d-a3108e172a44', N'SB', N'Νήσοι Σολομώντα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'b74befc6-cb81-412a-bb14-a41241619b0d', N'US', N'Ηνωμένες Πολιτείες Αμερικής', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'6b035fc5-bf6d-4c73-9d52-a4b239e52322', N'KW', N'Κουβέιτ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'098be65c-0739-44a9-a1f3-a6a0d1b5650c', N'BY', N'Λευκορωσία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'93601dc2-1a1f-4e5d-a408-a7a3c4a147a4', N'AI', N'Ανγκουίλα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'354d8867-de95-4ea6-9eee-a89d6ace8436', N'DM', N'Δομινίκα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'3fcef472-d993-411f-bd2a-aaff0352be4a', N'PA', N'Παναμάς', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'2440c257-5c33-4487-ad5c-ab73727ef22f', N'KI', N'Κιριμπάτι', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'9f63f8f0-34db-464a-a469-ac368053e40e', N'KY', N'Κέιμαν Νήσοι', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'22482525-c369-4dfd-a542-ac41840c9257', N'IM', N'Νήσος Μαν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'2a424ed7-3ced-4946-b447-b080b56a40b7', N'MM', N'Μιανμάρ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'0bd04239-3dd9-4354-b287-b10dfe87bb55', N'LC', N'Αγία Λουκία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'81a7d5ba-9f64-4ca0-a642-b16aaa548980', N'UG', N'Ουγκάντα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e80d65fb-3f0b-4ee2-9d7c-b18cf3442bd9', N'EG', N'Αίγυπτος', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'35eeaeaf-54a0-4d2c-942a-b2fded47bdba', N'GL', N'Γροιλανδία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'f297f887-48c2-4134-9016-b3ad58bdf7db', N'TM', N'Τουρκμενιστάν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'2667bea2-33b7-4e27-bdf7-b6da797ff5bb', N'GF', N'Γαλλική Γουιάνα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'37a2f29e-b7bc-4345-a1a8-b7c797584244', N'RO', N'Ρουμανία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e25b0da2-78b5-401d-a713-b9d161dd6009', N'WS', N'Σαμόα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'35889f0b-d5ae-43c1-b2f7-bab77a017166', N'PH', N'Φιλιππίνες', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'2c5ead9e-dfa5-4221-a2a8-baf35d7e3f52', N'SO', N'Σομαλία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'9d8858c2-f8a6-4b6c-841d-bc3d951849b5', N'ME', N'Μαυροβούνιο', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'07d17d70-757e-4496-b5c3-bc69475a9b20', N'SG', N'Σιγκαπούρη', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'5416e536-6fa7-4937-bae7-bd1c9ab024d8', N'MP', N'Βόρειες Μαριάνες Νήσοι', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'4020acc5-80d1-497e-8eaf-bdc7873ff438', N'CD', N'Λαϊκή Δημοκρατία του Κογκό', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'6f22e4e7-7b87-4087-a15b-be02f6ea5556', N'KE', N'Κένυα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'5251b3e3-f395-462d-9b3b-bf020d37aa4f', N'SM', N'Άγιος Μαρίνος', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'6895f6db-1eea-4c42-8603-bf1d80efecd0', N'AG', N'Αντίγκουα και Μπαρμπούντα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'4a11666a-812b-4e78-ba7e-c0024429f629', N'AL', N'Αλβανία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'09e0a870-f5e5-4e82-891f-c0f157d37ad4', N'CV', N'Πράσινο Ακρωτήρι', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'120411e1-059c-4f31-b758-c1d0d024fa0b', N'NZ', N'Νέα Ζηλανδία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'9fbdb677-eb93-4ee7-8ef2-c2f95767a73a', N'PY', N'Παραγουάη', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'12743761-1cd0-4dcf-a8e8-c59a9c61cb97', N'CO', N'Κολομβία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'43bc2594-9a2b-46ee-ae40-c5c9de1294e1', N'BM', N'Βερμούδες', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'a83291f3-1582-4dc0-a157-c69ce90c875c', N'BA', N'Βοσνία-Ερζεγοβίνη', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'1fd2cd6d-9f31-4d4d-b973-c6db3e2a3b84', N'ES', N'Ισπανία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'b6100238-b104-4bd9-b5ad-c71833653cfa', N'AQ', N'Ανταρκτική', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'bd81fa9e-5b0b-449a-9059-c80564203601', N'GI', N'Γιβραλτάρ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'3ab6b026-1521-443d-8834-ca2a2217d0ff', N'NU', N'Νιούε', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'65294d28-ad80-44a3-b2d5-cb2dfb2d62d8', N'PK', N'Πακιστάν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'13dab397-8a94-4fa0-99ff-ccdb0e71c5de', N'ER', N'Ερυθραία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'4d9b657f-7edd-4689-965f-cde9262a748e', N'MY', N'Μαλαισία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'ad0f2936-80bb-4838-affd-ceeac6f467fa', N'GY', N'Γουιάνα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'c4135511-232f-4a6a-b3c4-cf6331f45216', N'NG', N'Νιγηρία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'0440b170-bc85-4270-8720-d0753f0f079e', N'PM', N'Σαιν Πιερ και Μικελόν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'8eb4dbde-8fb7-4692-9304-d0bfc58714a2', N'PS', N'Δυτική Όχθη', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'c1349dde-6a96-46b3-85ad-d0c6dd5b14d2', N'GU', N'Γκουάμ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'f8039f5d-287c-4144-bd28-d17c5732b123', N'CR', N'Κόστα Ρίκα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'd1e5530e-159b-45c6-a12f-d46209a9c3f0', N'HR', N'Κροατία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'5b27ccf2-9da6-461a-a9bb-d4bbf3595ba9', N'DO', N'Δομινικανή Δημοκρατία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e5e6fc6d-3633-44b1-80b3-d91e6703c5fe', N'BV', N'Μπουβέ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'5180ecce-112a-48e0-b98c-da411d277d1c', N'AU', N'Αυστραλία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e1be53cd-9ef1-4fff-aafd-dc3004be976b', N'MQ', N'Μαρτινίκα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'f2192f04-2176-41d9-a5b0-dd33e74bf9da', N'BN', N'Μπρουνέι', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'cfe72e68-e9dd-4cdf-aacc-dd7d3913039b', N'UZ', N'Ουζμπεκιστάν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'1e201fa5-31cb-4e42-8ef8-ddae27e14f6e', N'NO', N'Νορβηγία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'7aa92dd6-86d8-4293-a23e-ddd99aa919be', N'SN', N'Σενεγάλη', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'096cb1d5-dab9-43a6-aebe-de77e1d84597', N'NI', N'Νικαράγουα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'53c4a05b-dc3f-423f-973e-defcb7949168', N'BT', N'Μπουτάν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'7bee543f-3ba4-446b-9993-df55b52202b9', N'BB', N'Μπαρμπάντος', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'5e0bac31-f8b8-45e1-b582-df6a2c05c8cc', N'MF', N'Άγιος Μαρτίνος (Γαλλία)', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'94e8359b-9db5-49eb-b834-e0b0a99ce831', N'LT', N'Λιθουανία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'bf5ec02e-4cdb-42fb-bfe0-e25310eb74a3', N'FK', N'Νήσοι Φώκλαντ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'73a56cfb-cfcb-4792-ab16-e3625462af5f', N'SI', N'Σλοβενία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'2655c076-912e-4850-8b24-e36871095300', N'CG', N'Κογκό', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'fa968c02-1e1b-4aa9-a8f6-e4fb816e89cf', N'PN', N'Νησιά Πίτκερν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e174af72-f2d9-4426-bb3d-e56bdbc8857b', N'PR', N'Πουέρτο Ρίκο', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'af764d07-afc7-4b09-9c3c-e7161b02a6d3', N'TV', N'Τουβαλού', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'b9df7545-86d8-43fa-b4bc-e77dca9af4e1', N'TG', N'Τόγκο', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'eb9e7197-7ade-4eb1-bbb6-e8226d3be730', N'RE', N'Ρεϊνιόν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'42203181-db51-4381-a9ca-e83aaec441fc', N'TL', N'Ανατολικό Τιμόρ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'3bf5efa7-8836-4042-8fd3-e84bbe1aa2db', N'QA', N'Κατάρ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'78b0e6d6-7a97-4e57-88a1-ea1d06dc14b0', N'PL', N'Πολωνία', 1)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'30f1360a-b881-4518-814b-ebed727f4663', N'NP', N'Νεπάλ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'6bfc7ead-58a5-4a9a-845e-ec59398b3ad0', N'AM', N'Αρμενία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'd962fe16-c441-4437-8123-eec82c3a57f6', N'BJ', N'Μπενίν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e88960c3-1f15-4ff0-84cf-efa5c7cf57c9', N'LI', N'Λίχτενσταϊν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'6d58e66f-d3af-4b24-8134-eff1e30a1273', N'TN', N'Τυνησία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'014791af-ce19-4770-b593-f00f58e5def2', N'LR', N'Λιβερία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'5fbaebf2-35da-4031-879e-f107326ad057', N'IS', N'Ισλανδία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'a176597d-9043-47cd-83db-f11e6bb142d6', N'UA', N'Ουκρανία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'9e5a0b17-c5bb-4411-967a-f329c3de028b', N'CA', N'Καναδάς', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'03ec8859-e05b-4c2c-a465-f35430373a06', N'HN', N'Ονδούρα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'084de454-7eb5-4be9-989c-f575dd92ff40', N'HT', N'Αϊτή', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'eb223ccf-a961-4ac1-a031-f57da09f4251', N'VI', N'Αμερικανικές Παρθένοι Νήσοι', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'bc098571-a303-4d1f-a365-f5c39b21af71', N'ID', N'Ινδονησία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'c18f8e19-a071-45f2-b7a5-f5f681184b68', N'CU', N'Κούβα', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'090e8698-95d2-4e20-ba54-f833faa40504', N'KG', N'Κιργιζία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'8ed46a96-b30a-4265-9aeb-f9a03e10453a', N'PF', N'Γαλλική Πολυνησία', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'b91c3427-4cb8-40a6-a9ed-f9c2aca37ea1', N'TD', N'Τσαντ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'256b5860-1e2a-460f-bc7b-fb3f4a22629c', N'NF', N'Νησί Νόρφολκ', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'268bd1b5-9f8c-4e2e-940e-fbb9b28a0f7c', N'FM', N'Ομόσπονδες Πολιτείες της Μικρονησίας', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'9245a622-a5b9-4cb7-9ab8-fcd9d0deccca', N'OM', N'Ομάν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'e8b50cb2-85eb-48ae-8d8c-fd7787136bac', N'AF', N'Αφγανιστάν', 0)
GO
INSERT [dbo].[Country] ([CountryId], [CountryCode], [DisplayMember], [IsEu]) VALUES (N'509840aa-2114-4458-a7d2-fdc380880a7e', N'VA', N'Βατικανό', 0)
GO
