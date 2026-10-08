using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.Json;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using IQToolkit.Data.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IQToolkitTest.RealProvider;

[TestClass]
public class ImplicitMappingIdentityTests
{
    // This declaration describes the acquired native name columns. Tests never
    // construct entity records or substitute rows for the installed engine.
    public sealed class NativeNameProjection
    {
        public string Name { get; set; }
    }

    [TestMethod]
    public void SingleNativeTableRetainsItsResults()
    {
        using var source = new NativeSources();
        var provider = source.CreateProvider(new ImplicitMapping());
        var table = provider.GetTable<NativeNameProjection>(source.FunctionsId);

        CollectionAssert.AreEquivalent(source.Functions.ToArray(), table.Select(row => row.Name).ToArray());
        StringAssert.Contains(source.Log.ToString(), source.FunctionsId);
    }

    [TestMethod]
    public void SameProjectionHasDistinctNativeTableEntities()
    {
        using var source = new NativeSources();
        var mapping = new ImplicitMapping();
        var functions = mapping.GetEntity(typeof(NativeNameProjection), source.FunctionsId);
        var modules = mapping.GetEntity(typeof(NativeNameProjection), source.ModulesId);

        Assert.AreEqual(source.FunctionsId, functions.TableId);
        Assert.AreEqual(source.ModulesId, modules.TableId);
        Assert.AreNotSame(functions, modules);
    }

    [TestMethod]
    public void FunctionsThenModulesExecuteTheirOwnNativeTables()
        => VerifyBothOrders(functionsFirst: true);

    [TestMethod]
    public void ModulesThenFunctionsExecuteTheirOwnNativeTables()
        => VerifyBothOrders(functionsFirst: false);

    [TestMethod]
    public void RepeatedResolvedPairReturnsCanonicalEntityAndTable()
    {
        using var source = new NativeSources();
        var mapping = new ImplicitMapping();
        var provider = source.CreateProvider(mapping);
        var first = mapping.GetEntity(typeof(NativeNameProjection), source.FunctionsId);

        Assert.AreSame(first, mapping.GetEntity(typeof(NativeNameProjection), source.FunctionsId));
        Assert.AreSame(provider.GetTable<NativeNameProjection>(source.FunctionsId),
            provider.GetTable<NativeNameProjection>(source.FunctionsId));
    }

    [TestMethod]
    public void OmittedIdResolvesDefaultAfterAnotherExplicitNativeTable()
    {
        using var source = new NativeSources();
        var mapping = new NativeDefaultMapping(source.ModulesId);
        var provider = source.CreateProvider(mapping);
        var functions = provider.GetTable<NativeNameProjection>(source.FunctionsId);
        CollectionAssert.AreEquivalent(source.Functions.ToArray(), functions.Select(row => row.Name).ToArray());

        // The omitted-ID public API invokes the existing default resolver.
        var defaultTable = provider.GetTable<NativeNameProjection>();
        CollectionAssert.AreEquivalent(source.Modules.ToArray(), defaultTable.Select(row => row.Name).ToArray());
        Assert.AreSame(defaultTable, provider.GetTable<NativeNameProjection>(source.ModulesId));
        Assert.AreEqual(source.ModulesId, mapping.GetEntity(typeof(NativeNameProjection)).TableId);
        StringAssert.Contains(source.Log.ToString(), source.ModulesId);
    }

    private static void VerifyBothOrders(bool functionsFirst)
    {
        using var source = new NativeSources();
        var provider = source.CreateProvider(new ImplicitMapping());
        var firstId = functionsFirst ? source.FunctionsId : source.ModulesId;
        var secondId = functionsFirst ? source.ModulesId : source.FunctionsId;
        var firstExpected = functionsFirst ? source.Functions : source.Modules;
        var secondExpected = functionsFirst ? source.Modules : source.Functions;
        var first = provider.GetTable<NativeNameProjection>(firstId);
        CollectionAssert.AreEquivalent(firstExpected.ToArray(), first.Select(row => row.Name).ToArray());
        var second = provider.GetTable<NativeNameProjection>(secondId);

        CollectionAssert.AreEquivalent(secondExpected.ToArray(), second.Select(row => row.Name).ToArray());
        CollectionAssert.AreEquivalent(firstExpected.ToArray(), first.Select(row => row.Name).ToArray());
        Assert.AreNotSame(first, second);
        Assert.AreSame(first, provider.GetTable<NativeNameProjection>(firstId));
        Assert.AreSame(second, provider.GetTable<NativeNameProjection>(secondId));
        StringAssert.Contains(source.Log.ToString(), firstId);
        StringAssert.Contains(source.Log.ToString(), secondId);
        Console.WriteLine("Mapped native SQL: " + source.Log);
    }

    private sealed class NativeDefaultMapping : ImplicitMapping
    {
        private readonly string _nativeTableId;

        internal NativeDefaultMapping(string nativeTableId) => _nativeTableId = nativeTableId;

        public override string GetTableId(Type entityType) => _nativeTableId;
    }

    private sealed class NativeSources : IDisposable
    {
        internal string FunctionsId => "pragma_function_list";
        internal string ModulesId => "pragma_module_list";
        internal SqliteConnection Connection { get; } = new("Data Source=:memory:");
        internal StringWriter Log { get; } = new();
        internal List<string> Functions { get; }
        internal List<string> Modules { get; }

        internal NativeSources()
        {
            Connection.Open();
            using (var provenance = Connection.CreateCommand())
            {
                provenance.CommandText = "SELECT sqlite_version(), sqlite_source_id();";
                using var reader = provenance.ExecuteReader();
                Assert.IsTrue(reader.Read(), "Installed native provenance is required.");
                Console.WriteLine("Native SQLite version: " + reader.GetString(0));
                Console.WriteLine("Native SQLite source: " + reader.GetString(1));
            }

            // Acquire BOTH schemas before capturing rows: SQLite can register
            // pragma virtual tables while preparing these actual requests.
            AcquireSchema("SELECT * FROM pragma_function_list LIMIT 0;");
            AcquireSchema("SELECT * FROM pragma_module_list LIMIT 0;");
            Functions = ReadNames("SELECT name FROM pragma_function_list;");
            Modules = ReadNames("SELECT name FROM pragma_module_list;");
            Assert.IsTrue(Functions.Count > 0 && Modules.Count > 0, "Both real native sources are required.");
            Assert.IsFalse(Functions.OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(Modules.OrderBy(name => name, StringComparer.Ordinal)),
                "Native sources must have distinguishable complete multisets; no fabricated fallback is allowed.");
        }

        internal SQLiteQueryProvider CreateProvider(QueryMapping mapping)
            => new(Connection, mapping, QueryPolicy.Default) { Log = Log };

        private void AcquireSchema(string statement)
        {
            using var command = Connection.CreateCommand();
            command.CommandText = statement;
            using var reader = command.ExecuteReader();
            var columns = reader.GetColumnSchema();
            Console.WriteLine("Native schema request: " + statement);
            foreach (var column in columns)
            {
                Console.WriteLine("Native column: " + column.ColumnName + " " + column.DataType);
            }
            Assert.IsTrue(columns.Any(column => column.ColumnName == "name" && column.DataType == typeof(string)),
                "The installed schema must supply the projected string name column.");
        }

        private List<string> ReadNames(string statement)
        {
            using var command = Connection.CreateCommand();
            command.CommandText = statement;
            using var reader = command.ExecuteReader();
            var rows = new List<string>();
            while (reader.Read())
            {
                rows.Add(reader.GetString(0));
            }
            Console.WriteLine("Native request: " + statement);
            Console.WriteLine("Native rows: " + JsonSerializer.Serialize(rows));
            return rows;
        }

        public void Dispose()
        {
            Console.WriteLine("Mapped native SQL: " + Log);
            Log.Dispose();
            Connection.Dispose();
        }
    }
}
