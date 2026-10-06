using Microsoft.Data.SqlClient;
using System.Data;
using TausendBackend.Api.Models;

namespace TausendBackend.Api.Dao
{
    public class UserDao
    {
        private readonly SqlDbContext _db;

        public UserDao(SqlDbContext db)
        {
            _db = db;
        }

        public void CreateUsers(List<User> users)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.CreateUserTag");
                _db.AddTableValuedParameter(sqlCommand, "Users", "dbo.UserTagType", BuildUserTable(users));
                sqlCommand.ExecuteNonQuery();
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

        public List<User> EnumUsers(long deviceId)
        {
            _db.OpenConnection();
            var res = new List<User>();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.EnumUserTags");
                _db.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                using (var reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var user = ReadUser(reader);
                        if (user != null)
                            res.Add(user);
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

        /// <summary>
        /// Looks up the tag/label an installer assigned to a PIN-holder (user number) on the
        /// panel identified by <paramref name="alarmIdentifier"/>, scoped to the account that
        /// owns it. Returns "" (not null) when there's no matching device/user -- the original
        /// WCF version cast the possibly-DBNull scalar straight to string, which would throw;
        /// this is hardened to return "" instead since NotificationBuilder calls this inline
        /// while composing push text and shouldn't blow up on an unlabeled user.
        /// </summary>
        public string GetUserTag(long accountId, string alarmIdentifier, int user)
        {
            _db.OpenConnection();
            var res = "";
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.GetUserTag");
                _db.AddLongParameter(sqlCommand, "AccountId", accountId);
                _db.AddStringParameter(sqlCommand, "AlarmIdentifier", alarmIdentifier);
                _db.AddIntParameter(sqlCommand, "User", user);
                var scalar = sqlCommand.ExecuteScalar();
                res = scalar as string ?? "";
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

        private User? ReadUser(SqlDataReader reader)
        {
            var userId = reader.IsDBNull(0) ? 0L : reader.GetInt64(0);
            if (userId <= 0)
                return null;
            return new User
            {
                UserId = userId,
                DeviceId = reader.IsDBNull(1) ? 0L : reader.GetInt64(1),
                UserNumber = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                UserName = reader.IsDBNull(3) ? "" : reader.GetString(3)
            };
        }

        /// <summary>
        /// Builds the dbo.UserTagType-shaped DataTable (UserNumber, UserName, DeviceId) consumed
        /// by CreateUserTag. Column order/types must match database/Types/UserTagType.sql exactly.
        /// </summary>
        private DataTable BuildUserTable(List<User> users)
        {
            var table = new DataTable();
            table.Columns.Add(new DataColumn("UserNumber", typeof(int)));
            table.Columns.Add(new DataColumn("UserName", typeof(string)));
            table.Columns.Add(new DataColumn("DeviceId", typeof(long)));
            foreach (var user in users)
            {
                table.Rows.Add(user.UserNumber, user.UserName ?? "", user.DeviceId);
            }
            table.AcceptChanges();
            return table;
        }
    }
}
