-- Client, 27 Oct: "Apagar notificaciones" on Gestionar central -- mutes push notifications for
-- one panel on one phone only, without affecting any other account/phone that has the same panel
-- linked. A row's mere existence means muted; there is no DeviceToken/Identifier unique index here
-- on purpose -- AccountDeviceTokens.DeviceToken is NVARCHAR(500) and a unique index over a pair
-- including it would exceed SQL Server's 900-byte nonclustered index key limit. SetNotificationMute
-- checks existence itself before inserting, the same way AccountDeviceTokens avoids a DB-level
-- uniqueness constraint on its own DeviceToken column.
CREATE TABLE [dbo].[MutedPanelNotifications]
(
	[MutedPanelNotificationId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	DeviceToken NVARCHAR(500) NOT NULL,
	[Identifier] NVARCHAR(20) NOT NULL
)
