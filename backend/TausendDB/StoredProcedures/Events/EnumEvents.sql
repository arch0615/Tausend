CREATE PROCEDURE [dbo].[EnumEvents]
	@DeviceId BIGINT
AS BEGIN
	SELECT TOP 200 E.EventId, E.EventDateTime, E.Text FROM Events E, Devices D
	WHERE D.DeviceId = @DeviceId AND
	E.AlarmIdentifier = D.Identifier
	ORDER BY E.EventId DESC
END
