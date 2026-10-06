-- Scoped by DeviceId as well as the row's own id -- an account authorized for one device must
-- not be able to delete another device's schedule by guessing its id.
CREATE PROCEDURE [dbo].[DeleteScheduledPgmAction]
	@ScheduledPgmActionId BIGINT,
	@DeviceId BIGINT
AS BEGIN
	SET NOCOUNT ON
	DELETE FROM ScheduledPgmActions WHERE ScheduledPgmActionId = @ScheduledPgmActionId AND DeviceId = @DeviceId
	SELECT CAST(@@ROWCOUNT AS BIGINT)
END
