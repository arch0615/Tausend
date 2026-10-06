CREATE PROCEDURE [dbo].[SetScheduledPgmActionEnabled]
	@ScheduledPgmActionId BIGINT,
	@DeviceId BIGINT,
	@Enabled BIT
AS BEGIN
	SET NOCOUNT ON
	UPDATE ScheduledPgmActions SET [Enabled] = @Enabled
	WHERE ScheduledPgmActionId = @ScheduledPgmActionId AND DeviceId = @DeviceId
	SELECT CAST(@@ROWCOUNT AS BIGINT)
END
