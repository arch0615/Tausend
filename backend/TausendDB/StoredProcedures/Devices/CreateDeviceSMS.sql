CREATE PROCEDURE [dbo].[CreateDeviceSMS]
	@AccessToken UniqueIdentifier,
	@Description NVARCHAR(255),
	@Identifier NVARCHAR(255),
	@DevicePin NVARCHAR(4),
	@SimPin NVARCHAR(4),
	@PhoneNumber NVARCHAR(30),
	@DeviceType NVARCHAR(20)
AS BEGIN
	SET NOCOUNT ON
	DECLARE 
		@AccountId BIGINT,
		@DevicesCount INT,
		@CurrentDateTime DATETIME,
		@Result BIGINT

	SELECT @AccountId = ISNULL(AccountID, 0) FROM AccessTokens WHERE AccessToken = @AccessToken
	IF @AccountId = 0 BEGIN
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	SELECT @DevicesCount = COUNT(*) FROM SMS_Devices WHERE @AccountId = AccountId AND @Description = [Description]

	IF @DevicesCount > 0 BEGIN
		SELECT CAST(-2 AS BIGINT)
		RETURN 0
	END

	SELECT @CurrentDateTime = GETUTCDATE()

	INSERT INTO SMS_Devices([AccountId], [Description], [Identifier], [Device_PIN], [SIM_PIN], [PhoneNumber], [DeviceType], [CreatedDateTime], [UpdateDateTime])
	VALUES(@AccountId, @Description, @Identifier, @DevicePin, @SimPin, @PhoneNumber, @DeviceType, @CurrentDateTime, @CurrentDateTime)

	SELECT @Result = CAST(SCOPE_IDENTITY() AS BIGINT)

	SELECT @Result

END