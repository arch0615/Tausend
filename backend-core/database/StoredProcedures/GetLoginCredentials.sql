-- Returns the raw credential material for an email so AccountDao can verify the
-- password in C# (bcrypt has no T-SQL equivalent of a WHERE Password = @Hash check).
CREATE PROCEDURE [dbo].[GetLoginCredentials]
	@Email NVARCHAR(255)
AS BEGIN
	SET NOCOUNT ON
	-- TOP 1 with an explicit ORDER BY: Accounts.Email has no unique constraint, and accounts
	-- deleted before the DeleteAccount tombstone fix can still leave two rows sharing one
	-- address. Without this, the unordered SELECT handed AccountDao whichever row the engine
	-- returned first (in practice the oldest, disabled one), so the live account could never
	-- log in. Enabled rows win, newest first.
	SELECT TOP 1 AccountId, PasswordHash, [Password], [Enabled]
	FROM Accounts
	WHERE Email = @Email
	ORDER BY [Enabled] DESC, AccountId DESC
END
