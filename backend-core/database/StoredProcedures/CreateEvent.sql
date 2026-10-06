CREATE PROCEDURE [dbo].[CreateEvent]
	@Event EventType READONLY
AS BEGIN

	DECLARE @Result BIGINT

	INSERT INTO Events (Secuence, EventDateTime, EventType, NotificationType, Partition, AlarmParameter, AlarmIdentifier, Text)
	SELECT Secuence, EventDateTime, EventType, NotificationType, Partition, AlarmParameter, AlarmIdentifier, Text
	FROM @Event

	SELECT @Result = CAST(SCOPE_IDENTITY() AS BIGINT)

	SELECT @Result

END
