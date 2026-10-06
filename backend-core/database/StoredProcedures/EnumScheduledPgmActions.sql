CREATE PROCEDURE [dbo].[EnumScheduledPgmActions]
	@DeviceId BIGINT
AS BEGIN
	SET NOCOUNT ON
	SELECT ScheduledPgmActionId, DeviceId, ProgramControlNumber, TimeOfDay, DaysOfWeekMask, DesiredState, [Enabled]
	FROM ScheduledPgmActions
	WHERE DeviceId = @DeviceId
	ORDER BY TimeOfDay
END
