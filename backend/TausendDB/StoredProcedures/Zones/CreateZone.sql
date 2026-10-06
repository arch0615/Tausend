CREATE PROCEDURE [dbo].[CreateZone]
	@Zones ZoneType READONLY
AS BEGIN

	DECLARE @DeviceId BIGINT

	SELECT TOP 1 @DeviceId = DeviceId FROM @Zones

	UPDATE Zones 
	SET Name = Z.Name
	FROM @Zones Z
	WHERE Zones.DeviceId = Z.DeviceId AND Zones.Zone = Z.Zone

	INSERT INTO Zones (DeviceId, Zone, Name)
	SELECT DeviceId, Zone, Name FROM @Zones
	WHERE Zone NOT IN (SELECT Zone FROM Zones WHERE DeviceId = @DeviceId)
END
