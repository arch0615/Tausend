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
		-- No ORDER BY here used to mean SQL Server could return any Enabled=1 row sharing this
		-- Identifier -- harmless when a panel is only ever linked once, but this codebase has
		-- repeatedly left duplicate Devices rows behind (re-pairing before CreateDevice.sql's
		-- reuse fix, blocked/reset flows, etc.), and an unscoped caller like the relay's own
		-- registration lookup (see TausendRelay/Business/BackendClient.cs) has no AccountId to
		-- disambiguate with. The most recently touched row is the one actually in active use.
		SELECT TOP 1 @DeviceId = ISNULL(D.DeviceId, 0) FROM Devices D WHERE Identifier = @Identifier AND Enabled = 1 ORDER BY UpdatedDateTime DESC
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
