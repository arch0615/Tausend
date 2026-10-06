CREATE PROCEDURE [dbo].[EnumAuditLog]
AS BEGIN
	SET NOCOUNT ON
	SELECT TOP 200
		L.AuditLogId, L.ActorAccountId, A.Email, A.FirstName, A.LastName,
		L.[Action], L.TargetType, L.TargetId, L.Details, L.CreatedDateTime
	FROM AuditLog L
	LEFT JOIN Accounts A ON A.AccountId = L.ActorAccountId
	ORDER BY L.CreatedDateTime DESC
END
