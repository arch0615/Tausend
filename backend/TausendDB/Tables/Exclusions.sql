CREATE TABLE [dbo].[Exclusions]
(
	[ExclusionId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[DeviceId] BIGINT NOT NULL REFERENCES Devices(DeviceId),
	[Exclusion] INT NOT NULL,
	[Name] NVARCHAR(100) NOT NULL
)
