INSERT INTO OilCompany (OilCompanyId, Name, Logo)
SELECT 
	'fc3280e0-2abd-4545-aa4e-b231698a3536', 
    'Hellenic Fuel Station',
    BulkColumn
FROM OPENROWSET(
        BULK 'E:\Temp\Images\logo.jpg',
        SINGLE_BLOB
    ) AS ImageSource;
