CREATE PROCEDURE [dbo].[UpdateDevice]
	@DeviceId BIGINT,
	@Description NVARCHAR(255),
	@Identifier NVARCHAR(255),
	@Pin NVARCHAR(15),
	@AccountId BIGINT
AS BEGIN
	SET NOCOUNT ON

	DECLARE @CurrentDateTime DATETIME,
		@Val BIGINT

	-- Caller must be one of the accounts this device is linked to (AccountDevicePins),
	-- not just any logged-in user -- see backend/DAY5_SUMMARY.md.
	IF NOT EXISTS (
		SELECT 1 FROM Devices D
		INNER JOIN AccountDevicePins ADP ON ADP.DeviceId = D.DeviceId
		WHERE D.DeviceId = @DeviceId AND D.Enabled = 1 AND ADP.AccountId = @AccountId
	) BEGIN
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	SELECT @Val = ISNULL(D.DeviceId, 0) FROM Devices D, AccountDevicePins ADP
	WHERE D.Description = @Description AND ADP.DeviceId = D.DeviceId AND D.DeviceId <> @DeviceId AND AccountId = @AccountId 

	IF @Val > 0 BEGIN
			SELECT CAST(-2 AS BIGINT)
			RETURN 0
	END

	SELECT @CurrentDateTime = GETUTCDATE()

	UPDATE Devices
	SET Description = @Description,
	Identifier =  @Identifier,
	UpdatedDateTime = @CurrentDateTime
	WHERE DeviceId = @DeviceId

	UPDATE AccountDevicePins
	SET PIN = @Pin
	WHERE DeviceId = @DeviceId

	SELECT @DeviceId
END
