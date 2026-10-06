-- Admin-only equivalent of DisassociateCentral's per-account branch, keyed by AccountId+DeviceId
-- directly (the dashboard already has both) instead of round-tripping through Identifier.
CREATE PROCEDURE [dbo].[AdminUnlinkAccountDevice]
	@AccountId BIGINT,
	@DeviceId BIGINT
AS BEGIN
	SET NOCOUNT ON

	DELETE FROM AccountDevicePins WHERE AccountId = @AccountId AND DeviceId = @DeviceId

	SELECT CAST(@@ROWCOUNT AS BIGINT)
END
