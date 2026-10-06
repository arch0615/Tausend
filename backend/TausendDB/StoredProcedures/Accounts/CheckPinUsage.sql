/*Returns count of Identifier|PIN pair in database*/
CREATE PROCEDURE CheckPinUsage
	@Identifier NVARCHAR(20),
	@PIN NVARCHAR(10)
AS BEGIN
	DECLARE @IDs AS TABLE(ID INT)
	INSERT INTO @IDs(ID) SELECT deviceId FROM Devices WHERE Identifier = @Identifier AND Enabled = 1

	SELECT COUNT(*) FROM AccountDevicePins WHERE DeviceId IN (SELECT ID FROM @IDs) AND PIN = @PIN
END