CREATE PROCEDURE [dbo].[BlockDevice]
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

	DECLARE @AccountsIds AS TABLE(AccountId INT)
	INSERT INTO @AccountsIds(AccountId) SELECT AccountId FROM AccountDevicePins WHERE DeviceId IN (SELECT DeviceId FROM @DevicesIds)

	DELETE FROM AccountDeviceTokens WHERE AccountId IN (SELECT * FROM @AccountsIds)
	DELETE FROM AccessTokens WHERE AccountID IN (SELECT * FROM @AccountsIds)
	-- Reassigned to the dedicated, permanently-disabled quarantine account (see
	-- QUARANTINE_ACCOUNT_EMAIL in StartupMigrations.cs) rather than a hardcoded AccountId -- a
	-- literal "1" isn't guaranteed to be anything special and in this database is a real,
	-- enabled admin account, not an inert placeholder.
	UPDATE AccountDevicePins SET AccountId = (SELECT AccountId FROM Accounts WHERE Email = 'quarantine@system.alarmastausend.internal') WHERE DeviceId IN (SELECT * FROM @DevicesIds)

	SELECT 1
END
