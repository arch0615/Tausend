-- Deployment order: Tables (Devices, UserTags) must exist before this Type, and this Type must
-- exist before any StoredProcedure that references it (CreateUserTag).
CREATE TYPE [dbo].[UserTagType] AS TABLE
(
	UserNumber INT,
	[UserName] NVARCHAR(100),
	DeviceId BIGINT
)
