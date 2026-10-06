CREATE PROCEDURE [dbo].[CreateUserTag]
	@Users UserTagType READONLY
AS BEGIN
	DECLARE @DeviceId BIGINT

	SELECT TOP 1 @DeviceId = DeviceId FROM @Users

	UPDATE UserTags
	SET UserName = U.UserName
	FROM @Users U
	WHERE UserTags.DeviceId = U.DeviceId AND UserTags.UserNumber = U.UserNumber

	INSERT INTO UserTags(DeviceId, UserNumber, UserName)
	SELECT DeviceId, UserNumber, UserName FROM @Users
	WHERE UserNumber NOT IN (SELECT UserNumber FROM UserTags WHERE DeviceId = @DeviceId)
END
