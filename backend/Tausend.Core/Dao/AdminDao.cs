using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Tausend.Core.Entities.Models;
using Tausend.Core.Enums;

namespace Tausend.Core.Dao
{
    public class AdminDao : BaseDao
    {
        public List<AdminAccountSummary> EnumAllAccounts()
        {
            dBContext.OpenConnection();
            var res = new List<AdminAccountSummary>();
            try
            {
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.EnumAllAccounts");
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
                            CreatedDateTime = reader.GetDateTime(6)
                        });
                    }
                }
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
            return res;
        }

        public List<AdminDeviceSummary> EnumAllDevices()
        {
            dBContext.OpenConnection();
            var res = new List<AdminDeviceSummary>();
            try
            {
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.EnumAllDevices");
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
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
            return res;
        }

        public long SetAccountRole(long accountId, AccountRole role)
        {
            dBContext.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.SetAccountRole");
                dBContext.AddLongParameter(sqlcommand, "AccountId", accountId);
                sqlcommand.Parameters.AddWithValue("Role", (byte)role);
                res = (long)sqlcommand.ExecuteScalar();
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
            return res;
        }

        /// <summary>Admin-only, unscoped equivalent of the owner-checked BlockDevice -- caller's
        /// admin status is verified by AdminBusiness before this runs. Returns -1 if not found.</summary>
        public long AdminBlockDevice(long deviceId)
        {
            dBContext.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.AdminBlockDevice");
                dBContext.AddLongParameter(sqlcommand, "DeviceId", deviceId);
                res = (long)sqlcommand.ExecuteScalar();
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
            return res;
        }

        /// <summary>Admin-only, unscoped equivalent of the owner-checked ResetDevice. Returns -1 if not found.</summary>
        public long AdminResetDevice(long deviceId)
        {
            dBContext.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.AdminResetDevice");
                dBContext.AddLongParameter(sqlcommand, "DeviceId", deviceId);
                res = (long)sqlcommand.ExecuteScalar();
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
            return res;
        }

        /// <summary>Admin-only, unscoped equivalent of DisassociateCentral's system-caller branch --
        /// unlinks every account from the device. Returns the number of links removed.</summary>
        public long AdminDisassociateDevice(long deviceId)
        {
            dBContext.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.AdminDisassociateDevice");
                dBContext.AddLongParameter(sqlcommand, "DeviceId", deviceId);
                res = (long)sqlcommand.ExecuteScalar();
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
            return res;
        }

        public void CreateAuditLogEntry(long actorAccountId, string action, string targetType, long? targetId, string details)
        {
            dBContext.OpenConnection();
            try
            {
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.CreateAuditLogEntry");
                dBContext.AddLongParameter(sqlcommand, "ActorAccountId", actorAccountId);
                dBContext.AddStringParameter(sqlcommand, "Action", action);
                dBContext.AddStringParameter(sqlcommand, "TargetType", targetType);
                var targetIdParam = sqlcommand.Parameters.AddWithValue("TargetId", targetId.HasValue ? (object)targetId.Value : DBNull.Value);
                targetIdParam.SqlDbType = System.Data.SqlDbType.BigInt;
                sqlcommand.Parameters.AddWithValue("Details", string.IsNullOrEmpty(details) ? (object)DBNull.Value : details);
                sqlcommand.ExecuteNonQuery();
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
        }

        public List<AuditLogEntry> EnumAuditLog()
        {
            dBContext.OpenConnection();
            var res = new List<AuditLogEntry>();
            try
            {
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.EnumAuditLog");
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
                dBContext.CommitTransaction();
            }
            catch (Exception e)
            {
                dBContext.RollbackTransaction();
                throw e;
            }
            finally
            {
                dBContext.CloseConnection();
            }
            return res;
        }
    }
}
