CREATE PROCEDURE [dbo].[EnumAllAccounts]
AS BEGIN
	SET NOCOUNT ON
	SELECT AccountId, Email, FirstName, LastName, Role, [Enabled], CreatedDateTime
	FROM Accounts
	ORDER BY CreatedDateTime DESC
END
