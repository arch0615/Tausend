CREATE PROCEDURE [dbo].[ValidateAccessToken]
	@AccessToken NVARCHAR(100)
AS BEGIN
	DECLARE @AccountId BIGINT
	-- TRY_CAST: a malformed (non-GUID) token must not throw, just fail to match
	DECLARE @TokenGuid UNIQUEIDENTIFIER = TRY_CAST(@AccessToken AS UNIQUEIDENTIFIER)

	SELECT @AccountId = ISNULL(AccountID, 0) FROM AccessTokens
		WHERE AccessToken = @TokenGuid AND ExpirationDateTime > GETUTCDATE()

	IF ISNULL(@AccountId, 0) = 0 BEGIN
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	SELECT @AccountId
END
