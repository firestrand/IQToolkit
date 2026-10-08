using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace IQToolkitTest.RealProvider;

internal static class SQLiteMetadata
{
    internal static List<string> ReadValues(string statement)
    {
        // Only documented native metadata requests are supplied by the tests.
        // Returned rows are retained unchanged; missing data is never synthesized.
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using (var provenance = connection.CreateCommand())
        {
            provenance.CommandText = "SELECT sqlite_version(), sqlite_source_id();";
            using var reader = provenance.ExecuteReader();
            if (!reader.Read())
            {
                throw new InvalidOperationException("Native SQLite metadata is unavailable.");
            }
            Console.WriteLine("Native SQLite version: " + reader.GetString(0));
            Console.WriteLine("Native SQLite source: " + reader.GetString(1));
        }

        using var command = connection.CreateCommand();
        command.CommandText = statement;
        using var values = command.ExecuteReader();
        var rows = new List<string>();
        while (values.Read())
        {
            rows.Add(values.GetString(0));
        }
        Console.WriteLine("Native request: " + statement);
        Console.WriteLine("Native rows: " + JsonSerializer.Serialize(rows));
        return rows;
    }
}
