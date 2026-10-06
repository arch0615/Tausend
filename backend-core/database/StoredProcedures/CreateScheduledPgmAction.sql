CREATE PROCEDURE [dbo].[CreateScheduledPgmAction]
	@DeviceId BIGINT,
	@ProgramControlNumber INT,
	@TimeOfDay TIME,
	@DaysOfWeekMask TINYINT,
	@DesiredState BIT
AS BEGIN
	SET NOCOUNT ON

	INSERT INTO ScheduledPgmActions(DeviceId, ProgramControlNumber, TimeOfDay, DaysOfWeekMask, DesiredState, [Enabled], CreatedDateTime)
	VALUES (@DeviceId, @ProgramControlNumber, @TimeOfDay, @DaysOfWeekMask, @DesiredState, 1, GETUTCDATE())

	SELECT CAST(SCOPE_IDENTITY() AS BIGINT)
END
