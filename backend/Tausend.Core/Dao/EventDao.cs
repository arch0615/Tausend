using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Tausend.Core.Entities.Models;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Tausend.Core.Dao
{
    public class EventDao : BaseDao
    {
        public long CreateEvents(List<Event> events)
        {
            dBContext.OpenConnection();
            long res = 0;
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.CreateEvent");
                FillEventType(sqlCommand, "Event", events);
                res = (long)sqlCommand.ExecuteScalar();
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

        public List<Event> EnumEvents(long deviceId)
        {
            dBContext.OpenConnection();
            var res = new List<Event>();
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.EnumEvents");
                AddLongParameter(sqlCommand, "DeviceId", deviceId);
                using (var reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var alarmEvent = ReadEvent(reader);
                        res.Add(alarmEvent);
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

        private Event ReadEvent(SqlDataReader reader)
        {
            var res = new Event();
            res.EventId = ReadLong(reader, 0);
            if (res.EventId > 0)
            {
                res.EventDateTime = ReadDateTime(reader, 1);
                res.Text = ReadString(reader, 2);
                if (Regex.IsMatch(res.Text, @"\[.*\]: "))
                {
                    string[] splitted = Regex.Split(res.Text, @"\[|\]: ");
                    string year = "20" + Regex.Match(splitted[1], @"\d{2} ").Value;
                    splitted[1] = Regex.Replace(splitted[1], @"\d{2} ", year);
                    res.StringDate = splitted[1].Replace('-', '/');
                    res.Text = splitted[2];
                }
                else
                {
                    res.StringDate = res.EventDateTime.ToString("g", CultureInfo.CreateSpecificCulture("es-ES"));
                }
            }
            else
            {
                res = null;
            }
            return res;
        }

        private void FillEventType(SqlCommand cmd, string parameterName, List<Event> events)
        {
            var tvp = new DataTable();
            tvp.Columns.Add(new DataColumn("Secuence", typeof(int)));
            tvp.Columns.Add(new DataColumn("EventDateTime", typeof(DateTime)));
            tvp.Columns.Add(new DataColumn("EventType", typeof(string)));
            tvp.Columns.Add(new DataColumn("NotificationType", typeof(int)));
            tvp.Columns.Add(new DataColumn("Partition", typeof(int)));
            tvp.Columns.Add(new DataColumn("AlarmParameter", typeof(int)));
            tvp.Columns.Add(new DataColumn("AlarmIdentifier", typeof(string)));
            tvp.Columns.Add(new DataColumn("Text", typeof(string)));
            foreach (var e in events)
            {
                var row = tvp.NewRow();
                row[0] = e.Secuence;
                row[1] = e.EventDateTime;
                row[2] = e.EventType;
                row[3] = e.NotificationType;
                row[4] = e.Partition;
                row[5] = e.AlarmParameter;
                row[6] = e.AlarmIdentifier;
                row[7] = e.Text;
                tvp.Rows.Add(row);
            }
            tvp.AcceptChanges();
            SqlParameter tvparam = cmd.Parameters.AddWithValue(parameterName, tvp);
            tvparam.SqlDbType = SqlDbType.Structured;
            tvparam.TypeName = "dbo.EventType";
        }
    }
}