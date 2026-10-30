-- Backs the "reset to default images" button the client asked for on 30 Oct ("it must also be
-- possible to restore the default images"). Nulling both slots is what restores the default look:
-- every screen that renders a logo already falls back to the bundled logo1.png/logo2.png when the
-- panel has none, so there is nothing to put back, only something to remove.
--
-- Same gate as SetDeviceLogos -- the caller has already proven it knows the panel's live user code.
CREATE PROCEDURE [dbo].[ClearDeviceLogos]
	@Identifier NVARCHAR(20)
AS BEGIN
	IF NOT EXISTS (SELECT 1 FROM Devices WHERE Identifier = @Identifier AND Enabled = 1) BEGIN
		SELECT -1
		RETURN 0
	END
	UPDATE Devices SET
		Logo1Base64 = NULL, Logo1MimeType = NULL,
		Logo2Base64 = NULL, Logo2MimeType = NULL,
		UpdatedDateTime = GETDATE()
	WHERE Identifier = @Identifier AND Enabled = 1
	SELECT 1
END
