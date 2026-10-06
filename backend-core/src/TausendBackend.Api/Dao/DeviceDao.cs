using Microsoft.Data.SqlClient;
using TausendBackend.Api.Models;

namespace TausendBackend.Api.Dao
{
    /// <summary>
    /// Every method that acts on an existing device takes the CALLER's account id and passes it
    /// to the stored procedure, which enforces that the account actually owns the device via
    /// AccountDevicePins (or SMS_Devices.AccountId for SMS devices) -- see
    /// ../backend/DAY5_SUMMARY.md for the security history behind this. accountId = 0 means an
    /// internal/system caller (the relay), which a handful of procedures special-case as
    /// "unscoped" -- see each method's comment.
    /// </summary>
    public class DeviceDao
    {
        private readonly SqlDbContext _db;

        public DeviceDao(SqlDbContext db)
        {
            _db = db;
        }

        public long CreateDevice(string accessToken, string description, string identifier, string pin, ushort publicKey)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.CreateDevice");
                _db.AddStringParameter(sqlCommand, "AccessToken", accessToken);
                _db.AddStringParameter(sqlCommand, "Description", description);
                _db.AddStringParameter(sqlCommand, "Identifier", identifier);
                _db.AddStringParameter(sqlCommand, "Pin", pin);
                _db.AddIntParameter(sqlCommand, "PublicKey", publicKey);
                var res = (long)sqlCommand.ExecuteScalar()!;
                _db.CommitTransaction();
                return res;
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

        public long UpdateDevice(long deviceId, string description, string identifier, string pin, long accountId)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.UpdateDevice");
                _db.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                _db.AddStringParameter(sqlCommand, "Description", description);
                _db.AddStringParameter(sqlCommand, "Identifier", identifier);
                _db.AddStringParameter(sqlCommand, "Pin", pin);
                _db.AddLongParameter(sqlCommand, "AccountId", accountId);
                var res = (long)sqlCommand.ExecuteScalar()!;
                _db.CommitTransaction();
                return res;
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

        public long UpdateDeviceConnectionParameters(long deviceId, string ip, string port)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.UpdateDeviceConnectionParameters");
                _db.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                _db.AddStringParameter(sqlCommand, "IP", ip);
                _db.AddStringParameter(sqlCommand, "Port", port);
                var res = (long)sqlCommand.ExecuteScalar()!;
                _db.CommitTransaction();
                return res;
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

        public long UpdateDeviceLastConnection(long deviceId)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.UpdateDeviceLastConnection");
                _db.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                var res = (long)sqlCommand.ExecuteScalar()!;
                _db.CommitTransaction();
                return res;
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

        /// <summary>accountId = 0 means an internal/system caller (the relay) -- unscoped lookup.</summary>
        public Device? GetDevice(long deviceId, long accountId)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.GetDevice");
                _db.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                _db.AddLongParameter(sqlCommand, "AccountId", accountId);
                Device? device;
                using (var reader = sqlCommand.ExecuteReader())
                {
                    device = GetDeviceFromReader(reader);
                }
                _db.CommitTransaction();
                return device;
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

        /// <summary>accountId = 0 means an internal/system caller (the relay) -- unscoped lookup.</summary>
        public Device? GetDevice(string identifier, long accountId)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.GetDeviceByIdentifier");
                _db.AddStringParameter(sqlCommand, "Identifier", identifier);
                _db.AddLongParameter(sqlCommand, "AccountId", accountId);
                Device? device;
                using (var reader = sqlCommand.ExecuteReader())
                {
                    device = GetDeviceFromReader(reader);
                }
                _db.CommitTransaction();
                return device;
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

        public long DeleteDevice(long deviceId, long accountId)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.DeleteDevice");
                _db.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                _db.AddLongParameter(sqlCommand, "AccountId", accountId);
                var res = (long)sqlCommand.ExecuteScalar()!;
                _db.CommitTransaction();
                return res;
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

        private static Device? GetDeviceFromReader(SqlDataReader reader)
        {
            Device? res = null;
            while (reader.Read())
            {
                var deviceId = reader.GetInt64(0);
                if (deviceId > 0)
                {
                    res = new Device
                    {
                        DeviceId = deviceId,
                        Identifier = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        IP = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Port = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        IsOnline = reader.GetBoolean(5),
                        LastConnection = reader.IsDBNull(6) ? DateTime.MinValue : reader.GetDateTime(6),
                        PublicKey = (ushort)(reader.IsDBNull(7) ? 0 : reader.GetInt32(7))
                    };
                }
                else
                {
                    res = null;
                }
            }
            return res;
        }

        public int ValidatePIN(string pin, string identifier)
        {
            _db.OpenConnection();
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.CheckPinUsage");
                _db.AddStringParameter(sqlcommand, "PIN", pin);
                _db.AddStringParameter(sqlcommand, "Identifier", identifier);
                var res = (int)sqlcommand.ExecuteScalar()!;
                _db.CommitTransaction();
                return res;
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

        public int BlockDevice(string identifier, long accountId)
        {
            _db.OpenConnection();
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.BlockDevice");
                _db.AddStringParameter(sqlcommand, "Identifier", identifier);
                _db.AddLongParameter(sqlcommand, "AccountId", accountId);
                var res = (int)sqlcommand.ExecuteScalar()!;
                _db.CommitTransaction();
                return res;
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

        public int ResetDevice(string identifier, long accountId)
        {
            _db.OpenConnection();
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.ResetDevice");
                _db.AddStringParameter(sqlcommand, "Identifier", identifier);
                _db.AddLongParameter(sqlcommand, "AccountId", accountId);
                var res = (int)sqlcommand.ExecuteScalar()!;
                _db.CommitTransaction();
                return res;
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

        public long CreateDeviceSMS(string accessToken, string identifier, string description, string devicePin, string simPin, string phoneNumber, string deviceType)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.CreateDeviceSMS");
                _db.AddStringParameter(sqlCommand, "AccessToken", accessToken);
                _db.AddStringParameter(sqlCommand, "Description", description);
                _db.AddStringParameter(sqlCommand, "Identifier", identifier);
                _db.AddStringParameter(sqlCommand, "DevicePin", devicePin);
                _db.AddStringParameter(sqlCommand, "SimPin", simPin);
                _db.AddStringParameter(sqlCommand, "PhoneNumber", phoneNumber);
                _db.AddStringParameter(sqlCommand, "DeviceType", deviceType);
                var res = (long)sqlCommand.ExecuteScalar()!;
                _db.CommitTransaction();
                return res;
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

        public long UpdateDeviceSMS(long deviceId, string identifier, string description, string devicePin, string simPin, string phoneNumber, string deviceType, long accountId)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.UpdateDeviceSMS");
                _db.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                _db.AddStringParameter(sqlCommand, "Description", description);
                _db.AddStringParameter(sqlCommand, "Identifier", identifier);
                _db.AddStringParameter(sqlCommand, "DevicePin", devicePin);
                _db.AddStringParameter(sqlCommand, "SimPin", simPin);
                _db.AddStringParameter(sqlCommand, "PhoneNumber", phoneNumber);
                _db.AddStringParameter(sqlCommand, "DeviceType", deviceType);
                _db.AddLongParameter(sqlCommand, "AccountId", accountId);
                var res = (long)sqlCommand.ExecuteScalar()!;
                _db.CommitTransaction();
                return res;
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

        public long DeleteDeviceSMS(long deviceId, long accountId)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.DeleteDeviceSMS");
                _db.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                _db.AddLongParameter(sqlCommand, "AccountId", accountId);
                var res = (long)sqlCommand.ExecuteScalar()!;
                _db.CommitTransaction();
                return res;
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

        public string GetDevicePIN(long deviceId)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.GetDevicePIN");
                _db.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                var pin = (string?)sqlCommand.ExecuteScalar();
                _db.CommitTransaction();
                return pin ?? "";
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

        /// <summary>accountId = 0 means an internal/system caller (the relay), e.g. a panel
        /// factory reset -- unlink every account from the device. Otherwise, only the caller's
        /// own link is removed.</summary>
        public int DisassociateCentral(string identifier, long accountId)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.DisassociateCentral");
                _db.AddStringParameter(sqlCommand, "Identifier", identifier);
                _db.AddLongParameter(sqlCommand, "AccountId", accountId);
                var res = (int)sqlCommand.ExecuteScalar()!;
                _db.CommitTransaction();
                return res;
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
    }
}
