CREATE PROCEDURE [dbo].[UpdateDeviceConnectionParameters]
	@DeviceId BIGINT,
	@IP NVARCHAR(255),
	@Port NVARCHAR(8)
	
AS BEGIN
SET NOCOUNT ON

	IF (SELECT COUNT(1) FROM Devices WHERE DeviceId = @DeviceId AND Enabled = 1) < 1 BEGIN
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	DECLARE @LastConnection DATETIME

	SELECT @LastConnection = GETUTCDATE()

	UPDATE Devices SET IP = @IP, Port = @Port, LastConnection = @LastConnection, IsOnline = 1 WHERE DeviceId = @DeviceId

	SELECT @DeviceId
END
