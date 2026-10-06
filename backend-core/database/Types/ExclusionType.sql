-- Deployment order: Tables (Devices, Exclusions) must exist before this Type, and this Type must
-- exist before any StoredProcedure that references it (CreateExclusions).
CREATE TYPE [dbo].[ExclusionType] AS TABLE
(
	[DeviceId] BIGINT,
	[Exclusion] INT,
	[Name] NVARCHAR(100)
)
