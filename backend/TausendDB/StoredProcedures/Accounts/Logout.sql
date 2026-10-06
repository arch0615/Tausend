CREATE PROCEDURE [dbo].[Logout]
	@AccessToken NVARCHAR(100),
	@DeviceToken NVARCHAR(500)
AS BEGIN
	DECLARE @AccountId BIGINT
	-- TRY_CAST: a malformed (non-GUID) token must not throw, just fail to match
	DECLARE @TokenGuid UNIQUEIDENTIFIER = TRY_CAST(@AccessToken AS UNIQUEIDENTIFIER)

	SELECT @AccountId = AccountId FROM AccessTokens WHERE AccessToken = @TokenGuid

	IF ISNULL(@AccountId, 0) = 0 BEGIN
		RETURN;
	END

	DELETE FROM AccountDeviceTokens WHERE AccountId = @AccountId AND DeviceToken = @DeviceToken

	UPDATE RefreshTokens SET RevokedDateTime = GETUTCDATE()
		WHERE AccountId = @AccountId AND RevokedDateTime IS NULL
END