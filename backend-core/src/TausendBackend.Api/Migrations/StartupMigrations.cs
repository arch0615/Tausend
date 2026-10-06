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

                SELECT AT.AccountID, Email, FirstName, LastName, CAST(@AccessToken AS CHAR(36)), D.Description, D.Identifier, ADP.PIN, D.DeviceId, A.Role, D.IsOnline
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

                SELECT @AccountID, @Email, FirstName, LastName, CAST(@AccessToken AS CHAR(36)), D.Description, D.Identifier, ADP.PIN, D.DeviceId, A.Role, D.IsOnline
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

                    SELECT @AccountId = ISNULL(AccountID, 0) FROM AccessTokens where AccessToken = @AccessToken

                    IF @AccountId = 0 BEGIN
                        SELECT CAST(-1 AS BIGINT)
                        RETURN 0
                    END

                    SELECT @Val = ISNULL(D.DeviceId, 0) FROM Devices D, AccountDevicePins ADP WHERE D.Description = @Description AND ADP.DeviceId = D.DeviceId AND AccountId = @AccountId

                    IF @Val > 0 BEGIN
                        SELECT CAST(-2 AS BIGINT)
                        RETURN 0
                    END

                    SELECT @CurrentDateTime = GETUTCDATE()

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
            """,
            // The previous single-@DeviceId lookup (no TOP/ORDER BY) only unlinked one arbitrary
            // panel on account deletion -- an account with more than one linked panel left the
            // rest orphaned to a disabled account. Now unlinks every linked device, set-based.
            """
            CREATE OR ALTER PROCEDURE [dbo].[DeleteAccount]
                @AccessToken NVARCHAR(100)
            AS BEGIN
                SET NOCOUNT ON
                DECLARE @AccountId BIGINT
                DECLARE @TokenGuid UNIQUEIDENTIFIER = TRY_CAST(@AccessToken AS UNIQUEIDENTIFIER)

                SELECT @AccountId = AccountId FROM AccessTokens WHERE AccessToken = @TokenGuid
                DELETE FROM AccessTokens WHERE AccountID = @AccountId

                UPDATE Accounts Set Enabled = 0, DeletedDateTime = GETUTCDATE() FROM Accounts WHERE AccountId = @AccountId

                UPDATE Devices SET Enabled = 0, DeletedDateTime = GETUTCDATE(), UpdatedDateTime = GETUTCDATE()
                    WHERE DeviceId IN (SELECT DeviceId FROM AccountDevicePins WHERE AccountId = @AccountId)

                DELETE FROM AccountDevicePins WHERE AccountId = @AccountId
            END
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
        };
    }
}
