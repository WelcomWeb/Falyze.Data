using System.Data.Common;
using System.Data.SqlClient;

namespace Falyze.Data.TypeInitializers
{
    public class MsSqlServerInitializer : FalyzeDbInitializer
    {
        public DbConnection GetConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        public DbCommand GetCommand(string query, DbConnection connection)
        {
            return new SqlCommand(query, connection as SqlConnection);
        }

        public DbParameter GetParameter(string name, object value)
        {
            return new SqlParameter(name, value);
        }
    }
}
