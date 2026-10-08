using System;
using System.Globalization;
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
[DoNotParallelize]
public class NativeAggregateCultureTests
{
    [TestMethod]
    public void NativeAggregateInputsAreAcquired() => AcquireInput();

    [TestMethod]
    public void RelationalMinimumPreservesNativeResultInTurkishCulture() =>
        VerifyAggregate(useMinimum: true, useTurkishCulture: true);

    [TestMethod]
    public void RelationalMinimumPreservesNativeResultInInvariantCulture() =>
        VerifyAggregate(useMinimum: true, useTurkishCulture: false);

    [TestMethod]
    public void RelationalMaximumPreservesNativeResultInTurkishCulture() =>
        VerifyAggregate(useMinimum: false, useTurkishCulture: true);

    private static void VerifyAggregate(bool useMinimum, bool useTurkishCulture)
    {
        var input = AcquireInput();
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = useTurkishCulture ? input.Culture : CultureInfo.InvariantCulture;
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var log = new StringWriter();
            var provider = new SQLiteQueryProvider(connection,
                new AttributeMapping(typeof(NativeProviderCacheTests.CompileOptionsContext)), QueryPolicy.Default)
            {
                Cache = null,
                Log = log
            };
            var query = provider.GetTable<NativeProviderCacheTests.NativeMetadataEntry>("Entries");
            var oracle = useMinimum ? input.Minimum : input.Maximum;
            string result;
            try
            {
                result = useMinimum ? query.Min(row => row.Name) : query.Max(row => row.Name);
            }
            catch (SqliteException error)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    Stage = "native-aggregate-failure",
                    Culture = CultureInfo.CurrentCulture.Name,
                    Sql = log.ToString(),
                    Oracle = oracle,
                    error.SqliteErrorCode,
                    error.SqliteExtendedErrorCode,
                    error.Message
                }));
                throw;
            }
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Stage = "relational-aggregate-result",
                Culture = CultureInfo.CurrentCulture.Name,
                Sql = log.ToString(),
                Oracle = oracle,
                Result = result
            }));
            Assert.AreEqual(oracle, result);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            Assert.AreSame(originalCulture, CultureInfo.CurrentCulture);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Stage = "culture-restored",
                Culture = CultureInfo.CurrentCulture.Name
            }));
        }
    }

    private static NativeInput AcquireInput()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        string version;
        string sourceId;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT sqlite_version(),sqlite_source_id()";
            using var reader = command.ExecuteReader();
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(2, reader.FieldCount);
            version = reader.GetString(0);
            sourceId = reader.GetString(1);
            Assert.IsFalse(reader.Read());
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT compile_options FROM pragma_compile_options LIMIT 0";
            using var reader = command.ExecuteReader();
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Stage = "native-aggregate-source-schema",
                FieldCount = reader.FieldCount,
                Name = reader.GetName(0)
            }));
            Assert.AreEqual(1, reader.FieldCount);
            Assert.AreEqual("compile_options", reader.GetName(0));
            Assert.IsFalse(reader.Read());
        }

        var values = new object[2];
        var fieldTypes = new string[2];
        var dataTypes = new string[2];
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT min(compile_options),max(compile_options) FROM pragma_compile_options";
            using var reader = command.ExecuteReader();
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(values.Length, reader.FieldCount);
            for (var column = 0; column < reader.FieldCount; column++)
            {
                values[column] = reader.GetValue(column);
                fieldTypes[column] = reader.GetFieldType(column).FullName;
                dataTypes[column] = reader.GetDataTypeName(column);
            }
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Stage = "native-aggregate-response",
                Version = version,
                SourceId = sourceId,
                FieldCount = reader.FieldCount,
                FieldTypes = fieldTypes,
                DataTypes = dataTypes,
                Values = values
            }));
            Assert.IsFalse(reader.Read());
        }

        for (var column = 0; column < values.Length; column++)
        {
            Assert.AreEqual(typeof(string).FullName, fieldTypes[column]);
            Assert.AreEqual("TEXT", dataTypes[column]);
            Assert.IsInstanceOfType<string>(values[column]);
            Assert.IsFalse(string.IsNullOrEmpty((string)values[column]), "Actual native aggregate must be nonempty.");
        }
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Stage = "native-aggregate-profile",
            Culture = culture.Name,
            Driver = typeof(SqliteConnection).Assembly.FullName,
            Runtime = Environment.Version.ToString()
        }));
        Assert.AreEqual("tr-TR", culture.Name);
        return new NativeInput((string)values[0], (string)values[1], culture);
    }

    private sealed record NativeInput(string Minimum, string Maximum, CultureInfo Culture);
}
