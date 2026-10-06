-- Plain access toggle -- unlike AdminResetDevice, this does NOT touch AccountDevicePins or any
-- tokens either way. "Unblock access" (spec item 11) re-enables a device without needing to
-- restore ownership rows that a prior Reset PIN may have already deleted.
CREATE PROCEDURE [dbo].[AdminSetDeviceEnabled]
	@DeviceId BIGINT,
	@Enabled BIT
AS BEGIN
	SET NOCOUNT ON

	IF NOT EXISTS (SELECT 1 FROM Devices WHERE DeviceId = @DeviceId) BEGIN
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	UPDATE Devices SET [Enabled] = @Enabled WHERE DeviceId = @DeviceId

	SELECT @DeviceId
END
