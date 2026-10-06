using Microsoft.Data.SqlClient;
using System.Data;
using TausendBackend.Api.Models;

namespace TausendBackend.Api.Dao
{
    public class ZoneDao
    {
        private readonly SqlDbContext _db;

        public ZoneDao(SqlDbContext db)
        {
            _db = db;
        }

        public void CreateZone(List<Zone> zones)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.CreateZone");
                _db.AddTableValuedParameter(sqlCommand, "Zones", "dbo.ZoneType", BuildZoneTable(zones));
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

        public List<Zone> EnumZones(long deviceId)
        {
            _db.OpenConnection();
            var res = new List<Zone>();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.EnumZones");
                _db.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                using (var reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var zone = ReadZone(reader);
                        if (zone != null)
                            res.Add(zone);
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

        private Zone? ReadZone(SqlDataReader reader)
        {
            var zoneId = reader.IsDBNull(0) ? 0L : reader.GetInt64(0);
            if (zoneId <= 0)
                return null;
            return new Zone
            {
                ZoneId = zoneId,
                DeviceId = reader.IsDBNull(1) ? 0L : reader.GetInt64(1),
                ZoneNumber = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                Name = reader.IsDBNull(3) ? "" : reader.GetString(3)
            };
        }

        /// <summary>
        /// Builds the dbo.ZoneType-shaped DataTable (DeviceId, Zone, Name) consumed by CreateZone.
        /// Column order/types must match database/Types/ZoneType.sql exactly.
        /// </summary>
        private DataTable BuildZoneTable(List<Zone> zones)
        {
            var table = new DataTable();
            table.Columns.Add(new DataColumn("DeviceId", typeof(long)));
            table.Columns.Add(new DataColumn("Zone", typeof(int)));
            table.Columns.Add(new DataColumn("Name", typeof(string)));
            foreach (var zone in zones)
            {
                table.Rows.Add(zone.DeviceId, zone.ZoneNumber, zone.Name ?? "");
            }
            table.AcceptChanges();
            return table;
        }
    }
}
