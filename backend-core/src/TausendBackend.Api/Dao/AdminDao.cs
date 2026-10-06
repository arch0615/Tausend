using TausendBackend.Api.Enums;
using TausendBackend.Api.Models;

namespace TausendBackend.Api.Dao
{
    public class AdminDao
    {
        private readonly SqlDbContext _db;

        public AdminDao(SqlDbContext db)
        {
            _db = db;
        }

        public List<AdminAccountSummary> EnumAllAccounts()
        {
            _db.OpenConnection();
            var res = new List<AdminAccountSummary>();
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.EnumAllAccounts");
                using (var reader = sqlcommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        res.Add(new AdminAccountSummary
                        {
                            AccountId = reader.GetInt64(0),
                            Email = reader.GetString(1),
                            FirstName = reader.GetString(2),
                            LastName = reader.GetString(3),
                            Role = (AccountRole)reader.GetByte(4),
                            Enabled = reader.GetBoolean(5),
                            CreatedDateTime = reader.GetDateTime(6),
                            LastLoginDateTime = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7)
                        });
                    }
                }
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        public List<AdminDeviceSummary> EnumAllDevices()
        {
            _db.OpenConnection();
            var res = new List<AdminDeviceSummary>();
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.EnumAllDevices");
                using (var reader = sqlcommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        res.Add(new AdminDeviceSummary
                        {
                            DeviceId = reader.GetInt64(0),
                            Description = reader.GetString(1),
                            Identifier = reader.GetString(2),
                            Enabled = reader.GetBoolean(3),
                            IsOnline = reader.GetBoolean(4),
                            LastConnection = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                            CreatedDateTime = reader.GetDateTime(6)
                        });
                    }
                }
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        public long SetAccountRole(long accountId, AccountRole role)
        {
            _db.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.SetAccountRole");
                _db.AddLongParameter(sqlcommand, "AccountId", accountId);
                sqlcommand.Parameters.AddWithValue("Role", (byte)role);
                res = (long)sqlcommand.ExecuteScalar()!;
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        public long SetAccountEnabled(long accountId, bool enabled)
        {
            _db.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.SetAccountEnabled");
                _db.AddLongParameter(sqlcommand, "AccountId", accountId);
                sqlcommand.Parameters.AddWithValue("Enabled", enabled);
                res = (long)sqlcommand.ExecuteScalar()!;
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        /// <summary>Deletes every AccessToken and revokes every RefreshToken for the account --
        /// see RevokeAccountSessions.sql for why this is needed on top of SetAccountEnabled.</summary>
        public long RevokeAccountSessions(long accountId)
        {
            _db.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.RevokeAccountSessions");
                _db.AddLongParameter(sqlcommand, "AccountId", accountId);
                res = (long)sqlcommand.ExecuteScalar()!;
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        /// <summary>Admin-only equivalent of DisassociateCentral's per-account branch, keyed by
        /// AccountId+DeviceId directly. Returns rows removed (0 if the pair wasn't linked).</summary>
        public long AdminUnlinkAccountDevice(long accountId, long deviceId)
        {
            _db.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.AdminUnlinkAccountDevice");
                _db.AddLongParameter(sqlcommand, "AccountId", accountId);
                _db.AddLongParameter(sqlcommand, "DeviceId", deviceId);
                res = (long)sqlcommand.ExecuteScalar()!;
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        /// <summary>Admin-only, unscoped equivalent of the owner-checked BlockDevice -- caller's
        /// admin status is verified by AdminBusiness before this runs. Returns -1 if not found.</summary>
        public long AdminBlockDevice(long deviceId)
        {
            _db.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.AdminBlockDevice");
                _db.AddLongParameter(sqlcommand, "DeviceId", deviceId);
                res = (long)sqlcommand.ExecuteScalar()!;
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        /// <summary>Admin-only, unscoped equivalent of the owner-checked ResetDevice. Returns -1 if not found.</summary>
        public long AdminResetDevice(long deviceId)
        {
            _db.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.AdminResetDevice");
                _db.AddLongParameter(sqlcommand, "DeviceId", deviceId);
                res = (long)sqlcommand.ExecuteScalar()!;
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        /// <summary>Admin-only, unscoped equivalent of DisassociateCentral's system-caller branch --
        /// unlinks every account from the device. Returns the number of links removed.</summary>
        public long AdminDisassociateDevice(long deviceId)
        {
            _db.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.AdminDisassociateDevice");
                _db.AddLongParameter(sqlcommand, "DeviceId", deviceId);
                res = (long)sqlcommand.ExecuteScalar()!;
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        public List<AccountDeviceLink> EnumAccountDeviceLinks()
        {
            _db.OpenConnection();
            var res = new List<AccountDeviceLink>();
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.EnumAccountDeviceLinks");
                using (var reader = sqlcommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        res.Add(new AccountDeviceLink
                        {
                            AccountId = reader.GetInt64(0),
                            DeviceId = reader.GetInt64(1)
                        });
                    }
                }
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        public long AdminSetDeviceEnabled(long deviceId, bool enabled)
        {
            _db.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.AdminSetDeviceEnabled");
                _db.AddLongParameter(sqlcommand, "DeviceId", deviceId);
                sqlcommand.Parameters.AddWithValue("Enabled", enabled);
                res = (long)sqlcommand.ExecuteScalar()!;
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        public List<Event> EnumAllEvents()
        {
            _db.OpenConnection();
            var res = new List<Event>();
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.EnumAllEvents");
                using (var reader = sqlcommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var eventId = reader.IsDBNull(0) ? 0L : reader.GetInt64(0);
                        if (eventId <= 0) continue;
                        res.Add(new Event
                        {
                            EventId = eventId,
                            EventDateTime = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1),
                            EventType = reader.IsDBNull(2) ? null : reader.GetString(2),
                            NotificationType = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                            Partition = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                            AlarmParameter = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                            AlarmIdentifier = reader.IsDBNull(6) ? null : reader.GetString(6),
                            Text = reader.IsDBNull(7) ? "" : reader.GetString(7)
                        });
                    }
                }
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }

        public void CreateAuditLogEntry(long actorAccountId, string action, string targetType, long? targetId, string? details)
        {
            _db.OpenConnection();
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.CreateAuditLogEntry");
                _db.AddLongParameter(sqlcommand, "ActorAccountId", actorAccountId);
                _db.AddStringParameter(sqlcommand, "Action", action);
                _db.AddStringParameter(sqlcommand, "TargetType", targetType);
                var targetIdParam = sqlcommand.Parameters.AddWithValue("TargetId", (object?)targetId ?? DBNull.Value);
                targetIdParam.SqlDbType = System.Data.SqlDbType.BigInt;
                sqlcommand.Parameters.AddWithValue("Details", (object?)details ?? DBNull.Value);
                sqlcommand.ExecuteNonQuery();
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
        }

        public List<AuditLogEntry> EnumAuditLog()
        {
            _db.OpenConnection();
            var res = new List<AuditLogEntry>();
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.EnumAuditLog");
                using (var reader = sqlcommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        res.Add(new AuditLogEntry
                        {
                            AuditLogId = reader.GetInt64(0),
                            ActorAccountId = reader.GetInt64(1),
                            ActorEmail = reader.IsDBNull(2) ? null : reader.GetString(2),
                            ActorFirstName = reader.IsDBNull(3) ? null : reader.GetString(3),
                            ActorLastName = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Action = reader.GetString(5),
                            TargetType = reader.GetString(6),
                            TargetId = reader.IsDBNull(7) ? (long?)null : reader.GetInt64(7),
                            Details = reader.IsDBNull(8) ? null : reader.GetString(8),
                            CreatedDateTime = reader.GetDateTime(9)
                        });
                    }
                }
                _db.CommitTransaction();
            }
            catch
            {
                _db.RollbackTransaction();
                throw;
            }
            finally
            {
                _db.CloseConnection();
            }
            return res;
        }
    }
}
