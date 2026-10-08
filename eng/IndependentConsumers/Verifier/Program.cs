using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;

namespace IQToolkit.IndependentConsumers.Verifier;

internal static class Program
{
    private const string PublicFeed = "https://api.nuget.org/v3/index.json";

    private static readonly Dictionary<string, string?> Profiles = new(StringComparer.Ordinal)
    {
        ["IQToolkit"] = null,
        ["IQToolkit.Data"] = null,
        ["IQToolkit.Data.SQLite"] = "Microsoft.Data.Sqlite",
        ["IQToolkit.Data.SqlClient"] = "Microsoft.Data.SqlClient",
        ["IQToolkit.Data.MySqlClient"] = "MySql.Data",
        ["IQToolkit.Data.Access"] = "System.Data.OleDb"
    };

    private static int Main(string[] args)
    {
        try
        {
            Require(args.Length == 4, "Usage: Verifier <graph|output> <consumer-directory> <package-id> <version>; or lock-delta <reviewed-lock> <prepared-lock> <feed-directory>; symbols <feed> <central-props> <commit>; symbol-pair <nupkg> <snupkg> <pdb-package-id>");
            if (args[0] == "lock-delta")
            {
                VerifyLockDelta(args[1], args[2], Path.GetFullPath(args[3]));
                return 0;
            }
            if (args[0] == "symbols")
            {
                VerifySymbols(Path.GetFullPath(args[1]), args[2], args[3]);
                return 0;
            }
            if (args[0] == "symbol-pair")
            {
                Require(Profiles.ContainsKey(args[3]), "Unknown symbol package ID.");
                VerifySymbolPair(args[1], args[2], args[3]);
                return 0;
            }
            var directory = Path.GetFullPath(args[1]);
            Require(Profiles.ContainsKey(args[2]), "Unknown independent package profile.");
            if (args[0] == "graph")
            {
                VerifyGraph(directory, args[2], args[3]);
            }
            else
            {
                Require(args[0] == "output", "Unknown verification operation.");
                VerifyOutput(directory, args[2]);
            }
            return 0;
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException
            or JsonException or System.Xml.XmlException or BadImageFormatException or ArgumentException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private const string RepositoryUrl = "https://github.com/firestrand/IQToolkit";
    private static readonly Guid SourceLinkKind = new("CC110556-A091-4D38-9FEC-25AB9A351A6A");
    private static readonly Guid EmbeddedSourceKind = new("0E8A571B-6926-466E-B4AD-8AB04611F5FE");

    private static void VerifySymbols(string feed, string propertiesPath, string commit)
    {
        var version = XDocument.Load(propertiesPath).Descendants("Version").Single().Value;
        Require(commit.Length == 40 && commit.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f'),
            "Expected a full lowercase Git commit ID.");
        Require(Directory.EnumerateFiles(feed, "*.nupkg").Count() == Profiles.Count
            && Directory.EnumerateFiles(feed, "*.snupkg").Count() == Profiles.Count,
            "Symbol qualification requires exactly six package/symbol archive pairs.");
        foreach (var id in Profiles.Keys)
        {
            var package = Path.Combine(feed, $"{id}.{version}.nupkg");
            var symbols = Path.Combine(feed, $"{id}.{version}.snupkg");
            foreach (var path in new[] { package, symbols })
            {
                VerifyArchiveIdentity(path, id, version);
                using var archive = ZipFile.OpenRead(path);
                var metadata = ArchiveMetadata(archive);
                var repository = metadata.Elements().Single(element => element.Name.LocalName == "repository");
                Require((string?)repository.Attribute("type") == "git"
                    && (string?)repository.Attribute("url") == RepositoryUrl
                    && (string?)repository.Attribute("commit") == commit,
                    "Package/symbol repository metadata does not match the captured source revision.");
            }
            VerifySymbolPair(package, symbols, id);
        }
        Console.WriteLine(JsonSerializer.Serialize(new { Status = "verified", PackagePairs = Profiles.Count, SourceRetrievalVerified = false }));
    }

    private static XElement ArchiveMetadata(ZipArchive archive)
    {
        var manifest = archive.Entries.Single(entry => entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        using var stream = manifest.Open();
        return XDocument.Load(stream).Root?.Elements().Single(element => element.Name.LocalName == "metadata")
            ?? throw new InvalidOperationException("Missing package metadata.");
    }

    private static byte[] ArchiveBytes(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path) ?? throw new InvalidOperationException("Required library/symbol entry is missing.");
        using var source = entry.Open();
        using var output = new MemoryStream();
        source.CopyTo(output);
        return output.ToArray();
    }

    private static void VerifySymbolPair(string packagePath, string symbolPath, string symbolPackage)
    {
        using var package = ZipFile.OpenRead(packagePath);
        using var symbols = ZipFile.OpenRead(symbolPath);
        var metadata = ArchiveMetadata(package);
        var id = metadata.Elements().Single(element => element.Name.LocalName == "id").Value;
        Require(Profiles.ContainsKey(id), "Unknown library package ID.");
        var dllBytes = ArchiveBytes(package, $"lib/net10.0/{id}.dll");
        var pdbBytes = ArchiveBytes(symbols, $"lib/net10.0/{symbolPackage}.pdb");
        using var dllStream = new MemoryStream(dllBytes);
        using var pe = new PEReader(dllStream);
        using var pdbStream = new MemoryStream(pdbBytes);
        using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
        var reader = provider.GetMetadataReader();
        var header = reader.DebugMetadataHeader ?? throw new InvalidOperationException("Missing portable PDB header.");
        Require(header.Id.Length == 20, "Unsupported portable PDB identity.");
        var identity = new BlobContentId(header.Id);
        var entries = pe.ReadDebugDirectory();
        var codeViews = entries.Where(entry => entry.Type == DebugDirectoryEntryType.CodeView).ToArray();
        Require(codeViews.Length == 1 && codeViews[0].IsPortableCodeView, "Expected exactly one portable CodeView record.");
        var codeView = pe.ReadCodeViewDebugDirectoryData(codeViews[0]);
        // Check identity before names so genuine cross-pair evidence reaches
        // the required GUID/stamp gate rather than a filename guard.
        Require(codeView.Age == 1 && new BlobContentId(codeView.Guid, codeViews[0].Stamp) == identity,
            "Portable PDB GUID/stamp does not match DLL.");
        var checksums = entries.Where(entry => entry.Type == DebugDirectoryEntryType.PdbChecksum).ToArray();
        Require(checksums.Length == 1, "Expected exactly one portable PDB checksum record.");
        var checksum = pe.ReadPdbChecksumDebugDirectoryData(checksums[0]);
        Require(checksum.AlgorithmName == "SHA256", "Unsupported PDB checksum algorithm in the current SDK profile.");
        var normalized = (byte[])pdbBytes.Clone();
        Require(header.IdStartOffset >= 0 && header.IdStartOffset <= normalized.Length - header.Id.Length,
            "Invalid portable PDB identity offset.");
        Array.Clear(normalized, header.IdStartOffset, header.Id.Length);
        var computed = SHA256.HashData(normalized);
        Require(checksum.Checksum.AsSpan().SequenceEqual(computed), "Portable PDB checksum does not match DLL.");
        Require(id == symbolPackage && pe.GetMetadataReader().GetString(pe.GetMetadataReader().GetAssemblyDefinition().Name) == id,
            "Library/symbol package assembly identity differs.");

        var sourceLinks = reader.CustomDebugInformation.Select(reader.GetCustomDebugInformation)
            .Where(record => reader.GetGuid(record.Kind) == SourceLinkKind).ToArray();
        Require(sourceLinks.Length == 1 && sourceLinks[0].Parent.Kind == HandleKind.ModuleDefinition
            && MetadataTokens.GetRowNumber(sourceLinks[0].Parent) == 1, "Expected exactly one module Source Link record.");
        using var sourceLink = JsonDocument.Parse(reader.GetBlobBytes(sourceLinks[0].Value));
        Require(sourceLink.RootElement.ValueKind == JsonValueKind.Object, "Invalid Source Link root.");
        var fields = sourceLink.RootElement.EnumerateObject().ToArray();
        Require(fields.Length == 1 && fields[0].Name == "documents" && fields[0].Value.ValueKind == JsonValueKind.Object,
            "Unsupported/duplicate Source Link fields.");
        var mappings = fields[0].Value.EnumerateObject().ToArray();
        var repository = metadata.Elements().Single(element => element.Name.LocalName == "repository");
        var commit = (string?)repository.Attribute("commit");
        Require((string?)repository.Attribute("type") == "git" && (string?)repository.Attribute("url") == RepositoryUrl
            && commit is not null && commit.Length == 40
            && commit.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f'), "Invalid Source Link repository/commit.");
        Require(mappings.Length == 1 && mappings[0].Name == "/_/*" && mappings[0].Value.ValueKind == JsonValueKind.String
            && mappings[0].Value.GetString() == $"https://raw.githubusercontent.com/firestrand/IQToolkit/{commit}/*",
            "Source Link mapping does not match the normalized commit-bound project profile.");
        var documents = reader.Documents.Select(handle =>
        {
            var document = reader.GetDocument(handle);
            var name = reader.GetString(document.Name);
            Require(name.StartsWith("/_/", StringComparison.Ordinal), "Document is outside the normalized Source Link profile.");
            return new
            {
                Name = name, Checksum = Convert.ToHexString(reader.GetBlobBytes(document.Hash)),
                Embedded = reader.GetCustomDebugInformation(handle).Select(reader.GetCustomDebugInformation)
                    .Any(record => reader.GetGuid(record.Kind) == EmbeddedSourceKind)
            };
        }).ToArray();
        Require(documents.Length > 0, "Portable symbols contain no document inventory.");
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Package = id, Commit = commit, DllSha256 = Convert.ToHexString(SHA256.HashData(dllBytes)),
            PdbSha256 = Convert.ToHexString(SHA256.HashData(pdbBytes)), identity.Guid, identity.Stamp,
            PdbChecksumSha256 = Convert.ToHexString(computed),
            ArchiveSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(packagePath))),
            SymbolArchiveSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(symbolPath))),
            SourceLink = sourceLink.RootElement, Documents = documents
        }));
    }

    // CI preparation may change only unsigned local package hashes. NuGet's
    // logical hashes for external (potentially signed) archives stay untouched.
    private static void VerifyLockDelta(string reviewedPath, string preparedPath, string feed)
    {
        using var reviewed = JsonDocument.Parse(File.ReadAllText(reviewedPath));
        using var prepared = JsonDocument.Parse(File.ReadAllText(preparedPath));
        var locked = SingleFramework(prepared.RootElement.GetProperty("dependencies"));
        var version = locked.GetProperty("IQToolkit").GetProperty("resolved").GetString()
            ?? throw new InvalidOperationException("Missing toolkit version.");
        var files = Directory.EnumerateFiles(feed, "*.nupkg").ToArray();
        Require(files.Length == Profiles.Count, "CI feed must contain exactly six toolkit archives.");
        var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var id in Profiles.Keys)
        {
            var path = Path.Combine(feed, $"{id}.{version}.nupkg");
            VerifyArchiveIdentity(path, id, version);
            using var archive = ZipFile.OpenRead(path);
            Require(archive.GetEntry(".signature.p7s") is null, "CI hash replacement requires unsigned toolkit packages.");
            hashes.Add(id, Sha512(path));
        }
        foreach (var entry in locked.EnumerateObject())
        {
            if (entry.Name == "IQToolkit" || entry.Name.StartsWith("IQToolkit.", StringComparison.OrdinalIgnoreCase))
            {
                Require(hashes.TryGetValue(entry.Name, out var hash)
                    && entry.Value.GetProperty("resolved").GetString() == version
                    && entry.Value.GetProperty("contentHash").GetString() == hash,
                    "Prepared toolkit identity/contentHash does not match the exact CI feed.");
            }
        }
        CompareLockValues(reviewed.RootElement, prepared.RootElement, [], hashes);
        Console.WriteLine("Verified complete lock equality except feed-bound toolkit contentHash leaves.");
    }

    private static void CompareLockValues(JsonElement reviewed, JsonElement prepared,
        string[] path, Dictionary<string, string> hashes)
    {
        Require(reviewed.ValueKind == prepared.ValueKind, $"Lock value kind changed at {string.Join('/', path)}.");
        if (path.Length == 4 && path[0] == "dependencies" && path[1] == "net10.0"
            && path[3] == "contentHash" && hashes.TryGetValue(path[2], out var hash))
        {
            Require(reviewed.ValueKind == JsonValueKind.String && prepared.GetString() == hash,
                "Toolkit hash replacement is not a feed-bound string leaf.");
            if (reviewed.GetString() != hash)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    Package = path[2], PreviousContentHash = reviewed.GetString(), PreparedContentHash = hash
                }));
            }
            return;
        }
        if (reviewed.ValueKind == JsonValueKind.Object)
        {
            // ToDictionary also rejects duplicate JSON properties, rather than
            // comparing only the last occurrence as GetProperty would do.
            var before = reviewed.EnumerateObject().ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);
            var after = prepared.EnumerateObject().ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);
            Require(before.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(after.Keys),
                $"Lock properties changed at {string.Join('/', path)}.");
            foreach (var item in before)
            {
                CompareLockValues(item.Value, after[item.Key], [.. path, item.Key], hashes);
            }
        }
        else if (reviewed.ValueKind == JsonValueKind.Array)
        {
            var before = reviewed.EnumerateArray().ToArray();
            var after = prepared.EnumerateArray().ToArray();
            Require(before.Length == after.Length, $"Lock array changed at {string.Join('/', path)}.");
            for (var index = 0; index < before.Length; index++)
            {
                CompareLockValues(before[index], after[index], [.. path, index.ToString()], hashes);
            }
        }
        else
        {
            var equal = reviewed.ValueKind == JsonValueKind.String
                ? reviewed.GetString() == prepared.GetString()
                : reviewed.GetRawText() == prepared.GetRawText();
            Require(equal, $"Lock value changed at {string.Join('/', path)}.");
        }
    }

    private static void VerifyGraph(string directory, string package, string version)
    {
        VerifyProject(directory, package, version);
        VerifyConfiguration(directory);
        VerifyFeed(directory, version);
        using var assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "obj", "project.assets.json")));
        using var packageLock = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "packages.lock.json")));
        var project = assets.RootElement.GetProperty("project");
        var restore = project.GetProperty("restore");
        var cache = Path.Combine(directory, "packages");
        Require(SamePath(restore.GetProperty("packagesPath").GetString(), cache), "Unexpected consumer cache.");
        var folders = assets.RootElement.GetProperty("packageFolders").EnumerateObject().ToArray();
        Require(folders.Length == 1 && SamePath(folders[0].Name, cache), "A fallback package folder was used.");
        var sources = restore.GetProperty("sources").EnumerateObject().Select(item => item.Name).ToArray();
        Require(sources.Length == 2 && sources.Contains(PublicFeed, StringComparer.Ordinal)
            && sources.Any(source => SamePath(source, Path.Combine(directory, "feed"))), "Unexpected restore sources.");
        var configurations = restore.GetProperty("configFilePaths").EnumerateArray().ToArray();
        Require(configurations.Length == 1 && SamePath(configurations[0].GetString(), Path.Combine(directory, "NuGet.config")),
            "Restore imported another NuGet configuration.");
        var restoreFramework = SingleFramework(restore.GetProperty("frameworks"));
        Require(!restoreFramework.GetProperty("projectReferences").EnumerateObject().Any(), "Assets contain project references.");
        var framework = SingleFramework(project.GetProperty("frameworks"));
        var direct = framework.GetProperty("dependencies").EnumerateObject().ToArray();
        Require(direct.Length == 1 && direct[0].Name == package
            && direct[0].Value.GetProperty("target").GetString() == "Package"
            && direct[0].Value.GetProperty("version").GetString() == $"[{version}, {version}]",
            "Assets do not declare exactly the selected direct package/version.");
        var declared = SingleFramework(assets.RootElement.GetProperty("projectFileDependencyGroups")).EnumerateArray().ToArray();
        Require(declared.Length == 1 && declared[0].GetString() == $"{package} >= {version} <= {version}",
            "Unexpected project-file dependency group.");

        var locked = SingleFramework(packageLock.RootElement.GetProperty("dependencies"));
        var lockedEntries = locked.EnumerateObject().ToArray();
        var lockedDirect = lockedEntries.Where(item => item.Value.GetProperty("type").GetString() == "Direct").ToArray();
        Require(lockedDirect.Length == 1 && lockedDirect[0].Name == package
            && lockedDirect[0].Value.GetProperty("resolved").GetString() == version
            && lockedDirect[0].Value.GetProperty("requested").GetString() == $"[{version}, {version}]",
            "The lock must contain exactly one Direct package at the selected version.");
        var libraries = assets.RootElement.GetProperty("libraries").EnumerateObject().ToArray();
        Require(libraries.Length == lockedEntries.Length, "Lock and assets have different package counts.");
        var expectedToolkit = new HashSet<string>(StringComparer.Ordinal) { "IQToolkit" };
        if (package != "IQToolkit")
        {
            expectedToolkit.Add("IQToolkit.Data");
            expectedToolkit.Add(package);
        }
        var actualToolkit = new HashSet<string>(StringComparer.Ordinal);
        var allIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var library in libraries)
        {
            Require(library.Value.GetProperty("type").GetString() == "package", "Assets contain a non-package library.");
            var identity = library.Name.Split('/');
            Require(identity.Length == 2, "Malformed package identity.");
            var id = identity[0];
            var resolved = identity[1];
            Require(allIds.Add(id), "Duplicate resolved package ID.");
            var isToolkit = id == "IQToolkit" || id.StartsWith("IQToolkit.", StringComparison.Ordinal);
            if (isToolkit)
            {
                Require(Profiles.ContainsKey(id) && resolved == version, "Unexpected toolkit package/version.");
                actualToolkit.Add(id);
            }
            Require(locked.TryGetProperty(id, out var lockEntry)
                && lockEntry.GetProperty("resolved").GetString() == resolved, "Resolved package is missing from the lock.");
            var relative = library.Value.GetProperty("path").GetString()
                ?? throw new InvalidOperationException("Missing package cache path.");
            Require(relative == $"{id.ToLowerInvariant()}/{resolved.ToLowerInvariant()}", "Unexpected cache package path.");
            var cachedDirectory = Path.Combine(cache, relative);
            var cachedArchive = Directory.EnumerateFiles(cachedDirectory, "*.nupkg").Single();
            var archiveHash = Sha512(cachedArchive);
            using var metadata = JsonDocument.Parse(File.ReadAllText(Path.Combine(cachedDirectory, ".nupkg.metadata")));
            var source = metadata.RootElement.GetProperty("source").GetString();
            Require(isToolkit ? SamePath(source, Path.Combine(directory, "feed")) : source == PublicFeed,
                "A package was restored from an unexpected source.");
            var contentHash = metadata.RootElement.GetProperty("contentHash").GetString();
            Require(contentHash is not null && contentHash == library.Value.GetProperty("sha512").GetString()
                && contentHash == lockEntry.GetProperty("contentHash").GetString(),
                "Package logical content hashes differ across lock/assets/source metadata.");
            Require(archiveHash == File.ReadAllText(cachedArchive + ".sha512").Trim(),
                "The complete cached archive hash differs from its sidecar.");
            // NuGet locked restore enforces logical content hashes. Signed
            // packages exclude signature metadata from that hash; their archive
            // bytes have a separate hash, independently checked above.
            using (var archive = ZipFile.OpenRead(cachedArchive))
            {
                Require(archiveHash == contentHash || (!isToolkit && archive.GetEntry(".signature.p7s") is not null),
                    "Unsigned package content must match the complete archive hash.");
            }
            VerifyArchiveIdentity(cachedArchive, id, resolved);
            if (isToolkit)
            {
                var feedArchive = Path.Combine(directory, "feed", $"{id}.{version}.nupkg");
                Require(archiveHash == contentHash && archiveHash == Sha512(feedArchive),
                    "Restored unsigned toolkit bytes differ from the tested local feed/content hash.");
                using var archive = ZipFile.OpenRead(feedArchive);
                var assemblyPath = $"lib/net10.0/{id}.dll";
                var entry = archive.GetEntry(assemblyPath) ?? throw new InvalidOperationException("Toolkit assembly missing from archive.");
                using var stream = entry.Open();
                var assemblyHash = Convert.ToBase64String(SHA512.HashData(stream));
                Require(assemblyHash == Sha512(Path.Combine(cachedDirectory, assemblyPath)), "Cached toolkit assembly differs from archive.");
            }
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Package = library.Name, Source = source, ArchiveSha512 = archiveHash, ContentHash = contentHash
            }));
        }
        Require(actualToolkit.SetEquals(expectedToolkit), "The independent toolkit dependency graph is incomplete or unexpected.");
        if (Profiles[package] is string driver)
        {
            Require(allIds.Contains(driver), "The provider's declared driver dependency was not restored.");
        }
        Console.WriteLine($"Verified sole direct dependency and complete package provenance: {package}.");
    }

    private static void VerifyProject(string directory, string package, string version)
    {
        var project = XDocument.Load(Path.Combine(directory, "Consumer.csproj"));
        Require(!project.Descendants("ProjectReference").Any() && !project.Descendants("Reference").Any(),
            "Consumers cannot use projects or copied assembly references.");
        var references = project.Descendants("PackageReference").ToArray();
        Require(references.Length == 1 && (string?)references[0].Attribute("Include") == package
            && (string?)references[0].Attribute("Version") == $"[{version}]", "Unexpected consumer PackageReference.");
        Require(project.Descendants("TargetFramework").Single().Value == "net10.0", "Unexpected compile target.");
    }

    private static void VerifyConfiguration(string directory)
    {
        var configuration = XDocument.Load(Path.Combine(directory, "NuGet.config"));
        var sources = configuration.Root?.Element("packageSources") ?? throw new InvalidOperationException("Missing sources.");
        Require(sources.Element("clear") is not null, "NuGet sources must be cleared.");
        var entries = sources.Elements("add").ToArray();
        Require(entries.Length == 2 && entries.Any(item => (string?)item.Attribute("key") == "local-packages"
                && (string?)item.Attribute("value") == "feed")
            && entries.Any(item => (string?)item.Attribute("key") == "nuget.org" && (string?)item.Attribute("value") == PublicFeed),
            "Unexpected configured feeds.");
        var mapping = configuration.Root?.Element("packageSourceMapping") ?? throw new InvalidOperationException("Missing source mapping.");
        Require(mapping.Element("clear") is not null, "Inherited mapping must be cleared.");
        var mapped = mapping.Elements("packageSource").ToArray();
        Require(mapped.Length == 2, "Unexpected mapping source count.");
        foreach (var item in mapped)
        {
            var key = (string?)item.Attribute("key");
            var patterns = item.Elements("package").Select(value => (string?)value.Attribute("pattern")).ToHashSet();
            Require(key == "local-packages" ? patterns.SetEquals(["IQToolkit", "IQToolkit.*"])
                : key == "nuget.org" && patterns.SetEquals(["*"]), "Unexpected package source mapping.");
        }
    }

    private static void VerifyFeed(string directory, string version)
    {
        var files = Directory.EnumerateFiles(Path.Combine(directory, "feed"), "*.nupkg").ToArray();
        Require(files.Length == Profiles.Count, "The feed must contain exactly the six selected toolkit archives.");
        foreach (var id in Profiles.Keys)
        {
            var path = Path.Combine(directory, "feed", $"{id}.{version}.nupkg");
            VerifyArchiveIdentity(path, id, version);
        }
    }

    private static void VerifyArchiveIdentity(string path, string id, string version)
    {
        using var archive = ZipFile.OpenRead(path);
        var nuspec = archive.Entries.Single(entry => entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        using var stream = nuspec.Open();
        var document = XDocument.Load(stream);
        var metadata = document.Root?.Elements().Single(element => element.Name.LocalName == "metadata")
            ?? throw new InvalidOperationException("Missing nuspec metadata.");
        Require(metadata.Elements().Single(element => element.Name.LocalName == "id").Value == id
            && metadata.Elements().Single(element => element.Name.LocalName == "version").Value == version,
            "Archive nuspec identity differs from the resolved package.");
    }

    private static void VerifyOutput(string directory, string package)
    {
        var output = Path.Combine(directory, "bin", "Release", "net10.0", "Consumer.dll");
        Require(File.Exists(Path.ChangeExtension(output, ".pdb")), "A matching compile PDB is required.");
        using var stream = File.OpenRead(output);
        using var reader = new PEReader(stream);
        var metadata = reader.GetMetadataReader();
        Require(metadata.GetString(metadata.GetAssemblyDefinition().Name) == "Consumer", "Unexpected compile output assembly.");
        var symbols = metadata.TypeDefinitions.Select(metadata.GetTypeDefinition)
            .Single(type => metadata.GetString(type.Name) == "PublicSymbols");
        var expectedMethod = package == "IQToolkit" ? "Execute" : "Create";
        Require(symbols.GetMethods().Select(metadata.GetMethodDefinition)
            .Any(method => metadata.GetString(method.Name) == expectedMethod && method.RelativeVirtualAddress != 0),
            "The named public-symbol compile body is missing.");
        var references = metadata.AssemblyReferences.Select(metadata.GetAssemblyReference)
            .Select(reference => metadata.GetString(reference.Name)).ToHashSet(StringComparer.Ordinal);
        Require(references.Contains("IQToolkit"), "The output does not reference the real core package assembly.");
        if (package != "IQToolkit")
        {
            Require(references.Contains("IQToolkit.Data") && references.Contains(package), "Missing toolkit compile assembly reference.");
        }
        if (Profiles[package] is string driver)
        {
            Require(references.Contains(driver), "The output does not reference the real transitive driver assembly.");
        }
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Package = package,
            Output = output,
            Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(output))),
            References = references.Order(StringComparer.Ordinal).ToArray()
        }));
        Console.WriteLine("Public-symbol compilation verified; no provider/backend code executed.");
    }

    private static JsonElement SingleFramework(JsonElement frameworks)
    {
        var entries = frameworks.EnumerateObject().ToArray();
        Require(entries.Length == 1 && entries[0].Name == "net10.0", "Unexpected restore/lock target profile.");
        return entries[0].Value;
    }

    private static bool SamePath(string? actual, string expected)
        => actual is not null && string.Equals(Path.GetFullPath(actual).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(expected).TrimEnd(Path.DirectorySeparatorChar), StringComparison.Ordinal);

    private static string Sha512(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToBase64String(SHA512.HashData(stream));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
