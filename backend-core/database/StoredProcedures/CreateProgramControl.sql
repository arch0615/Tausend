CREATE PROCEDURE [dbo].[CreateProgramControl]
	@ProgramControl ProgramControlType READONLY
AS BEGIN

	DECLARE @DeviceId BIGINT

	SELECT TOP 1 @DeviceId = DeviceId FROM @ProgramControl

	UPDATE ProgramControls
	SET Name = PGM.Name
	FROM @ProgramControl PGM
	WHERE ProgramControls.DeviceId = PGM.DeviceId AND ProgramControls.ProgramControl = PGM.ProgramControl

	INSERT INTO ProgramControls(DeviceId, ProgramControl, Name)
	SELECT DeviceId, ProgramControl, Name FROM @ProgramControl
	WHERE ProgramControl NOT IN (SELECT ProgramControl FROM ProgramControls WHERE DeviceId = @DeviceId)
END
