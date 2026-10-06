using System.Data;
using Microsoft.Data.SqlClient;

namespace TausendBackend.Api.Dao
{
    /// <summary>
    /// Same open/command/commit/close shape as the old WCF backend's DBContext, so the
    /// ported Dao classes below needed almost no structural changes -- just the
    /// connection string now comes from IConfiguration instead of ConfigurationManager.
    /// </summary>
    public class SqlDbContext : IDisposable
    {
        private readonly string _connectionString;
        private SqlConnection? _connection;
        private SqlTransaction? _transaction;

        public SqlDbContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("TausendConnectionString")
                ?? throw new InvalidOperationException("Missing ConnectionStrings:TausendConnectionString");
        }

        public void OpenConnection()
        {
            if (_connection != null || _transaction != null)
                throw new InvalidOperationException("Can't open connection");
            _connection = new SqlConnection(_connectionString);
            _connection.Open();
            _transaction = _connection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            _transaction?.Commit();
            _transaction = null;
        }

        public void RollbackTransaction()
        {
            _transaction?.Rollback();
            _transaction = null;
        }

        public void CloseConnection()
        {
            if (_connection != null)
            {
                if (_transaction != null)
                    RollbackTransaction();
                _connection.Close();
                _connection = null;
            }
        }

        public void Dispose() => CloseConnection();

        public SqlCommand CreateStoredProcedureCommand(string name)
        {
            if (_connection == null || _transaction == null)
                throw new InvalidOperationException("Can't create StoredProcedure command");
            var command = new SqlCommand(name, _connection, _transaction)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            return command;
        }

        public void AddStringParameter(SqlCommand command, string paramName, string? paramValue)
        {
            command.Parameters.AddWithValue(paramName, string.IsNullOrEmpty(paramValue) ? "" : paramValue);
        }

        public void AddLongParameter(SqlCommand command, string paramName, long paramValue)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }

        public void AddIntParameter(SqlCommand command, string paramName, int paramValue)
        {
            command.Parameters.AddWithValue(paramName, paramValue);
        }

        /// <summary>
        /// Adds a table-valued parameter (SqlDbType.Structured) to a command. Callers build the
        /// DataTable themselves -- shape (columns/order) has to match the target user-defined
        /// table type in dbo (see database/Types/*.sql) exactly. This is the ADO.NET equivalent
        /// of the old WCF backend's per-Dao "FillXxxType" private helpers (e.g. ZoneDao.FillZoneType),
        /// just factored out once so every Dao with a TVP-backed create SP can share it.
        /// </summary>
        /// <param name="command">The command that needs the parameter.</param>
        /// <param name="paramName">The name of the parameter in the Stored Procedure.</param>
        /// <param name="typeName">The fully-qualified SQL user-defined table type name, e.g. "dbo.ZoneType".</param>
        /// <param name="table">The rows to send, already shaped to match the table type's columns.</param>
        public void AddTableValuedParameter(SqlCommand command, string paramName, string typeName, DataTable table)
        {
            var parameter = command.Parameters.AddWithValue(paramName, table);
            parameter.SqlDbType = SqlDbType.Structured;
            parameter.TypeName = typeName;
        }
    }
}
