-- Issues an access/refresh token pair for an ALREADY-AUTHENTICATED account.
-- Credential verification happens in AccountDao (bcrypt can't be checked in T-SQL),
-- so this proc trusts @AccountId and only re-checks that the account is still enabled.
CREATE PROCEDURE [dbo].[CreateLoginSession]
	@AccountId BIGINT
AS BEGIN
	SET NOCOUNT ON
	DECLARE
		@AccessToken UNIQUEIDENTIFIER,
		@RefreshToken UNIQUEIDENTIFIER,
		@CurrentUTCDateTime DATETIME,
		@Email NVARCHAR(255)

	SELECT @Email = Email FROM Accounts WHERE AccountId = @AccountId AND [Enabled] = 1

	IF @Email IS NULL BEGIN
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	SELECT @CurrentUTCDateTime = GETUTCDATE()

	-- Access token: short-lived, reused while still valid
	SELECT TOP 1 @AccessToken = AccessToken FROM AccessTokens
		  WHERE AccountID = @AccountID AND ExpirationDateTime > @CurrentUTCDateTime

	IF @AccessToken IS NULL BEGIN
		SELECT @AccessToken = NEWID()
		DELETE FROM AccessTokens WHERE AccountID = @AccountID
		INSERT INTO AccessTokens(AccessToken, AccountID, CreatedDateTime, ExpirationDateTime)
		VALUES (@AccessToken, @AccountID, @CurrentUTCDateTime, DATEADD(DAY, 7, @CurrentUTCDateTime))
	END

	-- Refresh token: long-lived, reused while still valid and not revoked
	SELECT TOP 1 @RefreshToken = RefreshToken FROM RefreshTokens
		  WHERE AccountID = @AccountID AND RevokedDateTime IS NULL AND ExpirationDateTime > @CurrentUTCDateTime

	IF @RefreshToken IS NULL BEGIN
		SELECT @RefreshToken = NEWID()
		INSERT INTO RefreshTokens(RefreshToken, AccountID, CreatedDateTime, ExpirationDateTime)
		VALUES (@RefreshToken, @AccountID, @CurrentUTCDateTime, DATEADD(DAY, 90, @CurrentUTCDateTime))
	END

	SELECT @AccountID, @Email, FirstName, LastName, CAST(@AccessToken AS CHAR(36)), D.Description, D.Identifier, ADP.PIN, D.DeviceId, A.Role
	FROM Accounts A
	LEFT JOIN AccountDevicePins ADP ON ADP.AccountId = @AccountId
	LEFT JOIN Devices D ON D.DeviceId = ADP.DeviceId AND D.Enabled = 1
	WHERE A.AccountId = @AccountID

	SELECT DeviceId, [Description], Identifier, Device_PIN, SIM_PIN, PhoneNumber, DeviceType
	FROM SMS_Devices WHERE AccountId = @AccountId

	SELECT CAST(@RefreshToken AS CHAR(36)) AS RefreshToken
END
