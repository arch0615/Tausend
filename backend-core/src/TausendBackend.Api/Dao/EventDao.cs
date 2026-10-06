using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using TausendBackend.Api.Models;

namespace TausendBackend.Api.Dao
{
    public class EventDao
    {
        private readonly SqlDbContext _db;

        public EventDao(SqlDbContext db)
        {
            _db = db;
        }

        public long CreateEvents(List<Event> events)
        {
            _db.OpenConnection();
            long res = 0;
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.CreateEvent");
                _db.AddTableValuedParameter(sqlCommand, "Event", "dbo.EventType", BuildEventTable(events));
                var scalar = sqlCommand.ExecuteScalar();
                res = scalar is long l ? l : 0;
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

        public List<Event> EnumEvents(long deviceId)
        {
            _db.OpenConnection();
            var res = new List<Event>();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.EnumEvents");
                _db.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                using (var reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var alarmEvent = ReadEvent(reader);
                        if (alarmEvent != null)
                            res.Add(alarmEvent);
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

        /// <summary>Admin-only, fleet-wide equivalent of EnumEvents -- not filtered to one device.
        /// AlarmIdentifier is included so the caller can join back to a device/account client-side
        /// (Events has no DeviceId/AccountId column of its own, only the panel's Identifier).</summary>
        public List<Event> EnumAllEvents()
        {
            _db.OpenConnection();
            var res = new List<Event>();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.EnumAllEvents");
                using (var reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var alarmEvent = ReadEvent(reader);
                        if (alarmEvent != null)
                            res.Add(alarmEvent);
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
        /// EnumEvents/EnumAllEvents both project EventId, EventDateTime, EventType, NotificationType,
        /// Partition, AlarmParameter, AlarmIdentifier, Text in that fixed order (see the matching
        /// .sql files) -- StringDate is derived here, never stored.
        /// Some panel event texts embed their own timestamp as a "[dd-MM-yy]: " prefix (from
        /// store-and-forward SMS relay); when present it's split out into StringDate/Text so the
        /// client shows the panel's own clock instead of the server's insert time.
        /// </summary>
        private Event? ReadEvent(SqlDataReader reader)
        {
            var eventId = reader.IsDBNull(0) ? 0L : reader.GetInt64(0);
            if (eventId <= 0)
                return null;

            var res = new Event
            {
                EventId = eventId,
                EventDateTime = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1),
                EventType = reader.IsDBNull(2) ? null : reader.GetString(2),
                NotificationType = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                Partition = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                AlarmParameter = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                AlarmIdentifier = reader.IsDBNull(6) ? null : reader.GetString(6),
                Text = reader.IsDBNull(7) ? "" : reader.GetString(7)
            };

            if (Regex.IsMatch(res.Text!, @"\[.*\]: "))
            {
                string[] splitted = Regex.Split(res.Text!, @"\[|\]: ");
                string year = "20" + Regex.Match(splitted[1], @"\d{2} ").Value;
                splitted[1] = Regex.Replace(splitted[1], @"\d{2} ", year);
                res.StringDate = splitted[1].Replace('-', '/');
                res.Text = splitted[2];
            }
            else
            {
                res.StringDate = res.EventDateTime.ToString("g", CultureInfo.CreateSpecificCulture("es-ES"));
            }

            return res;
        }

        /// <summary>
        /// Builds the dbo.EventType-shaped DataTable consumed by CreateEvent. Column order/types
        /// must match database/Types/EventType.sql exactly.
        /// </summary>
        private DataTable BuildEventTable(List<Event> events)
        {
            var table = new DataTable();
            table.Columns.Add(new DataColumn("Secuence", typeof(int)));
            table.Columns.Add(new DataColumn("EventDateTime", typeof(DateTime)));
            table.Columns.Add(new DataColumn("EventType", typeof(string)));
            table.Columns.Add(new DataColumn("NotificationType", typeof(int)));
            table.Columns.Add(new DataColumn("Partition", typeof(int)));
            table.Columns.Add(new DataColumn("AlarmParameter", typeof(int)));
            table.Columns.Add(new DataColumn("AlarmIdentifier", typeof(string)));
            table.Columns.Add(new DataColumn("Text", typeof(string)));
            foreach (var e in events)
            {
                table.Rows.Add(
                    e.Secuence,
                    e.EventDateTime,
                    e.EventType ?? "",
                    e.NotificationType,
                    e.Partition,
                    e.AlarmParameter,
                    e.AlarmIdentifier ?? "",
                    e.Text ?? "");
            }
            table.AcceptChanges();
            return table;
        }
    }
}
