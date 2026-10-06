CREATE PROCEDURE [dbo].[EnumAllDevices]
AS BEGIN
	SET NOCOUNT ON
	SELECT DeviceId, Description, Identifier, [Enabled], IsOnline, LastConnection, CreatedDateTime
	FROM Devices
	ORDER BY CreatedDateTime DESC
END
