using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using IQToolkit;
using IQToolkit.Data;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using IQToolkit.Data.SQLite;
using Microsoft.Data.Sqlite;

namespace IQToolkit.PackageConsumer;

internal static class Program
{
    private static void Main()
    {
        VerifyPackageProvenance();
        RecordAssembly(typeof(QueryCache).Assembly);
        RecordAssembly(typeof(DbEntityProvider).Assembly);
        RecordAssembly(typeof(SQLiteQueryProvider).Assembly);
        RecordAssembly(typeof(SqliteConnection).Assembly);

        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        Console.WriteLine("Native SQLite source: " + JsonSerializer.Serialize(ReadNative(connection, "SELECT sqlite_source_id();")));
        var provider = new SQLiteQueryProvider(connection, new ImplicitMapping(), QueryPolicy.Default);
        var executor = ((ICreateExecutor)provider).CreateExecutor();

        VerifyMetadata(connection, executor, "SELECT sqlite_version();");
        VerifyMetadata(connection, executor, "PRAGMA compile_options;");
        Console.WriteLine("SQLite package-only native executor verification passed.");
    }

    private static void VerifyPackageProvenance()
    {
        using var assets = JsonDocument.Parse(File.ReadAllText("obj/project.assets.json"));
        var packages = assets.RootElement.GetProperty("project").GetProperty("restore").GetProperty("packagesPath").GetString()
            ?? throw new InvalidOperationException("The resolved package cache is required.");
        var toolkitCount = 0;
        foreach (var library in assets.RootElement.GetProperty("libraries").EnumerateObject())
        {
            if (!library.Name.StartsWith("IQToolkit/", StringComparison.Ordinal)
                && !library.Name.StartsWith("IQToolkit.", StringComparison.Ordinal))
            {
                continue;
            }
            toolkitCount++;
            var packagePath = Path.Combine(packages, library.Value.GetProperty("path").GetString()
                ?? throw new InvalidOperationException("The toolkit package path is required."));
            using var metadata = JsonDocument.Parse(File.ReadAllText(Path.Combine(packagePath, ".nupkg.metadata")));
            var source = metadata.RootElement.GetProperty("source").GetString();
            if (!StringComparer.Ordinal.Equals(source, Path.GetFullPath("feed")))
            {
                throw new InvalidOperationException("Toolkit dependencies must restore exclusively from the tested local feed.");
            }
            var idVersion = library.Name.Replace('/', '.');
            var feedPackage = Directory.EnumerateFiles("feed", "*.nupkg").Single(path =>
                StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileNameWithoutExtension(path), idVersion));
            var cachedPackage = Directory.EnumerateFiles(packagePath, "*.nupkg").Single();
            var hash = Convert.ToBase64String(SHA512.HashData(File.ReadAllBytes(feedPackage)));
            if (hash != library.Value.GetProperty("sha512").GetString()
                || hash != Convert.ToBase64String(SHA512.HashData(File.ReadAllBytes(cachedPackage))))
            {
                throw new InvalidOperationException("Resolved toolkit package hashes must match the tested feed bytes.");
            }
            Console.WriteLine(JsonSerializer.Serialize(new { Package = library.Name, Source = source, Sha512 = hash }));
        }
        if (toolkitCount != 3)
        {
            throw new InvalidOperationException("SQLite must resolve its core and Data package dependencies.");
        }
    }

    private static void VerifyMetadata(SqliteConnection connection, QueryExecutor executor, string statement)
    {
        var expected = ReadNative(connection, statement);
        var actual = executor.Execute(new QueryCommand(statement, null),
            field => field.ReadString(0), null, null).ToList();
        Console.WriteLine(JsonSerializer.Serialize(new { Request = statement, Native = expected, Toolkit = actual }));
        if (expected.Count == 0 || !expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Package execution must return the unchanged observed native metadata.");
        }
    }

    private static List<string> ReadNative(SqliteConnection connection, string statement)
    {
        using var command = connection.CreateCommand();
        command.CommandText = statement;
        using var reader = command.ExecuteReader();
        var rows = new List<string>();
        while (reader.Read())
        {
            rows.Add(reader.GetString(0));
        }
        if (rows.Count == 0)
        {
            throw new InvalidOperationException("Native metadata is a required data source; no generated fallback is permitted.");
        }
        return rows;
    }

    private static void RecordAssembly(Assembly assembly)
    {
        var directory = Path.GetDirectoryName(assembly.Location);
        var output = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!StringComparer.Ordinal.Equals(directory, output))
        {
            throw new InvalidOperationException("The consumer must load dependencies from its own restored-package build output.");
        }
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Assembly = assembly.FullName,
            Location = assembly.Location,
            Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location)))
        }));
    }
}
