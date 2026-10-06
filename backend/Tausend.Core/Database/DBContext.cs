using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Tausend.Core.Database
{
    public class DBContext : IDisposable
    {
        private SqlConnection connection;
        private SqlTransaction transcation;

        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["TausendConnectionString"].ConnectionString;
        }

        public void OpenConnection()
        {
            if (connection != null || transcation != null)
            {
                throw new Exception("Can't open connection");
            }
            connection = new SqlConnection(GetConnectionString());
            connection.Open();
            transcation = connection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            if(transcation != null)
                transcation.Commit();
            transcation = null;
        }

        public void RollbackTransaction()
        {
            if (transcation != null)
                transcation.Rollback();
            transcation = null;
        }

        public void CloseConnection()
        {
            if (connection != null)
            {
                if (transcation != null)
                    RollbackTransaction();
                connection.Close();
                connection = null;
            }
        }

        public void Dispose()
        {
            CloseConnection();
        }

        public SqlCommand CreateStoredProcedureCommand(string name)
        {
            if (connection == null || transcation == null)
                throw new Exception("Can't create StoredProcedure");
            SqlCommand command = new SqlCommand(name, connection, transcation);
            command.CommandType = CommandType.StoredProcedure;
            return command;
        }

        public void AddStringParameter(SqlCommand command, string paramName, string paramValue)
        {
            command.Parameters.AddWithValue(paramName, string.IsNullOrEmpty(paramValue) ? "" : paramValue);
        }

        public void AddLongParameter(SqlCommand command, string paramName, long paramValue)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }

        public void AddDateTimeParameter(SqlCommand command, string paramName, DateTime paramValue)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }
    }
}