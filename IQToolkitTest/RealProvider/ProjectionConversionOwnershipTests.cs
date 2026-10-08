using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using IQToolkit.Data.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IQToolkitTest.RealProvider;

[TestClass]
public class ProjectionConversionOwnershipTests
{
    private const string MetadataQuery = "PRAGMA compile_options;";

    [TestMethod]
    public void NativeGuidConversionFailureIsAcquired()
    {
        var capture = AcquireConversionFailure();
        Assert.IsInstanceOfType<FormatException>(capture.Error);
    }

    [TestMethod]
    public void BorrowedEagerReadGuidReleasesResourcesBeforeCallerDisposal()
        => VerifyOwnership(eager: true, borrowed: true, readValue: false);

    [TestMethod]
    public void BorrowedEagerReadValueGuidReleasesResourcesBeforeCallerDisposal()
        => VerifyOwnership(eager: true, borrowed: true, readValue: true);

    [TestMethod]
    public void OwnedEagerReadGuidReleasesResourcesBeforeFailureReturns()
        => VerifyOwnership(eager: true, borrowed: false, readValue: false);

    [TestMethod]
    public void OwnedEagerReadValueGuidReleasesResourcesBeforeFailureReturns()
        => VerifyOwnership(eager: true, borrowed: false, readValue: true);

    [TestMethod]
    public void BorrowedDeferredReadGuidReleasesResourcesBeforeCallerDisposal()
        => VerifyOwnership(eager: false, borrowed: true, readValue: false);

    [TestMethod]
    public void BorrowedDeferredReadValueGuidReleasesResourcesBeforeCallerDisposal()
        => VerifyOwnership(eager: false, borrowed: true, readValue: true);

    [TestMethod]
    public void OwnedDeferredReadGuidReleasesResourcesBeforeCallerDisposal()
        => VerifyOwnership(eager: false, borrowed: false, readValue: false);

    [TestMethod]
    public void OwnedDeferredReadValueGuidReleasesResourcesBeforeCallerDisposal()
        => VerifyOwnership(eager: false, borrowed: false, readValue: true);

    private static void VerifyOwnership(bool eager, bool borrowed, bool readValue)
    {
        var capture = AcquireConversionFailure();
        using var connection = new ObservedConnection();
        if (borrowed)
            connection.Open();
        var executor = new ObservedExecutor(new SQLiteQueryProvider(connection,
            new ImplicitMapping(), QueryPolicy.Default));
        string observedValue = null;
        FormatException projectedError = null;
        Func<FieldReader, Guid> projector = field =>
        {
            observedValue = field.ReadString(0);
            try
            {
                return readValue ? field.ReadValue<Guid>(0) : field.ReadGuid(0);
            }
            catch (FormatException error)
            {
                projectedError = error;
                throw;
            }
        };
        IEnumerator<Guid> enumerator = null;
        try
        {
            var actual = Assert.ThrowsExactly<FormatException>(() =>
            {
                var query = new QueryCommand(MetadataQuery, null);
                var results = eager
                    ? executor.Execute(query, projector, null, null)
                    : executor.ExecuteDeferred(query, projector, null, null);
                if (eager)
                {
                    // Only borrowed eager execution reaches here: owned eager
                    // execution converts while buffering inside Execute.
                    Assert.IsTrue(borrowed);
                    Assert.AreEqual(1, connection.CreatedCommands.Count);
                    Assert.AreEqual(1, executor.Readers.Count);
                    Assert.IsFalse(executor.Readers[0].IsClosed);
                }
                else
                {
                    Assert.AreEqual(0, connection.CreatedCommands.Count);
                    Assert.AreEqual(0, executor.Readers.Count);
                }
                enumerator = results.GetEnumerator();
                enumerator.MoveNext();
            });

            PrintError("Toolkit conversion failure", actual);
            Assert.AreEqual(capture.Value, observedValue);
            Assert.AreEqual(capture.Error.GetType(), actual.GetType());
            Assert.AreEqual(capture.Error.Message, actual.Message);
            Assert.AreEqual(capture.Error.HResult, actual.HResult);
            Assert.AreSame(projectedError, actual, "The original conversion exception must reach the caller.");
            // Inspect before caller Dispose/connection teardown can mask leaks.
            Assert.AreEqual(1, executor.Readers.Count);
            Assert.IsTrue(executor.Readers[0].IsClosed);
            Assert.AreEqual(1, connection.CreatedCommands.Count);
            Assert.AreEqual(1, connection.DisposedCommands.Count);
            Assert.AreSame(connection.CreatedCommands[0], connection.DisposedCommands[0]);
            Assert.AreEqual(borrowed ? ConnectionState.Open : ConnectionState.Closed, connection.State);
            Console.WriteLine("Ownership at caller catch: " + JsonSerializer.Serialize(new
            {
                Eager = eager,
                Borrowed = borrowed,
                ReadValue = readValue,
                ReaderClosed = executor.Readers[0].IsClosed,
                DisposalEvents = connection.DisposedCommands.Count,
                ConnectionState = connection.State.ToString()
            }));
        }
        finally
        {
            enumerator?.Dispose();
        }
    }

    private static (string Value, FormatException Error) AcquireConversionFailure()
    {
        var options = SQLiteMetadata.ReadValues(MetadataQuery);
        Assert.IsTrue(options.Count > 0, "Native input must exist; no invented fallback is allowed.");
        Console.WriteLine("Runtime: " + RuntimeInformation.FrameworkDescription);
        Console.WriteLine("BCL: " + typeof(Guid).Assembly.FullName);
        Console.WriteLine("BCL revision: " + typeof(Guid).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
        Console.WriteLine("Native driver: " + typeof(SqliteConnection).Assembly.FullName);
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = MetadataQuery;
        using var reader = command.ExecuteReader();
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(1, reader.FieldCount);
        var raw = reader.GetValue(0);
        Assert.IsInstanceOfType<string>(raw);
        Assert.AreEqual(typeof(string), reader.GetFieldType(0));
        var value = (string)raw;
        Assert.AreEqual(options[0], value);
        Console.WriteLine("Native conversion input: " + JsonSerializer.Serialize(new
        {
            Request = MetadataQuery,
            Name = reader.GetName(0),
            FieldType = reader.GetFieldType(0).FullName,
            DataType = reader.GetDataTypeName(0),
            Value = value
        }));
        // Independent BCL operation on unchanged captured input; the oracle
        // does not call the toolkit conversion implementation or alter data.
        var error = Assert.ThrowsExactly<FormatException>(() => new Guid(value));
        PrintError("BCL conversion failure", error);
        return (value, error);
    }

    private static void PrintError(string label, FormatException error)
    {
        Console.WriteLine(label + ": " + JsonSerializer.Serialize(new
        {
            Type = error.GetType().FullName,
            error.Message,
            error.HResult
        }));
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
