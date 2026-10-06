CREATE PROCEDURE [dbo].[EnumProgramControls]
	@DeviceId BIGINT
AS BEGIN
	SELECT ProgramControlId, @DeviceId, ProgramControl, Name
	FROM ProgramControls 
	WHERE DeviceId = @DeviceId
END
