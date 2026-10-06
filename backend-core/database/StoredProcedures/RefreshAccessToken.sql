CREATE PROCEDURE [dbo].[RefreshAccessToken]
	@RefreshToken NVARCHAR(100)
AS BEGIN
	SET NOCOUNT ON
	DECLARE
		@AccountID BIGINT,
		@CurrentUTCDateTime DATETIME,
		@NewAccessToken UNIQUEIDENTIFIER,
		@NewRefreshToken UNIQUEIDENTIFIER,
		-- TRY_CAST: a malformed (non-GUID) token must not throw, just fail to match
		@TokenGuid UNIQUEIDENTIFIER = TRY_CAST(@RefreshToken AS UNIQUEIDENTIFIER)

	SELECT @CurrentUTCDateTime = GETUTCDATE()

	SELECT @AccountID = AccountID FROM RefreshTokens
		WHERE RefreshToken = @TokenGuid
			AND RevokedDateTime IS NULL
			AND ExpirationDateTime > @CurrentUTCDateTime

	IF ISNULL(@AccountID, 0) < 1 BEGIN
		SELECT CAST(-1 AS BIGINT) AS AccountID, CAST(NULL AS CHAR(36)) AS AccessToken, CAST(NULL AS CHAR(36)) AS RefreshToken
		RETURN 0
	END

	-- Rotate: the used refresh token is single-use
	UPDATE RefreshTokens SET RevokedDateTime = @CurrentUTCDateTime WHERE RefreshToken = @TokenGuid

	SELECT @NewAccessToken = NEWID()
	DELETE FROM AccessTokens WHERE AccountID = @AccountID
	INSERT INTO AccessTokens(AccessToken, AccountID, CreatedDateTime, ExpirationDateTime)
		VALUES (@NewAccessToken, @AccountID, @CurrentUTCDateTime, DATEADD(DAY, 7, @CurrentUTCDateTime))

	SELECT @NewRefreshToken = NEWID()
	INSERT INTO RefreshTokens(RefreshToken, AccountID, CreatedDateTime, ExpirationDateTime)
		VALUES (@NewRefreshToken, @AccountID, @CurrentUTCDateTime, DATEADD(DAY, 90, @CurrentUTCDateTime))

	SELECT @AccountID AS AccountID, CAST(@NewAccessToken AS CHAR(36)) AS AccessToken, CAST(@NewRefreshToken AS CHAR(36)) AS RefreshToken
END
