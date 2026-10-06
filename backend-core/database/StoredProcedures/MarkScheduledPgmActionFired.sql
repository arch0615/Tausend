CREATE PROCEDURE [dbo].[MarkScheduledPgmActionFired]
	@ScheduledPgmActionId BIGINT,
	@Today DATE
AS BEGIN
	SET NOCOUNT ON
	UPDATE ScheduledPgmActions SET LastFiredDate = @Today WHERE ScheduledPgmActionId = @ScheduledPgmActionId
END
