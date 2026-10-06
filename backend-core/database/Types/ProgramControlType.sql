-- Deployment order: Tables (Devices, ProgramControls) must exist before this Type, and this Type
-- must exist before any StoredProcedure that references it (CreateProgramControl).
CREATE TYPE [dbo].[ProgramControlType] AS TABLE
(
	[DeviceId] BIGINT,
	[ProgramControl] INT,
	[Name] NVARCHAR(100)
)
