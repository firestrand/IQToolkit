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
public class NativeCommandFailureTests
{
    // Operation configuration over the installed read-only metadata module.
    // No generated rows, parameters or exceptions; plan V1f records provenance.
    private const string MetadataQuery = "SELECT compile_options FROM pragma_compile_options;";
    private const string RejectedWrite = "DELETE FROM pragma_compile_options;";

    [TestMethod]
    public void NativeRejectedMetadataWriteIsAcquired()
    {
        var error = AcquireNativeFailure();
        Assert.IsInstanceOfType<SqliteException>(error);
    }

    [TestMethod]
    public void RawNativeFailureDisposesCommandAndPreservesBorrowedConnection()
    {
        var expected = AcquireNativeFailure();
        using var connection = new ObservedConnection();
        connection.Open();
        var provider = CreateProvider(connection);
        var actual = Assert.ThrowsExactly<SqliteException>(() => provider.ExecuteCommand(RejectedWrite));

        AssertNativeFailure(expected, actual);
        AssertReleased(connection, ConnectionState.Open);
    }

    [TestMethod]
    public void NonqueryNativeFailureDisposesCommandAndPreservesBorrowedConnection()
    {
        var expected = AcquireNativeFailure();
        using var connection = new ObservedConnection();
        connection.Open();
        var executor = new ObservedExecutor(CreateProvider(connection));
        var actual = Assert.ThrowsExactly<SqliteException>(() =>
            executor.ExecuteCommand(new QueryCommand(RejectedWrite, null), null));

        AssertNativeFailure(expected, actual);
        AssertReleased(connection, ConnectionState.Open);
    }

    [TestMethod]
    public void DeferredNativeAcquisitionFailureDisposesCommandBeforeCallerDisposal()
    {
        var expected = AcquireNativeFailure();
        using var connection = new ObservedConnection();
        connection.Open();
        var executor = new ObservedExecutor(CreateProvider(connection));
        var results = executor.ExecuteDeferred(new QueryCommand(RejectedWrite, null),
            field => field.ReadString(0), null, null);
        Assert.AreEqual(0, connection.CreatedCommands.Count);
        Assert.AreEqual(0, executor.ReaderAttempts);
        var enumerator = results.GetEnumerator();
        try
        {
            var actual = Assert.ThrowsExactly<SqliteException>(() => enumerator.MoveNext());
            AssertReaderAcquisitionFailure(executor, expected, actual);
            AssertReleased(connection, ConnectionState.Open);
        }
        finally
        {
            enumerator.Dispose();
        }
    }

    [TestMethod]
    public void EagerNativeAcquisitionFailureDisposesCommandAndPreservesBorrowedConnection()
    {
        var expected = AcquireNativeFailure();
        using var connection = new ObservedConnection();
        connection.Open();
        var executor = new ObservedExecutor(CreateProvider(connection));
        var actual = Assert.ThrowsExactly<SqliteException>(() =>
            executor.Execute(new QueryCommand(RejectedWrite, null), field => field.ReadString(0), null, null));

        AssertReaderAcquisitionFailure(executor, expected, actual);
        AssertReleased(connection, ConnectionState.Open);
    }

    [TestMethod]
    public void InitiallyClosedEagerNativeFailureReleasesItsConnectionLease()
    {
        var expected = AcquireNativeFailure();
        using var connection = new ObservedConnection();
        var executor = new ObservedExecutor(CreateProvider(connection));
        var actual = Assert.ThrowsExactly<SqliteException>(() =>
            executor.Execute(new QueryCommand(RejectedWrite, null), field => field.ReadString(0), null, null));

        AssertReaderAcquisitionFailure(executor, expected, actual);
        // Native connection closure may dispose a leaked command, so this is a
        // lease/error preservation control, not the borrowed command oracle.
        AssertReleased(connection, ConnectionState.Closed);
    }

    private static SqliteException AcquireNativeFailure()
    {
        // This control uses an ordinary connection and the driver's own cleanup.
        // It acquires the native response before any toolkit failure assertions.
        var nativeOptions = SQLiteMetadata.ReadValues(MetadataQuery);
        Assert.IsTrue(nativeOptions.Count > 0, "Native metadata acquisition must succeed; no fallback is allowed.");
        Console.WriteLine("Native driver: " + typeof(SqliteConnection).Assembly.FullName);
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var before = ReadNativeOptions(connection);
        CollectionAssert.AreEqual(nativeOptions.ToArray(), before.ToArray());

        var disposalEvents = 0;
        SqliteException failure;
        using (var command = connection.CreateCommand())
        {
            command.Disposed += (sender, args) => disposalEvents++;
            command.CommandText = RejectedWrite;
            failure = Assert.ThrowsExactly<SqliteException>(() =>
            {
                using var reader = command.ExecuteReader();
            });
            Assert.AreEqual(0, disposalEvents);
        }
        Assert.AreEqual(1, disposalEvents, "The native observer must see ordinary command disposal.");
        Assert.AreEqual(ConnectionState.Open, connection.State);
        CollectionAssert.AreEqual(before.ToArray(), ReadNativeOptions(connection).ToArray(),
            "The rejected operation must leave the actual native metadata unchanged.");
        PrintFailure("Native rejection", failure);
        return failure;
    }

    private static List<string> ReadNativeOptions(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = MetadataQuery;
        using var reader = command.ExecuteReader();
        Assert.AreEqual(1, reader.FieldCount);
        Console.WriteLine("Native schema: " + JsonSerializer.Serialize(new
        {
            Name = reader.GetName(0),
            FieldType = reader.GetFieldType(0).FullName,
            DataType = reader.GetDataTypeName(0)
        }));
        var options = new List<string>();
        while (reader.Read())
        {
            options.Add(reader.GetString(0));
        }
        Console.WriteLine("Native request: " + MetadataQuery);
        Console.WriteLine("Native options: " + JsonSerializer.Serialize(options));
        return options;
    }

    private static void PrintFailure(string label, SqliteException failure)
    {
        Console.WriteLine(label + ": " + JsonSerializer.Serialize(new
        {
            Request = RejectedWrite,
            Type = failure.GetType().FullName,
            failure.SqliteErrorCode,
            failure.SqliteExtendedErrorCode,
            failure.Message
        }));
    }

    private static SQLiteQueryProvider CreateProvider(SqliteConnection connection)
        => new(connection, new ImplicitMapping(), QueryPolicy.Default);

    private static void AssertNativeFailure(SqliteException expected, SqliteException actual)
    {
        PrintFailure("Toolkit rejection", actual);
        Assert.AreEqual(expected.GetType(), actual.GetType());
        Assert.AreEqual(expected.SqliteErrorCode, actual.SqliteErrorCode);
        Assert.AreEqual(expected.SqliteExtendedErrorCode, actual.SqliteExtendedErrorCode);
        Assert.AreEqual(expected.Message, actual.Message);
    }

    private static void AssertReaderAcquisitionFailure(ObservedExecutor executor,
        SqliteException expected, SqliteException actual)
    {
        AssertNativeFailure(expected, actual);
        Assert.AreEqual(1, executor.ReaderAttempts);
        Assert.AreEqual(0, executor.ReturnedReaders.Count,
            "The driver rejected acquisition before returning a reader to the toolkit.");
        Assert.AreSame(executor.NativeFailure, actual,
            "The public caller must receive the original native exception object.");
    }

    private static void AssertReleased(ObservedConnection connection, ConnectionState expectedState)
    {
        Assert.AreEqual(expectedState, connection.State);
        Assert.AreEqual(1, connection.CreatedCommands.Count);
        Console.WriteLine("Observed command disposal events: " + connection.DisposedCommands.Count);
        Assert.AreEqual(1, connection.DisposedCommands.Count,
            "The acquired native command must dispose before connection teardown after failure.");
        Assert.AreSame(connection.CreatedCommands[0], connection.DisposedCommands[0]);
    }

    private sealed class ObservedExecutor : DbEntityProvider.Executor
    {
        internal ObservedExecutor(DbEntityProvider provider) : base(provider) { }
        internal int ReaderAttempts { get; private set; }
        internal List<DbDataReader> ReturnedReaders { get; } = new();
        internal SqliteException NativeFailure { get; private set; }

        protected override DbDataReader ExecuteReader(DbCommand command)
        {
            ReaderAttempts++;
            try
            {
                var reader = base.ExecuteReader(command);
                ReturnedReaders.Add(reader);
                return reader;
            }
            catch (SqliteException error)
            {
                NativeFailure = error;
                throw;
            }
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
