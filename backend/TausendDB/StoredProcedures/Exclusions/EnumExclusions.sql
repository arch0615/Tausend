CREATE PROCEDURE [dbo].[EnumExclusions]
	@DeviceId BIGINT
AS BEGIN
	SELECT ExclusionId, @DeviceId, Exclusion, Name
	FROM Exclusions 
	WHERE DeviceId = @DeviceId
END
