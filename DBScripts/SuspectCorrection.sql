--1
EXEC sp_resetstatus 'ASFuelControlDB';
ALTER DATABASE ASFuelControlDB SET EMERGENCY

--2
DBCC CHECKDB ('ASFuelControlDB')

--3
ALTER DATABASE ASFuelControlDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE

--4
DBCC CHECKDB ('ASFuelControlDB', REPAIR_ALLOW_DATA_LOSS)

--5
ALTER DATABASE ASFuelControlDB SET MULTI_USER
