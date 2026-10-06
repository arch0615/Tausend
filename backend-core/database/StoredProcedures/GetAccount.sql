CREATE PROCEDURE [dbo].[GetAccount]
	@AccessToken NVARCHAR(100)
AS	BEGIN
	SET NOCOUNT ON
	-- TRY_CAST: a malformed (non-GUID) token must not throw, just fail to match
	DECLARE @TokenGuid UNIQUEIDENTIFIER = TRY_CAST(@AccessToken AS UNIQUEIDENTIFIER)

	SELECT AT.AccountID, Email, FirstName, LastName, CAST(@AccessToken AS CHAR(36)), D.Description, D.Identifier, ADP.PIN, D.DeviceId, A.Role, D.IsOnline
	FROM Accounts A
	LEFT JOIN AccessTokens AT ON AT.AccountID = A.AccountId
	LEFT JOIN AccountDevicePins ADP ON ADP.AccountId = AT.AccountID
	LEFT JOIN Devices D ON D.DeviceId = ADP.DeviceId AND D.Enabled = 1
	WHERE AT.AccessToken = @TokenGuid AND AT.ExpirationDateTime > GETUTCDATE()

	DECLARE @AccountId BIGINT
	SELECT @AccountId = AccountId FROM AccessTokens WHERE AccessToken = @TokenGuid AND ExpirationDateTime > GETUTCDATE()

	SELECT DeviceId, [Description], Identifier, Device_PIN, SIM_PIN, PhoneNumber, DeviceType
	FROM SMS_Devices WHERE AccountId = @AccountId

END
