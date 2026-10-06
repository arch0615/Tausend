CREATE PROCEDURE [dbo].[EnumOwnersOfDevice]
	@Identifier VARCHAR(100)
AS BEGIN
	SET NOCOUNT ON

	SELECT A.AccountId, A.Email, A.FirstName, A.LastName 
	FROM Accounts A, AccountDevicePins ADP, Devices D
	WHERE D.Enabled = 1
	AND D.Identifier = @Identifier
	AND D.DeviceId = ADP.DeviceId
	AND ADP.AccountId = A.AccountId
	AND A.Enabled = 1

END