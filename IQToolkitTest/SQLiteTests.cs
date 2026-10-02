using System;
using System.IO;
using System.Linq;
using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using IQToolkit.Data.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IQToolkitTest
{
    [TestClass]
    public class SQLiteTests
    {
        public class Item
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Quantity { get; set; }
        }

        public class Context
        {
            [Table(Name = "Items")]
            [Column(Member = "Id", IsPrimaryKey = true)]
            [Column(Member = "Name")]
            [Column(Member = "Quantity")]
            public IEntityTable<Item> Items { get; set; }
        }

        private static DbEntityProvider CreateProvider()
        {
            var provider = DbEntityProvider.From("IQToolkit.Data.SQLite", "Data Source=:memory:",
                new AttributeMapping(typeof(Context)), QueryPolicy.Default);
            provider.Connection.Open();
            provider.ExecuteCommand("CREATE TABLE Items (Id INTEGER PRIMARY KEY, Name TEXT, Quantity INTEGER NOT NULL)");
            return provider;
        }

        [TestMethod]
        public void InsertQueryUpdateDelete()
        {
            var provider = CreateProvider();
            using (provider.Connection)
            {
                var items = provider.GetTable<Item>();
                var item = new Item { Id = 1, Name = "O'Brien", Quantity = 7 };
                Assert.AreEqual(1, items.Insert(item));
                Assert.AreEqual(1, items.Insert(new Item { Id = 2, Name = null, Quantity = 2 }));
                string name = item.Name;
                Assert.AreEqual(7, items.Where(x => x.Name == name).Select(x => x.Quantity).Single());
                Assert.AreEqual(1, items.Count(x => x.Name == null));
                int[] ids = { 1, 2 };
                Assert.AreEqual(2, items.Count(x => ids.Contains(x.Id)));
                int[] empty = Array.Empty<int>();
                Assert.AreEqual(0, items.Count(x => empty.Contains(x.Id)));
                CollectionAssert.AreEqual(new[] { 1, 2 }, items.OrderByDescending(x => x.Quantity).Select(x => x.Id).ToArray());
                item.Quantity = 12;
                Assert.AreEqual(1, items.Update(item));
                Assert.AreEqual(12, items.Single(x => x.Id == 1).Quantity);
                Assert.AreEqual(1, items.Delete(item));
                Assert.AreEqual(1, items.Count());
            }
        }

        [TestMethod]
        public void TransactionRollback()
        {
            var provider = CreateProvider();
            using (provider.Connection)
            {
                using (var transaction = provider.Connection.BeginTransaction())
                {
                    provider.Transaction = transaction;
                    provider.GetTable<Item>().Insert(new Item { Id = 1, Quantity = 3 });
                    Assert.AreEqual(1, provider.GetTable<Item>().Count());
                    transaction.Rollback();
                }
                provider.Transaction = null;
                Assert.AreEqual(0, provider.GetTable<Item>().Count());
            }
        }

        [TestMethod]
        public void CompiledQueryCanReuseParameters()
        {
            var provider = CreateProvider();
            using (provider.Connection)
            {
                var items = provider.GetTable<Item>();
                items.Insert(new Item { Id = 1, Name = "one", Quantity = 1 });
                items.Insert(new Item { Id = 2, Name = "two", Quantity = 2 });
                var query = QueryCompiler.Compile((int id) => items.Single(x => x.Id == id).Name);
                Assert.AreEqual("one", query(1));
                Assert.AreEqual("two", query(2));
            }
        }

        [TestMethod]
        public void ConnectionStringEscapesPathsAndHonorsFailIfMissing()
        {
            string path = Path.Combine(Path.GetTempPath(), "iqtoolkit;" + Guid.NewGuid() + ".db");
            var builder = new SqliteConnectionStringBuilder(SQLiteQueryProvider.GetConnectionString(path, true));
            Assert.AreEqual(path, builder.DataSource);
            Assert.AreEqual(SqliteOpenMode.ReadWrite, builder.Mode);
            using var connection = new SqliteConnection(builder.ToString());
            Assert.ThrowsExactly<SqliteException>(() => connection.Open());
            Assert.IsFalse(File.Exists(path));
        }
    }
}
