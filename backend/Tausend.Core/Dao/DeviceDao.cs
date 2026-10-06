using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Tausend.Backend.Models;

namespace Tausend.Core.Dao
{
    public class DeviceDao : BaseDao
    {
        public long CreateDevice(string accessToken, string description, string identifier, string pin, ushort publicKey)
        {
            dBContext.OpenConnection();
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.CreateDevice");
                dBContext.AddStringParameter(sqlCommand, "AccessToken", accessToken);
                dBContext.AddStringParameter(sqlCommand, "Description", description);
                dBContext.AddStringParameter(sqlCommand, "Identifier", identifier);
                dBContext.AddStringParameter(sqlCommand, "Pin", pin);
                AddIntParameter(sqlCommand, "PublicKey", publicKey);
                var res = (long)sqlCommand.ExecuteScalar();
                dBContext.CommitTransaction();
                return res;
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

        public long UpdateDevice(long deviceId, string description, string identifier, string pin, long accountId)
        {
            dBContext.OpenConnection();
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.UpdateDevice");
                dBContext.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                dBContext.AddStringParameter(sqlCommand, "Description", description);
                dBContext.AddStringParameter(sqlCommand, "Identifier", identifier);
                dBContext.AddStringParameter(sqlCommand, "Pin", pin);
                dBContext.AddLongParameter(sqlCommand, "AccountId", accountId);
                var res = (long)sqlCommand.ExecuteScalar();
                dBContext.CommitTransaction();
                return res;
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

        public long UpdateDeviceConnectionParameters(long deviceId, string ip, string port)
        {
            dBContext.OpenConnection();
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.UpdateDeviceConnectionParameters");
                dBContext.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                dBContext.AddStringParameter(sqlCommand, "IP", ip);
                dBContext.AddStringParameter(sqlCommand, "Port", port);
                var res = (long)sqlCommand.ExecuteScalar();
                dBContext.CommitTransaction();
                return res;
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

        public Device GetDevice(long deviceId, long accountId)
        {
            dBContext.OpenConnection();
            Device device = null;
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.GetDevice");
                dBContext.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                dBContext.AddLongParameter(sqlCommand, "AccountId", accountId);
                using (var reader = sqlCommand.ExecuteReader())
                {
                    device = GetDeviceFromReader(reader);
                }
                dBContext.CommitTransaction();
                return device;
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

        public Device GetDevice(string identifier, long accountId)
        {
            dBContext.OpenConnection();
            Device device = null;
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.GetDeviceByIdentifier");
                dBContext.AddStringParameter(sqlCommand, "Identifier", identifier);
                dBContext.AddLongParameter(sqlCommand, "AccountId", accountId);
                using (var reader = sqlCommand.ExecuteReader())
                {
                    device = GetDeviceFromReader(reader);
                }
                dBContext.CommitTransaction();
                return device;
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

        public long DeleteDevice(long deviceId, long accountId)
        {
            dBContext.OpenConnection();
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.DeleteDevice");
                dBContext.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                dBContext.AddLongParameter(sqlCommand, "AccountId", accountId);
                var res = (long)sqlCommand.ExecuteScalar();
                dBContext.CommitTransaction();
                return res;
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

        public long UpdateDeviceLastConnection(long deviceId)
        {
            dBContext.OpenConnection();
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.UpdateDeviceLastConnection");
                dBContext.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                var res = (long)sqlCommand.ExecuteScalar();
                dBContext.CommitTransaction();
                return res;
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

        private Device GetDeviceFromReader(SqlDataReader reader)
        {
            var res = new Device();
            while (reader.Read())
            {
                res.DeviceId = reader.GetInt64(0);
                if (res.DeviceId > 0)
                {
                    res.Identifier = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    res.Description = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    res.IP = reader.IsDBNull(3) ? "" : reader.GetString(3);
                    res.Port = reader.IsDBNull(4) ? "" : reader.GetString(4);
                    res.IsOnline = reader.GetBoolean(5);
                    res.LastConnection = reader.IsDBNull(6) ? DateTime.MinValue : reader.GetDateTime(6);
                    res.PublicKey = ReadUshort(reader, 7);
                }
                else
                {
                    res = null;
                }
            }
            return res;
        }

        public int ValidatePIN(string PIN, string identifier)
        {
            int res = -1;
            dBContext.OpenConnection();
            try
            {
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.CheckPinUsage");
                dBContext.AddStringParameter(sqlcommand, "PIN", PIN);
                dBContext.AddStringParameter(sqlcommand, "Identifier", identifier);
                var x = sqlcommand.ExecuteScalar();
                res = (int)x;
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

        public int BlockDevice(string identifier, long accountId)
        {
            int res;
            try
            {
                dBContext.OpenConnection();
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.BlockDevice");
                dBContext.AddStringParameter(sqlcommand, "Identifier", identifier);
                dBContext.AddLongParameter(sqlcommand, "AccountId", accountId);
                var x = sqlcommand.ExecuteScalar();
                res = (int)x;
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

        public int ResetDevice(string identifier, long accountId)
        {
            int res;
            try
            {
                dBContext.OpenConnection();
                var sqlcommand = dBContext.CreateStoredProcedureCommand("dbo.ResetDevice");
                dBContext.AddStringParameter(sqlcommand, "Identifier", identifier);
                dBContext.AddLongParameter(sqlcommand, "AccountId", accountId);
                var x = sqlcommand.ExecuteScalar();
                res = (int)x;
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

        public long CreateDeviceSMS(string accessToken, string identifier, string description, string devicePin, string simPin, string phoneNumber, string deviceType)
        {
            dBContext.OpenConnection();
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.CreateDeviceSMS");
                dBContext.AddStringParameter(sqlCommand, "AccessToken", accessToken);
                dBContext.AddStringParameter(sqlCommand, "Description", description);
                dBContext.AddStringParameter(sqlCommand, "Identifier", identifier);
                dBContext.AddStringParameter(sqlCommand, "DevicePin", devicePin);
                dBContext.AddStringParameter(sqlCommand, "SimPin", simPin);
                dBContext.AddStringParameter(sqlCommand, "PhoneNumber", phoneNumber);
                dBContext.AddStringParameter(sqlCommand, "DeviceType", deviceType);
                var res = (long)sqlCommand.ExecuteScalar();
                dBContext.CommitTransaction();
                return res;
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

        public long UpdateDeviceSMS(long deviceId, string identifier, string description, string devicePin, string simPin, string phoneNumber, string deviceType, long accountId)
        {
            dBContext.OpenConnection();
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.UpdateDeviceSMS");
                dBContext.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                dBContext.AddStringParameter(sqlCommand, "Description", description);
                dBContext.AddStringParameter(sqlCommand, "Identifier", identifier);
                dBContext.AddStringParameter(sqlCommand, "DevicePin", devicePin);
                dBContext.AddStringParameter(sqlCommand, "SimPin", simPin);
                dBContext.AddStringParameter(sqlCommand, "PhoneNumber", phoneNumber);
                dBContext.AddStringParameter(sqlCommand, "DeviceType", deviceType);
                dBContext.AddLongParameter(sqlCommand, "AccountId", accountId);
                var res = (long)sqlCommand.ExecuteScalar();
                dBContext.CommitTransaction();
                return res;
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

        public long DeleteDeviceSMS(long deviceId, long accountId)
        {
            dBContext.OpenConnection();
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.DeleteDeviceSMS");
                dBContext.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                dBContext.AddLongParameter(sqlCommand, "AccountId", accountId);
                var res = (long)sqlCommand.ExecuteScalar();
                dBContext.CommitTransaction();
                return res;
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

        public string GetDevicePIN(long deviceId)
        {
            string pin = "";
            dBContext.OpenConnection();
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.GetDevicePIN");
                dBContext.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                pin = (string)sqlCommand.ExecuteScalar();
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
            return pin;
        }


        public int DisassociateCentral(string identifier, long accountId)
        {
            dBContext.OpenConnection();
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.DisassociateCentral");
                dBContext.AddStringParameter(sqlCommand, "Identifier", identifier);
                dBContext.AddLongParameter(sqlCommand, "AccountId", accountId);
                var res = (int)sqlCommand.ExecuteScalar();
                dBContext.CommitTransaction();
                return res;
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
    }
}