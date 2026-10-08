using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using IQToolkit.Data.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IQToolkitTest.RealProvider;

[TestClass]
public class DeferredCommandOwnershipTests
{
    private const string MetadataCommand = "PRAGMA compile_options;";

    [TestMethod]
    public void CompleteDeferredEnumerationDisposesCommandAndPreservesBorrowedConnection()
        => VerifyEnumeration(startOpen: true, stopEarly: false);

    [TestMethod]
    public void EarlyDeferredDisposalReleasesCommandAndPreservesBorrowedConnection()
        => VerifyEnumeration(startOpen: true, stopEarly: true);

    [TestMethod]
    public void CompleteDeferredEnumerationReleasesOwnedConnectionLease()
        => VerifyEnumeration(startOpen: false, stopEarly: false);

    [TestMethod]
    public void EarlyDeferredDisposalReleasesOwnedConnectionLease()
        => VerifyEnumeration(startOpen: false, stopEarly: true);

    [TestMethod]
    public void UnstartedDeferredEnumeratorDoesNotAcquireClosedConnectionResources()
        => VerifyUnstarted(startOpen: false);

    [TestMethod]
    public void UnstartedDeferredEnumeratorPreservesBorrowedConnection()
        => VerifyUnstarted(startOpen: true);

    private static void VerifyEnumeration(bool startOpen, bool stopEarly)
    {
        // This independent connection reads unchanged installed-library metadata.
        var expected = SQLiteMetadata.ReadValues(MetadataCommand);
        Assert.IsTrue(expected.Count > 1,
            "Actual native rows are required for partial enumeration; no padding is allowed.");

        using var connection = new ObservedConnection();
        if (startOpen)
        {
            connection.Open();
        }
        var executor = CreateExecutor(connection);
        var query = executor.ExecuteDeferred(new QueryCommand(MetadataCommand, null),
            field => field.ReadString(0), null, null);
        Assert.AreEqual(0, connection.CreatedCommands.Count);
        Assert.AreEqual(0, executor.Readers.Count);

        if (stopEarly)
        {
            using (var enumerator = query.GetEnumerator())
            {
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(expected[0], enumerator.Current);
                Assert.AreEqual(1, executor.Readers.Count);
                Assert.IsFalse(executor.Readers[0].IsClosed);
                Assert.AreEqual(ConnectionState.Open, connection.State);
                Assert.AreEqual(0, connection.DisposedCommands.Count);
            }
        }
        else
        {
            CollectionAssert.AreEqual(expected.ToArray(), query.ToArray());
        }

        Assert.AreEqual(1, executor.Readers.Count);
        Assert.IsTrue(executor.Readers[0].IsClosed,
            "The actual native reader must close before connection teardown.");
        Assert.AreEqual(startOpen ? ConnectionState.Open : ConnectionState.Closed, connection.State);
        Assert.AreEqual(1, connection.CreatedCommands.Count);
        Assert.IsTrue(connection.DisposedCommands.Contains(connection.CreatedCommands[0]),
            "The deferred native command must dispose before connection teardown.");
    }

    private static void VerifyUnstarted(bool startOpen)
    {
        using var connection = new ObservedConnection();
        if (startOpen)
        {
            connection.Open();
        }
        var executor = CreateExecutor(connection);
        var query = executor.ExecuteDeferred(new QueryCommand(MetadataCommand, null),
            field => field.ReadString(0), null, null);
        using (query.GetEnumerator())
        {
            Assert.AreEqual(0, connection.CreatedCommands.Count);
            Assert.AreEqual(0, executor.Readers.Count);
        }

        Assert.AreEqual(0, connection.CreatedCommands.Count);
        Assert.AreEqual(0, executor.Readers.Count);
        Assert.AreEqual(startOpen ? ConnectionState.Open : ConnectionState.Closed, connection.State);
    }

    private static ObservedExecutor CreateExecutor(SqliteConnection connection)
        => new ObservedExecutor(new SQLiteQueryProvider(connection, new ImplicitMapping(), QueryPolicy.Default));

    private sealed class ObservedExecutor : DbEntityProvider.Executor
    {
        public ObservedExecutor(DbEntityProvider provider) : base(provider) { }

        public List<DbDataReader> Readers { get; } = new List<DbDataReader>();

        protected override DbDataReader ExecuteReader(DbCommand command)
        {
            var reader = base.ExecuteReader(command);
            this.Readers.Add(reader);
            return reader;
        }
    }

    private sealed class ObservedConnection : SqliteConnection
    {
        public ObservedConnection() : base("Data Source=:memory:") { }

        public List<SqliteCommand> CreatedCommands { get; } = new List<SqliteCommand>();
        public HashSet<SqliteCommand> DisposedCommands { get; } = new HashSet<SqliteCommand>();

        public override SqliteCommand CreateCommand()
        {
            var command = base.CreateCommand();
            this.CreatedCommands.Add(command);
            command.Disposed += (sender, args) => this.DisposedCommands.Add(command);
            return command;
        }
    }
}
