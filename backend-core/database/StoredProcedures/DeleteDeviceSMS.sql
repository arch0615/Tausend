CREATE PROCEDURE [dbo].[DeleteDeviceSMS]
	@DeviceId BIGINT,
	@AccountId BIGINT
AS BEGIN

	-- Caller must own this SMS device -- see ../../backend/DAY5_SUMMARY.md.
	IF (SELECT COUNT(1) FROM SMS_Devices WHERE DeviceId = @DeviceId AND AccountId = @AccountId) < 1 BEGIN
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	DELETE FROM SMS_Devices WHERE DeviceId = @DeviceId

	SELECT @DeviceId

END
