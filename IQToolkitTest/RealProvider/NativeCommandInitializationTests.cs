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
public class NativeCommandInitializationTests
{
    private const string OneParameterQuery = "SELECT compile_options FROM pragma_compile_options WHERE compile_options = @option;";
    private const string TwoParameterQuery = "SELECT compile_options FROM pragma_compile_options WHERE compile_options = @option AND sqlite_version() = @version;";

    [TestMethod]
    public void NativeCommandArgumentsAreAcquired()
    {
        var inputs = AcquireInputs();
        using var connection = new SqliteConnection("Data Source=:memory:");
        var provider = new SQLiteQueryProvider(connection, new ImplicitMapping(), QueryPolicy.Default);
        var parameters = new[]
        {
            new QueryParameter("@option", inputs.ArgumentTypes[0], provider.Language.TypeSystem.GetColumnType(inputs.ArgumentTypes[0])),
            new QueryParameter("@version", inputs.ArgumentTypes[1], provider.Language.TypeSystem.GetColumnType(inputs.ArgumentTypes[1]))
        };
        Assert.AreEqual(1, inputs.OneColumnArguments.Length);
        Assert.AreEqual(2, inputs.TwoColumnArguments.Length);
        Assert.AreEqual(inputs.TwoColumnArguments.Length, parameters.Length);
        var oracleMatches = inputs.OneParameterOracle.Order(StringComparer.Ordinal)
            .SequenceEqual(inputs.TwoParameterOracle.Order(StringComparer.Ordinal));
        Assert.IsTrue(oracleMatches);
        Console.WriteLine("Actual initialization acquisition: " + JsonSerializer.Serialize(new
        {
            inputs.OneColumnArguments, inputs.TwoColumnArguments,
            inputs.OneParameterOracle, inputs.TwoParameterOracle,
            ConfiguredParameters = parameters.Select(parameter => new
            {
                parameter.Name, Type = parameter.Type.FullName, QueryType = parameter.QueryType.ToString()
            }).ToArray(),
            OracleMatches = oracleMatches,
            ToolkitInitializationExecuted = false, InitializationOwnershipVerified = false
        }));
    }

    [TestMethod]
    public void BorrowedEagerInitializationFailureDisposesCommand()
        => AssertInitializationFailure(borrowed: true);

    [TestMethod]
    public void OwnedEagerInitializationFailureDisposesCommandBeforeConnectionClose()
        => AssertInitializationFailure(borrowed: false);

    [TestMethod]
    public void CompleteNativeArgumentsPreserveEagerSuccess()
        => AssertInitializationSuccess(complete: true);

    [TestMethod]
    public void NativeSingleArgumentPreservesEagerSuccess()
        => AssertInitializationSuccess(complete: false);

    private static void AssertInitializationFailure(bool borrowed)
    {
        var inputs = AcquireInputs();
        var unchangedArguments = inputs.OneColumnArguments.ToArray();
        using var connection = new ObservedConnection();
        if (borrowed)
            connection.Open();
        var provider = CreateObservedProvider(connection);
        var executor = new ObservedExecutor(provider);
        var query = CreateQuery(provider, inputs, complete: true);
        var failure = Assert.ThrowsExactly<IndexOutOfRangeException>(() =>
            executor.Execute(query, field => field.ReadString(0), null, inputs.OneColumnArguments));

        Assert.AreSame(executor.InitializationFailure, failure);
        Assert.AreEqual(1, connection.CreatedCommands.Count);
        Assert.AreSame(connection.CreatedCommands[0], executor.InitializationCommand);
        Assert.AreEqual(1, executor.FirstParameterCount);
        Assert.AreEqual(query.Parameters[0].Name, executor.FirstParameterName);
        Assert.AreEqual(inputs.OneColumnArguments[0], executor.FirstParameterValue);
        Assert.AreEqual(0, executor.ReaderAttempts);
        CollectionAssert.AreEqual(unchangedArguments, inputs.OneColumnArguments);
        Assert.AreEqual(borrowed ? ConnectionState.Open : ConnectionState.Closed, connection.State);
        Console.WriteLine("Actual initialization behavior: " + JsonSerializer.Serialize(new
        {
            Case = borrowed ? "BorrowedFailure" : "OwnedFailure",
            Sql = query.CommandText, FailureType = failure.GetType().FullName,
            OriginalExceptionPreserved = ReferenceEquals(executor.InitializationFailure, failure),
            InitialParameterCount = executor.FirstParameterCount,
            executor.FirstParameterName, executor.FirstParameterValue,
            CreatedCommands = connection.CreatedCommands.Count,
            DisposedBeforeCaller = connection.DisposedCommands.Count,
            DisposedBeforeConnectionClose = connection.DisposedCountBeforeClose,
            executor.ReaderAttempts, ConnectionState = connection.State.ToString(),
            Arguments = inputs.OneColumnArguments
        }));
        if (!borrowed)
            Assert.AreEqual<int?>(1, connection.DisposedCountBeforeClose,
                "Initialization failure must dispose the command before connection closure.");
        Assert.AreEqual(1, connection.DisposedCommands.Count,
            "Initialization failure must dispose the command before caller teardown.");
        Assert.AreSame(connection.CreatedCommands[0], connection.DisposedCommands[0]);
    }

    private static void AssertInitializationSuccess(bool complete)
    {
        var inputs = AcquireInputs();
        var arguments = complete ? inputs.TwoColumnArguments : inputs.OneColumnArguments;
        var unchangedArguments = arguments.ToArray();
        var expected = complete ? inputs.TwoParameterOracle : inputs.OneParameterOracle;
        using var connection = new ObservedConnection();
        connection.Open();
        var provider = CreateObservedProvider(connection);
        var executor = new ObservedExecutor(provider);
        var query = CreateQuery(provider, inputs, complete);
        var rows = executor.Execute(query, field => field.ReadString(0), null, arguments).ToArray();

        Assert.AreEqual(1, connection.CreatedCommands.Count);
        Assert.AreEqual(1, executor.ReaderAttempts);
        Assert.IsNull(executor.InitializationFailure);
        Assert.AreEqual(ConnectionState.Open, connection.State);
        CollectionAssert.AreEqual(unchangedArguments, arguments);
        CollectionAssert.AreEquivalent(expected, rows);
        Console.WriteLine("Actual initialization behavior: " + JsonSerializer.Serialize(new
        {
            Case = complete ? "CompleteArguments" : "SingleArgument",
            Sql = query.CommandText, CreatedCommands = connection.CreatedCommands.Count,
            DisposedBeforeCaller = connection.DisposedCommands.Count,
            DisposedBeforeConnectionClose = connection.DisposedCountBeforeClose,
            executor.ReaderAttempts, ConnectionState = connection.State.ToString(),
            Rows = rows, ExpectedRows = expected, Arguments = arguments
        }));
        Assert.AreEqual(1, connection.DisposedCommands.Count,
            "Successful eager enumeration must dispose its command exactly once before caller teardown.");
        Assert.AreSame(connection.CreatedCommands[0], connection.DisposedCommands[0]);
    }

    private static SQLiteQueryProvider CreateObservedProvider(ObservedConnection connection)
        => new(connection, new ImplicitMapping(), QueryPolicy.Default) { Log = null };

    private static QueryCommand CreateQuery(SQLiteQueryProvider provider, Inputs inputs, bool complete)
    {
        var parameters = new List<QueryParameter>
        {
            new("@option", inputs.ArgumentTypes[0], provider.Language.TypeSystem.GetColumnType(inputs.ArgumentTypes[0]))
        };
        if (complete)
            parameters.Add(new QueryParameter("@version", inputs.ArgumentTypes[1],
                provider.Language.TypeSystem.GetColumnType(inputs.ArgumentTypes[1])));
        return new QueryCommand(complete ? TwoParameterQuery : OneParameterQuery, parameters);
    }

    private sealed class ObservedExecutor : DbEntityProvider.Executor
    {
        internal ObservedExecutor(DbEntityProvider provider) : base(provider) { }
        internal int ReaderAttempts { get; private set; }
        internal Exception InitializationFailure { get; private set; }
        internal DbCommand InitializationCommand { get; private set; }
        internal int FirstParameterCount { get; private set; }
        internal string FirstParameterName { get; private set; }
        internal object FirstParameterValue { get; private set; }

        protected override void SetParameterValues(QueryCommand query, DbCommand command, object[] paramValues)
        {
            try
            {
                base.SetParameterValues(query, command, paramValues);
            }
            catch (Exception failure)
            {
                InitializationFailure = failure;
                InitializationCommand = command;
                FirstParameterCount = command.Parameters.Count;
                if (command.Parameters.Count > 0)
                {
                    FirstParameterName = command.Parameters[0].ParameterName;
                    FirstParameterValue = command.Parameters[0].Value;
                }
                throw;
            }
        }

        protected override DbDataReader ExecuteReader(DbCommand command)
        {
            ReaderAttempts++;
            return base.ExecuteReader(command);
        }
    }

    private sealed class ObservedConnection : SqliteConnection
    {
        internal ObservedConnection() : base("Data Source=:memory:") { }
        internal List<SqliteCommand> CreatedCommands { get; } = new();
        internal List<SqliteCommand> DisposedCommands { get; } = new();
        internal int? DisposedCountBeforeClose { get; private set; }

        public override SqliteCommand CreateCommand()
        {
            var command = base.CreateCommand();
            CreatedCommands.Add(command);
            command.Disposed += (sender, args) => DisposedCommands.Add(command);
            return command;
        }

        public override void Close()
        {
            DisposedCountBeforeClose = DisposedCommands.Count;
            base.Close();
        }
    }

    private static Inputs AcquireInputs()
    {
        var options = SQLiteMetadata.ReadValues("PRAGMA compile_options;");
        Assert.IsTrue(options.Count >= 1, "At least one actual native compile option is required; no fallback is allowed.");
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var version = RecordProvenance(connection);
        object[] one;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = OneParameterQuery;
            command.Parameters.AddWithValue("@option", options[0]);
            var response = ReadRows(command, "one-column argument capture", expectedFields: 1);
            Assert.AreEqual(1, response.Rows.Count);
            one = response.Rows[0];
            Assert.AreEqual(options[0], (string)one[0]);
        }
        object[] two;
        Type[] types;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT compile_options, sqlite_version() AS sqlite_version FROM pragma_compile_options WHERE compile_options = @option;";
            command.Parameters.AddWithValue("@option", one[0]);
            var response = ReadRows(command, "two-column argument capture", expectedFields: 2);
            Assert.AreEqual(1, response.Rows.Count);
            two = response.Rows[0];
            types = response.Types;
            Assert.AreEqual(one[0], two[0]);
            Assert.AreEqual(version, (string)two[1]);
        }
        using var oracleConnection = new SqliteConnection("Data Source=:memory:");
        oracleConnection.Open();
        string[] oneOracle;
        using (var command = oracleConnection.CreateCommand())
        {
            command.CommandText = OneParameterQuery;
            command.Parameters.AddWithValue("@option", one[0]);
            oneOracle = ReadRows(command, "one-parameter independent oracle", expectedFields: 1)
                .Rows.Select(row => (string)row[0]).ToArray();
        }
        string[] twoOracle;
        using (var command = oracleConnection.CreateCommand())
        {
            command.CommandText = TwoParameterQuery;
            command.Parameters.AddWithValue("@option", two[0]);
            command.Parameters.AddWithValue("@version", two[1]);
            twoOracle = ReadRows(command, "two-parameter independent oracle", expectedFields: 1)
                .Rows.Select(row => (string)row[0]).ToArray();
        }
        Assert.AreEqual(1, oneOracle.Length);
        Assert.AreEqual(1, twoOracle.Length);
        Assert.IsTrue(oneOracle.All(value => StringComparer.Ordinal.Equals(value, (string)one[0])));
        Assert.IsTrue(twoOracle.All(value => StringComparer.Ordinal.Equals(value, (string)two[0])));
        CollectionAssert.AreEquivalent(oneOracle, twoOracle);
        return new Inputs(one, two, types, oneOracle, twoOracle);
    }

    private static string RecordProvenance(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT sqlite_version(), sqlite_source_id();";
        using var reader = command.ExecuteReader();
        Assert.AreEqual(2, reader.FieldCount);
        Assert.IsTrue(reader.Read());
        var version = reader.GetString(0);
        var source = reader.GetString(1);
        Console.WriteLine("Actual initialization provenance: " + JsonSerializer.Serialize(new
        {
            Version = version, Source = source,
            Driver = typeof(SqliteConnection).Assembly.FullName, Runtime = Environment.Version.ToString()
        }));
        Assert.IsFalse(reader.Read());
        return version;
    }

    private static (List<object[]> Rows, Type[] Types) ReadRows(SqliteCommand command, string purpose, int expectedFields)
    {
        using var reader = command.ExecuteReader();
        Assert.AreEqual(expectedFields, reader.FieldCount);
        var rows = new List<object[]>();
        var types = new Type[reader.FieldCount];
        var names = new string[reader.FieldCount];
        var dataTypes = new string[reader.FieldCount];
        var copiedCounts = new List<int>();
        while (reader.Read())
        {
            var values = new object[reader.FieldCount];
            var copied = reader.GetValues(values);
            Assert.AreEqual(reader.FieldCount, copied);
            Assert.AreEqual(reader.FieldCount, values.Length);
            copiedCounts.Add(copied);
            for (var index = 0; index < reader.FieldCount; index++)
            {
                Assert.AreEqual(typeof(string), reader.GetFieldType(index));
                Assert.AreEqual("TEXT", reader.GetDataTypeName(index));
                Assert.IsInstanceOfType<string>(values[index]);
                types[index] = reader.GetFieldType(index);
                names[index] = reader.GetName(index);
                dataTypes[index] = reader.GetDataTypeName(index);
            }
            rows.Add(values);
        }
        Console.WriteLine("Actual initialization response: " + JsonSerializer.Serialize(new
        {
            Purpose = purpose, Sql = command.CommandText,
            Parameters = command.Parameters.Cast<SqliteParameter>().Select(parameter => new { parameter.ParameterName, parameter.Value }).ToArray(),
            FieldCount = reader.FieldCount,
            Columns = Enumerable.Range(0, reader.FieldCount).Select(index => new
            {
                Name = names[index], Type = types[index]?.FullName, DataType = dataTypes[index]
            }).ToArray(),
            Rows = rows, CopiedCounts = copiedCounts
        }));
        Assert.IsTrue(rows.Count > 0, "The native request must return actual rows; no padding is allowed.");
        return (rows, types);
    }

    private sealed record Inputs(object[] OneColumnArguments, object[] TwoColumnArguments, Type[] ArgumentTypes,
        string[] OneParameterOracle, string[] TwoParameterOracle);
}
