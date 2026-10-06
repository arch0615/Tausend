CREATE PROCEDURE [dbo].[DeleteDeviceToken]
	@DeviceToken NVARCHAR(500)
AS BEGIN
	SET NOCOUNT ON
	DELETE FROM AccountDeviceTokens WHERE DeviceToken = @DeviceToken
END
