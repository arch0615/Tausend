-- Deployment order: Tables (Events) must exist before this Type, and this Type must exist before
-- any StoredProcedure that references it (CreateEvent).
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
