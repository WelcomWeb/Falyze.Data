using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Reflection;

namespace Falyze.Data
{
    public class Context
    {

        private static IDictionary<string, PropertyInfo[]> _properties = new Dictionary<string, PropertyInfo[]>();

        private string _connectionString;

        public Context(string connectionString)
        {
            _connectionString = connectionString;
            QueryTimeout = 30;
        }

        private bool _shouldFailOnMissingPropertyField = false;
        public bool ShouldFailOnMissingPropertyField
        {
            get
            {
                return _shouldFailOnMissingPropertyField;
            }
            set
            {
                _shouldFailOnMissingPropertyField = value;
            }
        }

        public int QueryTimeout { get; set; }

        public IEnumerable<T> Get<T>() where T : Entity, new()
        {
            var properties = CheckTypeAccess<T>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = new SqlCommand(string.Format("select * from {0}", GetTableName(typeof(T))), connection))
                {
                    query.CommandTimeout = QueryTimeout;

                    using (var reader = query.ExecuteReader(System.Data.CommandBehavior.Default))
                    {
                        while (reader.Read())
                        {
                            yield return MapEntity<T>(reader, properties);
                        }
                    }
                }
                connection.Close();
            }
        }
        
        public IEnumerable<T> Get<T>(dynamic selectors) where T : Entity, new()
        {
            return Get<T>(selectors, new QueryClause
            {
                Operator = QueryClause.QueryClauseOperator.AND
            });
        }

        public IEnumerable<T> Get<T>(dynamic selectors, QueryClause clause) where T : Entity, new()
        {
            var properties = CheckTypeAccess<T>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    var parms = selectors.GetType().GetProperties();

                    var fields = new Dictionary<string, string>();
                    foreach (var parm in parms)
                    {
                        fields.Add(parm.Name, parm.Name);
                        query.Parameters.AddWithValue(parm.Name, parm.GetValue(selectors));
                    }
                    query.CommandText = string.Format("select * from {0} where {1}", GetTableName(typeof(T)), clause.Parse(fields));

                    using (var reader = query.ExecuteReader(System.Data.CommandBehavior.Default))
                    {
                        while (reader.Read())
                        {
                            yield return MapEntity<T>(reader, properties);
                        }
                    }
                }
                connection.Close();
            }
        }

        public IEnumerable<T> Get<T>(string sql, dynamic selectors) where T : Entity, new()
        {
            var properties = CheckTypeAccess<T>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    var parms = selectors.GetType().GetProperties();

                    foreach (var parm in parms)
                    {
                        query.Parameters.AddWithValue(parm.Name, parm.GetValue(selectors));
                    }
                    query.CommandText = sql;

                    using (var reader = query.ExecuteReader(System.Data.CommandBehavior.Default))
                    {
                        while (reader.Read())
                        {
                            yield return MapEntity<T>(reader, properties);
                        }
                    }
                }
                connection.Close();
            }
        }

        public T Single<T>(dynamic selector) where T : Entity, new()
        {
            return Single<T>(selector, new QueryClause
            {
                Operator = QueryClause.QueryClauseOperator.AND
            });
        }

        public T Single<T>(dynamic selectors, QueryClause clause) where T : Entity, new()
        {
            var properties = CheckTypeAccess<T>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    var parms = selectors.GetType().GetProperties();

                    var fields = new Dictionary<string, string>();
                    foreach (var parm in parms)
                    {
                        fields.Add(parm.Name, parm.Name);
                        query.Parameters.AddWithValue(parm.Name, parm.GetValue(selectors));
                    }
                    query.CommandText = string.Format("select * from {0} where {1}", GetTableName(typeof(T)), clause.Parse(fields));

                    using (var reader = query.ExecuteReader(System.Data.CommandBehavior.SingleRow))
                    {
                        try
                        {
                            reader.Read();
                            return MapEntity<T>(reader, properties);
                        }
                        catch
                        {
                            return null;
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
        }

        public void Create<T>(T entity) where T : Entity, new()
        {
            var properties = CheckTypeAccess<T>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    var fields = new List<string>();
                    var parms = new List<string>();
                    foreach (var property in properties)
                    {
                        var value = property.GetValue(entity);
                        if (value != null)
                        {
                            fields.Add(property.Name);
                            parms.Add(string.Format("@{0}", property.Name));
                            query.Parameters.AddWithValue(property.Name, value);
                        }
                    }
                    query.CommandText = string.Format("insert into {0} ({1}) values({2})", GetTableName(typeof(T)), string.Join(", ", fields), string.Join(", ", parms));
                    query.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public void Update<T>(T entity) where T : Entity, new()
        {
            var properties = CheckTypeAccess<T>();

            var primaryKey = GetPrimaryKey(typeof(T));
            if (primaryKey == null)
            {
                throw new Exception("No primary key specified for '" + (typeof(T).FullName) + "'");
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    var fields = new List<string>();
                    var primaryKeyClause = "";
                    foreach (var property in properties)
                    {
                        var value = property.GetValue(entity);
                        if (value != null)
                        {
                            if (property.Name != primaryKey)
                            {
                                fields.Add(string.Format("{0} = @{1}", property.Name, property.Name));
                            }
                            else
                            {
                                primaryKeyClause = string.Format("{0} = @{1}", property.Name, property.Name);
                            }
                            query.Parameters.AddWithValue(property.Name, value);
                        }
                    }
                    query.CommandText = string.Format("update {0} set {1} where {2}", GetTableName(typeof(T)), string.Join(", ", fields), primaryKeyClause);
                    query.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public void Delete<T>(T entity) where T : Entity, new()
        {
            var properties = CheckTypeAccess<T>();

            var primaryKey = GetPrimaryKey(typeof(T));
            if (primaryKey == null)
            {
                throw new Exception("No primary key specified for '" + (typeof(T).FullName) + "'");
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    var primaryKeyClause = "1 = 0";
                    foreach (var property in properties)
                    {
                        if (property.Name == primaryKey)
                        {
                            primaryKeyClause = string.Format("{0} = @{1}", property.Name, property.Name);
                            query.Parameters.AddWithValue(property.Name, property.GetValue(entity));
                        }
                    }

                    query.CommandText = string.Format("delete from {0} where {1}", GetTableName(typeof(T)), primaryKeyClause);
                    query.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public void Delete<T>(dynamic selectors)
        {
            Delete<T>(selectors, new QueryClause
            {
                Operator = QueryClause.QueryClauseOperator.AND
            });
        }

        public void Delete<T>(dynamic selectors, QueryClause clause)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    var parms = selectors.GetType().GetProperties();

                    var fields = new Dictionary<string, string>();
                    foreach (var parm in parms)
                    {
                        fields.Add(parm.Name, parm.Name);
                        query.Parameters.AddWithValue(parm.Name, parm.GetValue(selectors));
                    }
                    query.CommandText = string.Format("delete from {0} where {1}", GetTableName(typeof(T)), clause.Parse(fields));
                    query.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public void DeleteAll<T>() where T : Entity, new()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;
                    query.CommandText = string.Format("delete from {0}", GetTableName(typeof(T)));
                    query.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public T Select<T>(string sql) where T : new()
        {
            var properties = CheckTypeAccess<T>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;
                    query.CommandText = sql;
                    using (var reader = query.ExecuteReader(System.Data.CommandBehavior.SingleRow))
                    {
                        try
                        {
                            reader.Read();
                            return MapEntity<T>(reader, properties);
                        }
                        catch
                        {
                            return default(T);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
        }

        public T Select<T>(string sql, dynamic parameters) where T : new()
        {
            var properties = CheckTypeAccess<T>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    var parms = parameters.GetType().GetProperties();
                    
                    foreach (var parm in parms)
                    {
                        query.Parameters.AddWithValue(parm.Name, parm.GetValue(parameters));
                    }

                    query.CommandText = sql;
                    using (var reader = query.ExecuteReader(System.Data.CommandBehavior.SingleRow))
                    {
                        try
                        {
                            reader.Read();
                            return MapEntity<T>(reader, properties);
                        }
                        catch
                        {
                            return default(T);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
        }

        public IEnumerable<T> SelectMany<T>(string sql) where T : new()
        {
            var properties = CheckTypeAccess<T>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;
                    query.CommandText = sql;
                    using (var reader = query.ExecuteReader(System.Data.CommandBehavior.Default))
                    {
                        while (reader.Read())
                        {
                            yield return MapEntity<T>(reader, properties);
                        }
                    }
                }
                connection.Close();
            }
        }

        public IEnumerable<T> SelectMany<T>(string sql, dynamic parameters) where T : new()
        {
            var properties = CheckTypeAccess<T>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    var parms = parameters.GetType().GetProperties();

                    foreach (var parm in parms)
                    {
                        query.Parameters.AddWithValue(parm.Name, parm.GetValue(parameters));
                    }

                    query.CommandText = sql;
                    using (var reader = query.ExecuteReader(System.Data.CommandBehavior.Default))
                    {
                        while (reader.Read())
                        {
                            yield return MapEntity<T>(reader, properties);
                        }
                    }
                }
                connection.Close();
            }
        }

        public void Execute(string sql)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;
                    query.CommandText = sql;
                    query.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public void Execute(string sql, dynamic parameters)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    var parms = parameters.GetType().GetProperties();

                    foreach (var parm in parms)
                    {
                        query.Parameters.AddWithValue(parm.Name, parm.GetValue(parameters));
                    }

                    query.CommandText = sql;
                    query.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        public string GetTableName(Type type)
        {
            var attribute = type.GetTypeInfo().GetCustomAttribute(typeof(TableAttribute));
            return attribute != null ? (attribute as TableAttribute).TableName : type.Name;
        }

        public string GetPrimaryKey(Type type)
        {
            var attribute = type.GetTypeInfo().GetCustomAttribute(typeof(PkAttribute));
            return attribute != null ? (attribute as PkAttribute).Field : null;
        }

        public T MapEntity<T>(SqlDataReader reader, PropertyInfo[] properties)
        {
            var entity = Activator.CreateInstance<T>();
            var fieldNames = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName);

            foreach (var property in properties)
            {
                if (_shouldFailOnMissingPropertyField && !fieldNames.Contains(property.Name))
                {
                    var message = string.Format("The property '{0}' does not match any result set field name", property.Name);
                    throw new FalyzePropertyFieldException(message);
                }
                else if (fieldNames.Contains(property.Name))
                {
                    var ordinal = reader.GetOrdinal(property.Name);
                    var value = reader.GetValue(ordinal);
                    property.SetValue(entity, value == DBNull.Value ? null : value);
                }
            }
            return entity;
        }

        private PropertyInfo[] CheckTypeAccess<T>()
        {
            var type = typeof(T);
            if (!_properties.ContainsKey(type.FullName))
            {
                _properties.Add(type.FullName, type.GetProperties());
            }

            return _properties[type.FullName];
        }

        public class QueryClause
        {
            public enum QueryClauseOperator
            {
                AND,
                OR,
                IN
            }

            public QueryClauseOperator Operator { get; set; }

            public string Parse(IDictionary<string, string> values)
            {
                if (Operator == QueryClauseOperator.AND || Operator == QueryClauseOperator.OR)
                {
                    var buffer = new List<string>();
                    foreach (var key in values.Keys)
                    {
                        buffer.Add(string.Format("{0} = @{1}", key, values[key]));
                    }

                    return string.Join(", ", buffer);
                }

                if (Operator == QueryClauseOperator.IN)
                {
                    var buffer = new List<string>();
                    foreach (var key in values.Keys)
                    {
                        buffer.Add(string.Format("{0} in (@{1})", key, values[key]));
                    }

                    return string.Join(", ", buffer);
                }

                return "0 = 1";
            }
        }

    }
}
