using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Falyze.Data.TypeInitializers;

namespace Falyze.Data
{
    public class FalyzeContext
    {
        private Entity.Helper _helper = new Entity.Helper();

        private IFalyzeDbInitializer _initializer = new SqlServerInitializer();
        private string _connectionString;

        public int QueryTimeout { get; set; }

        public FalyzeContext(string connectionString)
        {
            _connectionString = connectionString;
            QueryTimeout = 30;
        }

        public FalyzeContext(IFalyzeDbInitializer initializer, string connectionString) : this(connectionString)
        {
            _initializer = initializer;
        }

        public async Task<IEnumerable<T>> SelectAsync<T>() where T : Entity, new()
        {
            return await SelectAsync<T>(string.Format("select * from {0}", _helper.GetTableName(typeof(T))));
        }
        public IEnumerable<T> Select<T>() where T : Entity, new()
        {
            return SelectAsync<T>().Result;
        }

        public async Task<IEnumerable<T>> SelectAsync<T>(dynamic selectors) where T : Entity, new()
        {
            var properties = _helper.CheckTypeAccess<T>();

            using (var connection = _initializer.GetConnection(_connectionString))
            {
                var entities = new List<T>();
                var task = connection.OpenAsync();

                Task.WaitAll(task);

                if (task.IsFaulted)
                {
                    throw new FalyzeConnectionException("A connection to the database could not be established.");
                }

                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;
                    
                    var parms = selectors.GetType().GetProperties();

                    var fields = new Dictionary<string, string>();
                    foreach (var parm in parms)
                    {
                        fields.Add(parm.Name, parm.Name);
                        query.Parameters.Add(_initializer.GetParameter(parm.Name, parm.GetValue(selectors)));
                    }

                    query.CommandText = string.Format("select * from {0} where {1}", _helper.GetTableName(typeof(T)), string.Join(" AND ", fields.Select(kvp => string.Format("{0} = @{1}", kvp.Key, kvp.Value))));
                    using (var reader = await query.ExecuteReaderAsync(System.Data.CommandBehavior.Default))
                    {
                        while (await reader.ReadAsync())
                        {
                            entities.Add(_helper.MapEntity<T>(reader, properties));
                        }
                    }
                }

                connection.Close();
                return entities;
            }
        }

        public IEnumerable<T> Select<T>(dynamic selectors) where T : Entity, new()
        {
            return SelectAsync<T>(selectors).Result;
        }

        public async Task<IEnumerable<T>> SelectAsync<T>(string sql, dynamic selectors = null) where T : Entity, new()
        {
            var properties = _helper.CheckTypeAccess<T>();

            using (var connection = _initializer.GetConnection(_connectionString))
            {
                var entities = new List<T>();
                var task = connection.OpenAsync();

                Task.WaitAll(task);

                if (task.IsFaulted)
                {
                    throw new FalyzeConnectionException("A connection to the database could not be established.");
                }

                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    if (selectors != null)
                    {
                        var parms = selectors.GetType().GetProperties();

                        var fields = new Dictionary<string, string>();
                        foreach (var parm in parms)
                        {
                            fields.Add(parm.Name, parm.Name);
                            query.Parameters.Add(_initializer.GetParameter(parm.Name, parm.GetValue(selectors)));
                        }
                    }

                    query.CommandText = sql;
                    using (var reader = await query.ExecuteReaderAsync(System.Data.CommandBehavior.Default))
                    {
                        while (await reader.ReadAsync())
                        {
                            entities.Add(_helper.MapEntity<T>(reader, properties));
                        }
                    }
                }

                connection.Close();
                return entities;
            }
        }

        public IEnumerable<T> Select<T>(string sql, dynamic selectors = null) where T : Entity, new()
        {
            return SelectAsync<T>(sql, selectors).Result;
        }

        public async Task<T> SingleAsync<T>(dynamic selector) where T : Entity, new()
        {
            var properties = _helper.CheckTypeAccess<T>();

            using (var connection = _initializer.GetConnection(_connectionString))
            {
                var task = connection.OpenAsync();

                Task.WaitAll(task);

                if (task.IsFaulted)
                {
                    throw new FalyzeConnectionException("A connection to the database could not be established.");
                }

                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    var parms = selector.GetType().GetProperties();

                    var fields = new Dictionary<string, string>();
                    foreach (var parm in parms)
                    {
                        fields.Add(parm.Name, parm.Name);
                        query.Parameters.Add(_initializer.GetParameter(parm.Name, parm.GetValue(selector)));
                    }

                    query.CommandText = string.Format("select * from {0} where {1}", _helper.GetTableName(typeof(T)), string.Join(" AND ", fields.Select(kvp => string.Format("{0} = @{1}", kvp.Key, kvp.Value))));
                    using (var reader = await query.ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow))
                    {
                        try
                        {
                            await reader.ReadAsync();
                            return _helper.MapEntity<T>(reader, properties);
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

        public T Single<T>(dynamic selector) where T : Entity, new()
        {
            return SingleAsync<T>(selector).Result;
        }

        public async void Create<T>(T entity) where T : Entity, new()
        {
            var properties = _helper.CheckTypeAccess<T>();

            using (var connection = _initializer.GetConnection(_connectionString))
            {
                var task = connection.OpenAsync();
                
                Task.WaitAll(task);

                if (task.IsFaulted)
                {
                    throw new FalyzeConnectionException("A connection to the database could not be established.");
                }

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
                            query.Parameters.Add(_initializer.GetParameter(property.Name, value));
                        }
                    }

                    query.CommandText = string.Format("insert into {0} ({1}) values({2})", _helper.GetTableName(typeof(T)), string.Join(", ", fields), string.Join(", ", parms));
                    await query.ExecuteNonQueryAsync();
                }

                connection.Close();
            }
        }

        public async void Update<T>(T entity) where T : Entity, new()
        {
            var properties = _helper.CheckTypeAccess<T>();

            var primaryKey = _helper.GetPrimaryKey(typeof(T));
            if (primaryKey == null)
            {
                throw new FalyzePropertyFieldException("No primary key specified for '" + (typeof(T).FullName) + "'");
            }

            using (var connection = _initializer.GetConnection(_connectionString))
            {
                var task = connection.OpenAsync();

                Task.WaitAll(task);

                if (task.IsFaulted)
                {
                    throw new FalyzeConnectionException("A connection to the database could not be established.");
                }

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
                            query.Parameters.Add(_initializer.GetParameter(property.Name, value));
                        }
                    }

                    query.CommandText = string.Format("update {0} set {1} where {2}", _helper.GetTableName(typeof(T)), string.Join(", ", fields), primaryKeyClause);
                    await query.ExecuteNonQueryAsync();
                }

                connection.Close();
            }
        }

        public async void Delete<T>(T entity) where T : Entity, new()
        {
            var primaryKey = _helper.GetPrimaryKey(typeof(T));
            if (primaryKey == null)
            {
                throw new FalyzePropertyFieldException("No primary key specified for '" + (typeof(T).FullName) + "'");
            }

            var property = _helper.CheckTypeAccess<T>().FirstOrDefault(p => p.Name == primaryKey);
            if (property == null)
            {
                throw new FalyzePropertyFieldException("The primary key specified for '" + (typeof(T).FullName) + "' does not exist on the entity");
            }

            var selector = new
            {
                PrimaryKeyValue = property.GetValue(entity)
            };

            await Task.Run(() => Delete<T>(string.Format("delete from {0} where {1} = @PrimaryKeyValue", _helper.GetTableName(typeof(T)), property.Name), selector));
        }

        public async void Delete<T>(string sql, dynamic selectors) where T : Entity, new()
        {
            using (var connection = _initializer.GetConnection(_connectionString))
            {
                var task = connection.OpenAsync();

                Task.WaitAll(task);

                if (task.IsFaulted)
                {
                    throw new FalyzeConnectionException("A connection to the database could not be established.");
                }

                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    var parms = selectors.GetType().GetProperties();

                    var fields = new Dictionary<string, string>();
                    foreach (var parm in parms)
                    {
                        fields.Add(parm.Name, parm.Name);
                        query.Parameters.Add(_initializer.GetParameter(parm.Name, parm.GetValue(selectors)));
                    }

                    query.CommandText = string.Format("delete from {0} where {1}", _helper.GetTableName(typeof(T)), string.Join(" AND ", fields.Select(kvp => string.Format("{0} = @{1}", kvp.Key, kvp.Value))));
                    await query.ExecuteNonQueryAsync();
                }

                connection.Close();
            }
        }

        public async void DeleteAll<T>() where T : Entity, new()
        {
            using (var connection = _initializer.GetConnection(_connectionString))
            {
                var task = connection.OpenAsync();

                Task.WaitAll(task);

                if (task.IsFaulted)
                {
                    throw new FalyzeConnectionException("A connection to the database could not be established.");
                }

                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;
                    query.CommandText = string.Format("delete from {0}", _helper.GetTableName(typeof(T)));
                    await query.ExecuteNonQueryAsync();
                }

                connection.Close();
            }
        }

        public async void Execute(string sql, dynamic selectors = null)
        {
            using (var connection = _initializer.GetConnection(_connectionString))
            {
                var task = connection.OpenAsync();

                Task.WaitAll(task);

                if (task.IsFaulted)
                {
                    throw new FalyzeConnectionException("A connection to the database could not be established.");
                }

                using (var query = connection.CreateCommand())
                {
                    query.CommandTimeout = QueryTimeout;

                    if (selectors != null)
                    {
                        var parms = selectors.GetType().GetProperties();

                        foreach (var parm in parms)
                        {
                            query.Parameters.Add(_initializer.GetParameter(parm.Name, parm.GetValue(selectors)));
                        }
                    }

                    query.CommandText = sql;
                    await query.ExecuteNonQueryAsync();
                }

                connection.Close();
            }
        }
    }
}
