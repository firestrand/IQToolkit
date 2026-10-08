using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace IQToolkit.FinalVerification;

internal static class CommandInitializationContinuity
{
    private const long MaximumBytes = 16 * 1024 * 1024;
    private static readonly XNamespace Trx = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
    private static readonly Guid Sha256Algorithm = new("8829d00f-11b8-4213-878b-770e8597ac16");
    private static bool baselineContext;
    private static readonly Dictionary<string, Input> Bindings = new(StringComparer.Ordinal);
    private static readonly HashSet<string> UsedBindings = new(StringComparer.Ordinal);
    private static JsonElement transition;
    private static object? transitionReceipt;
    private const string AddedSource = "IQToolkitTest/RealProvider/NativeCommandInitializationTests.cs";
    private const string AddedClass = "IQToolkitTest.RealProvider.NativeCommandInitializationTests";
    private const string DataSource = "IQToolkit.Data/DbEntityProvider.cs";
    private const string OldDataHash = "72781aeee7e01842f3634bdc64ebbcdcd7bd0cafbeb9097ad30da6873af4860e";
    private const string NewDataHash = "ea08fc785fd365bf93962e2d5e566b6ab9cb13a423c7302bc486172d13a31c9a";
    private const string TestSourceHash = "8e4ce7f9d15f41f45a111675e777f0f35df538a9bf264378f4a87bd43b67914a";
    private static Input[] historicalChain = [];
    private static readonly string[] AddedMethods =
    [
        "NativeCommandArgumentsAreAcquired",
        "BorrowedEagerInitializationFailureDisposesCommand",
        "OwnedEagerInitializationFailureDisposesCommandBeforeConnectionClose",
        "CompleteNativeArgumentsPreserveEagerSuccess",
        "NativeSingleArgumentPreservesEagerSuccess"
    ];

    private sealed record Input(string Path, string Sha256);
    private sealed record Module(string Name, Input Dll, Input Pdb);
    private sealed record Preservation(Input Before, Input After);
    private sealed record Descriptor(int Schema, string RepositoryRoot, Input[] Reports, Input CoverageTrx,
        Input StrictTrx, Input Translation, Input StrictReceipt, Input Settings, Input Collector,
        Input Mapping, Module[] Modules, Module TestAssembly, Preservation[] Preservation);
    private sealed record RetainedInput(string Sha256, string Snapshot);
    private sealed record Key(string Module, string Source, string Class, string Method, string Signature, int Line);
    private sealed record Condition(string Number, string Type, double Percentage);
    private sealed record Leaf(bool Covered, int CoveredBranches, int Branches, Condition[] Conditions);
    private sealed record Counts(int Lines, int CoveredLines, int Branches, int CoveredBranches);
    private sealed record Measurement(Descriptor Descriptor, Dictionary<Key, Leaf> Leaves,
        Dictionary<string, Counts> Modules, Dictionary<string, string> Sources, Counts Total,
        Dictionary<string, Dictionary<string, string>> Documents);

    // Reuses the executed transition verifier; retention belongs to the final coordinator.
    internal static object Validate(string baseline, string current, string manifest)
    {
        ReadTransition([baseline, current, string.Empty, manifest]);
        baselineContext = true;
        var before = Qualify(ReadDescriptor(baseline));
        baselineContext = false;
        var after = Qualify(ReadDescriptor(current));
        Compare(before, after);
        VerifyTransition(before, after);
        return transitionReceipt ?? throw new InvalidDataException("Missing command initialization transition receipt.");
    }

    private static void Require([DoesNotReturnIf(false)] bool condition, string reason)
    {
        if (!condition) throw new InvalidDataException(reason);
    }

    private static string Hash(string path, string? expected = null)
    {
        using var input = File.OpenRead(Capture(path, expected));
        return Convert.ToHexString(SHA256.HashData(input));
    }

    private static string Capture(string path, string? expected = null) =>
        Program.CaptureHistoricalCommand(Resolve(path), expected);

    private static string Capture(Input input) => Capture(input.Path, input.Sha256);

    private static Descriptor ReadDescriptor(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Capture(path)));
        RejectDuplicateProperties(document.RootElement);
        var descriptor = document.Deserialize<Descriptor>() ?? throw new InvalidDataException("Missing descriptor.");
        Require(descriptor.Schema == 1 && Path.IsPathFullyQualified(descriptor.RepositoryRoot), "Unsupported descriptor schema or repository root.");
        Require(descriptor.Reports.Length is > 0 and <= 16, "Missing coverage report copies.");
        foreach (var input in descriptor.Reports.Concat(new[] { descriptor.CoverageTrx, descriptor.StrictTrx,
            descriptor.Translation, descriptor.StrictReceipt, descriptor.Settings, descriptor.Collector, descriptor.Mapping })
            .Concat(descriptor.Modules.Append(descriptor.TestAssembly).SelectMany(module => new[] { module.Dll, module.Pdb }))
            .Concat(descriptor.Preservation.SelectMany(pair => new[] { pair.Before, pair.After })))
        {
            Require(Path.IsPathFullyQualified(input.Path) && Regex.IsMatch(input.Sha256, "^[0-9A-Fa-f]{64}$"), "Invalid input path or SHA256.");
            Capture(input);
        }
        Require(descriptor.Reports.Select(input => Path.GetFullPath(input.Path)).Distinct(StringComparer.Ordinal).Count() == descriptor.Reports.Length,
            "Duplicate coverage attachment path.");
        var preserved = new List<Dictionary<string, string>>();
        foreach (var pair in descriptor.Preservation)
        {
            var before = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllBytes(Capture(pair.Before)));
            var after = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllBytes(Capture(pair.After)));
            Require(before is { Count: > 0 } && after is not null && before.Count == after.Count &&
                before.All(item => after.TryGetValue(item.Key, out var hash) && hash == item.Value), "Original input path/hash preservation failed.");
            preserved.Add(before);
        }
        Require(descriptor.Preservation.Length > 0, "Missing original-preservation evidence.");
        foreach (var binary in descriptor.Modules.Append(descriptor.TestAssembly).SelectMany(module => new[] { module.Dll, module.Pdb }))
        {
            var relative = Path.GetRelativePath(descriptor.RepositoryRoot, binary.Path).Replace('\\', '/');
            Require(!relative.StartsWith("../", StringComparison.Ordinal) && !Path.IsPathRooted(relative) &&
                preserved.Any(manifest => manifest.TryGetValue(relative, out var hash) &&
                    hash.Equals(binary.Sha256, StringComparison.OrdinalIgnoreCase)),
                "Descriptor binary does not match the collection preservation manifest.");
        }
        return descriptor;
    }

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var properties = element.EnumerateObject().ToArray();
            Require(properties.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == properties.Length,
                "Duplicate descriptor property.");
            foreach (var property in properties) RejectDuplicateProperties(property.Value);
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var child in element.EnumerateArray()) RejectDuplicateProperties(child);
    }

    private static XElement Xml(Input input)
    {
        using var stream = File.OpenRead(Capture(input));
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaximumBytes
        });
        return XDocument.Load(reader).Root ?? throw new InvalidDataException("Missing XML root.");
    }

    private static int Number(string? value)
    {
        Require(int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number >= 0,
            "Invalid coverage count.");
        return number;
    }

    private static string Attribute(XElement element, string name) =>
        (string?)element.Attribute(name) ?? throw new InvalidDataException("Missing coverage attribute.");

    private static Measurement Qualify(Descriptor descriptor)
    {
        Require(descriptor.Reports.Select(input => input.Sha256.ToUpperInvariant()).Distinct().Count() == 1,
            "Coverage attachment copies disagree.");
        var root = Xml(descriptor.Reports[0]);
        var packages = root.Element("packages")?.Elements("package").ToArray() ?? [];
        Require(root.Name == "coverage" && packages.Length > 0 && root.Attribute("lines-valid") is not null &&
            root.Attribute("branches-valid") is not null, "No measured coverage modules or denominators.");
        var expected = descriptor.Modules.Select(module => module.Name).Order(StringComparer.Ordinal).ToArray();
        Require(expected.SequenceEqual(new[] { "IQToolkit", "IQToolkit.Data", "IQToolkit.Data.SQLite", "IQToolkit.Data.SqlClient" }) &&
            packages.Select(package => Attribute(package, "name")).Order(StringComparer.Ordinal).SequenceEqual(expected),
            "Coverage module scope differs from the qualified four-library profile.");
        ValidateExecutions(descriptor);
        var documents = descriptor.Modules.Append(descriptor.TestAssembly).ToDictionary(module => module.Name,
            module => VerifySymbols(module, descriptor), StringComparer.Ordinal);
        var leaves = new Dictionary<Key, Leaf>();
        var modules = new Dictionary<string, Counts>(StringComparer.Ordinal);
        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var package in packages)
        {
            var module = Attribute(package, "name");
            int lines = 0, coveredLines = 0, branches = 0, coveredBranches = 0;
            foreach (var type in package.Element("classes")?.Elements("class") ?? [])
            {
                var source = Path.GetRelativePath(descriptor.RepositoryRoot, Attribute(type, "filename")).Replace('\\', '/');
                var found = documents[module].TryGetValue(source, out var sourceHash);
                Require(!source.StartsWith("../", StringComparison.Ordinal) && !Path.IsPathRooted(source) && found,
                    "Coverage source has no authenticated PDB document.");
                var hash = Hash(Path.Combine(descriptor.RepositoryRoot, source), sourceHash);
                Require(hash == sourceHash, "Coverage source differs from the compiled PDB checksum.");
                sources[source] = hash;
                var name = Attribute(type, "name");
                foreach (var line in type.Element("lines")?.Elements("line") ?? [])
                {
                    var leaf = AddLeaf(leaves, module, source, name, string.Empty, string.Empty, line);
                    lines = checked(lines + 1);
                    coveredLines = checked(coveredLines + (leaf.Covered ? 1 : 0));
                    branches = checked(branches + leaf.Branches);
                    coveredBranches = checked(coveredBranches + leaf.CoveredBranches);
                }
                foreach (var method in type.Element("methods")?.Elements("method") ?? [])
                    foreach (var line in method.Element("lines")?.Elements("line") ?? [])
                        AddLeaf(leaves, module, source, name, Attribute(method, "name"), Attribute(method, "signature"), line);
            }
            Require(lines > 0 && branches > 0, "A declared module has no measured lines or branches.");
            modules.Add(module, new Counts(lines, coveredLines, branches, coveredBranches));
        }
        var total = new Counts(modules.Values.Sum(count => count.Lines), modules.Values.Sum(count => count.CoveredLines),
            modules.Values.Sum(count => count.Branches), modules.Values.Sum(count => count.CoveredBranches));
        Require(total.Lines == Number(Attribute(root, "lines-valid")) && total.CoveredLines == Number(Attribute(root, "lines-covered")) &&
            total.Branches == Number(Attribute(root, "branches-valid")) && total.CoveredBranches == Number(Attribute(root, "branches-covered")),
            "Class-level counts disagree with native coverage totals.");
        Require(descriptor.TestAssembly.Name == "IQToolkitTest", "Unexpected test assembly.");
        return new Measurement(descriptor, leaves, modules, sources, total, documents);
    }

    private static Dictionary<string, string> VerifySymbols(Module module, Descriptor descriptor)
    {
        var dll = File.ReadAllBytes(Capture(module.Dll));
        var pdb = File.ReadAllBytes(Capture(module.Pdb));
        using var pe = new PEReader(new MemoryStream(dll));
        using var provider = MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(pdb));
        var reader = provider.GetMetadataReader();
        var header = reader.DebugMetadataHeader ?? throw new InvalidDataException("Missing portable PDB header.");
        var entries = pe.ReadDebugDirectory();
        var codeViews = entries.Where(entry => entry.Type == DebugDirectoryEntryType.CodeView).ToArray();
        Require(header.Id.Length == 20 && codeViews.Length == 1 && codeViews[0].IsPortableCodeView, "Unsupported DLL/PDB identity.");
        var codeView = pe.ReadCodeViewDebugDirectoryData(codeViews[0]);
        Require(codeView.Age == 1 && new BlobContentId(header.Id) == new BlobContentId(codeView.Guid, codeViews[0].Stamp),
            "DLL and portable PDB identities differ.");
        var checksums = entries.Where(entry => entry.Type == DebugDirectoryEntryType.PdbChecksum).ToArray();
        Require(checksums.Length == 1 && header.IdStartOffset >= 0 && header.IdStartOffset <= pdb.Length - header.Id.Length,
            "Missing PDB checksum or invalid identity offset.");
        var checksum = pe.ReadPdbChecksumDebugDirectoryData(checksums[0]);
        Array.Clear(pdb, header.IdStartOffset, header.Id.Length);
        Require(checksum.AlgorithmName == "SHA256" && checksum.Checksum.AsSpan().SequenceEqual(SHA256.HashData(pdb)), "DLL PDB checksum differs.");
        var metadata = pe.GetMetadataReader();
        Require(metadata.GetString(metadata.GetAssemblyDefinition().Name) == module.Name, "DLL module name differs.");
        var mapping = File.ReadAllText(Capture(descriptor.Mapping), Encoding.Unicode);
        var sourceRoot = Path.GetFullPath(descriptor.RepositoryRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        Require(mapping.Split('\n').Any(line => line.TrimEnd('\r').EndsWith("|" + sourceRoot + "=/_/", StringComparison.Ordinal)),
            "Source-root mapping does not authenticate the normalized repository root.");
        var documents = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var handle in reader.Documents)
        {
            var document = reader.GetDocument(handle);
            var name = reader.GetString(document.Name);
            Require(reader.GetGuid(document.HashAlgorithm) == Sha256Algorithm, "Unsupported PDB checksum algorithm.");
            var documentRoot = sourceRoot;
            string suffix;
            if (name.StartsWith("/_/", StringComparison.Ordinal)) suffix = name[3..];
            else
            {
                // The installed test SDK compiles its real Program.cs from the
                // mapped package cache. Authenticate it rather than exclude it.
                documentRoot = Path.Combine(sourceRoot, "artifacts", "nuget", "packages") + Path.DirectorySeparatorChar;
                Require(module.Name == "IQToolkitTest" && name.StartsWith("/_1/", StringComparison.Ordinal) &&
                    mapping.Split('\n').Any(line => line.TrimEnd('\r').EndsWith("|" + documentRoot + "=/_1/", StringComparison.Ordinal)),
                    "Unsupported PDB document root or missing package-cache mapping.");
                suffix = name[4..];
            }
            var full = Path.GetFullPath(Path.Combine(documentRoot, suffix));
            Require(full.StartsWith(documentRoot, StringComparison.Ordinal), "PDB document escapes its authenticated root.");
            var relative = Path.GetRelativePath(sourceRoot, full).Replace('\\', '/');
            var expected = Convert.ToHexString(reader.GetBlobBytes(document.Hash));
            Capture(full, expected);
            Require(documents.TryAdd(relative, expected), "Duplicate PDB document.");
        }
        Require(documents.Count > 0, "PDB has no source documents.");
        if (!baselineContext && module.Name == "IQToolkitTest") VerifyAddedMethods(pe, reader);
        return documents;
    }

    private static Leaf AddLeaf(Dictionary<Key, Leaf> leaves, string module, string source, string type,
        string method, string signature, XElement line)
    {
        var branch = Attribute(line, "branch");
        Require(branch is "True" or "False", "Unexpected branch flag.");
        int covered = 0, valid = 0;
        if (branch == "True")
        {
            var fraction = Regex.Match(Attribute(line, "condition-coverage"), @"^[0-9.]+% \(([0-9]+)/([0-9]+)\)$");
            Require(fraction.Success, "Invalid branch fraction.");
            covered = Number(fraction.Groups[1].Value);
            valid = Number(fraction.Groups[2].Value);
            Require(valid > 0 && covered <= valid, "Invalid branch counts.");
        }
        var conditions = (line.Element("conditions")?.Elements("condition") ?? []).Select(condition =>
        {
            var text = Attribute(condition, "coverage");
            Require(text.EndsWith('%') && double.TryParse(text[..^1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                out var percentage) && double.IsFinite(percentage) && percentage is >= 0 and <= 100, "Invalid condition coverage percentage.");
            return new Condition(Attribute(condition, "number"), Attribute(condition, "type"), double.Parse(text[..^1], CultureInfo.InvariantCulture));
        }).ToArray();
        Require((branch == "True") == (conditions.Length > 0) &&
            conditions.Select(condition => (condition.Number, condition.Type)).Distinct().Count() == conditions.Length,
            "Missing or duplicate condition structure.");
        var number = Number(Attribute(line, "number"));
        Require(number > 0, "Invalid source line number.");
        var leaf = new Leaf(Number(Attribute(line, "hits")) > 0, covered, valid, conditions);
        Require(leaves.TryAdd(new Key(module, source, type, method, signature, number), leaf), "Duplicate coverage leaf identity.");
        return leaf;
    }

    private static Dictionary<string, string> Executions(Input input, bool focused = false)
    {
        var root = Xml(input);
        Require(root.Name == Trx + "TestRun", "Unexpected test report root.");
        var definitions = root.Element(Trx + "TestDefinitions")?.Elements(Trx + "UnitTest").ToDictionary(
            test => Attribute(test, "id"), StringComparer.Ordinal) ?? [];
        var results = root.Element(Trx + "Results")?.Elements(Trx + "UnitTestResult").ToArray() ?? [];
        var observed = new Dictionary<string, string>(StringComparer.Ordinal);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var passed = 0;
        foreach (var result in results)
        {
            Require(definitions.TryGetValue(Attribute(result, "testId"), out var definition), "Unlinked test result.");
            var execution = Attribute(result, "executionId");
            Require(ids.Add(execution) && (string?)definition!.Element(Trx + "Execution")?.Attribute("id") == execution, "Test execution identity differs.");
            var method = definition.Element(Trx + "TestMethod") ?? throw new InvalidDataException("Missing test method.");
            var type = Attribute(method, "className");
            Require(type.StartsWith("IQToolkitTest.", StringComparison.Ordinal) &&
                Path.GetFileName(Attribute(method, "codeBase")) == "IQToolkitTest.dll" &&
                Attribute(method, "adapterTypeName") == "executor://mstestadapter/v4", "Unexpected test suite or adapter.");
            var outcome = Attribute(result, "outcome");
            Require(outcome is "Passed" or "NotExecuted", "Test failure or unsupported test outcome.");
            if (outcome == "Passed") passed++;
            else Require(definition.Element(Trx + "TestCategory")?.Elements(Trx + "TestCategoryItem")
                .Any(category => (string?)category.Attribute("TestCategory") == "SqlServerIntegration") == true, "Unexpected offline test skip.");
            Require(observed.TryAdd(type + "." + Attribute(method, "name"), outcome), "Duplicate test method result.");
        }
        var summary = root.Element(Trx + "ResultSummary");
        var counters = summary?.Element(Trx + "Counters");
        Require(passed > 0 && definitions.Count == results.Length && (string?)summary?.Attribute("outcome") == "Completed" && counters is not null &&
            Number(Attribute(counters, "total")) == results.Length && Number(Attribute(counters, "passed")) == passed &&
            Number(Attribute(counters, "executed")) == passed, "Empty, incomplete or inconsistent test execution.");
        foreach (var name in new[] { "failed", "error", "timeout", "aborted", "inconclusive", "passedButRunAborted", "notRunnable",
            "disconnected", "warning", "completed", "inProgress", "pending" })
            Require(Number(Attribute(counters!, name)) == 0, "Incomplete or failed test run.");
        var skips = Number(Attribute(counters!, "notExecuted"));
        Require(skips == 0 || skips == results.Length - passed, "Unsupported skip counter discrepancy.");
        foreach (var info in summary!.Element(Trx + "RunInfos")?.Elements(Trx + "RunInfo") ?? [])
            Require(Attribute(info, "outcome") == "Passed", "Test run-level failure.");
        Require(focused || (observed.Any(pair => pair.Key.StartsWith("IQToolkitTest.SQLiteTests.", StringComparison.Ordinal) && pair.Value == "Passed") &&
            observed.Any(pair => pair.Key.StartsWith("IQToolkitTest.RealProvider.", StringComparison.Ordinal) && pair.Value == "Passed")), "Expected native test suites did not execute.");
        return observed;
    }

    private static void ValidateExecutions(Descriptor descriptor)
    {
        using var receipt = JsonDocument.Parse(File.ReadAllBytes(Capture(descriptor.StrictReceipt)));
        Require(receipt.RootElement.GetProperty("Status").GetString() == "verified" &&
            string.Equals(receipt.RootElement.GetProperty("TrxSha256").GetString(), descriptor.StrictTrx.Sha256, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(receipt.RootElement.GetProperty("TranslationSha256").GetString(), descriptor.Translation.Sha256, StringComparison.OrdinalIgnoreCase) &&
            receipt.RootElement.GetProperty("Translations").GetInt32() > 0, "Strict verification receipt does not bind the supplied results.");
        var strict = Executions(descriptor.StrictTrx);
        var collected = Executions(descriptor.CoverageTrx);
        Require(strict.Count == collected.Count && strict.All(pair => collected.TryGetValue(pair.Key, out var outcome) && outcome == pair.Value),
            "Collector did not execute the strict suite and outcomes.");
    }

    private static void Compare(Measurement baseline, Measurement current)
    {
        Require(baseline.Descriptor.Settings.Sha256.Equals(current.Descriptor.Settings.Sha256, StringComparison.OrdinalIgnoreCase) &&
            baseline.Descriptor.Collector.Sha256.Equals(current.Descriptor.Collector.Sha256, StringComparison.OrdinalIgnoreCase) &&
            baseline.Descriptor.Mapping.Sha256.Equals(current.Descriptor.Mapping.Sha256, StringComparison.OrdinalIgnoreCase), "Collector, settings or source mapping changed; explicit qualification required.");
        foreach (var module in baseline.Descriptor.Modules)
        {
            var matching = current.Descriptor.Modules.Append(current.Descriptor.TestAssembly).Single(item => item.Name == module.Name);
            var sameDll = module.Dll.Sha256.Equals(matching.Dll.Sha256, StringComparison.OrdinalIgnoreCase);
            var samePdb = module.Pdb.Sha256.Equals(matching.Pdb.Sha256, StringComparison.OrdinalIgnoreCase);
            Require(module.Name == "IQToolkit.Data" ? !sameDll && !samePdb : sameDll && samePdb,
                "Compiled module transition exceeds the exact Data DLL/PDB pair.");
        }
        Require(baseline.Sources.Count == 97 && current.Sources.Count == 97 &&
            baseline.Sources.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(current.Sources.Keys) &&
            baseline.Sources.Where(pair => pair.Key != DataSource).All(pair => current.Sources[pair.Key] == pair.Value) &&
            baseline.Sources[DataSource].Equals(OldDataHash, StringComparison.OrdinalIgnoreCase) &&
            current.Sources[DataSource].Equals(NewDataHash, StringComparison.OrdinalIgnoreCase),
            "Measured source transition is not exactly the pinned GetCommand edit and 96 unchanged sources.");
        Require(baseline.Leaves.Count == 16716 && current.Leaves.Count == 16724, "Coverage leaf growth is not the observed eight leaves.");
        var inherited = new HashSet<Key>();
        foreach (var pair in baseline.Leaves)
        {
            var key = MapKey(pair.Key);
            Require(inherited.Add(key), "Mapped inherited coverage leaf collides.");
            Require(current.Leaves.TryGetValue(key, out var leaf) && leaf.Branches == pair.Value.Branches,
                "Coverage leaf structure or denominator changed.");
            Require((!pair.Value.Covered || leaf!.Covered) && leaf!.CoveredBranches >= pair.Value.CoveredBranches, "Coverage leaf regressed.");
            Require(pair.Value.Conditions.Length == leaf.Conditions.Length, "Branch condition structure changed.");
            for (var index = 0; index < pair.Value.Conditions.Length; index++)
            {
                var before = pair.Value.Conditions[index];
                var after = leaf.Conditions[index];
                Require(before.Number == after.Number && before.Type == after.Type && after.Percentage >= before.Percentage,
                    "Branch condition structure changed or reported coverage regressed.");
            }
        }
        var additions = current.Leaves.Keys.Except(inherited).ToHashSet();
        var expected = new HashSet<Key>();
        foreach (var line in new[] { 640, 642, 643, 645 })
        {
            expected.Add(new Key("IQToolkit.Data", DataSource, "IQToolkit.Data.DbEntityProvider.Executor", string.Empty, string.Empty, line));
            expected.Add(new Key("IQToolkit.Data", DataSource, "IQToolkit.Data.DbEntityProvider.Executor", "GetCommand",
                "(IQToolkit.Data.Common.QueryCommand, object[])", line));
        }
        Require(additions.SetEquals(expected) && additions.All(key => current.Leaves[key] is
            { Covered: true, Branches: 0, CoveredBranches: 0 } && current.Leaves[key].Conditions.Length == 0),
            "New native coverage leaves differ from the exact covered catch/Dispose/rethrow/epilogue class and method leaves.");
    }

    private static Key MapKey(Key key) => key.Source == DataSource
        ? key with { Line = key.Line <= 631 ? key.Line : checked(key.Line + (key.Line <= 636 ? 2 : 8)) }
        : key;

    private static string Resolve(string path)
    {
        path = Path.GetFullPath(path);
        if (baselineContext && Bindings.TryGetValue(path, out var input))
        {
            UsedBindings.Add(path);
            return input.Path;
        }
        return path;
    }

    private static Input ReadInput(JsonElement element)
    {
        Require(element.ValueKind == JsonValueKind.Object && element.EnumerateObject().Select(item => item.Name)
            .Order(StringComparer.Ordinal).SequenceEqual(new[] { "Path", "Sha256" }), "Unexpected input fields.");
        var input = element.Deserialize<Input>() ?? throw new InvalidDataException("Missing transition input.");
        Require(Path.IsPathFullyQualified(input.Path) && Path.GetFullPath(input.Path) == input.Path &&
            Regex.IsMatch(input.Sha256, "^[0-9A-Fa-f]{64}$"), "Invalid transition input.");
        return input;
    }

    private static Input TransitionInput(string name) => ReadInput(transition.GetProperty(name));

    private static void ReadTransition(string[] args)
    {
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(Capture(args[3])));
        transition = manifest.RootElement.Clone();
        RejectDuplicateProperties(transition);
        Require(transition.EnumerateObject().Select(item => item.Name).Order(StringComparer.Ordinal).SequenceEqual(
            new[] { "BaselineBindings", "BaselineDescriptor", "CurrentDescriptor", "DataSource", "FocusedTrx", "OldDataSource", "Schema", "TestSource" }) &&
            transition.GetProperty("Schema").GetInt32() == 1, "Unexpected transition schema or fields.");
        var baselineInput = TransitionInput("BaselineDescriptor");
        var currentInput = TransitionInput("CurrentDescriptor");
        Require(Path.GetFullPath(args[0]) == baselineInput.Path && Path.GetFullPath(args[1]) == currentInput.Path &&
            baselineInput.Sha256.Equals("77495a1bcb8b29eb57deec221325b1305fdf2f728515cb885198dc82df3bd64c",
                StringComparison.OrdinalIgnoreCase), "Descriptor origins or qualified V1z baseline hash differ.");
        using var document = JsonDocument.Parse(File.ReadAllBytes(Capture(baselineInput)));
        RejectDuplicateProperties(document.RootElement);
        var descriptor = document.Deserialize<Descriptor>() ?? throw new InvalidDataException("Missing baseline descriptor.");
        var root = Path.GetFullPath(descriptor.RepositoryRoot);
        Require(baselineInput.Path == Path.Combine(root, "artifacts", "native-null-array", "coverage.yrni_8j2", "collection", "descriptor.json"),
            "Baseline descriptor is not the fixed qualified V1z origin.");
        Capture(currentInput);
        var source = TransitionInput("TestSource");
        Require(source.Path == Path.Combine(root, AddedSource) && source.Sha256.Equals(TestSourceHash, StringComparison.OrdinalIgnoreCase),
            "Added source origin or reviewed behavioral source hash differs.");
        Capture(source);
        var focused = TransitionInput("FocusedTrx");
        Require(focused.Path == Path.Combine(root, "artifacts", "command-initialization", "green.gl2ik3j8", "results", "behavior.trx"),
            "Focused TRX is not the authentic GREEN behavioral origin.");
        Capture(focused);
        var data = TransitionInput("DataSource");
        var oldData = TransitionInput("OldDataSource");
        Require(data.Path == Path.Combine(root, DataSource) && data.Sha256.Equals(NewDataHash, StringComparison.OrdinalIgnoreCase) &&
            oldData.Path == Path.Combine(root, "artifacts", "command-initialization", "red.bg8dit7i", "DbEntityProvider.before.cs.snapshot") &&
            oldData.Sha256.Equals(OldDataHash, StringComparison.OrdinalIgnoreCase), "Reviewed Data source transition differs.");
        VerifySourceTransformation(oldData, data);
        VerifyHistoricalChain(root, baselineInput);
        foreach (var property in transition.GetProperty("BaselineBindings").EnumerateObject())
        {
            Require(Path.IsPathFullyQualified(property.Name) && Path.GetFullPath(property.Name) == property.Name &&
                Bindings.TryAdd(property.Name, ReadInput(property.Value)), "Invalid or duplicate baseline binding.");
        }
        var required = new HashSet<string>(StringComparer.Ordinal);
        var oldModule = descriptor.Modules.Single(module => module.Name == "IQToolkit.Data");
        foreach (var module in new[] { oldModule, descriptor.TestAssembly })
        {
            var acquisition = module.Name == "IQToolkit.Data" ? "green.gl2ik3j8" : "acquire.tnhkhd1q";
            foreach (var binary in new[] { module.Dll, module.Pdb })
            {
                var logical = Path.GetFullPath(binary.Path);
                required.Add(logical);
                Require(logical == Path.Combine(root, "IQToolkitTest", "bin", "Release", "net10.0", Path.GetFileName(logical)) &&
                    Bindings.TryGetValue(logical, out var physical) && physical.Path == Path.Combine(root, "artifacts", "command-initialization",
                        acquisition, "previous-binaries", "IQToolkitTest", "bin", "Release", "net10.0", Path.GetFileName(logical)) &&
                    physical.Sha256.Equals(binary.Sha256, StringComparison.OrdinalIgnoreCase), "Old compiled binary binding differs.");
                Capture(Bindings[logical]);
            }
        }
        required.Add(data.Path);
        Require(Bindings.TryGetValue(data.Path, out var oldSource) && oldSource.Path == oldData.Path &&
            oldSource.Sha256.Equals(oldData.Sha256, StringComparison.OrdinalIgnoreCase) &&
            Bindings.Count == 5 && required.SetEquals(Bindings.Keys), "Expected exactly five reviewed historical aliases.");
    }

    private static void VerifySourceTransformation(Input before, Input after)
    {
        const string original = "                cmd.CommandText = query.CommandText;\n" +
            "                if (this.provider.Transaction != null)\n" +
            "                    cmd.Transaction = this.provider.Transaction;\n" +
            "                this.SetParameterValues(query, cmd, paramValues);\n" +
            "                return cmd;\n";
        const string replacement = "                try\n" +
            "                {\n" +
            "                    cmd.CommandText = query.CommandText;\n" +
            "                    if (this.provider.Transaction != null)\n" +
            "                        cmd.Transaction = this.provider.Transaction;\n" +
            "                    this.SetParameterValues(query, cmd, paramValues);\n" +
            "                    return cmd;\n" +
            "                }\n" +
            "                catch\n" +
            "                {\n" +
            "                    cmd.Dispose();\n" +
            "                    throw;\n" +
            "                }\n";
        var oldBytes = File.ReadAllBytes(Capture(before));
        var text = Encoding.UTF8.GetString(oldBytes);
        var index = text.IndexOf(original, StringComparison.Ordinal);
        Require(index >= 0 && text.IndexOf(original, index + original.Length, StringComparison.Ordinal) < 0 &&
            text[..index].Count(character => character == '\n') == 631,
            "Original initializer is not the sole pinned five-line block starting at line632.");
        Require(Encoding.UTF8.GetBytes(text.Replace(original, replacement, StringComparison.Ordinal))
            .AsSpan().SequenceEqual(File.ReadAllBytes(Capture(after))), "Data edit exceeds the exact GetCommand try/catch transformation.");
    }

    private static void VerifyHistoricalChain(string root, Input baseline)
    {
        historicalChain =
        [
            new(Path.Combine(root, "artifacts/coverage/runs/run.JbUMhIGI/descriptor.json"), "92ec8ed76f6319b09793a7098125cb3ded36acc98aa0d5959fbd39528de834c0"),
            new(Path.Combine(root, "artifacts/sql-string-literal/coverage.zzhhnhb6/collection/descriptor.json"), "173f9ce97c74f21f4c739f0afb74ce1abb596ed8e4ab7befd7280d849a940f10"),
            new(Path.Combine(root, "artifacts/sql-string-literal/coverage.zzhhnhb6/retention/transition.json"), "1762e22108cd2248c6b2fcfcb76cc54d3b52ac5628333d5e114168f468fb9740"),
            new(Path.Combine(root, "artifacts/sql-string-literal/coverage.zzhhnhb6/retention/inputs-before.json"), "26a0bb7822b2dba84c3e3fdb05ea703d7ffc2dadc0d31a207fa32595f12976fc"),
            new(Path.Combine(root, "artifacts/sql-string-literal/coverage.zzhhnhb6/retention/inputs-after.json"), "547318383be6a76a21ae2c2b3f98d1a66ebaa61b2daddc74b5b0afe91116bd09"),
            new(Path.Combine(root, "artifacts/native-null-array/coverage.yrni_8j2/transition.json"), "dc7ea30a288b9d64a50c98a5b23315a8b663fb2ad50382ba27d61b0fd298d497"),
            new(Path.Combine(root, "artifacts/native-null-array/coverage.yrni_8j2/retention/transition.json"), "6e2a328479e34786ccf5ba2e73072033123f7632ee78b7ef2c9939cb61cfcd2b"),
            new(Path.Combine(root, "artifacts/native-null-array/coverage.yrni_8j2/retention/inputs-before.json"), "6bbb7d086ed80474406f24cd777961276a815482ddaf9fd7ccc3f1152b27b5b0"),
            new(Path.Combine(root, "artifacts/native-null-array/coverage.yrni_8j2/retention/inputs-after.json"), "1e3a44f4972a229ef12280b7324cd8fbf3f24b26a8c2bac6cc13a47d0a155362")
        ];
        foreach (var input in historicalChain) Capture(input);
        using var first = JsonDocument.Parse(File.ReadAllBytes(Capture(historicalChain[2])));
        using var second = JsonDocument.Parse(File.ReadAllBytes(Capture(historicalChain[6])));
        RejectDuplicateProperties(first.RootElement); RejectDuplicateProperties(second.RootElement);
        bool Same(Input left, Input right) => left.Path == right.Path && left.Sha256.Equals(right.Sha256, StringComparison.OrdinalIgnoreCase);
        Require(first.RootElement.GetProperty("Status").GetString() == "verified" &&
            Same(ReadInput(first.RootElement.GetProperty("Baseline")), historicalChain[0]) &&
            Same(ReadInput(first.RootElement.GetProperty("Current")), historicalChain[1]) &&
            second.RootElement.GetProperty("Status").GetString() == "verified" &&
            Same(ReadInput(second.RootElement.GetProperty("BaselineDescriptor")), historicalChain[1]) &&
            Same(ReadInput(second.RootElement.GetProperty("CurrentDescriptor")), baseline), "Permanent original-to-V1y-to-V1z receipt chain differs.");
        VerifyHistoricalInputs(historicalChain[3], historicalChain[4], 198);
        VerifyHistoricalInputs(historicalChain[7], historicalChain[8], 188);
    }

    private static void VerifyHistoricalInputs(Input before, Input after, int count)
    {
        using var first = JsonDocument.Parse(File.ReadAllBytes(Capture(before)));
        using var second = JsonDocument.Parse(File.ReadAllBytes(Capture(after)));
        RejectDuplicateProperties(first.RootElement); RejectDuplicateProperties(second.RootElement);
        var old = first.RootElement.EnumerateObject().ToArray();
        Require(old.Length == count && second.RootElement.EnumerateObject().Count() == count, "Historical retained-input count differs.");
        foreach (var property in old)
        {
            var value = property.Value.Deserialize<RetainedInput>() ?? throw new InvalidDataException("Missing historical retained input.");
            Require(second.RootElement.TryGetProperty(property.Name, out var final) &&
                value.Sha256.Equals(final.GetString(), StringComparison.OrdinalIgnoreCase) &&
                Path.IsPathFullyQualified(value.Snapshot) && Path.GetFullPath(value.Snapshot) == value.Snapshot &&
                Path.GetDirectoryName(value.Snapshot) == Path.GetDirectoryName(before.Path), "Historical input preservation or snapshot origin differs.");
            Capture(value.Snapshot, value.Sha256);
        }
    }

    private static Dictionary<string, string> Definitions(Input input)
    {
        var root = Xml(input);
        var definitions = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var test in root.Element(Trx + "TestDefinitions")?.Elements(Trx + "UnitTest") ?? [])
        {
            var method = test.Element(Trx + "TestMethod") ?? throw new InvalidDataException("Missing test method.");
            var name = Attribute(method, "className") + "." + Attribute(method, "name");
            var categories = (test.Element(Trx + "TestCategory")?.Elements(Trx + "TestCategoryItem") ?? [])
                .Select(category => Attribute(category, "TestCategory")).Order(StringComparer.Ordinal).ToArray();
            Require(definitions.TryAdd(name, JsonSerializer.Serialize(new
            {
                Method = Attribute(method, "name"), Type = Attribute(method, "className"),
                Adapter = Attribute(method, "adapterTypeName"), CodeBase = Path.GetFileName(Attribute(method, "codeBase")), Categories = categories
            })), "Duplicate test definition.");
        }
        return definitions;
    }

    private static void VerifyAddedMethods(PEReader pe, MetadataReader pdb)
    {
        var metadata = pe.GetMetadataReader();
        var types = metadata.TypeDefinitions.Where(handle =>
        {
            var type = metadata.GetTypeDefinition(handle);
            return metadata.GetString(type.Namespace) + "." + metadata.GetString(type.Name) == AddedClass;
        }).ToArray();
        Require(types.Length == 1, "Added compiled test class differs.");
        var methods = metadata.GetTypeDefinition(types[0]).GetMethods().ToArray();
        foreach (var name in AddedMethods)
        {
            var matching = methods.Where(handle => metadata.GetString(metadata.GetMethodDefinition(handle).Name) == name).ToArray();
            Require(matching.Length == 1, "Added compiled test method differs.");
            var method = metadata.GetMethodDefinition(matching[0]);
            Require((method.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public &&
                (method.Attributes & MethodAttributes.Static) == 0 && method.RelativeVirtualAddress > 0 &&
                metadata.GetBlobBytes(method.Signature).AsSpan().SequenceEqual(new byte[] { 0x20, 0x00, 0x01 }),
                "Added test is not a compiled public instance void method without parameters.");
            var info = pdb.GetMethodDebugInformation(matching[0].ToDebugInformationHandle());
            var visible = info.GetSequencePoints().Where(point => !point.IsHidden).ToArray();
            Require(visible.Length > 0 && visible.All(point =>
            {
                var handle = point.Document.IsNil ? info.Document : point.Document;
                return !handle.IsNil && pdb.GetString(pdb.GetDocument(handle).Name) == "/_/" + AddedSource;
            }), "Added compiled test method does not bind the new PDB source document.");
        }
    }

    private static JsonElement[] Observations(string stdout, string prefix) => stdout.Split('\n')
        .Select(line => line.TrimEnd('\r')).Where(line => line.StartsWith(prefix, StringComparison.Ordinal)).Select(line =>
        {
            using var document = JsonDocument.Parse(line[prefix.Length..]);
            RejectDuplicateProperties(document.RootElement);
            return document.RootElement.Clone();
        }).ToArray();

    private static string[] Strings(JsonElement value) => value.EnumerateArray().Select(item => item.GetString()
        ?? throw new InvalidDataException("Null actual native string value.")).ToArray();

    private static void VerifyFocused()
    {
        const string oneSql = "SELECT compile_options FROM pragma_compile_options WHERE compile_options = @option;";
        const string twoSql = "SELECT compile_options, sqlite_version() AS sqlite_version FROM pragma_compile_options WHERE compile_options = @option;";
        const string oracleSql = "SELECT compile_options FROM pragma_compile_options WHERE compile_options = @option AND sqlite_version() = @version;";
        var input = TransitionInput("FocusedTrx");
        var expected = AddedMethods.Select(name => AddedClass + "." + name).ToHashSet(StringComparer.Ordinal);
        var results = Executions(input, focused: true);
        Require(results.Count == 5 && expected.SetEquals(results.Keys) && results.Values.All(outcome => outcome == "Passed"),
            "Focused suite must contain exactly the five passing native methods.");
        var root = Xml(input);
        foreach (var result in root.Element(Trx + "Results")!.Elements(Trx + "UnitTestResult"))
        {
            var name = Attribute(result, "testName");
            Require(AddedMethods.Contains(name, StringComparer.Ordinal) && result.Element(Trx + "Output")?.Element(Trx + "ErrorInfo") is null,
                "Focused name or successful result error information differs.");
            var stdout = result.Element(Trx + "Output")?.Element(Trx + "StdOut")?.Value ?? string.Empty;
            var identities = Observations(stdout, "Actual initialization provenance: ");
            var responses = Observations(stdout, "Actual initialization response: ");
            var acquisitions = Observations(stdout, "Actual initialization acquisition: ");
            var behaviors = Observations(stdout, "Actual initialization behavior: ");
            Require(identities.Length == 1 && responses.Length == 4, "Focused native identity or four actual responses are absent.");
            var identity = identities[0];
            Require(identity.GetProperty("Version").GetString() is { Length: > 0 } &&
                identity.GetProperty("Source").GetString() is { Length: > 0 } &&
                identity.GetProperty("Driver").GetString() is { } driver && driver.StartsWith("Microsoft.Data.Sqlite,", StringComparison.Ordinal) &&
                identity.GetProperty("Runtime").GetString() == "10.0.12", "Focused native provenance differs.");
            var byPurpose = responses.ToDictionary(value => value.GetProperty("Purpose").GetString()
                ?? throw new InvalidDataException("Missing native response purpose."), StringComparer.Ordinal);
            Require(byPurpose.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(new[]
            {
                "one-column argument capture", "two-column argument capture", "one-parameter independent oracle", "two-parameter independent oracle"
            }), "Focused native response purposes differ.");
            var one = Strings(byPurpose["one-column argument capture"].GetProperty("Rows")[0]);
            var two = Strings(byPurpose["two-column argument capture"].GetProperty("Rows")[0]);
            Require(one.Length == 1 && two.Length == 2 && one.Concat(two).All(value => value.Length > 0) &&
                one[0] == two[0] && two[1] == identity.GetProperty("Version").GetString(), "Actual acquired native arguments differ.");
            foreach (var pair in byPurpose)
            {
                var value = pair.Value;
                var twoFields = pair.Key == "two-column argument capture";
                var count = twoFields ? 2 : 1;
                var columns = value.GetProperty("Columns").EnumerateArray().ToArray();
                var rows = value.GetProperty("Rows").EnumerateArray().ToArray();
                Require(value.GetProperty("FieldCount").GetInt32() == count && columns.Length == count &&
                    columns.Select(column => column.GetProperty("Name").GetString()).SequenceEqual(twoFields
                        ? new[] { "compile_options", "sqlite_version" } : new[] { "compile_options" }) &&
                    columns.All(column => column.GetProperty("Type").GetString() == "System.String" &&
                        column.GetProperty("DataType").GetString() == "TEXT") &&
                    rows.Length == 1 && Strings(rows[0]).SequenceEqual(twoFields ? two : one) &&
                    value.GetProperty("CopiedCounts").EnumerateArray().Select(item => item.GetInt32()).SequenceEqual(new[] { count }),
                    "Native schema, complete GetValues response or independent oracle differs.");
                var secondParameter = pair.Key == "two-parameter independent oracle";
                Require(value.GetProperty("Sql").GetString() == (twoFields ? twoSql : secondParameter ? oracleSql : oneSql),
                    "Focused native SQL differs.");
                var parameters = value.GetProperty("Parameters").EnumerateArray().ToArray();
                Require(parameters.Length == (secondParameter ? 2 : 1) &&
                    parameters[0].GetProperty("ParameterName").GetString() == "@option" &&
                    parameters[0].GetProperty("Value").GetString() == one[0] &&
                    (!secondParameter || (parameters[1].GetProperty("ParameterName").GetString() == "@version" &&
                        parameters[1].GetProperty("Value").GetString() == two[1])), "Ordered native parameters differ.");
            }
            if (name == AddedMethods[0])
            {
                Require(acquisitions.Length == 1 && behaviors.Length == 0, "Acquisition-only observation scope differs.");
                var value = acquisitions[0];
                Require(Strings(value.GetProperty("OneColumnArguments")).SequenceEqual(one) &&
                    Strings(value.GetProperty("TwoColumnArguments")).SequenceEqual(two) &&
                    Strings(value.GetProperty("OneParameterOracle")).SequenceEqual(one) &&
                    Strings(value.GetProperty("TwoParameterOracle")).SequenceEqual(one) && value.GetProperty("OracleMatches").GetBoolean() &&
                    !value.GetProperty("ToolkitInitializationExecuted").GetBoolean() && !value.GetProperty("InitializationOwnershipVerified").GetBoolean(),
                    "Acquisition-only values or scope differ.");
                var configured = value.GetProperty("ConfiguredParameters").EnumerateArray().ToArray();
                Require(configured.Length == 2 && configured.Select(item => item.GetProperty("Name").GetString())
                    .SequenceEqual(new[] { "@option", "@version" }) && configured.All(item =>
                        item.GetProperty("Type").GetString() == "System.String" && item.GetProperty("QueryType").GetString() is { Length: > 0 }),
                    "Acquired parameter declarations differ.");
                continue;
            }
            Require(acquisitions.Length == 0 && behaviors.Length == 1, "Behavioral observation scope differs.");
            var behavior = behaviors[0];
            Require(behavior.GetProperty("CreatedCommands").GetInt32() == 1 && behavior.GetProperty("DisposedBeforeCaller").GetInt32() == 1,
                "Actual command creation or disposal-before-caller differs.");
            if (name is "BorrowedEagerInitializationFailureDisposesCommand" or "OwnedEagerInitializationFailureDisposesCommandBeforeConnectionClose")
            {
                var borrowed = name == AddedMethods[1];
                Require(behavior.GetProperty("Case").GetString() == (borrowed ? "BorrowedFailure" : "OwnedFailure") &&
                    behavior.GetProperty("Sql").GetString() == oracleSql && Strings(behavior.GetProperty("Arguments")).SequenceEqual(one) &&
                    behavior.GetProperty("FailureType").GetString() == "System.IndexOutOfRangeException" &&
                    behavior.GetProperty("OriginalExceptionPreserved").GetBoolean() && behavior.GetProperty("InitialParameterCount").GetInt32() == 1 &&
                    behavior.GetProperty("FirstParameterName").GetString() == "@option" && behavior.GetProperty("FirstParameterValue").GetString() == one[0] &&
                    behavior.GetProperty("ReaderAttempts").GetInt32() == 0 &&
                    behavior.GetProperty("ConnectionState").GetString() == (borrowed ? "Open" : "Closed") &&
                    (borrowed ? behavior.GetProperty("DisposedBeforeConnectionClose").ValueKind == JsonValueKind.Null :
                        behavior.GetProperty("DisposedBeforeConnectionClose").GetInt32() == 1), "Focused original initialization failure/resource guards differ.");
            }
            else
            {
                var complete = name == AddedMethods[3];
                Require(behavior.GetProperty("Case").GetString() == (complete ? "CompleteArguments" : "SingleArgument") &&
                    behavior.GetProperty("Sql").GetString() == (complete ? oracleSql : oneSql) &&
                    Strings(behavior.GetProperty("Arguments")).SequenceEqual(complete ? two : one) &&
                    Strings(behavior.GetProperty("Rows")).SequenceEqual(one) && Strings(behavior.GetProperty("ExpectedRows")).SequenceEqual(one) &&
                    behavior.GetProperty("ReaderAttempts").GetInt32() == 1 && behavior.GetProperty("ConnectionState").GetString() == "Open" &&
                    behavior.GetProperty("DisposedBeforeConnectionClose").ValueKind == JsonValueKind.Null, "Focused eager success/native oracle guards differ.");
            }
        }
    }

    private static void VerifyTransition(Measurement baseline, Measurement current)
    {
        Require(baseline.Descriptor.RepositoryRoot == current.Descriptor.RepositoryRoot, "Repository identity changed.");
        Require(!baseline.Descriptor.TestAssembly.Dll.Sha256.Equals(current.Descriptor.TestAssembly.Dll.Sha256, StringComparison.OrdinalIgnoreCase) &&
            !baseline.Descriptor.TestAssembly.Pdb.Sha256.Equals(current.Descriptor.TestAssembly.Pdb.Sha256, StringComparison.OrdinalIgnoreCase),
            "Expected Test DLL/PDB transition is absent.");
        foreach (var module in baseline.Documents)
        {
            var after = current.Documents[module.Key];
            Require(module.Value.All(pair => after.TryGetValue(pair.Key, out var hash) &&
                (module.Key == "IQToolkit.Data" && pair.Key == DataSource
                    ? pair.Value.Equals(OldDataHash, StringComparison.OrdinalIgnoreCase) && hash.Equals(NewDataHash, StringComparison.OrdinalIgnoreCase)
                    : hash == pair.Value)), "Inherited PDB source/checksum changed beyond the exact Data document.");
            var additions = after.Keys.Except(module.Value.Keys).ToArray();
            Require(module.Key == "IQToolkitTest" ? additions.SequenceEqual(new[] { AddedSource }) : additions.Length == 0,
                "PDB source growth is not exactly the one Test document.");
        }
        Require(current.Documents["IQToolkitTest"][AddedSource].Equals(TransitionInput("TestSource").Sha256, StringComparison.OrdinalIgnoreCase),
            "Added source does not match the compiled PDB document checksum.");
        Require(UsedBindings.SetEquals(Bindings.Keys), "Baseline alias was unused or unqualified.");
        var before = Executions(baseline.Descriptor.StrictTrx);
        var afterTests = Executions(current.Descriptor.StrictTrx);
        var added = AddedMethods.Select(name => AddedClass + "." + name).ToHashSet(StringComparer.Ordinal);
        Require(before.Count == 419 && afterTests.Count == 424 &&
            before.All(pair => afterTests.TryGetValue(pair.Key, out var outcome) && outcome == pair.Value) &&
            added.SetEquals(afterTests.Keys.Except(before.Keys)) && added.All(name => afterTests[name] == "Passed"),
            "Strict test growth or inherited outcomes differ.");
        var oldDefinitions = Definitions(baseline.Descriptor.StrictTrx);
        var newDefinitions = Definitions(current.Descriptor.StrictTrx);
        Require(oldDefinitions.Count == 419 && newDefinitions.Count == 424 &&
            oldDefinitions.All(pair => newDefinitions.TryGetValue(pair.Key, out var value) && value == pair.Value),
            "Inherited strict definitions/categories differ.");
        var collectedDefinitions = Definitions(current.Descriptor.CoverageTrx);
        Require(collectedDefinitions.Count == newDefinitions.Count && newDefinitions.All(pair =>
            collectedDefinitions.TryGetValue(pair.Key, out var value) && value == pair.Value), "Collector definitions/categories differ from strict.");
        VerifyFocused();
        var focusedDefinitions = Definitions(TransitionInput("FocusedTrx"));
        Require(focusedDefinitions.Count == 5 && added.All(name => focusedDefinitions.TryGetValue(name, out var value) && value == newDefinitions[name]),
            "Focused definitions differ from the strict added methods.");
        transitionReceipt = new
        {
            Status = "verified", Qualification = "Private GetCommand and one-Test-compilation transition only",
            BaselineDescriptor = TransitionInput("BaselineDescriptor"), CurrentDescriptor = TransitionInput("CurrentDescriptor"),
            BaselineBindings = Bindings, TestSource = TransitionInput("TestSource"), FocusedTrx = TransitionInput("FocusedTrx"),
            DataSource = TransitionInput("DataSource"), OldDataSource = TransitionInput("OldDataSource"), HistoricalChain = historicalChain,
            OldTests = before.Count, CurrentTests = afterTests.Count, AddedTests = AddedMethods,
            AddedPdbDocument = AddedSource, SourceFiles = current.Sources.Count, ComparedLeaves = current.Leaves.Count,
            ComparedInheritedLeaves = baseline.Leaves.Count, AddedLeafCount = current.Leaves.Count - baseline.Leaves.Count,
            SourceLineMapping = "DbEntityProvider.cs: <=631 unchanged; 632..636 +2; >=637 +8",
            MeasuredModules = current.Modules, current.Total,
            IndividualBranchArmsVerified = false, CoverageTargetsVerified = false,
            FullResourceOwnershipVerified = false, DurableFinalProfileAccepted = false, FullGoalComplete = false
        };
    }
}
