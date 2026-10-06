CREATE PROCEDURE [dbo].[UpdateDeviceSMS]
	@DeviceId BIGINT,
	@Identifier NVARCHAR(255),
	@Description NVARCHAR(255),
	@DevicePin NVARCHAR(4),
	@SimPin NVARCHAR(4),
	@PhoneNumber NVARCHAR(30),
	@DeviceType NVARCHAR(20),
	@AccountId BIGINT
AS BEGIN

	DECLARE @DeviceCount BIGINT

	-- @AccountId is now the caller's own account (was previously derived from the row being
	-- checked, which could never fail) -- see backend/DAY5_SUMMARY.md.
	SELECT @DeviceCount = COUNT(*) FROM SMS_Devices WHERE DeviceId = @DeviceId AND AccountId = @AccountId
	IF(@DeviceCount <> 1) BEGIN -- Check if exists and belongs to the caller
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	SELECT @DeviceCount = COUNT(*) FROM SMS_Devices WHERE [Description] = @Description AND AccountId = @AccountId AND DeviceId <> @DeviceId
	IF(@DeviceCount > 0) BEGIN -- Check if description is duplicated for account 
		SELECT CAST(-2 AS BIGINT)
		RETURN 0
	END

	UPDATE SMS_Devices SET
	[Identifier] = @Identifier,
	[Description] = @Description,
	[Device_PIN] = @DevicePin,
	[SIM_PIN] = @SimPin,
	[PhoneNumber] = @PhoneNumber,
	[DeviceType] = @DeviceType,
	[UpdateDateTime] = GETUTCDATE()
	WHERE [DeviceId] = @DeviceId

	SELECT @DeviceId
END
