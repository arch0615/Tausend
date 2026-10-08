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

		SELECT @AccountId = ISNULL(AccountID, 0) FROM AccessTokens where AccessToken = @AccessToken

		IF @AccountId = 0 BEGIN
			SELECT CAST(-1 AS BIGINT)
			RETURN 0
		END

	-- D.Enabled = 1: a deleted/disabled panel used to keep its description reserved forever,
	-- while being invisible in the account's own panel list (client issue #15).
	SELECT @Val = ISNULL(D.DeviceId, 0) FROM Devices D, AccountDevicePins ADP
		WHERE D.Description = @Description AND ADP.DeviceId = D.DeviceId AND ADP.AccountId = @AccountId AND D.Enabled = 1

		IF @Val > 0 BEGIN
			SELECT CAST(-2 AS BIGINT)
			RETURN 0
		END

		SELECT @CurrentDateTime = GETUTCDATE()

		-- A device that was Block-ed (see BlockDevice.sql) keeps its Devices row Enabled=1 --
		-- only its PIN rows get reassigned to the orphan account, so the same physical panel
		-- (same Identifier) can be re-linked with its own PIN afterward. Without this reuse
		-- check, re-linking used to INSERT a second Devices row sharing the same Identifier,
		-- silently duplicating the panel and leaving zones/PGM/events split across two DeviceIds
		-- for what is really one physical central.
		SELECT @Result = ISNULL(DeviceId, 0) FROM Devices WHERE Identifier = @Identifier AND Enabled = 1

		IF @Result = 0 BEGIN
			INSERT INTO Devices(Description, Identifier, CreatedDateTime, UpdatedDateTime, IsOnline, Enabled, PublicKey) VALUES (@Description, @Identifier, @CurrentDateTime, @CurrentDateTime, 0, 1, @PublicKey)
			SELECT @Result = CAST(SCOPE_IDENTITY() AS BIGINT)
		END
		ELSE BEGIN
			UPDATE Devices SET Description = @Description, UpdatedDateTime = @CurrentDateTime WHERE DeviceId = @Result
		END

		INSERT INTO AccountDevicePins(AccountId, DeviceId, PIN) VALUES (@AccountId, @Result, @Pin)

		SELECT @Result
	END
