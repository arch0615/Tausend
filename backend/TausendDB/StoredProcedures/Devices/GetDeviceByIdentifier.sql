CREATE PROCEDURE [dbo].[GetDeviceByIdentifier]
	@Identifier NVARCHAR(100),
	@AccountId BIGINT
AS BEGIN
	SET NOCOUNT ON

	DECLARE @DeviceId BIGINT

	IF @AccountId <> 0 BEGIN
		SELECT @DeviceId = ISNULL(D.DeviceId, 0) 
		FROM Devices D
		INNER JOIN AccountDevicePins ADP ON ADP.DeviceId = D.DeviceId
		WHERE Identifier = @Identifier AND Enabled = 1 AND ADP.AccountId = @AccountId
		END
	ELSE BEGIN
		SELECT TOP 1 @DeviceId = ISNULL(D.DeviceId, 0) FROM Devices D	WHERE Identifier = @Identifier AND Enabled = 1
	END

	IF ISNULL(@DeviceId, 0) = 0 BEGIN
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	DECLARE @IsOnline BIT,
		@LastConnection DATETIME

	SELECT @IsOnline = IsOnline, @LastConnection = LastConnection FROM Devices WHERE DeviceId = @DeviceId

	IF @IsOnline = 1 AND ABS(DATEDIFF(HOUR, @LastConnection, GETUTCDATE())) > 1 BEGIN
		UPDATE DEVICES SET IsOnline = 0 WHERE DeviceId = @DeviceId
	END

	SELECT D.DeviceId, D.Identifier, D.Description, D.IP, D.Port, D.IsOnline, D.LastConnection, D.PublicKey FROM Devices D WHERE DeviceId = @DeviceId

END