using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Tausend.Core.Entities.Models;

namespace Tausend.Core.Dao
{
    public class ProgramControlDao : BaseDao
    {

        public long CreateProgramControls(List<ProgramControl> programControls)
        {
            dBContext.OpenConnection();
            long res = 0;
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.CreateProgramControl");
                FillProgramControlType(sqlCommand, "ProgramControl", programControls);
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

        public List<ProgramControl> EnumProgramControls(long deviceId)
        {
            dBContext.OpenConnection();
            var res = new List<ProgramControl>();
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.EnumProgramControls");
                AddLongParameter(sqlCommand, "DeviceId", deviceId);
                using (var reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var ProgramControl = ReadProgramControl(reader);
                        res.Add(ProgramControl);
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

        private ProgramControl ReadProgramControl(SqlDataReader reader)
        {
            var res = new ProgramControl();
            res.ProgramControlId = ReadLong(reader, 0);
            if (res.ProgramControlId > 0)
            {
                res.DeviceId = ReadLong(reader, 1);
                res.ProgramControlNumber = ReadInt(reader, 2);
                res.Name = ReadString(reader, 3);
            }
            else
            {
                res = null;
            }
            return res;
        }

        private void FillProgramControlType(SqlCommand cmd, string parameterName, List<ProgramControl> ProgramControls)
        {
            var tvp = new DataTable();
            tvp.Columns.Add(new DataColumn("DeviceId", typeof(long)));
            tvp.Columns.Add(new DataColumn("ProgramControl", typeof(string)));
            tvp.Columns.Add(new DataColumn("Name", typeof(string)));
            foreach (var ProgramControl in ProgramControls)
            {
                var row = tvp.NewRow();
                row[0] = ProgramControl.DeviceId;
                row[1] = ProgramControl.ProgramControlNumber;
                row[2] = ProgramControl.Name;
                tvp.Rows.Add(row);
            }
            tvp.AcceptChanges();
            SqlParameter tvparam = cmd.Parameters.AddWithValue(parameterName, tvp);
            tvparam.SqlDbType = SqlDbType.Structured;
            tvparam.TypeName = "dbo.ProgramControlType";
        }
    }
}