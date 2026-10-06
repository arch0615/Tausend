-- Force-logout: kills every currently-valid access/refresh token for the account, without
-- touching Accounts.Enabled -- the account can still log back in and get a fresh session. Used
-- standalone ("Revoke active sessions") and also called by SetAccountEnabled(false), since
-- disabling an account alone doesn't invalidate tokens already issued (ValidateAccessToken and
-- RefreshAccessToken don't re-check Enabled, only CreateLoginSession does).
CREATE PROCEDURE [dbo].[RevokeAccountSessions]
	@AccountId BIGINT
AS BEGIN
	SET NOCOUNT ON

	IF NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountId = @AccountId) BEGIN
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	DELETE FROM AccessTokens WHERE AccountID = @AccountId

	UPDATE RefreshTokens
	SET RevokedDateTime = GETUTCDATE()
	WHERE AccountID = @AccountId AND RevokedDateTime IS NULL

	SELECT @AccountId
END
