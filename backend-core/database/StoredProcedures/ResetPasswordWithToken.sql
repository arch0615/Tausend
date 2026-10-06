CREATE PROCEDURE [dbo].[ResetPasswordWithToken]
	@ResetToken NVARCHAR(100),
	@NewPasswordHash NVARCHAR(100)
AS BEGIN
	SET NOCOUNT ON
	DECLARE
		@AccountId BIGINT,
		@CurrentUTCDateTime DATETIME,
		-- TRY_CAST: a malformed (non-GUID) token must not throw, just fail to match
		@TokenGuid UNIQUEIDENTIFIER = TRY_CAST(@ResetToken AS UNIQUEIDENTIFIER)

	SELECT @CurrentUTCDateTime = GETUTCDATE()

	SELECT @AccountId = AccountID FROM PasswordResetTokens
		WHERE ResetToken = @TokenGuid
			AND UsedDateTime IS NULL
			AND ExpirationDateTime > @CurrentUTCDateTime

	IF ISNULL(@AccountId, 0) < 1 BEGIN
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	UPDATE PasswordResetTokens SET UsedDateTime = @CurrentUTCDateTime WHERE ResetToken = @TokenGuid

	UPDATE Accounts
	SET PasswordHash = @NewPasswordHash, [Password] = NULL, UpdatedDateTime = @CurrentUTCDateTime
	WHERE AccountId = @AccountId

	DELETE FROM AccessTokens WHERE AccountID = @AccountId
	DELETE FROM AccountDeviceTokens WHERE AccountId = @AccountId
	UPDATE RefreshTokens SET RevokedDateTime = @CurrentUTCDateTime
		WHERE AccountId = @AccountId AND RevokedDateTime IS NULL

	SELECT @AccountId
END
