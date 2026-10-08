using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.Json;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using IQToolkit.Data.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IQToolkitTest.RealProvider;

[TestClass]
public class BatchCommandOwnershipTests
{
    private const string MetadataQuery =
        "SELECT compile_options FROM pragma_compile_options WHERE compile_options = @name;";

    [TestMethod]
    public void NativeBatchCommandDisposalEventIsObservedBeforeTeardown()
    {
        var source = new NativeBatchSource();
        using var connection = new ObservedConnection();
        connection.Open();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = MetadataQuery;
            command.Parameters.AddWithValue("@name", source.Parameters[0][0]);
            Assert.AreEqual(source.ReaderResults[0], command.ExecuteScalar());
            Assert.IsFalse(connection.DisposedCommands.Contains(command));
        }

        AssertCommandsDisposed(connection);
        Assert.AreEqual(ConnectionState.Open, connection.State);
    }

    [TestMethod]
    public void CompleteStreamedNonqueryBatchDisposesCommandAndPreservesBorrowedConnection()
        => VerifyNonquery(startOpen: true, stopEarly: false);

    [TestMethod]
    public void EarlyStreamedNonqueryBatchDisposalPreservesBorrowedConnection()
        => VerifyNonquery(startOpen: true, stopEarly: true);

    [TestMethod]
    public void CompleteStreamedReaderBatchDisposesCommandAndPreservesBorrowedConnection()
        => VerifyReader(startOpen: true, stopEarly: false);

    [TestMethod]
    public void EarlyStreamedReaderBatchDisposalReleasesReaderAndCommand()
        => VerifyReader(startOpen: true, stopEarly: true);

    [TestMethod]
    public void InitiallyClosedNonqueryBatchBuffersAndReleasesOwnedConnection()
        => VerifyNonquery(startOpen: false, stopEarly: false);

    [TestMethod]
    public void InitiallyClosedReaderBatchBuffersAndReleasesOwnedConnection()
        => VerifyReader(startOpen: false, stopEarly: false);

    private static void VerifyNonquery(bool startOpen, bool stopEarly)
    {
        var source = new NativeBatchSource();
        using var connection = new ObservedConnection();
        if (startOpen)
        {
            connection.Open();
        }
        var executor = CreateExecutor(connection);
        var results = executor.ExecuteBatch(source.CreateQuery(executor.Provider), source.Parameters,
            batchSize: 50, stream: true);

        Assert.AreEqual(startOpen ? 0 : 1, connection.CreatedCommands.Count);
        Assert.AreEqual(0, executor.Readers.Count);
        if (!startOpen)
        {
            // This path already executed/buffered before returning its list.
            Assert.AreEqual(ConnectionState.Closed, connection.State);
            AssertCommandsDisposed(connection);
        }

        if (stopEarly)
        {
            using (var enumerator = results.GetEnumerator())
            {
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(source.NonqueryResults[0], enumerator.Current);
                Assert.AreEqual(ConnectionState.Open, connection.State);
                Assert.AreEqual(0, connection.DisposedCommands.Count);
            }
        }
        else
        {
            CollectionAssert.AreEqual(source.NonqueryResults.ToArray(), results.ToArray());
        }

        Assert.AreEqual(startOpen ? ConnectionState.Open : ConnectionState.Closed, connection.State);
        AssertCommandsDisposed(connection);
    }

    private static void VerifyReader(bool startOpen, bool stopEarly)
    {
        var source = new NativeBatchSource();
        using var connection = new ObservedConnection();
        if (startOpen)
        {
            connection.Open();
        }
        var executor = CreateExecutor(connection);
        var results = executor.ExecuteBatch(source.CreateQuery(executor.Provider), source.Parameters,
            field => field.ReadString(0), null, batchSize: 50, stream: true);

        Assert.AreEqual(startOpen ? 0 : 1, connection.CreatedCommands.Count);
        Assert.AreEqual(startOpen ? 0 : source.Parameters.Count, executor.Readers.Count);
        if (!startOpen)
        {
            Assert.IsTrue(executor.Readers.All(reader => reader.IsClosed));
            Assert.AreEqual(ConnectionState.Closed, connection.State);
            AssertCommandsDisposed(connection);
        }

        if (stopEarly)
        {
            using (var enumerator = results.GetEnumerator())
            {
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(source.ReaderResults[0], enumerator.Current);
                Assert.AreEqual(1, executor.Readers.Count);
                Assert.IsFalse(executor.Readers[0].IsClosed);
                Assert.AreEqual(ConnectionState.Open, connection.State);
                Assert.AreEqual(0, connection.DisposedCommands.Count);
            }
        }
        else
        {
            CollectionAssert.AreEqual(source.ReaderResults.ToArray(), results.ToArray());
        }

        Assert.AreEqual(stopEarly ? 1 : source.Parameters.Count, executor.Readers.Count);
        Assert.IsTrue(executor.Readers.All(reader => reader.IsClosed),
            "Actual batch readers must close before observer/connection teardown.");
        Assert.AreEqual(startOpen ? ConnectionState.Open : ConnectionState.Closed, connection.State);
        AssertCommandsDisposed(connection);
    }

    private static ObservedExecutor CreateExecutor(SqliteConnection connection)
        => new(new SQLiteQueryProvider(connection, new ImplicitMapping(), QueryPolicy.Default));

    private static void AssertCommandsDisposed(ObservedConnection connection)
    {
        Assert.AreEqual(1, connection.CreatedCommands.Count);
        Assert.IsTrue(connection.DisposedCommands.Contains(connection.CreatedCommands[0]),
            "The actual native batch command must dispose before observer/connection teardown.");
    }

    private sealed class NativeBatchSource
    {
        internal List<object[]> Parameters { get; }
        internal List<int> NonqueryResults { get; } = new();
        internal List<string> ReaderResults { get; } = new();
        private readonly Type _fieldType;

        internal NativeBatchSource()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            Console.WriteLine("Native driver: " + typeof(SqliteConnection).Assembly.FullName);
            using (var provenance = connection.CreateCommand())
            {
                provenance.CommandText = "SELECT sqlite_version(), sqlite_source_id();";
                using var reader = provenance.ExecuteReader();
                Assert.IsTrue(reader.Read());
                Console.WriteLine("Native SQLite version: " + reader.GetString(0));
                Console.WriteLine("Native SQLite source: " + reader.GetString(1));
            }

            var acquired = new List<object[]>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA compile_options;";
                using var reader = command.ExecuteReader();
                Assert.AreEqual(1, reader.FieldCount);
                _fieldType = reader.GetFieldType(0);
                Assert.AreEqual(typeof(string), _fieldType);
                foreach (var column in reader.GetColumnSchema())
                {
                    Console.WriteLine("Native column: " + column.ColumnName + " " + column.DataType);
                }
                while (reader.Read())
                {
                    // These transport buffers contain only actual GetValues
                    // responses; no parameter row or field is assembled by us.
                    var values = new object[reader.FieldCount];
                    Assert.AreEqual(reader.FieldCount, reader.GetValues(values));
                    acquired.Add(values);
                }
            }
            Console.WriteLine("Native acquired responses: " + JsonSerializer.Serialize(acquired));
            Assert.IsTrue(acquired.Count >= 2, "Two actual responses are required; no generated fallback is allowed.");
            Parameters = acquired.Take(2).ToList();
            Console.WriteLine("Native batch parameters: " + JsonSerializer.Serialize(Parameters));

            // Ordinary native commands on this independent connection supply
            // both oracles, including affected counts that are never hardcoded.
            foreach (var values in Parameters)
            {
                using var command = connection.CreateCommand();
                command.CommandText = MetadataQuery;
                command.Parameters.AddWithValue("@name", values[0]);
                NonqueryResults.Add(command.ExecuteNonQuery());
                using var reader = command.ExecuteReader();
                Assert.IsTrue(reader.Read(), "The actual option must be available from the native metadata source.");
                ReaderResults.Add(reader.GetString(0));
                Assert.IsFalse(reader.Read(), "The retained option must have exactly one native match.");
            }
            Console.WriteLine("Native batch statement: " + MetadataQuery);
            Console.WriteLine("Native nonquery results: " + JsonSerializer.Serialize(NonqueryResults));
            Console.WriteLine("Native reader results: " + JsonSerializer.Serialize(ReaderResults));
        }

        internal QueryCommand CreateQuery(DbEntityProvider provider)
            => new(MetadataQuery, new[]
            {
                new QueryParameter("@name", _fieldType, provider.Language.TypeSystem.GetColumnType(_fieldType))
            });
    }

    private sealed class ObservedExecutor : DbEntityProvider.Executor
    {
        internal ObservedExecutor(DbEntityProvider provider) : base(provider) { }
        internal List<DbDataReader> Readers { get; } = new();

        protected override DbDataReader ExecuteReader(DbCommand command)
        {
            var reader = base.ExecuteReader(command);
            Readers.Add(reader);
            return reader;
        }
    }

    private sealed class ObservedConnection : SqliteConnection
    {
        internal ObservedConnection() : base("Data Source=:memory:") { }
        internal List<SqliteCommand> CreatedCommands { get; } = new();
        internal HashSet<SqliteCommand> DisposedCommands { get; } = new();

        public override SqliteCommand CreateCommand()
        {
            var command = base.CreateCommand();
            CreatedCommands.Add(command);
            command.Disposed += (sender, args) => DisposedCommands.Add(command);
            return command;
        }
    }
}
