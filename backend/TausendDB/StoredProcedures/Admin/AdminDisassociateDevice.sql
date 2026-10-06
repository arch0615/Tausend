CREATE PROCEDURE [dbo].[AdminDisassociateDevice]
	@DeviceId BIGINT
AS BEGIN
	SET NOCOUNT ON

	-- Admin-only unscoped equivalent of DisassociateCentral.sql's @AccountId = 0 branch --
	-- unlinks every account from the device, keyed by DeviceId since that's what the fleet
	-- dashboard already has on hand (no Identifier round trip needed).
	DELETE FROM AccountDevicePins WHERE DeviceId = @DeviceId

	SELECT CAST(@@ROWCOUNT AS BIGINT)
END
