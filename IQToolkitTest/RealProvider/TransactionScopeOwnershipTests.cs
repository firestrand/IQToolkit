using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using IQToolkit.Data.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IQToolkitTest.RealProvider;

[TestClass]
public class TransactionScopeOwnershipTests
{
    private const string MetadataQuery = "SELECT compile_options FROM pragma_compile_options;";
    private const string RejectedWrite = "DELETE FROM pragma_compile_options;";

    [TestMethod]
    public void NativeTransactionStateAndRejectionAreAcquired()
    {
        var capture = AcquireNativeTransaction();
        Assert.IsInstanceOfType<SqliteException>(capture.Error);
    }

    [TestMethod]
    public void BorrowedConnectionCreatedTransactionCompletesOnSuccess()
        => VerifyCreatedTransaction(borrowed: true, reject: false);

    [TestMethod]
    public void BorrowedConnectionCreatedTransactionCompletesOnNativeRejection()
        => VerifyCreatedTransaction(borrowed: true, reject: true);

    [TestMethod]
    public void OwnedConnectionCreatedTransactionCompletesBeforeCloseOnSuccess()
        => VerifyCreatedTransaction(borrowed: false, reject: false);

    [TestMethod]
    public void OwnedConnectionCreatedTransactionCompletesBeforeCloseOnNativeRejection()
        => VerifyCreatedTransaction(borrowed: false, reject: true);

    [TestMethod]
    public void CallerTransactionRemainsActiveOnSuccess()
        => VerifyCallerTransaction(reject: false);

    [TestMethod]
    public void CallerTransactionRemainsActiveOnNativeRejection()
        => VerifyCallerTransaction(reject: true);

    private static void VerifyCreatedTransaction(bool borrowed, bool reject)
    {
        var capture = AcquireNativeTransaction();
        using var connection = new ObservedConnection();
        if (borrowed)
            connection.Open();
        var provider = new SQLiteQueryProvider(connection, new ImplicitMapping(), QueryPolicy.Default);
        Assert.IsNull(provider.Transaction);
        Assert.AreEqual(borrowed ? ConnectionState.Open : ConnectionState.Closed, connection.State);
        SqliteTransaction transaction = null;
        SqliteException actionError = null;
        var closingCount = 0;
        var completedBeforeClose = false;
        var clearedBeforeClose = false;
        var autocommitBeforeClose = false;
        if (!borrowed)
        {
            connection.BeforeClose = () =>
            {
                // Observe before the driver's Close can complete leaked work.
                // This callback never completes or disposes the transaction.
                closingCount++;
                autocommitBeforeClose = ObserveAutocommit(connection, "Toolkit just before base.Close");
                completedBeforeClose = transaction != null && transaction.Connection == null;
                clearedBeforeClose = provider.Transaction == null;
            };
        }

        Action action = () =>
        {
            Assert.IsInstanceOfType<SqliteTransaction>(provider.Transaction);
            transaction = (SqliteTransaction)provider.Transaction;
            Assert.AreSame(connection, transaction.Connection);
            Assert.IsFalse(ObserveAutocommit(connection, "Toolkit created transaction inside action"));
            AssertNativeRows(capture.Rows, ReadNativeRows(connection, transaction));
            if (reject)
            {
                try
                {
                    provider.ExecuteCommand(RejectedWrite);
                }
                catch (SqliteException error)
                {
                    actionError = error;
                    throw;
                }
            }
        };
        if (reject)
        {
            var error = Assert.ThrowsExactly<SqliteException>(() => provider.DoTransacted(action));
            AssertNativeError(capture.Error, error, actionError);
        }
        else
        {
            provider.DoTransacted(action);
        }

        // All ownership checks precede any caller transaction/connection cleanup.
        Assert.IsNotNull(transaction);
        Assert.IsNull(provider.Transaction);
        Assert.IsNull(transaction.Connection, "Completion, not a Dispose-event oracle.");
        Assert.AreEqual(borrowed ? ConnectionState.Open : ConnectionState.Closed, connection.State);
        if (borrowed)
        {
            Assert.IsTrue(ObserveAutocommit(connection, "Toolkit completed on borrowed connection"));
            AssertNativeRows(capture.Rows, ReadNativeRows(connection, null));
            Assert.AreEqual(0, closingCount);
        }
        else
        {
            Assert.AreEqual(1, closingCount);
            Assert.IsTrue(autocommitBeforeClose);
            Assert.IsTrue(completedBeforeClose);
            Assert.IsTrue(clearedBeforeClose);
        }
        Console.WriteLine("Created transaction ownership before caller cleanup: " + JsonSerializer.Serialize(new
        {
            Borrowed = borrowed,
            Rejected = reject,
            Completed = transaction.Connection == null,
            ToolkitPropertyCleared = provider.Transaction == null,
            ConnectionState = connection.State.ToString(),
            ClosingCount = closingCount,
            AutocommitBeforeClose = borrowed ? (bool?)null : autocommitBeforeClose,
            CompletedBeforeClose = borrowed ? (bool?)null : completedBeforeClose,
            ClearedBeforeClose = borrowed ? (bool?)null : clearedBeforeClose
        }));
    }

    private static void VerifyCallerTransaction(bool reject)
    {
        var capture = AcquireNativeTransaction();
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var provider = new SQLiteQueryProvider(connection, new ImplicitMapping(), QueryPolicy.Default)
        {
            Transaction = transaction
        };
        Assert.IsFalse(ObserveAutocommit(connection, "Caller transaction before toolkit scope"));
        SqliteException actionError = null;
        Action action = () =>
        {
            Assert.AreSame(transaction, provider.Transaction);
            Assert.AreSame(connection, transaction.Connection);
            Assert.IsFalse(ObserveAutocommit(connection, "Caller transaction inside toolkit action"));
            AssertNativeRows(capture.Rows, ReadNativeRows(connection, transaction));
            if (reject)
            {
                try
                {
                    provider.ExecuteCommand(RejectedWrite);
                }
                catch (SqliteException error)
                {
                    actionError = error;
                    throw;
                }
            }
        };
        if (reject)
        {
            var error = Assert.ThrowsExactly<SqliteException>(() => provider.DoTransacted(action));
            AssertNativeError(capture.Error, error, actionError);
        }
        else
        {
            provider.DoTransacted(action);
        }

        Assert.AreEqual(ConnectionState.Open, connection.State);
        Assert.AreSame(transaction, provider.Transaction);
        Assert.AreSame(connection, transaction.Connection);
        Assert.IsFalse(ObserveAutocommit(connection, "Caller transaction after toolkit before caller cleanup"));
        AssertNativeRows(capture.Rows, ReadNativeRows(connection, transaction));
        Console.WriteLine("Caller transaction ownership before caller cleanup: " + JsonSerializer.Serialize(new
        {
            Rejected = reject,
            ToolkitPropertyRetained = ReferenceEquals(transaction, provider.Transaction),
            TransactionAttached = ReferenceEquals(connection, transaction.Connection),
            ConnectionState = connection.State.ToString()
        }));

        // Only the caller now completes its transaction, after the assertions.
        transaction.Rollback();
        Assert.IsTrue(ObserveAutocommit(connection, "Caller explicitly rolled back after ownership checks"));
        provider.Transaction = null;
    }

    private static void AssertNativeError(SqliteException expected, SqliteException actual, SqliteException actionError)
    {
        PrintError("Toolkit transaction rejection", actual);
        Assert.AreEqual(expected.GetType(), actual.GetType());
        Assert.AreEqual(expected.SqliteErrorCode, actual.SqliteErrorCode);
        Assert.AreEqual(expected.SqliteExtendedErrorCode, actual.SqliteExtendedErrorCode);
        Assert.AreEqual(expected.Message, actual.Message);
        Assert.AreSame(actionError, actual, "The original native action exception must reach the caller.");
    }

    private static (List<string> Rows, SqliteException Error) AcquireNativeTransaction()
    {
        var rows = SQLiteMetadata.ReadValues(MetadataQuery);
        Assert.IsTrue(rows.Count > 0, "Native metadata must exist; no fallback is allowed.");
        Console.WriteLine("Runtime: " + RuntimeInformation.FrameworkDescription);
        Console.WriteLine("Native driver: " + typeof(SqliteConnection).Assembly.FullName);
        Console.WriteLine("Native state wrapper: " + typeof(SQLitePCL.raw).Assembly.FullName);
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        Assert.IsTrue(ObserveAutocommit(connection, "Native before begin"));
        AssertNativeRows(rows, ReadNativeRows(connection, null));

        using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
        {
            Console.WriteLine("Native actual isolation: " + transaction.IsolationLevel);
            Assert.AreSame(connection, transaction.Connection);
            Assert.IsFalse(ObserveAutocommit(connection, "Native begun before commit"));
            AssertNativeRows(rows, ReadNativeRows(connection, transaction));
            transaction.Commit();
            Assert.IsNull(transaction.Connection);
            Assert.IsTrue(ObserveAutocommit(connection, "Native committed before disposal"));
        }

        SqliteException error;
        using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
        {
            Assert.IsFalse(ObserveAutocommit(connection, "Native begun before rejection"));
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = RejectedWrite;
            error = Assert.ThrowsExactly<SqliteException>(() =>
            {
                using var reader = command.ExecuteReader();
            });
            PrintError("Native transaction rejection", error);
            Assert.AreSame(connection, transaction.Connection);
            Assert.IsFalse(ObserveAutocommit(connection, "Native rejection before caller rollback"),
                "Acquire that this actual rejection does not automatically complete the transaction.");
            AssertNativeRows(rows, ReadNativeRows(connection, transaction));
            transaction.Rollback();
            Assert.IsNull(transaction.Connection);
            Assert.IsTrue(ObserveAutocommit(connection, "Native rolled back before disposal"));
        }
        AssertNativeRows(rows, ReadNativeRows(connection, null));
        return (rows, error);
    }

    private static bool ObserveAutocommit(SqliteConnection connection, string stage)
    {
        // The native API is valid only on an open, exclusively used handle.
        Assert.AreEqual(ConnectionState.Open, connection.State);
        Assert.IsNotNull(connection.Handle);
        var nativeValue = SQLitePCL.raw.sqlite3_get_autocommit(connection.Handle);
        Console.WriteLine("Native transaction state: " + JsonSerializer.Serialize(new
        {
            Stage = stage,
            Autocommit = nativeValue,
            Enabled = nativeValue != 0,
            ConnectionState = connection.State.ToString()
        }));
        return nativeValue != 0;
    }

    private static List<string> ReadNativeRows(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = MetadataQuery;
        using var reader = command.ExecuteReader();
        Assert.AreEqual(1, reader.FieldCount);
        Assert.AreEqual(typeof(string), reader.GetFieldType(0));
        Console.WriteLine("Native schema: " + JsonSerializer.Serialize(new
        {
            Name = reader.GetName(0),
            FieldType = reader.GetFieldType(0).FullName,
            DataType = reader.GetDataTypeName(0)
        }));
        var rows = new List<string>();
        while (reader.Read())
            rows.Add(reader.GetString(0));
        Console.WriteLine("Native transaction rows: " + JsonSerializer.Serialize(rows));
        return rows;
    }

    private static void AssertNativeRows(List<string> expected, List<string> actual)
        => CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray());

    private static void PrintError(string stage, SqliteException error)
        => Console.WriteLine("Transaction error: " + JsonSerializer.Serialize(new
        {
            Stage = stage,
            Request = RejectedWrite,
            Type = error.GetType().FullName,
            error.SqliteErrorCode,
            error.SqliteExtendedErrorCode,
            error.Message
        }));

    private sealed class ObservedConnection : SqliteConnection
    {
        internal ObservedConnection() : base("Data Source=:memory:") { }
        internal Action BeforeClose { get; set; }

        public override void Close()
        {
            if (State == ConnectionState.Open)
                BeforeClose?.Invoke();
            base.Close();
        }
    }
}
