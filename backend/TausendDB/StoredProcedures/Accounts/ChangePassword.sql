-- Deliberate password change (user-initiated change, or a completed reset-link flow).
-- Unlike SetPasswordHash, this invalidates every existing session for the account,
-- including refresh tokens -- otherwise a device that was logged in before the
-- password changed could just call RefreshAccessToken and stay logged in regardless.
CREATE PROCEDURE [dbo].[ChangePassword]
	@AccountId BIGINT,
	@PasswordHash NVARCHAR(100)
AS BEGIN
	SET NOCOUNT ON

	UPDATE Accounts
	SET PasswordHash = @PasswordHash, [Password] = NULL, UpdatedDateTime = GETUTCDATE()
	WHERE AccountId = @AccountId

	DELETE FROM AccessTokens WHERE AccountID = @AccountId
	DELETE FROM AccountDeviceTokens WHERE AccountId = @AccountId
	UPDATE RefreshTokens SET RevokedDateTime = GETUTCDATE()
		WHERE AccountId = @AccountId AND RevokedDateTime IS NULL
END
