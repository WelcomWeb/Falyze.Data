# Falyze.Data

[![Build Status](https://travis-ci.org/WelcomWeb/Falyze.Data.svg?branch=master)](https://travis-ci.org/WelcomWeb/Falyze.Data)

A small, fast and easy-to-use asynchronous entity mapper

## Specify an entity

By inheriting from Falyze.Data.Entity, a POCO can be used as a Falyze Data Entity:

    [Falyze.Data.Table(TableName = "products")]
	[Falyze.Data.Pk(Field = "Id")]
    public class Product : Falyze.Data.Entity
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
	}
	
## Usage

    var context = new FalyzeContext(CONNECTION_STRING);
	var products = await context.SelectAsync<Product>();

## Using something other than MS SQL Server?

Falyze.Data can easily be extended to use any other data source, as long as there are `DbConnection` implementations available for the drivers. Then you just need a `IFalyzeDbInitializer` for your data source type.

Example for MySQL and using MySql.Data.

    using System.Data.Common;
	using Falyze.Data.TypeInitializers;
    using MySql.Data;
	
    namespace MySqlTypeInitializerDemo
    {
        public class MySqlInitializer : IFalyzeDbInitializer
        {
            public DbConnection GetConnection(string connectionString)
            {
                return new MySqlConnection(connectionString);
            }

            public DbCommand GetCommand(string query, DbConnection connection)
            {
                return new MySqlCommand(query, connection as SqlConnection);
            }

            public DbParameter GetParameter(string name, object value)
            {
                return new MySqlParameter(name, value);
            }
        }
    }
	
	...
	
	var context = new FalyzeContext(new MySqlInitializerDemo.MySqlInitializer, CONNECTION_STRING);
	var products = await context.SelectAsync<Product>();
	
	
## API

##### `FalyzeContext.SelectAsync<T>()`

Selects all rows in data source for entity `T`.

---

##### `FalyzeContext.SelectAsync<T>(dynamic selector)`

Select all rows matching the selector;

    var rows = await context.SelectAsync<Product>(new { CategoryId = "21" });

---

##### `FalyzeContext.SelectAsync<T>(string sql[, dynamic selector])`

Specify your own SQL query to select rows from the data source, and optionally add parameters;

    var rows = await context.SelectAsync<Product>("select * from my_table where CategoryId = @CategoryId order by CreatedAt desc", new { CategoryId = "21" });

---

##### `FalyzeContext.SingleAsync<T>(dynamic selector)`

Select only one row from the datasource;

    var product = await context.SingleAsync<Product>(new { Id = "21" });
---

##### `FalyzeContext.CreateAsync<T>(T)`

Create an entity and store it in the data source.

---

##### `FalyzeContext.UpdateAsync<T>(T)`

Update an entity in the data source. The entity needs to have a primary key specified (via the `Pk` attribute).

---

##### `FalyzeContext.DeleteAsync<T>(T)`

Delete an entity in the data source. The entity needs to have a primary key specified (via the `Pk` attribute).

---

##### `FalyzeContext.ExecuteAsync(string sql[, dynamic selector])`

Execute arbritary SQL, with optional parameters;

    await context.ExecuteAsync("update my_table set Active = @Active", new { Active = false });