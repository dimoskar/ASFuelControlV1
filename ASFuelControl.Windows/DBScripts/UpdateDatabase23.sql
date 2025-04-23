update dbo.Trader set IsCustomer = 1 where IsNull(IsCustomer, 0) = 0 and IsNull(IsSupplier, 0) = 0
GO
