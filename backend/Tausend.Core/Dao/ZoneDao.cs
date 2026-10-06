using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Tausend.Core.Entities.Models;

namespace Tausend.Core.Dao
{
    public class ZoneDao : BaseDao
    {

        public long CreateZone(List<Zone> zones)
        {
            dBContext.OpenConnection();
            long res = 0;
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.CreateZone");
                FillZoneType(sqlCommand, "Zones", zones);
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

        public List<Zone> EnumZones(long deviceId)
        {
            dBContext.OpenConnection();
            var res = new List<Zone>();
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.EnumZones");
                AddLongParameter(sqlCommand, "DeviceId", deviceId);
                using (var reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var zone = ReadZone(reader);
                        res.Add(zone);
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

        private Zone ReadZone(SqlDataReader reader)
        {
            var res = new Zone();
            res.ZoneId = ReadLong(reader, 0);
            if (res.ZoneId > 0)
            {
                res.DeviceId = ReadLong(reader, 1);
                res.ZoneNumber = ReadInt(reader, 2);
                res.Name = ReadString(reader, 3);
            }
            else
            {
                res = null;
            }
            return res;
        }

        private void FillZoneType(SqlCommand cmd, string parameterName, List<Zone> zones)
        {
            var tvp = new DataTable();
            tvp.Columns.Add(new DataColumn("DeviceId", typeof(long)));
            tvp.Columns.Add(new DataColumn("Zone", typeof(string)));
            tvp.Columns.Add(new DataColumn("Name", typeof(string)));
            foreach (var zone in zones)
            {
                var row = tvp.NewRow();
                row[0] = zone.DeviceId;
                row[1] = zone.ZoneNumber;
                row[2] = zone.Name;
                tvp.Rows.Add(row);
            }
            tvp.AcceptChanges();
            SqlParameter tvparam = cmd.Parameters.AddWithValue(parameterName, tvp);
            tvparam.SqlDbType = SqlDbType.Structured;
            tvparam.TypeName = "dbo.ZoneType";
        }
    }
}