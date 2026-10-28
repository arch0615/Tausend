-- Called after DeviceBusiness.ValidatePinOnDevice already proved the caller knows this panel's
-- own live user code (the same live relay check CreateDevice/UpdateDevice use) -- no AccountId
-- check here, since the installer may be setting this up before any account is even linked to
-- the panel (client, 26 Oct: upload the logos for an identifier ahead of the customer downloading
-- the app at all). Both logos are set in one call, matching the client's own spec ("the installer
-- also uploads the (2) new JPG logos") -- there is no single-logo variant.
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
		Logo1Base64 = @Logo1Base64, Logo1MimeType = @Logo1MimeType,
		Logo2Base64 = @Logo2Base64, Logo2MimeType = @Logo2MimeType,
		UpdatedDateTime = GETDATE()
	WHERE Identifier = @Identifier AND Enabled = 1
	SELECT 1
END
