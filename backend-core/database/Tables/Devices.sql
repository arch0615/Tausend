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
	-- White-label branding (client, 26/27 Oct): two images uploaded through the app, base64-encoded,
	-- shown instead of the default Tausend eagle/wordmark on every account this panel is linked to.
	-- Logo1 is portrait, shown on the login screen in place of the eagle + stacked wordmark. Logo2
	-- is landscape, shown as a banner across the top of the home screen in place of the default
	-- "ALARMAS TAUSEND" wordmark strip. NULL means no custom branding for that slot.
	Logo1Base64 NVARCHAR(MAX) NULL,
	Logo2Base64 NVARCHAR(MAX) NULL,
	-- Client, 28 Oct: "it should admit both with and without background so it should be JPG or
	-- PNG" -- the picker's own detected MIME type (image/jpeg or image/png), not assumed. JPG has
	-- no transparency, so a backgroundless logo has to actually be stored and served as PNG or the
	-- transparent area renders as a solid color instead of "the app background screen" showing
	-- through, which is what the client asked for. NULL (pre-existing rows from before this column
	-- existed) is treated as image/jpeg by the reader, matching what every logo before this was.
	Logo1MimeType NVARCHAR(20) NULL,
	Logo2MimeType NVARCHAR(20) NULL
)
