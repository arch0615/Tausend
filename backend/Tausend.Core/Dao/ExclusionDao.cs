using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Tausend.Core.Entities.Models;

namespace Tausend.Core.Dao
{
    public class ExclusionDao : BaseDao
    {
        public long CreateExclusions(List<Exclusion> exclusions)
        {
            dBContext.OpenConnection();
            long res = 0;
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.CreateExclusions");
                FillZoneType(sqlCommand, "Exclusions", exclusions);
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

        public List<Exclusion> EnumExclusions(long deviceId)
        {
            dBContext.OpenConnection();
            var res = new List<Exclusion>();
            try
            {
                var sqlCommand = dBContext.CreateStoredProcedureCommand("dbo.EnumExclusions");
                AddLongParameter(sqlCommand, "DeviceId", deviceId);
                using (var reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var zone = ReadExclusion(reader);
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

        private Exclusion ReadExclusion(SqlDataReader reader)
        {
            var res = new Exclusion();
            res.ExclusionId = ReadLong(reader, 0);
            if (res.ExclusionId > 0)
            {
                res.DeviceId = ReadLong(reader, 1);
                res.ExclusionNumber = ReadInt(reader, 2);
                res.Name = ReadString(reader, 3);
            }
            else
            {
                res = null;
            }
            return res;
        }

        private void FillZoneType(SqlCommand cmd, string parameterName, List<Exclusion> exclusions)
        {
            var tvp = new DataTable();
            tvp.Columns.Add(new DataColumn("DeviceId", typeof(long)));
            tvp.Columns.Add(new DataColumn("Exclusion", typeof(string)));
            tvp.Columns.Add(new DataColumn("Name", typeof(string)));
            foreach (var exclusion in exclusions)
            {
                var row = tvp.NewRow();
                row[0] = exclusion.DeviceId;
                row[1] = exclusion.ExclusionNumber;
                row[2] = exclusion.Name;
                tvp.Rows.Add(row);
            }
            tvp.AcceptChanges();
            SqlParameter tvparam = cmd.Parameters.AddWithValue(parameterName, tvp);
            tvparam.SqlDbType = SqlDbType.Structured;
            tvparam.TypeName = "dbo.ExclusionType";
        }
    }
}