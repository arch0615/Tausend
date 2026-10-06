CREATE PROCEDURE [dbo].[GetUserTag]
	@AccountId BIGINT,
	@AlarmIdentifier NVARCHAR(100),
	@User INT
AS BEGIN
	DECLARE @DeviceId BIGINT,
		@Tag NVARCHAR(100)

	SELECT @DeviceId = D.DeviceId FROM Devices D, AccountDevicePins AD WHERE AD.DeviceId = D.DeviceId AND AD.AccountId = @AccountId AND D.Identifier = @AlarmIdentifier AND D.Enabled = 1

	SELECT @Tag = UserName FROM UserTags WHERE DeviceId = @DeviceId AND UserNumber = @User

	SELECT @Tag

END
