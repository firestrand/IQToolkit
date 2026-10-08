using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IQToolkit.CoverageCollector;

internal static class Program
{
    private const long MaximumBytes = 16 * 1024 * 1024;
    private static readonly string[] Projects = ["IQToolkit", "IQToolkit.Data", "IQToolkit.Data.Access",
        "IQToolkit.Data.MySqlClient", "IQToolkit.Data.SQLite", "IQToolkit.Data.SqlClient", "IQToolkitTest", "Test"];
    private static readonly string[] Modules = ["IQToolkit", "IQToolkit.Data", "IQToolkit.Data.SQLite", "IQToolkit.Data.SqlClient"];
    private sealed record Input(string Path, string Sha256);
    private sealed record Module(string Name, Input Dll, Input Pdb);
    private sealed record State(string Repository, string Outside, string Evidence, string StrictRun,
        Input StrictReceipt, string CollectorDirectory, Input Collector, Input Settings, Input Mapping,
        Module[] Modules, Module TestAssembly, Dictionary<string, string> Sources,
        Dictionary<string, string> Inputs, Dictionary<string, string> Binaries, Dictionary<string, string> Output);

    private static int Main(string[] args)
    {
        try
        {
            Require(args.Length > 0, "Expected a collector metadata operation.");
            switch (args[0])
            {
                case "prepare":
                    Require(args.Length == 6, "Expected repository, baseline, strict run, outside workspace and evidence.");
                    Prepare(args[1], args[2], args[3], args[4], args[5]);
                    break;
                case "preserve":
                    Require(args.Length == 3 && args[2] is "collection" or "terminal", "Expected evidence and preservation stage.");
                    Preserve(args[1], args[2]);
                    break;
                case "descriptor":
                    Require(args.Length == 2, "Expected collection evidence.");
                    Describe(args[1]);
                    break;
                default: throw new InvalidDataException("Unknown collector metadata operation.");
            }
            return 0;
        }
        catch (InvalidDataException error)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { Status = "rejected", Reason = error.Message }));
            return 1;
        }
        catch (Exception error)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { Status = "rejected", Reason = "Collector metadata operation failed.", FailureType = error.GetType().Name }));
            return 1;
        }
    }

    private static void Require([DoesNotReturnIf(false)] bool condition, string reason)
    {
        if (!condition) throw new InvalidDataException(reason);
    }

    private static string Hash(string path)
    {
        var info = new FileInfo(path);
        Require(info.Exists && info.LinkTarget is null && info.Length is > 0 and <= MaximumBytes, "Missing, linked, empty or oversized input.");
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static Input Identify(string path) => new(Path.GetFullPath(path), Hash(path));

    private static bool SameHash(string hash, string? expected) =>
        string.Equals(hash, expected, StringComparison.OrdinalIgnoreCase);

    private static void Write(string path, object value)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, new JsonSerializerOptions { WriteIndented = true });
        Require(bytes.Length <= MaximumBytes, "Metadata report exceeds its bound.");
        using var output = new FileStream(path, FileMode.CreateNew);
        output.Write(bytes);
    }

    private static T Read<T>(string path)
    {
        Hash(path);
        return JsonSerializer.Deserialize<T>(File.ReadAllBytes(path)) ?? throw new InvalidDataException("Missing metadata.");
    }

    private static string Git(string repository, params string[] arguments)
    {
        using var process = new Process { StartInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = repository, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true
        } };
        foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
        Require(process.Start(), "Unable to start repository inspection.");
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEndAsync();
        if (!process.WaitForExit(5000) || !Task.WhenAll(output, error).Wait(1000))
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                Require(process.WaitForExit(2000), "Repository inspection cleanup did not settle.");
            }
            catch (Exception cleanup)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { Status = "cleanup-failed", Operation = "repository inspection", FailureType = cleanup.GetType().Name }));
            }
            throw new InvalidDataException("Repository inspection exceeded its process or pipe bound.");
        }
        Require(process.ExitCode == 0 && error.Result.Length == 0 && output.Result.Length <= MaximumBytes, "Repository inspection failed.");
        return output.Result;
    }

    private static Dictionary<string, string> Sources(string repository, bool gitHashes = false)
    {
        var paths = Git(repository, "ls-files", "--cached", "--others", "--exclude-standard", "-z")
            .Split('\0', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        Require(paths.Length is > 0 and <= 2048, "Unexpected source manifest size.");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in paths)
            if (File.Exists(Path.Combine(repository, path)))
                result.Add(path, gitHashes ? Git(repository, "hash-object", "--", path).Trim() : Hash(Path.Combine(repository, path)));
        return result;
    }

    private static Dictionary<string, string> Files(string directory, string relativeRoot, Func<string, bool>? include = null)
    {
        Require(Directory.Exists(directory), "Missing input directory.");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFileSystemEntries(directory, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            Require(new FileInfo(path).LinkTarget is null, "Linked input entry is unsupported.");
            if (File.Exists(path) && (include is null || include(path)))
            {
                Require(result.Count < 2048, "Input file count exceeds its bound.");
                result.Add(Path.GetRelativePath(relativeRoot, path).Replace('\\', '/'), Hash(path));
            }
        }
        return result;
    }

    private static Dictionary<string, string> Binaries(string repository)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var project in Projects)
            foreach (var part in new[] { "bin/Release", "obj/Release" })
                foreach (var pair in Files(Path.Combine(repository, project, part), repository,
                    path => Path.GetExtension(path) is ".dll" or ".pdb")) result.Add(pair.Key, pair.Value);
        Require(result.Count > 0, "No ordinary binaries to preserve.");
        return result;
    }

    private static bool Equal(Dictionary<string, string> first, Dictionary<string, string> second) =>
        first.Count == second.Count && first.All(pair => second.TryGetValue(pair.Key, out var hash) && hash == pair.Value);

    private static void Prepare(string repository, string baselinePath, string strictRun, string outside, string evidence)
    {
        repository = Path.GetFullPath(repository); strictRun = Path.GetFullPath(strictRun);
        outside = Path.GetFullPath(outside); evidence = Path.GetFullPath(evidence);
        Require(!outside.StartsWith(repository + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
            new DirectoryInfo(outside).LinkTarget is null, "Collector workspace must be outside the checkout.");
        Require(Path.GetDirectoryName(strictRun) == Path.Combine(repository, "artifacts", "verification", "runs") &&
            File.ReadAllText(Path.Combine(strictRun, "status.txt")).Trim() == "passed", "Expected an actual passing strict run.");
        var lines = File.ReadAllLines(Path.Combine(strictRun, "snapshot.txt"));
        Require(lines.Length > 0 && lines.Length % 2 == 0, "Invalid strict source snapshot.");
        var strictSources = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < lines.Length; index += 2) strictSources.Add(lines[index + 1], lines[index]);
        Require(Equal(strictSources, Sources(repository, gitHashes: true)), "Strict run does not match the current checkout snapshot.");
        var receiptLines = File.ReadAllLines(Path.Combine(strictRun, "test-results.txt"));
        const string prefix = "Test result evidence: ";
        var receiptDirectories = receiptLines.Where(line => line.StartsWith(prefix, StringComparison.Ordinal)).Select(line => line[prefix.Length..]).ToArray();
        Require(receiptDirectories.Length == 1 && Path.GetDirectoryName(receiptDirectories[0]) == Path.Combine(repository, "artifacts", "test-results", "runs"),
            "Missing strict result evidence directory.");
        var receipt = Identify(Path.Combine(receiptDirectories[0], "outcome.jsonl"));
        using var validation = JsonDocument.Parse(File.ReadAllBytes(receipt.Path));
        Require(validation.RootElement.GetProperty("Status").GetString() == "verified" &&
            validation.RootElement.GetProperty("TrxSha256").GetString() == Hash(Path.Combine(strictRun, "tests.trx")) &&
            validation.RootElement.GetProperty("TranslationSha256").GetString() == Hash(Path.Combine(strictRun, "translation.txt")) &&
            validation.RootElement.GetProperty("Translations").GetInt32() > 0, "Strict execution receipt is not verified and hash-bound.");

        var baseline = JsonNode.Parse(File.ReadAllBytes(baselinePath)) ?? throw new InvalidDataException("Missing baseline descriptor.");
        Require(baseline["Schema"]?.GetValue<int>() == 1 && baseline["RepositoryRoot"]?.GetValue<string>() == repository, "Baseline profile root or schema differs.");
        using var dependencies = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repository, "IQToolkitTest", "packages.lock.json")));
        var version = dependencies.RootElement.GetProperty("dependencies").GetProperty("net10.0").GetProperty("Microsoft.CodeCoverage").GetProperty("resolved").GetString();
        Require(version is not null && Regex.IsMatch(version, "^[0-9]+\\.[0-9]+\\.[0-9]+$"), "Unsupported locked collector version.");
        var collectorRoot = Path.Combine(repository, "artifacts", "nuget", "packages", "microsoft.codecoverage", version);
        var archive = Identify(Path.Combine(collectorRoot, $"microsoft.codecoverage.{version}.nupkg"));
        var settings = Identify(Path.Combine(repository, "eng", "coverage.runsettings"));
        Require(SameHash(archive.Sha256, baseline["Collector"]?["Sha256"]?.GetValue<string>()) &&
            SameHash(settings.Sha256, baseline["Settings"]?["Sha256"]?.GetValue<string>()), "Collector or settings changed; explicit qualification required.");
        var inputs = new Dictionary<string, string>(StringComparer.Ordinal);
        var collectorFiles = Files(collectorRoot, collectorRoot);
        foreach (var pair in collectorFiles) inputs.Add(Path.Combine(collectorRoot, pair.Key), pair.Value);
        using (var package = ZipFile.OpenRead(archive.Path))
        {
            var entries = package.Entries.Where(entry => entry.FullName.StartsWith("build/netstandard2.0/", StringComparison.Ordinal) && !entry.FullName.EndsWith('/')).ToArray();
            Require(entries.Length > 0, "Collector archive has no selected adapter.");
            Require(entries.Select(entry => entry.FullName).Order(StringComparer.Ordinal).SequenceEqual(
                collectorFiles.Keys.Where(path => path.StartsWith("build/netstandard2.0/", StringComparison.Ordinal)).Order(StringComparer.Ordinal)),
                "Installed collector file set differs from the selected archive.");
            foreach (var entry in entries)
            {
                Require(entry.Length is > 0 and <= MaximumBytes, "Unsupported collector archive entry size.");
                using var stream = entry.Open();
                var actual = Convert.ToHexString(SHA256.HashData(stream));
                Require(collectorFiles.TryGetValue(entry.FullName, out var installed) && actual == installed, "Installed collector differs from the pinned archive.");
            }
        }
        var outputDirectory = Path.Combine(repository, "IQToolkitTest", "bin", "Release", "net10.0");
        var mapping = Identify(Path.Combine(outputDirectory, ".msCoverageSourceRootsMapping_IQToolkitTest"));
        Require(SameHash(mapping.Sha256, baseline["Mapping"]?["Sha256"]?.GetValue<string>()), "Source mapping changed; explicit qualification required.");
        Module IdentifyModule(string name) => new(name, Identify(Path.Combine(outputDirectory, name + ".dll")), Identify(Path.Combine(outputDirectory, name + ".pdb")));
        var modules = Modules.Select(IdentifyModule).ToArray();
        var test = IdentifyModule("IQToolkitTest");
        var baselineModules = baseline["Modules"]?.AsArray().Append(baseline["TestAssembly"]).ToArray() ?? [];
        foreach (var module in modules.Append(test))
        {
            var matching = baselineModules.Single(item => item?["Name"]?.GetValue<string>() == module.Name);
            Require(SameHash(module.Dll.Sha256, matching?["Dll"]?["Sha256"]?.GetValue<string>()) &&
                SameHash(module.Pdb.Sha256, matching?["Pdb"]?["Sha256"]?.GetValue<string>()), "Compiled module changed; explicit qualification required.");
        }
        foreach (var input in new[] { Identify(baselinePath), settings, mapping, receipt,
            Identify(Path.Combine(repository, "IQToolkitTest", "packages.lock.json")), Identify(Path.Combine(strictRun, "tests.trx")),
            Identify(Path.Combine(strictRun, "translation.txt")), Identify(Path.Combine(strictRun, "snapshot.txt")), Identify(Path.Combine(strictRun, "status.txt")) })
            inputs[input.Path] = input.Sha256;
        var state = new State(repository, outside, evidence, strictRun, receipt,
            Path.Combine(collectorRoot, "build", "netstandard2.0"), archive, settings, mapping, modules, test,
            Sources(repository), inputs, Binaries(repository), Files(outputDirectory, outputDirectory));
        Write(Path.Combine(evidence, "state.json"), state);
        Write(Path.Combine(evidence, "binaries-before.json"), state.Binaries);
        Write(Path.Combine(evidence, "output-before.json"), state.Output);
        var shadow = Path.Combine(outside, "shadow");
        Require(!Directory.Exists(shadow), "Shadow output already exists.");
        Directory.CreateDirectory(shadow);
        foreach (var pair in state.Output)
        {
            var target = Path.Combine(shadow, pair.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(Path.Combine(outputDirectory, pair.Key), target, overwrite: false);
        }
        Require(Equal(state.Output, Files(shadow, shadow)), "Shadow copy is not byte-identical.");
        Write(Path.Combine(evidence, "shadow-before.json"), Files(shadow, shadow));
        using (var output = new StreamWriter(new FileStream(Path.Combine(evidence, "collector-directory.txt"), FileMode.CreateNew)))
            output.WriteLine(state.CollectorDirectory);
        Console.WriteLine(JsonSerializer.Serialize(new { Status = "prepared", Sources = state.Sources.Count,
            OrdinaryBinaries = state.Binaries.Count, TestOutputFiles = state.Output.Count, CollectorVersion = version }));
    }

    private static void Preserve(string evidence, string prefix)
    {
        var state = Read<State>(Path.Combine(evidence, "state.json"));
        var sources = Sources(state.Repository);
        var binaries = Binaries(state.Repository);
        var outputDirectory = Path.Combine(state.Repository, "IQToolkitTest", "bin", "Release", "net10.0");
        var output = Files(outputDirectory, outputDirectory);
        var inputs = state.Inputs.ToDictionary(pair => pair.Key, pair => Hash(pair.Key), StringComparer.Ordinal);
        Write(Path.Combine(evidence, prefix + "-sources-after.json"), sources);
        Write(Path.Combine(evidence, prefix + "-binaries-after.json"), binaries);
        Write(Path.Combine(evidence, prefix + "-output-after.json"), output);
        Write(Path.Combine(evidence, prefix + "-inputs-after.json"), inputs);
        var shadow = Path.Combine(state.Outside, "shadow");
        bool shadowRestored = Directory.Exists(shadow) && Equal(state.Output, Files(shadow, shadow));
        Require(Equal(state.Sources, sources) && Equal(state.Binaries, binaries) && Equal(state.Output, output) && Equal(state.Inputs, inputs),
            "Original source, binary, output or collector inputs changed.");
        Console.WriteLine(JsonSerializer.Serialize(new { Status = "preserved", OrdinaryBinaries = binaries.Count,
            TestOutputFiles = output.Count, ShadowRestored = shadowRestored }));
    }

    private static void Describe(string evidence)
    {
        var state = Read<State>(Path.Combine(evidence, "state.json"));
        var results = Path.Combine(evidence, "results");
        var reports = Directory.EnumerateFiles(results, "*.cobertura.xml", SearchOption.AllDirectories).Order(StringComparer.Ordinal).Select(Identify).ToArray();
        Require(reports.Length is > 0 and <= 16, "Missing collector reports.");
        var descriptor = new
        {
            Schema = 1, RepositoryRoot = state.Repository, Reports = reports,
            CoverageTrx = Identify(Path.Combine(results, "coverage.trx")),
            StrictTrx = Identify(Path.Combine(state.StrictRun, "tests.trx")), Translation = Identify(Path.Combine(state.StrictRun, "translation.txt")),
            StrictReceipt = state.StrictReceipt, Settings = state.Settings, Collector = state.Collector, Mapping = state.Mapping,
            Modules = state.Modules, TestAssembly = state.TestAssembly,
            Preservation = new[] { new { Before = Identify(Path.Combine(evidence, "binaries-before.json")),
                After = Identify(Path.Combine(evidence, "collection-binaries-after.json")) },
                new { Before = Identify(Path.Combine(evidence, "output-before.json")), After = Identify(Path.Combine(evidence, "collection-output-after.json")) } }
        };
        Write(Path.Combine(evidence, "descriptor.json"), descriptor);
        Console.WriteLine(JsonSerializer.Serialize(new { Status = "described", ReportCopies = reports.Length }));
    }
}
