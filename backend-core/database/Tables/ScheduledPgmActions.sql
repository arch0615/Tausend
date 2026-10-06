-- A recurring "at this time, on these days, set this PGM output to this state" rule -- what
-- "Scheduled Departures" in the client's spec actually needs. Previously this screen name was
-- wired to the same immediate manual PGM toggle as the plain PGM screen, with no real scheduling
-- behind it at all.
CREATE TABLE [dbo].[ScheduledPgmActions]
(
	[ScheduledPgmActionId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	DeviceId BIGINT NOT NULL FOREIGN KEY REFERENCES Devices(DeviceId),
	ProgramControlNumber INT NOT NULL,
	TimeOfDay TIME NOT NULL,
	-- Bit N set (N = 0..6) means "fires on the day whose .NET DayOfWeek value is N"
	-- (Sunday=0 .. Saturday=6), so a mask of 1<<DayOfWeek from either side matches directly.
	DaysOfWeekMask TINYINT NOT NULL,
	DesiredState BIT NOT NULL,
	[Enabled] BIT NOT NULL,
	CreatedDateTime DATETIME NOT NULL,
	-- Date only (time truncated) of the last successful fire -- prevents firing twice if the
	-- dispatcher's poll interval and a schedule's minute ever line up more than once in a day.
	LastFiredDate DATE NULL
)
