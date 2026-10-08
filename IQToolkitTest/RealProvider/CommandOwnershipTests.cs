using System;
using System.Collections.Generic;
using System.Data;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using IQToolkit.Data.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IQToolkitTest.RealProvider;

[TestClass]
public class CommandOwnershipTests
{
    // The engine supplies the metadata; no user tables or fabricated rows are used.
    // Provenance and narrow fact scope: modernization-implementation-plan.md V1r.1.
    private const string MetadataCommand = "SELECT sqlite_version();";

    [TestMethod]
    public void NativeCommandDisposalEventIsObservedBeforeConnectionTeardown()
    {
        using var connection = new ObservedConnection();
        connection.Open();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = MetadataCommand;
            var version = command.ExecuteScalar();
            Assert.IsInstanceOfType<string>(version);
            Console.WriteLine("Native SQLite version: " + version);
            command.CommandText = "SELECT sqlite_source_id();";
            Console.WriteLine("Native SQLite source: " + command.ExecuteScalar());
            Assert.IsFalse(connection.DisposedCommands.Contains(command));
        }

        AssertCommandsDisposed(connection);
        Assert.AreEqual(ConnectionState.Open, connection.State);
    }

    [TestMethod]
    public void RawCommandDisposesCommandAndPreservesBorrowedOpenConnection()
    {
        using var connection = new ObservedConnection();
        connection.Open();
        CreateProvider(connection).ExecuteCommand(MetadataCommand);

        AssertCommandsDisposed(connection);
        Assert.AreEqual(ConnectionState.Open, connection.State);
    }

    [TestMethod]
    public void RawCommandReleasesCommandAndItsConnectionLease()
    {
        using var connection = new ObservedConnection();
        CreateProvider(connection).ExecuteCommand(MetadataCommand);

        AssertCommandsDisposed(connection);
        Assert.AreEqual(ConnectionState.Closed, connection.State);
    }

    [TestMethod]
    public void ExecutorCommandDisposesCommandAndPreservesBorrowedOpenConnection()
    {
        using var connection = new ObservedConnection();
        connection.Open();
        var executor = new DbEntityProvider.Executor(CreateProvider(connection));
        executor.ExecuteCommand(new QueryCommand(MetadataCommand, null), null);

        AssertCommandsDisposed(connection);
        Assert.AreEqual(ConnectionState.Open, connection.State);
    }

    [TestMethod]
    public void ExecutorCommandReleasesCommandAndItsConnectionLease()
    {
        using var connection = new ObservedConnection();
        var executor = new DbEntityProvider.Executor(CreateProvider(connection));
        executor.ExecuteCommand(new QueryCommand(MetadataCommand, null), null);

        AssertCommandsDisposed(connection);
        Assert.AreEqual(ConnectionState.Closed, connection.State);
    }

    private static SQLiteQueryProvider CreateProvider(SqliteConnection connection)
        => new SQLiteQueryProvider(connection, new ImplicitMapping(), QueryPolicy.Default);

    private static void AssertCommandsDisposed(ObservedConnection connection)
    {
        Assert.AreEqual(1, connection.CreatedCommands.Count);
        foreach (var command in connection.CreatedCommands)
        {
            Assert.IsTrue(connection.DisposedCommands.Contains(command),
                "The native command must be disposed before connection teardown.");
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
