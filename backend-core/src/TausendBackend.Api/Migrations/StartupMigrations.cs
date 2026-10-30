using Microsoft.Data.SqlClient;

namespace TausendBackend.Api.Migrations
{
    /// <summary>
    /// One-off schema fixes applied automatically at startup, using the app's own already-
    /// configured DB connection (see SqlDbContext) rather than a separate deploy-time SQL step.
    /// Written as CREATE OR ALTER (or, for the rare seed row, an IF NOT EXISTS guard) so
    /// re-running on every startup is a harmless no-op once applied. Add new statements here as
    /// they come up; there's no tracking table since every statement here is idempotent.
    /// </summary>
    public static class StartupMigrations
    {
        public static async Task ApplyAsync(IConfiguration configuration, ILogger logger)
        {
            var connectionString = configuration.GetConnectionString("TausendConnectionString");
            if (string.IsNullOrEmpty(connectionString))
            {
                logger.LogWarning("StartupMigrations skipped -- no TausendConnectionString configured.");
                return;
            }

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            foreach (var sql in Statements)
            {
                await using var command = new SqlCommand(sql, connection);
                await command.ExecuteNonQueryAsync();
            }

            logger.LogInformation("StartupMigrations applied ({Count} statements).", Statements.Length);
        }

        private static readonly string[] Statements =
        {
            // GetAccount and CreateLoginSession both used to omit D.IsOnline entirely, so every
            // device the app ever displayed came back with IsOnline defaulted to false regardless
            // of its real connection state (see AccountDao.GetAccountFromReader).
            """
            CREATE OR ALTER PROCEDURE [dbo].[GetAccount]
                @AccessToken NVARCHAR(100)
            AS	BEGIN
                SET NOCOUNT ON
                DECLARE @TokenGuid UNIQUEIDENTIFIER = TRY_CAST(@AccessToken AS UNIQUEIDENTIFIER)

                -- IsOnline is a flag the relay sets when a panel connects. Nothing clears it if the
                -- panel simply stops talking, so a panel that dropped off hours ago still reported
                -- itself online and the device selector kept showing "En línea" / "Conectada"
                -- (client report, 14 Oct). GetDeviceByIdentifier already guards against this with a
                -- one-hour freshness window; the same rule is applied here so the account's own
                -- panel list cannot disagree with it. A panel that has never connected
                -- (LastConnection NULL) counts as offline.
                SELECT AT.AccountID, Email, FirstName, LastName, CAST(@AccessToken AS CHAR(36)), D.Description, D.Identifier, ADP.PIN, D.DeviceId, A.Role,
                       CASE WHEN D.IsOnline = 1 AND D.LastConnection IS NOT NULL
                                 AND D.LastConnection > DATEADD(HOUR, -1, GETUTCDATE())
                            THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
                FROM Accounts A
                LEFT JOIN AccessTokens AT ON AT.AccountID = A.AccountId
                LEFT JOIN AccountDevicePins ADP ON ADP.AccountId = AT.AccountID
                LEFT JOIN Devices D ON D.DeviceId = ADP.DeviceId AND D.Enabled = 1
                WHERE AT.AccessToken = @TokenGuid AND AT.ExpirationDateTime > GETUTCDATE()

                DECLARE @AccountId BIGINT
                SELECT @AccountId = AccountId FROM AccessTokens WHERE AccessToken = @TokenGuid AND ExpirationDateTime > GETUTCDATE()

                SELECT DeviceId, [Description], Identifier, Device_PIN, SIM_PIN, PhoneNumber, DeviceType
                FROM SMS_Devices WHERE AccountId = @AccountId
            END
            """,
            """
            CREATE OR ALTER PROCEDURE [dbo].[CreateLoginSession]
                @AccountId BIGINT
            AS BEGIN
                SET NOCOUNT ON
                DECLARE
                    @AccessToken UNIQUEIDENTIFIER,
                    @RefreshToken UNIQUEIDENTIFIER,
                    @CurrentUTCDateTime DATETIME,
                    @Email NVARCHAR(255)

                SELECT @Email = Email FROM Accounts WHERE AccountId = @AccountId AND [Enabled] = 1

                IF @Email IS NULL BEGIN
                    SELECT CAST(-1 AS BIGINT)
                    RETURN 0
                END

                SELECT @CurrentUTCDateTime = GETUTCDATE()

                UPDATE Accounts SET LastLoginDateTime = @CurrentUTCDateTime WHERE AccountId = @AccountId

                SELECT TOP 1 @AccessToken = AccessToken FROM AccessTokens
                      WHERE AccountID = @AccountID AND ExpirationDateTime > @CurrentUTCDateTime

                IF @AccessToken IS NULL BEGIN
                    SELECT @AccessToken = NEWID()
                    DELETE FROM AccessTokens WHERE AccountID = @AccountID
                    INSERT INTO AccessTokens(AccessToken, AccountID, CreatedDateTime, ExpirationDateTime)
                    VALUES (@AccessToken, @AccountID, @CurrentUTCDateTime, DATEADD(DAY, 7, @CurrentUTCDateTime))
                END

                SELECT TOP 1 @RefreshToken = RefreshToken FROM RefreshTokens
                      WHERE AccountID = @AccountID AND RevokedDateTime IS NULL AND ExpirationDateTime > @CurrentUTCDateTime

                IF @RefreshToken IS NULL BEGIN
                    SELECT @RefreshToken = NEWID()
                    INSERT INTO RefreshTokens(RefreshToken, AccountID, CreatedDateTime, ExpirationDateTime)
                    VALUES (@RefreshToken, @AccountID, @CurrentUTCDateTime, DATEADD(DAY, 90, @CurrentUTCDateTime))
                END

                -- IsOnline is a flag the relay sets when a panel connects. Nothing clears it if the
                -- panel simply stops talking, so a panel that dropped off hours ago still reported
                -- itself online and the device selector kept showing "En línea" / "Conectada"
                -- (client report, 14 Oct). GetDeviceByIdentifier already guards against this with a
                -- one-hour freshness window; the same rule is applied here so the account's own
                -- panel list cannot disagree with it. A panel that has never connected
                -- (LastConnection NULL) counts as offline.
                SELECT @AccountID, @Email, FirstName, LastName, CAST(@AccessToken AS CHAR(36)), D.Description, D.Identifier, ADP.PIN, D.DeviceId, A.Role,
                       CASE WHEN D.IsOnline = 1 AND D.LastConnection IS NOT NULL
                                 AND D.LastConnection > DATEADD(HOUR, -1, GETUTCDATE())
                            THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
                FROM Accounts A
                LEFT JOIN AccountDevicePins ADP ON ADP.AccountId = @AccountId
                LEFT JOIN Devices D ON D.DeviceId = ADP.DeviceId AND D.Enabled = 1
                WHERE A.AccountId = @AccountID

                SELECT DeviceId, [Description], Identifier, Device_PIN, SIM_PIN, PhoneNumber, DeviceType
                FROM SMS_Devices WHERE AccountId = @AccountId

                SELECT CAST(@RefreshToken AS CHAR(36)) AS RefreshToken
            END
            """,
            // None of these four had an ORDER BY at all, so SQL Server was free to return rows in
            // whatever order was physically convenient -- normally insertion order, but an UPDATE
            // (e.g. renaming a zone to a longer string) can force a row to a new page, which then
            // reshuffles the *next* scan's natural order. That's exactly the client-reported bug:
            // editing a few zones/outputs/exclusions/labels "grouped them together" out of their
            // original numeric order. Ordering explicitly by the number column fixes it for good.
            """
            CREATE OR ALTER PROCEDURE [dbo].[EnumZones]
                @DeviceId BIGINT
            AS BEGIN
                SELECT ZoneId, @DeviceId, Zone, Name
                FROM Zones
                WHERE DeviceId = @DeviceId
                ORDER BY Zone
            END
            """,
            """
            CREATE OR ALTER PROCEDURE [dbo].[EnumProgramControls]
                @DeviceId BIGINT
            AS BEGIN
                SELECT ProgramControlId, @DeviceId, ProgramControl, Name
                FROM ProgramControls
                WHERE DeviceId = @DeviceId
                ORDER BY ProgramControl
            END
            """,
            """
            CREATE OR ALTER PROCEDURE [dbo].[EnumExclusions]
                @DeviceId BIGINT
            AS BEGIN
                SELECT ExclusionId, @DeviceId, Exclusion, Name
                FROM Exclusions
                WHERE DeviceId = @DeviceId
                ORDER BY Exclusion
            END
            """,
            """
            CREATE OR ALTER PROCEDURE [dbo].[EnumUserTags]
                @DeviceId BIGINT
            AS BEGIN
                SELECT UserTagId, @DeviceId, UserNumber, UserName
                FROM UserTags
                WHERE DeviceId = @DeviceId
                ORDER BY UserNumber
            END
            """,
            // AccountId = 1 is the orphan/quarantine bucket BlockDevice.sql (and AdminBlockDevice.sql)
            // reassign a device's PIN rows to instead of deleting them -- without this exclusion, a
            // blocked panel's own PIN could never be used to re-link it again, even by its rightful
            // owner, with no way to undo that from the app.
            """
            CREATE OR ALTER PROCEDURE [dbo].[CheckPinUsage]
                @Identifier NVARCHAR(20),
                @PIN NVARCHAR(10)
            AS BEGIN
                DECLARE @IDs AS TABLE(ID INT)
                INSERT INTO @IDs(ID) SELECT deviceId FROM Devices WHERE Identifier = @Identifier AND Enabled = 1

                SELECT COUNT(*) FROM AccountDevicePins WHERE DeviceId IN (SELECT ID FROM @IDs) AND PIN = @PIN AND AccountId <> 1
            END
            """,
            // Re-linking a previously Block-ed panel (same Identifier, still Enabled=1) used to
            // always INSERT a new Devices row, duplicating the physical panel across two DeviceIds
            // and splitting its zones/PGM/events. Reuse the existing row by Identifier instead.
            """
            CREATE OR ALTER PROCEDURE [dbo].[CreateDevice]
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

                -- D.Enabled = 1: a deleted panel used to keep its description reserved forever
                -- while being invisible in the account's own list (client issue #15).
                SELECT @Val = ISNULL(D.DeviceId, 0) FROM Devices D, AccountDevicePins ADP
                    WHERE D.Description = @Description AND ADP.DeviceId = D.DeviceId AND ADP.AccountId = @AccountId AND D.Enabled = 1

                IF ISNULL(@Val, 0) > 0 BEGIN
                    SELECT CAST(-2 AS BIGINT)
                    RETURN 0
                END

                SELECT @CurrentDateTime = GETUTCDATE()

                -- This used to be `SELECT @Result = ISNULL(DeviceId, 0) FROM Devices WHERE ...`,
                -- which looks like it defaults to 0 but does not: when the SELECT matches no rows
                -- the variable is never assigned and keeps its previous value, NULL. ISNULL only
                -- rewrites a NULL column inside a row that was actually returned. `IF @Result = 0`
                -- is then UNKNOWN rather than true, so the device INSERT was skipped and the link
                -- row went in with a NULL DeviceId, surfacing to the user as the raw SQL error
                -- "Cannot insert the value NULL into column 'DeviceId'". That is the client's
                -- "no es posible vincular centrales" from the 14 Oct report.
                --
                -- It bites whenever no ENABLED row exists for the identifier, which is exactly
                -- the state after a panel has been deleted, hence "after unlinking it can never
                -- be linked again".
                SET @Result = NULL
                SELECT TOP 1 @Result = DeviceId FROM Devices
                    WHERE Identifier = @Identifier AND Enabled = 1
                    ORDER BY DeviceId DESC

                IF @Result IS NULL BEGIN
                    -- Revive a retired row for the same panel rather than inserting a second one,
                    -- so a panel keeps one identity across unlink/relink cycles.
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

                        -- Reviving the row re-enables every AccountDevicePins row still attached
                        -- to it, so an account that held a code on this panel before it was
                        -- retired would silently see it again and be able to command it. Live
                        -- data (15 Oct) has one identifier with five disabled Devices rows
                        -- carrying seven link rows across four accounts. DeleteDevice clears
                        -- every link and BlockDevice moves them to quarantine, so once no
                        -- enabled row exists no legitimate link does either; these are residue.
                        DELETE FROM AccountDevicePins WHERE DeviceId = @Result
                    END
                END
                ELSE BEGIN
                    UPDATE Devices SET Description = @Description, UpdatedDateTime = @CurrentDateTime WHERE DeviceId = @Result
                END

                IF @Result IS NULL BEGIN
                    SELECT CAST(-1 AS BIGINT)
                    RETURN 0
                END

                -- Re-linking a panel this account already holds replaces its row instead of
                -- stacking a second one, which would list the same panel twice.
                DELETE FROM AccountDevicePins WHERE AccountId = @AccountId AND DeviceId = @Result
                INSERT INTO AccountDevicePins(AccountId, DeviceId, PIN) VALUES (@AccountId, @Result, @Pin)

                SELECT @Result
            END
            """,
            // The previous single-@DeviceId lookup (no TOP/ORDER BY) only unlinked one arbitrary
            // panel on account deletion -- an account with more than one linked panel left the
            // rest orphaned to a disabled account. Now unlinks every linked device, set-based.
            //
            // Also frees the email address. Deleting stays a soft delete (the row is kept for
            // audit/event history), but Accounts.Email has no unique constraint, so leaving the
            // address sitting on a disabled row meant a later CreateAccount inserted a SECOND row
            // for the same address. From then on GetLoginCredentials resolved the older, disabled
            // row and the address was dead: registration reported "already exists", login failed,
            // and password recovery (filtered on Enabled = 1) never issued a token. Reported by
            // the client as point 14, with prueba@ and diego@alarmastausend.com stuck that way.
            // Tombstoning the address here frees it immediately and keeps the historical row.
            // Admin suspension (SetAccountEnabled) deliberately does NOT tombstone: it leaves
            // DeletedDateTime NULL, so a suspended account keeps its address reserved.
            """
            CREATE OR ALTER PROCEDURE [dbo].[DeleteAccount]
                @AccessToken NVARCHAR(100)
            AS BEGIN
                SET NOCOUNT ON
                DECLARE @AccountId BIGINT
                DECLARE @TokenGuid UNIQUEIDENTIFIER = TRY_CAST(@AccessToken AS UNIQUEIDENTIFIER)

                SELECT @AccountId = AccountId FROM AccessTokens WHERE AccessToken = @TokenGuid
                DELETE FROM AccessTokens WHERE AccountID = @AccountId

                UPDATE Accounts
                SET [Enabled] = 0,
                    DeletedDateTime = GETUTCDATE(),
                    UpdatedDateTime = GETUTCDATE(),
                    Email = LEFT(CONCAT('deleted+', CAST(@AccountId AS NVARCHAR(20)), '+', Email), 255)
                WHERE AccountId = @AccountId AND Email NOT LIKE 'deleted+%'

                DELETE FROM AccountDeviceTokens WHERE AccountId = @AccountId
                DELETE FROM PasswordResetTokens WHERE AccountID = @AccountId
                UPDATE RefreshTokens SET RevokedDateTime = GETUTCDATE()
                    WHERE AccountId = @AccountId AND RevokedDateTime IS NULL

                UPDATE Devices SET Enabled = 0, DeletedDateTime = GETUTCDATE(), UpdatedDateTime = GETUTCDATE()
                    WHERE DeviceId IN (SELECT DeviceId FROM AccountDevicePins WHERE AccountId = @AccountId)

                DELETE FROM AccountDevicePins WHERE AccountId = @AccountId
            END
            """,
            // Same point-14 fault, read side. One address can still map to more than one row on
            // any database that predates the tombstone fix above, and this SELECT had no TOP and
            // no ORDER BY, so AccountDao.ReadLoginCredentials took whichever row came back first
            // (in practice the oldest, which is the deleted one). Enabled rows now win, newest
            // first, which lets an address that was already duplicated log in again.
            """
            CREATE OR ALTER PROCEDURE [dbo].[GetLoginCredentials]
                @Email NVARCHAR(255)
            AS BEGIN
                SET NOCOUNT ON
                SELECT TOP 1 AccountId, PasswordHash, [Password], [Enabled]
                FROM Accounts
                WHERE Email = @Email
                ORDER BY [Enabled] DESC, AccountId DESC
            END
            """,
            // Same determinism fix on the recovery path, so a duplicated address resolves to the
            // live account rather than assigning @AccountId from an arbitrary row.
            """
            CREATE OR ALTER PROCEDURE [dbo].[CreatePasswordResetToken]
                @Email NVARCHAR(255)
            AS BEGIN
                SET NOCOUNT ON
                DECLARE
                    @AccountId BIGINT,
                    @ResetToken UNIQUEIDENTIFIER,
                    @CurrentUTCDateTime DATETIME

                SELECT TOP 1 @AccountId = AccountId FROM Accounts
                    WHERE Email = @Email AND [Enabled] = 1
                    ORDER BY AccountId DESC

                IF ISNULL(@AccountId, 0) = 0 BEGIN
                    SELECT CAST(-1 AS BIGINT) AS AccountId, CAST(NULL AS CHAR(36)) AS ResetToken, CAST(NULL AS NVARCHAR(255)) AS Email, CAST(NULL AS NVARCHAR(100)) AS FirstName
                    RETURN 0
                END

                SELECT @CurrentUTCDateTime = GETUTCDATE()
                SELECT @ResetToken = NEWID()

                INSERT INTO PasswordResetTokens(ResetToken, AccountID, CreatedDateTime, ExpirationDateTime)
                    VALUES (@ResetToken, @AccountId, @CurrentUTCDateTime, DATEADD(HOUR, 1, @CurrentUTCDateTime))

                SELECT @AccountId AS AccountId, CAST(@ResetToken AS CHAR(36)) AS ResetToken, Email, FirstName
                FROM Accounts WHERE AccountId = @AccountId
            END
            """,
            // One-time repair for addresses already bricked before the fix above shipped (the
            // client's prueba@ and diego@alarmastausend.com). Any row that was deleted through
            // the app -- DeletedDateTime stamped, which admin suspension never sets -- and still
            // holds a plain address gets the same tombstone, releasing that address for
            // registration again. The NOT LIKE guard makes re-running this a no-op.
            """
            UPDATE Accounts
            SET Email = LEFT(CONCAT('deleted+', CAST(AccountId AS NVARCHAR(20)), '+', Email), 255),
                UpdatedDateTime = GETUTCDATE()
            WHERE [Enabled] = 0
              AND DeletedDateTime IS NOT NULL
              AND Email NOT LIKE 'deleted+%'
            """,
            // BlockDevice.sql/AdminBlockDevice.sql used to reassign a blocked panel's
            // AccountDevicePins rows to the literal AccountId=1 as an assumed inert "quarantine
            // bucket" -- in production that id belongs to a real, enabled admin account
            // (test@alarmastausend.com), which was silently accumulating every blocked device's
            // PIN link. This seeds one dedicated, permanently-disabled account for that purpose;
            // the three procedures below look it up by email instead of hardcoding an id. Not a
            // CREATE OR ALTER PROCEDURE like the rest of this file, but still idempotent, so it's
            // safe to run on every startup like everything else here.
            """
            IF NOT EXISTS (SELECT 1 FROM Accounts WHERE Email = 'quarantine@system.alarmastausend.internal')
            BEGIN
                INSERT INTO Accounts(Email, FirstName, LastName, PasswordHash, CreatedDateTime, Enabled, UpdatedDateTime, Role)
                VALUES ('quarantine@system.alarmastausend.internal', 'System', 'Quarantine', NULL, GETUTCDATE(), 0, GETUTCDATE(), 0)
            END
            """,
            """
            CREATE OR ALTER PROCEDURE [dbo].[BlockDevice]
                @Identifier NVARCHAR(20),
                @AccountId BIGINT
            AS BEGIN
                DECLARE @DevicesIds AS TABLE(DeviceId INT)
                INSERT INTO @DevicesIds(DeviceId)
                SELECT D.DeviceId FROM Devices D
                INNER JOIN AccountDevicePins ADP ON ADP.DeviceId = D.DeviceId
                WHERE D.Identifier = @Identifier AND D.Enabled = 1 AND ADP.AccountId = @AccountId
                IF((SELECT COUNT(*) FROM @DevicesIds) < 1) BEGIN
                    SELECT -1
                    RETURN 0
                END

                DECLARE @AccountsIds AS TABLE(AccountId INT)
                INSERT INTO @AccountsIds(AccountId) SELECT AccountId FROM AccountDevicePins WHERE DeviceId IN (SELECT DeviceId FROM @DevicesIds)

                DELETE FROM AccountDeviceTokens WHERE AccountId IN (SELECT * FROM @AccountsIds)
                DELETE FROM AccessTokens WHERE AccountID IN (SELECT * FROM @AccountsIds)
                UPDATE AccountDevicePins SET AccountId = (SELECT AccountId FROM Accounts WHERE Email = 'quarantine@system.alarmastausend.internal') WHERE DeviceId IN (SELECT * FROM @DevicesIds)

                SELECT 1
            END
            """,
            """
            CREATE OR ALTER PROCEDURE [dbo].[AdminBlockDevice]
                @DeviceId BIGINT
            AS BEGIN
                SET NOCOUNT ON

                IF NOT EXISTS (SELECT 1 FROM Devices WHERE DeviceId = @DeviceId AND Enabled = 1) BEGIN
                    SELECT CAST(-1 AS BIGINT)
                    RETURN 0
                END

                DECLARE @AccountsIds AS TABLE(AccountId BIGINT)
                INSERT INTO @AccountsIds(AccountId) SELECT AccountId FROM AccountDevicePins WHERE DeviceId = @DeviceId

                DELETE FROM AccountDeviceTokens WHERE AccountId IN (SELECT AccountId FROM @AccountsIds)
                DELETE FROM AccessTokens WHERE AccountID IN (SELECT AccountId FROM @AccountsIds)
                UPDATE AccountDevicePins SET AccountId = (SELECT AccountId FROM Accounts WHERE Email = 'quarantine@system.alarmastausend.internal') WHERE DeviceId = @DeviceId

                SELECT CAST(1 AS BIGINT)
            END
            """,
            """
            CREATE OR ALTER PROCEDURE [dbo].[CheckPinUsage]
                @Identifier NVARCHAR(20),
                @PIN NVARCHAR(10)
            AS BEGIN
                DECLARE @IDs AS TABLE(ID INT)
                INSERT INTO @IDs(ID) SELECT deviceId FROM Devices WHERE Identifier = @Identifier AND Enabled = 1

                SELECT COUNT(*) FROM AccountDevicePins WHERE DeviceId IN (SELECT ID FROM @IDs) AND PIN = @PIN
                    AND AccountId <> (SELECT AccountId FROM Accounts WHERE Email = 'quarantine@system.alarmastausend.internal')
            END
            """,
            // Found live in production: this panel's Identifier has 4+ Enabled=1 Devices rows
            // (duplicate re-pairs predating CreateDevice.sql's reuse fix). The relay's own
            // registration lookup (BackendClient.GetDeviceByIdentifier, no AccountId) hit this
            // ELSE branch's unordered TOP 1 and kept latching onto a stale duplicate instead of
            // the one actually in use, so UpdateDeviceLastConnection kept succeeding (200 OK)
            // while updating the wrong row -- the real cause of a persistent false "Sin conexion"
            // even while the panel was actively registering every few seconds.
            """
            CREATE OR ALTER PROCEDURE [dbo].[GetDeviceByIdentifier]
                @Identifier NVARCHAR(100),
                @AccountId BIGINT
            AS BEGIN
                SET NOCOUNT ON

                DECLARE @DeviceId BIGINT

                IF @AccountId <> 0 BEGIN
                    SELECT @DeviceId = ISNULL(D.DeviceId, 0)
                    FROM Devices D
                    INNER JOIN AccountDevicePins ADP ON ADP.DeviceId = D.DeviceId
                    WHERE Identifier = @Identifier AND Enabled = 1 AND ADP.AccountId = @AccountId
                    END
                ELSE BEGIN
                    SELECT TOP 1 @DeviceId = ISNULL(D.DeviceId, 0) FROM Devices D WHERE Identifier = @Identifier AND Enabled = 1 ORDER BY UpdatedDateTime DESC
                END

                IF ISNULL(@DeviceId, 0) = 0 BEGIN
                    SELECT CAST(-1 AS BIGINT)
                    RETURN 0
                END

                DECLARE @IsOnline BIT,
                    @LastConnection DATETIME

                SELECT @IsOnline = IsOnline, @LastConnection = LastConnection FROM Devices WHERE DeviceId = @DeviceId

                IF @IsOnline = 1 AND ABS(DATEDIFF(HOUR, @LastConnection, GETUTCDATE())) > 1 BEGIN
                    UPDATE DEVICES SET IsOnline = 0 WHERE DeviceId = @DeviceId
                END

                SELECT D.DeviceId, D.Identifier, D.Description, D.IP, D.Port, D.IsOnline, D.LastConnection, D.PublicKey FROM Devices D WHERE DeviceId = @DeviceId

            END
            """,

            // GetDevicePIN and UpdateDevice were both fixed in database/StoredProcedures but
            // neither was ever listed here, and this file is the only thing that reaches the
            // deployed database at boot -- so both fixes would have shipped as dead files.
            //
            // GetDevicePIN: the command path (CommandBusiness.CreateBody) reads the code that
            // gets sent to the panel. Unscoped, the tiebreak used to be PinId ascending, which
            // returned the OLDEST link row for the panel: a stale code from an earlier pairing,
            // or another account's code on a shared central. The panel obeys whatever valid code
            // it is given and logs the event against the user that code belongs to, which is the
            // client's "events show the wrong user" and their "the panel still answers after the
            // user code was changed" -- the code being sent was never the caller's own.
            """
            CREATE OR ALTER PROCEDURE [dbo].[GetDevicePIN]
                @DeviceId BIGINT,
                @AccountId BIGINT = NULL
            AS BEGIN
                SET NOCOUNT ON
                SELECT TOP 1 ISNULL(PIN, '') FROM AccountDevicePins
                WHERE DeviceId = @DeviceId
                  AND (@AccountId IS NULL OR AccountId = @AccountId)
                ORDER BY CASE WHEN AccountId = @AccountId THEN 0 ELSE 1 END, PinId DESC
            END
            """,

            // UpdateDevice: the AccountDevicePins update had no AccountId filter, so one user
            // saving their own code overwrote the stored code of every other account linked to
            // the same panel.
            """
            CREATE OR ALTER PROCEDURE [dbo].[UpdateDevice]
                @DeviceId BIGINT,
                @Description NVARCHAR(255),
                @Identifier NVARCHAR(255),
                @Pin NVARCHAR(15),
                @AccountId BIGINT
            AS BEGIN
                SET NOCOUNT ON

                DECLARE @CurrentDateTime DATETIME,
                    @Val BIGINT

                IF NOT EXISTS (
                    SELECT 1 FROM Devices D
                    INNER JOIN AccountDevicePins ADP ON ADP.DeviceId = D.DeviceId
                    WHERE D.DeviceId = @DeviceId AND D.Enabled = 1 AND ADP.AccountId = @AccountId
                ) BEGIN
                    SELECT CAST(-1 AS BIGINT)
                    RETURN 0
                END

                SELECT @Val = ISNULL(D.DeviceId, 0) FROM Devices D, AccountDevicePins ADP
                WHERE D.Description = @Description AND ADP.DeviceId = D.DeviceId AND D.DeviceId <> @DeviceId AND AccountId = @AccountId

                IF @Val > 0 BEGIN
                    SELECT CAST(-2 AS BIGINT)
                    RETURN 0
                END

                SELECT @CurrentDateTime = GETUTCDATE()

                UPDATE Devices
                SET Description = @Description,
                Identifier = @Identifier,
                UpdatedDateTime = @CurrentDateTime
                WHERE DeviceId = @DeviceId

                UPDATE AccountDevicePins
                SET PIN = @Pin
                WHERE DeviceId = @DeviceId AND AccountId = @AccountId

                SELECT @DeviceId
            END
            """,

            // ResetDevice and AdminResetDevice back "Eliminar para todos" on Gestionar central
            // (client, 20 Oct): a true wipe rather than the soft disable this used to be, since
            // the client's spec calls for the panel's identifier to be deleted from the server.
            // Devices.DeviceId is referenced by AccountDevicePins, ScheduledPgmActions, Zones,
            // UserTags, ProgramControls and Exclusions, so every one of those needs clearing
            // before the Devices row itself can be deleted, or this throws a foreign key
            // violation instead of deleting anything.
            """
            CREATE OR ALTER PROCEDURE [dbo].[ResetDevice]
                @Identifier NVARCHAR(20),
                @AccountId BIGINT
            AS BEGIN
                DECLARE @DevicesIds AS TABLE(DeviceId INT)
                INSERT INTO @DevicesIds(DeviceId)
                SELECT D.DeviceId FROM Devices D
                INNER JOIN AccountDevicePins ADP ON ADP.DeviceId = D.DeviceId
                WHERE D.Identifier = @Identifier AND D.Enabled = 1 AND ADP.AccountId = @AccountId
                IF((SELECT COUNT(*) FROM @DevicesIds) < 1) BEGIN
                    SELECT -1
                    RETURN 0
                END

                DECLARE @AccountsIds AS TABLE(AccountId INT)
                INSERT INTO @AccountsIds(AccountId) SELECT AccountId FROM AccountDevicePins WHERE DeviceId IN (SELECT DeviceId FROM @DevicesIds)

                DELETE FROM AccountDevicePins WHERE DeviceId IN (SELECT * FROM @DevicesIds)
                DELETE FROM ScheduledPgmActions WHERE DeviceId IN (SELECT * FROM @DevicesIds)
                DELETE FROM Zones WHERE DeviceId IN (SELECT * FROM @DevicesIds)
                DELETE FROM UserTags WHERE DeviceId IN (SELECT * FROM @DevicesIds)
                DELETE FROM ProgramControls WHERE DeviceId IN (SELECT * FROM @DevicesIds)
                DELETE FROM Exclusions WHERE DeviceId IN (SELECT * FROM @DevicesIds)
                DELETE FROM Devices WHERE DeviceId IN (SELECT * FROM @DevicesIds)
                DELETE FROM AccountDeviceTokens WHERE AccountId IN (SELECT * FROM @AccountsIds)
                DELETE FROM AccessTokens WHERE AccountID IN (SELECT * FROM @AccountsIds)

                SELECT 1
            END
            """,

            """
            CREATE OR ALTER PROCEDURE [dbo].[AdminResetDevice]
                @DeviceId BIGINT
            AS BEGIN
                SET NOCOUNT ON

                IF NOT EXISTS (SELECT 1 FROM Devices WHERE DeviceId = @DeviceId AND Enabled = 1) BEGIN
                    SELECT CAST(-1 AS BIGINT)
                    RETURN 0
                END

                DECLARE @AccountsIds AS TABLE(AccountId BIGINT)
                INSERT INTO @AccountsIds(AccountId) SELECT AccountId FROM AccountDevicePins WHERE DeviceId = @DeviceId

                DELETE FROM AccountDevicePins WHERE DeviceId = @DeviceId
                DELETE FROM ScheduledPgmActions WHERE DeviceId = @DeviceId
                DELETE FROM Zones WHERE DeviceId = @DeviceId
                DELETE FROM UserTags WHERE DeviceId = @DeviceId
                DELETE FROM ProgramControls WHERE DeviceId = @DeviceId
                DELETE FROM Exclusions WHERE DeviceId = @DeviceId
                DELETE FROM Devices WHERE DeviceId = @DeviceId
                DELETE FROM AccountDeviceTokens WHERE AccountId IN (SELECT AccountId FROM @AccountsIds)
                DELETE FROM AccessTokens WHERE AccountID IN (SELECT AccountId FROM @AccountsIds)

                SELECT CAST(1 AS BIGINT)
            END
            """,
        };
    }
}
