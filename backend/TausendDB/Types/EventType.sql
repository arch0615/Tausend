CREATE TYPE [dbo].[EventType] AS TABLE
(
	Secuence INT,
    EventDateTime DATETIME,
    EventType NVARCHAR(50),
    NotificationType INT,
    Partition INT,
    AlarmParameter INT,
    AlarmIdentifier NVARCHAR(50),
    Text NVARCHAR(100)
)
