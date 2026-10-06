CREATE PROCEDURE [dbo].[CreateExclusions]
	@Exclusions ExclusionType READONLY
AS BEGIN

	DECLARE @DeviceId BIGINT

	SELECT TOP 1 @DeviceId = DeviceId FROM @Exclusions

	UPDATE Exclusions
	SET Name = E.Name
	FROM @Exclusions E
	WHERE Exclusions.DeviceId = E.DeviceId AND Exclusions.Exclusion = E.Exclusion

	INSERT INTO Exclusions(DeviceId, Exclusion, Name)
	SELECT DeviceId, Exclusion, Name FROM @Exclusions
	WHERE Exclusion NOT IN (SELECT Exclusion FROM Exclusions WHERE DeviceId = @DeviceId)
END
