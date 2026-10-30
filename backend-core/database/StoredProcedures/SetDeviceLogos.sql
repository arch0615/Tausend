-- Called after DeviceBusiness.ValidatePinOnDevice already proved the caller knows this panel's
-- own live user code (the same live relay check CreateDevice/UpdateDevice use) -- no AccountId
-- check here, since the installer may be setting this up before any account is even linked to
-- the panel (client, 26 Oct: upload the logos for an identifier ahead of the customer downloading
-- the app at all).
--
-- An empty logo means "leave that slot as it is", not "clear it": "you can modify one or both
-- images; it is not mandatory to change both" (client, 30 Oct). Clearing is a separate, explicit
-- action -- see ClearDeviceLogos, behind the screen's "reset to default images" button.
CREATE PROCEDURE [dbo].[SetDeviceLogos]
	@Identifier NVARCHAR(20),
	@Logo1Base64 NVARCHAR(MAX),
	@Logo1MimeType NVARCHAR(20),
	@Logo2Base64 NVARCHAR(MAX),
	@Logo2MimeType NVARCHAR(20)
AS BEGIN
	IF NOT EXISTS (SELECT 1 FROM Devices WHERE Identifier = @Identifier AND Enabled = 1) BEGIN
		SELECT -1
		RETURN 0
	END
	UPDATE Devices SET
		Logo1Base64 = CASE WHEN ISNULL(@Logo1Base64, '') = '' THEN Logo1Base64 ELSE @Logo1Base64 END,
		Logo1MimeType = CASE WHEN ISNULL(@Logo1Base64, '') = '' THEN Logo1MimeType ELSE @Logo1MimeType END,
		Logo2Base64 = CASE WHEN ISNULL(@Logo2Base64, '') = '' THEN Logo2Base64 ELSE @Logo2Base64 END,
		Logo2MimeType = CASE WHEN ISNULL(@Logo2Base64, '') = '' THEN Logo2MimeType ELSE @Logo2MimeType END,
		UpdatedDateTime = GETDATE()
	WHERE Identifier = @Identifier AND Enabled = 1
	SELECT 1
END
