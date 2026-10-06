/*Returns count of Identifier|PIN pair in database*/
CREATE PROCEDURE CheckPinUsage
	@Identifier NVARCHAR(20),
	@PIN NVARCHAR(10)
AS BEGIN
	DECLARE @IDs AS TABLE(ID INT)
	INSERT INTO @IDs(ID) SELECT deviceId FROM Devices WHERE Identifier = @Identifier AND Enabled = 1

	-- BlockDevice.sql (and AdminBlockDevice.sql) reassign a device's PIN rows to a dedicated
	-- quarantine account instead of deleting them -- without this exclusion, a blocked panel's
	-- own PIN could never be used to re-link it again (through this same identifier+PIN the
	-- panel itself still has), even by its rightful owner, with no way to undo that from the app
	-- (no "Unlock" action exists; only the more destructive "Reset", which wipes the pairing
	-- rather than restoring it).
	SELECT COUNT(*) FROM AccountDevicePins WHERE DeviceId IN (SELECT ID FROM @IDs) AND PIN = @PIN
		AND AccountId <> (SELECT AccountId FROM Accounts WHERE Email = 'quarantine@system.alarmastausend.internal')
END
