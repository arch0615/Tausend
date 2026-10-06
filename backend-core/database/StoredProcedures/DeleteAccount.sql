CREATE PROCEDURE [dbo].[DeleteAccount]
	@AccessToken NVARCHAR(100)
AS BEGIN
	SET NOCOUNT ON
	DECLARE @AccountId BIGINT
	-- TRY_CAST: a malformed (non-GUID) token must not throw, just fail to match
	DECLARE @TokenGuid UNIQUEIDENTIFIER = TRY_CAST(@AccessToken AS UNIQUEIDENTIFIER)

	SELECT @AccountId = AccountId FROM AccessTokens WHERE AccessToken = @TokenGuid
	DELETE FROM AccessTokens WHERE AccountID = @AccountId

	UPDATE Accounts Set Enabled = 0, DeletedDateTime = GETUTCDATE() FROM Accounts WHERE AccountId = @AccountId

	-- Unlink every panel this account has, not just one -- the previous single-@DeviceId lookup
	-- (no TOP/ORDER BY, so it picked an arbitrary row) silently left any additional linked
	-- panels' AccountDevicePins rows orphaned to a now-disabled account. Same end state as
	-- DeleteAccountDevice.sql, just set-based across every linked device at once.
	UPDATE Devices SET Enabled = 0, DeletedDateTime = GETUTCDATE(), UpdatedDateTime = GETUTCDATE()
		WHERE DeviceId IN (SELECT DeviceId FROM AccountDevicePins WHERE AccountId = @AccountId)

	DELETE FROM AccountDevicePins WHERE AccountId = @AccountId
END
