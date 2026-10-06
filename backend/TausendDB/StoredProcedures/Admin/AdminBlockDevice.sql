CREATE PROCEDURE [dbo].[AdminBlockDevice]
	@DeviceId BIGINT
AS BEGIN
	SET NOCOUNT ON

	-- Admin-only unscoped equivalent of BlockDevice.sql -- the caller's admin status is
	-- already verified in AdminBusiness before this runs, so there's no ownership filter here.
	IF NOT EXISTS (SELECT 1 FROM Devices WHERE DeviceId = @DeviceId AND Enabled = 1) BEGIN
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	DECLARE @AccountsIds AS TABLE(AccountId BIGINT)
	INSERT INTO @AccountsIds(AccountId) SELECT AccountId FROM AccountDevicePins WHERE DeviceId = @DeviceId

	DELETE FROM AccountDeviceTokens WHERE AccountId IN (SELECT AccountId FROM @AccountsIds)
	DELETE FROM AccessTokens WHERE AccountID IN (SELECT AccountId FROM @AccountsIds)
	UPDATE AccountDevicePins SET AccountId = 1 WHERE DeviceId = @DeviceId

	SELECT CAST(1 AS BIGINT)
END
