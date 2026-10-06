CREATE PROCEDURE [dbo].[DeleteAccount]
	@AccessToken NVARCHAR(100)
AS BEGIN
	SET NOCOUNT ON
	DECLARE @AccountId BIGINT, @DeviceId BIGINT
	-- TRY_CAST: a malformed (non-GUID) token must not throw, just fail to match
	DECLARE @TokenGuid UNIQUEIDENTIFIER = TRY_CAST(@AccessToken AS UNIQUEIDENTIFIER)

	SELECT @AccountId = AccountId FROM AccessTokens WHERE AccessToken = @TokenGuid
	DELETE FROM AccessTokens WHERE AccountID = @AccountId
	
	UPDATE Accounts Set Enabled = 0, DeletedDateTime = GETUTCDATE() FROM Accounts WHERE AccountId = @AccountId

	SELECT @DeviceId = DeviceId FROM AccountDevicePins WHERE AccountId = @AccountId
	IF(@DeviceId IS NOT NULL) BEGIN -- DELETE RELATION
		EXEC DeleteAccountDevice @AccountId, @DeviceId
	END
END
