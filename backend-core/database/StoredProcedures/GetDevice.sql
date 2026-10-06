CREATE PROCEDURE [dbo].[GetDevice]
	@DeviceId BIGINT,
	@AccountId BIGINT
AS BEGIN
	SET NOCOUNT ON

	-- @AccountId = 0 means an internal/system caller (the relay, via SystemAuth) that isn't
	-- scoped to one customer's account; anything else must own this device via
	-- AccountDevicePins -- see ../../backend/DAY5_SUMMARY.md.
	IF @AccountId <> 0 BEGIN
		IF NOT EXISTS (
			SELECT 1 FROM Devices D
			INNER JOIN AccountDevicePins ADP ON ADP.DeviceId = D.DeviceId
			WHERE D.DeviceId = @DeviceId AND D.Enabled = 1 AND ADP.AccountId = @AccountId
		) BEGIN
			SELECT CAST(-1 AS BIGINT)
			RETURN 0
		END
	END
	ELSE BEGIN
		IF (SELECT COUNT(1) FROM Devices WHERE DeviceId = @DeviceId AND Enabled = 1) < 1 BEGIN
			SELECT CAST(-1 AS BIGINT)
			RETURN 0
		END
	END

	DECLARE @IsOnline BIT,
		@LastConnection DATETIME

	SELECT @IsOnline = IsOnline, @LastConnection = LastConnection FROM Devices WHERE DeviceId = @DeviceId

	IF @IsOnline = 1 AND ABS(DATEDIFF(HOUR, @LastConnection, GETUTCDATE())) > 1 BEGIN
		UPDATE DEVICES SET IsOnline = 0 WHERE DeviceId = @DeviceId
	END

	SELECT D.DeviceId, D.Identifier, D.Description, D.IP, D.Port, D.IsOnline, D.LastConnection, D.PublicKey FROM Devices D WHERE DeviceId = @DeviceId

END
