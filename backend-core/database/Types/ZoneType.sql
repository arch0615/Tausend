-- Deployment order: Tables (Devices, Zones) must exist before this Type, and this Type must
-- exist before any StoredProcedure that references it (CreateZone).
CREATE TYPE [dbo].[ZoneType] AS TABLE
(
	[DeviceId] BIGINT,
	[Zone] INT,
	[Name] NVARCHAR(100)
)
