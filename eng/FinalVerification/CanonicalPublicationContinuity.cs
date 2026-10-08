using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
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

internal static class CanonicalPublicationContinuity
{
    private const long MaximumBytes = 16 * 1024 * 1024;
    private static readonly XNamespace Trx = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
    private static readonly Guid Sha256Algorithm = new("8829d00f-11b8-4213-878b-770e8597ac16");
    private static string repository = string.Empty;
    private const string BaselineHash = "9bc0eb52ce5d50ba9cb9e89744851ed4e75c4d7aa1e589fc2772f6a6b8567d35";
    private const string TestSourcePath = "IQToolkitTest/RealProvider/CanonicalPublicationTests.cs";
    private const string TestSourceHash = "63c6e269ef893bce3f17ca9217d1d9df6009ae09d7dfb7b3fef226152616ee4b";
    private const string TestClass = "IQToolkitTest.RealProvider.CanonicalPublicationTests";
    private static readonly string[] AddedMethods =
    [
        "MappingCreationReturnsThePublishedNativeEntity", "TableCreationReturnsThePublishedNativeTable",
        "SequentialMappingRetainsItsNativeEntityAndResults", "SequentialTableRetainsItsNativeRootAndResults"
    ];
    private static readonly SourceChange[] SourceChanges =
    [
        new("IQToolkit.Data/Mapping/AttributeMapping.cs",
            "45b7e7c3da47e584a9e38d216c5fb23f6adfcd4ac35e64fb454a466601c336aa",
            "4dcaae30ecfa02e90cbc00da1a8412b1d68e4be12fec5102f907504f548088d1",
            "                this._entities.GetOrAdd(tableId, entity);",
            "                entity = this._entities.GetOrAdd(tableId, entity);"),
        new("IQToolkit.Data/EntityProvider.cs",
            "f050602ab8cd027c4b18aa6f9218aaf1004d645358a94c6f110c91eeb2e92016",
            "176306f6fd437bdfce693216bb57f4cde29cb692bb00b92230a6c843aa720b5a",
            "                this._tables.GetOrAdd(entity, table);",
            "                table = this._tables.GetOrAdd(entity, table);")
    ];
    private static Dictionary<string, Input> BaselineBindings = new(StringComparer.Ordinal);
    private static bool baselineContext;

    private sealed record SourceChange(string Path, string BeforeHash, string AfterHash, string Before, string After);
    private sealed record Transition(int Schema, Input BaselineDescriptor, Input CurrentDescriptor,
        Dictionary<string, Input> BaselineBindings, Input TestSource, Input FocusedTrx, Input CurrentStrictTrx);
    private sealed record TestIdentity(string Outcome, string Categories, string Adapter, string CodeBase);

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

    // The coordinator owns input bounds, terminal rehashing and publication.
    internal static object Validate(string repositoryRoot, string baselinePath, string currentPath, string manifest)
    {
        Require(Path.IsPathFullyQualified(repositoryRoot) && Path.GetFullPath(repositoryRoot) == repositoryRoot,
            "Repository root must be an absolute normalized path.");
        repository = repositoryRoot;
        baselineContext = false;
        BaselineBindings = FixedBindings();
        var transition = ReadTransition(manifest, baselinePath, currentPath);
        using var nativeProducer = AuthenticateNativeProducer(transition, manifest);
        VerifySourceChanges();
        Measurement baseline;
        baselineContext = true;
        try { baseline = Qualify(ReadDescriptor(baselinePath)); }
        finally { baselineContext = false; }
        var current = Qualify(ReadDescriptor(currentPath));
        Require(current.Descriptor.StrictTrx.Path == transition.CurrentStrictTrx.Path &&
            current.Descriptor.StrictTrx.Sha256.Equals(transition.CurrentStrictTrx.Sha256, StringComparison.OrdinalIgnoreCase),
            "Current descriptor does not bind the reviewed strict run.");
        VerifyTestMethods(current.Descriptor.TestAssembly);
        CompareSuites(baseline.Descriptor, current.Descriptor, transition.FocusedTrx);
        Compare(baseline, current);
        var transitionReceipt = new
        {
            Contract = "CANONICAL-PUBLICATION-COVERAGE-1", OldTests = 427, CurrentTests = 431,
            OldPassed = 102, CurrentPassed = 106, ExplicitSqlSkips = 325, Translations = 247,
            AddedTests = AddedMethods.Select(method => TestClass + "." + method).ToArray(),
            AddedPdbDocument = TestSourcePath, TestSourceSha256 = TestSourceHash,
            BaselineBindings = BaselineBindings.Count, ChangedProductionSources = SourceChanges.Length,
            ComparedInheritedLeaves = current.Leaves.Count, AddedLeafCount = 0,
            FullGoalComplete = false, CoverageTargetsVerified = false, IndividualBranchArmsVerified = false
        };
        var outcome = new { Status = "verified", MeasuredModules = current.Modules, current.Total,
            SourceFiles = current.Sources.Count, ComparedLeaves = current.Leaves.Count,
            IndividualBranchArmsVerified = false, CoverageTargetsVerified = false,
            FullGoalComplete = false, OldTests = 427, CurrentTests = 431, AddedTests = 4 };
        var native = nativeProducer.RootElement.GetProperty("NativeComparison");
        Require(SameJson(JsonSerializer.SerializeToElement(transitionReceipt), native.GetProperty("Transition")) &&
            SameJson(JsonSerializer.SerializeToElement(outcome), native.GetProperty("Outcome")),
            "Reconstructed canonical comparison differs from the accepted native outcome or receipt.");
        return new
        {
            Status = "verified", Operation = "canonical-compilation-continuity", Transition = transitionReceipt, Outcome = outcome,
            NativeProducerAuthenticated = true, CurrentComparisonVerified = true, CurrentFinalAccepted = false,
            FinalReadinessVerified = false, FullGoalComplete = false, CoverageTargetsVerified = false,
            IndividualBranchArmsVerified = false
        };
    }

    private static void Require([DoesNotReturnIf(false)] bool condition, string reason)
    {
        if (!condition) throw new InvalidDataException(reason);
    }

    private static string Hash(string path)
    {
        using var input = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(input));
    }

    private static string Capture(string path, string? expected = null)
    {
        path = Path.GetFullPath(path);
        if (baselineContext && BaselineBindings.TryGetValue(path, out var binding))
        {
            Require(expected is not null && expected.Equals(binding.Sha256, StringComparison.OrdinalIgnoreCase),
                "Baseline alias lacks the exact original expected identity.");
            path = binding.Path;
        }
        return Program.CaptureHistoricalRecent(path, expected);
    }

    private static string Capture(Input input) => Capture(input.Path, input.Sha256);

    private static Descriptor ReadDescriptor(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Capture(path)));
        RejectDuplicateProperties(document.RootElement);
        var descriptor = document.Deserialize<Descriptor>() ?? throw new InvalidDataException("Missing descriptor.");
        Require(descriptor.Schema == 1 && descriptor.RepositoryRoot == repository, "Unsupported descriptor schema or repository root.");
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
                var hash = Hash(Capture(Path.Combine(descriptor.RepositoryRoot, source), sourceHash));
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

    private static Dictionary<string, string> Executions(Input input, bool requireNativeScope = true)
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
        if (requireNativeScope) Require(observed.Any(pair => pair.Key.StartsWith("IQToolkitTest.SQLiteTests.", StringComparison.Ordinal) && pair.Value == "Passed") &&
            observed.Any(pair => pair.Key.StartsWith("IQToolkitTest.RealProvider.", StringComparison.Ordinal) && pair.Value == "Passed"), "Expected native test suites did not execute.");
        return observed;
    }

    private static void ValidateExecutions(Descriptor descriptor)
    {
        using var receipt = JsonDocument.Parse(File.ReadAllBytes(Capture(descriptor.StrictReceipt)));
        RejectDuplicateProperties(receipt.RootElement);
        Require(receipt.RootElement.GetProperty("Status").GetString() == "verified" &&
            string.Equals(receipt.RootElement.GetProperty("TrxSha256").GetString(), descriptor.StrictTrx.Sha256, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(receipt.RootElement.GetProperty("TranslationSha256").GetString(), descriptor.Translation.Sha256, StringComparison.OrdinalIgnoreCase) &&
            receipt.RootElement.GetProperty("Translations").GetInt32() == 247, "Strict verification receipt does not bind the supplied results.");
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
        foreach (var module in baseline.Descriptor.Modules.Append(baseline.Descriptor.TestAssembly))
        {
            var matching = current.Descriptor.Modules.Append(current.Descriptor.TestAssembly).Single(item => item.Name == module.Name);
            var sameDll = module.Dll.Sha256.Equals(matching.Dll.Sha256, StringComparison.OrdinalIgnoreCase);
            var samePdb = module.Pdb.Sha256.Equals(matching.Pdb.Sha256, StringComparison.OrdinalIgnoreCase);
            Require(module.Name is "IQToolkit.Data" or "IQToolkitTest" ? !sameDll && !samePdb : sameDll && samePdb,
                "Compiled module transition differs from the reviewed Data/Test change.");
            if (module.Name == "IQToolkit.Data")
                Require(matching.Dll.Sha256.Equals("715585815ed7c954c032e98a0eeef888a5c2c8038be09959d50c2123da516374", StringComparison.OrdinalIgnoreCase) &&
                    matching.Pdb.Sha256.Equals("1b9845da7b85556bdb05c6210da7434e98417a8ae079c2c98b17e04f025607c3", StringComparison.OrdinalIgnoreCase),
                    "Current Data pair differs from the reviewed native GREEN/strict outputs.");
            if (module.Name == "IQToolkitTest")
                Require(matching.Dll.Sha256.Equals("bd7df8c454cc29b85962c791045533833fba2681b50517415882594e0ed6925a", StringComparison.OrdinalIgnoreCase) &&
                    matching.Pdb.Sha256.Equals("5397e459e73f56a21f11ceb657a823bb7f5979cbc71c9fe814bd87e4b87b0784", StringComparison.OrdinalIgnoreCase),
                    "Current Test pair differs from the reviewed native GREEN/strict outputs.");
        }
        Require(baseline.Sources.Count == 97 && current.Sources.Count == 97 &&
            baseline.Sources.Keys.Order(StringComparer.Ordinal).SequenceEqual(current.Sources.Keys.Order(StringComparer.Ordinal)),
            "Measured source scope changed.");
        foreach (var pair in baseline.Sources)
        {
            var change = SourceChanges.SingleOrDefault(item => item.Path == pair.Key);
            Require(change is null ? pair.Value == current.Sources[pair.Key] :
                pair.Value.Equals(change.BeforeHash, StringComparison.OrdinalIgnoreCase) &&
                current.Sources[pair.Key].Equals(change.AfterHash, StringComparison.OrdinalIgnoreCase),
                "Measured source differs from the fixed two-source transition.");
        }
        Require(SourceChanges.All(change => baseline.Sources.ContainsKey(change.Path)), "Changed sources were not measured.");
        CompareDocuments(baseline, current);
        Require(baseline.Leaves.Count == 16724 && current.Leaves.Count == 16724, "Coverage leaf denominator changed.");
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

    private static Dictionary<string, Input> FixedBindings()
    {
        var bindings = new Dictionary<string, Input>(StringComparer.Ordinal);
        void Add(string logical, string physical, string hash) =>
            bindings.Add(Path.Combine(repository, logical), new Input(Path.Combine(repository, physical), hash));
        const string green = "artifacts/canonical-publication/green.zk1rnuil/previous-binaries/IQToolkitTest/bin/Release/net10.0/";
        const string red = "artifacts/canonical-publication/red.wptrw9ln/previous-binaries/IQToolkitTest/bin/Release/net10.0/";
        Add("IQToolkitTest/bin/Release/net10.0/IQToolkit.Data.dll", green + "IQToolkit.Data.dll",
            "8a9f1c975cea2e93807d06bdbaae68597e4328e02728d89e29ee61501a87f4db");
        Add("IQToolkitTest/bin/Release/net10.0/IQToolkit.Data.pdb", green + "IQToolkit.Data.pdb",
            "e4b3139e2483243410c3a42fa1ab12e8bd5de63bca799e8716313ec08a2ac277");
        Add("IQToolkitTest/bin/Release/net10.0/IQToolkitTest.dll", red + "IQToolkitTest.dll",
            "2448859d14643becc5b0ff6fbbf2b9d2d8060e13b5e3666d6d09d075680c08ce");
        Add("IQToolkitTest/bin/Release/net10.0/IQToolkitTest.pdb", red + "IQToolkitTest.pdb",
            "708e9e25c523c0654245ffe7626ed5eba03ee7a9a52af23daacf648103e06185");
        foreach (var change in SourceChanges)
            Add(change.Path, "artifacts/canonical-publication/before-repair/" + change.Path, change.BeforeHash);
        return bindings;
    }

    private static void ValidateInput(Input input)
    {
        Require(input is not null && input.Path is not null && input.Sha256 is not null &&
            Path.IsPathFullyQualified(input.Path) && input.Path == Path.GetFullPath(input.Path) &&
            Regex.IsMatch(input.Sha256, "^[0-9A-Fa-f]{64}$"), "Invalid transition input.");
        Capture(input);
    }

    private static Transition ReadTransition(string path, string baselinePath, string currentPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Capture(path)));
        RejectDuplicateProperties(document.RootElement);
        Require(document.RootElement.ValueKind == JsonValueKind.Object &&
            document.RootElement.EnumerateObject().Select(item => item.Name).Order(StringComparer.Ordinal).SequenceEqual(
                new[] { "Schema", "BaselineDescriptor", "CurrentDescriptor", "BaselineBindings", "TestSource", "FocusedTrx", "CurrentStrictTrx" }.Order(StringComparer.Ordinal)),
            "Transition field scope differs.");
        var transition = document.Deserialize<Transition>() ?? throw new InvalidDataException("Missing transition.");
        Require(transition.Schema == 1 && transition.BaselineBindings is not null && transition.BaselineBindings.Count == 6,
            "Unsupported transition schema or binding count.");
        foreach (var input in new[] { transition.BaselineDescriptor, transition.CurrentDescriptor, transition.TestSource,
            transition.FocusedTrx, transition.CurrentStrictTrx }) ValidateInput(input);
        Require(transition.BaselineDescriptor.Path == Path.Combine(repository, "artifacts/coverage/runs/run.TFv2whz3/descriptor.json") &&
            transition.BaselineDescriptor.Path == Path.GetFullPath(baselinePath) &&
            transition.BaselineDescriptor.Sha256.Equals(BaselineHash, StringComparison.OrdinalIgnoreCase) &&
            transition.CurrentDescriptor.Path == Path.GetFullPath(currentPath) &&
            transition.CurrentDescriptor.Path != transition.BaselineDescriptor.Path,
            "Descriptor identity differs from the sealed transition.");
        Require(transition.TestSource.Path == Path.Combine(repository, TestSourcePath) &&
            transition.TestSource.Sha256.Equals(TestSourceHash, StringComparison.OrdinalIgnoreCase), "Added test source differs.");
        Require(transition.FocusedTrx.Path == Path.Combine(repository, "artifacts/canonical-publication/green.zk1rnuil/results/behavior.trx") &&
            transition.FocusedTrx.Sha256.Equals("455fd06990e1fe117801550f2d5e63030cb173407d2dfbf35eda1aaafb6d52ca", StringComparison.OrdinalIgnoreCase),
            "Focused native GREEN identity differs.");
        Require(transition.CurrentStrictTrx.Path == Path.Combine(repository, "artifacts/verification/runs/run.Rj3XBXVk/tests.trx") &&
            transition.CurrentStrictTrx.Sha256.Equals("8201f89bd57355452e533d28fe0ab993c0bcc65dbe124dd5e7c8fa4a2d2d04e2", StringComparison.OrdinalIgnoreCase),
            "Reviewed current strict identity differs.");
        foreach (var pair in BaselineBindings)
        {
            Require(transition.BaselineBindings.TryGetValue(pair.Key, out var actual) && actual is not null &&
                actual.Path == pair.Value.Path && string.Equals(actual.Sha256, pair.Value.Sha256, StringComparison.OrdinalIgnoreCase),
                "Baseline binding origin or hash differs.");
            ValidateInput(actual!);
        }
        return transition;
    }

    private static void VerifySourceChanges()
    {
        foreach (var change in SourceChanges)
        {
            var before = File.ReadAllBytes(Capture(BaselineBindings[Path.Combine(repository, change.Path)]));
            var after = File.ReadAllBytes(Capture(Path.Combine(repository, change.Path), change.AfterHash));
            var text = Encoding.UTF8.GetString(before);
            var first = text.IndexOf(change.Before, StringComparison.Ordinal);
            Require(first >= 0 && text.IndexOf(change.Before, first + change.Before.Length, StringComparison.Ordinal) < 0 &&
                !text.EndsWith('\n') && !text.EndsWith('\r'), "Original source does not match the fixed assignment recipe.");
            var expected = Encoding.UTF8.GetBytes(text.Replace(change.Before, change.After, StringComparison.Ordinal) + "\n");
            Require(expected.AsSpan().SequenceEqual(after), "Source differs from one fixed assignment and one final LF.");
        }
    }

    private static void CompareDocuments(Measurement baseline, Measurement current)
    {
        foreach (var module in baseline.Documents)
        {
            var next = current.Documents[module.Key];
            var added = module.Key == "IQToolkitTest" ? 1 : 0;
            Require(next.Count == module.Value.Count + added, "PDB document scope changed.");
            foreach (var document in module.Value)
            {
                var change = module.Key == "IQToolkit.Data" ? SourceChanges.SingleOrDefault(item => item.Path == document.Key) : null;
                Require(next.TryGetValue(document.Key, out var hash) && (change is null ? hash == document.Value :
                    document.Value.Equals(change.BeforeHash, StringComparison.OrdinalIgnoreCase) &&
                    hash!.Equals(change.AfterHash, StringComparison.OrdinalIgnoreCase)), "Inherited PDB source checksum changed unexpectedly.");
            }
            if (module.Key == "IQToolkit.Data")
                Require(SourceChanges.All(change => module.Value.ContainsKey(change.Path)), "Changed Data documents are absent.");
            if (module.Key == "IQToolkitTest")
                Require(!module.Value.ContainsKey(TestSourcePath) && next.TryGetValue(TestSourcePath, out var addedHash) &&
                    addedHash.Equals(TestSourceHash, StringComparison.OrdinalIgnoreCase), "Added test PDB document differs.");
        }
    }

    private static Dictionary<string, TestIdentity> Suite(Input input, bool requireNativeScope = true)
    {
        var outcomes = Executions(input, requireNativeScope);
        var root = Xml(input);
        var suite = new Dictionary<string, TestIdentity>(StringComparer.Ordinal);
        foreach (var definition in root.Element(Trx + "TestDefinitions")!.Elements(Trx + "UnitTest"))
        {
            var method = definition.Element(Trx + "TestMethod")!;
            var categories = (definition.Element(Trx + "TestCategory")?.Elements(Trx + "TestCategoryItem") ?? [])
                .Select(item => Attribute(item, "TestCategory")).Order(StringComparer.Ordinal).ToArray();
            Require(categories.Distinct(StringComparer.Ordinal).Count() == categories.Length, "Duplicate test category.");
            var name = Attribute(method, "className") + "." + Attribute(method, "name");
            suite.Add(name, new TestIdentity(outcomes[name], JsonSerializer.Serialize(categories),
                Attribute(method, "adapterTypeName"), Path.GetFileName(Attribute(method, "codeBase"))));
        }
        return suite;
    }

    private static void CompareSuites(Descriptor baseline, Descriptor current, Input focused)
    {
        var old = Suite(baseline.StrictTrx);
        var next = Suite(current.StrictTrx);
        var collected = Suite(current.CoverageTrx);
        var previousCollected = Suite(baseline.CoverageTrx);
        Require(old.Count == 427 && old.Values.Count(item => item.Outcome == "Passed") == 102 &&
            old.Values.Count(item => item.Outcome == "NotExecuted") == 325 && previousCollected.Count == old.Count &&
            old.All(pair => previousCollected.TryGetValue(pair.Key, out var actual) && actual == pair.Value), "Baseline suite identity differs.");
        Require(next.Count == 431 && next.Values.Count(item => item.Outcome == "Passed") == 106 &&
            next.Values.Count(item => item.Outcome == "NotExecuted") == 325 &&
            old.All(pair => next.TryGetValue(pair.Key, out var actual) && actual == pair.Value) &&
            collected.Count == next.Count && next.All(pair => collected.TryGetValue(pair.Key, out var actual) && actual == pair.Value),
            "Inherited definitions, categories, adapter or outcomes changed.");
        var expected = AddedMethods.Select(method => TestClass + "." + method).Order(StringComparer.Ordinal).ToArray();
        Require(next.Keys.Except(old.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).SequenceEqual(expected), "Added suite differs.");
        var native = Suite(focused, requireNativeScope: false);
        Require(native.Count == 4 && native.Keys.Order(StringComparer.Ordinal).SequenceEqual(expected) &&
            native.All(pair => pair.Value.Outcome == "Passed" && next[pair.Key] == pair.Value), "Focused four-case GREEN differs.");
    }

    private static string AttributeType(MetadataReader metadata, CustomAttributeHandle handle)
    {
        var constructor = metadata.GetCustomAttribute(handle).Constructor;
        EntityHandle type;
        if (constructor.Kind == HandleKind.MemberReference) type = metadata.GetMemberReference((MemberReferenceHandle)constructor).Parent;
        else if (constructor.Kind == HandleKind.MethodDefinition) type = metadata.GetMethodDefinition((MethodDefinitionHandle)constructor).GetDeclaringType();
        else return string.Empty;
        if (type.Kind == HandleKind.TypeReference)
        {
            var reference = metadata.GetTypeReference((TypeReferenceHandle)type);
            return metadata.GetString(reference.Namespace) + "." + metadata.GetString(reference.Name);
        }
        if (type.Kind == HandleKind.TypeDefinition)
        {
            var definition = metadata.GetTypeDefinition((TypeDefinitionHandle)type);
            return metadata.GetString(definition.Namespace) + "." + metadata.GetString(definition.Name);
        }
        return string.Empty;
    }

    private static void VerifyTestMethods(Module test)
    {
        using var pe = new PEReader(new MemoryStream(File.ReadAllBytes(Capture(test.Dll))));
        using var provider = MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(File.ReadAllBytes(Capture(test.Pdb))));
        var metadata = pe.GetMetadataReader();
        var pdb = provider.GetMetadataReader();
        var types = metadata.TypeDefinitions.Where(handle =>
        {
            var candidate = metadata.GetTypeDefinition(handle);
            return metadata.GetString(candidate.Namespace) + "." + metadata.GetString(candidate.Name) == TestClass;
        }).ToArray();
        Require(types.Length == 1, "Added test class is missing or ambiguous.");
        var type = metadata.GetTypeDefinition(types[0]);
        const string mstest = "Microsoft.VisualStudio.TestTools.UnitTesting.";
        Require((type.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public &&
            type.GetCustomAttributes().Count(handle => AttributeType(metadata, handle) == mstest + "TestClassAttribute") == 1,
            "Added class is not the public MSTest class.");
        var methods = type.GetMethods().Where(handle => metadata.GetMethodDefinition(handle).GetCustomAttributes()
            .Any(attribute => AttributeType(metadata, attribute) == mstest + "TestMethodAttribute")).ToArray();
        Require(methods.Length == 4 && methods.Select(handle => metadata.GetString(metadata.GetMethodDefinition(handle).Name))
            .Order(StringComparer.Ordinal).SequenceEqual(AddedMethods.Order(StringComparer.Ordinal)), "Compiled added test methods differ.");
        var sourceLines = File.ReadAllLines(Capture(Path.Combine(repository, TestSourcePath), TestSourceHash)).Length;
        foreach (var handle in methods)
        {
            var method = metadata.GetMethodDefinition(handle);
            Require((method.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public &&
                (method.Attributes & (MethodAttributes.Static | MethodAttributes.Abstract)) == 0 &&
                metadata.GetBlobBytes(method.Signature).AsSpan().SequenceEqual(new byte[] { 0x20, 0x00, 0x01 }) &&
                method.RelativeVirtualAddress > 0 && pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes() is { Length: > 0 } &&
                method.GetCustomAttributes().Count(attribute => AttributeType(metadata, attribute) == mstest + "TestMethodAttribute") == 1,
                "Added test lacks an actual public instance void body.");
            var debug = pdb.GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(MetadataTokens.GetRowNumber(handle)));
            var points = debug.GetSequencePoints().Where(point => !point.IsHidden).ToArray();
            Require(points.Length > 0, "Added method has no visible source sequence points.");
            foreach (var point in points)
            {
                var documentHandle = point.Document.IsNil ? debug.Document : point.Document;
                Require(!documentHandle.IsNil && point.StartLine > 0 && point.EndLine >= point.StartLine && point.EndLine <= sourceLines,
                    "Added method has an invalid source range.");
                var document = pdb.GetDocument(documentHandle);
                Require(pdb.GetString(document.Name) == "/_/" + TestSourcePath &&
                    pdb.GetGuid(document.HashAlgorithm) == Sha256Algorithm &&
                    Convert.ToHexString(pdb.GetBlobBytes(document.Hash)).Equals(TestSourceHash, StringComparison.OrdinalIgnoreCase),
                    "Added method is not bound to the reviewed test source.");
            }
        }
    }

    private static JsonDocument ReadJson(Input input)
    {
        var document = JsonDocument.Parse(File.ReadAllBytes(Capture(input)));
        RejectDuplicateProperties(document.RootElement);
        return document;
    }

    private static bool SameJson(JsonElement left, JsonElement right)
    {
        if (left.ValueKind != right.ValueKind) return false;
        if (left.ValueKind == JsonValueKind.Object)
        {
            var properties = left.EnumerateObject().ToArray();
            return properties.Length == right.EnumerateObject().Count() && properties.All(property =>
                right.TryGetProperty(property.Name, out var value) && SameJson(property.Value, value));
        }
        if (left.ValueKind == JsonValueKind.Array)
        {
            var items = left.EnumerateArray().ToArray();
            var others = right.EnumerateArray().ToArray();
            return items.Length == others.Length && items.Zip(others).All(pair => SameJson(pair.First, pair.Second));
        }
        return left.ValueKind == JsonValueKind.String ? left.GetString() == right.GetString() : left.GetRawText() == right.GetRawText();
    }

    private static JsonDocument AuthenticateNativeProducer(Transition transition, string manifest)
    {
        const string run = "artifacts/canonical-publication/qualify.55018kq5/";
        const string sourceHash = "3d4ca25f23159be70303bde6d6544badfac383447f6e35b0ee772c8d27da9e0a";
        const string nativeParent = "/tmp/iqtoolkit-canonical-coverage.51kaqevg";
        const string dotnet = "/home/firestrand/.dotnet/dotnet";
        var resultInput = new Input(Path.Combine(repository, run + "result.json"),
            "15a5e74d9566e326047b3432f51ebc8a519f33eb6118d48efb09d75831cb8626");
        var receiptInput = new Input(Path.Combine(repository, run + "retention/transition.json"),
            "41a659abbc72556948c589a47385915b7694ce5cd2de9ce9e904065f04cbc3b7");
        var beforeInput = new Input(Path.Combine(repository, run + "retention/inputs-before.json"),
            "79a7eb5a9e93a2a0cc569fbf7693911f2e8681627e602a3e14628df216cd7ac0");
        var afterInput = new Input(Path.Combine(repository, run + "retention/inputs-after.json"),
            "42ca4653537fffa8a14f7d857c1229865ed99a0ec0bbba8009ff6f5f128c4081");
        Capture(new Input(Path.Combine(repository, "artifacts/canonical-publication/coverage-helper/Program.cs"), sourceHash));
        Capture(new Input(Path.Combine(repository, run + "environment-retired/qualifier/Program.cs.snapshot"), sourceHash));
        using var receiptDocument = ReadJson(receiptInput);
        using var beforeDocument = ReadJson(beforeInput);
        using var afterDocument = ReadJson(afterInput);
        var resultDocument = ReadJson(resultInput);
        try
        {
            var result = resultDocument.RootElement;
            Require(result.GetProperty("Status").GetString() == "passed" && !result.GetProperty("Cancelled").GetBoolean() &&
                result.GetProperty("CleanupErrors").GetArrayLength() == 0 && result.GetProperty("StorageErrors").GetArrayLength() == 0 &&
                result.GetProperty("RootSourcesUnchanged").GetBoolean() && result.GetProperty("ReviewedInputsUnchanged").GetBoolean() &&
                result.GetProperty("RootIndexUnchanged").GetBoolean() && result.GetProperty("OrdinaryBinariesClassified").GetBoolean() &&
                result.GetProperty("OrdinaryBinariesUnchanged").GetInt32() == 336 && result.GetProperty("OrdinaryBinaryDeltaPaths").GetArrayLength() == 0 &&
                result.GetProperty("CoverageQualified").GetBoolean() && result.GetProperty("ScopedOutcomeVerified").GetBoolean(),
                "Accepted canonical producer was incomplete or failed preservation.");
            foreach (var flag in new[] { "FullGoalComplete", "CoverageTargetsVerified", "IndividualBranchArmsVerified" })
                Require(!result.GetProperty(flag).GetBoolean(), "Accepted canonical producer broad qualification differs.");
            var commands = result.GetProperty("Commands").EnumerateArray().ToArray();
            Require(commands.Select(command => command.GetProperty("Name").GetString()).SequenceEqual(
                new[] { "sdk-version", "qualifier-restore", "qualifier-build", "compare" }), "Accepted canonical command scope differs.");
            string[] options = ["-p:ImportDirectoryBuildProps=false", "-p:ImportDirectoryBuildTargets=false",
                "-p:ManagePackageVersionsCentrally=false", "-p:ContinuousIntegrationBuild=true", "-warnaserror",
                "--disable-build-servers", "-p:UseSharedCompilation=false", "-nr:false"];
            string[][] expectedArgv =
            [
                [dotnet, "--version"],
                new[] { dotnet, "restore", nativeParent + "/qualifier/Verifier.csproj", "--configfile",
                    nativeParent + "/qualifier/NuGet.config" }.Concat(options).ToArray(),
                new[] { dotnet, "build", nativeParent + "/qualifier/Verifier.csproj", "-c", "Release", "--no-restore" }
                    .Concat(options).ToArray(),
                [dotnet, nativeParent + "/qualifier/bin/Release/net10.0/Verifier.dll", transition.BaselineDescriptor.Path,
                    transition.CurrentDescriptor.Path, Path.Combine(repository, run + "retention"), Path.GetFullPath(manifest)]
            ];
            for (var index = 0; index < commands.Length; index++)
            {
                var command = commands[index];
                Require(command.GetProperty("Argv").EnumerateArray().Select(value => value.GetString()).SequenceEqual(expectedArgv[index]) &&
                    command.GetProperty("Cwd").GetString() == (index is 0 or 3 ? repository : nativeParent) &&
                    command.GetProperty("Exit").GetInt32() == 0 && command.GetProperty("ExpectedExit").GetInt32() == 0 &&
                    command.GetProperty("DeadlineSeconds").GetInt32() == 120 && command.GetProperty("Pid").GetInt32() > 0 &&
                    command.GetProperty("Environment").EnumerateObject().Count() == 0 &&
                    command.GetProperty("RemainingLiveMembers").GetArrayLength() == 0 && command.GetProperty("SecondaryErrors").GetArrayLength() == 0 &&
                    (!command.TryGetProperty("ForcedTermination", out var forced) || !forced.GetBoolean()) &&
                    (!command.TryGetProperty("NativeExitUnobserved", out var unobserved) || !unobserved.GetBoolean()),
                    "Accepted canonical native command or invocation did not complete ordinarily.");
                var logHash = command.GetProperty("LogSha256").GetString();
                Require(logHash is not null && Regex.IsMatch(logHash, "^[0-9A-Fa-f]{64}$"), "Accepted canonical command log hash differs.");
                var log = Capture(Path.Combine(repository, run + command.GetProperty("Name").GetString() + ".log"), logHash);
                Require(new FileInfo(log).Length == command.GetProperty("LogBytes").GetInt64(), "Accepted canonical command log length differs.");
            }
            AuthenticateRetirement(run, nativeParent, sourceHash);
            var native = result.GetProperty("NativeComparison");
            Require(native.GetProperty("Outcome").GetProperty("Status").GetString() == "verified" &&
                native.GetProperty("RetainedInputs").GetInt32() == 195 && native.GetProperty("RetainedBytes").GetInt64() == 27546690 &&
                SameJson(native.GetProperty("Transition"), receiptDocument.RootElement), "Accepted canonical receipt/result binding differs.");
            foreach (var flag in new[] { "FullGoalComplete", "CoverageTargetsVerified", "IndividualBranchArmsVerified" })
                Require(!native.GetProperty("Outcome").GetProperty(flag).GetBoolean() && !receiptDocument.RootElement.GetProperty(flag).GetBoolean(),
                    "Accepted canonical native outcome broad qualification differs.");
            foreach (var pair in new[] { ("InputBeforeSha256", beforeInput), ("InputAfterSha256", afterInput), ("TransitionSha256", receiptInput) })
                Require(string.Equals(native.GetProperty(pair.Item1).GetString(), pair.Item2.Sha256, StringComparison.OrdinalIgnoreCase),
                    "Accepted canonical native receipt hash differs.");
            var before = beforeDocument.RootElement.Deserialize<Dictionary<string, RetainedInput>>() ?? throw new InvalidDataException("Missing accepted canonical input map.");
            var after = afterDocument.RootElement.Deserialize<Dictionary<string, string>>() ?? throw new InvalidDataException("Missing accepted canonical terminal map.");
            Require(before.Count == 195 && after.Count == before.Count && before.Keys.All(after.ContainsKey), "Accepted canonical input-map scope differs.");
            var snapshots = new HashSet<string>(StringComparer.Ordinal);
            long bytes = 0;
            foreach (var pair in before)
            {
                Require(Path.IsPathFullyQualified(pair.Key) && pair.Key == Path.GetFullPath(pair.Key) &&
                    Regex.IsMatch(pair.Value.Sha256, "^[0-9A-Fa-f]{64}$") &&
                    string.Equals(pair.Value.Sha256, after[pair.Key], StringComparison.OrdinalIgnoreCase),
                    "Accepted canonical terminal input identity differs.");
                var snapshot = Path.GetFullPath(pair.Value.Snapshot);
                Require(snapshot == pair.Value.Snapshot && Path.GetDirectoryName(snapshot) == Path.Combine(repository, run + "retention") &&
                    snapshots.Add(snapshot), "Accepted canonical snapshot is outside authentic retention or duplicate.");
                Capture(pair.Key, pair.Value.Sha256);
                var retained = Capture(snapshot, pair.Value.Sha256);
                bytes = checked(bytes + new FileInfo(retained).Length);
            }
            Require(bytes == 27546690, "Accepted canonical physical snapshot bytes differ.");
            foreach (var input in new[] { transition.BaselineDescriptor, transition.CurrentDescriptor,
                new Input(Path.GetFullPath(manifest), "48dcf621aab069c222402e84dc948d62dfcad8fb729cfdb2364492967fa12b6f") })
                Require(before.TryGetValue(input.Path, out var binding) && binding.Sha256.Equals(input.Sha256, StringComparison.OrdinalIgnoreCase),
                    "Accepted canonical invocation input is absent from native retention.");
            return resultDocument;
        }
        catch
        {
            resultDocument.Dispose();
            throw;
        }
    }

    private static void AuthenticateRetirement(string run, string nativeParent, string sourceHash)
    {
        using var document = ReadJson(new Input(Path.Combine(repository, run + "parent-retirement.json"),
            "22f03d6081e102524a5f9ae52d265026be9c5e8cd9f524a418992e02ef584af4"));
        var retired = document.RootElement;
        var destination = Path.Combine(repository, run + "environment-retired");
        Require(retired.GetProperty("Status").GetString() == "verified-owned-parent-retirement" &&
            !retired.GetProperty("SdkDispatched").GetBoolean() && retired.GetProperty("Parent").GetString() == nativeParent &&
            retired.GetProperty("ParentAbsent").GetBoolean() && retired.GetProperty("Destination").GetString() == destination &&
            retired.GetProperty("SourcesUnchanged").GetBoolean() && retired.GetProperty("IndexUnchanged").GetBoolean() &&
            retired.GetProperty("OrdinaryBinariesUnchanged").GetInt32() == 336 && retired.GetProperty("ReviewedInputsPreserved").GetBoolean() &&
            retired.GetProperty("NoOriginalLiveSessions").GetBoolean(), "Accepted canonical retirement metadata differs.");
        var originals = retired.GetProperty("OriginalFiles");
        var retained = retired.GetProperty("RetainedFiles");
        var mappings = retired.GetProperty("FilenameMappings");
        Require(originals.EnumerateObject().Count() == 51 && retained.EnumerateObject().Count() == 51 &&
            mappings.EnumerateObject().Count() == 12, "Accepted canonical retirement inventory scope differs.");
        var targets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var original in originals.EnumerateObject())
        {
            var target = mappings.TryGetProperty(original.Name, out var mapping) ? mapping.GetString() : original.Name;
            Require(target is not null && !Path.IsPathRooted(original.Name) && !Path.IsPathRooted(target) &&
                Path.GetRelativePath(destination, Path.GetFullPath(Path.Combine(destination, original.Name))).Replace('\\', '/') == original.Name &&
                Path.GetRelativePath(destination, Path.GetFullPath(Path.Combine(destination, target))).Replace('\\', '/') == target &&
                !original.Name.StartsWith("../", StringComparison.Ordinal) && !target.StartsWith("../", StringComparison.Ordinal) &&
                targets.Add(target) && retained.TryGetProperty(target, out var actual) && SameJson(original.Value, actual),
                "Accepted canonical retirement mapping or recorded identity differs.");
        }
        Require(targets.SetEquals(retained.EnumerateObject().Select(item => item.Name)) &&
            mappings.EnumerateObject().All(item => originals.TryGetProperty(item.Name, out _)),
            "Accepted canonical retirement inventory has an unbound entry.");
        const string logicalSource = "qualifier/Program.cs";
        const string retainedSource = "qualifier/Program.cs.snapshot";
        Require(mappings.GetProperty(logicalSource).GetString() == retainedSource &&
            originals.GetProperty(logicalSource).GetProperty("Sha256").GetString() == sourceHash &&
            originals.GetProperty(logicalSource).GetProperty("Bytes").GetInt64() == 49388,
            "Accepted canonical retired producer source identity differs.");
        var captured = Capture(Path.Combine(destination, retainedSource), sourceHash);
        Require(new FileInfo(captured).Length == 49388, "Accepted canonical producer source length differs.");
        // The pinned inventory authenticates historical retirement metadata. Only
        // consumed source/evidence files are captured; no whole-tree rehash is claimed.
    }


}
