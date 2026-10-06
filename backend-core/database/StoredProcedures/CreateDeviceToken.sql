CREATE PROCEDURE [dbo].[CreateDeviceToken]
	@AccessToken NVARCHAR(100),
	@OS NVARCHAR(10),
	@DeviceToken NVARCHAR(500)
AS BEGIN

	SET NOCOUNT ON
	
	DECLARE @AccountId BIGINT, @Cant INT, @Result BIGINT, @ExistingId BIGINT
	-- TRY_CAST: a malformed (non-GUID) token must not throw, just fail to match
	DECLARE @TokenGuid UNIQUEIDENTIFIER = TRY_CAST(@AccessToken AS UNIQUEIDENTIFIER)

	SELECT @AccountId = ISNULL(AT.AccountId, 0) FROM AccessTokens AT, Accounts A WHERE AT.AccessToken = @TokenGuid AND AT.AccountID = A.AccountId AND A.Enabled = 1 AND AT.ExpirationDateTime > GETUTCDATE()

	IF @AccountId = 0 BEGIN
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	-- Permitir que el mismo device token pase de una cuenta a otra (por ejemplo, logout/login en el mismo dispositivo)
	DELETE FROM AccountDeviceTokens WHERE DeviceToken = @DeviceToken AND AccountId <> @AccountId

	-- Si ya existe para esta cuenta, actualizar OS y devolver el mismo id
	SELECT @ExistingId = DeviceTokenId FROM AccountDeviceTokens WHERE AccountId = @AccountId AND DeviceToken = @DeviceToken
	IF ISNULL(@ExistingId, 0) > 0 BEGIN
		UPDATE AccountDeviceTokens SET OS = @OS WHERE DeviceTokenId = @ExistingId
		SELECT @ExistingId
		RETURN 0
	END

	INSERT INTO AccountDeviceTokens(AccountId, OS, DeviceToken) VALUES (@AccountId, @OS, @DeviceToken)


	SELECT @Result = CAST(SCOPE_IDENTITY() AS BIGINT)

	SELECT @Result 

END
