CREATE PROCEDURE [dbo].[CreateDevice]
	@AccessToken UniqueIdentifier,
	@Description NVARCHAR(255),
	@Identifier NVARCHAR(255),
	@Pin NVARCHAR(15),
	@PublicKey INT
AS BEGIN
	SET NOCOUNT ON
	DECLARE
		@AccountId BIGINT,
		@Val BIGINT,
		@CurrentDateTime DATETIME,
		@Result BIGINT

		SELECT @AccountId = ISNULL(AccountID, 0) FROM AccessTokens where AccessToken = @AccessToken

		IF @AccountId = 0 BEGIN
			SELECT CAST(-1 AS BIGINT)
			RETURN 0
		END

		SELECT @Val = ISNULL(D.DeviceId, 0) FROM Devices D, AccountDevicePins ADP WHERE D.Description = @Description AND ADP.DeviceId = D.DeviceId AND AccountId = @AccountId

		IF @Val > 0 BEGIN
			SELECT CAST(-2 AS BIGINT)
			RETURN 0
		END

		SELECT @CurrentDateTime = GETUTCDATE()

		INSERT INTO Devices(Description, Identifier, CreatedDateTime, UpdatedDateTime, IsOnline, Enabled, PublicKey) VALUES (@Description, @Identifier, @CurrentDateTime, @CurrentDateTime, 0, 1, @PublicKey)

		SELECT @Result = CAST(SCOPE_IDENTITY() AS BIGINT)

		INSERT INTO AccountDevicePins(AccountId, DeviceId, PIN) VALUES (@AccountId, @Result, @Pin)

		SELECT @Result
	END
