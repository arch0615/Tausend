using Microsoft.Data.SqlClient;
using System.Data;
using TausendBackend.Api.Models;

namespace TausendBackend.Api.Dao
{
    public class ProgramControlDao
    {
        private readonly SqlDbContext _db;

        public ProgramControlDao(SqlDbContext db)
        {
            _db = db;
        }

        public void CreateProgramControls(List<ProgramControl> programControls)
        {
            _db.OpenConnection();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.CreateProgramControl");
                _db.AddTableValuedParameter(sqlCommand, "ProgramControl", "dbo.ProgramControlType", BuildProgramControlTable(programControls));
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

        public List<ProgramControl> EnumProgramControls(long deviceId)
        {
            _db.OpenConnection();
            var res = new List<ProgramControl>();
            try
            {
                var sqlCommand = _db.CreateStoredProcedureCommand("dbo.EnumProgramControls");
                _db.AddLongParameter(sqlCommand, "DeviceId", deviceId);
                using (var reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var programControl = ReadProgramControl(reader);
                        if (programControl != null)
                            res.Add(programControl);
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

        private ProgramControl? ReadProgramControl(SqlDataReader reader)
        {
            var programControlId = reader.IsDBNull(0) ? 0L : reader.GetInt64(0);
            if (programControlId <= 0)
                return null;
            return new ProgramControl
            {
                ProgramControlId = programControlId,
                DeviceId = reader.IsDBNull(1) ? 0L : reader.GetInt64(1),
                ProgramControlNumber = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                Name = reader.IsDBNull(3) ? "" : reader.GetString(3)
            };
        }

        /// <summary>
        /// Builds the dbo.ProgramControlType-shaped DataTable (DeviceId, ProgramControl, Name)
        /// consumed by CreateProgramControl. Column order/types must match
        /// database/Types/ProgramControlType.sql exactly.
        /// </summary>
        private DataTable BuildProgramControlTable(List<ProgramControl> programControls)
        {
            var table = new DataTable();
            table.Columns.Add(new DataColumn("DeviceId", typeof(long)));
            table.Columns.Add(new DataColumn("ProgramControl", typeof(int)));
            table.Columns.Add(new DataColumn("Name", typeof(string)));
            foreach (var programControl in programControls)
            {
                table.Rows.Add(programControl.DeviceId, programControl.ProgramControlNumber, programControl.Name ?? "");
            }
            table.AcceptChanges();
            return table;
        }
    }
}
