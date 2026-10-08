using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using IQToolkit;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using IQToolkit.Data.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IQToolkitTest.RealProvider;

[TestClass]
public class NativeStringColumnInequalityTests
{
    private const string NativeTable = "pragma_compile_options";
    private const string AllSql = "SELECT name, dflt_value, arg FROM pragma_table_xinfo(@table);";
    private const string HiddenSql = "SELECT name, dflt_value, arg FROM pragma_table_xinfo WHERE arg = @table;";
    private const string BareSql = "SELECT name, dflt_value, arg FROM pragma_table_xinfo(@table) WHERE dflt_value <> name;";
    private const string CandidateSql = "SELECT name, dflt_value, arg FROM pragma_table_xinfo(@table) WHERE dflt_value <> name OR (dflt_value IS NULL AND name IS NOT NULL) OR (dflt_value IS NOT NULL AND name IS NULL);";

    // These members configure the existing native schema's CLR projection.
    // No replacement metadata records are constructed by the tests.
    public sealed class NativeColumnMetadataRow
    {
        public string Name { get; set; }
        public string DefaultValue { get; set; }
        public string Argument { get; set; }
    }

    public sealed class NativeColumnMetadataContext
    {
        [Table(Name = "pragma_table_xinfo")]
        [Column(Member = "Name", Name = "name")]
        [Column(Member = "DefaultValue", Name = "dflt_value")]
        [Column(Member = "Argument", Name = "arg")]
        public IEntityTable<NativeColumnMetadataRow> Entries { get; set; }
    }

    [TestMethod]
    public void NativeStringColumnsPreserveProjectionAndLiteralNull()
    {
        using var source = NativeSource.Acquire();
        source.VerifyMaterializationAndBinding();
        var literal = source.Run("literal-null", source.Rows.Where(row => row.DefaultValue == null).Select(row => row.Name));
        var expected = source.All.Rows.Where(row => row[1] is DBNull).Select(row => (string)row[0]).ToArray();
        source.Observe(nameof(NativeStringColumnsPreserveProjectionAndLiteralNull), new { ExpectedNames = expected, ActualNames = literal });
        CollectionAssert.AreEquivalent(expected, literal, "The existing literal-null predicate must preserve actual native NULL rows.");
    }

    [TestMethod]
    public void NativeStringColumnInequalityFiltersNullRow()
    {
        using var source = NativeSource.Acquire();
        source.VerifyMaterializationAndBinding();
        Expression<Func<NativeColumnMetadataRow, bool>> inequality = row => row.DefaultValue != row.Name;
        source.RecordExpression("original-compiler-predicate", inequality);
        var actual = source.Run("column-inequality-predicate", source.Rows.Where(inequality).Select(row => row.Name));
        var expected = source.ClrNames;
        source.Observe(nameof(NativeStringColumnInequalityFiltersNullRow), new { ExpectedNames = expected, ActualNames = actual });
        Assert.AreEqual(expected.Length, actual.Length, "SQLite string-column inequality must preserve the acquired CLR selection.");
        CollectionAssert.AreEquivalent(expected, actual, "SQLite string-column inequality must preserve the acquired CLR selection.");
    }

    [TestMethod]
    public void NativeStringColumnInequalityProjectsClrResult()
    {
        using var source = NativeSource.Acquire();
        source.VerifyMaterializationAndBinding();
        Expression<Func<NativeColumnMetadataRow, bool>> inequality = row => row.DefaultValue != row.Name;
        source.RecordExpression("original-compiler-projection", inequality);
        bool canBeColumn = SQLiteLanguage.Default.CanBeColumn(inequality.Body);
        Raw(new { Stage = "projection-placement", CanBeColumn = canBeColumn, MustBeColumn = SQLiteLanguage.Default.MustBeColumn(inequality.Body) });
        Assert.IsFalse(canBeColumn, "Prerequisite: the ordinary inequality projection retains the existing client boundary.");
        var actual = source.Run("ordinary-client-projection", source.Rows.Select(inequality));
        var expected = source.All.Rows.Select(NativeUnequal).ToArray();
        source.Observe(nameof(NativeStringColumnInequalityProjectsClrResult), new { ExpectedBooleans = expected, ActualBooleans = actual, CanBeColumn = canBeColumn });
        CollectionAssert.AreEquivalent(expected, actual, "Ordinary client projection must preserve acquired CLR inequality results.");
    }

    [TestMethod]
    public void NativeStringColumnInequalityPreservesAcquiredControls()
    {
        using var source = NativeSource.Acquire();
        source.VerifyMaterializationAndBinding();
        // Explicit operator configuration avoids a C# same-variable warning;
        // both operands still refer to the same actual mapped native column.
        Expression<Func<NativeColumnMetadataRow, string>> name = row => row.Name;
        Expression<Func<NativeColumnMetadataRow, string>> defaultValue = row => row.DefaultValue;
        var nameSelf = Expression.Lambda<Func<NativeColumnMetadataRow, bool>>(Expression.NotEqual(name.Body, name.Body), name.Parameters);
        var defaultSelf = Expression.Lambda<Func<NativeColumnMetadataRow, bool>>(Expression.NotEqual(defaultValue.Body, defaultValue.Body), defaultValue.Parameters);
        Expression<Func<NativeColumnMetadataRow, bool>> nonNull = row => row.Name != row.Argument;
        source.RecordExpression("explicit-name-self", nameSelf);
        source.RecordExpression("explicit-default-self", defaultSelf);
        source.RecordExpression("original-compiler-nonnull", nonNull);
        var nativeNameSelf = source.ReadIndependent("name-self", "SELECT name, dflt_value, arg FROM pragma_table_xinfo(@table) WHERE name <> name;");
        var nativeDefaultSelf = source.ReadIndependent("default-self", "SELECT name, dflt_value, arg FROM pragma_table_xinfo(@table) WHERE dflt_value <> dflt_value;");
        var nativeNonNull = source.ReadIndependent("nonnull", "SELECT name, dflt_value, arg FROM pragma_table_xinfo(@table) WHERE name <> arg;");
        var nativeLiteral = source.ReadIndependent("literal-null", "SELECT name, dflt_value, arg FROM pragma_table_xinfo(@table) WHERE dflt_value IS NULL;");
        var expectedNameSelf = source.All.Rows.Where(row => NativeString(row[0]) != NativeString(row[0])).Select(row => (string)row[0]).ToArray();
        var expectedDefaultSelf = source.All.Rows.Where(row => NativeString(row[1]) != NativeString(row[1])).Select(row => (string)row[0]).ToArray();
        var expectedNonNull = source.All.Rows.Where(row => NativeString(row[0]) != NativeString(row[2])).Select(row => (string)row[0]).ToArray();
        var expectedLiteral = source.All.Rows.Where(row => row[1] is DBNull).Select(row => (string)row[0]).ToArray();
        CollectionAssert.AreEquivalent(expectedNameSelf, Names(nativeNameSelf), "Prerequisite: acquired name self-comparison oracle must agree.");
        CollectionAssert.AreEquivalent(expectedDefaultSelf, Names(nativeDefaultSelf), "Prerequisite: acquired NULL self-comparison oracle must agree.");
        CollectionAssert.AreEquivalent(expectedNonNull, Names(nativeNonNull), "Prerequisite: acquired nonnull comparison oracle must agree.");
        CollectionAssert.AreEquivalent(expectedLiteral, Names(nativeLiteral), "Prerequisite: acquired literal-null oracle must agree.");
        var actualNameSelf = source.Run("name-self", source.Rows.Where(nameSelf).Select(row => row.Name));
        var actualDefaultSelf = source.Run("default-self", source.Rows.Where(defaultSelf).Select(row => row.Name));
        var actualNonNull = source.Run("nonnull", source.Rows.Where(nonNull).Select(row => row.Name));
        var actualLiteral = source.Run("literal-null", source.Rows.Where(row => row.DefaultValue == null).Select(row => row.Name));
        source.Observe(nameof(NativeStringColumnInequalityPreservesAcquiredControls), new
        {
            ExpectedNameSelf = expectedNameSelf, ActualNameSelf = actualNameSelf,
            ExpectedDefaultSelf = expectedDefaultSelf, ActualDefaultSelf = actualDefaultSelf,
            ExpectedNonNull = expectedNonNull, ActualNonNull = actualNonNull,
            ExpectedLiteral = expectedLiteral, ActualLiteral = actualLiteral
        });
        CollectionAssert.AreEquivalent(expectedNameSelf, actualNameSelf, "Actual name self-inequality must preserve the native control.");
        CollectionAssert.AreEquivalent(expectedDefaultSelf, actualDefaultSelf, "Actual NULL self-inequality must preserve the native control.");
        CollectionAssert.AreEquivalent(expectedNonNull, actualNonNull, "Actual two-nonNULL-column inequality must preserve the native control.");
        CollectionAssert.AreEquivalent(expectedLiteral, actualLiteral, "Literal-null behavior must remain unchanged.");
    }

    private static string NativeString(object value) => value is DBNull ? null : (string)value;
    private static bool NativeUnequal(object[] row) => NativeString(row[1]) != NativeString(row[0]);
    private static string[] Names(NativeResponse response) => response.Rows.Select(row => (string)row[0]).ToArray();
    private static string NativeKey(object[] row) => JsonSerializer.Serialize(row.Select(NativeString).ToArray());
    private static string MappedKey(NativeColumnMetadataRow row) => JsonSerializer.Serialize(new[] { row.Name, row.DefaultValue, row.Argument });
    private static void Raw(object observation) => Console.WriteLine("Native string raw: " + JsonSerializer.Serialize(observation));
    private static string Hash(string path) { using var file = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(file)); }

    private sealed class NativeSource : IDisposable
    {
        private readonly SqliteConnection primary = new("Data Source=:memory:;Pooling=False;Default Timeout=10");
        private readonly SqliteConnection independent = new("Data Source=:memory:;Pooling=False;Default Timeout=10");
        private readonly StringWriter log = new();
        private readonly List<object> nativeCaptures = [];
        private readonly List<object> expressions = [];
        private readonly List<object> queries = [];
        private object provenance;
        private SQLiteQueryProvider provider;
        private string argument;
        private bool materializationAndBindingPassed;
        private BinaryExpression recordedBinary;

        internal NativeResponse All { get; private set; }
        internal string[] ClrNames { get; private set; }
        internal IQueryable<NativeColumnMetadataRow> Rows { get; private set; }

        internal static NativeSource Acquire()
        {
            var source = new NativeSource();
            try { source.Initialize(); return source; }
            catch { source.Dispose(); throw; }
        }

        private void Initialize()
        {
            primary.Open();
            independent.Open();
            provenance = new
            {
                Runtime = Environment.Version.ToString(), OS = RuntimeInformation.OSDescription,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                ConnectionsDistinct = !ReferenceEquals(primary, independent), NativeHandlesDistinct = !ReferenceEquals(primary.Handle, independent.Handle),
                Managed = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.GetName().Name == "Microsoft.Data.Sqlite" || assembly.GetName().Name?.StartsWith("SQLitePCLRaw.", StringComparison.Ordinal) == true).Select(assembly => new { Name = assembly.GetName().Name, Version = assembly.GetName().Version?.ToString(), Path = assembly.Location, Sha256 = Hash(assembly.Location) }).ToArray(),
                NativeMappings = (OperatingSystem.IsLinux() ? File.ReadLines("/proc/self/maps").Select(line => line.Split(' ', 6, StringSplitOptions.RemoveEmptyEntries)).Where(fields => fields.Length == 6 && Path.GetFileName(fields[5]) == "libe_sqlite3.so").Select(fields => fields[5]).Distinct(StringComparer.Ordinal).ToArray() : Array.Empty<string>()).Select(path => new { Path = path, Sha256 = Hash(path) }).ToArray()
            };
            Raw(new { Stage = "provenance", Actual = provenance });
            Assert.AreNotSame(primary.Handle, independent.Handle, "Prerequisite: two independent nonpooled native handles are required.");
            var firstIdentity = Read(primary, "primary.identity", "SELECT sqlite_version(), sqlite_source_id();");
            var otherIdentity = Read(independent, "independent.identity", "SELECT sqlite_version(), sqlite_source_id();");
            Assert.AreEqual(2, firstIdentity.FieldCount, "Prerequisite: actual engine identity has two fields.");
            Assert.AreEqual(1, firstIdentity.Rows.Count, "Prerequisite: actual engine identity is present.");
            Assert.IsTrue(firstIdentity.Rows[0].All(value => value is string text && text.Length > 0), "Prerequisite: native engine version/source are strings.");
            CollectionAssert.AreEquivalent(firstIdentity.Rows.Select(NativeKey).ToArray(), otherIdentity.Rows.Select(NativeKey).ToArray(), "Prerequisite: independent engine identities agree.");
            Register(primary, "primary.register");
            Register(independent, "independent.register");
            All = Read(primary, "primary.all", AllSql, NativeTable);
            ValidateMetadata(All);
            Assert.IsTrue(All.Rows.Count > 0 && All.Rows.Any(row => row[1] is DBNull), "Prerequisite: genuine metadata with an actual native NULL default is required.");
            // Preserve the actual native schema distinction; do not relabel it
            // as TEXT merely because the explicit CLR projection uses string.
            Assert.AreEqual(typeof(byte[]), All.Schema[1].DataType, "Prerequisite: retain the acquired native default field's actual BLOB/Byte[] schema.");
            Assert.AreEqual("BLOB", All.Schema[1].DataTypeName, "Prerequisite: the native default schema remains BLOB.");
            argument = (string)All.Rows[0][2];
            var otherAll = ReadIndependent("all", AllSql);
            var primaryHidden = Read(primary, "primary.hidden", HiddenSql, argument);
            ValidateMetadata(primaryHidden);
            var otherHidden = ReadIndependent("hidden", HiddenSql);
            var bare = ReadIndependent("bare", BareSql);
            var candidate = ReadIndependent("compensated-candidate", CandidateSql);
            ClrNames = All.Rows.Where(NativeUnequal).Select(row => (string)row[0]).ToArray();
            Raw(new { Stage = "clr-oracles", PerRow = All.Rows.Select((row, index) => new { Ordinal = index, StringNotEqual = NativeUnequal(row) }).ToArray(), ClrNames, BareNames = Names(bare), CandidateNames = Names(candidate) });
            foreach (var response in new[] { otherAll, primaryHidden, otherHidden })
                CollectionAssert.AreEquivalent(All.Rows.Select(NativeKey).ToArray(), response.Rows.Select(NativeKey).ToArray(), "Prerequisite: independent native and hidden-argument multisets agree.");
            CollectionAssert.AreEquivalent(ClrNames, Names(candidate), "Prerequisite: independently acquired compensation candidate agrees with CLR inequality.");
            var mapping = new AttributeMapping(typeof(NativeColumnMetadataContext));
            var entity = mapping.GetEntity(typeof(NativeColumnMetadataRow), nameof(NativeColumnMetadataContext.Entries));
            Assert.AreEqual("pragma_table_xinfo", mapping.GetTableName(entity), "Prerequisite: explicit mapping targets the existing native table.");
            foreach (var pair in new[] { ("Name", "name"), ("DefaultValue", "dflt_value"), ("Argument", "arg") })
                Assert.AreEqual(pair.Item2, mapping.GetColumnName(entity, typeof(NativeColumnMetadataRow).GetProperty(pair.Item1)), "Prerequisite: native column mapping is exact.");
            provider = new SQLiteQueryProvider(primary, mapping, QueryPolicy.Default) { Cache = null, Log = log };
            string acquiredArgument = argument;
            Rows = provider.GetTable<NativeColumnMetadataRow>(nameof(NativeColumnMetadataContext.Entries)).Where(row => row.Argument == acquiredArgument);
        }

        private void Register(SqliteConnection connection, string purpose)
        {
            var schema = Read(connection, purpose, "SELECT * FROM pragma_compile_options LIMIT 0;");
            Assert.AreEqual(1, schema.FieldCount, "Prerequisite: existing native compile-option schema is present.");
            CollectionAssert.AreEqual(new[] { "compile_options" }, schema.Names, "Prerequisite: register the existing native eponymous table.");
            Assert.AreEqual(0, schema.Rows.Count, "Prerequisite: registration SELECT supplies no authored rows.");
        }

        internal NativeResponse ReadIndependent(string purpose, string sql)
        {
            var response = Read(independent, "independent." + purpose, sql, argument);
            ValidateMetadata(response);
            return response;
        }

        internal void VerifyMaterializationAndBinding()
        {
            var projected = Run("unfiltered-native-projection", Rows);
            Raw(new { Stage = "materialization", ActualRows = projected, ExpectedDecodedRows = All.Rows.Select(row => row.Select(NativeString).ToArray()).ToArray() });
            CollectionAssert.AreEquivalent(All.Rows.Select(NativeKey).ToArray(), projected.Select(MappedKey).ToArray(), "Prerequisite: native hidden-argument binding and full CLR string materialization preserve actual buffers.");
            var actualNullNames = projected.Where(row => row.DefaultValue == null).Select(row => row.Name).ToArray();
            CollectionAssert.AreEquivalent(All.Rows.Where(row => row[1] is DBNull).Select(row => (string)row[0]).ToArray(), actualNullNames, "Prerequisite: actual BLOB-schema DBNull materializes as CLR string null.");
            materializationAndBindingPassed = true;
        }

        internal T[] Run<T>(string purpose, IQueryable<T> query)
        {
            var translated = new QueryTranslator(provider.Language, provider.Mapping, provider.Policy).Translate(query.Expression);
            var predicates = new WhereInequalityObserver();
            predicates.Inspect(translated);
            string queryText = provider.GetQueryText(query.Expression);
            string plan = provider.GetQueryPlan(query.Expression);
            log.GetStringBuilder().Clear();
            Raw(new { Stage = "toolkit-query", Purpose = purpose, TranslatedPredicateInequalities = predicates.Observations, QueryText = queryText, QueryPlan = plan });
            if (purpose == "column-inequality-predicate")
            {
                var matches = predicates.Binaries.Where(binary => binary.Left is ColumnExpression left && left.Name == All.Names[1] && binary.Right is ColumnExpression right && right.Name == All.Names[0]).ToArray();
                Assert.AreEqual(1, matches.Length, "Prerequisite: the translated WHERE contains the acquired default/name column inequality.");
                Assert.AreEqual(typeof(bool), matches[0].Type);
                Assert.IsFalse(matches[0].IsLifted);
                Assert.IsFalse(matches[0].IsLiftedToNull);
                Assert.AreEqual(recordedBinary.Method, matches[0].Method, "Prerequisite: translation retains the actually observed original string operator.");
            }
            var actual = query.ToArray();
            var evidence = new { Purpose = purpose, TranslatedPredicateInequalities = predicates.Observations, QueryText = queryText, QueryPlan = plan, ExecutedSql = log.ToString(), Rows = actual, ConnectionState = primary.State.ToString() };
            queries.Add(evidence);
            Raw(new { Stage = "toolkit-result", Actual = evidence });
            Assert.IsTrue(actual.Length <= 1024, "Prerequisite: the toolkit metadata row bound is preserved.");
            return actual;
        }

        internal void RecordExpression(string purpose, Expression<Func<NativeColumnMetadataRow, bool>> expression)
        {
            var binary = expression.Body as BinaryExpression;
            var evidence = new { Purpose = purpose, Expression = expression.ToString(), NodeType = expression.Body.NodeType.ToString(), ResultType = expression.Body.Type.FullName, LeftType = binary?.Left.Type.FullName, RightType = binary?.Right.Type.FullName, MethodDeclaringType = binary?.Method?.DeclaringType?.FullName, MethodName = binary?.Method?.Name, IsLifted = binary?.IsLifted, IsLiftedToNull = binary?.IsLiftedToNull };
            expressions.Add(evidence);
            Raw(new { Stage = "expression", Actual = evidence });
            Assert.IsNotNull(binary, "Prerequisite: actual scalar inequality is a binary expression.");
            Assert.AreEqual(ExpressionType.NotEqual, binary.NodeType, "Prerequisite: inequality uses NotEqual.");
            Assert.AreEqual(typeof(bool), binary.Type, "Prerequisite: scalar string inequality returns bool.");
            Assert.AreEqual(typeof(string), binary.Left.Type);
            Assert.AreEqual(typeof(string), binary.Right.Type);
            Assert.IsFalse(binary.IsLifted);
            Assert.IsFalse(binary.IsLiftedToNull);
            Assert.IsTrue(binary.Method == null || (binary.Method.DeclaringType == typeof(string) && binary.Method.Name == "op_Inequality"), "Prerequisite: the actual operator is built-in string inequality.");
            recordedBinary = binary;
        }

        internal void Observe(string method, object outcome)
        {
            Console.WriteLine("Native string observation: " + JsonSerializer.Serialize(new
            {
                Schema = 1, Contract = "SQLITE-STRING-COLUMN-INEQUALITY-1", Method = method,
                MaterializationAndBindingPassed = materializationAndBindingPassed, Argument = argument,
                Provenance = provenance, NativeResponses = nativeCaptures, Expressions = expressions, ToolkitQueries = queries,
                ExpectedClrNames = ClrNames, Outcome = outcome,
                GeneralizedClrNullPolicy = false, ProviderCertification = false, FullGoalComplete = false
            }));
        }

        private NativeResponse Read(SqliteConnection connection, string purpose, string sql, string parameter = null)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 10;
            if (parameter != null) command.Parameters.AddWithValue("@table", parameter);
            var parameters = command.Parameters.Cast<SqliteParameter>().Select(value => new { value.ParameterName, ClrType = value.Value?.GetType().FullName, value.Value }).ToArray();
            Raw(new { Stage = "native-query", Purpose = purpose, Sql = sql, Parameters = parameters, command.CommandTimeout });
            using var reader = command.ExecuteReader();
            var response = new NativeResponse { FieldCount = reader.FieldCount, Names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray(), Schema = reader.GetColumnSchema().ToArray() };
            var schema = response.Schema.Select(column => new { column.ColumnOrdinal, column.ColumnName, DataType = column.DataType?.FullName, column.DataTypeName, column.AllowDBNull, column.IsHidden }).ToArray();
            Raw(new { Stage = "native-schema", Purpose = purpose, response.FieldCount, response.Names, Schema = schema });
            var captures = new List<object>();
            while (reader.Read())
            {
                var values = new object[reader.FieldCount];
                int copied = reader.GetValues(values);
                response.Rows.Add(values);
                var capture = new { CopiedCount = copied, Cells = values.Select((value, index) => new { Ordinal = index, ClrType = value?.GetType().FullName, Value = value is DBNull ? null : value, IsDBNull = reader.IsDBNull(index), FieldType = reader.GetFieldType(index).FullName, DataTypeName = reader.GetDataTypeName(index) }).ToArray() };
                captures.Add(capture);
                Raw(new { Stage = "native-row", Purpose = purpose, Ordinal = response.Rows.Count - 1, Actual = capture });
                Assert.AreEqual(reader.FieldCount, copied, "Prerequisite: native GetValues captures the complete unchanged buffer.");
                Assert.IsTrue(response.Rows.Count <= 1024, "Prerequisite: the native metadata row bound is preserved.");
                Assert.IsTrue(values.Select((value, index) => (value is DBNull) == reader.IsDBNull(index)).All(value => value), "Prerequisite: native NULL flags match actual buffer values.");
            }
            var complete = new { Purpose = purpose, Sql = sql, Parameters = parameters, response.FieldCount, response.Names, Schema = schema, Rows = captures, Completed = true };
            nativeCaptures.Add(complete);
            Raw(new { Stage = "native-complete", Actual = complete });
            return response;
        }

        private static void ValidateMetadata(NativeResponse response)
        {
            Assert.AreEqual(3, response.FieldCount, "Prerequisite: native metadata has three selected columns.");
            CollectionAssert.AreEqual(new[] { "name", "dflt_value", "arg" }, response.Names, "Prerequisite: native metadata column order is unchanged.");
            foreach (var row in response.Rows)
                Assert.IsTrue(row[0] is string name && name.Length > 0 && row[1] is string or DBNull && row[2] is string argument && argument == NativeTable, "Prerequisite: only actual name/default/argument values supply this test.");
        }

        public void Dispose()
        {
            try { independent.Dispose(); }
            finally { try { primary.Dispose(); } finally { log.Dispose(); } }
        }
    }

    private sealed class NativeResponse
    {
        internal int FieldCount { get; init; }
        internal string[] Names { get; init; }
        internal DbColumn[] Schema { get; init; }
        internal List<object[]> Rows { get; } = [];
    }

    private sealed class WhereInequalityObserver : DbExpressionVisitor
    {
        private readonly HashSet<BinaryExpression> observed = [];
        private bool inWhere;
        internal List<object> Observations { get; } = [];
        internal List<BinaryExpression> Binaries { get; } = [];
        internal void Inspect(Expression expression) => Visit(expression);

        protected override Expression VisitSelect(SelectExpression select)
        {
            bool previous = inWhere;
            try { inWhere = true; Visit(select.Where); }
            finally { inWhere = previous; }
            return base.VisitSelect(select);
        }

        protected override Expression VisitBinary(BinaryExpression binary)
        {
            if (inWhere && binary.NodeType == ExpressionType.NotEqual && binary.Left.Type == typeof(string) && binary.Right.Type == typeof(string) && observed.Add(binary))
            {
                Binaries.Add(binary);
                Observations.Add(new
                {
                    Context = "Select.Where", NodeType = binary.NodeType.ToString(), ResultType = binary.Type.FullName,
                    LeftType = binary.Left.Type.FullName, RightType = binary.Right.Type.FullName,
                    LeftNode = binary.Left is ColumnExpression ? nameof(ColumnExpression) : binary.Left.NodeType.ToString(),
                    RightNode = binary.Right is ColumnExpression ? nameof(ColumnExpression) : binary.Right.NodeType.ToString(),
                    LeftColumn = (binary.Left as ColumnExpression)?.Name, RightColumn = (binary.Right as ColumnExpression)?.Name,
                    MethodDeclaringType = binary.Method?.DeclaringType?.FullName, MethodName = binary.Method?.Name,
                    binary.IsLifted, binary.IsLiftedToNull
                });
            }
            return base.VisitBinary(binary);
        }
    }
}
