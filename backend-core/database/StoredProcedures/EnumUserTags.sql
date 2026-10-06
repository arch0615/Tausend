CREATE PROCEDURE [dbo].[EnumUserTags]
	@DeviceId BIGINT
AS BEGIN
	SELECT UserTagId, @DeviceId, UserNumber, UserName
	FROM UserTags
	WHERE DeviceId = @DeviceId
	ORDER BY UserNumber
END
