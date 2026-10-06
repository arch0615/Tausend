CREATE TABLE [dbo].[ProgramControls]
(
	[ProgramControlId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[DeviceId] BIGINT NOT NULL REFERENCES Devices(DeviceId),
	[ProgramControl] INT NOT NULL,
	[Name] NVARCHAR(100) NOT NULL
)
