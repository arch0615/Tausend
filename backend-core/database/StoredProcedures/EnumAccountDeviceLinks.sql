CREATE PROCEDURE [dbo].[EnumAccountDeviceLinks]
AS BEGIN
	SET NOCOUNT ON
	-- Every AccountId<->DeviceId link in the fleet, one row per pair -- the dashboard's Admin
	-- console loads this alongside EnumAllAccounts/EnumAllDevices (already-established pattern:
	-- whole small tables, correlated client-side) to answer "which devices does this account
	-- have" and "which accounts have this device" in either direction without an admin-scoped
	-- per-row lookup endpoint. Deliberately excludes the PIN column (AccountDevicePins.PIN) --
	-- no existing admin screen exposes PIN codes, and this endpoint isn't the place to start.
	SELECT DISTINCT AccountId, DeviceId
	FROM AccountDevicePins
	ORDER BY AccountId, DeviceId
END
