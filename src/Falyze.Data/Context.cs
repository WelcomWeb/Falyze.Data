using System;
using System.Collections.Generic;
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
        }

        public IEnumerable<T> Get<T>() where T : Entity, new()
        {
            var properties = CheckTypeAccess<T>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = new SqlCommand(string.Format("select * from {0}", GetTableName(typeof(T))), connection))
                {
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

        public IEnumerable<T> Get<T>(dynamic selectors, string expression = "and") where T : Entity, new()
        {
            var properties = CheckTypeAccess<T>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = connection.CreateCommand())
                {
                    var parms = (IDictionary<string, object>)selectors;

                    var fields = new List<string>();
                    foreach (var key in parms.Keys)
                    {
                        fields.Add(string.Format("{0} = {1}", key.Replace("@", ""), key));
                        query.Parameters.AddWithValue(key, parms[key]);
                    }
                    query.CommandText = string.Format("select * from {0} where {1}", GetTableName(typeof(T)), string.Join(" " + expression + " ", fields));

                    using (var reader = query.ExecuteReader(System.Data.CommandBehavior.SingleRow))
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
                    var parms = (IDictionary<string, object>)selectors;

                    foreach (var key in parms.Keys)
                    {
                        query.Parameters.AddWithValue(key, parms[key]);
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

        public T Single<T>(dynamic selectors, string expression = "and") where T : Entity, new()
        {
            var properties = CheckTypeAccess<T>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var query = connection.CreateCommand())
                {
                    var parms = (IDictionary<string, object>)selectors;

                    var fields = new List<string>();
                    foreach (var key in parms.Keys)
                    {
                        fields.Add(string.Format("{0} = {1}", key.Replace("@", ""), key));
                        query.Parameters.AddWithValue(key, parms[key]);
                    }
                    query.CommandText = string.Format("select * from {0} where {1}", GetTableName(typeof(T)), string.Join(" " + expression + " ", fields));

                    using (var reader = query.ExecuteReader(System.Data.CommandBehavior.SingleRow))
                    {
                        try
                        {
                            reader.Read();
                            var entity = MapEntity<T>(reader, properties);

                            connection.Close();
                            return entity;
                        }
                        catch
                        {
                            connection.Close();
                            return null;
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
                    var fields = new List<string>();
                    var parms = new List<string>();
                    foreach (var property in properties)
                    {
                        fields.Add(property.Name);
                        parms.Add(string.Format("@{0}", property.Name));
                        query.Parameters.AddWithValue(property.Name, property.GetValue(entity));
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
                    var fields = new List<string>();
                    var primaryKeyClause = "";
                    foreach (var property in properties)
                    {
                        if (property.Name != primaryKey)
                        {
                            fields.Add(string.Format("{0} = {1}", property.Name, "@" + property.Name));
                        }
                        else
                        {
                            primaryKeyClause = string.Format("{0} = {1}", property.Name, "@" + property.Name);
                        }
                        query.Parameters.AddWithValue(property.Name, property.GetValue(entity));
                    }
                    query.CommandText = string.Format("update {0} set {1} where {2}", GetTableName(typeof(T)), string.Join(", ", fields), primaryKeyClause);
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
            foreach (var property in properties)
            {
                var ordinal = reader.GetOrdinal(property.Name);
                property.SetValue(entity, reader.GetValue(ordinal));
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

    }
}
