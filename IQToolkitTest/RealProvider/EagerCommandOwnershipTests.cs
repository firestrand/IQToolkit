using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using IQToolkit.Data.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IQToolkitTest.RealProvider;

[TestClass]
public class EagerCommandOwnershipTests
{
    private const string MetadataCommand = "PRAGMA compile_options;";

    [TestMethod]
    public void EagerExhaustionReleasesReaderAndCommandBeforeCallerDisposal()
    {
        var expected = SQLiteMetadata.ReadValues(MetadataCommand);
        using var connection = new ObservedConnection();
        connection.Open();
        var executor = CreateExecutor(connection);
        var results = Execute(executor);
        AssertBorrowedEagerAcquisition(connection, executor, results);
        var actual = new List<string>();
        var enumerator = results.GetEnumerator();
        try
        {
            while (enumerator.MoveNext())
            {
                actual.Add(enumerator.Current);
            }
            CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray());
            // Reaching false must release resources even before the caller's
            // Dispose; automatic enumeration helpers would hide that boundary.
            AssertReleased(connection, executor, ConnectionState.Open);
        }
        finally
        {
            enumerator.Dispose();
        }
        AssertReleased(connection, executor, ConnectionState.Open);
    }

    [TestMethod]
    public void ActiveEagerDisposalReleasesReaderAndCommandAndPreservesBorrowedConnection()
    {
        var expected = SQLiteMetadata.ReadValues(MetadataCommand);
        Assert.IsTrue(expected.Count > 1, "Actual rows are required for early disposal; no padding is allowed.");
        using var connection = new ObservedConnection();
        connection.Open();
        var executor = CreateExecutor(connection);
        var results = Execute(executor);
        AssertBorrowedEagerAcquisition(connection, executor, results);

        using (var enumerator = results.GetEnumerator())
        {
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(expected[0], enumerator.Current);
            Assert.IsFalse(executor.Readers[0].IsClosed);
            Assert.AreEqual(0, connection.DisposedCommands.Count);
        }
        AssertReleased(connection, executor, ConnectionState.Open);
    }

    [TestMethod]
    public void UnstartedEagerEnumeratorDisposalReleasesAlreadyAcquiredResourcesOnce()
    {
        SQLiteMetadata.ReadValues(MetadataCommand);
        using var connection = new ObservedConnection();
        connection.Open();
        var executor = CreateExecutor(connection);
        var results = Execute(executor);
        AssertBorrowedEagerAcquisition(connection, executor, results);
        var enumerator = results.GetEnumerator();
        enumerator.Dispose();
        enumerator.Dispose();

        Console.WriteLine("Observed unstarted reader closed: " + executor.Readers[0].IsClosed);
        Console.WriteLine("Observed unstarted command disposal events: " + connection.DisposedCommands.Count);
        AssertReleased(connection, executor, ConnectionState.Open);
    }

    [TestMethod]
    public void InitiallyClosedEagerQueryBuffersAndReleasesOwnedResourcesBeforeReturning()
    {
        var expected = SQLiteMetadata.ReadValues(MetadataCommand);
        using var connection = new ObservedConnection();
        var executor = CreateExecutor(connection);
        var results = Execute(executor);

        AssertReleased(connection, executor, ConnectionState.Closed);
        CollectionAssert.AreEqual(expected.ToArray(), results.ToArray());
        AssertReleased(connection, executor, ConnectionState.Closed);
    }

    private static IEnumerable<string> Execute(ObservedExecutor executor)
        => executor.Execute(new QueryCommand(MetadataCommand, null), field => field.ReadString(0), null, null);

    private static ObservedExecutor CreateExecutor(SqliteConnection connection)
    {
        Console.WriteLine("Native driver: " + typeof(SqliteConnection).Assembly.FullName);
        return new(new SQLiteQueryProvider(connection, new ImplicitMapping(), QueryPolicy.Default));
    }

    private static void AssertBorrowedEagerAcquisition(ObservedConnection connection,
        ObservedExecutor executor, IEnumerable<string> results)
    {
        Assert.IsInstanceOfType<EnumerateOnce<string>>(results);
        Assert.AreEqual(1, connection.CreatedCommands.Count);
        Assert.AreEqual(1, executor.Readers.Count);
        Assert.IsFalse(executor.Readers[0].IsClosed, "The existing execution path acquires its reader eagerly.");
        Assert.AreEqual(0, connection.DisposedCommands.Count);
        Assert.AreEqual(ConnectionState.Open, connection.State);
    }

    private static void AssertReleased(ObservedConnection connection, ObservedExecutor executor,
        ConnectionState expectedState)
    {
        Assert.AreEqual(1, executor.Readers.Count);
        Assert.IsTrue(executor.Readers[0].IsClosed,
            "Explicit eager enumeration cleanup must close the actual reader before connection teardown.");
        Assert.AreEqual(1, connection.CreatedCommands.Count);
        Assert.AreEqual(1, connection.DisposedCommands.Count,
            "The eagerly acquired command must dispose once before connection teardown.");
        Assert.AreSame(connection.CreatedCommands[0], connection.DisposedCommands[0]);
        Assert.AreEqual(expectedState, connection.State);
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
        internal List<SqliteCommand> DisposedCommands { get; } = new();

        public override SqliteCommand CreateCommand()
        {
            var command = base.CreateCommand();
            CreatedCommands.Add(command);
            command.Disposed += (sender, args) => DisposedCommands.Add(command);
            return command;
        }
    }
}
