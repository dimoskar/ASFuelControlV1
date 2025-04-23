CREATE TABLE [dbo].[OilCompany](
	[OilCompanyId] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](150) NOT NULL,
	[Logo] [varbinary](max) NOT NULL,
	CONSTRAINT [PK_OilCompany] PRIMARY KEY CLUSTERED 
	(
		[OilCompanyId] ASC
	)
)
GO

