CREATE TABLE [dbo].[Devices]
(
	[DeviceId] BIGINT NOT NULL PRIMARY KEY IDENTITY(1,1),
	Description NVARCHAR(255) NOT NULL,
	IP NVARCHAR(255) NULL,
	Port NVARCHAR(8) NULL,
	[Identifier] NVARCHAR(255) NOT NULL,
	[Enabled] BIT NOT NULL, 
	IsOnline BIT NOT NULL,
	LastConnection DATETIME NULL,
	CreatedDateTime DATETIME NOT NULL,
	DeletedDateTime DATETIME NULL, 
    UpdatedDateTime DATETIME NOT NULL,
	PublicKey INT NULL,
	-- White-label branding (client, 26/27 Oct): two jpgs uploaded through the app, base64-encoded,
	-- shown instead of the default Tausend eagle/wordmark on every account this panel is linked to.
	-- Logo1 is portrait, shown on the login screen in place of the eagle + stacked wordmark. Logo2
	-- is landscape, shown as a banner across the top of the home screen in place of the default
	-- "ALARMAS TAUSEND" wordmark strip. NULL means no custom branding for that slot.
	Logo1Base64 NVARCHAR(MAX) NULL,
	Logo2Base64 NVARCHAR(MAX) NULL
)
