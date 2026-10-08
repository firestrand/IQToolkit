using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using IQToolkit.Data.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IQToolkitTest.RealProvider;

[TestClass]
[DoNotParallelize]
public class NativeMathIdentifierCultureTests
{
    private static readonly JsonSerializerOptions ObservationOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    [TestMethod]
    public void NativeSQLiteMathSinInputsAreAcquired() => AcquireInput();

    [TestMethod]
    public void SQLiteMathSinPreservesNativeResultInInvariantCulture() => Verify(useTurkishCulture: false);

    [TestMethod]
    public void SQLiteMathSinPreservesNativeResultInTurkishCulture() => Verify(useTurkishCulture: true);

    private static void Verify(bool useTurkishCulture)
    {
        var input = AcquireInput();
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = useTurkishCulture ? input.Culture : CultureInfo.InvariantCulture;
            var method = typeof(Math).GetMethod(nameof(Math.Sin), new[] { typeof(double) });
            var expression = SQLiteFormatter.Format(Expression.Call(method, Expression.Constant(input.Actual.Value)));
            var sql = "SELECT " + expression;
            Write("Actual math identifier request: ", new
            {
                Culture = CultureInfo.CurrentCulture.Name, Method = method.ToString(), Expression = expression,
                Sql = sql, Actual = input.Actual, ParameterOracle = input.Oracle
            });
            try
            {
                using var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                var actual = QualifyNumeric(ReadResponse(command, "formatted-sin native result"));
                Write("Actual math identifier result: ", new
                {
                    Culture = CultureInfo.CurrentCulture.Name, Actual = actual, ParameterOracle = input.Oracle
                });
                Assert.AreEqual(input.Oracle.FieldCount, actual.FieldCount);
                Assert.AreEqual(input.Oracle.FieldType, actual.FieldType);
                Assert.AreEqual(input.Oracle.DataType, actual.DataType);
                Assert.AreEqual(input.Oracle.Bits, actual.Bits);
            }
            catch (SqliteException error)
            {
                Write("Actual math identifier failure: ", new
                {
                    Culture = CultureInfo.CurrentCulture.Name, Sql = sql, ExceptionType = error.GetType().FullName,
                    ErrorCode = error.SqliteErrorCode, ExtendedErrorCode = error.SqliteExtendedErrorCode,
                    error.Message, ParameterOracle = input.Oracle
                });
                throw;
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            Write("Actual math identifier restoration: ", new
            {
                OriginalCulture = originalCulture.Name, RestoredCulture = CultureInfo.CurrentCulture.Name,
                SameReference = ReferenceEquals(originalCulture, CultureInfo.CurrentCulture)
            });
            Assert.AreSame(originalCulture, CultureInfo.CurrentCulture);
        }
    }

    private static NativeInput AcquireInput()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        Response provenance;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT sqlite_version(), sqlite_source_id(), substr(sqlite_source_id(),1,19);";
            provenance = ReadResponse(command, "provenance");
        }
        AssertResponse(provenance, expectedFields: 3);
        Assert.AreEqual(1, provenance.Rows.Count);
        foreach (var value in provenance.Rows[0])
            Assert.IsInstanceOfType<string>(value);
        var version = (string)provenance.Rows[0][0];
        var sourceId = (string)provenance.Rows[0][1];
        var timestamp = (string)provenance.Rows[0][2];
        Write("Actual math identifier provenance: ", new
        {
            Version = version, SourceId = sourceId, Timestamp = timestamp,
            Driver = typeof(SqliteConnection).Assembly.FullName, Runtime = Environment.Version.ToString()
        });
        Assert.IsTrue(sourceId.StartsWith(timestamp, StringComparison.Ordinal));
        Assert.IsTrue(DateTime.TryParseExact(timestamp, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _), "The actual native source timestamp must be available.");

        NumericResult actual;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT julianday(substr(sqlite_source_id(),1,19));";
            actual = QualifyNumeric(ReadResponse(command, "native-double input"));
        }
        Assert.AreNotEqual(Math.Truncate(actual.Value), actual.Value, "The actual native Double must have a fractional part.");

        Response functions;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name, builtin, narg FROM pragma_function_list WHERE name = @function AND builtin = 1 AND narg = 1;";
            command.Parameters.AddWithValue("@function", "sin");
            functions = ReadResponse(command, "native-sin function catalog");
        }
        AssertResponse(functions, expectedFields: 3);
        Assert.IsTrue(functions.Rows.Count > 0, "The actual native builtin one-argument sin function must be available; no fallback is allowed.");
        Assert.AreEqual("name", functions.Columns[0].Name);
        Assert.AreEqual(typeof(string).FullName, functions.Columns[0].Type);
        Assert.AreEqual("TEXT", functions.Columns[0].DataType);
        Assert.AreEqual("builtin", functions.Columns[1].Name);
        Assert.AreEqual("narg", functions.Columns[2].Name);
        for (var index = 1; index < functions.FieldCount; index++)
        {
            Assert.AreEqual(typeof(long).FullName, functions.Columns[index].Type);
            Assert.AreEqual("INTEGER", functions.Columns[index].DataType);
        }
        foreach (var row in functions.Rows)
        {
            Assert.IsInstanceOfType<string>(row[0]);
            Assert.IsInstanceOfType<long>(row[1]);
            Assert.IsInstanceOfType<long>(row[2]);
            Assert.IsTrue(StringComparer.Ordinal.Equals("sin", (string)row[0]));
            Assert.AreEqual(1L, (long)row[1]);
            Assert.AreEqual(1L, (long)row[2]);
        }
        var functionName = (string)functions.Rows[0][0];

        using var oracleConnection = new SqliteConnection("Data Source=:memory:");
        oracleConnection.Open();
        NumericResult oracle;
        using (var command = oracleConnection.CreateCommand())
        {
            command.CommandText = "SELECT sin(@value);";
            command.Parameters.AddWithValue("@value", actual.Value);
            oracle = QualifyNumeric(ReadResponse(command, "independent-sin parameter oracle"));
        }
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var turkishUpper = functionName.ToUpper(culture);
        var invariantUpper = functionName.ToUpperInvariant();
        Write("Actual math identifier culture: ", new
        {
            Culture = culture.Name, FunctionName = functionName, TurkishUpper = turkishUpper, InvariantUpper = invariantUpper
        });
        Assert.AreEqual("tr-TR", culture.Name);
        Assert.IsTrue(turkishUpper.Contains('\u0130'), "The installed Turkish culture must expose the actual dotted-I mapping.");
        Assert.IsFalse(StringComparer.Ordinal.Equals(invariantUpper, turkishUpper));
        Write("Actual math identifier acquisition: ", new
        {
            Actual = actual, ParameterOracle = oracle, FunctionName = functionName,
            Culture = culture.Name, TurkishUpper = turkishUpper, InvariantUpper = invariantUpper,
            FormatterExecuted = false, BehaviorVerified = false
        });
        return new NativeInput(actual, oracle, culture);
    }

    private static Response ReadResponse(SqliteCommand command, string purpose)
    {
        using var reader = command.ExecuteReader();
        var fieldCount = reader.FieldCount;
        var rows = new List<object[]>();
        var copiedCounts = new List<int>();
        Column[] columns = null;
        while (reader.Read())
        {
            columns ??= ReadColumns(reader);
            var values = new object[fieldCount];
            copiedCounts.Add(reader.GetValues(values));
            rows.Add(values);
        }
        columns ??= ReadColumns(reader);
        var response = new Response(fieldCount, columns, rows, copiedCounts);
        Write("Actual math identifier response: ", new
        {
            Purpose = purpose, Sql = command.CommandText,
            Parameters = command.Parameters.Cast<SqliteParameter>().Select(parameter => new { parameter.ParameterName, parameter.Value }).ToArray(),
            response.FieldCount, response.Columns, response.Rows, response.CopiedCounts,
            DoubleBits = rows.Select(row => row.Select(value => value is double native
                ? (long?)BitConverter.DoubleToInt64Bits(native) : null).ToArray()).ToArray()
        });
        return response;
    }

    private static Column[] ReadColumns(SqliteDataReader reader)
        => Enumerable.Range(0, reader.FieldCount).Select(index => new Column(
            reader.GetName(index), reader.GetFieldType(index).FullName, reader.GetDataTypeName(index))).ToArray();

    private static void AssertResponse(Response response, int expectedFields)
    {
        Assert.AreEqual(expectedFields, response.FieldCount);
        Assert.AreEqual(response.FieldCount, response.Columns.Length);
        Assert.AreEqual(response.Rows.Count, response.CopiedCounts.Count);
        Assert.IsTrue(response.Rows.All(row => row.Length == response.FieldCount));
        Assert.IsTrue(response.CopiedCounts.All(count => count == response.FieldCount), "Native GetValues must copy every actual field.");
    }

    private static NumericResult QualifyNumeric(Response response)
    {
        AssertResponse(response, expectedFields: 1);
        Assert.AreEqual(1, response.Rows.Count);
        Assert.AreEqual(typeof(double).FullName, response.Columns[0].Type);
        Assert.AreEqual("REAL", response.Columns[0].DataType);
        Assert.IsInstanceOfType<double>(response.Rows[0][0]);
        var value = (double)response.Rows[0][0];
        Assert.IsTrue(double.IsFinite(value), "The actual native Double must be finite.");
        return new NumericResult(response.FieldCount, response.Columns[0].Name, response.Columns[0].Type,
            response.Columns[0].DataType, value, BitConverter.DoubleToInt64Bits(value));
    }

    private static void Write(string prefix, object observation)
        => Console.WriteLine(prefix + JsonSerializer.Serialize(observation, ObservationOptions));

    private sealed record NativeInput(NumericResult Actual, NumericResult Oracle, CultureInfo Culture);
    private sealed record NumericResult(int FieldCount, string Name, string FieldType, string DataType, double Value, long Bits);
    private sealed record Column(string Name, string Type, string DataType);
    private sealed record Response(int FieldCount, Column[] Columns, List<object[]> Rows, List<int> CopiedCounts);
}
