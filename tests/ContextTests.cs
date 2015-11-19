using System;
using System.Linq;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Falyze.Data;

namespace FalyzeDataWithTests.Tests
{
    [Table(TableName = "FalyzeEntities")]
    [Pk(Field = "Id")]
    public class FalyzeEntity : Entity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    [TestClass]
    public class ContextTests
    {
        private static string CONNECTION_STRING = "Server=.\\SQLEXPRESS;Database=FalyzeDataTest;User Id=developer;Password=d3v3l0p3r;";

        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            using (var connection = new SqlConnection(CONNECTION_STRING))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "create table FalyzeEntities (Id uniqueidentifier not null, Name nvarchar(100) not null, CreatedAt datetime default GETDATE())";
                    command.ExecuteNonQuery();
                }
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "insert into FalyzeEntities (Id, Name) values (NEWID(), 'Default Entity 1'); insert into FalyzeEntities (Id, Name) values (NEWID(), 'Default Entity 2');";
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
        }

        [TestMethod]
        public void TestCanCreateEntity()
        {
            var context = new Context(CONNECTION_STRING);
            var entity = new FalyzeEntity
            {
                Id = Guid.NewGuid(),
                Name = "Falyze Test Entity"
            };
            context.Create<FalyzeEntity>(entity);

            Assert.AreEqual(context.Single<FalyzeEntity>(new { Id = entity.Id }).Name, entity.Name);
        }

        [TestMethod]
        public void TestCanSelectAllEntities()
        {
            var context = new Context(CONNECTION_STRING);
            var entities = context.Get<FalyzeEntity>();

            Assert.IsTrue(entities.Count() > 1);
        }

        [TestMethod]
        public void TestCanSelectSingleEntityWithClause()
        {
            var context = new Context(CONNECTION_STRING);
            var entity = new FalyzeEntity
            {
                Id = Guid.NewGuid(),
                Name = "Falyze Test Entity"
            };
            context.Create<FalyzeEntity>(entity);

            Assert.AreEqual(context.Single<FalyzeEntity>(new { Id = entity.Id, Name = entity.Name }, new Context.QueryClause
            {
                Operator = Context.QueryClause.QueryClauseOperator.AND
            }).Name, entity.Name);
        }

        [TestMethod]
        public void TestCanSelectEntityByQuery()
        {
            var context = new Context(CONNECTION_STRING);
            var entities = context.Get<FalyzeEntity>("select * from FalyzeEntities where Name = @Name", new { Name = "Default Entity 1" });

            Assert.IsTrue(entities.Count() == 1);
        }

        [TestMethod]
        public void TestCanUpdateEntity()
        {
            var context = new Context(CONNECTION_STRING);
            var entity1 = new FalyzeEntity
            {
                Id = Guid.NewGuid(),
                Name = "First Name"
            };
            context.Create<FalyzeEntity>(entity1);

            entity1.Name = "Second Name";
            context.Update<FalyzeEntity>(entity1);

            var entity2 = context.Single<FalyzeEntity>(new { Id = entity1.Id });
            Assert.IsTrue(entity1.Name == "Second Name");
            Assert.AreEqual(entity1.Name, entity2.Name);
        }

        [TestMethod]
        public void TestCanRemoveEntity()
        {
            var context = new Context(CONNECTION_STRING);
            var entity = new FalyzeEntity
            {
                Id = Guid.NewGuid(),
                Name = "Remove me"
            };
            context.Create<FalyzeEntity>(entity);

            Assert.IsTrue(context.Get<FalyzeEntity>(new { Id = entity.Id }).Count() == 1);

            context.Delete<FalyzeEntity>(entity);

            Assert.IsTrue(context.Get<FalyzeEntity>(new { Id = entity.Id }).Count() == 0);
        }

        [TestMethod]
        public void TestCanRemoveAllEntities()
        {
            var context = new Context(CONNECTION_STRING);
            context.DeleteAll<FalyzeEntity>();

            Assert.IsTrue(context.Get<FalyzeEntity>().Count() == 0);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            using (var connection = new SqlConnection(CONNECTION_STRING))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "drop table FalyzeEntities";
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
        }
    }
}
