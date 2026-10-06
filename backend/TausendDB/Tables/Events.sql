CREATE TABLE [dbo].[Events]
(
    EventId BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
    Secuence INT,
    EventDateTime DATETIME NOT NULL,
    EventType NVARCHAR(50) NOT NULL,
    NotificationType INT NOT NULL,
    Partition INT NOT NULL,
    AlarmParameter INT NOT NULL,
    AlarmIdentifier NVARCHAR(50) NOT NULL,
    Text NVARCHAR(100) NOT NULL
)
