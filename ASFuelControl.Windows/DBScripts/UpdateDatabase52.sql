CREATE TABLE [dbo].[MyDataInvoiceHistory](
	[MyDataInvoiceId] [uniqueidentifier] NOT NULL,
	[Uid] [nvarchar](100) NULL,
	[Mark] [bigint] NULL,
	[CanceledByMark] [bigint] NULL,
	[CancelationMark] [bigint] NULL,
	[InvoiceId] [uniqueidentifier] NOT NULL,
	[DateTimeSent] [datetime] NULL,
	[Status] [int] NOT NULL,
	[Data] [ntext] NULL,
	[Errors] [ntext] NULL,
	CONSTRAINT [PK_MyDataInvoiceHistory] PRIMARY KEY CLUSTERED ([MyDataInvoiceId] ASC)
)
GO
