CREATE PROCEDURE [dbo].[DeleteDevice]
	@DeviceId BIGINT,
	@AccountId BIGINT
AS BEGIN

	-- Caller must be one of the accounts this device is linked to (AccountDevicePins),
	-- not just any logged-in user -- see ../../backend/DAY5_SUMMARY.md.
	IF NOT EXISTS (
		SELECT 1 FROM Devices D
		INNER JOIN AccountDevicePins ADP ON ADP.DeviceId = D.DeviceId
		WHERE D.DeviceId = @DeviceId AND D.Enabled = 1 AND ADP.AccountId = @AccountId
	) BEGIN
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	DECLARE @DateTimeUTC DATETIME

	SELECT @DateTimeUTC = GETUTCDATE()

	UPDATE Devices SET Enabled = 0, IsOnline = 0, DeletedDateTime = @DateTimeUTC, UpdatedDateTime = @DateTimeUTC WHERE DeviceId = @DeviceId

	DELETE FROM AccountDevicePins WHERE DeviceId = @DeviceId

	SELECT @DeviceId

END
