using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;
using IQToolkit.Data.Common;
using IQToolkit.Data.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IQToolkitTest.RealProvider;

[TestClass]
[DoNotParallelize]
public class NativeFloatingLiteralTests
{
    [TestMethod]
    public void NativeDoubleInputsAreAcquired() => AcquireInput();

    [TestMethod]
    public void CommonFormatterPreservesNativeDoubleInCommaCulture() =>
        VerifyLiteral(SqlFormatter.Format, useCommaCulture: true);

    [TestMethod]
    public void CommonFormatterPreservesNativeDoubleInInvariantCulture() =>
        VerifyLiteral(SqlFormatter.Format, useCommaCulture: false);

    [TestMethod]
    public void SQLiteFormatterPreservesNativeDoubleInCommaCulture() =>
        VerifyLiteral(SQLiteFormatter.Format, useCommaCulture: true);

    [TestMethod]
    public void SQLiteFormatterPreservesNativeDoubleInInvariantCulture() =>
        VerifyLiteral(SQLiteFormatter.Format, useCommaCulture: false);

    private static void VerifyLiteral(Func<Expression, string> format, bool useCommaCulture)
    {
        var input = AcquireInput();
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = useCommaCulture ? input.Culture : CultureInfo.InvariantCulture;
            var literal = format(Expression.Constant(input.Actual.Value));
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT " + literal;
            using var reader = command.ExecuteReader();
            Assert.IsTrue(reader.Read());
            var values = new object[reader.FieldCount];
            var fieldTypes = new string[reader.FieldCount];
            var dataTypes = new string[reader.FieldCount];
            for (var column = 0; column < reader.FieldCount; column++)
            {
                values[column] = reader.GetValue(column);
                fieldTypes[column] = reader.GetFieldType(column).FullName;
                dataTypes[column] = reader.GetDataTypeName(column);
            }
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Stage = "formatted-native-double",
                Formatter = format.Method.DeclaringType.FullName,
                Culture = CultureInfo.CurrentCulture.Name,
                Literal = literal,
                FieldCount = reader.FieldCount,
                FieldTypes = fieldTypes,
                DataTypes = dataTypes,
                Values = values,
                ParameterOracle = input.Oracle
            }));
            Assert.AreEqual(input.Oracle.FieldCount, reader.FieldCount,
                "SQL literal must preserve native oracle column count.");
            Assert.AreEqual(input.Oracle.FieldType, fieldTypes[0]);
            Assert.AreEqual(input.Oracle.DataType, dataTypes[0]);
            Assert.IsInstanceOfType<double>(values[0]);
            Assert.AreEqual(input.Oracle.Bits, BitConverter.DoubleToInt64Bits((double)values[0]));
            Assert.IsFalse(reader.Read());
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
        string timestamp;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT sqlite_version(), sqlite_source_id(), substr(sqlite_source_id(),1,19)";
            using var reader = command.ExecuteReader();
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(3, reader.FieldCount);
            version = reader.GetString(0);
            sourceId = reader.GetString(1);
            timestamp = reader.GetString(2);
            Assert.IsFalse(reader.Read());
        }

        Assert.IsTrue(sourceId.StartsWith(timestamp, StringComparison.Ordinal));
        Assert.IsTrue(DateTime.TryParseExact(timestamp, "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out _), "Native source timestamp is unavailable.");
        var actual = ReadNumeric(connection, "SELECT julianday(substr(sqlite_source_id(),1,19))");
        Assert.AreEqual(1, actual.FieldCount);
        Assert.AreEqual(typeof(double).FullName, actual.FieldType);
        Assert.AreEqual("REAL", actual.DataType);
        Assert.IsTrue(double.IsFinite(actual.Value), "Native Double must be finite.");
        Assert.AreNotEqual(Math.Truncate(actual.Value), actual.Value, "Native Double must have a fractional part.");
        var oracle = ReadNumeric(connection, "SELECT @value", actual.Value);
        Assert.AreEqual(actual.FieldCount, oracle.FieldCount);
        Assert.AreEqual(actual.FieldType, oracle.FieldType);
        Assert.AreEqual(actual.DataType, oracle.DataType);
        Assert.AreEqual(actual.Bits, oracle.Bits);
        var culture = CultureInfo.GetCultureInfo("fr-FR");
        Assert.AreEqual(",", culture.NumberFormat.NumberDecimalSeparator, "Installed comma-decimal culture is unavailable.");
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Stage = "native-double-acquisition",
            Version = version,
            SourceId = sourceId,
            Timestamp = timestamp,
            Actual = actual,
            ParameterOracle = oracle,
            Culture = culture.Name,
            DecimalSeparator = culture.NumberFormat.NumberDecimalSeparator,
            Driver = typeof(SqliteConnection).Assembly.FullName,
            Runtime = Environment.Version.ToString()
        }));
        return new NativeInput(actual, oracle, culture);
    }

    private static NumericResult ReadNumeric(SqliteConnection connection, string sql, double? parameter = null)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (parameter.HasValue)
            command.Parameters.AddWithValue("@value", parameter.Value);
        using var reader = command.ExecuteReader();
        Assert.IsTrue(reader.Read());
        var count = reader.FieldCount;
        var type = reader.GetFieldType(0).FullName;
        var dataType = reader.GetDataTypeName(0);
        var value = reader.GetValue(0);
        Assert.IsInstanceOfType<double>(value);
        var native = (double)value;
        Assert.IsFalse(reader.Read());
        return new NumericResult(count, type, dataType, native, BitConverter.DoubleToInt64Bits(native));
    }

    private sealed record NativeInput(NumericResult Actual, NumericResult Oracle, CultureInfo Culture);

    private sealed record NumericResult(int FieldCount, string FieldType, string DataType, double Value, long Bits);
}
