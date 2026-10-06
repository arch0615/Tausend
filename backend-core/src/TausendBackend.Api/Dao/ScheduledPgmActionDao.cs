using Microsoft.Data.SqlClient;
using TausendBackend.Api.Models;

namespace TausendBackend.Api.Dao
{
    public class ScheduledPgmActionDao
    {
        private readonly SqlDbContext _db;

        public ScheduledPgmActionDao(SqlDbContext db)
        {
            _db = db;
        }

        public long CreateScheduledPgmAction(long deviceId, int programControlNumber, TimeSpan timeOfDay, byte daysOfWeekMask, bool desiredState)
        {
            _db.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.CreateScheduledPgmAction");
                _db.AddLongParameter(sqlcommand, "DeviceId", deviceId);
                _db.AddIntParameter(sqlcommand, "ProgramControlNumber", programControlNumber);
                sqlcommand.Parameters.AddWithValue("TimeOfDay", timeOfDay);
                sqlcommand.Parameters.AddWithValue("DaysOfWeekMask", daysOfWeekMask);
                sqlcommand.Parameters.AddWithValue("DesiredState", desiredState);
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

        public List<ScheduledPgmAction> EnumScheduledPgmActions(long deviceId)
        {
            _db.OpenConnection();
            var res = new List<ScheduledPgmAction>();
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.EnumScheduledPgmActions");
                _db.AddLongParameter(sqlcommand, "DeviceId", deviceId);
                using (var reader = sqlcommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        res.Add(ReadAction(reader));
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

        public long DeleteScheduledPgmAction(long scheduledPgmActionId, long deviceId)
        {
            _db.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.DeleteScheduledPgmAction");
                _db.AddLongParameter(sqlcommand, "ScheduledPgmActionId", scheduledPgmActionId);
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

        public long SetScheduledPgmActionEnabled(long scheduledPgmActionId, long deviceId, bool enabled)
        {
            _db.OpenConnection();
            long res = 0;
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.SetScheduledPgmActionEnabled");
                _db.AddLongParameter(sqlcommand, "ScheduledPgmActionId", scheduledPgmActionId);
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

        /// <summary>Polled by ScheduledPgmDispatcher roughly once a minute. currentTime should
        /// already be truncated to the minute (seconds zeroed).</summary>
        public List<ScheduledPgmAction> FindDueScheduledPgmActions(TimeSpan currentTime, byte currentDayMask, DateTime today)
        {
            _db.OpenConnection();
            var res = new List<ScheduledPgmAction>();
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.FindDueScheduledPgmActions");
                sqlcommand.Parameters.AddWithValue("CurrentTime", currentTime);
                sqlcommand.Parameters.AddWithValue("CurrentDayMask", currentDayMask);
                sqlcommand.Parameters.AddWithValue("Today", today.Date);
                using (var reader = sqlcommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        res.Add(new ScheduledPgmAction
                        {
                            ScheduledPgmActionId = reader.GetInt64(0),
                            DeviceId = reader.GetInt64(1),
                            ProgramControlNumber = reader.GetInt32(2),
                            DesiredState = reader.GetBoolean(3),
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

        public void MarkScheduledPgmActionFired(long scheduledPgmActionId, DateTime today)
        {
            _db.OpenConnection();
            try
            {
                var sqlcommand = _db.CreateStoredProcedureCommand("dbo.MarkScheduledPgmActionFired");
                _db.AddLongParameter(sqlcommand, "ScheduledPgmActionId", scheduledPgmActionId);
                sqlcommand.Parameters.AddWithValue("Today", today.Date);
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

        private static ScheduledPgmAction ReadAction(SqlDataReader reader) => new ScheduledPgmAction
        {
            ScheduledPgmActionId = reader.GetInt64(0),
            DeviceId = reader.GetInt64(1),
            ProgramControlNumber = reader.GetInt32(2),
            TimeOfDay = reader.GetTimeSpan(3),
            DaysOfWeekMask = reader.GetByte(4),
            DesiredState = reader.GetBoolean(5),
            Enabled = reader.GetBoolean(6),
        };
    }
}
