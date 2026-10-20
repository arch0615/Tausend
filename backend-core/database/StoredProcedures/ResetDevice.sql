-- "Eliminar para todos" on Gestionar central (client, 20 Oct): unlike BlockDevice, which parks
-- every account's link so the same identifier and code can relink later, this one is meant to be
-- a true wipe -- "all control panel data must be deleted from the server, including the panel
-- identifier". Devices.DeviceId is referenced by AccountDevicePins, ScheduledPgmActions, Zones,
-- UserTags, ProgramControls and Exclusions (REFERENCES Devices(DeviceId) on each -- every one of
-- them needs clearing before the Devices row itself can be deleted, or this throws a foreign key
-- violation instead of deleting anything. A relink after this creates a brand new DeviceId; that
-- loses no history, since those are exactly the rows just deleted, and Events is keyed by the
-- identifier string rather than DeviceId, so it is untouched either way.
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

	DECLARE @AccountsIds AS TABLE(AccountId INT)
	INSERT INTO @AccountsIds(AccountId) SELECT AccountId FROM AccountDevicePins WHERE DeviceId IN (SELECT DeviceId FROM @DevicesIds)

	DELETE FROM AccountDevicePins WHERE DeviceId IN (SELECT * FROM @DevicesIds)
	DELETE FROM ScheduledPgmActions WHERE DeviceId IN (SELECT * FROM @DevicesIds)
	DELETE FROM Zones WHERE DeviceId IN (SELECT * FROM @DevicesIds)
	DELETE FROM UserTags WHERE DeviceId IN (SELECT * FROM @DevicesIds)
	DELETE FROM ProgramControls WHERE DeviceId IN (SELECT * FROM @DevicesIds)
	DELETE FROM Exclusions WHERE DeviceId IN (SELECT * FROM @DevicesIds)
	DELETE FROM Devices WHERE DeviceId IN (SELECT * FROM @DevicesIds)
	DELETE FROM AccountDeviceTokens WHERE AccountId IN (SELECT * FROM @AccountsIds)
	DELETE FROM AccessTokens WHERE AccountID IN (SELECT * FROM @AccountsIds)

	SELECT 1
END
