CREATE PROCEDURE [dbo].[EnumDeviceTokensOfAccount]
	@AccountId BIGINT
AS BEGIN

	SET NOCOUNT ON

	SELECT ADT.DeviceTokenId, @AccountId, ADT.DeviceToken, ADT.OS
	FROM AccountDeviceTokens ADT
	WHERE ADT.AccountId = @AccountId

END
