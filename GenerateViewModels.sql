DECLARE @tableName NVARCHAR(MAX), @schemaName NVARCHAR(MAX), @className NVARCHAR(MAX), @namespace NVARCHAR(MAX)
DECLARE @baseClassNameSpace nvarchar(MAX), @catalog nvarchar(MAX), @databaseName nvarchar(MAX)
DECLARE @isprimarykey int
DECLARE @pkName nvarchar(MAX)

SET @namespace = 'ASFuelControl.Windows.ViewModels'
SET @baseClassNameSpace = 'Data'
Set @catalog = 'ASFuelControlDB_MiniPratirio'
Set @databaseName = 'Data.DatabaseModel'

DECLARE table_cursor CURSOR FOR 
SELECT TABLE_NAME
FROM [INFORMATION_SCHEMA].[TABLES] 
ORDER BY TABLE_NAME

OPEN table_cursor

print 'using System;'
print 'using System.Collections.ObjectModel;'
print 'using System.Linq;'
print ''
print 'namespace ' + @namespace
print '{'

FETCH NEXT FROM table_cursor 
INTO @tableName

WHILE @@FETCH_STATUS = 0
BEGIN
 
	--------------- Input arguments ---------------
	--SET @tableName = 'InvoiceType'
	SET @schemaName = 'dbo'
	SET @className = Replace(@tableName, 'CG', '')
	
	--------------- Input arguments end -----------

	DECLARE tableColumns CURSOR LOCAL FOR
	SELECT cols.name, cols.system_type_id, cols.is_nullable, 
(
	SELECT count(*)
	FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS tc
		INNER JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE AS ccu
			ON tc.CONSTRAINT_NAME = ccu.CONSTRAINT_NAME
	WHERE tc.TABLE_CATALOG = @catalog    -- replace with your catalog
		AND tc.TABLE_SCHEMA = 'dbo'            -- replace with your schema
		AND tc.TABLE_NAME = tbl.name       -- replace with your table name
		AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY' AND ccu.COLUMN_NAME = cols.name)
FROM sys.columns cols
	JOIN sys.tables tbl ON cols.object_id = tbl.object_id
	WHERE tbl.name = @tableName

	PRINT '    public partial class ' + @className + 'ViewModel : BaseViewModel<' + @baseClassNameSpace + '.' + @tableName + '>'
	PRINT '    {'
	
	OPEN tableColumns
	DECLARE @name NVARCHAR(MAX), @typeId INT, @isNullable BIT, @typeName NVARCHAR(MAX)
	FETCH NEXT FROM tableColumns INTO @name, @typeId, @isNullable, @isprimarykey
	set @pkName = ''
	WHILE @@FETCH_STATUS = 0
	BEGIN
		SET @typeName =
		CASE @typeId
			WHEN 36 THEN 'Guid'
			WHEN 40 THEN 'DateTime'
			WHEN 41 THEN 'DateTime'
			WHEN 52 THEN 'Int16'
			WHEN 56 THEN 'int'
			WHEN 61 THEN 'DateTime'
			WHEN 99 THEN 'string'
			WHEN 104 THEN 'bool'
			WHEN 106 THEN 'decimal'
			WHEN 127 THEN 'Int64'
			WHEN 165 THEN 'byte[]'
			WHEN 167 THEN 'string'
			WHEN 189 THEN 'long'
			WHEN 231 THEN 'string'
			WHEN 239 THEN 'string'
			WHEN 241 THEN 'XElement'
			ELSE 'TODO(' + CAST(@typeId AS NVARCHAR) + ')'
		END;
		IF @isNullable = 1 AND @typeId != 231 AND @typeId != 239 AND @typeId != 241 AND @typeId != 99 AND @typeId != 165
			SET @typeName = @typeName + '?'

		PRINT '        private ' + @typeName + ' ' + Lower(@name) + ';'	
		IF @isprimarykey = 1
		BEGIN
			set @pkName = @name
			PRINT '        [PrimaryKey]'
		END
		
		PRINT '        public ' + @typeName + ' ' + @name	
		PRINT '        {'			
		PRINT '            set'
		PRINT '            {'
		PRINT '                if (this.' + Lower(@name) + ' == value)'
		PRINT '                    return;'
		PRINT '                this.' + Lower(@name) + ' = value;'
		PRINT '                this.OnPropertyChanged("' + @name + '");'
		PRINT '            }'
		PRINT '            get { return this.' + Lower(@name) + '; }'
		PRINT '        }'
		PRINT ''
		FETCH NEXT FROM tableColumns INTO @name, @typeId, @isNullable, @isprimarykey
	END
	
	PRINT '    }'

	
	
	CLOSE tableColumns
	DEALLOCATE tableColumns
	print ''
	FETCH NEXT FROM table_cursor INTO @tableName
END
CLOSE table_cursor
DEALLOCATE table_cursor
print '}'
