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

internal static class CompilationContinuity
{
    private const long MaximumBytes = 16 * 1024 * 1024;
    private static readonly XNamespace Trx = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
    private static readonly Guid Sha256Algorithm = new("8829d00f-11b8-4213-878b-770e8597ac16");
    private static bool baselineContext;
    private static readonly Dictionary<string, Input> Bindings = new(StringComparer.Ordinal);
    private static readonly HashSet<string> UsedBindings = new(StringComparer.Ordinal);
    private static JsonElement transition;
    private static object? transitionReceipt;
    private const string AddedSource = "IQToolkitTest/RealProvider/NativeNullArrayMembershipTests.cs";
    private const string AddedClass = "IQToolkitTest.RealProvider.NativeNullArrayMembershipTests";
    private static readonly string[] AddedMethods =
    [
        "NativeNullArrayInputsAreAcquired",
        "CompilerNullArrayMembershipObservesReplacementWithoutCache",
        "CompilerNullArrayMembershipObservesReplacementWithCache"
    ];

    private sealed record Input(string Path, string Sha256);
    private sealed record Module(string Name, Input Dll, Input Pdb);
    private sealed record Preservation(Input Before, Input After);
    private sealed record Descriptor(int Schema, string RepositoryRoot, Input[] Reports, Input CoverageTrx,
        Input StrictTrx, Input Translation, Input StrictReceipt, Input Settings, Input Collector,
        Input Mapping, Module[] Modules, Module TestAssembly, Preservation[] Preservation);
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
        return transitionReceipt ?? throw new InvalidDataException("Missing compilation transition receipt.");
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
        Program.CaptureHistoricalCompilation(Resolve(path), expected);

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
            Require(module.Dll.Sha256.Equals(matching.Dll.Sha256, StringComparison.OrdinalIgnoreCase) &&
                module.Pdb.Sha256.Equals(matching.Pdb.Sha256, StringComparison.OrdinalIgnoreCase), "Compiled module changed; explicit qualification required.");
        }
        Require(baseline.Sources.Count == 97 && current.Sources.Count == 97 && baseline.Sources.All(pair => current.Sources.TryGetValue(pair.Key, out var hash) && hash == pair.Value),
            "Measured source changed; explicit qualification required.");
        Require(baseline.Leaves.Count == 16716 && current.Leaves.Count == 16716, "Coverage leaf denominator changed.");
        foreach (var pair in baseline.Leaves)
        {
            Require(current.Leaves.TryGetValue(pair.Key, out var leaf) && leaf.Branches == pair.Value.Branches,
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
    }

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
            new[] { "BaselineBindings", "BaselineDescriptor", "CurrentDescriptor", "FocusedTrx", "Schema", "TestSource" }) &&
            transition.GetProperty("Schema").GetInt32() == 1, "Unexpected transition schema or fields.");
        var baselineInput = TransitionInput("BaselineDescriptor");
        var currentInput = TransitionInput("CurrentDescriptor");
        Require(Path.GetFullPath(args[0]) == baselineInput.Path && Path.GetFullPath(args[1]) == currentInput.Path &&
            baselineInput.Sha256.Equals("173f9ce97c74f21f4c739f0afb74ce1abb596ed8e4ab7befd7280d849a940f10",
                StringComparison.OrdinalIgnoreCase), "Descriptor origins or qualified baseline hash differ.");
        using var document = JsonDocument.Parse(File.ReadAllBytes(Capture(baselineInput)));
        RejectDuplicateProperties(document.RootElement);
        var descriptor = document.Deserialize<Descriptor>() ?? throw new InvalidDataException("Missing baseline descriptor.");
        var root = Path.GetFullPath(descriptor.RepositoryRoot);
        Require(baselineInput.Path == Path.Combine(root, "artifacts", "sql-string-literal", "coverage.zzhhnhb6", "collection", "descriptor.json"),
            "Baseline descriptor is not the fixed qualified V1y origin.");
        Capture(currentInput);
        var source = TransitionInput("TestSource");
        Require(source.Path == Path.Combine(root, AddedSource), "Added source origin differs.");
        Capture(source);
        var focused = TransitionInput("FocusedTrx");
        Require(focused.Path == Path.Combine(root, "artifacts", "native-null-array", "behavior.nms6tn73", "results", "behavior.trx"),
            "Focused TRX is not the authentic qualified behavioral origin.");
        Capture(focused);
        foreach (var property in transition.GetProperty("BaselineBindings").EnumerateObject())
        {
            Require(Path.IsPathFullyQualified(property.Name) && Path.GetFullPath(property.Name) == property.Name &&
                Bindings.TryAdd(property.Name, ReadInput(property.Value)), "Invalid or duplicate baseline binding.");
        }
        var required = new HashSet<string>(StringComparer.Ordinal);
        foreach (var binary in new[] { descriptor.TestAssembly.Dll, descriptor.TestAssembly.Pdb })
        {
            var logical = Path.GetFullPath(binary.Path);
            required.Add(logical);
            Require(logical == Path.Combine(root, "IQToolkitTest", "bin", "Release", "net10.0", Path.GetFileName(logical)) &&
                Bindings.TryGetValue(logical, out var physical) && physical.Path == Path.Combine(root, "artifacts", "native-null-array",
                    "acquire.7ovl4p05", "previous-binaries", "IQToolkitTest", "bin", "Release", "net10.0", Path.GetFileName(logical)) &&
                physical.Sha256.Equals(binary.Sha256, StringComparison.OrdinalIgnoreCase), "Old Test binary binding differs.");
            Capture(Bindings[logical]);
        }
        var generated = new[] { "IQToolkitTest.AssemblyInfo.cs", ".NETCoreApp,Version=v10.0.AssemblyAttributes.cs" }
            .Select(name => Path.Combine(root, "IQToolkitTest", "obj", "Release", "net10.0", name)).ToHashSet(StringComparer.Ordinal);
        foreach (var pair in Bindings.Where(pair => !required.Contains(pair.Key)))
        {
            Require(generated.Contains(pair.Key) && pair.Value.Path == Path.Combine(root, "artifacts", "native-null-array",
                "acquire.7ovl4p05", "previous-generated-sources", Path.GetRelativePath(root, pair.Key)) &&
                !Hash(pair.Key).Equals(pair.Value.Sha256, StringComparison.OrdinalIgnoreCase), "Unnecessary or unreviewed generated-source alias.");
            Capture(pair.Value);
        }
        Require(Bindings.Count is >= 2 and <= 4 && required.IsSubsetOf(Bindings.Keys), "Old Test binding set differs.");
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

    private static void VerifyFocused()
    {
        var input = TransitionInput("FocusedTrx");
        var expected = AddedMethods.Select(name => AddedClass + "." + name).ToHashSet(StringComparer.Ordinal);
        var results = Executions(input, focused: true);
        Require(results.Count == 3 && expected.SetEquals(results.Keys) && results.Values.All(outcome => outcome == "Passed"),
            "Focused suite must contain exactly the three passing native methods.");
        var root = Xml(input);
        var labels = new[] { "null-initial", "nonempty", "empty", "nonempty-restored", "null-restored" };
        foreach (var result in root.Element(Trx + "Results")!.Elements(Trx + "UnitTestResult"))
        {
            var name = Attribute(result, "testName");
            var stdout = result.Element(Trx + "Output")?.Element(Trx + "StdOut")?.Value ?? string.Empty;
            Require(stdout.Contains("Actual native provenance: ", StringComparison.Ordinal) &&
                stdout.Contains("Actual native response: ", StringComparison.Ordinal) &&
                stdout.Contains("Actual compiler binding: ", StringComparison.Ordinal), "Focused native provenance/input/compiler observations are absent.");
            var observations = stdout.Split('\n').Select(line => line.TrimEnd('\r'))
                .Where(line => line.StartsWith("Actual null membership enumeration: ", StringComparison.Ordinal)).ToArray();
            if (name == AddedMethods[0])
            {
                Require(observations.Length == 0 && stdout.Contains("Actual acquisition scope: ", StringComparison.Ordinal),
                    "Acquisition observation scope differs.");
                continue;
            }
            Require(observations.Length == 5, "Focused ordinary query lacks five actual enumeration observations.");
            for (var i = 0; i < observations.Length; i++)
            {
                using var observation = JsonDocument.Parse(observations[i]["Actual null membership enumeration: ".Length..]);
                RejectDuplicateProperties(observation.RootElement);
                var value = observation.RootElement;
                Require(value.GetProperty("Capture").GetString() == labels[i] &&
                    value.GetProperty("CaptureIsNull").GetBoolean() == (i is 0 or 4) &&
                    value.GetProperty("Cached").GetBoolean() == (name == AddedMethods[2]) &&
                    value.GetProperty("ConnectionStateBefore").GetString() == "Closed" &&
                    value.GetProperty("ConnectionStateAfter").GetString() == "Closed" &&
                    value.GetProperty("Sql").GetString() is { Length: > 0 }, "Focused enumeration boundary/order differs.");
                var actual = value.GetProperty("ActualRows").EnumerateArray().Select(row => row.GetString()
                    ?? throw new InvalidDataException("Null native membership row.")).Order(StringComparer.Ordinal).ToArray();
                var oracle = value.GetProperty("ExpectedRows").EnumerateArray().Select(row => row.GetString()
                    ?? throw new InvalidDataException("Null native oracle row.")).Order(StringComparer.Ordinal).ToArray();
                Require(actual.SequenceEqual(oracle) && (i is 1 or 3 ? actual.Length > 0 : actual.Length == 0),
                    "Focused actual membership rows differ from the full native oracle.");
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
            Require(module.Value.All(pair => after.TryGetValue(pair.Key, out var hash) && hash == pair.Value),
                "Inherited PDB source/checksum changed.");
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
        Require(before.Count == 416 && afterTests.Count == 419 &&
            before.All(pair => afterTests.TryGetValue(pair.Key, out var outcome) && outcome == pair.Value) &&
            added.SetEquals(afterTests.Keys.Except(before.Keys)) && added.All(name => afterTests[name] == "Passed"),
            "Strict test growth or inherited outcomes differ.");
        var oldDefinitions = Definitions(baseline.Descriptor.StrictTrx);
        var newDefinitions = Definitions(current.Descriptor.StrictTrx);
        Require(oldDefinitions.Count == 416 && newDefinitions.Count == 419 &&
            oldDefinitions.All(pair => newDefinitions.TryGetValue(pair.Key, out var value) && value == pair.Value),
            "Inherited strict definitions/categories differ.");
        var collectedDefinitions = Definitions(current.Descriptor.CoverageTrx);
        Require(collectedDefinitions.Count == newDefinitions.Count && newDefinitions.All(pair =>
            collectedDefinitions.TryGetValue(pair.Key, out var value) && value == pair.Value), "Collector definitions/categories differ from strict.");
        VerifyFocused();
        var focusedDefinitions = Definitions(TransitionInput("FocusedTrx"));
        Require(focusedDefinitions.Count == 3 && added.All(name => focusedDefinitions.TryGetValue(name, out var value) && value == newDefinitions[name]),
            "Focused definitions differ from the strict added methods.");
        transitionReceipt = new
        {
            Status = "verified", Qualification = "Private one-Test-compilation transition only",
            BaselineDescriptor = TransitionInput("BaselineDescriptor"), CurrentDescriptor = TransitionInput("CurrentDescriptor"),
            BaselineBindings = Bindings, TestSource = TransitionInput("TestSource"), FocusedTrx = TransitionInput("FocusedTrx"),
            OldTests = before.Count, CurrentTests = afterTests.Count, AddedTests = AddedMethods,
            AddedPdbDocument = AddedSource, SourceFiles = current.Sources.Count, ComparedLeaves = current.Leaves.Count,
            MeasuredModules = current.Modules, current.Total,
            IndividualBranchArmsVerified = false, CoverageTargetsVerified = false,
            FullArrayNullVerified = false, DurableFinalProfileAccepted = false
        };
    }
}
