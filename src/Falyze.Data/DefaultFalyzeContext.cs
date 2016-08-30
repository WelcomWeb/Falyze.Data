using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

using Falyze.Data.Entities;
using Falyze.Data.Exceptions;
using Falyze.Data.Property;
using Falyze.Data.TypeInitializers;

namespace Falyze.Data
{
    public class DefaultFalyzeContext : FalyzeContext
    {
        private string _connectionString;

        public DefaultFalyzeContext(string connectionString)
        {
            _connectionString = connectionString;
            DbInitializer = new MsSqlServerInitializer();
            QueryTimeout = 30;
        }

        public DefaultFalyzeContext(FalyzeDbInitializer initializer, string connectionString) : this(connectionString)
        {
            DbInitializer = initializer;
        }

        public FalyzeDbInitializer DbInitializer { get; set; }
        public int QueryTimeout { get; set; }

        public Task<IEnumerable<T>> SelectAsync<T>() where T : Entity, new()
        {
            return SelectAsync<T>(string.Format("select * from {0}", Attributes.GetTableName(typeof(T))));
        }

        public async Task<IEnumerable<T>> SelectAsync<T>(dynamic selectors) where T : Entity, new()
        {
            var properties = PropertyCache.GetProperties<T>();

            using (var connection = DbInitializer.GetConnection(_connectionString))
            {
                var entities = new List<T>();

                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;
                    
                    var parms = selectors.GetType().GetProperties();

                    var fields = new Dictionary<string, string>();
                    foreach (var parm in parms)
                    {
                        fields.Add(parm.Name, parm.Name);
                        query.Parameters.Add(DbInitializer.GetParameter(parm.Name, parm.GetValue(selectors)));
                    }

                    query.CommandText = string.Format("select * from [{0}] where {1}", Attributes.GetTableName(typeof(T)), string.Join(" AND ", fields.Select(kvp => string.Format("[{0}] = @{1}", kvp.Key, kvp.Value))));
                    await connection.OpenAsync();
                    using (var reader = await query.ExecuteReaderAsync(System.Data.CommandBehavior.Default))
                    {
                        while (await reader.ReadAsync())
                        {
                            entities.Add(AsEntity.Map<T>(reader, properties));
                        }
                    }
                }
                
                return entities;
            }
        }

        public async Task<IEnumerable<T>> SelectAsync<T>(string sql, dynamic selectors = null) where T : Entity, new()
        {
            var properties = PropertyCache.GetProperties<T>();

            using (var connection = DbInitializer.GetConnection(_connectionString))
            {
                var entities = new List<T>();

                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    if (selectors != null)
                    {
                        var parms = selectors.GetType().GetProperties();
                        
                        foreach (var parm in parms)
                        {
                            query.Parameters.Add(DbInitializer.GetParameter(parm.Name, parm.GetValue(selectors)));
                        }
                    }

                    query.CommandText = sql;
                    await connection.OpenAsync();
                    using (var reader = await query.ExecuteReaderAsync(System.Data.CommandBehavior.Default))
                    {
                        while (await reader.ReadAsync())
                        {
                            entities.Add(AsEntity.Map<T>(reader, properties));
                        }
                    }
                }
                
                return entities;
            }
        }

        public async Task<T> SingleAsync<T>(dynamic selector) where T : Entity, new()
        {
            var properties = PropertyCache.GetProperties<T>();

            using (var connection = DbInitializer.GetConnection(_connectionString))
            {
                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    var parms = selector.GetType().GetProperties();

                    var fields = new Dictionary<string, string>();
                    foreach (var parm in parms)
                    {
                        fields.Add(parm.Name, parm.Name);
                        query.Parameters.Add(DbInitializer.GetParameter(parm.Name, parm.GetValue(selector)));
                    }

                    query.CommandText = string.Format("select * from [{0}] where {1}", Attributes.GetTableName(typeof(T)), string.Join(" AND ", fields.Select(kvp => string.Format("[{0}] = @{1}", kvp.Key, kvp.Value))));
                    await connection.OpenAsync();
                    using (var reader = await query.ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow))
                    {
                        try
                        {
                            await reader.ReadAsync();
                            return AsEntity.Map<T>(reader, properties);
                        }
                        catch
                        {
                            return default(T);
                        }
                    }
                }
            }
        }

        public async Task<T> SingleAsync<T>(string sql, dynamic selector) where T : Entity, new()
        {
            var properties = PropertyCache.GetProperties<T>();

            using (var connection = DbInitializer.GetConnection(_connectionString))
            {
                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    var parms = selector.GetType().GetProperties();
                    foreach (var parm in parms)
                    {
                        query.Parameters.Add(DbInitializer.GetParameter(parm.Name, parm.GetValue(selector)));
                    }

                    query.CommandText = sql;
                    await connection.OpenAsync();
                    using (var reader = await query.ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow))
                    {
                        try
                        {
                            await reader.ReadAsync();
                            return AsEntity.Map<T>(reader, properties);
                        }
                        catch
                        {
                            return default(T);
                        }
                    }
                }
            }
        }

        public async Task<int> CreateAsync<T>(T entity) where T : Entity, new()
        {
            var properties = PropertyCache.GetProperties<T>();
            var result = -1;

            using (var connection = DbInitializer.GetConnection(_connectionString))
            {
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
                            fields.Add(string.Format("[{0}]", property.Name));
                            parms.Add(string.Format("@{0}", property.Name));
                            query.Parameters.Add(DbInitializer.GetParameter(property.Name, value));
                        }
                    }

                    query.CommandText = string.Format("insert into [{0}] ({1}) values({2})", Attributes.GetTableName(typeof(T)), string.Join(", ", fields), string.Join(", ", parms));

                    await connection.OpenAsync();
                    result = await query.ExecuteNonQueryAsync();
                }
                
                return result;
            }
        }

        public int BatchCreate<T>(IEnumerable<T> entities) where T : Entity, new()
        {
            if (entities.Count() == 0)
            {
                return 0;
            }

            var properties = PropertyCache.GetProperties<T>();
            var result = -1;

            using (var connection = DbInitializer.GetConnection(_connectionString))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();

                using (var query = connection.CreateCommand())
                {
                    query.Transaction = transaction;
                    query.CommandTimeout = QueryTimeout;

                    var fields = new List<string>();
                    var parms = new List<string>();
                    foreach (var property in properties)
                    {
                        fields.Add(string.Format("[{0}]", property.Name));
                        parms.Add(string.Format("@{0}", property.Name));
                    }

                    query.CommandText = string.Format("insert into [{0}] ({1}) values({2})", Attributes.GetTableName(typeof(T)), string.Join(", ", fields), string.Join(", ", parms));

                    foreach (var entity in entities)
                    {
                        foreach (var property in properties)
                        {
                            query.Parameters.Add(DbInitializer.GetParameter(property.Name, property.GetValue(entity)));
                        }
                        result += query.ExecuteNonQuery();
                        query.Parameters.Clear();
                    }

                    try
                    {
                        transaction.Commit();
                    }
                    catch (DbException e)
                    {
                        transaction.Rollback();
                        throw e;
                    }
                }

                return result;
            }
        }

        public async Task<int> UpdateAsync<T>(T entity) where T : Entity, new()
        {
            var properties = PropertyCache.GetProperties<T>();
            var result = -1;

            var primaryKey = Attributes.GetPrimaryKey(typeof(T));
            if (primaryKey == null)
            {
                throw new FalyzePropertyFieldException("No primary key specified for '" + (typeof(T).FullName) + "'");
            }

            using (var connection = DbInitializer.GetConnection(_connectionString))
            {
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
                                fields.Add(string.Format("[{0}] = @{1}", property.Name, property.Name));
                            }
                            else
                            {
                                primaryKeyClause = string.Format("[{0}] = @{1}", property.Name, property.Name);
                            }
                            query.Parameters.Add(DbInitializer.GetParameter(property.Name, value));
                        }
                    }

                    query.CommandText = string.Format("update [{0}] set {1} where {2}", Attributes.GetTableName(typeof(T)), string.Join(", ", fields), primaryKeyClause);
                    await connection.OpenAsync();
                    result = await query.ExecuteNonQueryAsync();
                }
                
                return result;
            }
        }

        public int BatchUpdate<T>(IEnumerable<T> entities) where T : Entity, new()
        {
            if (entities.Count() == 0)
            {
                return 0;
            }

            var properties = PropertyCache.GetProperties<T>();
            var result = -1;

            var primaryKey = Attributes.GetPrimaryKey(typeof(T));
            if (primaryKey == null)
            {
                throw new FalyzePropertyFieldException("No primary key specified for '" + (typeof(T).FullName) + "'");
            }

            using (var connection = DbInitializer.GetConnection(_connectionString))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();

                using (var query = connection.CreateCommand())
                {
                    query.Transaction = transaction;
                    query.CommandTimeout = QueryTimeout;

                    var fields = new List<string>();
                    var primaryKeyClause = "";
                    foreach (var property in properties)
                    {
                        if (property.Name == primaryKey)
                        {
                            primaryKeyClause = string.Format("[{0}] = @{1}", property.Name, property.Name);
                        }
                        else
                        {
                            fields.Add(string.Format("[{0}] = @{1}", property.Name, property.Name));
                        }
                    }

                    query.CommandText = string.Format("update [{0}] set {1} where {2}", Attributes.GetTableName(typeof(T)), string.Join(", ", fields), primaryKeyClause);

                    foreach (var entity in entities)
                    {
                        foreach (var property in properties)
                        {
                            query.Parameters.Add(DbInitializer.GetParameter(property.Name, property.GetValue(entity)));
                        }
                        result += query.ExecuteNonQuery();
                        query.Parameters.Clear();
                    }

                    try
                    {
                        transaction.Commit();
                    }
                    catch (DbException e)
                    {
                        transaction.Rollback();
                        throw e;
                    }
                }

                return result;
            }
        }

        public async Task<int> DeleteAsync<T>(T entity) where T : Entity, new()
        {
            var primaryKey = Attributes.GetPrimaryKey(typeof(T));
            if (primaryKey == null)
            {
                throw new FalyzePropertyFieldException("No primary key specified for '" + (typeof(T).FullName) + "'");
            }

            var property = PropertyCache.GetProperties<T>().FirstOrDefault(p => p.Name == primaryKey);
            if (property == null)
            {
                throw new FalyzePropertyFieldException("The primary key specified for '" + (typeof(T).FullName) + "' does not exist on the entity");
            }

            using (var connection = DbInitializer.GetConnection(_connectionString))
            {
                var result = -1;

                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;
                    query.Parameters.Add(DbInitializer.GetParameter(property.Name, property.GetValue(entity)));

                    query.CommandText = string.Format("delete from [{0}] where [{1}] = @{2}", Attributes.GetTableName(typeof(T)), property.Name, property.Name);
                    await connection.OpenAsync();
                    result = await query.ExecuteNonQueryAsync();
                }
                
                return result;
            }
        }

        public async Task<int> DeleteAsync<T>(string sql, dynamic selectors) where T : Entity, new()
        {
            using (var connection = DbInitializer.GetConnection(_connectionString))
            {
                var result = -1;

                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;
                    var parms = selectors.GetType().GetProperties();
                    
                    foreach (var parm in parms)
                    {
                        query.Parameters.Add(DbInitializer.GetParameter(parm.Name, parm.GetValue(selectors)));
                    }

                    query.CommandText = sql;
                    await connection.OpenAsync();
                    result = await query.ExecuteNonQueryAsync();
                }
                
                return result;
            }
        }

        public async Task<int> DeleteAllAsync<T>() where T : Entity, new()
        {
            using (var connection = DbInitializer.GetConnection(_connectionString))
            {
                var result = -1;

                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;
                    query.CommandText = string.Format("delete from [{0}]", Attributes.GetTableName(typeof(T)));
                    await connection.OpenAsync();
                    result = await query.ExecuteNonQueryAsync();
                }
                
                return result;
            }
        }

        public async Task<int> ExecuteAsync(string sql, dynamic selectors = null)
        {
            using (var connection = DbInitializer.GetConnection(_connectionString))
            {
                var result = -1;

                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    if (selectors != null)
                    {
                        var parms = selectors.GetType().GetProperties();

                        foreach (var parm in parms)
                        {
                            query.Parameters.Add(DbInitializer.GetParameter(parm.Name, parm.GetValue(selectors)));
                        }
                    }

                    query.CommandText = sql;
                    await connection.OpenAsync();
                    result = await query.ExecuteNonQueryAsync();
                }
                
                return result;
            }
        }
    }
}
