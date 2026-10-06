-- Quiet hash upgrade only — no session invalidation. Used when a legacy SHA-256
-- account logs in successfully and gets silently migrated to bcrypt; the session
-- being created right now must not be torn down by its own login.
CREATE PROCEDURE [dbo].[SetPasswordHash]
	@AccountId BIGINT,
	@PasswordHash NVARCHAR(100)
AS BEGIN
	SET NOCOUNT ON
	UPDATE Accounts
	SET PasswordHash = @PasswordHash, [Password] = NULL, UpdatedDateTime = GETUTCDATE()
	WHERE AccountId = @AccountId
END
