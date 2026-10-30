CREATE PROCEDURE [dbo].[IsNotificationMuted]
	@DeviceToken NVARCHAR(500),
	@Identifier NVARCHAR(20)
AS BEGIN
	SELECT CASE WHEN EXISTS (
		SELECT 1 FROM MutedPanelNotifications WHERE DeviceToken = @DeviceToken AND Identifier = @Identifier
	) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
END
