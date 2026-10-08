using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using IQToolkit;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using IQToolkit.Data.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NativeContext = IQToolkitTest.RealProvider.NativeProviderCacheTests.CompileOptionsContext;
using NativeEntry = IQToolkitTest.RealProvider.NativeProviderCacheTests.NativeMetadataEntry;

namespace IQToolkitTest.RealProvider;

[TestClass]
public class NativeNullArrayMembershipTests
{
    [TestMethod]
    public void NativeNullArrayInputsAreAcquired()
    {
        var inputs = AcquireInputs();
        // This null is supplied by the human's boundary contract, not acquired native data.
        string[] selected = null;
        Expression<Func<NativeEntry, bool>> predicate = row => selected.Contains(row.Name);
        Assert.IsNull(selected);
        RequireBinding(predicate, nameof(selected));
        Console.WriteLine("Actual acquisition scope: " + JsonSerializer.Serialize(new
        {
            Boundary = "Requirement-supplied captured string[] null",
            NullBoundaryIsNativeData = false, NativeNonemptyRows = inputs.Nonempty, NativeEmptyRows = inputs.Empty,
            Table = inputs.Table, Column = inputs.Column,
            ToolkitQueryExecuted = false, NullMembershipBehaviorVerified = false
        }));
    }

    [TestMethod]
    public void CompilerNullArrayMembershipObservesReplacementWithoutCache() => VerifyReplacement(cached: false);

    [TestMethod]
    public void CompilerNullArrayMembershipObservesReplacementWithCache() => VerifyReplacement(cached: true);

    private static void VerifyReplacement(bool cached)
    {
        var inputs = AcquireInputs();
        using var connection = new SqliteConnection("Data Source=:memory:");
        using var log = new StringWriter();
        var mapping = new AttributeMapping(typeof(NativeContext));
        var entity = mapping.GetEntity(typeof(NativeEntry), nameof(NativeContext.Entries));
        Assert.AreEqual(inputs.Table, mapping.GetTableName(entity));
        Assert.AreEqual(inputs.Column, mapping.GetColumnName(entity, typeof(NativeEntry).GetProperty(nameof(NativeEntry.Name))));
        var provider = new SQLiteQueryProvider(connection, mapping, QueryPolicy.Default)
        {
            Cache = cached ? new QueryCache(2) : null,
            Log = log
        };
        string[] selected = null;
        Expression<Func<NativeEntry, bool>> predicate = row => selected.Contains(row.Name);
        Assert.IsNull(selected);
        RequireBinding(predicate, nameof(selected));
        var query = provider.GetTable<NativeEntry>(nameof(NativeContext.Entries))
            .Where(predicate).Select(row => row.Name);

        VerifyEnumeration(query, inputs.EmptyOracle, "null-initial", selected, cached, connection, log);
        selected = inputs.Nonempty;
        VerifyEnumeration(query, inputs.NonemptyOracle, "nonempty", selected, cached, connection, log);
        selected = inputs.Empty;
        VerifyEnumeration(query, inputs.EmptyOracle, "empty", selected, cached, connection, log);
        selected = inputs.Nonempty;
        VerifyEnumeration(query, inputs.NonemptyOracle, "nonempty-restored", selected, cached, connection, log);
        selected = null;
        VerifyEnumeration(query, inputs.EmptyOracle, "null-restored", selected, cached, connection, log);
        CollectionAssert.AreEquivalent(inputs.NonemptyOracle, inputs.Nonempty);
        CollectionAssert.AreEquivalent(inputs.EmptyOracle, inputs.Empty);
    }

    private static void VerifyEnumeration(IQueryable<string> query, string[] expected, string capture,
        string[] selected, bool cached, SqliteConnection connection, StringWriter log)
    {
        var before = connection.State;
        Assert.AreEqual(System.Data.ConnectionState.Closed, before);
        log.GetStringBuilder().Clear();
        var actual = query.ToArray();
        var after = connection.State;
        Console.WriteLine("Actual null membership enumeration: " + JsonSerializer.Serialize(new
        {
            Capture = capture, CaptureIsNull = selected is null, CapturedRows = selected,
            ActualRows = actual, ExpectedRows = expected, Sql = log.ToString(),
            ConnectionStateBefore = before.ToString(), ConnectionStateAfter = after.ToString(), Cached = cached
        }));
        CollectionAssert.AreEquivalent(expected, actual);
        Assert.AreEqual(System.Data.ConnectionState.Closed, after);
        StringAssert.Contains(log.ToString(), "pragma_compile_options");
        StringAssert.Contains(log.ToString(), "WHERE");
    }

    private static Inputs AcquireInputs()
    {
        var options = SQLiteMetadata.ReadValues("PRAGMA compile_options;");
        Assert.IsTrue(options.Count >= 2, "Two distinct actual native options are required; no padding is allowed.");
        Assert.AreEqual(options.Count, options.Distinct(StringComparer.Ordinal).Count());

        var mapping = new AttributeMapping(typeof(NativeContext));
        var entity = mapping.GetEntity(typeof(NativeEntry), nameof(NativeContext.Entries));
        Assert.AreEqual("pragma_compile_options", mapping.GetTableName(entity));
        Assert.AreEqual("compile_options", mapping.GetColumnName(entity, typeof(NativeEntry).GetProperty(nameof(NativeEntry.Name))));

        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        RecordProvenance(connection);
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT compile_options FROM pragma_compile_options;";
            var independent = ReadRows(command, "independent native schema and rows", requireTextSchema: true);
            CollectionAssert.AreEquivalent(options.ToArray(), independent);
        }

        string[] nonempty;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT compile_options FROM pragma_compile_options WHERE compile_options = @selected;";
            command.Parameters.AddWithValue("@selected", options[0]);
            nonempty = ReadRows(command, "acquired nonempty capture", requireTextSchema: true);
            Assert.IsTrue(nonempty.All(value => StringComparer.Ordinal.Equals(value, options[0])));
        }
        string[] nonemptyOracle;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT compile_options FROM pragma_compile_options WHERE compile_options IN (@observed);";
            command.Parameters.AddWithValue("@observed", options[0]);
            nonemptyOracle = ReadRows(command, "independent nonempty membership oracle", requireTextSchema: true);
            CollectionAssert.AreEquivalent(nonempty, nonemptyOracle);
        }

        string[] empty;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT compile_options FROM pragma_compile_options WHERE compile_options = @first AND compile_options = @second;";
            command.Parameters.AddWithValue("@first", options[0]);
            command.Parameters.AddWithValue("@second", options[1]);
            empty = ReadRows(command, "acquired zero-row capture", requireTextSchema: false);
            Assert.AreEqual(0, empty.Length);
        }
        string[] emptyOracle;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT compile_options FROM pragma_compile_options WHERE compile_options = @observed AND compile_options <> @observed;";
            command.Parameters.AddWithValue("@observed", options[0]);
            emptyOracle = ReadRows(command, "independent zero-row membership oracle", requireTextSchema: false);
            Assert.AreEqual(0, emptyOracle.Length);
            CollectionAssert.AreEquivalent(empty, emptyOracle);
        }
        return new Inputs(nonempty, nonemptyOracle, empty, emptyOracle, mapping.GetTableName(entity),
            mapping.GetColumnName(entity, typeof(NativeEntry).GetProperty(nameof(NativeEntry.Name))));
    }

    private static void RecordProvenance(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT sqlite_version(), sqlite_source_id();";
        using var reader = command.ExecuteReader();
        Assert.AreEqual(2, reader.FieldCount);
        Assert.IsTrue(reader.Read(), "Actual native SQLite version/source metadata is required.");
        var version = reader.GetString(0);
        var source = reader.GetString(1);
        Console.WriteLine("Actual native provenance: " + JsonSerializer.Serialize(new
        {
            Sql = command.CommandText, Version = version, Source = source,
            Driver = typeof(SqliteConnection).Assembly.FullName, Runtime = Environment.Version.ToString()
        }));
        Assert.IsFalse(reader.Read());
    }

    private static string[] ReadRows(SqliteCommand command, string purpose, bool requireTextSchema)
    {
        using var reader = command.ExecuteReader();
        Assert.AreEqual(1, reader.FieldCount);
        var name = reader.GetName(0);
        var type = reader.GetFieldType(0);
        var dataType = reader.GetDataTypeName(0);
        Assert.AreEqual("compile_options", name);
        var rows = new List<string>();
        while (reader.Read())
            rows.Add(reader.GetString(0));
        Console.WriteLine("Actual native response: " + JsonSerializer.Serialize(new
        {
            Purpose = purpose, Sql = command.CommandText,
            Parameters = command.Parameters.Cast<SqliteParameter>().Select(parameter => new { parameter.ParameterName, parameter.Value }).ToArray(),
            Rows = rows, FieldCount = reader.FieldCount, Name = name,
            ObservedFieldType = type.FullName, ObservedDataType = dataType,
            Driver = typeof(SqliteConnection).Assembly.FullName, Runtime = Environment.Version.ToString()
        }));
        if (requireTextSchema)
        {
            Assert.IsTrue(rows.Count > 0, "Actual native nonempty data is required; no fallback is allowed.");
            Assert.AreEqual(typeof(string), type);
            Assert.AreEqual("TEXT", dataType);
        }
        // Zero-row reader types are observations, not prerequisites or inferred string types.
        return rows.ToArray();
    }

    private static void RequireBinding(Expression<Func<NativeEntry, bool>> predicate, string captureName)
    {
        Assert.IsInstanceOfType<MethodCallExpression>(predicate.Body);
        var call = (MethodCallExpression)predicate.Body;
        Assert.AreEqual(typeof(MemoryExtensions), call.Method.DeclaringType);
        Assert.AreEqual("Contains", call.Method.Name);
        Assert.AreEqual(typeof(bool), call.Type);
        Assert.IsTrue(call.Method.IsGenericMethod);
        Assert.AreEqual(1, call.Method.GetGenericArguments().Length);
        Assert.AreEqual(typeof(string), call.Method.GetGenericArguments()[0]);
        Assert.AreEqual(2, call.Method.GetParameters().Length);
        Assert.AreEqual(typeof(ReadOnlySpan<string>), call.Method.GetParameters()[0].ParameterType);
        Assert.AreEqual(typeof(string), call.Method.GetParameters()[1].ParameterType);
        Assert.AreEqual(2, call.Arguments.Count);
        Assert.IsInstanceOfType<MemberExpression>(call.Arguments[1]);
        var member = (MemberExpression)call.Arguments[1];
        Assert.AreEqual(nameof(NativeEntry.Name), member.Member.Name);
        Assert.AreEqual(typeof(string), member.Type);
        Assert.AreSame(predicate.Parameters[0], member.Expression);
        Assert.AreEqual(typeof(NativeEntry), predicate.Parameters[0].Type);

        Assert.IsInstanceOfType<MethodCallExpression>(call.Arguments[0]);
        var conversion = (MethodCallExpression)call.Arguments[0];
        Assert.AreEqual("op_Implicit", conversion.Method.Name);
        Assert.AreEqual(typeof(ReadOnlySpan<string>), conversion.Method.DeclaringType);
        Assert.AreEqual(typeof(ReadOnlySpan<string>), conversion.Type);
        Assert.AreEqual(1, conversion.Method.GetParameters().Length);
        Assert.AreEqual(typeof(string[]), conversion.Method.GetParameters()[0].ParameterType);
        Assert.AreEqual(1, conversion.Arguments.Count);
        Assert.AreEqual(typeof(string[]), conversion.Arguments[0].Type);
        Assert.IsInstanceOfType<MemberExpression>(conversion.Arguments[0]);
        var captured = (MemberExpression)conversion.Arguments[0];
        Assert.AreEqual(captureName, captured.Member.Name);
        Assert.IsInstanceOfType<FieldInfo>(captured.Member);
        Assert.IsInstanceOfType<ConstantExpression>(captured.Expression);
        Assert.IsNull(((FieldInfo)captured.Member).GetValue(((ConstantExpression)captured.Expression).Value));
        Console.WriteLine("Actual compiler binding: " + JsonSerializer.Serialize(new
        {
            Membership = call.Method.ToString(), MembershipAssembly = call.Method.DeclaringType.Assembly.FullName,
            MembershipModule = call.Method.Module.ModuleVersionId, MembershipToken = call.Method.MetadataToken,
            Conversion = conversion.Method.ToString(), ConversionAssembly = conversion.Method.DeclaringType.Assembly.FullName,
            ConversionModule = conversion.Method.Module.ModuleVersionId, ConversionToken = conversion.Method.MetadataToken,
            ParameterType = predicate.Parameters[0].Type.FullName, ParameterMember = member.Member.ToString(),
            CaptureField = captured.Member.ToString(), CaptureIsRequirementSuppliedNull = true
        }));
    }

    private sealed class Inputs
    {
        internal Inputs(string[] nonempty, string[] nonemptyOracle, string[] empty, string[] emptyOracle,
            string table, string column)
        {
            Nonempty = nonempty;
            NonemptyOracle = nonemptyOracle;
            Empty = empty;
            EmptyOracle = emptyOracle;
            Table = table;
            Column = column;
        }

        internal string[] Nonempty { get; }
        internal string[] NonemptyOracle { get; }
        internal string[] Empty { get; }
        internal string[] EmptyOracle { get; }
        internal string Table { get; }
        internal string Column { get; }
    }
}
