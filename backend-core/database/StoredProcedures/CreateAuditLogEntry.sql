CREATE PROCEDURE [dbo].[CreateAuditLogEntry]
	@ActorAccountId BIGINT,
	@Action NVARCHAR(50),
	@TargetType NVARCHAR(30),
	@TargetId BIGINT = NULL,
	@Details NVARCHAR(400) = NULL
AS BEGIN
	SET NOCOUNT ON

	INSERT INTO AuditLog (ActorAccountId, [Action], TargetType, TargetId, Details, CreatedDateTime)
	VALUES (@ActorAccountId, @Action, @TargetType, @TargetId, @Details, GETUTCDATE())
END
