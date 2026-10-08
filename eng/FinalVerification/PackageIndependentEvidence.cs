#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace IQToolkit.FinalVerification;

internal static partial class Program
{
    private const string PackagePublicFeed = "https://api.nuget.org/v3/index.json";
    private const string PackageEmptySha = "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855";
    private const string PackageLargeArchiveSha = "4A22C02FFCF489792903CF263DEF9FCE27739716883F965F0B5EEFF1637430AC";
    private static readonly Guid PackageDocumentSha256 = new("8829d00f-11b8-4213-878b-770e8597ac16");
    private static readonly string[] PackageProfiles = ["IQToolkit", "IQToolkit.Data", "IQToolkit.Data.SQLite", "IQToolkit.Data.SqlClient", "IQToolkit.Data.MySqlClient", "IQToolkit.Data.Access"];
    private static readonly Dictionary<string, string> PackageDrivers = new(StringComparer.Ordinal)
    {
        ["IQToolkit.Data.SQLite"] = "Microsoft.Data.Sqlite", ["IQToolkit.Data.SqlClient"] = "Microsoft.Data.SqlClient",
        ["IQToolkit.Data.MySqlClient"] = "MySql.Data", ["IQToolkit.Data.Access"] = "System.Data.OleDb"
    };
    private sealed class PackageContext
    {
        internal required string Outer;
        internal required string Inner;
        internal required string Environment;
        internal required string OriginalOuter;
        internal required string OriginalInner;
        internal readonly HashSet<string> Closure = new(StringComparer.Ordinal);
        internal readonly HashSet<string> Markers = new(StringComparer.Ordinal);
        internal readonly Dictionary<string, string> Projects = new(StringComparer.Ordinal);
        internal int OriginalRecords;
        internal int BuildControlFiles;
        internal int GeneratedControlFiles;
    }

    // This receipt is tentative; the coordinator alone publishes after terminal rehash.
    private static object ValidateIndependentPackageProducer(string consumerDirectory, string apiDirectory)
    {
        var outer = System.IO.Path.GetFullPath(consumerDirectory);
        var api = System.IO.Path.GetFullPath(apiDirectory);
        PackageNoLinks(outer); PackageNoLinks(api);
        Require(System.IO.Path.GetDirectoryName(outer) == System.IO.Path.Combine(repository, "artifacts/ci-consumers/runs"), "Independent producer is outside its owned CI evidence area.");
        Require(File.ReadAllText(Capture(System.IO.Path.Combine(outer, "status.txt"))).Trim() == "passed", "Independent package producer did not complete successfully.");
        var candidates = Directory.EnumerateDirectories(System.IO.Path.Combine(outer, "environment/artifacts/independent-consumers/verify-runs")).Take(65).ToArray();
        Require(candidates.Length is > 0 and <= 64, "Unexpected independent verification run count.");
        var innerCandidates = candidates.Where(path =>
        {
            PackageNoLinks(path); var status = System.IO.Path.Combine(path, "status.txt"); PackageNoLinks(status);
            if (!File.Exists(status)) return false;
            Require(new FileInfo(status).Length <= 1024, "Unexpected producer status-file size.");
            return File.ReadAllText(status).Trim() == "passed";
        }).ToArray();
        Require(innerCandidates.Length == 1, "Independent producer lacks exactly one complete six-profile run.");
        var inner = innerCandidates[0];
        PackageNoLinks(inner);
        var originalOuter = File.ReadAllText(Capture(System.IO.Path.Combine(outer, "temporary-directory.txt"))).Trim();
        var originalInner = File.ReadAllText(Capture(System.IO.Path.Combine(inner, "temporary-directory.txt"))).Trim();
        Require(System.IO.Path.IsPathFullyQualified(originalOuter) && System.IO.Path.GetFullPath(originalOuter) == originalOuter &&
            System.IO.Path.IsPathFullyQualified(originalInner) && System.IO.Path.GetFullPath(originalInner) == originalInner,
            "Original producer workspace paths are not canonical absolute paths.");
        var context = new PackageContext { Outer = outer, Inner = inner, Environment = System.IO.Path.Combine(inner, "environment"), OriginalOuter = originalOuter, OriginalInner = originalInner };
        var outerMapping = File.ReadAllText(PackageCapture(context, System.IO.Path.Combine(outer, "retained-path-mapping.txt"))).TrimEnd('\n', '\r').Split('\t');
        Require(outerMapping.SequenceEqual(new[] { originalOuter, System.IO.Path.Combine(outer, "environment") }), "Producer relocation mapping differs.");
        PackageVerifyProjectMappings(context);
        var witness = PackageNativeWitness(outer, api);
        var version = PackageArchiveIdentity(Capture(System.IO.Path.Combine(api, "packages", "IQToolkit.1.1.2-preview.1.nupkg")), "IQToolkit", null);
        Require(version == "1.1.2-preview.1", "Independent producer package version differs from the declared profile.");
        PackageSelectClosure(context);
        PackageAuthenticateOriginalInputs(context, outer);
        PackageAuthenticateOriginalInputs(context, inner);
        Require(context.OriginalRecords == 74, "Independent producer original input-record count differs.");
        var packages = new List<object>();
        var profiles = new List<object>();
        foreach (var id in PackageProfiles)
        {
            var directory = System.IO.Path.Combine(context.Environment, id);
            Require(File.ReadAllText(PackageCapture(context, System.IO.Path.Combine(inner, id, "status.txt"))).Trim() == "passed", "Independent package profile is incomplete.");
            packages.AddRange(PackageVerifyGraph(context, directory, id, version, api));
            profiles.Add(PackageVerifyOutput(context, directory, id));
        }
        Require(context.Markers.Count == 50 && context.BuildControlFiles == 171 && context.GeneratedControlFiles == 12, "Graph-selected structural marker or analyzer/build-control scope differs.");
        PackageVerifyCopies(context, api, version);
        PackageVerifyNativeLogs(context);
        var reviewed = PackageVerifyReviewedLocks(context, api, version);
        foreach (var path in context.Closure.Order(StringComparer.Ordinal).ToArray()) PackageCapture(context, path);
        Require(context.Closure.Count == 801, "Complete independent producer closure differs from the frozen six-profile contract.");
        var source = new Input(System.IO.Path.Combine(repository, "eng/FinalVerification/PackageIndependentEvidence.cs"), Hash(Capture(System.IO.Path.Combine(repository, "eng/FinalVerification/PackageIndependentEvidence.cs"))));
        var compiled = PackageVerifyPair(null, Assembly.GetExecutingAssembly().Location, "IQToolkit.FinalVerification.Program", "ValidateIndependentPackageProducer", source);
        return new
        {
            Status = "verified", Contract = "PACKAGE-INDEPENDENT-BINDING-1", Producer = outer, IndependentProducer = inner, ApiProducer = api,
            Profiles = profiles, Packages = packages, NativeWitness = witness, ReviewedLocks = reviewed,
            RequiredClosureFiles = context.Closure.Count, RequiredClosureBytes = context.Closure.Sum(path => new FileInfo(path).Length),
            OriginalInputRecords = context.OriginalRecords, ProjectMappings = context.Projects.Count, PhysicalEmptyMarkers = context.Markers.Count,
            InstalledAnalyzerBuildControlFiles = context.BuildControlFiles, GeneratedNuGetControlFiles = context.GeneratedControlFiles,
            AnalyzerBuildControlFiles = context.BuildControlFiles + context.GeneratedControlFiles, VerifierSource = source, CompiledVerifier = compiled,
            BackendRuntimeVerified = false, RemoteSourceRetrievalVerified = false, OtherPlatformsVerified = false,
            FullReadinessVerified = false, FullGoalComplete = false
        };
    }

    private static void PackageNoLinks(string path)
    {
        var full = System.IO.Path.GetFullPath(path);
        for (string? current = full; !string.IsNullOrEmpty(current); current = System.IO.Path.GetDirectoryName(current))
        {
            var info = new FileInfo(current);
            Require(info.LinkTarget is null && (!File.Exists(current) && !Directory.Exists(current) || (File.GetAttributes(current) & FileAttributes.ReparsePoint) == 0), "Linked package producer input or ancestor.");
        }
    }

    private static string PackageSafePath(string directory, string relative)
    {
        Require(!System.IO.Path.IsPathRooted(relative) && !relative.Contains('\\') && relative.Split('/').All(part => part.Length > 0 && part is not "." and not ".."), "Package path escapes its authenticated origin.");
        var result = System.IO.Path.GetFullPath(System.IO.Path.Combine(directory, relative));
        Require(result.StartsWith(System.IO.Path.GetFullPath(directory).TrimEnd('/') + "/", StringComparison.Ordinal), "Package path escapes its authenticated origin.");
        PackageNoLinks(result); return result;
    }

    private static string PackageMap(PackageContext context, string original)
    {
        Require(System.IO.Path.IsPathFullyQualified(original) && System.IO.Path.GetFullPath(original) == original, "Noncanonical original producer path.");
        string mapped;
        if (original.StartsWith(context.OriginalInner + "/", StringComparison.Ordinal)) mapped = PackageSafePath(context.Environment, original[(context.OriginalInner.Length + 1)..]);
        else if (original.StartsWith(context.OriginalOuter + "/", StringComparison.Ordinal)) mapped = PackageSafePath(System.IO.Path.Combine(context.Outer, "environment"), original[(context.OriginalOuter.Length + 1)..]);
        else { mapped = original; PackageNoLinks(mapped); }
        if (mapped.StartsWith(context.Environment + "/", StringComparison.Ordinal) && context.Projects.TryGetValue(mapped[(context.Environment.Length + 1)..], out var snapshot))
            mapped = PackageSafePath(context.Environment, snapshot);
        return mapped;
    }

    private static string PackageCapture(PackageContext context, string path, string? expected = null)
    {
        path = System.IO.Path.GetFullPath(path); PackageNoLinks(path);
        var info = new FileInfo(path);
        Require(info.Exists && (info.Attributes & FileAttributes.Directory) == 0, "Missing regular package producer input.");
        context.Closure.Add(path);
        if (info.Length is > 0 and <= Limit) return Capture(path, expected);
        var large = path == System.IO.Path.Combine(context.Environment, "IQToolkit.Data.SQLite/packages/sqlitepclraw.lib.e_sqlite3/2.1.12/sqlitepclraw.lib.e_sqlite3.2.1.12.nupkg") &&
            info.Length == 18762916 && info.Length <= 24L * 1024 * 1024;
        var marker = info.Length == 0 && System.IO.Path.GetFileName(path) == "_._" && context.Markers.Contains(path);
        Require(large || marker, "Unapproved oversized or empty package producer input.");
        var required = large ? PackageLargeArchiveSha : PackageEmptySha;
        Require(expected is null || expected.Equals(required, StringComparison.OrdinalIgnoreCase), "Special package input expected identity differs.");
        if (Inputs.TryGetValue(path, out var known))
        {
            Require(known == required && Hash(path) == required, "Previously captured special package input differs.");
            return Snapshots[path];
        }
        Require(Inputs.Count < MaximumInputs && bytes + info.Length <= MaximumRetainedBytes, "Special package input capture exceeds coordinator bounds.");
        Require(Hash(path) == required, "Special package input does not have its exact approved identity.");
        var snapshot = System.IO.Path.Combine(retained, $"{Inputs.Count:D4}-{info.Name}.snapshot");
        File.Copy(path, snapshot, overwrite: false);
        Require(new FileInfo(snapshot).Length == info.Length && Hash(snapshot) == required && Hash(path) == required && new FileInfo(path).Length == info.Length,
            "Special package input changed during capture.");
        PackageNoLinks(path); PackageNoLinks(snapshot);
        Inputs.Add(path, required); Snapshots.Add(path, snapshot); bytes += info.Length;
        return snapshot;
    }

    private static JsonDocument PackageJson(PackageContext context, string path)
    {
        var document = JsonDocument.Parse(File.ReadAllBytes(PackageCapture(context, path))); DuplicateCheck(document.RootElement); return document;
    }

    private static XElement PackageXml(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = Limit });
        return XDocument.Load(reader).Root ?? throw new InvalidDataException("Missing package XML root.");
    }

    private static void PackageAddTree(PackageContext context, string directory, bool recursive)
    {
        PackageNoLinks(directory); Require(Directory.Exists(directory), "Missing required producer directory.");
        foreach (var file in Directory.EnumerateFiles(directory)) { PackageNoLinks(file); context.Closure.Add(file); }
        if (recursive) foreach (var child in Directory.EnumerateDirectories(directory)) PackageAddTree(context, child, true);
        Require(context.Closure.Count <= 1024, "Producer closure selection exceeded its finite file bound.");
    }

    private static void PackageVerifyProjectMappings(PackageContext context)
    {
        var lines = File.ReadAllLines(PackageCapture(context, System.IO.Path.Combine(context.Inner, "project-filename-mapping.tsv")));
        Require(lines.Length == 7, "Expected seven inactive producer projects.");
        foreach (var line in lines)
        {
            var fields = line.Split('\t'); Require(fields.Length == 3 && fields[1] == fields[0] + ".snapshot" && Regex.IsMatch(fields[2], "^[a-f0-9]{64}$"), "Invalid project filename mapping.");
            Require(fields[0] == "tool/Verifier.csproj" || PackageProfiles.Any(id => fields[0] == id + "/Consumer.csproj"), "Unexpected inactive producer project mapping.");
            Require(context.Projects.TryAdd(fields[0], fields[1]), "Duplicate producer project mapping.");
            PackageCapture(context, PackageSafePath(context.Environment, fields[1]), fields[2]);
            Require(!File.Exists(PackageSafePath(context.Environment, fields[0])), "Retained producer project is still discoverable.");
        }
    }

    private static void PackageSelectClosure(PackageContext context)
    {
        PackageAddTree(context, context.Outer, false);
        foreach (var name in new[] { "source", "prepared-locks", "reviewed-locks", "tested-packages" }) PackageAddTree(context, System.IO.Path.Combine(context.Outer, name), true);
        PackageAddTree(context, context.Inner, false);
        foreach (var name in new[] { "source", "feed" }) PackageAddTree(context, System.IO.Path.Combine(context.Environment, name), true);
        context.Closure.Add(System.IO.Path.Combine(context.Environment, "global.json"));
        PackageAddTree(context, System.IO.Path.Combine(context.Environment, "tool"), false);
        PackageAddTree(context, System.IO.Path.Combine(context.Environment, "tool/bin/Release/net10.0"), true);
        foreach (var id in PackageProfiles)
        {
            var directory = System.IO.Path.Combine(context.Environment, id);
            PackageAddTree(context, System.IO.Path.Combine(context.Inner, id), false);
            foreach (var name in new[] { "Consumer.csproj.snapshot", "PublicSymbols.cs", "NuGet.config", "packages.lock.json", "obj/project.assets.json" }) context.Closure.Add(System.IO.Path.Combine(directory, name));
            PackageAddTree(context, System.IO.Path.Combine(directory, "feed"), true); PackageAddTree(context, System.IO.Path.Combine(directory, "bin"), true);
            foreach (var path in Directory.EnumerateFiles(System.IO.Path.Combine(directory, "obj"), "*.cs", SearchOption.AllDirectories)) { PackageNoLinks(path); context.Closure.Add(path); }
            foreach (var path in Directory.EnumerateFiles(System.IO.Path.Combine(directory, "obj"), "*.nuget.g.*")) { PackageNoLinks(path); context.Closure.Add(path); context.GeneratedControlFiles++; }
        }
        var relative = File.ReadAllText(PackageCapture(context, System.IO.Path.Combine(context.Outer, "verifier-relative-path.txt"))).Trim();
        var preparationDll = PackageSafePath(System.IO.Path.Combine(context.Outer, "environment"), relative);
        var preparationTool = System.IO.Path.GetFullPath(System.IO.Path.Combine(preparationDll, "../../../.."));
        PackageAddTree(context, preparationTool, false); PackageAddTree(context, System.IO.Path.Combine(preparationTool, "bin/Release/net10.0"), true);
    }

    private static void PackageAuthenticateOriginalInputs(PackageContext context, string directory)
    {
        var before = File.ReadAllBytes(PackageCapture(context, System.IO.Path.Combine(directory, "inputs-before.sha256")));
        Require(before.AsSpan().SequenceEqual(File.ReadAllBytes(PackageCapture(context, System.IO.Path.Combine(directory, "inputs-after.sha256")))), "Original producer inputs changed during execution.");
        foreach (var line in Encoding.UTF8.GetString(before).Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = Regex.Match(line.TrimEnd('\r'), "^([a-f0-9]{64})  (.+)$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
            Require(match.Success && !match.Groups[2].Value.StartsWith('\\'), "Unsupported original producer input record.");
            var original = match.Groups[2].Value;
            var actual = System.IO.Path.IsPathFullyQualified(original) ? PackageMap(context, original) : PackageSafePath(repository, original);
            PackageCapture(context, actual, match.Groups[1].Value);
            if (directory == context.Outer && !System.IO.Path.IsPathFullyQualified(original))
                PackageCapture(context, PackageSafePath(System.IO.Path.Combine(context.Outer, "source"), original), match.Groups[1].Value);
            if (directory == context.Inner)
            {
                var templatePrefix = context.OriginalOuter + "/eng/IndependentConsumers/";
                var copied = original.StartsWith(templatePrefix, StringComparison.Ordinal)
                    ? PackageSafePath(System.IO.Path.Combine(context.Environment, "source"), original[templatePrefix.Length..])
                    : original.StartsWith(context.OriginalOuter + "/feed/", StringComparison.Ordinal)
                        ? PackageSafePath(System.IO.Path.Combine(context.Environment, "feed"), System.IO.Path.GetFileName(original))
                        : PackageSafePath(System.IO.Path.Combine(context.Environment, "source"), System.IO.Path.GetFileName(original));
                PackageCapture(context, copied, match.Groups[1].Value);
            }
            context.OriginalRecords++;
        }
    }

    private static string PackageSha512(string path)
    {
        using var stream = File.OpenRead(path); return Convert.ToBase64String(SHA512.HashData(stream));
    }

    private static byte[] PackageEntryBytes(ZipArchive archive, string relative)
    {
        var entry = archive.GetEntry(relative) ?? throw new InvalidDataException("Graph-selected package entry is absent from its whole archive.");
        Require(entry.Length is >= 0 and <= Limit, "Package entry exceeds bounded inspection size.");
        using var stream = entry.Open(); using var output = new MemoryStream();
        var buffer = new byte[8192];
        for (var count = stream.Read(buffer, 0, buffer.Length); count > 0; count = stream.Read(buffer, 0, buffer.Length))
        {
            Require(output.Length + count <= Limit && output.Length + count <= entry.Length, "Package entry exceeds its declared bounded size.");
            output.Write(buffer, 0, count);
        }
        Require(output.Length == entry.Length, "Package entry length changed while read."); return output.ToArray();
    }

    private static string PackageArchiveIdentity(string path, string id, string? version)
    {
        using var archive = ZipFile.OpenRead(path);
        var entries = archive.Entries.Where(entry => entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase)).ToArray();
        Require(entries.Length == 1 && entries[0].Length <= Limit, "Package nuspec identity is ambiguous or oversized.");
        using var stream = entries[0].Open();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = Limit });
        var metadata = XDocument.Load(reader).Root?.Elements().Single(element => element.Name.LocalName == "metadata") ?? throw new InvalidDataException("Missing package metadata.");
        var actual = metadata.Elements().Single(element => element.Name.LocalName == "version").Value;
        Require(metadata.Elements().Single(element => element.Name.LocalName == "id").Value == id && (version is null || actual == version), "Package nuspec ID/version differs from the resolved graph.");
        return actual;
    }

    private static JsonElement PackageFramework(JsonElement frameworks)
    {
        var entries = frameworks.EnumerateObject().ToArray(); Require(entries.Length == 1 && entries[0].Name == "net10.0", "Unexpected consumer target framework."); return entries[0].Value;
    }

    private static bool PackageSamePath(PackageContext context, string? original, string retainedPath) => original is not null && PackageMap(context, original.TrimEnd('/')) == retainedPath.TrimEnd('/');

    private static IEnumerable<object> PackageVerifyGraph(PackageContext context, string directory, string package, string version, string api)
    {
        var project = PackageXml(PackageCapture(context, System.IO.Path.Combine(directory, "Consumer.csproj.snapshot")));
        Require(!project.Descendants("ProjectReference").Any() && !project.Descendants("Reference").Any(), "Independent consumer uses project or copied-assembly references.");
        var references = project.Descendants("PackageReference").ToArray();
        Require(references.Length == 1 && (string?)references[0].Attribute("Include") == package && (string?)references[0].Attribute("Version") == "[" + version + "]" &&
            project.Descendants("TargetFramework").Single().Value == "net10.0", "Consumer project is not the sole exact package/profile.");
        Require(File.ReadAllBytes(PackageCapture(context, System.IO.Path.Combine(directory, "Consumer.csproj.snapshot"))).AsSpan().SequenceEqual(
            File.ReadAllBytes(PackageCapture(context, System.IO.Path.Combine(context.Environment, "source/Profiles", package, "Consumer.csproj.template")))) &&
            File.ReadAllBytes(PackageCapture(context, System.IO.Path.Combine(directory, "PublicSymbols.cs"))).AsSpan().SequenceEqual(
            File.ReadAllBytes(PackageCapture(context, System.IO.Path.Combine(context.Environment, "source/Profiles", package, "PublicSymbols.cs")))), "Actual compiled project/source differ from captured templates.");
        var config = PackageXml(PackageCapture(context, System.IO.Path.Combine(directory, "NuGet.config")));
        var sourcesConfig = config.Element("packageSources") ?? throw new InvalidDataException("Missing consumer NuGet sources.");
        var sourceEntries = sourcesConfig.Elements("add").ToArray();
        Require(sourcesConfig.Element("clear") is not null && sourceEntries.Length == 2 && sourceEntries.Any(item => (string?)item.Attribute("key") == "local-packages" && (string?)item.Attribute("value") == "feed") &&
            sourceEntries.Any(item => (string?)item.Attribute("key") == "nuget.org" && (string?)item.Attribute("value") == PackagePublicFeed), "Consumer NuGet source boundary differs.");
        var mapping = config.Element("packageSourceMapping") ?? throw new InvalidDataException("Missing package-source mapping.");
        Require(mapping.Element("clear") is not null && mapping.Elements("packageSource").Count() == 2, "Consumer source mapping must replace inherited mappings.");
        foreach (var item in mapping.Elements("packageSource"))
        {
            var patterns = item.Elements("package").Select(value => (string?)value.Attribute("pattern") ?? throw new InvalidDataException("Missing source-mapping pattern.")).ToHashSet(StringComparer.Ordinal);
            Require((string?)item.Attribute("key") == "local-packages" ? patterns.SetEquals(new[] { "IQToolkit", "IQToolkit.*" }) :
                (string?)item.Attribute("key") == "nuget.org" && patterns.SetEquals(new[] { "*" }), "Unexpected package-source mapping.");
        }
        var feed = System.IO.Path.Combine(directory, "feed");
        var feedFiles = Directory.EnumerateFiles(feed, "*.nupkg").ToArray(); Require(feedFiles.Length == 6, "Consumer local feed does not contain exactly six toolkit archives.");
        foreach (var id in PackageProfiles)
        {
            var name = id + "." + version + ".nupkg";
            var ownFeed = PackageCapture(context, System.IO.Path.Combine(feed, name));
            var tested = PackageCapture(context, System.IO.Path.Combine(context.Outer, "tested-packages", name));
            Require(File.ReadAllBytes(ownFeed).AsSpan().SequenceEqual(File.ReadAllBytes(tested)) && File.ReadAllBytes(tested).AsSpan().SequenceEqual(File.ReadAllBytes(Capture(System.IO.Path.Combine(api, "packages", name)))),
                "Independent feed/archive bytes differ from the authenticated API feed.");
            PackageArchiveIdentity(ownFeed, id, version);
        }
        using var assetsDocument = PackageJson(context, System.IO.Path.Combine(directory, "obj/project.assets.json"));
        using var lockDocument = PackageJson(context, System.IO.Path.Combine(directory, "packages.lock.json"));
        var assets = assetsDocument.RootElement; var locked = PackageFramework(lockDocument.RootElement.GetProperty("dependencies"));
        var projectMetadata = assets.GetProperty("project"); var restore = projectMetadata.GetProperty("restore");
        var cache = System.IO.Path.Combine(directory, "packages");
        Require(PackageSamePath(context, restore.GetProperty("projectPath").GetString(), System.IO.Path.Combine(directory, "Consumer.csproj.snapshot")) &&
            PackageSamePath(context, restore.GetProperty("projectUniqueName").GetString(), System.IO.Path.Combine(directory, "Consumer.csproj.snapshot")) &&
            restore.GetProperty("projectName").GetString() == "Consumer", "Assets identify another compiled project.");
        Require(PackageSamePath(context, restore.GetProperty("packagesPath").GetString(), cache), "Consumer restored into another package cache.");
        var folders = assets.GetProperty("packageFolders").EnumerateObject().ToArray();
        Require(folders.Length == 1 && PackageSamePath(context, folders[0].Name, cache), "Consumer used a fallback package cache.");
        var sources = restore.GetProperty("sources").EnumerateObject().Select(item => item.Name).ToArray();
        Require(sources.Length == 2 && sources.Contains(PackagePublicFeed, StringComparer.Ordinal) && sources.Any(source => source != PackagePublicFeed && PackageSamePath(context, source, feed)), "Assets restore sources differ.");
        var configs = restore.GetProperty("configFilePaths").EnumerateArray().ToArray();
        Require(configs.Length == 1 && PackageSamePath(context, configs[0].GetString(), System.IO.Path.Combine(directory, "NuGet.config")), "Restore inherited another NuGet configuration.");
        Require(!PackageFramework(restore.GetProperty("frameworks")).GetProperty("projectReferences").EnumerateObject().Any(), "Assets contain project references.");
        var direct = PackageFramework(projectMetadata.GetProperty("frameworks")).GetProperty("dependencies").EnumerateObject().ToArray();
        Require(direct.Length == 1 && direct[0].Name == package && direct[0].Value.GetProperty("target").GetString() == "Package" && direct[0].Value.GetProperty("version").GetString() == "[" + version + ", " + version + "]", "Assets sole-direct package/version differs.");
        var groups = PackageFramework(assets.GetProperty("projectFileDependencyGroups")).EnumerateArray().ToArray();
        Require(groups.Length == 1 && groups[0].GetString() == package + " >= " + version + " <= " + version, "Project-file direct dependencies differ.");
        var lockEntries = locked.EnumerateObject().ToArray(); var lockDirect = lockEntries.Where(item => item.Value.GetProperty("type").GetString() == "Direct").ToArray();
        Require(lockDirect.Length == 1 && lockDirect[0].Name == package && lockDirect[0].Value.GetProperty("resolved").GetString() == version &&
            lockDirect[0].Value.GetProperty("requested").GetString() == "[" + version + ", " + version + "]", "Lock sole-direct package/version/range differs.");
        var libraries = assets.GetProperty("libraries").EnumerateObject().ToArray(); var targets = PackageFramework(assets.GetProperty("targets"));
        Require(libraries.Length == lockEntries.Length && targets.EnumerateObject().Count() == libraries.Length, "Lock/assets/targets package counts differ.");
        var toolkit = new HashSet<string>(StringComparer.Ordinal); var allIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var observations = new List<object>();
        foreach (var library in libraries)
        {
            Require(library.Value.GetProperty("type").GetString() == "package", "Assets contain a nonpackage library.");
            var identity = library.Name.Split('/'); Require(identity.Length == 2 && allIds.Add(identity[0]), "Malformed or duplicate asset package identity.");
            var id = identity[0]; var resolved = identity[1]; var target = targets.GetProperty(library.Name);
            Require(target.GetProperty("type").GetString() == "package", "Target contains a nonpackage library.");
            Require(locked.TryGetProperty(id, out var entry) && entry.GetProperty("resolved").GetString() == resolved, "Target/lock resolved identity differs.");
            var toolkitPackage = id == "IQToolkit" || id.StartsWith("IQToolkit.", StringComparison.Ordinal);
            if (toolkitPackage) { Require(PackageProfiles.Contains(id, StringComparer.Ordinal) && resolved == version, "Unknown toolkit node/version."); toolkit.Add(id); }
            var targetEdges = target.TryGetProperty("dependencies", out var targetDependencies) ? targetDependencies.EnumerateObject().ToArray() : [];
            var lockEdges = entry.TryGetProperty("dependencies", out var lockDependencies) ? lockDependencies.EnumerateObject().ToArray() : [];
            Require(targetEdges.Length == lockEdges.Length && targetEdges.All(edge => lockEdges.Any(other => other.Name == edge.Name && JsonElement.DeepEquals(other.Value, edge.Value))) &&
                targetEdges.All(edge => locked.TryGetProperty(edge.Name, out _)), "Lock and asset dependency edges differ or refer to absent nodes.");
            var relative = library.Value.GetProperty("path").GetString()!;
            Require(relative == id.ToLowerInvariant() + "/" + resolved.ToLowerInvariant(), "Package cache path differs from resolved identity.");
            var installed = PackageSafePath(cache, relative); var archives = Directory.EnumerateFiles(installed, "*.nupkg").ToArray();
            Require(archives.Length == 1, "Installed package whole archive is missing or ambiguous.");
            var archivePath = PackageCapture(context, archives[0]); var archiveHash = PackageSha512(archivePath);
            using var sourceMetadata = PackageJson(context, System.IO.Path.Combine(installed, ".nupkg.metadata"));
            var source = sourceMetadata.RootElement.GetProperty("source").GetString();
            Require(toolkitPackage ? PackageSamePath(context, source, feed) : source == PackagePublicFeed, "Package was restored from an unexpected source.");
            var contentHash = sourceMetadata.RootElement.GetProperty("contentHash").GetString();
            Require(contentHash is not null && contentHash == library.Value.GetProperty("sha512").GetString() && contentHash == entry.GetProperty("contentHash").GetString(), "Logical content hashes differ across cache metadata/assets/lock.");
            Require(archiveHash == File.ReadAllText(PackageCapture(context, archives[0] + ".sha512")).Trim(), "Whole archive SHA512 sidecar differs.");
            using var archive = ZipFile.OpenRead(archivePath);
            Require(archiveHash == contentHash || !toolkitPackage && archive.GetEntry(".signature.p7s") is not null, "Unsigned archive/logical content hash differs.");
            PackageArchiveIdentity(archivePath, id, resolved);
            if (toolkitPackage)
            {
                var feedPath = PackageCapture(context, System.IO.Path.Combine(feed, id + "." + version + ".nupkg"));
                Require(archiveHash == contentHash && archiveHash == PackageSha512(feedPath), "Unsigned toolkit archive differs from tested feed/logical hash.");
                var dll = "lib/net10.0/" + id + ".dll";
                Require(PackageEntryBytes(archive, dll).AsSpan().SequenceEqual(File.ReadAllBytes(PackageCapture(context, PackageSafePath(installed, dll)))), "Installed toolkit DLL differs from whole archive.");
            }
            var controlPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in new[] { "compile", "build", "buildMultiTargeting", "buildTransitive", "analyzers" })
                if (target.TryGetProperty(group, out var selected)) foreach (var asset in selected.EnumerateObject())
                {
                    if (System.IO.Path.GetFileName(asset.Name) == "_._") continue;
                    var actual = PackageSafePath(installed, asset.Name);
                    Require(PackageEntryBytes(archive, asset.Name).AsSpan().SequenceEqual(File.ReadAllBytes(PackageCapture(context, actual))), "Selected compile/build asset differs from whole archive.");
                    if (group != "compile") controlPaths.Add(actual);
                }
            foreach (var file in library.Value.GetProperty("files").EnumerateArray())
            {
                var name = file.GetString()!; var first = name.Split('/')[0];
                if (first is not ("analyzers" or "build" or "buildTransitive" or "buildMultiTargeting")) continue;
                var actual = PackageSafePath(installed, name); if (!File.Exists(actual)) continue;
                controlPaths.Add(actual);
                if (System.IO.Path.GetFileName(actual) == "_._")
                {
                    Require(new FileInfo(actual).Length == 0 && archive.GetEntry(name)?.Length == 0, "Graph-selected structural marker is not authentic empty archive content.");
                    context.Markers.Add(actual);
                }
                Require(PackageEntryBytes(archive, name).AsSpan().SequenceEqual(File.ReadAllBytes(PackageCapture(context, actual))), "Installed analyzer/build control differs from whole archive.");
            }
            context.BuildControlFiles += controlPaths.Count;
            observations.Add(new { Profile = package, Package = library.Name, Source = source, ArchiveSha512 = archiveHash, ContentHash = contentHash, DependencyEdges = targetEdges.Length });
        }
        var expectedToolkit = package == "IQToolkit" ? new[] { "IQToolkit" } : package == "IQToolkit.Data" ? new[] { "IQToolkit", "IQToolkit.Data" } : new[] { "IQToolkit", "IQToolkit.Data", package };
        Require(toolkit.SetEquals(expectedToolkit) && (!PackageDrivers.TryGetValue(package, out var driver) || allIds.Contains(driver)), "Independent graph lacks required toolkit/driver nodes.");
        return observations;
    }

    private static object PackageVerifyPair(PackageContext? context, string dllPath, string typeName, string methodName, Input source)
    {
        PackageNoLinks(dllPath);
        var pdbPath = System.IO.Path.ChangeExtension(dllPath, ".pdb"); PackageNoLinks(pdbPath);
        var dllCopy = context is null ? Capture(dllPath) : PackageCapture(context, dllPath);
        var pdbCopy = context is null ? Capture(pdbPath) : PackageCapture(context, pdbPath);
        var dll = File.ReadAllBytes(dllCopy); var pdbBytes = File.ReadAllBytes(pdbCopy);
        using var pe = new PEReader(new MemoryStream(dll));
        using var provider = MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(pdbBytes));
        var pdb = provider.GetMetadataReader(); var header = pdb.DebugMetadataHeader ?? throw new InvalidDataException("Missing portable PDB identity.");
        var views = pe.ReadDebugDirectory().Where(entry => entry.Type == DebugDirectoryEntryType.CodeView).ToArray();
        Require(header.Id.Length == 20 && views.Length == 1 && views[0].IsPortableCodeView, "Consumer/verifier portable identity differs.");
        var view = pe.ReadCodeViewDebugDirectoryData(views[0]);
        Require(view.Age == 1 && new BlobContentId(header.Id) == new BlobContentId(view.Guid, views[0].Stamp), "Consumer/verifier DLL and PDB identities differ.");
        var checksums = pe.ReadDebugDirectory().Where(entry => entry.Type == DebugDirectoryEntryType.PdbChecksum).ToArray();
        Require(checksums.Length == 1 && header.IdStartOffset >= 0 && header.IdStartOffset <= pdbBytes.Length - header.Id.Length, "Missing portable PDB checksum.");
        var checksum = pe.ReadPdbChecksumDebugDirectoryData(checksums[0]);
        var normalized = (byte[])pdbBytes.Clone(); Array.Clear(normalized, header.IdStartOffset, header.Id.Length);
        Require(checksum.AlgorithmName == "SHA256" && checksum.Checksum.AsSpan().SequenceEqual(SHA256.HashData(normalized)), "Portable PDB checksum differs from compiled DLL.");
        var metadata = pe.GetMetadataReader();
        var types = metadata.TypeDefinitions.Where(handle =>
        {
            var type = metadata.GetTypeDefinition(handle); return metadata.GetString(type.Namespace) + "." + metadata.GetString(type.Name) == typeName;
        }).ToArray(); Require(types.Length == 1, "Compiled producer/consumer class differs.");
        var methods = metadata.GetTypeDefinition(types[0]).GetMethods().Where(handle => metadata.GetString(metadata.GetMethodDefinition(handle).Name) == methodName).ToArray();
        Require(methods.Length == 1, "Compiled producer/consumer method differs.");
        var method = metadata.GetMethodDefinition(methods[0]);
        Require(method.RelativeVirtualAddress != 0 && (method.Attributes & MethodAttributes.Static) != 0, "Compiled static public-symbol/helper body is absent.");
        var body = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes() ?? throw new InvalidDataException("Missing compiled method IL.");
        Require(body.Length > 0, "Compiled public-symbol/helper body is empty.");
        var sourceHash = source.Sha256.ToUpperInvariant();
        if (context is null) Capture(source.Path, source.Sha256); else PackageCapture(context, source.Path, source.Sha256);
        var documents = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var handle in pdb.Documents)
        {
            var document = pdb.GetDocument(handle); var name = pdb.GetString(document.Name);
            Require(pdb.GetGuid(document.HashAlgorithm) == PackageDocumentSha256, "Unsupported compiled source document checksum.");
            var expected = Convert.ToHexString(pdb.GetBlobBytes(document.Hash));
            Require(documents.TryAdd(name, expected), "Duplicate compiled source document.");
            if (context is not null && typeName == "IQToolkit.IndependentConsumer.PublicSymbols")
                PackageCapture(context, PackageMap(context, name), expected);
        }
        var info = pdb.GetMethodDebugInformation(methods[0].ToDebugInformationHandle());
        var points = info.GetSequencePoints().Where(point => !point.IsHidden).ToArray();
        Require(points.Length > 0 && points.All(point =>
        {
            var handle = point.Document.IsNil ? info.Document : point.Document;
            if (handle.IsNil) return false;
            var document = pdb.GetDocument(handle); var name = pdb.GetString(document.Name);
            return System.IO.Path.GetFileName(name) == System.IO.Path.GetFileName(source.Path) &&
                Convert.ToHexString(pdb.GetBlobBytes(document.Hash)) == sourceHash;
        }), "Compiled method does not bind the exact reviewed source checksum.");
        var references = metadata.AssemblyReferences.Select(handle => metadata.GetString(metadata.GetAssemblyReference(handle).Name)).Order(StringComparer.Ordinal).ToArray();
        return new
        {
            Dll = new Input(dllPath, Hash(dllCopy)), Pdb = new Input(pdbPath, Hash(pdbCopy)), Assembly = metadata.GetString(metadata.GetAssemblyDefinition().Name),
            Class = typeName, Method = methodName, BodySha256 = Convert.ToHexString(SHA256.HashData(body)), Source = source,
            Documents = documents, References = references, PortablePdbChecksumVerified = true
        };
    }

    private static object PackageVerifyOutput(PackageContext context, string directory, string package)
    {
        var dll = System.IO.Path.Combine(directory, "bin/Release/net10.0/Consumer.dll");
        var sourcePath = System.IO.Path.Combine(directory, "PublicSymbols.cs");
        var source = new Input(sourcePath, Hash(PackageCapture(context, sourcePath)));
        var proof = PackageVerifyPair(context, dll, "IQToolkit.IndependentConsumer.PublicSymbols", package == "IQToolkit" ? "Execute" : "Create", source);
        var element = JsonSerializer.SerializeToElement(proof);
        Require(element.GetProperty("Assembly").GetString() == "Consumer", "Consumer output assembly name differs.");
        var references = element.GetProperty("References").EnumerateArray().Select(item => item.GetString()!).ToHashSet(StringComparer.Ordinal);
        Require(references.Contains("IQToolkit") && (package == "IQToolkit" || references.Contains("IQToolkit.Data") && references.Contains(package)) &&
            (!PackageDrivers.TryGetValue(package, out var driver) || references.Contains(driver)), "Public-symbol consumer lacks the actual toolkit/transitive-driver assembly references.");
        using var output = new PEReader(new MemoryStream(File.ReadAllBytes(PackageCapture(context, dll))));
        var metadata = output.GetMetadataReader();
        var type = metadata.TypeDefinitions.Select(metadata.GetTypeDefinition).Single(value => metadata.GetString(value.Name) == "PublicSymbols");
        var method = type.GetMethods().Select(metadata.GetMethodDefinition).Single(value => metadata.GetString(value.Name) == (package == "IQToolkit" ? "Execute" : "Create"));
        Require((method.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public, "Public-symbol compiled method is not public.");
        var log = File.ReadAllText(PackageCapture(context, System.IO.Path.Combine(context.Inner, package, "compile-output.log")));
        var records = log.Split('\n').Where(line => line.StartsWith("{", StringComparison.Ordinal)).Select(line =>
        {
            using var document = JsonDocument.Parse(line); DuplicateCheck(document.RootElement); return document.RootElement.Clone();
        }).ToArray();
        Require(records.Length == 1 && records[0].GetProperty("Package").GetString() == package &&
            PackageSamePath(context, records[0].GetProperty("Output").GetString(), dll) &&
            records[0].GetProperty("Sha256").GetString() == Hash(PackageCapture(context, dll)) &&
            records[0].GetProperty("References").EnumerateArray().Select(item => item.GetString()!).ToHashSet(StringComparer.Ordinal).SetEquals(references) &&
            log.Contains("Public-symbol compilation verified; no provider/backend code executed.", StringComparison.Ordinal), "Native public-symbol output does not bind the retained compiled output/references.");
        return new { Package = package, Compiled = proof, BackendRuntimeVerified = false };
    }

    private static object PackageNativeWitness(string outer, string api)
    {
        var area = System.IO.Path.Combine(repository, "artifacts/final-verification/runs"); PackageNoLinks(area);
        var candidates = Directory.EnumerateDirectories(area).Take(257).ToArray(); Require(candidates.Length <= 256, "Native consumer witness discovery exceeds its finite bound.");
        var matches = new List<string>();
        var evidenceLine = "CI consumer evidence: " + outer;
        foreach (var directory in candidates)
        {
            PackageNoLinks(directory); var path = System.IO.Path.Combine(directory, "consumers.log");
            if (!File.Exists(path)) continue; PackageNoLinks(path);
            var info = new FileInfo(path); Require(info.Length <= Limit, "Native consumer witness candidate log exceeds its bound.");
            if (File.ReadLines(path).Any(line => line == evidenceLine)) matches.Add(directory);
        }
        Require(matches.Count == 1, "Producer lacks a unique native completed consumer command witness.");
        var selected = matches[0];
        var logPath = System.IO.Path.Combine(selected, "consumers.log"); var log = File.ReadAllText(Capture(logPath));
        Require(log.Split('\n').Count(line => line.TrimEnd('\r') == evidenceLine) == 1, "Native producer evidence identity is duplicate or changed.");
        foreach (var name in new[] { "consumers.exit.txt", "consumers.process-exit.txt" })
            Require(File.ReadAllText(Capture(System.IO.Path.Combine(selected, name))).Trim() == "0", "Native consumer command did not exit successfully.");
        var command = File.ReadAllText(Capture(System.IO.Path.Combine(selected, "consumers.command.txt"))).Trim();
        Require(command == "/bin/bash eng/verify-ci-consumers.sh " + System.IO.Path.Combine(api, "packages"), "Native consumer command used another operation/API feed.");
        var pidPath = System.IO.Path.Combine(selected, "consumers.pid.txt");
        Require(int.TryParse(File.ReadAllText(Capture(pidPath)).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var pid) && pid > 0, "Native consumer process identity is missing.");
        var identity = File.ReadAllText(Capture(System.IO.Path.Combine(selected, "consumers.identity.txt")));
        Require(identity.Split('\n', StringSplitOptions.RemoveEmptyEntries).Any(line => Regex.IsMatch(line, "^" + pid.ToString(CultureInfo.InvariantCulture) + "[ \\t]+" + pid.ToString(CultureInfo.InvariantCulture) + "[ \\t]+" + pid.ToString(CultureInfo.InvariantCulture) + "[ \\t]+", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))), "Native consumer session identity differs.");
        return new { Directory = selected, Log = new Input(logPath, Hash(Capture(logPath))), Pid = pid, NativeExit = 0, ProcessExit = 0, ParentResultRequired = false };
    }

    private static void PackageVerifyCopies(PackageContext context, string api, string version)
    {
        Require(File.ReadAllText(PackageCapture(context, System.IO.Path.Combine(context.Outer, "package-directory.txt"))).Trim() == System.IO.Path.Combine(api, "packages") &&
            File.ReadAllText(PackageCapture(context, System.IO.Path.Combine(context.Inner, "package-directory.txt"))).Trim() == context.OriginalOuter + "/feed",
            "Producer feed pointers differ from the authenticated API and original shadow feed.");
        var feedRecords = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var id in PackageProfiles)
        {
            var name = id + "." + version + ".nupkg";
            var expected = Hash(Capture(System.IO.Path.Combine(api, "packages", name)));
            feedRecords.Add(name, expected);
            PackageCapture(context, System.IO.Path.Combine(context.Environment, "feed", name), expected);
        }
        foreach (var manifest in new[] { System.IO.Path.Combine(context.Outer, "tested-packages.sha256"), System.IO.Path.Combine(context.Inner, "feed.sha256"),
            System.IO.Path.Combine(context.Environment, "source/prepared-feed.sha256") })
        {
            var observed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in File.ReadAllLines(PackageCapture(context, manifest)))
            {
                var match = Regex.Match(line, "^([a-f0-9]{64})  (.+)$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
                Require(match.Success && observed.Add(match.Groups[2].Value) && feedRecords.TryGetValue(match.Groups[2].Value, out var expected) &&
                    match.Groups[1].Value.Equals(expected, StringComparison.OrdinalIgnoreCase), "Captured feed manifest differs from the complete actual API archive set.");
            }
            Require(observed.SetEquals(feedRecords.Keys), "Captured feed manifest omits toolkit archives.");
        }
        var copiedRecords = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in File.ReadAllLines(PackageCapture(context, System.IO.Path.Combine(context.Outer, "copied-sources.sha256"))))
        {
            var match = Regex.Match(line, "^([a-f0-9]{64})  (.+)$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
            Require(match.Success && copiedRecords.Add(match.Groups[2].Value), "Invalid or duplicate original copied-source record.");
            PackageCapture(context, PackageSafePath(System.IO.Path.Combine(context.Outer, "source"), match.Groups[2].Value), match.Groups[1].Value);
            Capture(PackageSafePath(repository, match.Groups[2].Value), match.Groups[1].Value);
        }
        Require(copiedRecords.Count == 35, "Original CI copied-source scope differs.");
        foreach (var name in new[] { "global.json", "Program.cs", "NuGet.config", "Verifier.csproj.snapshot" })
        {
            var actual = name == "global.json" ? System.IO.Path.Combine(context.Environment, name) : System.IO.Path.Combine(context.Environment, "tool", name);
            var original = name == "global.json" ? System.IO.Path.Combine(context.Environment, "source/global.json")
                : System.IO.Path.Combine(context.Environment, "source/Verifier", name == "Verifier.csproj.snapshot" ? "Verifier.csproj.template" : name);
            Require(File.ReadAllBytes(PackageCapture(context, actual)).AsSpan().SequenceEqual(File.ReadAllBytes(PackageCapture(context, original))),
                "Actual verifier project/config/source or SDK selection differs from copied inputs.");
        }
        foreach (var id in PackageProfiles)
        {
            var directory = System.IO.Path.Combine(context.Environment, id);
            foreach (var name in new[] { "NuGet.config", "packages.lock.json" })
            {
                var original = name == "NuGet.config" ? System.IO.Path.Combine(context.Environment, "source", name)
                    : System.IO.Path.Combine(context.Environment, "source/Profiles", id, name);
                Require(File.ReadAllBytes(PackageCapture(context, System.IO.Path.Combine(directory, name))).AsSpan().SequenceEqual(File.ReadAllBytes(PackageCapture(context, original))),
                    "Actual consumer config or lock differs from copied inputs.");
            }
        }
    }

    private static void PackageVerifyNativeLogs(PackageContext context)
    {
        var aggregate = File.ReadAllText(PackageCapture(context, System.IO.Path.Combine(context.Outer, "verify-independent.log")));
        Require(aggregate.Contains("All six independent package graphs restored locked unchanged and compiled their named public symbols.", StringComparison.Ordinal) &&
            aggregate.Split('\n').Count(line => line.TrimEnd('\r') == "Independent package verify evidence: " + context.OriginalOuter + context.Inner[(System.IO.Path.Combine(context.Outer, "environment")).Length..]) == 1,
            "Native independent producer aggregate is incomplete or points to another run.");
        Require(File.ReadAllText(PackageCapture(context, System.IO.Path.Combine(context.Inner, "retained-environment.txt"))).Trim() ==
            context.OriginalOuter + context.Inner[(System.IO.Path.Combine(context.Outer, "environment")).Length..] + "/environment", "Inner retained environment origin differs.");
        foreach (var id in PackageProfiles)
        {
            var build = File.ReadAllText(PackageCapture(context, System.IO.Path.Combine(context.Inner, id, "build.log")));
            Require(build.Contains("Build succeeded.", StringComparison.Ordinal) && build.Contains("0 Warning(s)", StringComparison.Ordinal) && build.Contains("0 Error(s)", StringComparison.Ordinal),
                "Native strict profile build did not complete without warnings/errors.");
            var restore = File.ReadAllText(PackageCapture(context, System.IO.Path.Combine(context.Inner, id, "restore.log")));
            Require(restore.Contains("Restored ", StringComparison.Ordinal) && !Regex.IsMatch(restore, "\\b(?:warning|error) (?:NU|MSB|CS)[0-9]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)),
                "Native locked profile restore is absent or reported warnings/errors.");
            using var assets = PackageJson(context, System.IO.Path.Combine(context.Environment, id, "obj/project.assets.json"));
            var expected = assets.RootElement.GetProperty("libraries").EnumerateObject().Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
            var provenance = File.ReadAllText(PackageCapture(context, System.IO.Path.Combine(context.Inner, id, "provenance.log")));
            var observed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in provenance.Split('\n').Where(line => line.StartsWith("{", StringComparison.Ordinal)))
            {
                using var record = JsonDocument.Parse(line); DuplicateCheck(record.RootElement);
                var package = record.RootElement.GetProperty("Package").GetString()!; Require(observed.Add(package), "Duplicate native package provenance observation.");
                var library = assets.RootElement.GetProperty("libraries").GetProperty(package);
                var installed = PackageSafePath(System.IO.Path.Combine(context.Environment, id, "packages"), library.GetProperty("path").GetString()!);
                using var metadata = PackageJson(context, System.IO.Path.Combine(installed, ".nupkg.metadata"));
                var archive = Directory.EnumerateFiles(installed, "*.nupkg").Single();
                Require(record.RootElement.GetProperty("Source").GetString() == metadata.RootElement.GetProperty("source").GetString() &&
                    record.RootElement.GetProperty("ContentHash").GetString() == library.GetProperty("sha512").GetString() &&
                    record.RootElement.GetProperty("ArchiveSha512").GetString() == PackageSha512(PackageCapture(context, archive)), "Native package provenance disagrees with actual retained archive/graph.");
            }
            Require(expected.SetEquals(observed) && provenance.Contains("Verified sole direct dependency and complete package provenance: " + id + ".", StringComparison.Ordinal), "Native complete package provenance did not cover its actual graph.");
            var evidenceAssets = File.ReadAllBytes(PackageCapture(context, System.IO.Path.Combine(context.Inner, id, "project.assets.json")));
            Require(evidenceAssets.AsSpan().SequenceEqual(File.ReadAllBytes(PackageCapture(context, System.IO.Path.Combine(context.Environment, id, "obj/project.assets.json")))) &&
                File.ReadAllBytes(PackageCapture(context, System.IO.Path.Combine(context.Inner, id, "packages.lock.json"))).AsSpan().SequenceEqual(File.ReadAllBytes(PackageCapture(context, System.IO.Path.Combine(context.Environment, id, "packages.lock.json")))),
                "Native evidence assets/lock differ from the compiled profile.");
        }
        var producerSourcePath = System.IO.Path.Combine(repository, "eng/IndependentConsumers/Verifier/Program.cs");
        var producerSourceHash = Hash(Capture(producerSourcePath));
        foreach (var path in new[] { System.IO.Path.Combine(context.Environment, "source/Verifier/Program.cs"), System.IO.Path.Combine(context.Environment, "tool/Program.cs") })
            PackageCapture(context, path, producerSourceHash);
        var toolBuild = File.ReadAllText(PackageCapture(context, System.IO.Path.Combine(context.Inner, "verifier-build.log")));
        Require(toolBuild.Contains("Build succeeded.", StringComparison.Ordinal) && toolBuild.Contains("0 Warning(s)", StringComparison.Ordinal) && toolBuild.Contains("0 Error(s)", StringComparison.Ordinal), "Native producer verifier build failed or warned.");
        PackageVerifyPair(context, System.IO.Path.Combine(context.Environment, "tool/bin/Release/net10.0/Verifier.dll"), "IQToolkit.IndependentConsumers.Verifier.Program", "VerifyGraph",
            new Input(System.IO.Path.Combine(context.Environment, "tool/Program.cs"), producerSourceHash));
        var relative = File.ReadAllText(PackageCapture(context, System.IO.Path.Combine(context.Outer, "verifier-relative-path.txt"))).Trim();
        var preparationDll = PackageSafePath(System.IO.Path.Combine(context.Outer, "environment"), relative);
        var preparationSource = System.IO.Path.GetFullPath(System.IO.Path.Combine(preparationDll, "../../../../Program.cs"));
        PackageCapture(context, preparationSource, producerSourceHash);
        PackageVerifyPair(context, preparationDll, "IQToolkit.IndependentConsumers.Verifier.Program", "VerifyLockDelta", new Input(preparationSource, producerSourceHash));
    }

    private static object PackageVerifyReviewedLocks(PackageContext context, string api, string version)
    {
        int reviewed = 0;
        foreach (var name in PackageProfiles.Append("SQLite-runtime"))
        {
            using var before = PackageJson(context, System.IO.Path.Combine(context.Outer, "reviewed-locks", name + ".json"));
            using var after = PackageJson(context, System.IO.Path.Combine(context.Outer, "prepared-locks", name + ".json"));
            var left = before.RootElement; var right = after.RootElement;
            Require(left.EnumerateObject().Select(item => item.Name).ToHashSet(StringComparer.Ordinal).SetEquals(right.EnumerateObject().Select(item => item.Name)), "Reviewed lock root fields changed.");
            foreach (var field in left.EnumerateObject().Where(item => item.Name != "dependencies")) Require(JsonElement.DeepEquals(field.Value, right.GetProperty(field.Name)), "Reviewed lock metadata changed.");
            var oldFrameworks = left.GetProperty("dependencies"); var newFrameworks = right.GetProperty("dependencies");
            Require(oldFrameworks.EnumerateObject().Select(item => item.Name).ToHashSet(StringComparer.Ordinal).SetEquals(newFrameworks.EnumerateObject().Select(item => item.Name)), "Reviewed lock framework targets changed.");
            var changes = new Dictionary<string, (string Before, string After)>(StringComparer.Ordinal);
            foreach (var framework in oldFrameworks.EnumerateObject())
            {
                var nodes = newFrameworks.GetProperty(framework.Name);
                Require(framework.Value.EnumerateObject().Select(item => item.Name).ToHashSet(StringComparer.Ordinal).SetEquals(nodes.EnumerateObject().Select(item => item.Name)), "Reviewed lock package nodes changed.");
                foreach (var node in framework.Value.EnumerateObject())
                {
                    var current = nodes.GetProperty(node.Name);
                    Require(node.Value.EnumerateObject().Select(item => item.Name).ToHashSet(StringComparer.Ordinal).SetEquals(current.EnumerateObject().Select(item => item.Name)), "Reviewed lock package fields changed.");
                    foreach (var field in node.Value.EnumerateObject())
                    {
                        var now = current.GetProperty(field.Name);
                        if (field.Name == "contentHash" && PackageProfiles.Contains(node.Name, StringComparer.Ordinal) && !JsonElement.DeepEquals(field.Value, now))
                        {
                            Require(node.Value.GetProperty("resolved").GetString() == version && now.GetString() == PackageSha512(Capture(System.IO.Path.Combine(api, "packages", node.Name + "." + version + ".nupkg"))) &&
                                changes.TryAdd(node.Name, (field.Value.GetString()!, now.GetString()!)), "Prepared toolkit hash does not bind the authenticated unsigned feed.");
                        }
                        else Require(JsonElement.DeepEquals(field.Value, now), "Reviewed lock changed beyond feed-bound toolkit contentHash leaves.");
                    }
                }
            }
            var log = File.ReadAllText(PackageCapture(context, System.IO.Path.Combine(context.Outer, name + ".json.delta.log")));
            var records = log.Split('\n').Where(line => line.StartsWith("{", StringComparison.Ordinal)).Select(line =>
            {
                using var record = JsonDocument.Parse(line); DuplicateCheck(record.RootElement); return record.RootElement.Clone();
            }).ToArray();
            Require(records.Length == changes.Count && records.All(record => changes.TryGetValue(record.GetProperty("Package").GetString()!, out var change) &&
                record.GetProperty("PreviousContentHash").GetString() == change.Before && record.GetProperty("PreparedContentHash").GetString() == change.After) &&
                records.Select(record => record.GetProperty("Package").GetString()).Distinct(StringComparer.Ordinal).Count() == records.Length &&
                log.Contains("Verified complete lock equality except feed-bound toolkit contentHash leaves.", StringComparison.Ordinal), "Native seven-lock review does not match actual retained deltas.");
            if (name != "SQLite-runtime")
                Require(File.ReadAllBytes(PackageCapture(context, System.IO.Path.Combine(context.Outer, "prepared-locks", name + ".json"))).AsSpan().SequenceEqual(
                    File.ReadAllBytes(PackageCapture(context, System.IO.Path.Combine(context.Environment, name, "packages.lock.json")))), "Compiled independent lock differs from the reviewed preparation.");
            reviewed++;
        }
        Require(reviewed == 7, "Incomplete native lock-review profile set.");
        return new { Profiles = reviewed, GraphsUnchanged = true, SourceRetrievalVerified = false };
    }
}
