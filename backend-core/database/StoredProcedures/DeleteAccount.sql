CREATE PROCEDURE [dbo].[DeleteAccount]
	@AccessToken NVARCHAR(100)
AS BEGIN
	SET NOCOUNT ON
	DECLARE @AccountId BIGINT
	-- TRY_CAST: a malformed (non-GUID) token must not throw, just fail to match
	DECLARE @TokenGuid UNIQUEIDENTIFIER = TRY_CAST(@AccessToken AS UNIQUEIDENTIFIER)

	SELECT @AccountId = AccountId FROM AccessTokens WHERE AccessToken = @TokenGuid
	DELETE FROM AccessTokens WHERE AccountID = @AccountId

	-- Deleting is a soft delete (the row stays for audit/event history), but the email address
	-- must NOT stay reserved by it. Accounts.Email has no unique constraint, so leaving the
	-- address on a disabled row meant a later CreateAccount inserted a SECOND row for the same
	-- address, after which GetLoginCredentials resolved the older disabled row and the address
	-- was locked out permanently: registration said "already exists", login failed, and password
	-- recovery (which filters on Enabled = 1) never issued a token. Tombstoning the address here
	-- frees it immediately and keeps the historical row intact and identifiable.
	-- Admin suspension (SetAccountEnabled) deliberately does NOT do this: it leaves
	-- DeletedDateTime NULL, so a suspended account keeps its address reserved.
	UPDATE Accounts
	SET [Enabled] = 0,
		DeletedDateTime = GETUTCDATE(),
		UpdatedDateTime = GETUTCDATE(),
		Email = LEFT(CONCAT('deleted+', CAST(@AccountId AS NVARCHAR(20)), '+', Email), 255)
	WHERE AccountId = @AccountId AND Email NOT LIKE 'deleted+%'

	-- Anything that could still authenticate as the old account goes with it.
	DELETE FROM AccountDeviceTokens WHERE AccountId = @AccountId
	DELETE FROM PasswordResetTokens WHERE AccountID = @AccountId
	UPDATE RefreshTokens SET RevokedDateTime = GETUTCDATE()
		WHERE AccountId = @AccountId AND RevokedDateTime IS NULL

	-- Unlink every panel this account has, not just one -- the previous single-@DeviceId lookup
	-- (no TOP/ORDER BY, so it picked an arbitrary row) silently left any additional linked
	-- panels' AccountDevicePins rows orphaned to a now-disabled account. Same end state as
	-- DeleteAccountDevice.sql, just set-based across every linked device at once.
	UPDATE Devices SET Enabled = 0, DeletedDateTime = GETUTCDATE(), UpdatedDateTime = GETUTCDATE()
		WHERE DeviceId IN (SELECT DeviceId FROM AccountDevicePins WHERE AccountId = @AccountId)

	DELETE FROM AccountDevicePins WHERE AccountId = @AccountId
END
