-- Returns the raw credential material for an email so AccountDao can verify the
-- password in C# (bcrypt has no T-SQL equivalent of a WHERE Password = @Hash check).
CREATE PROCEDURE [dbo].[GetLoginCredentials]
	@Email NVARCHAR(255)
AS BEGIN
	SET NOCOUNT ON
	SELECT AccountId, PasswordHash, [Password], [Enabled]
	FROM Accounts
	WHERE Email = @Email
END
