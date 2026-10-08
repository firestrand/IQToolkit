using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
public class NativeArrayMembershipTests
{
    [TestMethod]
    public void NativeArrayInputsAreAcquired()
    {
        var inputs = AcquireInputs();
        string[] selected = inputs.First;
        Expression<Func<NativeEntry, bool>> compiler = row => selected.Contains(row.Name);
        Expression<Func<NativeEntry, bool>> enumerable = row => Enumerable.Contains(selected, row.Name);
        RequireBinding(compiler, true);
        RequireBinding(enumerable, false);
    }

    [TestMethod]
    public void CompilerArrayMembershipObservesReplacementWithoutCache() => VerifyReplacement(true, false);

    [TestMethod]
    public void EnumerableArrayMembershipObservesReplacementWithoutCache() => VerifyReplacement(false, false);

    [TestMethod]
    public void CompilerArrayMembershipObservesReplacementWithCache() => VerifyReplacement(true, true);

    [TestMethod]
    public void EnumerableArrayMembershipObservesReplacementWithCache() => VerifyReplacement(false, true);

    private static void VerifyReplacement(bool compilerBinding, bool cached)
    {
        var inputs = AcquireInputs();
        using var connection = new SqliteConnection("Data Source=:memory:");
        using var log = new StringWriter();
        var mapping = new AttributeMapping(typeof(NativeContext));
        var entity = mapping.GetEntity(typeof(NativeEntry), nameof(NativeContext.Entries));
        Assert.AreEqual("pragma_compile_options", mapping.GetTableName(entity));
        Assert.AreEqual("compile_options", mapping.GetColumnName(entity, typeof(NativeEntry).GetProperty(nameof(NativeEntry.Name))));
        var provider = new SQLiteQueryProvider(connection, mapping, QueryPolicy.Default)
        {
            Cache = cached ? new QueryCache(2) : null,
            Log = log
        };
        string[] selected = inputs.First;
        Expression<Func<NativeEntry, bool>> predicate;
        if (compilerBinding)
            predicate = row => selected.Contains(row.Name);
        else
            predicate = row => Enumerable.Contains(selected, row.Name);
        RequireBinding(predicate, compilerBinding);
        var query = provider.GetTable<NativeEntry>(nameof(NativeContext.Entries))
            .Where(predicate).Select(row => row.Name);

        VerifyEnumeration(query, inputs.FirstExpected, "A", provider, connection, log);
        selected = inputs.Second;
        VerifyEnumeration(query, inputs.SecondExpected, "B", provider, connection, log);
        selected = inputs.First;
        VerifyEnumeration(query, inputs.FirstExpected, "A-restored", provider, connection, log);
        CollectionAssert.AreEquivalent(inputs.FirstExpected, inputs.First);
        CollectionAssert.AreEquivalent(inputs.SecondExpected, inputs.Second);
    }

    private static void VerifyEnumeration(IQueryable<string> query, string[] expected, string capture,
        SQLiteQueryProvider provider, SqliteConnection connection, StringWriter log)
    {
        Assert.AreEqual(System.Data.ConnectionState.Closed, connection.State);
        log.GetStringBuilder().Clear();
        var actual = query.ToArray();
        CollectionAssert.AreEquivalent(expected, actual);
        Assert.AreEqual(System.Data.ConnectionState.Closed, connection.State);
        StringAssert.Contains(log.ToString(), "pragma_compile_options");
        StringAssert.Contains(log.ToString(), "WHERE");
        Console.WriteLine("Actual query enumeration: " + JsonSerializer.Serialize(new
        {
            Capture = capture, Rows = actual, Sql = log.ToString(),
            CacheEntries = provider.Cache?.Count, ConnectionState = connection.State.ToString()
        }));
    }

    private static Inputs AcquireInputs()
    {
        var options = SQLiteMetadata.ReadValues("PRAGMA compile_options;");
        Assert.IsTrue(options.Count >= 3, "Three actual native options are required; no padding is allowed.");
        Assert.AreEqual(options.Count, options.Distinct(StringComparer.Ordinal).Count());
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT compile_options FROM pragma_compile_options;";
            using var reader = command.ExecuteReader();
            Assert.AreEqual(1, reader.FieldCount);
            Assert.AreEqual("compile_options", reader.GetName(0));
            Assert.AreEqual(typeof(string), reader.GetFieldType(0));
            Assert.AreEqual("TEXT", reader.GetDataTypeName(0));
            Console.WriteLine("Actual native schema: " + JsonSerializer.Serialize(new
            {
                Name = reader.GetName(0), Type = reader.GetFieldType(0).FullName,
                DataType = reader.GetDataTypeName(0), Driver = typeof(SqliteConnection).Assembly.FullName,
                Runtime = Environment.Version.ToString()
            }));
            var independent = new List<string>();
            while (reader.Read())
                independent.Add(reader.GetString(0));
            CollectionAssert.AreEquivalent(options, independent);
        }
        using var firstCommand = connection.CreateCommand();
        firstCommand.CommandText = "SELECT compile_options FROM pragma_compile_options WHERE compile_options IN (@first, @second);";
        firstCommand.Parameters.AddWithValue("@first", options[0]);
        firstCommand.Parameters.AddWithValue("@second", options[1]);
        var first = ReadRows(firstCommand);
        using var secondCommand = connection.CreateCommand();
        secondCommand.CommandText = "SELECT compile_options FROM pragma_compile_options WHERE compile_options = @third;";
        secondCommand.Parameters.AddWithValue("@third", options[2]);
        var second = ReadRows(secondCommand);
        Assert.IsTrue(first.Length > 0 && second.Length > 0);
        Assert.IsFalse(first.Intersect(second, StringComparer.Ordinal).Any());
        var firstExpected = NativeOracle(connection, first);
        var secondExpected = NativeOracle(connection, second);
        CollectionAssert.AreEquivalent(first, firstExpected);
        CollectionAssert.AreEquivalent(second, secondExpected);
        return new Inputs(first, second, firstExpected, secondExpected);
    }

    private static string[] NativeOracle(SqliteConnection connection, string[] selection)
    {
        using var command = connection.CreateCommand();
        var names = new List<string>();
        for (int index = 0; index < selection.Length; index++)
        {
            string name = "@native" + index;
            names.Add(name);
            command.Parameters.AddWithValue(name, selection[index]);
        }
        command.CommandText = "SELECT compile_options FROM pragma_compile_options WHERE compile_options IN (" + string.Join(", ", names) + ");";
        return ReadRows(command);
    }

    private static string[] ReadRows(SqliteCommand command)
    {
        using var reader = command.ExecuteReader();
        var rows = new List<string>();
        while (reader.Read())
            rows.Add(reader.GetString(0));
        Console.WriteLine("Actual native response: " + JsonSerializer.Serialize(new
        {
            Sql = command.CommandText,
            Parameters = command.Parameters.Cast<SqliteParameter>().Select(parameter => new { parameter.ParameterName, parameter.Value }).ToArray(),
            Rows = rows
        }));
        return rows.ToArray();
    }

    private static void RequireBinding(Expression<Func<NativeEntry, bool>> predicate, bool compilerBinding)
    {
        Assert.IsInstanceOfType<MethodCallExpression>(predicate.Body);
        var call = (MethodCallExpression)predicate.Body;
        Assert.AreEqual(compilerBinding ? typeof(MemoryExtensions) : typeof(Enumerable), call.Method.DeclaringType);
        Assert.AreEqual("Contains", call.Method.Name);
        Assert.AreEqual(typeof(bool), call.Type);
        Assert.IsTrue(call.Method.IsGenericMethod);
        CollectionAssert.AreEqual(new[] { typeof(string) }, call.Method.GetGenericArguments());
        Assert.AreEqual(2, call.Arguments.Count);
        Assert.IsInstanceOfType<MemberExpression>(call.Arguments[1]);
        var member = (MemberExpression)call.Arguments[1];
        Assert.AreEqual(nameof(NativeEntry.Name), member.Member.Name);
        Assert.AreSame(predicate.Parameters[0], member.Expression);
        if (compilerBinding)
        {
            Assert.IsInstanceOfType<MethodCallExpression>(call.Arguments[0]);
            var conversion = (MethodCallExpression)call.Arguments[0];
            Assert.AreEqual("op_Implicit", conversion.Method.Name);
            Assert.AreEqual(typeof(ReadOnlySpan<string>), conversion.Method.DeclaringType);
            Assert.AreEqual(1, conversion.Arguments.Count);
            Assert.AreEqual(typeof(string[]), conversion.Arguments[0].Type);
            Console.WriteLine("Actual array conversion: " + conversion.Method + "; assembly: " + conversion.Method.DeclaringType.Assembly.FullName);
        }
        Console.WriteLine("Actual membership binding: " + call.Method + "; assembly: " + call.Method.DeclaringType.Assembly.FullName);
    }

    private sealed class Inputs
    {
        internal Inputs(string[] first, string[] second, string[] firstExpected, string[] secondExpected)
        {
            First = first;
            Second = second;
            FirstExpected = firstExpected;
            SecondExpected = secondExpected;
        }

        internal string[] First { get; }
        internal string[] Second { get; }
        internal string[] FirstExpected { get; }
        internal string[] SecondExpected { get; }
    }
}
