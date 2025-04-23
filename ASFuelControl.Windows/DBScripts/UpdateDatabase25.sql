ALTER TABLE dbo.SendLog ADD
	SentStatus int NULL,
	LastSent datetime NULL,
	EntityIdentity nvarchar(100) NULL
GO
