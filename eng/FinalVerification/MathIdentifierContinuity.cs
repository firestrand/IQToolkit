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

internal static class MathIdentifierContinuity
{
    private const long MaximumBytes = 16 * 1024 * 1024;
    private static readonly XNamespace Trx = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
    private static readonly Guid Sha256Algorithm = new("8829d00f-11b8-4213-878b-770e8597ac16");
    private static bool baselineContext;
    private static readonly Dictionary<string, Input> Bindings = new(StringComparer.Ordinal);
    private static readonly HashSet<string> UsedBindings = new(StringComparer.Ordinal);
    private static JsonElement transition;
    private static object? transitionReceipt;
    private const string AddedSource = "IQToolkitTest/RealProvider/NativeMathIdentifierCultureTests.cs";
    private const string AddedClass = "IQToolkitTest.RealProvider.NativeMathIdentifierCultureTests";
    private const string SQLiteSource = "IQToolkit.Data.SQLite/SQLiteFormatter.cs";
    private const string OldSQLiteHash = "aea8ac3cadb0b1836e04293e120df48cf06a2e8647212225c02b2181de7efa90";
    private const string NewSQLiteHash = "7c694080670dd15ed8ba0327b92516f32619893a91b878b523ec7810de84a1c0";
    private const string TestSourceHash = "c93ac9671eeaedf6d972078fa9b8f1dbf7285f358667987a59d58246e4286a49";
    private static Input[] historicalChain = [];
    private static readonly string[] AddedMethods =
    [
        "NativeSQLiteMathSinInputsAreAcquired",
        "SQLiteMathSinPreservesNativeResultInInvariantCulture",
        "SQLiteMathSinPreservesNativeResultInTurkishCulture"
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

    // The final coordinator owns capture bounds and terminal publication.
    internal static object Validate(string baseline, string current, string manifest)
    {
        ReadTransition([baseline, current, string.Empty, manifest]);
        baselineContext = true;
        var before = Qualify(ReadDescriptor(baseline));
        baselineContext = false;
        var after = Qualify(ReadDescriptor(current));
        Compare(before, after);
        VerifyTransition(before, after);
        return transitionReceipt ?? throw new InvalidDataException("Missing math identifier transition receipt.");
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
        Program.CaptureHistoricalRecent(Resolve(path), expected);

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
            var matching = current.Descriptor.Modules.Single(item => item.Name == module.Name);
            var sameDll = module.Dll.Sha256.Equals(matching.Dll.Sha256, StringComparison.OrdinalIgnoreCase);
            var samePdb = module.Pdb.Sha256.Equals(matching.Pdb.Sha256, StringComparison.OrdinalIgnoreCase);
            Require(module.Name == "IQToolkit.Data.SQLite" ? !sameDll && !samePdb : sameDll && samePdb,
                "Compiled module transition exceeds the exact SQLite DLL/PDB pair.");
        }
        Require(baseline.Sources.Count == 97 && current.Sources.Count == 97 &&
            baseline.Sources.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(current.Sources.Keys) &&
            baseline.Sources.Where(pair => pair.Key != SQLiteSource).All(pair => current.Sources[pair.Key] == pair.Value) &&
            baseline.Sources[SQLiteSource].Equals(OldSQLiteHash, StringComparison.OrdinalIgnoreCase) &&
            current.Sources[SQLiteSource].Equals(NewSQLiteHash, StringComparison.OrdinalIgnoreCase),
            "Measured source transition is not exactly the pinned SQLite formatter edit and 96 unchanged sources.");
        Require(baseline.Leaves.Count == 16724 && current.Leaves.Count == 16724 &&
            baseline.Leaves.Keys.ToHashSet().SetEquals(current.Leaves.Keys), "Coverage leaf identities changed.");
        foreach (var pair in baseline.Leaves)
        {
            var leaf = current.Leaves[pair.Key];
            Require(leaf.Branches == pair.Value.Branches, "Coverage leaf denominator changed.");
            Require((!pair.Value.Covered || leaf.Covered) && leaf.CoveredBranches >= pair.Value.CoveredBranches, "Coverage leaf regressed.");
            Require(pair.Value.Conditions.Length == leaf.Conditions.Length, "Branch condition structure changed.");
            for (var index = 0; index < pair.Value.Conditions.Length; index++)
            {
                var before = pair.Value.Conditions[index];
                var after = leaf.Conditions[index];
                Require(before.Number == after.Number && before.Type == after.Type && after.Percentage >= before.Percentage,
                    "Branch condition structure changed or reported coverage regressed.");
            }
        }
        foreach (var module in baseline.Modules)
        {
            var after = current.Modules[module.Key];
            Require(after.Lines == module.Value.Lines && after.Branches == module.Value.Branches &&
                after.CoveredLines >= module.Value.CoveredLines && after.CoveredBranches >= module.Value.CoveredBranches,
                "Measured module denominator changed or aggregate coverage regressed.");
        }
        foreach (var key in TargetLeaves())
            Require(baseline.Leaves.TryGetValue(key, out var before) && !before.Covered && before.Branches == 0 &&
                before.CoveredBranches == 0 && before.Conditions.Length == 0 && current.Leaves[key] is
                { Covered: true, Branches: 0, CoveredBranches: 0 } && current.Leaves[key].Conditions.Length == 0,
                "The exact formerly uncovered SQLite Math-name class/method leaves must become covered.");
    }

    private static Key[] TargetLeaves() =>
    [
        new("IQToolkit.Data.SQLite", SQLiteSource, "IQToolkit.Data.SQLite.SQLiteFormatter", string.Empty, string.Empty, 371),
        new("IQToolkit.Data.SQLite", SQLiteSource, "IQToolkit.Data.SQLite.SQLiteFormatter", "VisitMethodCall", "(System.Linq.Expressions.MethodCallExpression)", 371)
    ];

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
            new[] { "BaselineBindings", "BaselineDescriptor", "CurrentDescriptor", "FocusedTrx", "OldSQLiteSource", "SQLiteSource", "Schema", "TestSource" }) &&
            transition.GetProperty("Schema").GetInt32() == 1, "Unexpected transition schema or fields.");
        var baselineInput = TransitionInput("BaselineDescriptor");
        var currentInput = TransitionInput("CurrentDescriptor");
        Require(Path.GetFullPath(args[0]) == baselineInput.Path && Path.GetFullPath(args[1]) == currentInput.Path &&
            baselineInput.Sha256.Equals("d70a5e3734b25302f7d700122c178f7604d084a819ed202fb9c8a824621989f8",
                StringComparison.OrdinalIgnoreCase), "Descriptor origins or qualified V1aa baseline hash differ.");
        using var document = JsonDocument.Parse(File.ReadAllBytes(Capture(baselineInput)));
        RejectDuplicateProperties(document.RootElement);
        var descriptor = document.Deserialize<Descriptor>() ?? throw new InvalidDataException("Missing baseline descriptor.");
        var root = Path.GetFullPath(descriptor.RepositoryRoot);
        Require(baselineInput.Path == Path.Combine(root, "artifacts", "command-initialization", "coverage.gw5awfx_", "collection", "descriptor.json"),
            "Baseline descriptor is not the fixed qualified V1aa origin.");
        Capture(currentInput);
        var source = TransitionInput("TestSource");
        Require(source.Path == Path.Combine(root, AddedSource) && source.Sha256.Equals(TestSourceHash, StringComparison.OrdinalIgnoreCase),
            "Added source origin or reviewed behavioral source hash differs.");
        Capture(source);
        var focused = TransitionInput("FocusedTrx");
        Require(focused.Path == Path.Combine(root, "artifacts", "sqlite-math-culture", "green.1_odmlyu", "results", "behavior.trx") &&
            focused.Sha256.Equals("1da07cd8af1864bb6c0973246fa51dcc205dad6609387c2292c2b36ee9996ea2", StringComparison.OrdinalIgnoreCase),
            "Focused TRX is not the authentic GREEN behavioral origin.");
        Capture(focused);
        var sqlite = TransitionInput("SQLiteSource");
        var oldSQLite = TransitionInput("OldSQLiteSource");
        Require(sqlite.Path == Path.Combine(root, SQLiteSource) && sqlite.Sha256.Equals(NewSQLiteHash, StringComparison.OrdinalIgnoreCase) &&
            oldSQLite.Path == Path.Combine(root, "artifacts", "sqlite-math-culture", "red.b18i41v7", "reviewed-inputs", "0024-SQLiteFormatter.cs.snapshot") &&
            oldSQLite.Sha256.Equals(OldSQLiteHash, StringComparison.OrdinalIgnoreCase), "Reviewed SQLite source transition differs.");
        VerifySourceTransformation(oldSQLite, sqlite);
        VerifyHistoricalChain(root, baselineInput);
        foreach (var property in transition.GetProperty("BaselineBindings").EnumerateObject())
        {
            Require(Path.IsPathFullyQualified(property.Name) && Path.GetFullPath(property.Name) == property.Name &&
                Bindings.TryAdd(property.Name, ReadInput(property.Value)), "Invalid or duplicate baseline binding.");
        }
        var required = new HashSet<string>(StringComparer.Ordinal);
        var oldModule = descriptor.Modules.Single(module => module.Name == "IQToolkit.Data.SQLite");
        foreach (var module in new[] { oldModule, descriptor.TestAssembly })
        {
            var acquisition = module.Name == "IQToolkit.Data.SQLite" ? "green.1_odmlyu" : "acquire.meb_1d_2";
            foreach (var binary in new[] { module.Dll, module.Pdb })
            {
                var logical = Path.GetFullPath(binary.Path);
                required.Add(logical);
                Require(logical == Path.Combine(root, "IQToolkitTest", "bin", "Release", "net10.0", Path.GetFileName(logical)) &&
                    Bindings.TryGetValue(logical, out var physical) && physical.Path == Path.Combine(root, "artifacts", "sqlite-math-culture",
                        acquisition, "previous-binaries", "IQToolkitTest", "bin", "Release", "net10.0", Path.GetFileName(logical)) &&
                    physical.Sha256.Equals(binary.Sha256, StringComparison.OrdinalIgnoreCase), "Old compiled binary binding differs.");
                Capture(Bindings[logical]);
            }
        }
        required.Add(sqlite.Path);
        Require(Bindings.TryGetValue(sqlite.Path, out var oldSource) && oldSource.Path == oldSQLite.Path &&
            oldSource.Sha256.Equals(oldSQLite.Sha256, StringComparison.OrdinalIgnoreCase) &&
            Bindings.Count == 5 && required.SetEquals(Bindings.Keys), "Expected exactly five reviewed historical aliases.");
    }

    private static void VerifySourceTransformation(Input before, Input after)
    {
        const string original = "this.Write(m.Method.Name.ToUpper());";
        const string replacement = "this.Write(m.Method.Name.ToUpperInvariant());";
        var oldBytes = File.ReadAllBytes(Capture(before));
        var originalBytes = Encoding.UTF8.GetBytes(original);
        var replacementBytes = Encoding.UTF8.GetBytes(replacement);
        var index = oldBytes.AsSpan().IndexOf(originalBytes);
        Require(index >= 0 && oldBytes.AsSpan(index + originalBytes.Length).IndexOf(originalBytes) < 0 &&
            oldBytes.Take(index).Count(character => character == (byte)'\n') == 370,
            "Original SQLite Math-name casing is not the sole pinned statement at line371.");
        var expected = new byte[oldBytes.Length - originalBytes.Length + replacementBytes.Length];
        oldBytes.AsSpan(0, index).CopyTo(expected);
        replacementBytes.CopyTo(expected, index);
        oldBytes.AsSpan(index + originalBytes.Length).CopyTo(expected.AsSpan(index + replacementBytes.Length));
        Require(expected.AsSpan().SequenceEqual(File.ReadAllBytes(Capture(after))),
            "SQLite edit exceeds the exact ToUpper-to-ToUpperInvariant byte transformation.");
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
            new(Path.Combine(root, "artifacts/native-null-array/coverage.yrni_8j2/retention/inputs-after.json"), "1e3a44f4972a229ef12280b7324cd8fbf3f24b26a8c2bac6cc13a47d0a155362"),
            new(Path.Combine(root, "artifacts/command-initialization/coverage.gw5awfx_/collection/descriptor.json"), "d70a5e3734b25302f7d700122c178f7604d084a819ed202fb9c8a824621989f8"),
            new(Path.Combine(root, "artifacts/command-initialization/coverage-qualification._c6y3y6f/transition.json"), "b9691f9b2c9acf77c41b1d9c0efb07d1ffc9cb6a54e4dfd5005442bb4fa2202e"),
            new(Path.Combine(root, "artifacts/command-initialization/coverage-qualification._c6y3y6f/retention/transition.json"), "474402be9e5645471395d88895284afd6be4b2d42f87eb1294975d7bb0bcffe8"),
            new(Path.Combine(root, "artifacts/command-initialization/coverage-qualification._c6y3y6f/retention/inputs-before.json"), "9eae976161a59ba716ff549e5a1c82fadb67a86249595632b8828dc09eeda56a"),
            new(Path.Combine(root, "artifacts/command-initialization/coverage-qualification._c6y3y6f/retention/inputs-after.json"), "aa585a7f7e56c03c63c0639208941001ebaeaf568c6f03e277d03969f5c8fd88"),
            new(Path.Combine(root, "artifacts/command-initialization/coverage-qualification._c6y3y6f/result.json"), "e61396b3efd6614d094a1e60347e178b9234da41ecd26a5cb64638fdd8e50985")
        ];
        foreach (var input in historicalChain) Capture(input);
        using var first = JsonDocument.Parse(File.ReadAllBytes(Capture(historicalChain[2])));
        using var second = JsonDocument.Parse(File.ReadAllBytes(Capture(historicalChain[6])));
        using var third = JsonDocument.Parse(File.ReadAllBytes(Capture(historicalChain[11])));
        using var thirdManifest = JsonDocument.Parse(File.ReadAllBytes(Capture(historicalChain[10])));
        RejectDuplicateProperties(first.RootElement); RejectDuplicateProperties(second.RootElement);
        RejectDuplicateProperties(third.RootElement); RejectDuplicateProperties(thirdManifest.RootElement);
        bool Same(Input left, Input right) => left.Path == right.Path && left.Sha256.Equals(right.Sha256, StringComparison.OrdinalIgnoreCase);
        Require(first.RootElement.GetProperty("Status").GetString() == "verified" &&
            Same(ReadInput(first.RootElement.GetProperty("Baseline")), historicalChain[0]) &&
            Same(ReadInput(first.RootElement.GetProperty("Current")), historicalChain[1]) &&
            second.RootElement.GetProperty("Status").GetString() == "verified" &&
            Same(ReadInput(second.RootElement.GetProperty("BaselineDescriptor")), historicalChain[1]) &&
            Same(ReadInput(second.RootElement.GetProperty("CurrentDescriptor")), ReadInput(third.RootElement.GetProperty("BaselineDescriptor"))) &&
            ReadInput(third.RootElement.GetProperty("BaselineDescriptor")).Path == Path.Combine(root, "artifacts/native-null-array/coverage.yrni_8j2/collection/descriptor.json") &&
            ReadInput(third.RootElement.GetProperty("BaselineDescriptor")).Sha256.Equals("77495a1bcb8b29eb57deec221325b1305fdf2f728515cb885198dc82df3bd64c", StringComparison.OrdinalIgnoreCase) &&
            third.RootElement.GetProperty("Status").GetString() == "verified" &&
            Same(ReadInput(third.RootElement.GetProperty("CurrentDescriptor")), baseline) && Same(baseline, historicalChain[9]) &&
            Same(ReadInput(thirdManifest.RootElement.GetProperty("BaselineDescriptor")), ReadInput(third.RootElement.GetProperty("BaselineDescriptor"))) &&
            Same(ReadInput(thirdManifest.RootElement.GetProperty("CurrentDescriptor")), baseline),
            "Permanent original-to-V1y-to-V1z-to-V1aa receipt chain differs.");
        VerifyHistoricalInputs(historicalChain[3], historicalChain[4], 198);
        VerifyHistoricalInputs(historicalChain[7], historicalChain[8], 188);
        VerifyHistoricalInputs(historicalChain[12], historicalChain[13], 587);
        using var native = JsonDocument.Parse(File.ReadAllBytes(Capture(historicalChain[14])));
        RejectDuplicateProperties(native.RootElement);
        var result = native.RootElement;
        Require(result.GetProperty("Status").GetString() == "passed" && !result.GetProperty("Cancelled").GetBoolean() &&
            result.GetProperty("CleanupErrors").GetArrayLength() == 0 && result.GetProperty("StorageErrors").GetArrayLength() == 0 &&
            result.GetProperty("RootSourcesUnchanged").GetBoolean() && result.GetProperty("ReviewedInputsUnchanged").GetBoolean() &&
            result.GetProperty("RootIndexUnchanged").GetBoolean() && result.GetProperty("OrdinaryBinariesUnchanged").GetInt32() == 336,
            "Qualified V1aa native producer did not preserve its inputs.");
        var commands = result.GetProperty("Commands").EnumerateArray().ToArray();
        Require(commands.Length == 4 && commands.All(command => command.GetProperty("Exit").GetInt32() == 0 &&
            command.GetProperty("ExpectedExit").GetInt32() == 0 && command.GetProperty("RemainingLiveMembers").GetArrayLength() == 0 &&
            command.GetProperty("SecondaryErrors").GetArrayLength() == 0 &&
            (!command.TryGetProperty("ForcedTermination", out var forced) || !forced.GetBoolean())), "Qualified V1aa native command failed or left processes.");
        var compare = commands.Single(command => command.GetProperty("Name").GetString() == "compare");
        var argv = compare.GetProperty("Argv").EnumerateArray().Select(value => value.GetString()).ToArray();
        Require(argv.Length == 6 && argv[2] == ReadInput(third.RootElement.GetProperty("BaselineDescriptor")).Path &&
            argv[3] == baseline.Path && argv[4] == Path.GetDirectoryName(historicalChain[11].Path) && argv[5] == historicalChain[10].Path,
            "Qualified V1aa native comparison does not bind the recorded chain.");
        var log = Path.Combine(Path.GetDirectoryName(historicalChain[14].Path)!, "compare.log");
        using var observed = JsonDocument.Parse(File.ReadAllBytes(Capture(log, compare.GetProperty("LogSha256").GetString())));
        RejectDuplicateProperties(observed.RootElement);
        Require(observed.RootElement.GetProperty("Status").GetString() == "verified" &&
            observed.RootElement.GetProperty("SourceFiles").GetInt32() == third.RootElement.GetProperty("SourceFiles").GetInt32() &&
            observed.RootElement.GetProperty("ComparedLeaves").GetInt32() == third.RootElement.GetProperty("ComparedLeaves").GetInt32() &&
            JsonElement.DeepEquals(observed.RootElement.GetProperty("Total"), third.RootElement.GetProperty("Total")),
            "Qualified V1aa native comparison log differs from its executed receipt.");
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

    private static void VerifyNativeResponse(JsonElement response, int fields, bool numeric)
    {
        Require(response.GetProperty("FieldCount").GetInt32() == fields && response.GetProperty("Columns").GetArrayLength() == fields,
            "Actual native response column count differs.");
        var rows = response.GetProperty("Rows").EnumerateArray().ToArray();
        var copies = response.GetProperty("CopiedCounts").EnumerateArray().ToArray();
        var bits = response.GetProperty("DoubleBits").EnumerateArray().ToArray();
        Require(rows.Length > 0 && copies.Length == rows.Length && bits.Length == rows.Length &&
            rows.Select((row, index) => row.GetArrayLength() == fields && bits[index].GetArrayLength() == fields && copies[index].GetInt32() == fields).All(value => value),
            "Actual native rows, GetValues counts or bit observations differ.");
        if (numeric)
        {
            Require(rows.Length == 1 && fields == 1, "Numeric native response is not exactly one row and field.");
            var value = rows[0][0].GetDouble();
            var column = response.GetProperty("Columns")[0];
            Require(double.IsFinite(value) && column.GetProperty("Type").GetString() == "System.Double" &&
                column.GetProperty("DataType").GetString() == "REAL" && bits[0][0].GetInt64() == BitConverter.DoubleToInt64Bits(value),
                "Actual native numeric schema, finite value or bits differ.");
        }
    }

    private static void VerifyNativeNumeric(JsonElement summary, JsonElement response)
    {
        VerifyNativeResponse(response, 1, numeric: true);
        var column = response.GetProperty("Columns")[0];
        var value = response.GetProperty("Rows")[0][0].GetDouble();
        Require(summary.GetProperty("FieldCount").GetInt32() == 1 &&
            summary.GetProperty("Name").GetString() == column.GetProperty("Name").GetString() &&
            summary.GetProperty("FieldType").GetString() == column.GetProperty("Type").GetString() &&
            summary.GetProperty("DataType").GetString() == column.GetProperty("DataType").GetString() &&
            BitConverter.DoubleToInt64Bits(summary.GetProperty("Value").GetDouble()) == BitConverter.DoubleToInt64Bits(value) &&
            summary.GetProperty("Bits").GetInt64() == BitConverter.DoubleToInt64Bits(value),
            "Native numeric summary does not bind its complete response/schema/value/bits.");
    }

    private static void VerifyFocused()
    {
        var input = TransitionInput("FocusedTrx");
        var expected = AddedMethods.Select(name => AddedClass + "." + name).ToHashSet(StringComparer.Ordinal);
        var results = Executions(input, focused: true);
        Require(results.Count == 3 && expected.SetEquals(results.Keys) && results.Values.All(outcome => outcome == "Passed"),
            "Focused suite must contain exactly the three passing native methods.");
        var root = Xml(input);
        JsonElement? sharedActual = null;
        JsonElement? sharedOracle = null;
        JsonElement? sharedProvenance = null;
        foreach (var result in root.Element(Trx + "Results")!.Elements(Trx + "UnitTestResult"))
        {
            var name = Attribute(result, "testName");
            Require(AddedMethods.Contains(name, StringComparer.Ordinal) && result.Element(Trx + "Output")?.Element(Trx + "ErrorInfo") is null,
                "Focused native method or error fields differ.");
            var stdout = result.Element(Trx + "Output")?.Element(Trx + "StdOut")?.Value ?? throw new InvalidDataException("Missing native stdout.");
            var responseRecords = Observations(stdout, "Actual math identifier response: ");
            var provenanceRecords = Observations(stdout, "Actual math identifier provenance: ");
            var cultureRecords = Observations(stdout, "Actual math identifier culture: ");
            var acquisitions = Observations(stdout, "Actual math identifier acquisition: ");
            var requests = Observations(stdout, "Actual math identifier request: ");
            var resultRecords = Observations(stdout, "Actual math identifier result: ");
            var restorations = Observations(stdout, "Actual math identifier restoration: ");
            var failures = Observations(stdout, "Actual math identifier failure: ");
            var acquisitionOnly = name == AddedMethods[0];
            Require(provenanceRecords.Length == 1 && cultureRecords.Length == 1 && acquisitions.Length == 1 && failures.Length == 0 &&
                responseRecords.Length == (acquisitionOnly ? 4 : 5) && requests.Length == (acquisitionOnly ? 0 : 1) &&
                resultRecords.Length == (acquisitionOnly ? 0 : 1) && restorations.Length == (acquisitionOnly ? 0 : 1) &&
                stdout.Split('\n').Count(line => line.StartsWith("Actual math identifier ", StringComparison.Ordinal)) == (acquisitionOnly ? 7 : 11),
                "Missing, duplicate or unexpected native observation records.");
            var responses = responseRecords.ToDictionary(value => value.GetProperty("Purpose").GetString()!, StringComparer.Ordinal);
            var expectedPurposes = new HashSet<string>(StringComparer.Ordinal)
            {
                "provenance", "native-double input", "native-sin function catalog", "independent-sin parameter oracle"
            };
            if (!acquisitionOnly) expectedPurposes.Add("formatted-sin native result");
            Require(expectedPurposes.SetEquals(responses.Keys), "Native request purposes differ.");
            var provenanceResponse = responses["provenance"];
            VerifyNativeResponse(provenanceResponse, 3, numeric: false);
            Require(provenanceResponse.GetProperty("Sql").GetString() == "SELECT sqlite_version(), sqlite_source_id(), substr(sqlite_source_id(),1,19);" &&
                provenanceResponse.GetProperty("Parameters").GetArrayLength() == 0 && provenanceResponse.GetProperty("Rows").GetArrayLength() == 1,
                "Native provenance request or row scope differs.");
            var provenanceColumns = provenanceResponse.GetProperty("Columns").EnumerateArray().ToArray();
            Require(provenanceColumns.Select(column => column.GetProperty("Name").GetString()).SequenceEqual(new[]
                { "sqlite_version()", "sqlite_source_id()", "substr(sqlite_source_id(),1,19)" }) &&
                provenanceColumns.All(column => column.GetProperty("Type").GetString() == "System.String" && column.GetProperty("DataType").GetString() == "TEXT") &&
                provenanceResponse.GetProperty("DoubleBits")[0].EnumerateArray().All(value => value.ValueKind == JsonValueKind.Null), "Native provenance schema differs.");
            var provenanceValues = Strings(provenanceResponse.GetProperty("Rows")[0]);
            var provenance = provenanceRecords[0];
            var timestamp = provenance.GetProperty("Timestamp").GetString()!;
            Require(provenance.GetProperty("Version").GetString() == provenanceValues[0] && !string.IsNullOrEmpty(provenanceValues[0]) &&
                provenance.GetProperty("SourceId").GetString() == provenanceValues[1] && timestamp == provenanceValues[2] &&
                provenanceValues[1].StartsWith(timestamp, StringComparison.Ordinal) &&
                DateTime.TryParseExact(timestamp, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) &&
                provenance.GetProperty("Driver").GetString() is { Length: > 0 } driver && driver.StartsWith("Microsoft.Data.Sqlite,", StringComparison.Ordinal) &&
                provenance.GetProperty("Runtime").GetString() is { Length: > 0 }, "Native provenance does not bind its response.");
            var actualResponse = responses["native-double input"];
            Require(actualResponse.GetProperty("Sql").GetString() == "SELECT julianday(substr(sqlite_source_id(),1,19));" &&
                actualResponse.GetProperty("Parameters").GetArrayLength() == 0 &&
                actualResponse.GetProperty("Columns")[0].GetProperty("Name").GetString() == "julianday(substr(sqlite_source_id(),1,19))", "Native Double request differs.");
            var acquisition = acquisitions[0];
            var actual = acquisition.GetProperty("Actual");
            var oracle = acquisition.GetProperty("ParameterOracle");
            VerifyNativeNumeric(actual, actualResponse);
            Require(Math.Truncate(actual.GetProperty("Value").GetDouble()) != actual.GetProperty("Value").GetDouble(), "Native input must retain its actual fractional value.");
            var functions = responses["native-sin function catalog"];
            VerifyNativeResponse(functions, 3, numeric: false);
            Require(functions.GetProperty("Sql").GetString() == "SELECT name, builtin, narg FROM pragma_function_list WHERE name = @function AND builtin = 1 AND narg = 1;" &&
                functions.GetProperty("Parameters").GetArrayLength() == 1 &&
                functions.GetProperty("Parameters")[0].GetProperty("ParameterName").GetString() == "@function" &&
                functions.GetProperty("Parameters")[0].GetProperty("Value").GetString() == "sin", "Native builtin sin catalog request differs.");
            var columns = functions.GetProperty("Columns").EnumerateArray().ToArray();
            Require(columns.Select(column => column.GetProperty("Name").GetString()).SequenceEqual(new[] { "name", "builtin", "narg" }) &&
                columns[0].GetProperty("Type").GetString() == "System.String" && columns[0].GetProperty("DataType").GetString() == "TEXT" &&
                columns.Skip(1).All(column => column.GetProperty("Type").GetString() == "System.Int64" && column.GetProperty("DataType").GetString() == "INTEGER") &&
                functions.GetProperty("Rows").EnumerateArray().All(row => row[0].GetString() == "sin" && row[1].GetInt64() == 1 && row[2].GetInt64() == 1) &&
                functions.GetProperty("DoubleBits").EnumerateArray().All(row => row.EnumerateArray().All(value => value.ValueKind == JsonValueKind.Null)),
                "Actual complete sin catalog rows or schema differ.");
            var functionName = functions.GetProperty("Rows")[0][0].GetString()!;
            var oracleResponse = responses["independent-sin parameter oracle"];
            Require(oracleResponse.GetProperty("Sql").GetString() == "SELECT sin(@value);" &&
                oracleResponse.GetProperty("Parameters").GetArrayLength() == 1 &&
                oracleResponse.GetProperty("Parameters")[0].GetProperty("ParameterName").GetString() == "@value" &&
                BitConverter.DoubleToInt64Bits(oracleResponse.GetProperty("Parameters")[0].GetProperty("Value").GetDouble()) == actual.GetProperty("Bits").GetInt64() &&
                oracleResponse.GetProperty("Columns")[0].GetProperty("Name").GetString() == "sin(@value)", "Independent native sine parameter or request differs.");
            VerifyNativeNumeric(oracle, oracleResponse);
            var culture = cultureRecords[0];
            Require(culture.GetProperty("Culture").GetString() == "tr-TR" && culture.GetProperty("FunctionName").GetString() == functionName &&
                culture.GetProperty("TurkishUpper").GetString() == functionName.ToUpper(CultureInfo.GetCultureInfo("tr-TR")) &&
                culture.GetProperty("TurkishUpper").GetString()!.Contains('\u0130') && culture.GetProperty("InvariantUpper").GetString() == functionName.ToUpperInvariant() &&
                !StringComparer.Ordinal.Equals(culture.GetProperty("TurkishUpper").GetString(), culture.GetProperty("InvariantUpper").GetString()) &&
                acquisition.GetProperty("Culture").GetString() == "tr-TR" && acquisition.GetProperty("FunctionName").GetString() == functionName &&
                acquisition.GetProperty("TurkishUpper").GetString() == culture.GetProperty("TurkishUpper").GetString() &&
                acquisition.GetProperty("InvariantUpper").GetString() == culture.GetProperty("InvariantUpper").GetString() &&
                !acquisition.GetProperty("FormatterExecuted").GetBoolean() && !acquisition.GetProperty("BehaviorVerified").GetBoolean(),
                "Installed culture or acquisition-only scope differs.");
            if (sharedActual is { } previousActual)
                Require(JsonElement.DeepEquals(previousActual, actual) && JsonElement.DeepEquals(sharedOracle!.Value, oracle) &&
                    JsonElement.DeepEquals(sharedProvenance!.Value, provenance), "Focused cases did not retain the same observed engine prerequisites.");
            else { sharedActual = actual.Clone(); sharedOracle = oracle.Clone(); sharedProvenance = provenance.Clone(); }
            if (acquisitionOnly) continue;
            var expectedCulture = name == AddedMethods[2] ? "tr-TR" : string.Empty;
            var request = requests[0];
            var expression = request.GetProperty("Expression").GetString()!;
            Require(request.GetProperty("Culture").GetString() == expectedCulture && request.GetProperty("Method").GetString() == "Double Sin(Double)" &&
                expression.StartsWith("SIN(", StringComparison.Ordinal) && expression.EndsWith(")", StringComparison.Ordinal) &&
                double.TryParse(expression[4..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var argument) &&
                BitConverter.DoubleToInt64Bits(argument) == actual.GetProperty("Bits").GetInt64() &&
                request.GetProperty("Sql").GetString() == "SELECT " + expression &&
                JsonElement.DeepEquals(request.GetProperty("Actual"), actual) && JsonElement.DeepEquals(request.GetProperty("ParameterOracle"), oracle),
                "Actual formatted native sine request does not preserve its unchanged acquired Double/oracle.");
            var formatted = responses["formatted-sin native result"];
            Require(formatted.GetProperty("Sql").GetString() == request.GetProperty("Sql").GetString() && formatted.GetProperty("Parameters").GetArrayLength() == 0 &&
                formatted.GetProperty("Columns")[0].GetProperty("Name").GetString() == expression, "Formatted native result request differs.");
            var observed = resultRecords[0];
            Require(observed.GetProperty("Culture").GetString() == expectedCulture && JsonElement.DeepEquals(observed.GetProperty("ParameterOracle"), oracle),
                "Formatted native result culture or oracle differs.");
            var nativeResult = observed.GetProperty("Actual");
            VerifyNativeNumeric(nativeResult, formatted);
            Require(nativeResult.GetProperty("FieldCount").GetInt32() == oracle.GetProperty("FieldCount").GetInt32() &&
                nativeResult.GetProperty("FieldType").GetString() == oracle.GetProperty("FieldType").GetString() &&
                nativeResult.GetProperty("DataType").GetString() == oracle.GetProperty("DataType").GetString() &&
                nativeResult.GetProperty("Bits").GetInt64() == oracle.GetProperty("Bits").GetInt64(), "Formatted native sine result regressed its independent oracle.");
            var restoration = restorations[0];
            Require(restoration.GetProperty("OriginalCulture").GetString() == restoration.GetProperty("RestoredCulture").GetString() &&
                restoration.GetProperty("SameReference").GetBoolean(), "Focused method did not restore its original culture reference.");
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
                (module.Key == "IQToolkit.Data.SQLite" && pair.Key == SQLiteSource
                    ? pair.Value.Equals(OldSQLiteHash, StringComparison.OrdinalIgnoreCase) && hash.Equals(NewSQLiteHash, StringComparison.OrdinalIgnoreCase)
                    : hash == pair.Value)), "Inherited PDB source/checksum changed beyond the exact SQLite document.");
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
        Require(before.Count == 424 && afterTests.Count == 427 &&
            before.All(pair => afterTests.TryGetValue(pair.Key, out var outcome) && outcome == pair.Value) &&
            added.SetEquals(afterTests.Keys.Except(before.Keys)) && added.All(name => afterTests[name] == "Passed"),
            "Strict test growth or inherited outcomes differ.");
        var oldDefinitions = Definitions(baseline.Descriptor.StrictTrx);
        var newDefinitions = Definitions(current.Descriptor.StrictTrx);
        Require(oldDefinitions.Count == 424 && newDefinitions.Count == 427 &&
            oldDefinitions.All(pair => newDefinitions.TryGetValue(pair.Key, out var value) && value == pair.Value),
            "Inherited strict definitions/categories differ.");
        var collectedDefinitions = Definitions(current.Descriptor.CoverageTrx);
        Require(collectedDefinitions.Count == newDefinitions.Count && newDefinitions.All(pair =>
            collectedDefinitions.TryGetValue(pair.Key, out var value) && value == pair.Value), "Collector definitions/categories differ from strict.");
        Require(afterTests.Count(pair => pair.Value == "Passed") == 102 && afterTests.Count(pair => pair.Value == "NotExecuted") == 325,
            "Current strict native pass/explicit-skip counts differ.");
        using var strictReceipt = JsonDocument.Parse(File.ReadAllBytes(Capture(current.Descriptor.StrictReceipt)));
        Require(strictReceipt.RootElement.GetProperty("Translations").GetInt32() == 247, "Current translation count differs.");
        VerifyFocused();
        var focusedDefinitions = Definitions(TransitionInput("FocusedTrx"));
        Require(focusedDefinitions.Count == 3 && added.All(name => focusedDefinitions.TryGetValue(name, out var value) && value == newDefinitions[name]),
            "Focused definitions differ from the strict added methods.");
        transitionReceipt = new
        {
            Status = "verified", Qualification = "Private SQLite Math.Sin identifier and one-Test-compilation transition only",
            BaselineDescriptor = TransitionInput("BaselineDescriptor"), CurrentDescriptor = TransitionInput("CurrentDescriptor"),
            BaselineBindings = Bindings, TestSource = TransitionInput("TestSource"), FocusedTrx = TransitionInput("FocusedTrx"),
            SQLiteSource = TransitionInput("SQLiteSource"), OldSQLiteSource = TransitionInput("OldSQLiteSource"), HistoricalChain = historicalChain,
            OldTests = before.Count, CurrentTests = afterTests.Count, AddedTests = AddedMethods,
            AddedPdbDocument = AddedSource, SourceFiles = current.Sources.Count, ComparedLeaves = current.Leaves.Count,
            ComparedInheritedLeaves = baseline.Leaves.Count, AddedLeafCount = current.Leaves.Count - baseline.Leaves.Count,
            SourceLineMapping = "Identity; SQLiteFormatter.cs line371 only", NewlyCoveredLeaves = TargetLeaves(),
            MeasuredModules = current.Modules, current.Total,
            IndividualBranchArmsVerified = false, CoverageTargetsVerified = false,
            FullGlobalizationVerified = false, OtherMathMethodsVerified = false, OtherDialectsVerified = false, OtherPlatformsVerified = false, DurableFinalProfileAccepted = false, FullGoalComplete = false
        };
    }
}
