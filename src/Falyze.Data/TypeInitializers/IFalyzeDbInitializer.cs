﻿using System.Data.Common;

namespace Falyze.Data.TypeInitializers
{
    public interface IFalyzeDbInitializer
    {
        DbConnection GetConnection(string connectionString);
        DbCommand GetCommand(string query, DbConnection connection);
        DbParameter GetParameter(string name, object value);
    }
}
