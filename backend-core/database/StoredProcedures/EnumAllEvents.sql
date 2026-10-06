-- Admin-only, fleet-wide equivalent of EnumEvents -- not filtered to one device. Same TOP 200 /
-- column order as EnumEvents.sql; AlarmIdentifier lets the caller join back to a device client-side.
CREATE PROCEDURE [dbo].[EnumAllEvents]
AS BEGIN
	SELECT TOP 200 EventId, EventDateTime, EventType, NotificationType, Partition, AlarmParameter, AlarmIdentifier, Text
	FROM Events
	ORDER BY EventId DESC
END
