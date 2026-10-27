CREATE PROCEDURE [dbo].[SetNotificationMute]
	@DeviceToken NVARCHAR(500),
	@Identifier NVARCHAR(20),
	@Muted BIT
AS BEGIN
	IF @Muted = 1 BEGIN
		IF NOT EXISTS (SELECT 1 FROM MutedPanelNotifications WHERE DeviceToken = @DeviceToken AND Identifier = @Identifier)
			INSERT INTO MutedPanelNotifications(DeviceToken, Identifier) VALUES (@DeviceToken, @Identifier)
	END ELSE BEGIN
		DELETE FROM MutedPanelNotifications WHERE DeviceToken = @DeviceToken AND Identifier = @Identifier
	END
	SELECT 1
END
