CREATE PROCEDURE [dbo].[ResetDevice]
	@Identifier NVARCHAR(20),
	@AccountId BIGINT
AS BEGIN
	-- Caller must be one of the accounts this device is linked to -- see ../../backend/DAY5_SUMMARY.md.
	DECLARE @DevicesIds AS TABLE(DeviceId INT)
	INSERT INTO @DevicesIds(DeviceId)
	SELECT D.DeviceId FROM Devices D
	INNER JOIN AccountDevicePins ADP ON ADP.DeviceId = D.DeviceId
	WHERE D.Identifier = @Identifier AND D.Enabled = 1 AND ADP.AccountId = @AccountId
	IF((SELECT COUNT(*) FROM @DevicesIds) < 1) BEGIN
		SELECT -1
		RETURN 0
	END
	UPDATE Devices SET Enabled = 0 WHERE DeviceId IN (SELECT * FROM @DevicesIds)

	DECLARE @AccountsIds AS TABLE(AccountId INT)
	INSERT INTO @AccountsIds(AccountId) SELECT AccountId FROM AccountDevicePins WHERE DeviceId IN (SELECT DeviceId FROM @DevicesIds)

	DELETE FROM AccountDevicePins WHERE DeviceId IN (SELECT * FROM @DevicesIds)
	DELETE FROM AccountDeviceTokens WHERE AccountId IN (SELECT * FROM @AccountsIds)
	DELETE FROM AccessTokens WHERE AccountID IN (SELECT * FROM @AccountsIds)

	SELECT 1
END
