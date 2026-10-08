-- Issues a short-lived, single-use reset token and returns enough account info
-- to compose the reset email. Does not reveal whether the email exists to the
-- caller's response shape -- AccountBusiness treats "no row" the same as "sent"
-- from the client's point of view, to avoid leaking account existence.
CREATE PROCEDURE [dbo].[CreatePasswordResetToken]
	@Email NVARCHAR(255)
AS BEGIN
	SET NOCOUNT ON
	DECLARE
		@AccountId BIGINT,
		@ResetToken UNIQUEIDENTIFIER,
		@CurrentUTCDateTime DATETIME

	-- TOP 1 + ORDER BY for the same reason as GetLoginCredentials: one address can still map to
	-- more than one row on databases that predate the DeleteAccount tombstone fix.
	SELECT TOP 1 @AccountId = AccountId FROM Accounts
		WHERE Email = @Email AND [Enabled] = 1
		ORDER BY AccountId DESC

	IF ISNULL(@AccountId, 0) = 0 BEGIN
		SELECT CAST(-1 AS BIGINT) AS AccountId, CAST(NULL AS CHAR(36)) AS ResetToken, CAST(NULL AS NVARCHAR(255)) AS Email, CAST(NULL AS NVARCHAR(100)) AS FirstName
		RETURN 0
	END

	SELECT @CurrentUTCDateTime = GETUTCDATE()
	SELECT @ResetToken = NEWID()

	INSERT INTO PasswordResetTokens(ResetToken, AccountID, CreatedDateTime, ExpirationDateTime)
		VALUES (@ResetToken, @AccountId, @CurrentUTCDateTime, DATEADD(HOUR, 1, @CurrentUTCDateTime))

	SELECT @AccountId AS AccountId, CAST(@ResetToken AS CHAR(36)) AS ResetToken, Email, FirstName
	FROM Accounts WHERE AccountId = @AccountId
END
