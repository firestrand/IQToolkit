using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.Json;
using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using IQToolkit.Data.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IQToolkitTest.RealProvider;

[TestClass]
public class NativeProviderCacheTests
{
    // These CLR projections describe installed native metadata schemas.
    // The tests never construct replacement metadata records.
    public sealed class NativeMetadataEntry
    {
        public string Name { get; set; }
    }

    public sealed class CompileOptionsContext
    {
        [Table(Name = "pragma_compile_options")]
        [Column(Member = "Name", Name = "compile_options")]
        public IEntityTable<NativeMetadataEntry> Entries { get; set; }
    }

    public sealed class FunctionsContext
    {
        [Table(Name = "pragma_function_list")]
        [Column(Member = "Name", Name = "name")]
        public IEntityTable<NativeMetadataEntry> Entries { get; set; }
    }

    [TestMethod]
    public void SharedCacheKeepsNativeProviderMappingsSeparate()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        RecordProvenance();
        VerifySchema(connection, "SELECT * FROM pragma_compile_options LIMIT 0;", "compile_options");
        VerifySchema(connection, "SELECT * FROM pragma_function_list LIMIT 0;", "name");
        var options = ReadRows(connection, "SELECT compile_options FROM pragma_compile_options;");
        var functions = ReadRows(connection, "SELECT name FROM pragma_function_list;");
        Assert.IsTrue(options.Count > 0 && functions.Count > 0, "Both native sources are required.");

        var cache = new QueryCache(2);
        using var optionsLog = new StringWriter();
        using var functionsLog = new StringWriter();
        var optionsProvider = new SQLiteQueryProvider(connection,
            new AttributeMapping(typeof(CompileOptionsContext)), QueryPolicy.Default)
        {
            Cache = cache,
            Log = optionsLog
        };
        var functionsProvider = new SQLiteQueryProvider(connection,
            new AttributeMapping(typeof(FunctionsContext)), QueryPolicy.Default)
        {
            Cache = cache,
            Log = functionsLog
        };
        var optionQuery = optionsProvider.GetTable<NativeMetadataEntry>("Entries").Select(row => row.Name);
        var functionQuery = functionsProvider.GetTable<NativeMetadataEntry>("Entries").Select(row => row.Name);

        CollectionAssert.AreEquivalent(options.ToArray(), optionQuery.ToArray());
        CollectionAssert.AreEquivalent(functions.ToArray(), functionQuery.ToArray());
        CollectionAssert.AreEquivalent(options.ToArray(), optionQuery.ToArray());
        CollectionAssert.AreEquivalent(functions.ToArray(), functionQuery.ToArray());
        Assert.AreEqual(2, cache.Count);
        cache.Clear();
        CollectionAssert.AreEquivalent(functions.ToArray(), functionQuery.ToArray());
        CollectionAssert.AreEquivalent(options.ToArray(), optionQuery.ToArray());
        CollectionAssert.AreEquivalent(functions.ToArray(), functionQuery.ToArray());
        CollectionAssert.AreEquivalent(options.ToArray(), optionQuery.ToArray());
        Assert.AreEqual(2, cache.Count);
        StringAssert.Contains(optionsLog.ToString(), "pragma_compile_options");
        StringAssert.Contains(functionsLog.ToString(), "pragma_function_list");
        Console.WriteLine("Native options execution: " + optionsLog);
        Console.WriteLine("Native functions execution: " + functionsLog);
    }

    [TestMethod]
    public void SharedCacheKeepsDistinctNativeProviderPoliciesSeparate()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        RecordProvenance();
        VerifySchema(connection, "SELECT * FROM pragma_compile_options LIMIT 0;", "compile_options");
        var options = ReadRows(connection, "SELECT compile_options FROM pragma_compile_options;");
        Assert.IsTrue(options.Distinct(StringComparer.Ordinal).Count() >= 2,
            "Two distinct native options are required; no fabricated fallback is allowed.");
        var firstName = options[0];
        var secondName = options.First(name => !StringComparer.Ordinal.Equals(name, firstName));
        var firstExpected = ReadRows(connection,
            "SELECT compile_options FROM pragma_compile_options WHERE compile_options = @name;", firstName);
        var secondExpected = ReadRows(connection,
            "SELECT compile_options FROM pragma_compile_options WHERE compile_options = @name;", secondName);
        Assert.IsTrue(firstExpected.Count > 0 && secondExpected.Count > 0);

        var firstPolicy = new EntityPolicy();
        firstPolicy.Apply<NativeMetadataEntry>(rows => rows.Where(row => row.Name == firstName));
        var secondPolicy = new EntityPolicy();
        secondPolicy.Apply<NativeMetadataEntry>(rows => rows.Where(row => row.Name == secondName));
        var mapping = new AttributeMapping(typeof(CompileOptionsContext));
        var cache = new QueryCache(2);
        using var firstLog = new StringWriter();
        using var secondLog = new StringWriter();
        var firstProvider = new SQLiteQueryProvider(connection, mapping, firstPolicy)
        {
            Cache = cache,
            Log = firstLog
        };
        var secondProvider = new SQLiteQueryProvider(connection, mapping, secondPolicy)
        {
            Cache = cache,
            Log = secondLog
        };
        var firstQuery = firstProvider.GetTable<NativeMetadataEntry>("Entries").Select(row => row.Name);
        var secondQuery = secondProvider.GetTable<NativeMetadataEntry>("Entries").Select(row => row.Name);

        CollectionAssert.AreEquivalent(firstExpected.ToArray(), firstQuery.ToArray());
        CollectionAssert.AreEquivalent(secondExpected.ToArray(), secondQuery.ToArray());
        CollectionAssert.AreEquivalent(firstExpected.ToArray(), firstQuery.ToArray());
        CollectionAssert.AreEquivalent(secondExpected.ToArray(), secondQuery.ToArray());
        Assert.AreEqual(2, cache.Count);
        cache.Clear();
        CollectionAssert.AreEquivalent(secondExpected.ToArray(), secondQuery.ToArray());
        CollectionAssert.AreEquivalent(firstExpected.ToArray(), firstQuery.ToArray());
        CollectionAssert.AreEquivalent(secondExpected.ToArray(), secondQuery.ToArray());
        CollectionAssert.AreEquivalent(firstExpected.ToArray(), firstQuery.ToArray());
        Assert.AreEqual(2, cache.Count);
        StringAssert.Contains(firstLog.ToString(), "WHERE");
        StringAssert.Contains(secondLog.ToString(), "WHERE");
        Console.WriteLine("First native policy execution: " + firstLog);
        Console.WriteLine("Second native policy execution: " + secondLog);
    }

    private static void RecordProvenance()
        => SQLiteMetadata.ReadValues("SELECT sqlite_version();");

    private static void VerifySchema(SqliteConnection connection, string statement, string expectedColumn)
    {
        using var command = connection.CreateCommand();
        command.CommandText = statement;
        using var reader = command.ExecuteReader();
        var columns = reader.GetColumnSchema();
        Console.WriteLine("Native schema request: " + statement);
        foreach (var column in columns)
        {
            Console.WriteLine("Native column: " + column.ColumnName + " " + column.DataType);
        }
        Assert.IsTrue(columns.Any(column => column.ColumnName == expectedColumn && column.DataType == typeof(string)),
            "The installed native schema must supply the mapped string column.");
    }

    private static List<string> ReadRows(SqliteConnection connection, string statement, string observedName = null)
    {
        using var command = connection.CreateCommand();
        command.CommandText = statement;
        if (observedName != null)
        {
            command.Parameters.AddWithValue("@name", observedName);
        }
        using var reader = command.ExecuteReader();
        var rows = new List<string>();
        while (reader.Read())
        {
            rows.Add(reader.GetString(0));
        }
        Console.WriteLine("Native oracle request: " + statement);
        Console.WriteLine("Native oracle parameter: " + observedName);
        Console.WriteLine("Native oracle rows: " + JsonSerializer.Serialize(rows));
        return rows;
    }
}
