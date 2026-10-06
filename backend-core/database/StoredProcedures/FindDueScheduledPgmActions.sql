-- Polled by the backend's ScheduledPgmDispatcher background service roughly once a minute.
-- @CurrentTime should already be truncated to the minute by the caller (seconds zeroed) so an
-- exact TIME match is meaningful despite the poll not landing on the exact second configured.
CREATE PROCEDURE [dbo].[FindDueScheduledPgmActions]
	@CurrentTime TIME,
	@CurrentDayMask TINYINT,
	@Today DATE
AS BEGIN
	SET NOCOUNT ON
	SELECT ScheduledPgmActionId, DeviceId, ProgramControlNumber, DesiredState
	FROM ScheduledPgmActions
	WHERE [Enabled] = 1
		AND TimeOfDay = @CurrentTime
		AND (DaysOfWeekMask & @CurrentDayMask) <> 0
		AND (LastFiredDate IS NULL OR LastFiredDate <> @Today)
END
