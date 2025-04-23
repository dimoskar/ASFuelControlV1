CREATE TABLE [dbo].[ChangeLog](
	[ChangeLogId] [uniqueidentifier] NOT NULL,
	[DateTimeStamp] [datetime] NOT NULL,
	[TableName] [nvarchar](100) NULL,
	[ColumnName] [nvarchar](100) NULL,
	[AdditionalDescription] [nvarchar](100) NULL,
	[OldValue] [ntext] NULL,
	[NewValue] [ntext] NULL,
	[ApplicationUserName] [nvarchar](100) NULL,
	[PrimaryKey] [nvarchar](50) NULL,
    CONSTRAINT [PK_ChangeLog] PRIMARY KEY CLUSTERED 
    (
	   [ChangeLogId] ASC
    )
 )
GO

ALTER TABLE dbo.SalesTransaction ADD
	InvalidSale bit NULL
GO

ALTER TABLE dbo.TankCheck ADD
	Temperature decimal(18,2) NULL
GO

