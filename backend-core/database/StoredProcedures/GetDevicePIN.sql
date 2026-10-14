CREATE PROCEDURE [dbo].[GetDevicePIN]
	@DeviceId BIGINT,
	@AccountId BIGINT = NULL
AS BEGIN
	SET NOCOUNT ON
	-- Scoped to the caller's own link. AccountDevicePins has one row per (account, device), so
	-- without the AccountId filter this returned whichever account's code happened to come back
	-- first on a panel shared by more than one user. UpdateDevice compares that value against the
	-- code being submitted to decide whether the code changed; a mismatch made it re-run the
	-- in-use check, which then found the caller's OWN link row and refused the edit with "el
	-- código de usuario ingresado ya se encuentra en uso". That is why renaming a panel failed
	-- (client report, 14 Oct, video 3).
	--
	-- @AccountId defaults to NULL so an older caller that passes only @DeviceId still works,
	-- falling back to the previous unscoped behaviour rather than erroring.
	SELECT TOP 1 ISNULL(PIN, '') FROM AccountDevicePins
	WHERE DeviceId = @DeviceId
	  AND (@AccountId IS NULL OR AccountId = @AccountId)
	-- PinId DESC, not ASC. @AccountId IS NULL makes the CASE above evaluate to 1 for every row
	-- (AccountId = NULL is UNKNOWN), so the tiebreak decided the unscoped result on its own and
	-- ascending meant the OLDEST link row won. On a panel that had been paired more than once,
	-- or shared between accounts, that is a stale code: the command path then sent it to the
	-- panel, which obeyed it and attributed the event to whichever user that code belongs to.
	ORDER BY CASE WHEN AccountId = @AccountId THEN 0 ELSE 1 END, PinId DESC
END
