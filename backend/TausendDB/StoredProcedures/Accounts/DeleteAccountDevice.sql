CREATE PROCEDURE [dbo].[DeleteAccountDevice]
	@AccountId BIGINT,
	@DeviceId BIGINT
AS BEGIN
	DELETE FROM AccountDevicePins WHERE AccountId = @AccountId AND DeviceId = @DeviceId
	UPDATE Devices SET [Enabled] = 0, DeletedDateTime = GETUTCDATE(), UpdatedDateTime = GETUTCDATE() WHERE DeviceId = @DeviceId
END
