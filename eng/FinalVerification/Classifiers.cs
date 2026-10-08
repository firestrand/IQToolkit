using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;

namespace IQToolkit.FinalVerification;

internal static partial class Program
{
    private const string BaselineCommit = "60ddef60d4b635c4b24b78bff4327e3f3ff9ca5e";
    private static readonly XNamespace TrxNamespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
    private static readonly Guid Sha256Algorithm = new("8829d00f-11b8-4213-878b-770e8597ac16");
    private sealed record Execution(string TestId, string ExecutionId, string Outcome);
    private sealed record FileMeasurement(int CoveredLines, int Lines, int CoveredBranches, int Branches, int ClassLines);

    private static string RepositoryPath(string relative)
    {
        Require(!System.IO.Path.IsPathRooted(relative) && !relative.Contains('\\') && !relative.Split('/').Any(part => part is "" or "." or ".."), "Invalid repository-relative classifier path.");
        return System.IO.Path.Combine(repository, relative);
    }

    private static XElement Xml(string path, string? expected = null)
    {
        using var stream = File.OpenRead(Capture(path, expected));
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = Limit });
        return XDocument.Load(reader).Root ?? throw new InvalidDataException("Missing native XML root.");
    }

    private static Dictionary<string, Execution> Executions(Input input)
    {
        var root = Xml(input.Path, input.Sha256);
        Require(root.Name == TrxNamespace + "TestRun", "Unexpected TRX root.");
        var definitions = root.Element(TrxNamespace + "TestDefinitions")?.Elements(TrxNamespace + "UnitTest").ToArray() ?? [];
        var results = root.Element(TrxNamespace + "Results")?.Elements(TrxNamespace + "UnitTestResult").ToArray() ?? [];
        Require(definitions.Length > 0 && definitions.Length == results.Length, "Missing linked test executions.");
        string Attribute(XElement element, string name) => (string?)element.Attribute(name) ?? throw new InvalidDataException("Missing TRX identity.");
        Require(definitions.Select(definition => Attribute(definition, "id")).Distinct(StringComparer.Ordinal).Count() == definitions.Length &&
            results.Select(result => Attribute(result, "testId")).Distinct(StringComparer.Ordinal).Count() == results.Length &&
            results.Select(result => Attribute(result, "executionId")).Distinct(StringComparer.Ordinal).Count() == results.Length, "Duplicate TRX identity.");
        var byId = results.ToDictionary(result => Attribute(result, "testId"), StringComparer.Ordinal);
        var executions = new Dictionary<string, Execution>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            var method = definition.Element(TrxNamespace + "TestMethod") ?? throw new InvalidDataException("Missing test method binding.");
            var id = Attribute(definition, "id");
            Require(byId.TryGetValue(id, out var result), "Unlinked test result.");
            var executionId = Attribute(definition.Element(TrxNamespace + "Execution") ?? throw new InvalidDataException("Missing test execution."), "id");
            Require(executionId == Attribute(result, "executionId") && Attribute(method, "adapterTypeName") == "executor://mstestadapter/v4" &&
                System.IO.Path.GetFileName(Attribute(method, "codeBase")) == "IQToolkitTest.dll", "Test execution binding differs.");
            var key = Attribute(method, "className") + "." + Attribute(method, "name");
            Require(executions.TryAdd(key, new Execution(id, executionId, Attribute(result, "outcome"))), "Ambiguous test method execution.");
        }
        return executions;
    }

    private static Dictionary<string, string> Documents(JsonElement module)
    {
        var dll = ParseInput(module.GetProperty("Dll")); var pdb = ParseInput(module.GetProperty("Pdb"));
        var dllBytes = File.ReadAllBytes(Capture(dll.Path, dll.Sha256)); var pdbBytes = File.ReadAllBytes(Capture(pdb.Path, pdb.Sha256));
        using var pe = new PEReader(new MemoryStream(dllBytes));
        using var provider = MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(pdbBytes));
        var reader = provider.GetMetadataReader();
        var header = reader.DebugMetadataHeader ?? throw new InvalidDataException("Missing portable PDB identity.");
        var entries = pe.ReadDebugDirectory(); var codeViews = entries.Where(entry => entry.Type == DebugDirectoryEntryType.CodeView).ToArray();
        Require(header.Id.Length == 20 && codeViews.Length == 1 && codeViews[0].IsPortableCodeView, "Unsupported compiled identity.");
        var codeView = pe.ReadCodeViewDebugDirectoryData(codeViews[0]);
        Require(codeView.Age == 1 && new BlobContentId(header.Id) == new BlobContentId(codeView.Guid, codeViews[0].Stamp), "Compiled DLL/PDB identity differs.");
        var checksums = entries.Where(entry => entry.Type == DebugDirectoryEntryType.PdbChecksum).ToArray();
        Require(checksums.Length == 1 && header.IdStartOffset >= 0 && header.IdStartOffset <= pdbBytes.Length - header.Id.Length, "Missing compiled PDB checksum.");
        var checksum = pe.ReadPdbChecksumDebugDirectoryData(checksums[0]);
        Array.Clear(pdbBytes, header.IdStartOffset, header.Id.Length);
        Require(checksum.AlgorithmName == "SHA256" && checksum.Checksum.AsSpan().SequenceEqual(SHA256.HashData(pdbBytes)), "Compiled PDB checksum differs.");
        Require(pe.GetMetadataReader().GetString(pe.GetMetadataReader().GetAssemblyDefinition().Name) == module.GetProperty("Name").GetString(), "Compiled module name differs.");
        var documents = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var handle in reader.Documents)
        {
            var document = reader.GetDocument(handle); var name = reader.GetString(document.Name);
            if (!name.StartsWith("/_/", StringComparison.Ordinal)) continue;
            Require(reader.GetGuid(document.HashAlgorithm) == Sha256Algorithm, "Unsupported source checksum.");
            var relative = name[3..]; RepositoryPath(relative);
            Require(documents.TryAdd(relative, Convert.ToHexString(reader.GetBlobBytes(document.Hash))), "Duplicate compiled source document.");
        }
        return documents;
    }

    private static void BindMethod(JsonElement test, JsonElement module, HashSet<string> testSources)
    {
        var dll = ParseInput(module.GetProperty("Dll")); var pdb = ParseInput(module.GetProperty("Pdb"));
        using var pe = new PEReader(File.OpenRead(Capture(dll.Path, dll.Sha256)));
        using var provider = MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(Capture(pdb.Path, pdb.Sha256)));
        var metadata = pe.GetMetadataReader(); var symbols = provider.GetMetadataReader();
        var className = test.GetProperty("Class").GetString()!; var methodName = test.GetProperty("Method").GetString()!;
        var methods = metadata.TypeDefinitions.Select(metadata.GetTypeDefinition)
            .Where(type => metadata.GetString(type.Namespace) + "." + metadata.GetString(type.Name) == className)
            .SelectMany(type => type.GetMethods()).Where(handle => metadata.GetString(metadata.GetMethodDefinition(handle).Name) == methodName).ToArray();
        Require(methods.Length == 1 && metadata.GetMethodDefinition(methods[0]).RelativeVirtualAddress > 0, "Missing or ambiguous compiled test method.");
        var debug = symbols.GetMethodDebugInformation(methods[0]);
        var documentHandles = debug.GetSequencePoints().Select(point => point.Document.IsNil ? debug.Document : point.Document).Where(handle => !handle.IsNil).Distinct();
        Require(documentHandles.Select(handle => symbols.GetString(symbols.GetDocument(handle).Name))
            .Any(name => name.StartsWith("/_/", StringComparison.Ordinal) && testSources.Contains(name[3..])), "Compiled test body does not bind its reviewed source.");
    }

    private static bool ClassifyFacts(JsonElement descriptor, string consumerDirectory, string apiDirectory)
    {
        using var bindings = Read(RepositoryPath("eng/FinalVerification/fact-bindings.json")); var manifest = bindings.RootElement;
        Require(manifest.GetProperty("Schema").GetInt32() == 1 && manifest.GetProperty("Contract").GetString() == "FINAL-CLASSIFICATION-1", "Unsupported fact bindings.");
        var ledger = manifest.GetProperty("Ledger");
        Require(ledger.GetProperty("Path").GetString() == "docs/modernization-implementation-plan.md" && ledger.GetProperty("RowCount").GetInt32() == 34, "Unexpected fact ledger scope.");
        var rows = File.ReadAllLines(Capture(RepositoryPath(ledger.GetProperty("Path").GetString()!)))
            .Where(row => Regex.IsMatch(row, @"^\| [A-Z][A-Z0-9.]+\.v1 \|", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))).ToArray();
        var facts = manifest.GetProperty("Facts").EnumerateArray().ToArray();
        Require(rows.Length == 34 && facts.Length == rows.Length, "Fact ledger omission or scope change.");
        var strictInput = ParseInput(descriptor.GetProperty("StrictTrx")); var coverageInput = ParseInput(descriptor.GetProperty("CoverageTrx"));
        Require(strictInput.Path != coverageInput.Path, "Strict and coverage executions must be distinct invocations.");
        var strict = Executions(strictInput); var coverage = Executions(coverageInput);
        var testAssembly = descriptor.GetProperty("TestAssembly");
        var testDocuments = Documents(testAssembly);
        var productionDocuments = descriptor.GetProperty("Modules").EnumerateArray().Select(Documents).ToArray();
        var output = new List<object>(); bool complete = true;
        for (int i = 0; i < rows.Length; i++)
        {
            var fact = facts[i]; var columns = rows[i].Split('|').Select(column => column.Trim()).ToArray();
            Require(columns.Length == 6, "Ambiguous ledger row.");
            var kindAndLifecycle = columns[2].Split(" / ", 2, StringSplitOptions.None);
            Require(kindAndLifecycle.Length == 2 && fact.GetProperty("FactId").GetString() == columns[1] &&
                fact.GetProperty("Kind").GetString() == kindAndLifecycle[0] && fact.GetProperty("Lifecycle").GetString() == kindAndLifecycle[1] &&
                fact.GetProperty("Claim").GetString() == columns[3] && fact.GetProperty("EvidenceText").GetString() == columns[4] &&
                fact.GetProperty("RawRowSha256").GetString()!.Equals(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rows[i]))), StringComparison.OrdinalIgnoreCase), "Fact claim, lifecycle or evidence changed.");
            var evidenceIds = Regex.Matches(columns[4], @"\bEV-[A-Z0-9-]+\b", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)).Select(match => match.Value).Distinct(StringComparer.Ordinal);
            Require(evidenceIds.SequenceEqual(fact.GetProperty("EvidenceIds").EnumerateArray().Select(value => value.GetString()!)), "Fact evidence identifiers differ.");
            var qualification = fact.GetProperty("Qualification").GetString(); var due = fact.GetProperty("DuePhase").GetString();
            Require(due == (columns[1] == "ASYNC.EXECUTION.v1" ? "M1" : "M0") && qualification is "scoped-current-tests" or "independent-package-producer" or "native-package-consumer-producer" or "unqualified" or "not-due", "Unreviewed fact disposition.");
            var tests = fact.GetProperty("Tests").EnumerateArray().ToArray();
            var reason = fact.GetProperty("Reason").GetString(); Require(!string.IsNullOrWhiteSpace(reason), "Missing fact scope reason.");
            if (qualification == "independent-package-producer")
            {
                Require(columns[1] == "PACKAGE.INDEPENDENT.v1" && due == "M0" && tests.Length == 0 &&
                    !kindAndLifecycle[1].Contains("Proposed", StringComparison.Ordinal), "Unreviewed producer-backed fact scope.");
                var producerDependencies = fact.GetProperty("Dependencies").EnumerateArray().ToArray();
                Require(producerDependencies.Length == 1 && producerDependencies[0].GetProperty("Path").GetString() == "eng/FinalVerification/PackageIndependentEvidence.cs" &&
                    producerDependencies[0].GetProperty("Role").GetString() == "producer-verifier-source", "Unreviewed producer verifier dependency.");
                var source = RepositoryPath(producerDependencies[0].GetProperty("Path").GetString()!);
                var expected = producerDependencies[0].GetProperty("Sha256").GetString()!;
                Capture(source, expected);
                var proof = ValidateIndependentPackageProducer(consumerDirectory, apiDirectory);
                var boundSource = ParseInput(JsonSerializer.SerializeToElement(proof).GetProperty("VerifierSource"));
                Require(boundSource.Path == source && boundSource.Sha256.Equals(expected, StringComparison.OrdinalIgnoreCase),
                    "Producer verifier is not the reviewed compilation.");
                output.Add(new { FactId = columns[1], Lifecycle = kindAndLifecycle[1], DuePhase = due,
                    State = "executed-producer", Reason = reason, ProducerEvidence = proof, Dependencies = producerDependencies });
                continue;
            }
            if (qualification == "native-package-consumer-producer")
            {
                Require(columns[1] == "PACKAGE.NATIVE.CONSUMER.v1" && due == "M0" && tests.Length == 0 &&
                    kindAndLifecycle[1] == "Observed scoped; phase gate open", "Unreviewed native consumer fact scope.");
                var nativeDependencies = fact.GetProperty("Dependencies").EnumerateArray().ToArray();
                Require(nativeDependencies.Length == 2 &&
                    nativeDependencies[0].GetProperty("Path").GetString() == "eng/FinalVerification/PackageNativeConsumerEvidence.cs" &&
                    nativeDependencies[0].GetProperty("Role").GetString() == "producer-verifier-source" &&
                    nativeDependencies[1].GetProperty("Path").GetString() == "eng/FinalVerification/PackageIndependentEvidence.cs" &&
                    nativeDependencies[1].GetProperty("Role").GetString() == "producer-shared-source", "Unreviewed native consumer source dependencies.");
                var nativeSource = RepositoryPath(nativeDependencies[0].GetProperty("Path").GetString()!);
                var nativeExpected = nativeDependencies[0].GetProperty("Sha256").GetString()!;
                var nativeSharedSource = RepositoryPath(nativeDependencies[1].GetProperty("Path").GetString()!);
                var nativeSharedExpected = nativeDependencies[1].GetProperty("Sha256").GetString()!;
                Capture(nativeSource, nativeExpected); Capture(nativeSharedSource, nativeSharedExpected);
                var nativeProof = ValidateNativePackageConsumer(consumerDirectory, apiDirectory);
                var nativeReceipt = JsonSerializer.SerializeToElement(nativeProof);
                var nativeBoundSource = ParseInput(nativeReceipt.GetProperty("VerifierSource"));
                var nativeBoundShared = ParseInput(nativeReceipt.GetProperty("SharedVerifierSource"));
                Require(nativeReceipt.GetProperty("Status").GetString() == "verified" &&
                    nativeReceipt.GetProperty("Contract").GetString() == "PACKAGE-NATIVE-CONSUMER-BINDING-1" &&
                    nativeBoundSource.Path == nativeSource && nativeBoundSource.Sha256.Equals(nativeExpected, StringComparison.OrdinalIgnoreCase) &&
                    nativeBoundShared.Path == nativeSharedSource && nativeBoundShared.Sha256.Equals(nativeSharedExpected, StringComparison.OrdinalIgnoreCase),
                    "Native consumer verifier is not the reviewed compilation.");
                output.Add(new { FactId = columns[1], Lifecycle = kindAndLifecycle[1], DuePhase = due,
                    State = "executed-producer", Reason = reason, ProducerEvidence = nativeProof, Dependencies = nativeDependencies });
                continue;
            }
            if (qualification != "scoped-current-tests")
            {
                Require(tests.Length == 0 && (qualification == "not-due") == (due == "M1"), "Unqualified fact cannot supply current proof.");
                if (due == "M0") complete = false;
                output.Add(new { FactId = columns[1], Lifecycle = kindAndLifecycle[1], DuePhase = due, State = qualification, Reason = reason });
                continue;
            }
            Require(due == "M0" && !kindAndLifecycle[1].Contains("Proposed", StringComparison.Ordinal) && tests.Length > 0, "Proposed fact cannot be qualified by scoped methods.");
            var dependencies = fact.GetProperty("Dependencies").EnumerateArray().ToArray();
            var testSources = new HashSet<string>(StringComparer.Ordinal); bool oracleSource = false;
            foreach (var dependency in dependencies)
            {
                var path = dependency.GetProperty("Path").GetString()!; var expected = dependency.GetProperty("Sha256").GetString()!;
                Capture(RepositoryPath(path), expected);
                var role = dependency.GetProperty("Role").GetString();
                Require(role is "test-source" or "oracle-source" or "production-source", "Current fact dependency must be compiled source.");
                if (role == "production-source") Require(productionDocuments.Any(documents => documents.TryGetValue(path, out var sha) && sha.Equals(expected, StringComparison.OrdinalIgnoreCase)), "Production dependency is not the tested compilation.");
                else
                {
                    Require(testDocuments.TryGetValue(path, out var sha) && sha.Equals(expected, StringComparison.OrdinalIgnoreCase), "Evidence source is not the tested compilation.");
                    if (role == "test-source") Require(testSources.Add(path), "Duplicate test source binding.");
                    else oracleSource = true;
                }
            }
            Require(testSources.Count > 0 && oracleSource, "Missing test or independent oracle source.");
            var observed = new List<object>(); bool passed = true; var named = new HashSet<string>(StringComparer.Ordinal);
            foreach (var test in tests)
            {
                var key = test.GetProperty("Class").GetString() + "." + test.GetProperty("Method").GetString();
                Require(named.Add(key), "Duplicate fact method binding."); BindMethod(test, testAssembly, testSources);
                strict.TryGetValue(key, out var strictExecution); coverage.TryGetValue(key, out var coverageExecution);
                bool methodPassed = strictExecution?.Outcome == "Passed" && coverageExecution?.Outcome == "Passed" && strictExecution.ExecutionId != coverageExecution.ExecutionId;
                passed &= methodPassed;
                observed.Add(new { Method = key, Strict = strictExecution, Coverage = coverageExecution, Passed = methodPassed });
            }
            complete &= passed;
            output.Add(new { FactId = columns[1], Lifecycle = kindAndLifecycle[1], DuePhase = due, State = passed ? "executed-scoped" : "unqualified-current-execution", Reason = reason, Executions = observed, Dependencies = dependencies });
        }
        Write("fact-classification.json", new { Contract = "FINAL-CLASSIFICATION-1", LedgerRows = rows.Length, AllDueFactsBound = complete, BroadPhaseVerified = false, Facts = output });
        return complete;
    }

    private static Dictionary<string, FileMeasurement> FileMeasurements(XElement root)
    {
        var files = new Dictionary<string, List<(string Class, int Line, bool Covered, int CoveredBranches, int Branches)>>(StringComparer.Ordinal);
        foreach (var package in root.Element("packages")?.Elements("package") ?? [])
            foreach (var type in package.Element("classes")?.Elements("class") ?? [])
            {
                var full = (string?)type.Attribute("filename") ?? throw new InvalidDataException("Missing coverage source.");
                Require(full.StartsWith(repository + "/", StringComparison.Ordinal), "Coverage source outside repository.");
                var source = full[(repository.Length + 1)..]; RepositoryPath(source);
                if (!files.TryGetValue(source, out var leaves)) files.Add(source, leaves = []);
                var classIdentity = (string?)package.Attribute("name") + "/" + (string?)type.Attribute("name");
                foreach (var line in type.Element("lines")?.Elements("line") ?? [])
                {
                    int Integer(string name) => int.Parse((string?)line.Attribute(name) ?? string.Empty, NumberStyles.None, CultureInfo.InvariantCulture);
                    int number = Integer("number"), hits = Integer("hits"), branches = 0, coveredBranches = 0;
                    Require(number > 0 && hits >= 0, "Invalid native line counts.");
                    var branch = (string?)line.Attribute("branch"); Require(branch is "True" or "False", "Missing native branch measurement.");
                    if (branch == "True")
                    {
                        var fraction = Regex.Match((string?)line.Attribute("condition-coverage") ?? string.Empty, @"^\d+(?:\.\d+)?% \((\d+)/(\d+)\)$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
                        Require(fraction.Success, "Invalid native branch fraction.");
                        coveredBranches = int.Parse(fraction.Groups[1].Value, CultureInfo.InvariantCulture); branches = int.Parse(fraction.Groups[2].Value, CultureInfo.InvariantCulture);
                        Require(branches > 0 && coveredBranches <= branches, "Invalid branch denominator.");
                    }
                    Require(!leaves.Any(leaf => leaf.Class == classIdentity && leaf.Line == number), "Duplicate class-level coverage leaf.");
                    leaves.Add((classIdentity, number, hits > 0, coveredBranches, branches));
                }
            }
        return files.ToDictionary(pair => pair.Key, pair => new FileMeasurement(pair.Value.GroupBy(leaf => leaf.Line).Count(group => group.Any(leaf => leaf.Covered)),
            pair.Value.Select(leaf => leaf.Line).Distinct().Count(), pair.Value.Sum(leaf => leaf.CoveredBranches), pair.Value.Sum(leaf => leaf.Branches), pair.Value.Count), StringComparer.Ordinal);
    }

    private static bool ClassifyTouched(XElement coverageRoot, string baselineInventory, string currentInventory, string baselineCommitObject)
    {
        using var layers = Read(RepositoryPath("eng/FinalVerification/coverage-layers.json")); var manifest = layers.RootElement;
        Require(manifest.GetProperty("Schema").GetInt32() == 1 && manifest.GetProperty("Contract").GetString() == "FINAL-CLASSIFICATION-1" &&
            manifest.GetProperty("BaselineCommit").GetString() == BaselineCommit, "Unsupported layer baseline.");
        var commitBytes = File.ReadAllBytes(Capture(baselineCommitObject));
        Require(GitObjectHash("commit", commitBytes) == BaselineCommit && Encoding.UTF8.GetString(commitBytes).StartsWith("tree " + manifest.GetProperty("BaselineTree").GetString() + "\n", StringComparison.Ordinal), "Baseline commit/tree authentication differs.");
        var baseline = new Dictionary<string, string>(StringComparer.Ordinal);
        var raw = File.ReadAllText(Capture(baselineInventory, manifest.GetProperty("BaselineInventorySha256").GetString()));
        Require(raw.EndsWith('\0'), "Incomplete baseline tree inventory.");
        foreach (var entry in raw.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = entry.Split('\t'); Require(fields.Length == 2, "Ambiguous baseline path.");
            var metadata = fields[0].Split(' '); Require(metadata.Length == 3 && metadata[0] == "100644" && metadata[1] == "blob" && Regex.IsMatch(metadata[2], "^[0-9a-f]{40}$"), "Unsupported baseline Git entry.");
            RepositoryPath(fields[1]); Require(baseline.TryAdd(fields[1], metadata[2]), "Duplicate baseline path.");
        }
        var current = new Dictionary<string, string>(StringComparer.Ordinal);
        var inventory = File.ReadAllLines(Capture(currentInventory)); Require(inventory.Length > 0 && inventory.Length % 2 == 0, "Incomplete current checkout inventory.");
        for (int i = 0; i < inventory.Length; i += 2)
        {
            var path = RepositoryPath(inventory[i + 1]); var actual = File.ReadAllBytes(Capture(path));
            Require(inventory[i] == GitObjectHash("blob", actual) && current.TryAdd(inventory[i + 1], inventory[i]), "Current checkout inventory differs.");
        }
        var rules = manifest.GetProperty("Rules").EnumerateArray().ToArray(); var measurements = FileMeasurements(coverageRoot);
        var output = new List<object>(); bool complete = true;
        foreach (var path in baseline.Keys.Union(current.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            baseline.TryGetValue(path, out var old); current.TryGetValue(path, out var now); if (old == now) continue;
            var rule = rules.FirstOrDefault(rule => Regex.IsMatch(path, rule.GetProperty("Pattern").GetString()!, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)));
            var change = old is null ? "added" : now is null ? "deleted" : "modified";
            if (rule.ValueKind == JsonValueKind.Undefined)
            {
                complete = false; output.Add(new { Path = path, Change = change, State = "unknown-layer", BaselineBlob = old, CurrentBlob = now }); continue;
            }
            var disposition = rule.GetProperty("Disposition").GetString(); var layer = rule.GetProperty("Layer").GetString();
            if (now is null)
            {
                var removals = manifest.GetProperty("ReviewedRemovals").EnumerateArray().Where(removal => removal.GetProperty("Path").GetString() == path).ToArray();
                Require(removals.Length == 1 && removals[0].GetProperty("BaselineBlob").GetString() == old, "Unreviewed source removal.");
                Capture(RepositoryPath(removals[0].GetProperty("EvidencePath").GetString()!), removals[0].GetProperty("EvidenceSha256").GetString());
                output.Add(new { Path = path, Change = change, Layer = layer, State = "removed-no-current-denominator", BaselineBlob = old, CurrentBlob = now, RemovalReview = removals[0] }); continue;
            }
            if (disposition == "assembly-only")
            {
                var source = File.ReadAllText(Capture(RepositoryPath(path)));
                source = Regex.Replace(source, @"/\*.*?\*/|//[^\r\n]*", "", RegexOptions.Singleline | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
                source = Regex.Replace(source, @"(?m)^\s*using\s+[A-Za-z0-9_.]+\s*;|\[assembly:\s*[A-Za-z0-9_.]+\s*\([^\r\n]*\)\s*\]", "", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
                Require(string.IsNullOrWhiteSpace(source), "Assembly metadata exclusion has executable or unsupported content.");
            }
            if (disposition is "test-source" or "assembly-only" or "documentation" or "non-runtime")
            {
                output.Add(new { Path = path, Change = change, Layer = layer, State = "non-runtime-disposition", Disposition = disposition, BaselineBlob = old, CurrentBlob = now }); continue;
            }
            var target = rule.GetProperty("TargetPercent").GetInt32();
            Require(target is 90 or 92 or 95 && disposition is "runtime" or "configuration", "Unsupported layer policy.");
            measurements.TryGetValue(path, out var counts);
            var linePercent = counts is { Lines: > 0 } ? (decimal?)counts.CoveredLines * 100 / counts.Lines : null;
            var branchPercent = counts is { Branches: > 0 } ? (decimal?)counts.CoveredBranches * 100 / counts.Branches : null;
            var state = counts is null || counts.Lines == 0 ? "unmeasured" : linePercent < target || branchPercent < target ? "below-target" : counts.Branches == 0 ? "line-qualified-no-branch-denominator" : "measured-layer-target";
            bool passed = counts is { Lines: > 0 } && linePercent >= target && (counts.Branches == 0 || branchPercent >= target);
            complete &= passed;
            output.Add(new { Path = path, Change = change, Layer = layer, State = state, TargetPercent = target, Counts = counts, LinePercent = linePercent, BranchPercent = branchPercent,
                IndividualBranchArmsVerified = false, BaselineBlob = old, CurrentBlob = now });
        }
        Write("touched-classification.json", new { Contract = "FINAL-CLASSIFICATION-1", BaselineCommit, BaselineTree = manifest.GetProperty("BaselineTree").GetString(), BaselinePaths = baseline.Count,
            CurrentPaths = current.Count, AllTouchedTargetsMet = complete, Files = output });
        return complete;
    }

    private static string GitObjectHash(string kind, byte[] bytes)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        hash.AppendData(Encoding.ASCII.GetBytes(kind + " " + bytes.Length.ToString(CultureInfo.InvariantCulture) + "\0")); hash.AppendData(bytes);
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }
}
