CREATE PROCEDURE [dbo].[SetAccountEnabled]
	@AccountId BIGINT,
	@Enabled BIT
AS BEGIN
	SET NOCOUNT ON

	IF NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountId = @AccountId) BEGIN
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	UPDATE Accounts
	SET [Enabled] = @Enabled, UpdatedDateTime = GETUTCDATE()
	WHERE AccountId = @AccountId

	SELECT @AccountId
END
