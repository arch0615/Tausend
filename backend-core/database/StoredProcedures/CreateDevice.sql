CREATE PROCEDURE [dbo].[CreateDevice]
	@AccessToken UniqueIdentifier,
	@Description NVARCHAR(255),
	@Identifier NVARCHAR(255),
	@Pin NVARCHAR(15),
	@PublicKey INT
AS BEGIN
	SET NOCOUNT ON
	DECLARE
		@AccountId BIGINT,
		@Val BIGINT,
		@CurrentDateTime DATETIME,
		@Result BIGINT

	SELECT @AccountId = ISNULL(AccountID, 0) FROM AccessTokens WHERE AccessToken = @AccessToken

	IF ISNULL(@AccountId, 0) = 0 BEGIN
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	-- D.Enabled = 1: a deleted/disabled panel used to keep its description reserved forever,
	-- while being invisible in the account's own panel list (client issue #15).
	SELECT @Val = ISNULL(D.DeviceId, 0) FROM Devices D, AccountDevicePins ADP
		WHERE D.Description = @Description AND ADP.DeviceId = D.DeviceId AND ADP.AccountId = @AccountId AND D.Enabled = 1

	IF ISNULL(@Val, 0) > 0 BEGIN
		SELECT CAST(-2 AS BIGINT)
		RETURN 0
	END

	SELECT @CurrentDateTime = GETUTCDATE()

	-- Find a live row for this panel.
	--
	-- This used to be `SELECT @Result = ISNULL(DeviceId, 0) FROM Devices WHERE ...`, which looks
	-- like it defaults to 0 but does not: when the SELECT matches no rows at all, @Result is
	-- never assigned and keeps its previous value, which here is NULL. ISNULL only rewrites a
	-- NULL column inside a row that was actually returned. `IF @Result = 0` is then UNKNOWN
	-- rather than true, so the INSERT below was skipped and the link row was inserted with a
	-- NULL DeviceId -- surfacing to the user as the raw SQL error "Cannot insert the value NULL
	-- into column 'DeviceId'". That is the client's "no es posible vincular centrales".
	--
	-- It bites whenever no ENABLED row exists for the identifier, which is exactly the state
	-- after a panel has been deleted, hence "after unlinking it can never be linked again".
	SET @Result = NULL
	SELECT TOP 1 @Result = DeviceId FROM Devices
		WHERE Identifier = @Identifier AND Enabled = 1
		ORDER BY DeviceId DESC

	IF @Result IS NULL BEGIN
		-- A retired row for this same panel is revived rather than inserting a second one, so a
		-- panel keeps one identity (and its event history) across unlink/relink cycles.
		SELECT TOP 1 @Result = DeviceId FROM Devices
			WHERE Identifier = @Identifier
			ORDER BY DeviceId DESC

		IF @Result IS NULL BEGIN
			INSERT INTO Devices(Description, Identifier, CreatedDateTime, UpdatedDateTime, IsOnline, Enabled, PublicKey)
				VALUES (@Description, @Identifier, @CurrentDateTime, @CurrentDateTime, 0, 1, @PublicKey)
			SELECT @Result = CAST(SCOPE_IDENTITY() AS BIGINT)
		END
		ELSE BEGIN
			UPDATE Devices
			SET Description = @Description,
				Enabled = 1,
				DeletedDateTime = NULL,
				UpdatedDateTime = @CurrentDateTime,
				PublicKey = @PublicKey
			WHERE DeviceId = @Result

			-- Reviving the row brings it back to Enabled = 1, which also brings back every
			-- AccountDevicePins row still attached to it: GetAccount lists an account's panels
			-- through those rows joined to enabled Devices, so any account that held a code on
			-- this panel before it was retired would silently see it again, and be able to
			-- command it. Found in live data (15 Oct): one identifier with five disabled Devices
			-- rows carrying seven link rows across four accounts.
			--
			-- Those rows are residue. DeleteDevice clears every link for the device and
			-- BlockDevice reassigns them to the quarantine account, so by the time no enabled row
			-- exists, no legitimate link does either. Clearing them keeps a relink granting
			-- access to exactly one account: the one doing it.
			DELETE FROM AccountDevicePins WHERE DeviceId = @Result
		END
	END
	ELSE BEGIN
		UPDATE Devices SET Description = @Description, UpdatedDateTime = @CurrentDateTime WHERE DeviceId = @Result
	END

	-- Belt and braces: never attempt the link insert without a real DeviceId.
	IF @Result IS NULL BEGIN
		SELECT CAST(-1 AS BIGINT)
		RETURN 0
	END

	-- Re-linking a panel this account already holds replaces its row instead of stacking a
	-- second one, which would make the account's own panel list show the same panel twice.
	DELETE FROM AccountDevicePins WHERE AccountId = @AccountId AND DeviceId = @Result
	INSERT INTO AccountDevicePins(AccountId, DeviceId, PIN) VALUES (@AccountId, @Result, @Pin)

	SELECT @Result
END
