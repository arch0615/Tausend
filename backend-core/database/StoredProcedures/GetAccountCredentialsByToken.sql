-- Same as GetLoginCredentials but resolved via an access token, for the
-- change-password flow (old-password verification).
CREATE PROCEDURE [dbo].[GetAccountCredentialsByToken]
	@AccessToken NVARCHAR(100)
AS BEGIN
	SET NOCOUNT ON
	-- TRY_CAST: a malformed (non-GUID) token must not throw, just fail to match
	DECLARE @TokenGuid UNIQUEIDENTIFIER = TRY_CAST(@AccessToken AS UNIQUEIDENTIFIER)

	SELECT A.AccountId, A.PasswordHash, A.[Password], A.[Enabled]
	FROM Accounts A
	INNER JOIN AccessTokens AT ON AT.AccountID = A.AccountId
	WHERE AT.AccessToken = @TokenGuid AND AT.ExpirationDateTime > GETUTCDATE()
END
