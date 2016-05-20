# Falyze.Data

[![Build Status](https://travis-ci.org/WelcomWeb/Falyze.Data.svg?branch=master)](https://travis-ci.org/WelcomWeb/Falyze.Data)
[![NuGet version](https://badge.fury.io/nu/Falyze.Data.svg)](https://badge.fury.io/nu/Falyze.Data)

A small, fast and easy-to-use asynchronous entity mapper

## Specify an entity

By inheriting from Falyze.Data.Entities.Entity, a POCO can be used as a Falyze Data Entity:

    [Falyze.Data.Entities.Table(Name = "Products")]
	[Falyze.Data.Entities.PrimaryKey(Field = "Id")]
    public class Product : Falyze.Data.Entities.Entity
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
	}
	
## Usage

    FalyzeContext context = new DefaultFalyzeContext(CONNECTION_STRING);
	var products = await context.SelectAsync<Product>();

## Using something other than MS SQL Server?

Falyze.Data can easily be extended to use any other data source, as long as there are `DbConnection` implementations available for the drivers. Then you just need a `FalyzeDbInitializer` for your data source type.

Example for MySQL and using MySql.Data.

    using System.Data.Common;
	using Falyze.Data.TypeInitializers;
    using MySql.Data;
	
    namespace MySqlTypeInitializerDemo
    {
        public class MySqlInitializer : FalyzeDbInitializer
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
	
	FalyzeContext context = new DefaultFalyzeContext(new MySqlInitializerDemo.MySqlInitializer, CONNECTION_STRING);
	var products = await context.SelectAsync<Product>();

## Breaking changes with v3.0.0

There are some breaking changes if updating from v2.6.0 to v3.0.0, because of a lot of code refactoring:

- `FalyzeContext` is now an interface, and the implementation has moved to `DefaultFalyzeContext`.
- The earlier entity defining abstract class `Falyze.Data.Entity`, is now an interface and is moved into `Falyze.Data.Entities.Entity`.
- Attributes has moved to namespace `Falyze.Data.Entities`.
- The `Table`-attribute has changed it's property `TableName` to be just `Name`.
- The `Pk`-attribute now has a more descriptive name of `PrimaryKey`.
- Exceptions specific to Falyze.Data has moved into `Falyze.Data.Exceptions`.
- The interface `IFalyzeDbInitializer` has been renamed to `FalyzeDbInitializer`.
- The default `FalyzeDbInitializer` has been renamed to `MsSqlServerInitializer`.

---


## FalyzeContext Properties

##### `FalyzeContext.DbInitializer`

Get or set the `FalyzeDbInitializer` to use. Defaults to `Falyze.Data.TypeInitializers.MsSqlServerInitializer`.

##### `FalyzeContext.QueryTimeout`

Get or set the database query timeout. Defaults to 30 seconds.

---
	
## Data Querying Methods

##### `FalyzeContext.SelectAsync<T>()`

Selects all rows in data source for entity `T`.


##### `FalyzeContext.SelectAsync<T>(dynamic selector)`

Select all rows matching the selector;

    var rows = await context.SelectAsync<Product>(new { CategoryId = "21" });


##### `FalyzeContext.SelectAsync<T>(string sql[, dynamic selector])`

Specify your own SQL query to select rows from the data source, and optionally add parameters;

    var rows = await context.SelectAsync<Product>("select * from [Products] where [CategoryId] = @CategoryId order by [CreatedAt] desc", new { CategoryId = "21" });


##### `FalyzeContext.SingleAsync<T>(dynamic selector)`

Select only one row from the datasource;

    var product = await context.SingleAsync<Product>(new { Id = "21" });


##### `FalyzeContext.SingleAsync<T>(string sql, dynamic selector)`

Select only onw row from the datasource, with your own SQL query;

	var product = await context.SingleAsync<Product>("select p.* from [Products] inner join [Categories] as c on c.Id = p.CategoryId where c.Name = @CategoryName and p.Id = @ProductId", new { CategoryName = "Clothes", ProductId = "21" });


##### `FalyzeContext.CreateAsync<T>(T)`

Create an entity and store it in the data source.


##### `FalyzeContext.BatchCreate<T>(IEnumerable<T> entities)` (non-awaitable)

Batch up creation of multiple entities. Falyze.Data uses `DbTransaction` in the background, meaning a rollback will occur if something fails. `DbTransaction` doesn't support async/await - so this method is non-awaitable.


##### `FalyzeContext.UpdateAsync<T>(T)`

Update an entity in the data source. The entity needs to have a primary key specified (via the `PrimaryKey` attribute).


##### `FalyzeContext.BatchUpdate<T>(IEnumerable<T> entities)` (non-awaitable)

Batch up update of multiple entities. The entities needs to have a primary key specified (via the `PrimaryKey` attribute). Falyze.Data uses `DbTransaction` in the background, meaning a rollback will occur if something fails. `DbTransaction` doesn't support async/await - so this method is non-awaitable.


##### `FalyzeContext.DeleteAsync<T>(T)`

Delete an entity in the data source. The entity needs to have a primary key specified (via the `PrimaryKey` attribute).


##### `FalyzeContext.DeleteAsync<T>(string sql, dynamic selectors)`

Delete entities using a custom SQL query, with specified parameters for row matching.


##### `FalyzeContext.DeleteAllAsync<T>()`

Delete all entities of a specified type.


##### `FalyzeContext.ExecuteAsync(string sql[, dynamic selector])`

Execute arbritary SQL, with optional parameters;

    await context.ExecuteAsync("update my_table set Active = @Active", new { Active = false });