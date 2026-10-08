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

internal static class NullableColumnContinuity
{
    private const long MaximumBytes = 16 * 1024 * 1024;
    private static readonly XNamespace Trx = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
    private static readonly Guid Sha256Algorithm = new("8829d00f-11b8-4213-878b-770e8597ac16");
    private static string repository = string.Empty;
    private const string BaselineHash = "66ef50d8b67c6bbf71a6dcb82f0479133ee292c05e0ddef7c3ce9a2b8583c415";
    private const string FormatterPath = "IQToolkit.Data.SQLite/SQLiteFormatter.cs";
    private const string OldRetention = "artifacts/canonical-publication/qualify.55018kq5/retention/";
    private const string TestSourcePath = "IQToolkitTest/RealProvider/NativeStringColumnInequalityTests.cs";
    private const string TestSourceHash = "d7a9e7e5befd7defbef13567b69536c2aa6c72cef8a2263b8c4eaa534bf1678c";
    private const string TestClass = "IQToolkitTest.RealProvider.NativeStringColumnInequalityTests";
    private static readonly string[] AddedMethods =
    [
        "NativeStringColumnInequalityFiltersNullRow", "NativeStringColumnsPreserveProjectionAndLiteralNull",
        "NativeStringColumnInequalityProjectsClrResult", "NativeStringColumnInequalityPreservesAcquiredControls"
    ];
    private static readonly SourceChange[] SourceChanges =
    [
        new(FormatterPath,
            "7c694080670dd15ed8ba0327b92516f32619893a91b878b523ec7810de84a1c0",
            "7f4180c30a777d6b10de21bf102c4b59d423b81e4735ac6c394788c81eaf6918")
    ];
    private const string InsertedGuard =
        "            else if (b.NodeType == ExpressionType.NotEqual\n" +
        "                && b.Left is ColumnExpression && b.Left.Type == typeof(string)\n" +
        "                && b.Right is ColumnExpression && b.Right.Type == typeof(string)\n" +
        "                && b.Method != null && b.Method.DeclaringType == typeof(string)\n" +
        "                && b.Method.Name == \"op_Inequality\")\n" +
        "            {\n" +
        "                return \"IS NOT\";\n" +
        "            }\n";
    private static Dictionary<string, Input> PredecessorPins = new(StringComparer.Ordinal);
    private static Dictionary<string, Input> BaselineBindings = new(StringComparer.Ordinal);
    private static bool baselineContext;

    private sealed record SourceChange(string Path, string BeforeHash, string AfterHash);
    private sealed record Transition(int Schema, Input BaselineDescriptor, Input CurrentDescriptor,
        Dictionary<string, Input> BaselineBindings, Input TestSource, Input FocusedTrx, Input CurrentStrictTrx,
        Dictionary<string, Input> PredecessorAnchors);
    private sealed record SequenceLocation(int Offset, int StartLine, int EndLine);
    private sealed record OperatorScope(string Source, string SourceSha256, string Class, string Method,
        string Signature, int MethodToken, int ReturnLine, SequenceLocation[] SequencePoints);
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

    // The coordinator owns capture limits, terminal rehashing and publication.
    internal static object Validate(string repositoryRoot, string baselinePath, string currentPath, string manifest)
    {
        Require(Path.IsPathFullyQualified(repositoryRoot) && Path.GetFullPath(repositoryRoot) == repositoryRoot,
            "Repository root must be an absolute normalized path.");
        repository = repositoryRoot;
        baselineContext = false;
        PredecessorPins = FixedPredecessorPins();
        BaselineBindings = FixedBindings();
        var transition = ReadTransition(manifest, baselinePath, currentPath);
        using var nativeProducer = AuthenticateNativeProducer(transition, manifest);
        VerifyPredecessor(transition.PredecessorAnchors);
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
        var pdbScope = VerifyOperatorScope(current.Descriptor.Modules.Single(module => module.Name == "IQToolkit.Data.SQLite"));
        var additions = Compare(baseline, current, pdbScope);
        var transitionReceipt = new
        {
            Contract = "SQLITE-STRING-COLUMN-COVERAGE-1", OldTests = 431, CurrentTests = 435,
            OldPassed = 106, CurrentPassed = 110, ExplicitSqlSkips = 325, Translations = 247,
            AddedTests = AddedMethods.Select(method => TestClass + "." + method).ToArray(),
            AddedPdbDocument = TestSourcePath, TestSourceSha256 = TestSourceHash,
            BaselineBindings = BaselineBindings.Count, ChangedProductionSources = SourceChanges.Length,
            ComparedInheritedLeaves = 16724, CurrentLeaves = current.Leaves.Count, AddedLeafCount = additions.Count,
            AddedLeaves = additions.OrderBy(pair => pair.Key.Method, StringComparer.Ordinal).ThenBy(pair => pair.Key.Line)
                .Select(pair => new { pair.Key.Module, pair.Key.Source, pair.Key.Class, pair.Key.Method,
                    pair.Key.Signature, pair.Key.Line, Leaf = pair.Value }).ToArray(),
            PdbScope = pdbScope, PredecessorRetainedInputs = 195, PredecessorRetainedBytes = 27546690,
            PredecessorDescriptorSha256 = BaselineHash,
            PredecessorQualificationSha256 = PredecessorPins["artifacts/canonical-publication/qualify.55018kq5/result.json"].Sha256,
            PermanentBaselineSha256 = PredecessorPins["artifacts/coverage/runs/run.TFv2whz3/descriptor.json"].Sha256,
            FullGoalComplete = false, CoverageTargetsVerified = false, IndividualBranchArmsVerified = false
        };
        var outcome = new { Status = "verified", MeasuredModules = current.Modules, current.Total,
            SourceFiles = current.Sources.Count, ComparedLeaves = current.Leaves.Count,
            IndividualBranchArmsVerified = false, CoverageTargetsVerified = false,
            FullGoalComplete = false, OldTests = 431, CurrentTests = 435, AddedTests = 4,
            ComparedInheritedLeaves = 16724, AddedLeafCount = additions.Count };
        var native = nativeProducer.RootElement.GetProperty("NativeComparison");
        Require(SameJson(JsonSerializer.SerializeToElement(transitionReceipt), native.GetProperty("Transition")) &&
            SameJson(JsonSerializer.SerializeToElement(outcome), native.GetProperty("Outcome")),
            "Reconstructed nullable comparison differs from the accepted native outcome or receipt.");
        return new
        {
            Status = "verified", Operation = "nullable-continuity-control", Transition = transitionReceipt, Outcome = outcome,
            NativeProducerAuthenticated = true, CurrentComparisonVerified = true, CurrentFinalAccepted = false,
            FullGoalComplete = false, CoverageTargetsVerified = false, IndividualBranchArmsVerified = false
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
        return Program.CaptureBound(path, expected);
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

    private static Dictionary<Key, Leaf> Compare(Measurement baseline, Measurement current, OperatorScope scope)
    {
        Require(baseline.Descriptor.Settings.Sha256.Equals(current.Descriptor.Settings.Sha256, StringComparison.OrdinalIgnoreCase) &&
            baseline.Descriptor.Collector.Sha256.Equals(current.Descriptor.Collector.Sha256, StringComparison.OrdinalIgnoreCase) &&
            baseline.Descriptor.Mapping.Sha256.Equals(current.Descriptor.Mapping.Sha256, StringComparison.OrdinalIgnoreCase), "Collector, settings or source mapping changed; explicit qualification required.");
        foreach (var module in baseline.Descriptor.Modules.Append(baseline.Descriptor.TestAssembly))
        {
            var matching = current.Descriptor.Modules.Append(current.Descriptor.TestAssembly).Single(item => item.Name == module.Name);
            var sameDll = module.Dll.Sha256.Equals(matching.Dll.Sha256, StringComparison.OrdinalIgnoreCase);
            var samePdb = module.Pdb.Sha256.Equals(matching.Pdb.Sha256, StringComparison.OrdinalIgnoreCase);
            Require(module.Name is "IQToolkit.Data.SQLite" or "IQToolkitTest" ? !sameDll && !samePdb : sameDll && samePdb,
                "Compiled module transition differs from the reviewed SQLite/Test change.");
            if (module.Name == "IQToolkit.Data.SQLite")
                Require(matching.Dll.Sha256.Equals("348cd7ffce9a61169f9d851aa420e1a5a97c294d93abfbd68a8a5071dc4aff83", StringComparison.OrdinalIgnoreCase) &&
                    matching.Pdb.Sha256.Equals("e7dbe3eb4ea7b956a7853d828cfcada36609f1bfe02837e69182906491a13427", StringComparison.OrdinalIgnoreCase),
                    "Current SQLite pair differs from the reviewed native GREEN/strict outputs.");
            if (module.Name == "IQToolkitTest")
                Require(matching.Dll.Sha256.Equals("ca225d10823478c23aa3c42c8046ba938fedafc577707c968bfea50ab56a30d6", StringComparison.OrdinalIgnoreCase) &&
                    matching.Pdb.Sha256.Equals("e53140911e6f43a9e2b9f8aafe159dca25195336d61083a493c67fc517d4e487", StringComparison.OrdinalIgnoreCase),
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
                "Measured source differs from the fixed formatter transition.");
        }
        Require(SourceChanges.All(change => baseline.Sources.ContainsKey(change.Path)), "Changed sources were not measured.");
        CompareDocuments(baseline, current);
        Require(baseline.Leaves.Count == 16724, "Inherited coverage leaf denominator changed.");
        var inherited = new HashSet<Key>();
        foreach (var pair in baseline.Leaves)
        {
            var mapped = Map(pair.Key);
            Require(inherited.Add(mapped), "Inherited leaf mapping is not injective.");
            Require(current.Leaves.TryGetValue(mapped, out var leaf) && leaf.Branches == pair.Value.Branches,
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
        var added = current.Leaves.Where(pair => !inherited.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
        Require(added.Count > 0 && inherited.Count == 16724 && current.Leaves.Count == inherited.Count + added.Count,
            "Current leaf scope does not retain every inherited leaf plus observed additions.");
        foreach (var pair in added)
        {
            var key = pair.Key;
            Require(key.Module == "IQToolkit.Data.SQLite" && key.Source == FormatterPath &&
                key.Class == "IQToolkit.Data.SQLite.SQLiteFormatter" && key.Line is >= 502 and <= 509 &&
                ((key.Method.Length == 0 && key.Signature.Length == 0) ||
                    (key.Method == "GetOperator" && key.Signature == "(System.Linq.Expressions.BinaryExpression)")) &&
                scope.SequencePoints.Any(point => point.StartLine <= key.Line && point.EndLine >= key.Line),
                "Added leaf escapes the PDB-authenticated new GetOperator guard.");
            var corresponding = key.Method.Length == 0
                ? key with { Method = "GetOperator", Signature = "(System.Linq.Expressions.BinaryExpression)" }
                : key with { Method = string.Empty, Signature = string.Empty };
            Require(added.TryGetValue(corresponding, out var other) && SameLeaf(pair.Value, other),
                "Added method and corresponding class measurements differ.");
        }
        Require(added.Any(pair => pair.Key.Method == "GetOperator" && pair.Key.Line == scope.ReturnLine && pair.Value.Covered),
            "The PDB-bound IS NOT return did not execute.");
        var addedClass = added.Where(pair => pair.Key.Method.Length == 0).Select(pair => pair.Value).ToArray();
        Require(current.Total.Lines == baseline.Total.Lines + addedClass.Length &&
            current.Total.Branches == baseline.Total.Branches + addedClass.Sum(leaf => leaf.Branches),
            "Current denominators differ from inherited scope plus observed new class leaves.");
        return added;
    }

    private static Dictionary<string, Input> FixedBindings()
    {
        var bindings = new Dictionary<string, Input>(StringComparer.Ordinal);
        void Add(string logical, string snapshot, string hash) =>
            bindings.Add(Path.Combine(repository, logical), new Input(Path.Combine(repository, OldRetention + snapshot), hash));
        Add("IQToolkitTest/bin/Release/net10.0/IQToolkit.Data.SQLite.dll", "0025-IQToolkit.Data.SQLite.dll.snapshot",
            "5412519419aab722196937753ea63c058145f5b030947149bca61d6142f69871");
        Add("IQToolkitTest/bin/Release/net10.0/IQToolkit.Data.SQLite.pdb", "0026-IQToolkit.Data.SQLite.pdb.snapshot",
            "12d9d8f6427fed0a36408aec9386c19ad7c721853641cebd43503001145cb473");
        Add("IQToolkitTest/bin/Release/net10.0/IQToolkitTest.dll", "0189-IQToolkitTest.dll.snapshot",
            "bd7df8c454cc29b85962c791045533833fba2681b50517415882594e0ed6925a");
        Add("IQToolkitTest/bin/Release/net10.0/IQToolkitTest.pdb", "0190-IQToolkitTest.pdb.snapshot",
            "5397e459e73f56a21f11ceb657a823bb7f5979cbc71c9fe814bd87e4b87b0784");
        Add(FormatterPath, "0140-SQLiteFormatter.cs.snapshot", SourceChanges[0].BeforeHash);
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
                new[] { "Schema", "BaselineDescriptor", "CurrentDescriptor", "BaselineBindings", "TestSource", "FocusedTrx", "CurrentStrictTrx", "PredecessorAnchors" }.Order(StringComparer.Ordinal)),
            "Transition field scope differs.");
        var transition = document.Deserialize<Transition>() ?? throw new InvalidDataException("Missing transition.");
        Require(transition.Schema == 1 && transition.BaselineBindings is not null && transition.BaselineBindings.Count == 5,
            "Unsupported transition schema or binding count.");
        foreach (var input in new[] { transition.BaselineDescriptor, transition.CurrentDescriptor, transition.TestSource,
            transition.FocusedTrx, transition.CurrentStrictTrx }) ValidateInput(input);
        Require(transition.BaselineDescriptor.Path == Path.Combine(repository, "artifacts/canonical-publication/coverage.3w2as6e8/collection/descriptor.json") &&
            transition.BaselineDescriptor.Path == Path.GetFullPath(baselinePath) &&
            transition.BaselineDescriptor.Sha256.Equals(BaselineHash, StringComparison.OrdinalIgnoreCase) &&
            transition.CurrentDescriptor.Path == Path.GetFullPath(currentPath) &&
            transition.CurrentDescriptor.Path == Path.Combine(repository, "artifacts/nullable-column/coverage.843ik22m/collection/descriptor.json") &&
            transition.CurrentDescriptor.Sha256.Equals("378b60b0e4dd4e189583fbd23e64a8b0d62fa59951ae763e92253784288613bf", StringComparison.OrdinalIgnoreCase),
            "Descriptor identity differs from the sealed transition.");
        Require(transition.TestSource.Path == Path.Combine(repository, TestSourcePath) &&
            transition.TestSource.Sha256.Equals(TestSourceHash, StringComparison.OrdinalIgnoreCase), "Added test source differs.");
        Require(transition.FocusedTrx.Path == Path.Combine(repository, "artifacts/nullable-column/green.8do5vhwy/results/behavior.trx") &&
            transition.FocusedTrx.Sha256.Equals("fc5770c7ef8d02937bf6447926ce0fd218ff2b960eae4ff8c419d0c0df50b65a", StringComparison.OrdinalIgnoreCase),
            "Focused native GREEN identity differs.");
        Require(transition.CurrentStrictTrx.Path == Path.Combine(repository, "artifacts/verification/runs/run.EeJwUVqF/tests.trx") &&
            transition.CurrentStrictTrx.Sha256.Equals("6caf2665d22583ecaa5106318b32317f4ffa4ab826d66d8cdc20522f7a6580a6", StringComparison.OrdinalIgnoreCase),
            "Reviewed current strict identity differs.");
        foreach (var pair in BaselineBindings)
        {
            Require(transition.BaselineBindings.TryGetValue(pair.Key, out var actual) && actual is not null &&
                actual.Path == pair.Value.Path && string.Equals(actual.Sha256, pair.Value.Sha256, StringComparison.OrdinalIgnoreCase),
                "Baseline binding origin or hash differs.");
            ValidateInput(actual!);
        }
        Require(transition.PredecessorAnchors is not null && transition.PredecessorAnchors.Count == PredecessorPins.Count,
            "Predecessor anchor scope differs.");
        foreach (var pair in PredecessorPins)
        {
            Require(transition.PredecessorAnchors.TryGetValue(pair.Key, out var actual) && actual is not null &&
                actual.Path == pair.Value.Path && string.Equals(actual.Sha256, pair.Value.Sha256, StringComparison.OrdinalIgnoreCase),
                "Predecessor anchor path or hash differs.");
            ValidateInput(actual!);
        }
        return transition;
    }

    private static void VerifySourceChanges()
    {
        var change = SourceChanges[0];
        var before = File.ReadAllBytes(Capture(BaselineBindings[Path.Combine(repository, change.Path)]));
        var after = File.ReadAllBytes(Capture(Path.Combine(repository, change.Path), change.AfterHash));
        var offset = 0;
        var lines = 0;
        while (offset < before.Length && lines < 501)
        {
            if (before[offset] == (byte)'\n') lines++;
            offset++;
        }
        Require(lines == 501 && offset == 18996 && InsertedGuard.Count(character => character == '\n') == 8,
            "Original formatter or fixed eight-line insertion differs.");
        var insertion = Encoding.UTF8.GetBytes(InsertedGuard);
        var expected = before.AsSpan(0, offset).ToArray().Concat(insertion).Concat(before.AsSpan(offset).ToArray()).ToArray();
        Require(expected.AsSpan().SequenceEqual(after), "Formatter differs from the exact eight-line insertion.");
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
                var change = module.Key == "IQToolkit.Data.SQLite" ? SourceChanges.SingleOrDefault(item => item.Path == document.Key) : null;
                Require(next.TryGetValue(document.Key, out var hash) && (change is null ? hash == document.Value :
                    document.Value.Equals(change.BeforeHash, StringComparison.OrdinalIgnoreCase) &&
                    hash!.Equals(change.AfterHash, StringComparison.OrdinalIgnoreCase)), "Inherited PDB source checksum changed unexpectedly.");
            }
            if (module.Key == "IQToolkit.Data.SQLite")
                Require(SourceChanges.All(change => module.Value.ContainsKey(change.Path)), "Changed SQLite document is absent.");
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
        Require(old.Count == 431 && old.Values.Count(item => item.Outcome == "Passed") == 106 &&
            old.Values.Count(item => item.Outcome == "NotExecuted") == 325 && previousCollected.Count == old.Count &&
            old.All(pair => previousCollected.TryGetValue(pair.Key, out var actual) && actual == pair.Value), "Baseline suite identity differs.");
        Require(next.Count == 435 && next.Values.Count(item => item.Outcome == "Passed") == 110 &&
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

    private static Dictionary<string, Input> FixedPredecessorPins() => new(StringComparer.Ordinal)
    {
        ["artifacts/canonical-publication/coverage.3w2as6e8/collection/descriptor.json"] = new(Path.Combine(repository, "artifacts/canonical-publication/coverage.3w2as6e8/collection/descriptor.json"), "66ef50d8b67c6bbf71a6dcb82f0479133ee292c05e0ddef7c3ce9a2b8583c415"),
        ["artifacts/canonical-publication/qualify.55018kq5/result.json"] = new(Path.Combine(repository, "artifacts/canonical-publication/qualify.55018kq5/result.json"), "15a5e74d9566e326047b3432f51ebc8a519f33eb6118d48efb09d75831cb8626"),
        ["artifacts/canonical-publication/qualify.55018kq5/environment-retired/qualifier/Program.cs.snapshot"] = new(Path.Combine(repository, "artifacts/canonical-publication/qualify.55018kq5/environment-retired/qualifier/Program.cs.snapshot"), "3d4ca25f23159be70303bde6d6544badfac383447f6e35b0ee772c8d27da9e0a"),
        ["artifacts/canonical-publication/qualify.55018kq5/retention/transition.json"] = new(Path.Combine(repository, "artifacts/canonical-publication/qualify.55018kq5/retention/transition.json"), "41a659abbc72556948c589a47385915b7694ce5cd2de9ce9e904065f04cbc3b7"),
        ["artifacts/canonical-publication/qualify.55018kq5/retention/inputs-before.json"] = new(Path.Combine(repository, "artifacts/canonical-publication/qualify.55018kq5/retention/inputs-before.json"), "79a7eb5a9e93a2a0cc569fbf7693911f2e8681627e602a3e14628df216cd7ac0"),
        ["artifacts/canonical-publication/qualify.55018kq5/retention/inputs-after.json"] = new(Path.Combine(repository, "artifacts/canonical-publication/qualify.55018kq5/retention/inputs-after.json"), "42ca4653537fffa8a14f7d857c1229865ed99a0ec0bbba8009ff6f5f128c4081"),
        ["artifacts/coverage/runs/run.TFv2whz3/descriptor.json"] = new(Path.Combine(repository, "artifacts/coverage/runs/run.TFv2whz3/descriptor.json"), "9bc0eb52ce5d50ba9cb9e89744851ed4e75c4d7aa1e589fc2772f6a6b8567d35"),
    };

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

    private static void VerifyPredecessor(Dictionary<string, Input> anchors)
    {
        const string prior = "artifacts/canonical-publication/qualify.55018kq5/";
        using var resultDocument = ReadJson(anchors[prior + "result.json"]);
        using var transitionDocument = ReadJson(anchors[prior + "retention/transition.json"]);
        using var beforeDocument = ReadJson(anchors[prior + "retention/inputs-before.json"]);
        using var afterDocument = ReadJson(anchors[prior + "retention/inputs-after.json"]);
        var result = resultDocument.RootElement;
        Require(result.GetProperty("Status").GetString() == "passed" && !result.GetProperty("Cancelled").GetBoolean() &&
            result.GetProperty("CleanupErrors").GetArrayLength() == 0 && result.GetProperty("StorageErrors").GetArrayLength() == 0 &&
            result.GetProperty("RootSourcesUnchanged").GetBoolean() && result.GetProperty("ReviewedInputsUnchanged").GetBoolean() &&
            result.GetProperty("RootIndexUnchanged").GetBoolean() && result.GetProperty("OrdinaryBinariesClassified").GetBoolean() &&
            result.GetProperty("OrdinaryBinariesUnchanged").GetInt32() == 336 && result.GetProperty("OrdinaryBinaryDeltaPaths").GetArrayLength() == 0 &&
            result.GetProperty("CoverageQualified").GetBoolean() && result.GetProperty("ScopedOutcomeVerified").GetBoolean(),
            "Predecessor producer was incomplete or failed preservation.");
        foreach (var flag in new[] { "FullGoalComplete", "CoverageTargetsVerified", "IndividualBranchArmsVerified" })
            Require(!result.GetProperty(flag).GetBoolean(), "Predecessor broad qualification differs.");
        var commands = result.GetProperty("Commands").EnumerateArray().ToArray();
        Require(commands.Select(command => command.GetProperty("Name").GetString()).SequenceEqual(
            new[] { "sdk-version", "qualifier-restore", "qualifier-build", "compare" }), "Predecessor native command scope differs.");
        foreach (var command in commands)
            Require(command.GetProperty("Exit").GetInt32() == 0 && command.GetProperty("ExpectedExit").GetInt32() == 0 &&
                command.GetProperty("RemainingLiveMembers").GetArrayLength() == 0 && command.GetProperty("SecondaryErrors").GetArrayLength() == 0 &&
                (!command.TryGetProperty("ForcedTermination", out var forced) || !forced.GetBoolean()) &&
                (!command.TryGetProperty("NativeExitUnobserved", out var unobserved) || !unobserved.GetBoolean()),
                "Predecessor native command did not complete ordinarily.");
        var argv = commands[^1].GetProperty("Argv").EnumerateArray().Select(value => value.GetString()).ToArray();
        Require(argv.Length == 6 && argv[2] == anchors["artifacts/coverage/runs/run.TFv2whz3/descriptor.json"].Path &&
            argv[3] == anchors["artifacts/canonical-publication/coverage.3w2as6e8/collection/descriptor.json"].Path &&
            argv[4] == Path.Combine(repository, OldRetention.TrimEnd('/')), "Permanent-to-predecessor invocation identity differs.");
        var native = result.GetProperty("NativeComparison");
        var outcome = native.GetProperty("Outcome");
        Require(outcome.GetProperty("Status").GetString() == "verified" && outcome.GetProperty("SourceFiles").GetInt32() == 97 &&
            outcome.GetProperty("ComparedLeaves").GetInt32() == 16724 && outcome.GetProperty("OldTests").GetInt32() == 427 &&
            outcome.GetProperty("CurrentTests").GetInt32() == 431 && outcome.GetProperty("AddedTests").GetInt32() == 4 &&
            native.GetProperty("RetainedInputs").GetInt32() == 195 && native.GetProperty("RetainedBytes").GetInt64() == 27546690 &&
            SameJson(native.GetProperty("Transition"), transitionDocument.RootElement), "Predecessor receipt/result binding differs.");
        foreach (var flag in new[] { "FullGoalComplete", "CoverageTargetsVerified", "IndividualBranchArmsVerified" })
            Require(!outcome.GetProperty(flag).GetBoolean() && !transitionDocument.RootElement.GetProperty(flag).GetBoolean(),
                "Predecessor outcome broad qualification differs.");
        foreach (var pair in new[] { ("InputBeforeSha256", "inputs-before.json"), ("InputAfterSha256", "inputs-after.json"), ("TransitionSha256", "transition.json") })
            Require(string.Equals(native.GetProperty(pair.Item1).GetString(), anchors[prior + "retention/" + pair.Item2].Sha256,
                StringComparison.OrdinalIgnoreCase), "Predecessor native receipt hash differs.");
        var before = beforeDocument.RootElement.Deserialize<Dictionary<string, RetainedInput>>() ?? throw new InvalidDataException("Missing predecessor input map.");
        var after = afterDocument.RootElement.Deserialize<Dictionary<string, string>>() ?? throw new InvalidDataException("Missing predecessor terminal map.");
        Require(before.Count == 195 && after.Count == before.Count && before.Keys.All(after.ContainsKey), "Predecessor input-map scope differs.");
        var snapshots = new HashSet<string>(StringComparer.Ordinal);
        long bytes = 0;
        baselineContext = true;
        try
        {
            foreach (var pair in before)
            {
                Require(Path.IsPathFullyQualified(pair.Key) && pair.Key == Path.GetFullPath(pair.Key) &&
                    Regex.IsMatch(pair.Value.Sha256, "^[0-9A-Fa-f]{64}$") &&
                    string.Equals(pair.Value.Sha256, after[pair.Key], StringComparison.OrdinalIgnoreCase),
                    "Predecessor terminal input identity differs.");
                var snapshot = Path.GetFullPath(pair.Value.Snapshot);
                Require(snapshot == pair.Value.Snapshot && Path.GetDirectoryName(snapshot) == Path.Combine(repository, OldRetention.TrimEnd('/')) &&
                    snapshots.Add(snapshot), "Predecessor snapshot is outside the authentic retention or duplicate.");
                Capture(pair.Key, pair.Value.Sha256);
                var retained = Capture(snapshot, pair.Value.Sha256);
                bytes = checked(bytes + new FileInfo(retained).Length);
            }
        }
        finally { baselineContext = false; }
        Require(bytes == 27546690, "Predecessor physical snapshot bytes differ.");
        foreach (var relative in new[] { "artifacts/coverage/runs/run.TFv2whz3/descriptor.json", "artifacts/canonical-publication/coverage.3w2as6e8/collection/descriptor.json" })
            Require(before.TryGetValue(anchors[relative].Path, out var binding) &&
                binding.Sha256.Equals(anchors[relative].Sha256, StringComparison.OrdinalIgnoreCase), "Permanent/predecessor descriptor is absent from native input retention.");
        Require(argv[5] is not null && before.ContainsKey(argv[5]!), "Executed predecessor transition is absent from native retention.");
    }

    private static JsonDocument AuthenticateNativeProducer(Transition transition, string manifest)
    {
        const string run = "artifacts/nullable-column/qualify.26k5mzn2/";
        const string sourceHash = "2aa1017b3f535b877e803173be970d0c59ac19c9bc399f0cde660715c62499f5";
        const string nativeParent = "/tmp/iqtoolkit-native-string-qualify.fnlehq_x";
        const string dotnet = "/home/firestrand/.dotnet/dotnet";
        var resultInput = new Input(Path.Combine(repository, run + "result.json"),
            "c6637b5ef7bac99cd998b68511bc68537ea1bd2763618e3bfd44ab30e9ca5e25");
        var receiptInput = new Input(Path.Combine(repository, run + "retention/transition.json"),
            "ddfa85f821962f2c69197a5890dc6e5ea13d74ac363c43b1bf8ac61005016876");
        var beforeInput = new Input(Path.Combine(repository, run + "retention/inputs-before.json"),
            "31673bd3b6777d33c19742521891dbb5ce23d08e70c8c353c8e2cb74532f3f96");
        var afterInput = new Input(Path.Combine(repository, run + "retention/inputs-after.json"),
            "4943582af56851884625cee992b7a662154ff7c74393d181b02ce7de78776cd3");
        Capture(new Input(Path.Combine(repository, "artifacts/nullable-column/coverage-qualifier/Program.cs"), sourceHash));
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
                result.GetProperty("CoverageQualified").GetBoolean() && result.GetProperty("ScopedOutcomeVerified").GetBoolean() &&
                result.GetProperty("OriginalParentAbsent").GetBoolean() && result.GetProperty("PostRetirementFencesVerified").GetBoolean(),
                "Accepted nullable producer was incomplete or failed preservation.");
            foreach (var flag in new[] { "FullGoalComplete", "CoverageTargetsVerified", "IndividualBranchArmsVerified" })
                Require(!result.GetProperty(flag).GetBoolean(), "Accepted nullable producer broad qualification differs.");
            var commands = result.GetProperty("Commands").EnumerateArray().ToArray();
            Require(commands.Select(command => command.GetProperty("Name").GetString()).SequenceEqual(
                new[] { "sdk-version", "qualifier-restore", "qualifier-build", "compare" }), "Accepted nullable command scope differs.");
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
                    "Accepted nullable native command or invocation did not complete ordinarily.");
                var logHash = command.GetProperty("LogSha256").GetString();
                Require(logHash is not null && Regex.IsMatch(logHash, "^[0-9A-Fa-f]{64}$"), "Accepted nullable command log hash differs.");
                var log = Capture(Path.Combine(repository, run + command.GetProperty("Name").GetString() + ".log"), logHash);
                Require(new FileInfo(log).Length == command.GetProperty("LogBytes").GetInt64(), "Accepted nullable command log length differs.");
            }
            var sessions = result.GetProperty("PreRetirementSessionScan").EnumerateArray().ToArray();
            Require(sessions.Length == commands.Length && sessions.Select(session => session.GetProperty("Session").GetInt32())
                .SequenceEqual(commands.Select(command => command.GetProperty("Pid").GetInt32())) &&
                sessions.All(session => session.GetProperty("RemainingLiveMembers").GetArrayLength() == 0),
                "Accepted nullable producer session settlement differs.");
            var native = result.GetProperty("NativeComparison");
            Require(native.GetProperty("Outcome").GetProperty("Status").GetString() == "verified" &&
                native.GetProperty("RetainedInputs").GetInt32() == 409 && native.GetProperty("RetainedBytes").GetInt64() == 62474255 &&
                SameJson(native.GetProperty("Transition"), receiptDocument.RootElement), "Accepted nullable receipt/result binding differs.");
            foreach (var flag in new[] { "FullGoalComplete", "CoverageTargetsVerified", "IndividualBranchArmsVerified" })
                Require(!native.GetProperty("Outcome").GetProperty(flag).GetBoolean() && !receiptDocument.RootElement.GetProperty(flag).GetBoolean(),
                    "Accepted nullable native outcome broad qualification differs.");
            foreach (var pair in new[] { ("InputBeforeSha256", beforeInput), ("InputAfterSha256", afterInput), ("TransitionSha256", receiptInput) })
                Require(string.Equals(native.GetProperty(pair.Item1).GetString(), pair.Item2.Sha256, StringComparison.OrdinalIgnoreCase),
                    "Accepted nullable native receipt hash differs.");
            var before = beforeDocument.RootElement.Deserialize<Dictionary<string, RetainedInput>>() ?? throw new InvalidDataException("Missing accepted nullable input map.");
            var after = afterDocument.RootElement.Deserialize<Dictionary<string, string>>() ?? throw new InvalidDataException("Missing accepted nullable terminal map.");
            Require(before.Count == 409 && after.Count == before.Count && before.Keys.All(after.ContainsKey), "Accepted nullable input-map scope differs.");
            var snapshots = new HashSet<string>(StringComparer.Ordinal);
            long bytes = 0;
            foreach (var pair in before)
            {
                Require(Path.IsPathFullyQualified(pair.Key) && pair.Key == Path.GetFullPath(pair.Key) &&
                    Regex.IsMatch(pair.Value.Sha256, "^[0-9A-Fa-f]{64}$") &&
                    string.Equals(pair.Value.Sha256, after[pair.Key], StringComparison.OrdinalIgnoreCase),
                    "Accepted nullable terminal input identity differs.");
                var snapshot = Path.GetFullPath(pair.Value.Snapshot);
                Require(snapshot == pair.Value.Snapshot && Path.GetDirectoryName(snapshot) == Path.Combine(repository, run + "retention") &&
                    snapshots.Add(snapshot), "Accepted nullable snapshot is outside authentic retention or duplicate.");
                Capture(pair.Key, pair.Value.Sha256);
                var retained = Capture(snapshot, pair.Value.Sha256);
                bytes = checked(bytes + new FileInfo(retained).Length);
            }
            Require(bytes == 62474255, "Accepted nullable physical snapshot bytes differ.");
            foreach (var input in new[] { transition.BaselineDescriptor, transition.CurrentDescriptor,
                new Input(Path.GetFullPath(manifest), "9a9e42a4ebcb84b250d0be826e1b6162d6be7f03efe7ceed063547b4fb60e23f") })
                Require(before.TryGetValue(input.Path, out var binding) && binding.Sha256.Equals(input.Sha256, StringComparison.OrdinalIgnoreCase),
                    "Accepted nullable invocation input is absent from native retention.");
            return resultDocument;
        }
        catch
        {
            resultDocument.Dispose();
            throw;
        }
    }

    private static Key Map(Key key) => key.Source == FormatterPath && key.Line >= 502 ? key with { Line = checked(key.Line + 8) } : key;

    private static bool SameLeaf(Leaf left, Leaf right) => left.Covered == right.Covered &&
        left.CoveredBranches == right.CoveredBranches && left.Branches == right.Branches && left.Conditions.SequenceEqual(right.Conditions);

    private static OperatorScope VerifyOperatorScope(Module module)
    {
        using var pe = new PEReader(new MemoryStream(File.ReadAllBytes(Capture(module.Dll))));
        using var provider = MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(File.ReadAllBytes(Capture(module.Pdb))));
        var metadata = pe.GetMetadataReader();
        var pdb = provider.GetMetadataReader();
        const string className = "IQToolkit.Data.SQLite.SQLiteFormatter";
        var types = metadata.TypeDefinitions.Where(candidateHandle =>
        {
            var candidate = metadata.GetTypeDefinition(candidateHandle);
            return metadata.GetString(candidate.Namespace) + "." + metadata.GetString(candidate.Name) == className;
        }).ToArray();
        Require(types.Length == 1, "Compiled SQLite formatter type is absent or ambiguous.");
        var methods = metadata.GetTypeDefinition(types[0]).GetMethods().Where(candidateHandle => metadata.GetString(metadata.GetMethodDefinition(candidateHandle).Name) == "GetOperator").ToArray();
        Require(methods.Length == 1, "Compiled GetOperator is absent or ambiguous.");
        var handle = methods[0];
        var method = metadata.GetMethodDefinition(handle);
        var signature = metadata.GetBlobReader(method.Signature);
        Require(signature.ReadByte() == 0x20 && signature.ReadCompressedInteger() == 1 && signature.ReadByte() == 0x0e && signature.ReadByte() == 0x12,
            "Compiled GetOperator signature differs.");
        var parameter = signature.ReadCompressedInteger();
        Require((parameter & 3) == 1 && parameter >> 2 > 0 && signature.RemainingBytes == 0 && method.RelativeVirtualAddress > 0,
            "Compiled GetOperator parameter/body differs.");
        var parameterType = metadata.GetTypeReference(MetadataTokens.TypeReferenceHandle(parameter >> 2));
        Require(metadata.GetString(parameterType.Namespace) == "System.Linq.Expressions" && metadata.GetString(parameterType.Name) == "BinaryExpression",
            "Compiled GetOperator does not accept BinaryExpression.");
        var il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes();
        Require(il is { Length: > 0 }, "Compiled GetOperator has no actual IL.");
        var debug = pdb.GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(MetadataTokens.GetRowNumber(handle)));
        var points = debug.GetSequencePoints().Where(point => !point.IsHidden).ToArray();
        Require(points.Length > 0, "Compiled GetOperator has no visible sequence points.");
        var sourceLines = File.ReadAllLines(Capture(Path.Combine(repository, FormatterPath), SourceChanges[0].AfterHash)).Length;
        foreach (var point in points)
        {
            var documentHandle = point.Document.IsNil ? debug.Document : point.Document;
            Require(!documentHandle.IsNil && point.Offset >= 0 && point.Offset < il.Length && point.StartLine > 0 && point.EndLine >= point.StartLine && point.EndLine <= sourceLines,
                "Compiled GetOperator sequence-point scope differs.");
            var document = pdb.GetDocument(documentHandle);
            Require(pdb.GetString(document.Name) == "/_/" + FormatterPath && pdb.GetGuid(document.HashAlgorithm) == Sha256Algorithm &&
                Convert.ToHexString(pdb.GetBlobBytes(document.Hash)).Equals(SourceChanges[0].AfterHash, StringComparison.OrdinalIgnoreCase),
                "GetOperator is not bound to the reviewed formatter source.");
        }
        Require(points.Any(point => point.StartLine == 508 && point.EndLine == 508), "IS NOT return lacks its actual source sequence point.");
        return new OperatorScope(FormatterPath, SourceChanges[0].AfterHash, className, "GetOperator", "(System.Linq.Expressions.BinaryExpression)",
            MetadataTokens.GetToken(handle), 508, points.Select(point => new SequenceLocation(point.Offset, point.StartLine, point.EndLine)).ToArray());
    }

}
