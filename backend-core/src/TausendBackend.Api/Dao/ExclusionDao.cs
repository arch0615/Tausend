using Microsoft.Data.SqlClient;
using System.Data;
using TausendBackend.Api.Models;

namespace TausendBackend.Api.Dao
{
    public class ExclusionDao
    {
        private readonly SqlDbContext _db;

        public ExclusionDao(SqlDbContext db)
        {
            _db = db;
        }

        public void CreateExclusions(List<Exclusion> exclusions)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.CreateExclusions");
                _db.AddTableValuedParameter(sqlCommand, "Exclusions", "dbo.ExclusionType", BuildExclusionTable(exclusions));
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

        public List<Exclusion> EnumExclusions(long deviceId)
        {
            _db.OpenConnection();
            var res = new List<Exclusion>();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.EnumExclusions");
                _db.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                using (var reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var exclusion = ReadExclusion(reader);
                        if (exclusion != null)
                            res.Add(exclusion);
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

        private Exclusion? ReadExclusion(SqlDataReader reader)
        {
            var exclusionId = reader.IsDBNull(0) ? 0L : reader.GetInt64(0);
            if (exclusionId <= 0)
                return null;
            return new Exclusion
            {
                ExclusionId = exclusionId,
                DeviceId = reader.IsDBNull(1) ? 0L : reader.GetInt64(1),
                ExclusionNumber = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                Name = reader.IsDBNull(3) ? "" : reader.GetString(3)
            };
        }

        /// <summary>
        /// Builds the dbo.ExclusionType-shaped DataTable (DeviceId, Exclusion, Name) consumed by
        /// CreateExclusions. Column order/types must match database/Types/ExclusionType.sql exactly.
        /// </summary>
        private DataTable BuildExclusionTable(List<Exclusion> exclusions)
        {
            var table = new DataTable();
            table.Columns.Add(new DataColumn("DeviceId", typeof(long)));
            table.Columns.Add(new DataColumn("Exclusion", typeof(int)));
            table.Columns.Add(new DataColumn("Name", typeof(string)));
            foreach (var exclusion in exclusions)
            {
                table.Rows.Add(exclusion.DeviceId, exclusion.ExclusionNumber, exclusion.Name ?? "");
            }
            table.AcceptChanges();
            return table;
        }
    }
}
