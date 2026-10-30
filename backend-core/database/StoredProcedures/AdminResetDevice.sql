-- Admin-only unscoped equivalent of ResetDevice.sql -- see that file for why this deletes the
-- Devices row (and everything with a foreign key to it) instead of just disabling it.
CREATE PROCEDURE [dbo].[AdminResetDevice]
	@DeviceId BIGINT
AS BEGIN
	SET NOCOUNT ON

	-- The caller's admin status is already verified in AdminBusiness before this runs, so
	-- there's no ownership filter here.
	IF NOT EXISTS (SELECT 1 FROM Devices WHERE DeviceId = @DeviceId AND Enabled = 1) BEGIN
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	DECLARE @AccountsIds AS TABLE(AccountId BIGINT)
	INSERT INTO @AccountsIds(AccountId) SELECT AccountId FROM AccountDevicePins WHERE DeviceId = @DeviceId

	DELETE FROM AccountDevicePins WHERE DeviceId = @DeviceId
	DELETE FROM ScheduledPgmActions WHERE DeviceId = @DeviceId
	DELETE FROM Zones WHERE DeviceId = @DeviceId
	DELETE FROM UserTags WHERE DeviceId = @DeviceId
	DELETE FROM ProgramControls WHERE DeviceId = @DeviceId
	DELETE FROM Exclusions WHERE DeviceId = @DeviceId
	DELETE FROM Devices WHERE DeviceId = @DeviceId
	DELETE FROM AccountDeviceTokens WHERE AccountId IN (SELECT AccountId FROM @AccountsIds)
	DELETE FROM AccessTokens WHERE AccountID IN (SELECT AccountId FROM @AccountsIds)

	SELECT CAST(1 AS BIGINT)
END
