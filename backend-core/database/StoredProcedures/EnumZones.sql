CREATE PROCEDURE [dbo].[EnumZones]
	@DeviceId BIGINT
AS BEGIN
	SELECT ZoneId, @DeviceId, Zone, Name
	FROM Zones
	WHERE DeviceId = @DeviceId
	ORDER BY Zone
END
