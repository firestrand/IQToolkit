using System;
using System.Linq.Expressions;
using System.Text.Json;
using IQToolkit.Data.Common;
using IQToolkit.Data.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IQToolkitTest.RealProvider;

[TestClass]
[DoNotParallelize]
public class NativeStringLiteralTests
{
    [TestMethod]
    public void NativeStringLiteralInputsAreAcquired() => AcquireInput();

    [TestMethod]
    public void CommonFormatterPreservesNativeQuotedString() =>
        VerifyLiteral(SqlFormatter.Format, quoted: true);

    [TestMethod]
    public void SQLiteFormatterPreservesNativeQuotedString() =>
        VerifyLiteral(SQLiteFormatter.Format, quoted: true);

    [TestMethod]
    public void CommonFormatterPreservesNativeRawString() =>
        VerifyLiteral(SqlFormatter.Format, quoted: false);

    [TestMethod]
    public void SQLiteFormatterPreservesNativeRawString() =>
        VerifyLiteral(SQLiteFormatter.Format, quoted: false);

    private static void VerifyLiteral(Func<Expression, string> format, bool quoted)
    {
        var input = AcquireInput();
        var value = quoted ? input.QuotedSourceId : input.SourceId;
        var oracle = quoted ? input.QuotedOracle : input.RawOracle;
        var literal = format(Expression.Constant(value));
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        StringResult actual;
        try
        {
            actual = ReadString(connection, "SELECT " + literal);
        }
        catch (SqliteException error)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Stage = "native-string-literal-failure",
                Formatter = format.Method.DeclaringType.FullName,
                Literal = literal,
                ParameterOracle = oracle,
                error.SqliteErrorCode,
                error.SqliteExtendedErrorCode,
                error.Message
            }));
            throw;
        }
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Stage = "native-string-literal-result",
            Formatter = format.Method.DeclaringType.FullName,
            Literal = literal,
            Actual = actual,
            ParameterOracle = oracle
        }));
        Assert.AreEqual(oracle, actual);
        Assert.IsTrue(StringComparer.Ordinal.Equals(oracle.Value, actual.Value));
    }

    private static NativeInput AcquireInput()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        string version;
        string sourceId;
        string quotedSourceId;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT sqlite_version(),sqlite_source_id(),quote(sqlite_source_id())";
            using var reader = command.ExecuteReader();
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(3, reader.FieldCount);
            var names = new string[reader.FieldCount];
            var types = new string[reader.FieldCount];
            var dataTypes = new string[reader.FieldCount];
            var values = new string[reader.FieldCount];
            for (var column = 0; column < reader.FieldCount; column++)
            {
                names[column] = reader.GetName(column);
                types[column] = reader.GetFieldType(column).FullName;
                dataTypes[column] = reader.GetDataTypeName(column);
                Assert.AreEqual(typeof(string).FullName, types[column]);
                Assert.AreEqual("TEXT", dataTypes[column]);
                values[column] = reader.GetString(column);
                Assert.IsFalse(string.IsNullOrEmpty(values[column]), "Actual native string must be nonempty.");
            }
            version = values[0];
            sourceId = values[1];
            quotedSourceId = values[2];
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Stage = "native-string-response",
                FieldCount = reader.FieldCount,
                Names = names,
                FieldTypes = types,
                DataTypes = dataTypes,
                Values = values
            }));
            Assert.IsFalse(reader.Read());
        }
        Assert.IsFalse(sourceId.Contains('\''), "Actual native source ID must supply the unquoted control.");
        StringAssert.Contains(quotedSourceId, "'", "Actual native quote response must contain an apostrophe.");
        var rawOracle = ReadString(connection, "SELECT @value", sourceId);
        var quotedOracle = ReadString(connection, "SELECT @value", quotedSourceId);
        Assert.AreEqual(1, rawOracle.FieldCount);
        Assert.AreEqual(1, quotedOracle.FieldCount);
        Assert.AreEqual(sourceId, rawOracle.Value);
        Assert.AreEqual(quotedSourceId, quotedOracle.Value);
        Assert.IsTrue(StringComparer.Ordinal.Equals(sourceId, rawOracle.Value));
        Assert.IsTrue(StringComparer.Ordinal.Equals(quotedSourceId, quotedOracle.Value));
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Stage = "native-string-acquisition",
            Version = version,
            SourceId = sourceId,
            QuotedSourceId = quotedSourceId,
            RawParameterOracle = rawOracle,
            QuotedParameterOracle = quotedOracle,
            Driver = typeof(SqliteConnection).Assembly.FullName,
            Runtime = Environment.Version.ToString()
        }));
        return new NativeInput(sourceId, quotedSourceId, rawOracle, quotedOracle);
    }

    private static StringResult ReadString(SqliteConnection connection, string sql, string parameter = null)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (parameter != null)
            command.Parameters.AddWithValue("@value", parameter);
        using var reader = command.ExecuteReader();
        Assert.IsTrue(reader.Read());
        var count = reader.FieldCount;
        Assert.AreEqual(1, count);
        var fieldType = reader.GetFieldType(0).FullName;
        var dataType = reader.GetDataTypeName(0);
        Assert.AreEqual(typeof(string).FullName, fieldType);
        Assert.AreEqual("TEXT", dataType);
        var value = reader.GetString(0);
        Assert.IsFalse(reader.Read());
        return new StringResult(count, fieldType, dataType, value);
    }

    private sealed record NativeInput(string SourceId, string QuotedSourceId,
        StringResult RawOracle, StringResult QuotedOracle);

    private sealed record StringResult(int FieldCount, string FieldType, string DataType, string Value);
}
