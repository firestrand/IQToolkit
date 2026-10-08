#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IQToolkit.FinalVerification;

internal static partial class Program
{
    private const string NativeConsumerSharedSha = "61AB21EE85BBAE480D690CA1D607506E4C46C4497DFDBF5778D4B6C28BF3D2B6";
    private const string NativeConsumerPackage = "IQToolkit.Data.SQLite";
    private const string NativeConsumerVersion = "1.1.2-preview.1";

    // Tentative receipt: only the coordinator publishes after its terminal fence.
    private static object ValidateNativePackageConsumer(string consumerDirectory, string apiDirectory)
    {
        var outer = System.IO.Path.GetFullPath(consumerDirectory);
        var api = System.IO.Path.GetFullPath(apiDirectory);
        var sharedPath = System.IO.Path.Combine(repository, "eng/FinalVerification/PackageIndependentEvidence.cs");
        var shared = new Input(sharedPath, Hash(Capture(sharedPath, NativeConsumerSharedSha)));
        var independent = ValidateIndependentPackageProducer(outer, api);
        var area = System.IO.Path.Combine(outer, "environment/artifacts/package-consumer/runs");
        PackageNoLinks(area);
        var candidates = Directory.EnumerateDirectories(area).Take(65).ToArray();
        Require(candidates.Length is > 0 and <= 64, "Native consumer discovery exceeds its finite bound.");
        var passed = candidates.Where(path =>
        {
            PackageNoLinks(path); var status = System.IO.Path.Combine(path, "status.txt"); PackageNoLinks(status);
            if (!File.Exists(status)) return false;
            Require(new FileInfo(status).Length is > 0 and <= 1024, "Native consumer status size differs.");
            return File.ReadAllText(status).Trim() == "passed";
        }).ToArray();
        Require(passed.Length == 1, "Native producer lacks exactly one completed package consumer.");
        var inner = passed[0];
        var originalOuter = File.ReadAllText(Capture(System.IO.Path.Combine(outer, "temporary-directory.txt"))).Trim();
        var originalInner = File.ReadAllText(Capture(System.IO.Path.Combine(inner, "temporary-directory.txt"))).Trim();
        Require(System.IO.Path.IsPathFullyQualified(originalInner) && System.IO.Path.GetFullPath(originalInner) == originalInner &&
            !originalInner.StartsWith(repository + "/", StringComparison.Ordinal) && !originalInner.StartsWith(originalOuter + "/", StringComparison.Ordinal),
            "Native consumer original workspace is not a canonical isolated root.");
        var context = new PackageContext { Outer = outer, Inner = inner, Environment = System.IO.Path.Combine(inner, "environment"), OriginalOuter = originalOuter, OriginalInner = originalInner };
        context.Projects.Add("SQLiteConsumer.csproj", "SQLiteConsumer.csproj.snapshot");
        NativeConsumerSelect(context);
        NativeConsumerAuthenticateInputs(context);
        var graph = NativeConsumerGraph(context, context.Environment, NativeConsumerPackage, NativeConsumerVersion, api).ToArray();
        Require(graph.Length == 12 && context.BuildControlFiles == 17 && context.GeneratedControlFiles == 2 && context.Markers.Count == 6,
            "Native consumer graph/control/physical marker scope differs.");
        var methods = NativeConsumerCompiledMethods(context);
        var runtime = NativeConsumerRuntime(context);
        foreach (var path in context.Closure.Order(StringComparer.Ordinal).ToArray()) NativeConsumerCapture(context, path);
        Require(context.Closure.Count == 121, "Native package consumer complete closure differs from its finite contract.");
        var ownPath = System.IO.Path.Combine(repository, "eng/FinalVerification/PackageNativeConsumerEvidence.cs");
        var source = new Input(ownPath, Hash(Capture(ownPath)));
        var compiled = PackageVerifyPair(null, Assembly.GetExecutingAssembly().Location, "IQToolkit.FinalVerification.Program", "ValidateNativePackageConsumer", source);
        return new
        {
            Status = "verified", Contract = "PACKAGE-NATIVE-CONSUMER-BINDING-1", Producer = outer, NativeProducer = inner, ApiProducer = api,
            VerifierSource = source, SharedVerifierSource = shared, CompiledVerifier = compiled, IndependentProducer = independent,
            RequiredClosureFiles = context.Closure.Count, RequiredClosureBytes = context.Closure.Sum(path => new FileInfo(path).Length),
            OriginalInputRecords = context.OriginalRecords, RelocatedOriginalSources = 5, SharedOriginalOrigins = 7,
            ProjectMappings = context.Projects.Count, Graph = graph, DependencyEdges = 14, InstalledAnalyzerBuildControlFiles = context.BuildControlFiles,
            GeneratedNuGetControlFiles = context.GeneratedControlFiles, PhysicalEmptyMarkers = context.Markers.Count, CompiledMethods = methods, Runtime = runtime,
            ActualMappedNativePathVerified = false, LINQRuntimeVerified = false, OtherPlatformsVerified = false, RemoteSourceRetrievalVerified = false,
            UniversalSignatureSafetyVerified = false, FullReadinessVerified = false, FullGoalComplete = false
        };
    }

    private static string NativeConsumerMap(PackageContext context, string original)
    {
        Require(original.StartsWith(context.OriginalInner + "/", StringComparison.Ordinal) || original.StartsWith(context.OriginalOuter + "/", StringComparison.Ordinal),
            "Native consumer path is outside its two authenticated original roots.");
        return PackageMap(context, original);
    }

    private static bool NativeConsumerSamePath(PackageContext context, string? original, string actual) =>
        original is not null && NativeConsumerMap(context, original.TrimEnd('/')) == actual.TrimEnd('/');

    private static string NativeConsumerCapture(PackageContext context, string path, string? expected = null)
    {
        path = System.IO.Path.GetFullPath(path); PackageNoLinks(path); var info = new FileInfo(path);
        Require(info.Exists && (info.Attributes & FileAttributes.Directory) == 0, "Missing regular native consumer input.");
        context.Closure.Add(path);
        if (info.Length is > 0 and <= Limit) return Capture(path, expected);
        var large = path == System.IO.Path.Combine(context.Environment, "packages/sqlitepclraw.lib.e_sqlite3/2.1.12/sqlitepclraw.lib.e_sqlite3.2.1.12.nupkg") &&
            info.Length == 18762916 && info.Length <= 24L * 1024 * 1024;
        var marker = info.Length == 0 && System.IO.Path.GetFileName(path) == "_._" && context.Markers.Contains(path);
        Require(large || marker, "Unapproved empty or oversized native consumer input.");
        var required = large ? PackageLargeArchiveSha : PackageEmptySha;
        Require(expected is null || expected.Equals(required, StringComparison.OrdinalIgnoreCase), "Special native consumer expected identity differs.");
        if (Inputs.TryGetValue(path, out var known))
        {
            Require(known == required && Hash(path) == required, "Previously captured special native input changed."); return Snapshots[path];
        }
        Require(Inputs.Count < MaximumInputs && bytes + info.Length <= MaximumRetainedBytes, "Native consumer capture exceeds coordinator limits.");
        Require(Hash(path) == required, "Special native consumer input differs from its pinned identity.");
        var snapshot = System.IO.Path.Combine(retained, $"{Inputs.Count:D4}-{info.Name}.snapshot"); File.Copy(path, snapshot, overwrite: false);
        Require(new FileInfo(snapshot).Length == info.Length && Hash(snapshot) == required && Hash(path) == required && new FileInfo(path).Length == info.Length,
            "Special native consumer input changed while captured.");
        PackageNoLinks(path); PackageNoLinks(snapshot); Inputs.Add(path, required); Snapshots.Add(path, snapshot); bytes += info.Length; return snapshot;
    }

    private static JsonDocument NativeConsumerJson(PackageContext context, string path)
    {
        var document = JsonDocument.Parse(File.ReadAllBytes(NativeConsumerCapture(context, path))); DuplicateCheck(document.RootElement); return document;
    }

    private static void NativeConsumerSelect(PackageContext context)
    {
        PackageNoLinks(context.Inner); PackageNoLinks(context.Environment);
        foreach (var name in new[] { "feed", "obj", "bin/Release/net10.0" }) PackageNoLinks(System.IO.Path.Combine(context.Environment, name));
        foreach (var path in Directory.EnumerateFiles(context.Inner)) { PackageNoLinks(path); context.Closure.Add(path); }
        foreach (var name in new[] { "SQLiteConsumer.csproj.snapshot", "Program.cs", "NuGet.config", "packages.lock.json", "global.json", "verify-consumer.sh", "obj/project.assets.json", "obj/SQLiteConsumer.csproj.nuget.dgspec.json" })
            context.Closure.Add(System.IO.Path.Combine(context.Environment, name));
        foreach (var path in Directory.EnumerateFiles(System.IO.Path.Combine(context.Environment, "feed"))) { PackageNoLinks(path); context.Closure.Add(path); }
        foreach (var path in Directory.EnumerateFiles(System.IO.Path.Combine(context.Environment, "obj"), "*.cs", SearchOption.AllDirectories)) { PackageNoLinks(path); context.Closure.Add(path); }
        foreach (var path in Directory.EnumerateFiles(System.IO.Path.Combine(context.Environment, "obj"), "*.nuget.g.*")) { PackageNoLinks(path); context.Closure.Add(path); context.GeneratedControlFiles++; }
        foreach (var path in Directory.EnumerateFiles(System.IO.Path.Combine(context.Environment, "bin/Release/net10.0"))) { PackageNoLinks(path); context.Closure.Add(path); }
        Require(context.Closure.Count <= 128, "Native consumer initial file selection exceeds its finite bound.");
        Require(!File.Exists(System.IO.Path.Combine(context.Environment, "SQLiteConsumer.csproj")), "Retained native project is still discoverable.");
    }

    private static void NativeConsumerAuthenticateInputs(PackageContext context)
    {
        var before = File.ReadAllBytes(NativeConsumerCapture(context, System.IO.Path.Combine(context.Inner, "inputs-before.txt")));
        Require(before.AsSpan().SequenceEqual(File.ReadAllBytes(NativeConsumerCapture(context, System.IO.Path.Combine(context.Inner, "inputs-after.txt")))), "Native consumer original inputs changed.");
        var lines = Encoding.UTF8.GetString(before).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.TrimEnd('\r')).ToArray();
        Require(lines.Length == 24, "Native consumer original input record count differs.");
        var sources = new HashSet<string>(StringComparer.Ordinal);
        var expectedSources = new HashSet<string>(StringComparer.Ordinal)
        {
            "eng/PackageConsumer/SQLiteConsumer.csproj.template", "eng/PackageConsumer/Program.cs", "eng/PackageConsumer/NuGet.config", "eng/PackageConsumer/packages.lock.json", "global.json", "eng/verify-consumer.sh"
        };
        foreach (var id in PackageProfiles) expectedSources.Add("feed/" + id + "." + NativeConsumerVersion + ".nupkg");
        for (var index = 0; index < lines.Length; index += 2)
        {
            Require(Regex.IsMatch(lines[index], "^[a-f0-9]{40}$") && lines[index + 1].StartsWith(context.OriginalOuter + "/", StringComparison.Ordinal), "Unsupported native original Git-blob record.");
            var relative = lines[index + 1][(context.OriginalOuter.Length + 1)..]; Require(sources.Add(relative) && expectedSources.Contains(relative), "Unexpected or duplicate native original input.");
            var actual = NativeConsumerMap(context, lines[index + 1]);
            var sharedOrigin = relative == "global.json" || relative.StartsWith("feed/", StringComparison.Ordinal);
            Require(!sharedOrigin || Inputs.ContainsKey(actual), "Native shared original was not authenticated by the independent producer.");
            var copy = sharedOrigin ? Capture(actual) : NativeConsumerCapture(context, actual);
            var value = File.ReadAllBytes(copy);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
            hash.AppendData(Encoding.UTF8.GetBytes("blob " + value.Length.ToString(CultureInfo.InvariantCulture) + "\0")); hash.AppendData(value);
            Require(Convert.ToHexString(hash.GetHashAndReset()).Equals(lines[index], StringComparison.OrdinalIgnoreCase), "Native original Git-blob bytes differ.");
            var copiedRelative = relative.StartsWith("feed/", StringComparison.Ordinal) ? relative : relative == "eng/PackageConsumer/SQLiteConsumer.csproj.template" ? "SQLiteConsumer.csproj.snapshot" : System.IO.Path.GetFileName(relative);
            Require(value.AsSpan().SequenceEqual(File.ReadAllBytes(NativeConsumerCapture(context, PackageSafePath(context.Environment, copiedRelative)))), "Native compiled source/feed differs from original pre-copy input.");
            context.OriginalRecords++;
        }
        Require(sources.SetEquals(expectedSources), "Native original input set differs.");
        var originalRetained = context.OriginalOuter + context.Inner[(System.IO.Path.Combine(context.Outer, "environment")).Length..] + "/environment";
        Require(File.ReadAllText(NativeConsumerCapture(context, System.IO.Path.Combine(context.Inner, "retained-environment.txt"))).Trim() == originalRetained,
            "Native retained-environment origin differs.");
        Require(File.ReadAllText(NativeConsumerCapture(context, System.IO.Path.Combine(context.Inner, "project-filenames.txt"))).Trim() ==
            "SQLiteConsumer.csproj.template -> outside SQLiteConsumer.csproj -> retained SQLiteConsumer.csproj.snapshot (identical bytes)", "Native project disposition differs.");
        foreach (var name in new[] { "SQLiteConsumer.csproj.snapshot", "Program.cs", "NuGet.config", "packages.lock.json", "global.json", "verify-consumer.sh" })
            Require(File.ReadAllBytes(NativeConsumerCapture(context, System.IO.Path.Combine(context.Inner, name))).AsSpan().SequenceEqual(
                File.ReadAllBytes(NativeConsumerCapture(context, System.IO.Path.Combine(context.Environment, name)))), "Native evidence source/config/project differs from the actual compiled input.");
        Require(File.ReadAllBytes(NativeConsumerCapture(context, System.IO.Path.Combine(context.Environment, "packages.lock.json"))).AsSpan().SequenceEqual(
            File.ReadAllBytes(Capture(System.IO.Path.Combine(context.Outer, "prepared-locks/SQLite-runtime.json")))), "Native consumer lock differs from the authenticated seven-lock review.");
        Require(File.ReadAllText(NativeConsumerCapture(context, System.IO.Path.Combine(context.Inner, "copied-input-inspection.txt"))).Trim() ==
            "All copied consumer/feed identities match the pre-copy snapshot.", "Native copy inspection did not complete.");
    }

    private static IEnumerable<object> NativeConsumerGraph(PackageContext context, string directory, string package, string version, string api)
    {
        var project = PackageXml(NativeConsumerCapture(context, System.IO.Path.Combine(directory, "SQLiteConsumer.csproj.snapshot")));
        Require(!project.Descendants("ProjectReference").Any() && !project.Descendants("Reference").Any(), "Native consumer uses project or copied-assembly references.");
        var directReferences = project.Descendants("PackageReference").ToArray();
        Require(directReferences.Length == 1 && (string?)directReferences[0].Attribute("Include") == package && (string?)directReferences[0].Attribute("Version") == "[" + version + "]" &&
            project.Descendants("TargetFramework").Single().Value == "net10.0", "Native consumer sole exact SQLite dependency differs.");
        var config = PackageXml(NativeConsumerCapture(context, System.IO.Path.Combine(directory, "NuGet.config")));
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
            var ownFeed = NativeConsumerCapture(context, System.IO.Path.Combine(feed, name));
            var tested = Capture(System.IO.Path.Combine(context.Outer, "tested-packages", name));
            Require(File.ReadAllBytes(ownFeed).AsSpan().SequenceEqual(File.ReadAllBytes(tested)) && File.ReadAllBytes(tested).AsSpan().SequenceEqual(File.ReadAllBytes(Capture(System.IO.Path.Combine(api, "packages", name)))),
                "Independent feed/archive bytes differ from the authenticated API feed.");
            PackageArchiveIdentity(ownFeed, id, version);
        }
        using var assetsDocument = NativeConsumerJson(context, System.IO.Path.Combine(directory, "obj/project.assets.json"));
        using var lockDocument = NativeConsumerJson(context, System.IO.Path.Combine(directory, "packages.lock.json"));
        var assets = assetsDocument.RootElement; var locked = PackageFramework(lockDocument.RootElement.GetProperty("dependencies"));
        var projectMetadata = assets.GetProperty("project"); var restore = projectMetadata.GetProperty("restore");
        var cache = System.IO.Path.Combine(directory, "packages");
        Require(restore.GetProperty("restoreLockProperties").GetProperty("restoreLockedMode").GetBoolean() &&
            restore.GetProperty("warningProperties").GetProperty("allWarningsAsErrors").GetBoolean() &&
            restore.GetProperty("restoreAuditProperties").GetProperty("enableAudit").GetString() == "true" &&
            restore.GetProperty("restoreAuditProperties").GetProperty("auditMode").GetString() == "all" &&
            restore.GetProperty("restoreAuditProperties").GetProperty("auditLevel").GetString() == "high", "Native restore profile is not locked/strict/audited.");
        using var specDocument = NativeConsumerJson(context, System.IO.Path.Combine(directory, "obj/SQLiteConsumer.csproj.nuget.dgspec.json"));
        var spec = specDocument.RootElement;
        var specProjects = spec.GetProperty("projects").EnumerateObject().ToArray(); var specRestore = spec.GetProperty("restore").EnumerateObject().ToArray();
        Require(specProjects.Length == 1 && specRestore.Length == 1 && specProjects[0].Name == specRestore[0].Name &&
            NativeConsumerSamePath(context, specProjects[0].Name, System.IO.Path.Combine(directory, "SQLiteConsumer.csproj.snapshot")) &&
            JsonElement.DeepEquals(specProjects[0].Value, projectMetadata), "Native restore specification differs from actual assets/profile.");
        Require(NativeConsumerSamePath(context, restore.GetProperty("projectPath").GetString(), System.IO.Path.Combine(directory, "SQLiteConsumer.csproj.snapshot")) &&
            NativeConsumerSamePath(context, restore.GetProperty("projectUniqueName").GetString(), System.IO.Path.Combine(directory, "SQLiteConsumer.csproj.snapshot")) &&
            restore.GetProperty("projectName").GetString() == "SQLiteConsumer", "Assets identify another compiled project.");
        Require(NativeConsumerSamePath(context, restore.GetProperty("packagesPath").GetString(), cache), "Consumer restored into another package cache.");
        var folders = assets.GetProperty("packageFolders").EnumerateObject().ToArray();
        Require(folders.Length == 1 && NativeConsumerSamePath(context, folders[0].Name, cache), "Consumer used a fallback package cache.");
        var sources = restore.GetProperty("sources").EnumerateObject().Select(item => item.Name).ToArray();
        Require(sources.Length == 2 && sources.Contains(PackagePublicFeed, StringComparer.Ordinal) && sources.Any(source => source != PackagePublicFeed && NativeConsumerSamePath(context, source, feed)), "Assets restore sources differ.");
        var configs = restore.GetProperty("configFilePaths").EnumerateArray().ToArray();
        Require(configs.Length == 1 && NativeConsumerSamePath(context, configs[0].GetString(), System.IO.Path.Combine(directory, "NuGet.config")), "Restore inherited another NuGet configuration.");
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
        var toolkit = new HashSet<string>(StringComparer.Ordinal); var allIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var observations = new List<object>(); var edgeCount = 0; var nativeTargets = 0;
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
            edgeCount += targetEdges.Length;
            var lockEdges = entry.TryGetProperty("dependencies", out var lockDependencies) ? lockDependencies.EnumerateObject().ToArray() : [];
            Require(targetEdges.Length == lockEdges.Length && targetEdges.All(edge => lockEdges.Any(other => other.Name == edge.Name && JsonElement.DeepEquals(other.Value, edge.Value))) &&
                targetEdges.All(edge => locked.TryGetProperty(edge.Name, out _)), "Lock and asset dependency edges differ or refer to absent nodes.");
            var relative = library.Value.GetProperty("path").GetString()!;
            Require(relative == id.ToLowerInvariant() + "/" + resolved.ToLowerInvariant(), "Package cache path differs from resolved identity.");
            var installed = PackageSafePath(cache, relative); var archives = Directory.EnumerateFiles(installed, "*.nupkg").ToArray();
            Require(archives.Length == 1, "Installed package whole archive is missing or ambiguous.");
            var archivePath = NativeConsumerCapture(context, archives[0]); var archiveHash = PackageSha512(archivePath);
            using var sourceMetadata = NativeConsumerJson(context, System.IO.Path.Combine(installed, ".nupkg.metadata"));
            var source = sourceMetadata.RootElement.GetProperty("source").GetString();
            Require(toolkitPackage ? NativeConsumerSamePath(context, source, feed) : source == PackagePublicFeed, "Package was restored from an unexpected source.");
            var contentHash = sourceMetadata.RootElement.GetProperty("contentHash").GetString();
            Require(contentHash is not null && contentHash == library.Value.GetProperty("sha512").GetString() && contentHash == entry.GetProperty("contentHash").GetString(), "Logical content hashes differ across cache metadata/assets/lock.");
            Require(archiveHash == File.ReadAllText(NativeConsumerCapture(context, archives[0] + ".sha512")).Trim(), "Whole archive SHA512 sidecar differs.");
            using var archive = ZipFile.OpenRead(archivePath);
            Require(archiveHash == contentHash || !toolkitPackage && archive.GetEntry(".signature.p7s") is not null, "Unsigned archive/logical content hash differs.");
            PackageArchiveIdentity(archivePath, id, resolved);
            if (toolkitPackage)
            {
                var feedPath = NativeConsumerCapture(context, System.IO.Path.Combine(feed, id + "." + version + ".nupkg"));
                Require(archiveHash == contentHash && archiveHash == PackageSha512(feedPath), "Unsigned toolkit archive differs from tested feed/logical hash.");
                var dll = "lib/net10.0/" + id + ".dll";
                Require(PackageEntryBytes(archive, dll).AsSpan().SequenceEqual(File.ReadAllBytes(NativeConsumerCapture(context, PackageSafePath(installed, dll)))), "Installed toolkit DLL differs from whole archive.");
            }
            var controlPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in new[] { "compile", "runtime", "build", "buildMultiTargeting", "buildTransitive", "analyzers" })
                if (target.TryGetProperty(group, out var selected)) foreach (var asset in selected.EnumerateObject())
                {
                    if (System.IO.Path.GetFileName(asset.Name) == "_._") continue;
                    var actual = PackageSafePath(installed, asset.Name);
                    Require(PackageEntryBytes(archive, asset.Name).AsSpan().SequenceEqual(File.ReadAllBytes(NativeConsumerCapture(context, actual))), "Selected compile/build asset differs from whole archive.");
                    if (group is not ("compile" or "runtime")) controlPaths.Add(actual);
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
                Require(PackageEntryBytes(archive, name).AsSpan().SequenceEqual(File.ReadAllBytes(NativeConsumerCapture(context, actual))), "Installed analyzer/build control differs from whole archive.");
            }
            if (target.TryGetProperty("runtimeTargets", out var runtimeTargets))
                foreach (var native in runtimeTargets.EnumerateObject())
                    if (native.Value.GetProperty("rid").GetString() == "linux-arm64" && native.Value.GetProperty("assetType").GetString() == "native")
                    {
                        Require(id == "SQLitePCLRaw.lib.e_sqlite3" && native.Name == "runtimes/linux-arm64/native/libe_sqlite3.so", "Unexpected native host asset.");
                        var installedNative = NativeConsumerCapture(context, PackageSafePath(installed, native.Name));
                        var outputNative = NativeConsumerCapture(context, PackageSafePath(System.IO.Path.Combine(directory, "bin/Release/net10.0"), native.Name));
                        Require(PackageEntryBytes(archive, native.Name).AsSpan().SequenceEqual(File.ReadAllBytes(installedNative)) &&
                            File.ReadAllBytes(installedNative).AsSpan().SequenceEqual(File.ReadAllBytes(outputNative)), "Native cache/archive/output bytes differ.");
                        nativeTargets++;
                    }
            context.BuildControlFiles += controlPaths.Count;
            observations.Add(new { Profile = package, Package = library.Name, Source = source, ArchiveSha512 = archiveHash, ContentHash = contentHash, DependencyEdges = targetEdges.Length });
        }
        var expectedToolkit = package == "IQToolkit" ? new[] { "IQToolkit" } : package == "IQToolkit.Data" ? new[] { "IQToolkit", "IQToolkit.Data" } : new[] { "IQToolkit", "IQToolkit.Data", package };
        Require(toolkit.SetEquals(expectedToolkit) && (!PackageDrivers.TryGetValue(package, out var driver) || allIds.Contains(driver)), "Independent graph lacks required toolkit/driver nodes.");
        Require(libraries.Length == 12 && edgeCount == 14 && nativeTargets == 1, "Native graph node/edge/host-asset set differs.");
        return observations;
    }


    private static object[] NativeConsumerCompiledMethods(PackageContext context)
    {
        var sourcePath = System.IO.Path.Combine(context.Environment, "Program.cs");
        var source = new Input(sourcePath, Hash(NativeConsumerCapture(context, sourcePath)));
        var result = new List<object>();
        foreach (var method in new[] { "Main", "VerifyPackageProvenance", "VerifyMetadata", "ReadNative", "RecordAssembly" })
        {
            var proof = PackageVerifyPair(context, System.IO.Path.Combine(context.Environment, "bin/Release/net10.0/SQLiteConsumer.dll"), "IQToolkit.PackageConsumer.Program", method, source);
            var element = JsonSerializer.SerializeToElement(proof);
            Require(element.GetProperty("Assembly").GetString() == "SQLiteConsumer", "Native output assembly identity differs.");
            var references = element.GetProperty("References").EnumerateArray().Select(value => value.GetString()!).ToHashSet(StringComparer.Ordinal);
            Require(references.IsSupersetOf(new[] { "IQToolkit", "IQToolkit.Data", "IQToolkit.Data.SQLite", "Microsoft.Data.Sqlite" }), "Native consumer compiled references omit the actual package/driver APIs.");
            var documents = element.GetProperty("Documents").EnumerateObject().ToArray();
            Require(documents.Length == 3, "Native consumer PDB source document set differs.");
            var expected = new HashSet<string>(StringComparer.Ordinal)
            {
                "Program.cs", "obj/Release/net10.0/.NETCoreApp,Version=v10.0.AssemblyAttributes.cs", "obj/Release/net10.0/SQLiteConsumer.AssemblyInfo.cs"
            };
            var observed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var document in documents)
            {
                var physical = NativeConsumerMap(context, document.Name);
                Require(physical.StartsWith(context.Environment + "/", StringComparison.Ordinal), "Native PDB source is outside the consumer workspace.");
                Require(observed.Add(physical[(context.Environment.Length + 1)..]), "Duplicate native PDB source document.");
                NativeConsumerCapture(context, physical, document.Value.GetString());
            }
            Require(observed.SetEquals(expected), "Native compiled sources differ from the actual three reviewed documents.");
            result.Add(proof);
        }
        return result.ToArray();
    }

    private static string[] NativeConsumerStrings(JsonElement value)
    {
        Require(value.ValueKind == JsonValueKind.Array, "Native runtime response is not an actual string array.");
        return value.EnumerateArray().Select(item =>
        {
            Require(item.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(item.GetString()), "Native runtime response contains missing/nonstring values.");
            return item.GetString()!;
        }).ToArray();
    }

    private static string NativeConsumerAssemblyName(string copy)
    {
        using var pe = new PEReader(File.OpenRead(copy)); var metadata = pe.GetMetadataReader(); var assembly = metadata.GetAssemblyDefinition();
        var name = metadata.GetString(assembly.Name); var culture = metadata.GetString(assembly.Culture); var key = metadata.GetBlobBytes(assembly.PublicKey);
        var token = key.Length == 0 ? "null" : Convert.ToHexString(SHA1.HashData(key)[^8..].Reverse().ToArray()).ToLowerInvariant();
        return name + ", Version=" + assembly.Version + ", Culture=" + (culture.Length == 0 ? "neutral" : culture) + ", PublicKeyToken=" + token;
    }

    private static object NativeConsumerRuntime(PackageContext context)
    {
        var originalEvidence = context.OriginalOuter + context.Inner[(System.IO.Path.Combine(context.Outer, "environment")).Length..];
        var aggregate = File.ReadAllText(Capture(System.IO.Path.Combine(context.Outer, "verify-sqlite.log")));
        Require(aggregate.Split('\n').Count(line => line.TrimEnd('\r') == "Consumer evidence: " + originalEvidence) == 1 &&
            aggregate.Contains("SQLite package-only native executor verification passed.", StringComparison.Ordinal), "Native child producer path/completion is missing from the authenticated CI log.");
        Require(File.ReadAllText(NativeConsumerCapture(context, System.IO.Path.Combine(context.Inner, "package-directory.txt"))).Trim() == context.OriginalOuter + "/feed",
            "Native consumer restored from another original feed.");
        var build = File.ReadAllText(NativeConsumerCapture(context, System.IO.Path.Combine(context.Inner, "build.log")));
        Require(build.Contains("Build succeeded.", StringComparison.Ordinal) && build.Contains("0 Warning(s)", StringComparison.Ordinal) && build.Contains("0 Error(s)", StringComparison.Ordinal), "Native strict build did not complete without warnings/errors.");
        var restoreLog = File.ReadAllText(NativeConsumerCapture(context, System.IO.Path.Combine(context.Inner, "restore.log")));
        Require(restoreLog.Contains("Restored ", StringComparison.Ordinal) && !Regex.IsMatch(restoreLog, "\\b(?:warning|error) (?:NU|MSB|CS)[0-9]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)),
            "Native locked restore failed or warned.");
        Require(File.ReadAllBytes(NativeConsumerCapture(context, System.IO.Path.Combine(context.Inner, "project.assets.json"))).AsSpan().SequenceEqual(
            File.ReadAllBytes(NativeConsumerCapture(context, System.IO.Path.Combine(context.Environment, "obj/project.assets.json")))), "Native evidence assets differ from compiled/restored assets.");
        using var assetsDocument = NativeConsumerJson(context, System.IO.Path.Combine(context.Environment, "obj/project.assets.json"));
        var assets = assetsDocument.RootElement; var targets = PackageFramework(assets.GetProperty("targets")); var libraries = assets.GetProperty("libraries");
        var output = System.IO.Path.Combine(context.Environment, "bin/Release/net10.0");
        using var runtimeConfigDocument = NativeConsumerJson(context, System.IO.Path.Combine(output, "SQLiteConsumer.runtimeconfig.json"));
        var runtimeOptions = runtimeConfigDocument.RootElement.GetProperty("runtimeOptions");
        Require(runtimeOptions.GetProperty("tfm").GetString() == "net10.0" && runtimeOptions.GetProperty("framework").GetProperty("name").GetString() == "Microsoft.NETCore.App" &&
            runtimeOptions.GetProperty("framework").GetProperty("version").GetString() == "10.0.0" && !runtimeOptions.TryGetProperty("frameworks", out _), "Native framework-dependent runtime configuration differs.");
        var managed = new Dictionary<string, (string Output, string Identity)>(StringComparer.Ordinal);
        using var depsDocument = NativeConsumerJson(context, System.IO.Path.Combine(output, "SQLiteConsumer.deps.json"));
        var deps = depsDocument.RootElement;
        Require(deps.GetProperty("runtimeTarget").GetProperty("name").GetString() == ".NETCoreApp,Version=v10.0" && deps.GetProperty("runtimeTarget").GetProperty("signature").GetString() == "", "Native runtime profile differs.");
        var runtimeTargets = deps.GetProperty("targets").EnumerateObject().ToArray(); Require(runtimeTargets.Length == 1 && runtimeTargets[0].Name == ".NETCoreApp,Version=v10.0", "Native runtime target count differs.");
        var depsTargets = runtimeTargets[0].Value;
        Require(depsTargets.EnumerateObject().Count() == 13 && deps.GetProperty("libraries").EnumerateObject().Count() == 13, "Native deps graph is incomplete.");
        foreach (var library in libraries.EnumerateObject())
        {
            var target = targets.GetProperty(library.Name); var dependency = depsTargets.GetProperty(library.Name);
            var targetEdges = target.TryGetProperty("dependencies", out var edges) ? edges.EnumerateObject().ToArray() : [];
            var runtimeEdges = dependency.TryGetProperty("dependencies", out var depsEdges) ? depsEdges.EnumerateObject().ToArray() : [];
            Require(targetEdges.Length == runtimeEdges.Length && targetEdges.All(edge => runtimeEdges.Any(other => edge.Name == other.Name && JsonElement.DeepEquals(edge.Value, other.Value))), "Runtime dependency edges differ from assets.");
            var cache = PackageSafePath(System.IO.Path.Combine(context.Environment, "packages"), library.Value.GetProperty("path").GetString()!);
            var selectedRuntime = target.TryGetProperty("runtime", out var runtime) ? runtime.EnumerateObject().Where(item => System.IO.Path.GetFileName(item.Name) != "_._").ToArray() : [];
            var outputRuntime = dependency.TryGetProperty("runtime", out var recorded) ? recorded.EnumerateObject().ToArray() : [];
            Require(selectedRuntime.Select(item => item.Name).ToHashSet(StringComparer.Ordinal).SetEquals(outputRuntime.Select(item => item.Name)), "Native deps managed asset selection differs.");
            foreach (var asset in selectedRuntime)
            {
                var cached = NativeConsumerCapture(context, PackageSafePath(cache, asset.Name));
                var actual = PackageSafePath(output, System.IO.Path.GetFileName(asset.Name)); var copied = NativeConsumerCapture(context, actual);
                Require(File.ReadAllBytes(cached).AsSpan().SequenceEqual(File.ReadAllBytes(copied)), "Copied managed runtime assembly differs from selected cache/archive bytes.");
                using var pe = new PEReader(File.OpenRead(copied)); var metadata = pe.GetMetadataReader(); var name = metadata.GetString(metadata.GetAssemblyDefinition().Name);
                Require(managed.TryAdd(name, (actual, NativeConsumerAssemblyName(copied))), "Duplicate managed runtime assembly name.");
            }
            var depsLibrary = deps.GetProperty("libraries").GetProperty(library.Name);
            Require(depsLibrary.GetProperty("type").GetString() == "package" && depsLibrary.GetProperty("sha512").GetString() == "sha512-" + library.Value.GetProperty("sha512").GetString(), "Native deps package identity/content hash differs.");
            var selectedNative = target.TryGetProperty("runtimeTargets", out var nativeAssets) ? nativeAssets.EnumerateObject().ToArray() : [];
            var recordedNative = dependency.TryGetProperty("runtimeTargets", out var nativeDeps) ? nativeDeps.EnumerateObject().ToArray() : [];
            Require(selectedNative.Length == recordedNative.Length && selectedNative.All(asset => recordedNative.Any(other => other.Name == asset.Name &&
                other.Value.GetProperty("rid").GetString() == asset.Value.GetProperty("rid").GetString() && other.Value.GetProperty("assetType").GetString() == asset.Value.GetProperty("assetType").GetString())), "Native deps RID/asset selection differs.");
        }
        Require(managed.Count == 10, "Native immediate managed runtime output count differs.");
        var project = depsTargets.GetProperty("SQLiteConsumer/1.0.0");
        Require(project.GetProperty("dependencies").EnumerateObject().Count() == 1 && project.GetProperty("dependencies").GetProperty(NativeConsumerPackage).GetString() == NativeConsumerVersion &&
            project.GetProperty("runtime").EnumerateObject().Single().Name == "SQLiteConsumer.dll" && deps.GetProperty("libraries").GetProperty("SQLiteConsumer/1.0.0").GetProperty("type").GetString() == "project", "Native deps project sole-direct graph differs.");
        var logPath = System.IO.Path.Combine(context.Inner, "runtime.log");
        var lines = File.ReadAllLines(NativeConsumerCapture(context, logPath)); Require(lines.Length == 11, "Native runtime observation count differs from its compiled source contract.");
        var packageRecords = new HashSet<string>(StringComparer.Ordinal); var assemblyRecords = new HashSet<string>(StringComparer.Ordinal);
        var responses = new List<object>(); var packageObservations = new List<JsonElement>(); var assemblyObservations = new List<JsonElement>(); string[] nativeSource = []; string[] observedVersion = [];
        for (var index = 0; index < lines.Length; index++)
        {
            if (index == 7)
            {
                const string prefix = "Native SQLite source: "; Require(lines[index].StartsWith(prefix, StringComparison.Ordinal), "Native source response is missing.");
                using var sourceDocument = JsonDocument.Parse(lines[index][prefix.Length..]); DuplicateCheck(sourceDocument.RootElement);
                nativeSource = NativeConsumerStrings(sourceDocument.RootElement);
                Require(nativeSource.Length == 1 && Regex.IsMatch(nativeSource[0], "^[0-9]{4}-[0-9]{2}-[0-9]{2} [0-9]{2}:[0-9]{2}:[0-9]{2} [a-f0-9]{64}$"), "Native SQLite source ID shape differs."); continue;
            }
            if (index == 10) { Require(lines[index] == "SQLite package-only native executor verification passed.", "Native runtime terminal confirmation is absent."); continue; }
            using var document = JsonDocument.Parse(lines[index]); DuplicateCheck(document.RootElement); var record = document.RootElement;
            if (index < 3)
            {
                var identity = record.GetProperty("Package").GetString()!; Require(packageRecords.Add(identity), "Duplicate native toolkit provenance.");
                Require(identity is "IQToolkit/1.1.2-preview.1" or "IQToolkit.Data/1.1.2-preview.1" or "IQToolkit.Data.SQLite/1.1.2-preview.1", "Unexpected native toolkit package record.");
                var library = libraries.GetProperty(identity);
                Require(NativeConsumerSamePath(context, record.GetProperty("Source").GetString(), System.IO.Path.Combine(context.Environment, "feed")) &&
                    record.GetProperty("Sha512").GetString() == library.GetProperty("sha512").GetString(), "Native package stdout does not bind actual local-feed provenance.");
                packageObservations.Add(record.Clone());
            }
            else if (index < 7)
            {
                var identity = record.GetProperty("Assembly").GetString()!; var name = identity.Split(',')[0];
                Require(assemblyRecords.Add(name), "Duplicate native managed assembly observation.");
                Require(managed.TryGetValue(name, out var actual), "Unexpected native managed assembly observation.");
                Require(identity == actual.Identity && NativeConsumerSamePath(context, record.GetProperty("Location").GetString(), actual.Output) &&
                    record.GetProperty("Sha256").GetString() == Hash(NativeConsumerCapture(context, actual.Output)), "Native loaded managed identity/location/hash differs from actual selected package output.");
                assemblyObservations.Add(record.Clone());
            }
            else
            {
                var request = index == 8 ? "SELECT sqlite_version();" : "PRAGMA compile_options;";
                Require(record.GetProperty("Request").GetString() == request, "Native runtime request differs from its reviewed configured operation.");
                var native = NativeConsumerStrings(record.GetProperty("Native")); var toolkit = NativeConsumerStrings(record.GetProperty("Toolkit"));
                Require(native.Length > 0 && native.SequenceEqual(toolkit, StringComparer.Ordinal) && (index != 8 || native.Length == 1), "Public executor did not return the complete unchanged actual native metadata.");
                if (index == 8) observedVersion = native;
                responses.Add(new { Request = request, Native = native, Toolkit = toolkit, NativeRows = native.Length, ToolkitRows = toolkit.Length, ExactOrderedEquality = true });
            }
        }
        Require(packageRecords.Count == 3 && assemblyRecords.SetEquals(new[] { "IQToolkit", "IQToolkit.Data", "IQToolkit.Data.SQLite", "Microsoft.Data.Sqlite" }) && responses.Count == 2,
            "Native actual package/managed/metadata observations are incomplete.");
        var nativeRelative = "runtimes/linux-arm64/native/libe_sqlite3.so";
        var nativeOutput = PackageSafePath(output, nativeRelative);
        var nativeCache = PackageSafePath(System.IO.Path.Combine(context.Environment, "packages/sqlitepclraw.lib.e_sqlite3/2.1.12"), nativeRelative);
        return new { Log = new Input(logPath, Hash(NativeConsumerCapture(context, logPath))), NativeSource = nativeSource, Version = observedVersion.Single(), MetadataResponses = responses,
            ToolkitProvenance = packageObservations, ManagedAssemblyObservations = assemblyObservations,
            NativeAsset = new { Output = new Input(nativeOutput, Hash(NativeConsumerCapture(context, nativeOutput))), Cache = new Input(nativeCache, Hash(NativeConsumerCapture(context, nativeCache))),
                Bytes = new FileInfo(nativeOutput).Length, ActualMappedPathVerified = false },
            ToolkitPackageRecords = packageRecords.Count, ManagedLoadedAssemblyRecords = assemblyRecords.Count, SelectedManagedRuntimeFiles = managed.Count,
            NativeCacheArchiveOutputIdentityVerified = true, ActualMappedNativePathVerified = false, IndividualRuntimeProcessReceiptAvailable = false,
            NativeExitWitnessScope = "authenticated complete outer CI command", UniversalVulnerabilitySafetyVerified = false, BroadProviderRuntimeVerified = false };
    }
}
