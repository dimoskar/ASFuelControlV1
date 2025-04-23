-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
ALTER PROCEDURE [dbo].[InsertTable]
	-- Add the parameters for the stored procedure here
	@tabName nvarchar(1000)
AS
BEGIN
	declare @query nvarchar(1000)
	declare @delQuery nvarchar(1000)
	set @query = 'INSERT INTO [ASFuelControlDB_new].' + @tabName + ' SELECT * FROM [ASFuelControlDB].' + @tabName;
	set @delQuery = 'DELETE ' + @tabName;
	print @query;
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	
	begin try
		EXECUTE (@delQuery);
		EXECUTE (@query);
	end try
	begin catch
		print 'ERROR on ' + @tabName
	end catch
	
END