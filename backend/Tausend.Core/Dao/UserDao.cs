using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Tausend.Core.Entities.Models;

namespace Tausend.Core.Dao
{
    public class UserDao : BaseDao
    {
        public long CreateUsers(List<User> users)
        {
            dBContext.OpenConnection();
            long res = 0;
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.CreateUserTag");
                FillUserType(sqlCommand, "Users", users);
                sqlCommand.ExecuteNonQuery();
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

        public List<User> EnumUsers(long deviceId)
        {
            dBContext.OpenConnection();
            var res = new List<User>();
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.EnumUserTags");
                AddLongParameter(sqlCommand, "DeviceId", deviceId);
                using (var reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var user = ReadUser(reader);
                        res.Add(user);
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

        public string GetUserTag(long accountId, string alarmIdentifier, int user)
        {
            dBContext.OpenConnection();
            var res = "";
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.GetUserTag");
                AddLongParameter(sqlCommand, "AccountId", accountId);
                AddStringParameter(sqlCommand, "AlarmIdentifier", alarmIdentifier);
                AddIntParameter(sqlCommand, "User", user);
                res = (string)sqlCommand.ExecuteScalar();
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

        private User ReadUser(SqlDataReader reader)
        {
            var res = new User();
            res.UserId = ReadLong(reader, 0);
            if (res.UserId > 0)
            {
                res.DeviceId = ReadLong(reader, 1);
                res.UserNumber = ReadInt(reader, 2);
                res.UserName = ReadString(reader, 3);
            }
            else
            {
                res = null;
            }
            return res;
        }

        private void FillUserType(SqlCommand cmd, string parameterName, List<User> users)
        {
            var tvp = new DataTable();
            tvp.Columns.Add(new DataColumn("UserNumber", typeof(int)));
            tvp.Columns.Add(new DataColumn("UserName", typeof(string)));
            tvp.Columns.Add(new DataColumn("DeviceId", typeof(long)));
            foreach (var user in users)
            {
                var row = tvp.NewRow();
                row[0] = user.UserNumber;
                row[1] = user.UserName;
                row[2] = user.DeviceId;
                tvp.Rows.Add(row);
            }
            tvp.AcceptChanges();
            SqlParameter tvparam = cmd.Parameters.AddWithValue(parameterName, tvp);
            tvparam.SqlDbType = SqlDbType.Structured;
            tvparam.TypeName = "dbo.UserTagType";
        }
    }
}