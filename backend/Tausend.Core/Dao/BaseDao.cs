using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Tausend.Core.Database;

namespace Tausend.Core.Dao
{
    public class BaseDao
    {
        protected DBContext dBContext;

        public BaseDao()
        {
            dBContext = new DBContext();
        }

        /// <summary>
        /// If exists, gets the string from a SqlDataReader in the position.
        /// </summary>
        /// <param name="reader">Reader to read from.</param>
        /// <param name="position">Position to read.</param>
        /// <returns>The string or empty string.</returns>
        protected string ReadString(SqlDataReader reader, int position)
        {
            if (!HasPositionValue(reader, position))
                return "";
            return reader.GetString(position);
        }

        /// <summary>
        /// If exists, gets the string from a SqlDataReader in the position.
        /// </summary>
        /// <param name="reader">Reader to read from.</param>
        /// <param name="position">Position to read.</param>
        /// <returns>The string or empty string.</returns>
        protected DateTime ReadDateTime(SqlDataReader reader, int position)
        {
            if (!HasPositionValue(reader, position))
                return DateTime.MinValue;
            return reader.GetDateTime(position);
        }

        /// <summary>
        /// If exists, gets the long from a SqlDataReader in the position.
        /// </summary>
        /// <param name="reader">Reader to read from.</param>
        /// <param name="position">Position to read.</param>
        /// <returns>The long or 0.</returns>
        protected long ReadLong(SqlDataReader reader, int position)
        {
            if (!HasPositionValue(reader, position))
                return 0L;
            return reader.GetInt64(position);
        }

        /// <summary>
        /// If exists, gets the double from a SqlDataReader in the position.
        /// </summary>
        /// <param name="reader">Reader to read from.</param>
        /// <param name="position">Position to read.</param>
        /// <returns>The double or 0.</returns>
        protected double ReadDouble(SqlDataReader reader, int position)
        {
            if (!HasPositionValue(reader, position))
                return 0F;
            return reader.GetDouble(position);
        }

        protected decimal ReadDecimal(SqlDataReader reader, int position)
        {
            if (!HasPositionValue(reader, position))
                return 0;
            return reader.GetDecimal(position);
        }

        /// <summary>
        /// If exists, gets the float from a SqlDataReader in the position.
        /// </summary>
        /// <param name="reader">Reader to read from.</param>
        /// <param name="position">Position to read.</param>
        /// <returns>The float or 0.</returns>
        protected float ReadFloat(SqlDataReader reader, int position)
        {
            if (!HasPositionValue(reader, position))
                return 0F;
            return reader.GetFloat(position);
        }

        /// <summary>
        /// If exists, gets the int from a SqlDataReader in the position.
        /// </summary>
        /// <param name="reader">Reader to read from.</param>
        /// <param name="position">Position to read.</param>
        /// <returns>The int or 0.</returns>
        protected int ReadInt(SqlDataReader reader, int position)
        {
            if (!HasPositionValue(reader, position))
                return 0;
            return reader.GetInt32(position);
        }

        /// <summary>
        /// If exists, gets the ushort from a SqlDataReader in the position.
        /// </summary>
        /// <param name="reader">Reader to read from.</param>
        /// <param name="position">Position to read.</param>
        /// <returns>The ushort or 0.</returns>
        protected ushort ReadUshort(SqlDataReader reader, int position)
        {
            if (!HasPositionValue(reader, position))
                return 0;
            return (ushort)reader.GetInt32(position);
        }

        /// <summary>
        /// If exists, gets the bool from a SqlDataReader in the position.
        /// </summary>
        /// <param name="reader">Reader to read from.</param>
        /// <param name="position">Position to read.</param>
        /// <returns>The boolean or false.</returns>
        protected bool ReadBool(SqlDataReader reader, int position)
        {
            if (!HasPositionValue(reader, position))
                return false;
            return reader.GetBoolean(position);
        }

        /// <summary>
        /// Validates if the reader has a value in a certain position.
        /// </summary>
        /// <param name="reader">Reader to validate.</param>
        /// <param name="position">Column to validate.</param>
        /// <returns>true if the column exists and has value.</returns>
        protected bool HasPositionValue(SqlDataReader reader, int position)
        {
            return reader.HasRows && reader.FieldCount > position && !reader.IsDBNull(position);
        }

        /// <summary>
        /// Adds a string parameter to a SqlCommand.
        /// </summary>
        /// <param name="command">The command that needs the parameter</param>
        /// <param name="paramName">The name of the parameter in the Stored Procedure</param>
        /// <param name="paramValue">The value of the parameter to pass the Stored Procedure</param>
        public void AddStringParameter(SqlCommand command, string paramName, string paramValue)
        {
            command.Parameters.AddWithValue(paramName, string.IsNullOrEmpty(paramValue) ? "" : paramValue);
        }

        /// <summary>
        /// Adds a string parameter to a SqlCommand.
        /// </summary>
        /// <param name="command">The command that needs the parameter</param>
        /// <param name="paramName">The name of the parameter in the Stored Procedure</param>
        /// <param name="paramValue">The value of the parameter to pass the Stored Procedure</param>
        public void AddLongParameter(SqlCommand command, string paramName, long paramValue)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }

        /// <summary>
        /// Adds a Datetime parameter to a SqlCommand.
        /// </summary>
        /// <param name="command">The command that needs the parameter</param>
        /// <param name="paramName">The name of the parameter in the Stored Procedure</param>
        /// <param name="paramValue">The value of the parameter to pass the Stored Procedure</param>
        public void AddDateTimeParameter(SqlCommand command, string paramName, DateTime paramValue)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }

        /// <summary>
        /// Adds a int parameter to a SqlCommand.
        /// </summary>
        /// <param name="command">The command that needs the parameter</param>
        /// <param name="paramName">The name of the parameter in the Stored Procedure</param>
        /// <param name="paramValue">The value of the parameter to pass the Stored Procedure</param>
        public void AddIntParameter(SqlCommand command, string paramName, int paramValue)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }

        /// <summary>
        /// Adds a bool parameter to a SqlCommand.
        /// </summary>
        /// <param name="command">The command that needs the parameter</param>
        /// <param name="paramName">The name of the parameter in the Stored Procedure</param>
        /// <param name="paramValue">The value of the parameter to pass the Stored Procedure</param>
        public void AddBoolParameter(SqlCommand command, string paramName, bool paramValue)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }

        /// <summary>
        /// Adds a decimal parameter to a SqlCommand.
        /// </summary>
        /// <param name="command">The command that needs the parameter</param>
        /// <param name="paramName">The name of the parameter in the Stored Procedure</param>
        /// <param name="paramValue">The value of the parameter to pass the Stored Procedure</param>
        public void AddDecimalParameter(SqlCommand command, string paramName, decimal paramValue)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }

        /// <summary>
        /// Fills a Datatable as a parameter for a SP with a List of ints.
        /// </summary>
        /// <param name="cmd">SqlCommand that calls the SP.</param>
        /// <param name="parameterName">Parameter Name.</param>
        /// <param name="values">Parameter that receives the SP.</param>
        protected void FillListOfIntdataType(SqlCommand cmd, string parameterName, List<int> values)
        {
            var tvp = new DataTable();
            tvp.Columns.Add(new DataColumn("Value", typeof(int)));
            foreach (var item in values)
                tvp.Rows.Add(item);
            SqlParameter tvparam = cmd.Parameters.AddWithValue(parameterName, tvp);
            tvparam.SqlDbType = SqlDbType.Structured;
            tvparam.TypeName = "dbo.ListOfIntdataType";
        }

        /// <summary>
        /// Fills a Datatable as a parameter for a SP with a List of Bigints.
        /// </summary>
        /// <param name="cmd">SqlCommand that calls the SP.</param>
        /// <param name="parameterName">Parameter Name.</param>
        /// <param name="values">Parameter that receives the SP.</param>
        protected void FillListOfBigintdataType(SqlCommand cmd, string parameterName, List<long> values)
        {
            var tvp = new DataTable();
            tvp.Columns.Add(new DataColumn("Value", typeof(long)));
            foreach (var item in values)
                tvp.Rows.Add(item);
            SqlParameter tvparam = cmd.Parameters.AddWithValue(parameterName, tvp);
            tvparam.SqlDbType = SqlDbType.Structured;
            tvparam.TypeName = "dbo.ListOfBigintdataType";
        }

        /// <summary>
        /// Fills a Datatable as a parameter for a SP with a List of Datetimes.
        /// </summary>
        /// <param name="cmd">SqlCommand that calls the SP.</param>
        /// <param name="parameterName">Parameter Name.</param>
        /// <param name="values">Parameter that receives the SP.</param>
        protected void FillListOfDateTimeType(SqlCommand cmd, string parameterName, List<DateTime> values)
        {
            var tvp = new DataTable();
            tvp.Columns.Add(new DataColumn("Value", typeof(DateTime)));
            foreach (var item in values)
                tvp.Rows.Add(item);
            SqlParameter tvparam = cmd.Parameters.AddWithValue(parameterName, tvp);
            tvparam.SqlDbType = SqlDbType.Structured;
            tvparam.TypeName = "dbo.ListOfDateTimeType";
        }

        /// <summary>
        /// Fills a Datatable as a parameter for a SP with a Dictionary of prices (ServiceId, price).
        /// </summary>
        /// <param name="cmd">SqlCommand that calls the SP.</param>
        /// <param name="parameterName">Parameter Name.</param>
        /// <param name="values">Parameter that receives the SP.</param>
        protected void FillListOfPricesType(SqlCommand cmd, string parameterName, Dictionary<long, decimal> values)
        {
            var tvp = new DataTable();
            tvp.Columns.Add(new DataColumn("Id", typeof(long)));
            tvp.Columns.Add(new DataColumn("Price", typeof(decimal)));
            foreach (var key in values.Keys)
            {
                var row = tvp.NewRow();
                row[0] = key;
                row[1] = values[key];
                tvp.Rows.Add(row);
            }
            SqlParameter tvparam = cmd.Parameters.AddWithValue(parameterName, tvp);
            tvparam.SqlDbType = SqlDbType.Structured;
            tvparam.TypeName = "dbo.ListOfPricesDataType";
        }
    }
}