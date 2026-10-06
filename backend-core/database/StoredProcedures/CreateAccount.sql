CREATE PROCEDURE [dbo].[CreateAccount]
	@FirstName nvarchar(100),
	@LastName nvarchar(100),
	@EMail nvarchar(255),
	@PasswordHash nvarchar(100)
AS BEGIN
	SET NOCOUNT ON
	DECLARE
		@Result BIGINT = 0,
		@Val BIGINT,
		@CurrentDateTime DATETIME

		SELECT @Val = ISNULL(AccountId, 0) FROM Accounts(READPAST) WHERE EMail = @EMail AND [Enabled] = 1
		IF @Val > 0 BEGIN
			SELECT CAST(-1 AS BIGINT)
			RETURN 0
		END

		SELECT @CurrentDateTime = GETUTCDATE()

		INSERT INTO Accounts([EMail], FirstName, LastName,	PasswordHash, CreatedDateTime, [Enabled], UpdatedDateTime)
					VALUES(@EMail, @FirstName, @LastName, @PasswordHash, @CurrentDateTime, 1, @CurrentDateTime)

		SELECT @Result = CAST(SCOPE_IDENTITY() AS BIGINT)

		SELECT @Result

END
