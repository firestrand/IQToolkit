#nullable enable
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

internal static partial class Program
{
    private const string CacheArea = "artifacts/cache-sensitivity/run.3_dhn7xv";
    private const string CacheHistoricalParent = "/tmp/iqtoolkit-cache-seeds.wapfh628";
    private const string CacheOriginalVerifierSha = "e489a74d84c75ee828d14fb5223d56ea4aa21df93e64d986eb79ceadd43f6346";
    private static readonly Dictionary<string, string> CachePins = new(StringComparer.Ordinal)
    {
        ["packet.json"] = "3659b04fc8d2282b7d51f79078951b2cb0766efb11c30665450ad356b22c748f",
        ["reviewed-input-retention.json"] = "508e6605adafa55e89e644325ac45769480ec04fea8e78bfb8b02396fd329a52",
        ["reviewed-inputs.json"] = "33cb2d8d2ce8db67e6760c98c844222576a61377c1f6b93bd9ca4aae015de531",
        ["inputs-after.json"] = "33cb2d8d2ce8db67e6760c98c844222576a61377c1f6b93bd9ca4aae015de531",
        ["environment-disposition.json"] = "9e1ede0be31917066323ef1e68c5ca6c7f37f4e59362e74e4d3207c04882ee1f",
        ["root-sources-before.json"] = "f943bc6eb77a80acfa077a8d5361a00d6a0189fb7f55224685bd9a451689e366",
        ["root-sources-after.json"] = "f943bc6eb77a80acfa077a8d5361a00d6a0189fb7f55224685bd9a451689e366",
        ["root-binaries-before.json"] = "fc033b3270ce57af2145e648265775c9834c69e00f4bb565497a2286f732fd74",
        ["root-binaries-after.json"] = "fc033b3270ce57af2145e648265775c9834c69e00f4bb565497a2286f732fd74",
        ["root-index-before.json"] = "21fd440e94aea75eeaea31b9d725d640cc3c6130a51c7bdb9c09cd47a1d4da01",
        ["root-index-after.json"] = "21fd440e94aea75eeaea31b9d725d640cc3c6130a51c7bdb9c09cd47a1d4da01",
        ["protecting.json"] = "5bfb208e9049a7a7794eb445304fa98c3ff8f487bcc8c776add9866e802b6be5",
        ["integrated-oracle.json"] = "33853e186c44771b689cbe8df8fb0c7c8ea20c31c13b41e7c08816a85e0e3957",
        ["retained-stage-inputs.json"] = "83c909c0a1b9bcf92cd9bc9616a7f5c5b43a9e62209fe725c6497c87f2dfbba9",
    };
    private static readonly Dictionary<string, string> CacheCompiledPins = new(StringComparer.Ordinal)
    {
        ["initial"] = "b074adec10f1fe32de2068e8f856ab065134ae8e4f45bc7eda087c44db39cd89",
        ["untyped-equality-red"] = "1c0b8ae7afc3b3b0966461ef8951c5b0bd27650d8b26fcb75bb9e89e7c25b2ef",
        ["untyped-equality-restored"] = "b074adec10f1fe32de2068e8f856ab065134ae8e4f45bc7eda087c44db39cd89",
        ["same-clr-policy-red"] = "f4f84717e0ed7b0818e925ba6c2500a83f01676bce38b274de07c601f8876e5d",
        ["same-clr-policy-restored"] = "b074adec10f1fe32de2068e8f856ab065134ae8e4f45bc7eda087c44db39cd89",
        ["wrong-entry-red"] = "0a87e7640d7db9dc8465494b3c983c0cc5a872fb210a1c990f6128796e9f30a4",
        ["wrong-entry-restored"] = "b074adec10f1fe32de2068e8f856ab065134ae8e4f45bc7eda087c44db39cd89",
        ["nonadding-miss-red"] = "4f76707c9ec9a3a9033cfca7da116d4d1da7c8f95a1c33ea4e81d6c05c1e1d92",
        ["nonadding-miss-restored"] = "b074adec10f1fe32de2068e8f856ab065134ae8e4f45bc7eda087c44db39cd89",
        ["disabled-promotion-red"] = "208d73e3098e53ffa240910131e5af73215223bd159806cba6092d1eb7f82273",
        ["disabled-promotion-restored"] = "b074adec10f1fe32de2068e8f856ab065134ae8e4f45bc7eda087c44db39cd89",
        ["frozen-parameters-red"] = "5b70b230dc3d90fbe62caab05191720df76e089c1db6a3010cef5ce6ffecc50a",
        ["frozen-parameters-restored"] = "b074adec10f1fe32de2068e8f856ab065134ae8e4f45bc7eda087c44db39cd89",
        ["final"] = "b074adec10f1fe32de2068e8f856ab065134ae8e4f45bc7eda087c44db39cd89",
    };
    private static readonly string[] CacheIds = ["untyped-equality", "same-clr-policy", "wrong-entry", "nonadding-miss", "disabled-promotion", "frozen-parameters"];
    private static readonly string[] CacheDeltaPaths = ["AGENT_STATE.md", "README.md", "docs/modernization-implementation-plan.md",
        "docs/standards/dotnet-rules.json", "eng/FinalVerification/Program.cs", "eng/verify-final.sh",
        "IQToolkit.Data/DbEntityProvider.cs", "eng/verify-ci-consumers.sh", "eng/FinalVerification/fact-bindings.json",
        "IQToolkit.Data.SQLite/SQLiteFormatter.cs", "eng/FinalVerification/Classifiers.cs",
        "IQToolkit.Data/Mapping/AttributeMapping.cs", "IQToolkit.Data/EntityProvider.cs",
        "eng/IndependentConsumers/Profiles/IQToolkit/packages.lock.json", "eng/IndependentConsumers/Profiles/IQToolkit.Data/packages.lock.json",
        "eng/IndependentConsumers/Profiles/IQToolkit.Data.Access/packages.lock.json", "eng/IndependentConsumers/Profiles/IQToolkit.Data.MySqlClient/packages.lock.json",
        "eng/IndependentConsumers/Profiles/IQToolkit.Data.SQLite/packages.lock.json", "eng/IndependentConsumers/Profiles/IQToolkit.Data.SqlClient/packages.lock.json",
        "eng/PackageConsumer/packages.lock.json", "eng/IndependentConsumers/prepared-feed.sha256"];
    private static readonly string[] CacheAdditionPaths = ["IQToolkitTest/RealProvider/NativeNullArrayMembershipTests.cs",
        "eng/FinalVerification/CompilationContinuity.cs", "eng/FinalVerification/CacheSeedEvidence.cs",
        "docs/modernization-null-array-hardness.md", "docs/modernization-cache-sensitivity-hardness.md", "docs/modernization-final-binding-hardness.md",
        "IQToolkitTest/RealProvider/NativeCommandInitializationTests.cs", "eng/FinalVerification/CommandInitializationContinuity.cs",
        "eng/verify-command-continuity-outcome.sh", "eng/verify-ci-consumer-outcome.sh", "eng/verify-final-continuity-outcome.sh",
        "docs/modernization-command-initialization-hardness.md", "docs/modernization-consumer-interruption-hardness.md",
        "docs/modernization-command-final-continuity-hardness.md", "docs/modernization-final-current-binding-hardness.md",
        "IQToolkitTest/RealProvider/NativeMathIdentifierCultureTests.cs", "eng/FinalVerification/MathIdentifierContinuity.cs",
        "docs/modernization-sqlite-math-culture-hardness.md", "eng/FinalVerification/PackageIndependentEvidence.cs",
        "docs/modernization-package-independent-binding-hardness.md", "eng/FinalVerification/PackageNativeConsumerEvidence.cs",
        "docs/modernization-native-consumer-binding-hardness.md",
        "IQToolkitTest/RealProvider/CanonicalPublicationTests.cs", "IQToolkitTest/RealProvider/NativeStringColumnInequalityTests.cs",
        "eng/FinalVerification/NullableColumnContinuity.cs", "eng/FinalVerification/CanonicalPublicationContinuity.cs",
        "docs/modernization-canonical-publication-hardness.md", "docs/modernization-nullable-column-acquisition-hardness.md",
        "docs/modernization-nullable-continuity-hardness.md", "docs/modernization-sqlite-string-column-inequality-hardness.md",
        "docs/modernization-recent-final-continuity-hardness.md"];
    private static int cacheDuplicateNativePaths;
    private static long cacheDuplicateLogicalBytes;
    private sealed record CacheAlias(string Stage, string LogicalPath, string RetainedPath, string Sha256, long Bytes, string Kind);

    // The coordinator publishes this returned receipt only after its terminal input checks.
    private static object ValidateCacheSeeds(JsonElement profile)
    {
        var chosen = ParseInput(profile.GetProperty("CacheSeedResult"));
        var chosenPath = CacheRepositoryPath(chosen.Path);
        var attempts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["artifacts/cache-sensitivity/run.3_dhn7xv/result.json"] = "bfe32ff5c8158e13f92523a2f6e2fb3f8c4c4f3ae8708ea33cb6f1fc03476479",
            ["artifacts/cache-sensitivity/run.am_lcsgh/result.json"] = "ec8dc61ef062d61c2ff501fd3fa6ed6c7b192a2ad271037ebdf4dd12d8a4b421",
            ["artifacts/cache-sensitivity/run.lq_ht8hn/result.json"] = "82e0a4c260ecaa19968a5024271ff2e22c7e2e6cba08090f9c5fd1c3ed7fed4b"
        };
        var relative = System.IO.Path.GetRelativePath(repository, chosenPath).Replace('\\', '/');
        Require(attempts.TryGetValue(relative, out var selectedSha) && chosen.Sha256.Equals(selectedSha, StringComparison.OrdinalIgnoreCase),
            "Cache producer is not one of the authenticated native attempts.");
        using var resultDocument = Read(chosenPath, chosen.Sha256);
        var result = resultDocument.RootElement;
        Require(result.GetProperty("Status").GetString() == "passed", "Authentic cache producer failed; six diagnostic kills are not qualified.");
        Require(relative == CacheArea + "/result.json", "Successful cache producer origin differs.");
        using var packetDocument = CacheRead("packet.json"); var packet = packetDocument.RootElement;
        Require(packet.GetProperty("Schema").GetInt32() == 1 && packet.GetProperty("Contract").GetString() == "CACHE-SEEDS-1" &&
            packet.GetProperty("Parent").GetString() == CacheHistoricalParent && result.GetProperty("Worktree").GetString() == CacheHistoricalParent + "/worktree",
            "Historical cache packet identity differs.");
        CacheValidateProducer(result, packet);
        var producerInputAuthentication = CacheValidateReviewedInputs();
        using var manifestDocument = Read(System.IO.Path.Combine(repository, "artifacts/cache-sensitivity/seeds.json"),
            "a1cad9cfe2700af226cc5cae078e0e7cd82fef846bb9d04130bfc76b83f48901");
        var manifest = manifestDocument.RootElement;
        Require(manifest.GetProperty("Schema").GetInt32() == 1 && manifest.GetProperty("Contract").GetString() == "CACHE-SEEDS-1" &&
            manifest.GetProperty("Categories").EnumerateArray().Select(category => category.GetProperty("Id").GetString()).SequenceEqual(CacheIds),
            "Reviewed six-category manifest differs.");
        using var bindingsDocument = CacheRead("retained-stage-inputs.json");
        var bindings = bindingsDocument.RootElement;
        Require(bindings.GetProperty("Status").GetString() == "verified" && bindings.GetProperty("Contract").GetString() == "CACHE-SEEDS-1" &&
            bindings.GetProperty("Bindings").GetInt32() == 4676 && bindings.GetProperty("NativeDescriptorsUnchanged").GetBoolean() &&
            !bindings.GetProperty("NewNativeExecution").GetBoolean(), "Historical retained-stage binding manifest differs.");
        using var initialDocument = Read(System.IO.Path.Combine(repository, CacheArea, "initial/descriptor.json"));
        var initial = initialDocument.RootElement;
        var baselineSources = initial.GetProperty("Sources").EnumerateObject().ToDictionary(property => property.Name,
            property => property.Value.GetString()!, StringComparer.Ordinal);
        Require(baselineSources.Count == 310, "Historical copied-source scope differs.");
        var nonmissing = packet.GetProperty("Sources").EnumerateObject().Where(property => property.Value.ValueKind != JsonValueKind.Null)
            .ToDictionary(property => property.Name, property => property.Value.GetString()!, StringComparer.Ordinal);
        Require(nonmissing.Count == 310 && nonmissing.All(pair => baselineSources.TryGetValue(pair.Key, out var hash) && hash == pair.Value),
            "Initial sources differ from the actual nonmissing producer inputs.");
        var physical = new Dictionary<string, CacheAlias>(StringComparer.Ordinal);
        var stages = bindings.GetProperty("Stages").EnumerateArray().ToDictionary(stage => stage.GetProperty("Stage").GetString()!, StringComparer.Ordinal);
        Require(stages.Count == 14 && stages.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(CacheCompiledPins.Keys), "Native cache stage set differs.");
        var summaries = new List<object>(); int totalExecutions = 0, totalPassed = 0, totalFailed = 0;
        foreach (var name in CacheCompiledPins.Keys)
        {
            var stage = stages[name];
            var aliases = stage.GetProperty("Aliases").Deserialize<CacheAlias[]>() ?? throw new InvalidDataException("Missing cache aliases.");
            Require(aliases.Length == 334 && aliases.Select(alias => alias.LogicalPath).Distinct(StringComparer.Ordinal).Count() == 334 &&
                aliases.All(alias => alias.Stage == name), "Stage/logical alias count or identity differs.");
            var byLogical = aliases.ToDictionary(alias => alias.LogicalPath, StringComparer.Ordinal);
            foreach (var alias in aliases)
            {
                CacheValidateAlias(alias);
                if (physical.TryGetValue(alias.RetainedPath, out var prior)) Require(prior.Sha256 == alias.Sha256 && prior.Bytes == alias.Bytes,
                    "Repeated physical cache alias has conflicting identity.");
                else physical.Add(alias.RetainedPath, alias);
                var copy = Capture(alias.RetainedPath, alias.Sha256);
                Require(new FileInfo(copy).Length == alias.Bytes, "Cache alias byte count differs.");
            }
            var inspectPath = System.IO.Path.Combine(repository, CacheArea, CacheInspectionName(name) + ".log");
            Require(stage.GetProperty("InspectionLog").GetString() == inspectPath, "Inspection log origin differs.");
            using var observedDocument = Read(inspectPath, stage.GetProperty("InspectionLogSha256").GetString());
            var observed = observedDocument.RootElement;
            Require(observed.GetProperty("Status").GetString() == "verified" && observed.GetProperty("InputStabilityVerified").GetBoolean() &&
                observed.GetProperty("TerminalError").ValueKind == JsonValueKind.Null, "Historical native stage verifier rejected or lost stability.");
            var observedInputs = observed.GetProperty("ActualInputs").EnumerateArray().ToArray();
            Require(observedInputs.Length == 334 && observedInputs.Select(input => input.GetProperty("Path").GetString()).Distinct(StringComparer.Ordinal).Count() == 334 &&
                observedInputs.All(input => byLogical.TryGetValue(input.GetProperty("Path").GetString()!, out var alias) &&
                    alias.Sha256.Equals(input.GetProperty("Sha256").GetString(), StringComparison.OrdinalIgnoreCase) && alias.Bytes == input.GetProperty("Bytes").GetInt64()),
                "Aliases do not exactly preserve the native observed stage inputs.");
            var descriptorPath = System.IO.Path.Combine(repository, CacheArea, name, "descriptor.json");
            using var descriptorDocument = Read(descriptorPath, byLogical[descriptorPath].Sha256);
            var descriptor = descriptorDocument.RootElement;
            CacheValidateStageSources(name, descriptor, baselineSources, manifest, result);
            CacheValidateStageExpectations(name, descriptor, initial, manifest);
            var replay = CacheNativeStage.Validate(descriptorPath,
                logical => byLogical.TryGetValue(logical, out var alias) ? alias.RetainedPath : throw new InvalidDataException("Unbound historical native read: " + logical),
                logical => byLogical.TryGetValue(logical, out var alias) ? alias.Sha256 : throw new InvalidDataException("Unbound historical native checksum: " + logical));
            Require(replay.GetProperty("ActualInputPaths").EnumerateArray().Select(value => value.GetString()!).ToHashSet(StringComparer.Ordinal).SetEquals(byLogical.Keys),
                "Requalified stage did not consume every exact historical input alias.");
            var actual = replay.GetProperty("Executions").EnumerateArray().ToArray();
            var originalExecutions = observed.GetProperty("Operation").GetProperty("Executions").EnumerateArray().ToArray();
            Require(actual.Length == originalExecutions.Length && actual.All(execution => originalExecutions.Any(original =>
                original.GetProperty("ExecutionId").GetString() == execution.GetProperty("ExecutionId").GetString() &&
                original.GetProperty("CompiledMethod").GetProperty("BodySha256").GetString() == execution.GetProperty("CompiledMethod").GetProperty("BodySha256").GetString() &&
                original.GetProperty("StdOut").GetString() == execution.GetProperty("StdOut").GetString() &&
                original.GetProperty("Message").GetString() == execution.GetProperty("Message").GetString() &&
                original.GetProperty("StackTrace").GetString() == execution.GetProperty("StackTrace").GetString())),
                "Retained native semantics differ from the executed verifier observation.");
            totalExecutions += actual.Length; totalPassed += actual.Count(execution => execution.GetProperty("Outcome").GetString() == "Passed");
            totalFailed += actual.Count(execution => execution.GetProperty("Outcome").GetString() == "Failed");
            summaries.Add(new { Stage = name, Descriptor = new Input(descriptorPath, byLogical[descriptorPath].Sha256),
                Inspection = new Input(inspectPath, stage.GetProperty("InspectionLogSha256").GetString()!),
                Executions = actual.Select(execution => new
                {
                    Class = execution.GetProperty("Class").GetString(), Method = execution.GetProperty("Method").GetString(),
                    ExecutionId = execution.GetProperty("ExecutionId").GetString(), Outcome = execution.GetProperty("Outcome").GetString(),
                    AssertionMessage = execution.GetProperty("Message").GetString(), AssertionStack = execution.GetProperty("StackTrace").GetString(),
                    CompiledMethod = execution.GetProperty("CompiledMethod").Clone(), Native = execution.GetProperty("Native").Clone()
                }).ToArray(), SourceQualified = true });
        }
        Require(physical.Count == 509 && physical.Values.Sum(alias => alias.Bytes) == 18621425 &&
            totalExecutions == 68 && totalPassed == 62 && totalFailed == 6, "Native seed execution or physical-retention denominators differ.");
        var runtimeFiles = CacheValidateRuntime();
        var compatible = CacheValidateCurrent(profile, baselineSources);
        using var protectingDocument = CacheRead("protecting.json");
        var protecting = protectingDocument.RootElement;
        Require(protecting.GetProperty("Status").GetString() == "verified" && protecting.GetProperty("ExactRecordedCommands").GetInt32() == 58 &&
            protecting.GetProperty("UnoptimizedPrivateGraphBuilds").GetInt32() == 13 && protecting.GetProperty("NoInliningNativeTests").GetInt32() == 14 &&
            protecting.GetProperty("AllRestorationsVerified").GetBoolean() && protecting.GetProperty("DiagnosticProfileOnly").GetBoolean() &&
            !protecting.GetProperty("DefaultOptimizedArtifactsVerified").GetBoolean(), "Protecting diagnostic receipt differs.");
        using var oracleDocument = CacheRead("integrated-oracle.json");
        Require(oracleDocument.RootElement.GetProperty("Status").GetString() == "CLEAR" &&
            !oracleDocument.RootElement.GetProperty("OwnerApproval").GetBoolean() && !oracleDocument.RootElement.GetProperty("OptimizedArtifactQualification").GetBoolean(),
            "Integrated diagnostic review identity differs.");
        return new
        {
            Status = "verified-diagnostic-profile", Contract = "CACHE-SEEDS-1", Producer = chosen,
            Commands = 58, UnoptimizedPrivateRebuilds = 13, NoInliningNativeStages = 14, NativeExecutions = totalExecutions,
            PassedExecutions = totalPassed, IntendedFailures = totalFailed, RestoredGreenStages = 6,
            StageLogicalBindings = 4676, RetainedPhysicalFiles = physical.Count, RetainedPhysicalBytes = physical.Values.Sum(alias => alias.Bytes),
            NativeStageEvidence = summaries, ProducerInputAuthentication = producerInputAuthentication,
            CurrentSourceCompatibility = compatible, RuntimeDependencyFiles = runtimeFiles,
            CurrentDuplicateNativePaths = cacheDuplicateNativePaths, CurrentDuplicateLogicalBytes = cacheDuplicateLogicalBytes,
            SharedLogicalInputPathsAtCacheBinding = Inputs.Count,
            SharedPhysicalCopiesAtCacheBinding = Snapshots.Values.Distinct(StringComparer.Ordinal).Count(),
            SharedRetainedPhysicalBytesAtCacheBinding = bytes,
            DefaultOptimizedArtifactsUnqualified = true, BroadCacheContractVerified = false, SecurityVerified = false,
            ConcurrencyVerified = false, SameInstancePolicyReplacementVerified = false, FullReadinessVerified = false
        };
    }

    private static string CacheRepositoryPath(string relative)
    {
        Require(!System.IO.Path.IsPathRooted(relative) && !relative.Contains('\\') && relative.Split('/').All(part => part.Length > 0 && part is not "." and not ".."),
            "Cache profile requires canonical repository-relative paths.");
        return System.IO.Path.Combine(repository, relative);
    }
    private static JsonDocument CacheRead(string name) => Read(System.IO.Path.Combine(repository, CacheArea, name), CachePins[name]);
    private static string CacheInspectionName(string stage) => stage.EndsWith("-red", StringComparison.Ordinal) ? stage[..^4] + "-inspect" : stage + "-inspect";

    private static void CacheValidateProducer(JsonElement result, JsonElement packet)
    {
        foreach (var flag in new[] { "SixFixedCategoriesKilled", "PrivateSourcesRestored", "RootSourcesUnchanged", "RootBinariesUnchanged", "ReviewedInputsUnchanged", "RootIndexUnchanged" })
            Require(result.GetProperty(flag).GetBoolean(), "Producer preservation or six-kill outcome failed.");
        Require(!result.GetProperty("Cancelled").GetBoolean() && result.GetProperty("CleanupErrors").GetArrayLength() == 0 && result.GetProperty("StorageErrors").GetArrayLength() == 0 &&
            result.GetProperty("ElapsedSeconds").GetDouble() is > 0 and <= 900 && result.GetProperty("OwnedBytes").GetInt64() is > 0 and <= 2147483648,
            "Cache producer deadline/cleanup/storage failed.");
        var commands = result.GetProperty("Commands").EnumerateArray().ToArray(); var declared = packet.GetProperty("Commands");
        Require(commands.Length == 58 && commands.Select(command => command.GetProperty("Name").GetString())
            .SequenceEqual(declared.EnumerateObject().Select(property => property.Name)), "Recorded command count/order differs from the sealed producer packet.");
        int builds = 0, nativeTests = 0;
        foreach (var command in commands)
        {
            var name = command.GetProperty("Name").GetString()!; var expected = declared.GetProperty(name);
            foreach (var field in new[] { "Name", "Argv", "Cwd", "ExpectedExit", "DeadlineSeconds", "Environment" })
                Require(command.GetProperty(field).GetRawText() == expected.GetProperty(field).GetRawText(), "Recorded command differs from its native sealed declaration.");
            Require(command.GetProperty("Exit").GetInt32() == command.GetProperty("ExpectedExit").GetInt32() && command.GetProperty("Pid").GetInt32() > 0 &&
                command.GetProperty("RemainingLiveMembers").GetArrayLength() == 0 && !command.TryGetProperty("ForcedTermination", out _) &&
                command.GetProperty("ElapsedSeconds").GetDouble() is >= 0 and <= 120 && command.GetProperty("LogBytes").GetInt64() is >= 0 and <= 16777216,
                "Native command failed, exceeded bounds or required forced cleanup.");
            var argv = command.GetProperty("Argv").EnumerateArray().Select(value => value.GetString()!).ToArray();
            var log = CacheCaptureCommandLog(name, command.GetProperty("LogSha256").GetString()!, command.GetProperty("LogBytes").GetInt64());
            Require(new FileInfo(log).Length == command.GetProperty("LogBytes").GetInt64(), "Recorded command log byte count differs.");
            if (name.EndsWith("-build", StringComparison.Ordinal))
            {
                var text = File.ReadAllText(log);
                Require(text.Contains("Build succeeded.", StringComparison.Ordinal) && text.Contains("0 Warning(s)", StringComparison.Ordinal) && text.Contains("0 Error(s)", StringComparison.Ordinal),
                    "Native build did not report zero warnings and errors.");
                if (name != "helper-build")
                {
                    Require(argv.Contains("-t:Rebuild") && argv.Contains("-p:Optimize=false") && argv.Contains("--no-restore") &&
                        argv.Contains("-warnaserror") && argv.Contains("-p:CheckDeadCode=true"), "Private rebuild did not use the exact diagnostic profile.");
                    builds++;
                }
            }
            if (name.EndsWith("-test", StringComparison.Ordinal))
            {
                var environment = command.GetProperty("Environment");
                Require(environment.EnumerateObject().Count() == 1 && environment.GetProperty("DOTNET_JitNoInline").GetString() == "1" &&
                    argv.Contains("--no-build") && argv.Contains("--no-restore"), "Native cache stage did not use the exact no-inline diagnostic profile.");
                nativeTests++;
            }
        }
        Require(builds == 13 && nativeTests == 14, "Private rebuild/native-stage count differs.");
        using var disposition = CacheRead("environment-disposition.json");
        var value = disposition.RootElement;
        Require(value.GetProperty("Status").GetString() == "verified" && value.GetProperty("Original").GetString() == CacheHistoricalParent &&
            value.GetProperty("Archive").GetString() == System.IO.Path.Combine(repository, CacheArea, "environment") && value.GetProperty("OriginalAbsent").GetBoolean() &&
            value.GetProperty("WorktreeRegistrationRemoved").GetBoolean() && value.GetProperty("NativeRemovalExit").GetInt32() == 0 &&
            value.GetProperty("FilesPreserved").GetInt32() == 713 && value.GetProperty("AllBytesPreserved").GetBoolean(), "Historical environment disposition failed.");
        foreach (var pair in new[] { ("root-sources-before.json", "root-sources-after.json"), ("root-binaries-before.json", "root-binaries-after.json"), ("root-index-before.json", "root-index-after.json") })
        {
            using var before = CacheRead(pair.Item1); using var after = CacheRead(pair.Item2);
            Require(before.RootElement.GetRawText() == after.RootElement.GetRawText(), "Historical root input preservation differs.");
        }
    }

    private static string CacheCaptureCommandLog(string name, string expected, long declaredBytes)
    {
        var path = System.IO.Path.Combine(repository, CacheArea, name + ".log");
        if (declaredBytes != 0) return Capture(path, expected);
        const string emptySha256 = "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855";
        Require(CacheIds.Any(id => name == id + "-patch" || name == id + "-patch-check") &&
            expected.Equals(emptySha256, StringComparison.OrdinalIgnoreCase), "Only authentic empty native Git patch/check logs are admitted.");
        CacheNativeStage.NoLinkedPath(path);
        Require(new FileInfo(path).Length == 0, "Native zero-byte command log is no longer empty.");
        if (Inputs.TryGetValue(path, out var previous))
        {
            Require(previous == emptySha256 && Hash(path) == previous, "Previously captured native empty log differs.");
            return Snapshots[path];
        }
        Require(Inputs.Count < MaximumInputs, "Final input count bound exceeded for native empty log.");
        var hash = Hash(path);
        Require(hash == emptySha256, "Native empty log checksum differs.");
        var snapshot = System.IO.Path.Combine(retained, $"{Inputs.Count:D4}-{System.IO.Path.GetFileName(path)}.snapshot");
        File.Copy(path, snapshot, overwrite: false);
        Require(new FileInfo(snapshot).Length == 0 && Hash(snapshot) == hash && Hash(path) == hash && new FileInfo(path).Length == 0,
            "Native empty log changed while retained.");
        CacheNativeStage.NoLinkedPath(path);
        Inputs.Add(path, hash); Snapshots.Add(path, snapshot);
        return snapshot;
    }

    private static string CacheCaptureDuplicate(string path, string expected)
    {
        path = System.IO.Path.GetFullPath(path);
        CacheNativeStage.NoLinkedPath(path);
        var file = new FileInfo(path);
        Require(file.Exists && file.LinkTarget is null && (file.Attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) == 0 &&
            file.Length is > 0 and <= Limit, "Missing, nonregular, linked or oversized duplicate-content cache input.");
        if (Inputs.ContainsKey(path)) return Capture(path, expected);
        var proof = Inputs.FirstOrDefault(pair => pair.Value.Equals(expected, StringComparison.OrdinalIgnoreCase) &&
            new FileInfo(Snapshots[pair.Key]).Length == file.Length);
        if (proof.Key is null) return Capture(path, expected);
        Require(Inputs.Count < MaximumInputs, "Final input path bound exceeded for duplicate cache input.");
        var snapshot = Snapshots[proof.Key];
        var hash = Hash(path);
        Require(hash.Equals(expected, StringComparison.OrdinalIgnoreCase) && Hash(snapshot) == hash &&
            new FileInfo(path).Length == file.Length && new FileInfo(snapshot).Length == file.Length,
            "Native duplicate-content input does not match its retained authentic copy.");
        CacheNativeStage.NoLinkedPath(path);
        Require(Hash(path) == hash, "Native duplicate-content input changed before registration.");
        Inputs.Add(path, hash); Snapshots.Add(path, snapshot);
        cacheDuplicateNativePaths++; cacheDuplicateLogicalBytes += file.Length;
        // No new physical copy: the coordinator's byte counter retains its physical-copy bound.
        // This actual path is still independently hashed by the ordinary terminal fence.
        return snapshot;
    }

    private static object CacheValidateReviewedInputs()
    {
        using var receipt = CacheRead("reviewed-input-retention.json"); using var before = CacheRead("reviewed-inputs.json"); using var after = CacheRead("inputs-after.json");
        var records = receipt.RootElement.EnumerateArray().ToArray();
        using var stageBindings = CacheRead("retained-stage-inputs.json");
        var initialStage = stageBindings.RootElement.GetProperty("Stages").EnumerateArray().Single(stage => stage.GetProperty("Stage").GetString() == "initial");
        var initialPath = System.IO.Path.Combine(repository, CacheArea, "initial/descriptor.json");
        var initialAlias = initialStage.GetProperty("Aliases").EnumerateArray().Single(alias => alias.GetProperty("LogicalPath").GetString() == initialPath);
        using var initial = Read(initialPath, initialAlias.GetProperty("Sha256").GetString());
        var sources = initial.RootElement.GetProperty("Sources");
        Require(records.Length == 324 && before.RootElement.EnumerateObject().Count() == 324 && after.RootElement.EnumerateObject().Count() == 324 &&
            records.Select(record => record.GetProperty("LogicalPath").GetString()).Distinct(StringComparer.Ordinal).Count() == 324, "Reviewed producer-input retention scope differs.");
        int reused = 0, direct = 0;
        foreach (var record in records)
        {
            var logical = record.GetProperty("LogicalPath").GetString()!; var physical = record.GetProperty("RetainedPath").GetString()!; var hash = record.GetProperty("Sha256").GetString()!;
            Require(before.RootElement.GetProperty(logical).GetString() == hash && after.RootElement.GetProperty(logical).GetString() == hash &&
                physical.StartsWith(System.IO.Path.Combine(repository, CacheArea, "reviewed-inputs") + "/", StringComparison.Ordinal), "Reviewed retained input origin or terminal hash differs.");
            var relative = System.IO.Path.GetRelativePath(repository, logical).Replace('\\', '/');
            if (sources.TryGetProperty(relative, out var source))
            {
                Require(source.GetString() == hash, "Reviewed source record differs from the authenticated native stage source.");
                reused++;
                // The pinned stage aliases authenticate these exact baseline bytes below.
                // Do not retain another copy of each already preserved historical source.
            }
            else { Capture(physical, hash); direct++; }
        }
        Require(records.Any(record => record.GetProperty("LogicalPath").GetString() == System.IO.Path.Combine(repository, "artifacts/cache-sensitivity/run.py") &&
            record.GetProperty("Sha256").GetString() == "b4583a6ce46d17572fa9fd02887bce6c8c18407c42eb67840c9899be0c41b52e") &&
            records.Any(record => record.GetProperty("LogicalPath").GetString() == CacheHistoricalParent + "/tool/Program.cs" && record.GetProperty("Sha256").GetString() == CacheOriginalVerifierSha),
            "Executed producer/verifier source provenance differs.");
        Require(reused == 310 && direct == 14, "Producer-input authentication disposition differs.");
        return new { ReviewedManifestEntries = 324, DirectRetainedReviewInputs = direct,
            ReusedStageSourceBindings = reused, MatchingLogicalRelativePathAndSha256Required = true,
            EveryRedundantReviewedPhysicalCopyRehashed = false };
    }

    private static void CacheValidateAlias(CacheAlias alias)
    {
        Require(System.IO.Path.GetFullPath(alias.LogicalPath) == alias.LogicalPath && System.IO.Path.GetFullPath(alias.RetainedPath) == alias.RetainedPath &&
            alias.Bytes is > 0 and <= 16777216 && Regex.IsMatch(alias.Sha256, "^[a-f0-9]{64}$"), "Invalid historical alias input.");
        var area = System.IO.Path.Combine(repository, CacheArea); var worktree = CacheHistoricalParent + "/worktree/";
        var compiledPrefix = worktree + "IQToolkitTest/bin/Release/net10.0/";
        var expected = alias.Kind switch
        {
            "stage-compiled-copy" when alias.LogicalPath.StartsWith(compiledPrefix, StringComparison.Ordinal) =>
                System.IO.Path.Combine(area, alias.Stage, "compiled", alias.LogicalPath[compiledPrefix.Length..]),
            "retained-restored-baseline-or-generated-source" when alias.LogicalPath.StartsWith(worktree, StringComparison.Ordinal) =>
                System.IO.Path.Combine(area, "environment", "worktree", alias.LogicalPath[worktree.Length..]) +
                    (alias.LogicalPath.EndsWith(".csproj", StringComparison.Ordinal) ? ".snapshot" : string.Empty),
            "single-seeded-production-source" when alias.Stage.EndsWith("-red", StringComparison.Ordinal) =>
                System.IO.Path.Combine(area, alias.Stage[..^4], "seed-source.snapshot"),
            "unchanged-owned-report" when alias.LogicalPath.StartsWith(area + "/", StringComparison.Ordinal) => alias.LogicalPath,
            "captured-actual-external-source" when alias.LogicalPath == System.IO.Path.Combine(repository,
                "artifacts/nuget/packages/microsoft.net.test.sdk/18.10.1/build/net8.0/Microsoft.NET.Test.Sdk.Program.cs") =>
                System.IO.Path.Combine(area, "external-inputs", alias.Sha256 + "-Microsoft.NET.Test.Sdk.Program.cs.snapshot"),
            _ => throw new InvalidDataException("Unreviewed historical cache alias kind or origin.")
        };
        Require(alias.RetainedPath == expected, "Historical alias retained origin differs.");
        CacheNativeStage.NoLinkedPath(alias.RetainedPath);
    }

    private static void CacheValidateStageSources(string stage, JsonElement descriptor, Dictionary<string, string> baseline, JsonElement manifest, JsonElement result)
    {
        Require(descriptor.GetProperty("Schema").GetInt32() == 1 && descriptor.GetProperty("Worktree").GetString() == CacheHistoricalParent + "/worktree", "Historical stage worktree differs.");
        var sources = descriptor.GetProperty("Sources").EnumerateObject().ToDictionary(property => property.Name, property => property.Value.GetString()!, StringComparer.Ordinal);
        Require(sources.Count == baseline.Count && sources.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(baseline.Keys), "Seeded/restored copied-source set changed.");
        var red = stage.EndsWith("-red", StringComparison.Ordinal); string? mutated = null;
        if (red)
        {
            var id = stage[..^4]; var category = manifest.GetProperty("Categories").EnumerateArray().Single(value => value.GetProperty("Id").GetString() == id);
            mutated = category.GetProperty("Source").GetString()!;
            var seed = result.GetProperty("Seeds").EnumerateArray().Single(value => value.GetProperty("Id").GetString() == id);
            Require(seed.GetProperty("Outcome").GetString() == "intended-assertion-killed" && seed.GetProperty("RestorationVerified").GetBoolean() &&
                baseline[mutated] == category.GetProperty("BaselineSha256").GetString() && sources[mutated] == category.GetProperty("MutatedSha256").GetString() &&
                seed.GetProperty("BaselineSha256").GetString() == baseline[mutated] && seed.GetProperty("MutatedSha256").GetString() == sources[mutated] &&
                seed.GetProperty("Source").GetString() == mutated && seed.GetProperty("PatchSha256").GetString() == category.GetProperty("PatchSha256").GetString(),
                "Seeded source, patch or restored result differs from the fixed category.");
            Capture(CacheRepositoryPath(category.GetProperty("Patch").GetString()!), category.GetProperty("PatchSha256").GetString());
        }
        Require(sources.All(pair => pair.Key == mutated || baseline[pair.Key] == pair.Value), "More than the one reviewed production source changed.");
    }

    private static void CacheValidateStageExpectations(string stage, JsonElement descriptor, JsonElement initial, JsonElement manifest)
    {
        var tests = descriptor.GetProperty("ExpectedTests").EnumerateArray().ToArray();
        if (!stage.EndsWith("-red", StringComparison.Ordinal))
        {
            Require(tests.Length == 7 && descriptor.GetProperty("ExpectedTests").GetRawText() == initial.GetProperty("ExpectedTests").GetRawText(),
                "Initial/restored/final native expected suites differ.");
            return;
        }
        var category = manifest.GetProperty("Categories").EnumerateArray().Single(value => value.GetProperty("Id").GetString() == stage[..^4]);
        Require(tests.Length == 2 && tests.Count(test => test.GetProperty("Outcome").GetString() == "Failed") == 1, "Seeded stage does not have exactly one failure and one passing control.");
        var failed = tests.Single(test => test.GetProperty("Outcome").GetString() == "Failed");
        foreach (var field in new[] { "Class", "Method", "FailureLine", "Assertion" }) Require(failed.GetProperty(field).GetRawText() == category.GetProperty(field).GetRawText(), "Native failure expectation differs from the reviewed category.");
        var control = tests.Single(test => test.GetProperty("Outcome").GetString() == "Passed");
        Require(control.GetProperty("Class").GetString() == manifest.GetProperty("Control").GetProperty("Class").GetString() &&
            control.GetProperty("Method").GetString() == manifest.GetProperty("Control").GetProperty("Method").GetString(), "Typed native control identity differs.");
    }

    private static object CacheValidateCurrent(JsonElement profile, Dictionary<string, string> baseline)
    {
        Require(recentCompilationQualified && mathCompilationQualified && commandCompilationQualified,
            "Current cache compatibility requires authenticated recent, math and command compilation continuity.");
        var deltas = profile.GetProperty("CacheSeedReviewedDeltas"); var additions = profile.GetProperty("CacheSeedReviewedAdditions");
        Require(deltas.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal).SetEquals(CacheDeltaPaths) &&
            additions.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal).SetEquals(CacheAdditionPaths), "Reviewed cache continuity delta/addition scope differs.");
        foreach (var property in deltas.EnumerateObject().Concat(additions.EnumerateObject()))
            Require(property.Value.ValueKind == JsonValueKind.String && property.Value.GetString() is { } value &&
                Regex.IsMatch(value, "^[0-9A-Fa-f]{64}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)),
                "Reviewed cache continuity hashes must be explicit SHA256 strings.");
        foreach (var pair in baseline)
        {
            var path = CacheRepositoryPath(pair.Key);
            if (pair.Key == "eng/FinalVerification/profile.json") { Capture(path); continue; }
            CacheCaptureDuplicate(path, deltas.TryGetProperty(pair.Key, out var delta) ? delta.GetString()! : pair.Value);
        }
        foreach (var property in additions.EnumerateObject()) Capture(CacheRepositoryPath(property.Name), property.Value.GetString());
        var roots = new[] { "IQToolkit", "IQToolkit.Data", "IQToolkit.Data.Access", "IQToolkit.Data.MySqlClient", "IQToolkit.Data.SQLite", "IQToolkit.Data.SqlClient", "IQToolkit.Data.SqlServerCe", "IQToolkitTest", "Test", "docs", "eng" };
        var actual = roots.SelectMany(root => CacheSourceFiles(CacheRepositoryPath(root))).Select(path => System.IO.Path.GetRelativePath(repository, path).Replace('\\', '/')).ToHashSet(StringComparer.Ordinal);
        var expected = baseline.Keys.Where(path => roots.Contains(path.Split('/')[0], StringComparer.Ordinal)).Concat(CacheAdditionPaths).ToHashSet(StringComparer.Ordinal);
        Require(actual.SetEquals(expected), "Current production/test/docs/engineering file additions or removals are outside the reviewed cache continuity scope.");
        var intermediatePath = System.IO.Path.Combine(repository, "artifacts/native-null-array/coverage.yrni_8j2/collection/descriptor.json");
        using var intermediateDescriptor = Read(intermediatePath,
            "77495a1bcb8b29eb57deec221325b1305fdf2f728515cb885198dc82df3bd64c");
        using var transition = Read(System.IO.Path.Combine(repository, "artifacts/native-null-array/coverage.yrni_8j2/retention/transition.json"),
            "6e2a328479e34786ccf5ba2e73072033123f7632ee78b7ef2c9939cb61cfcd2b");
        var newSource = ParseInput(transition.RootElement.GetProperty("TestSource"));
        Require(newSource.Path == CacheRepositoryPath(CacheAdditionPaths[0]) && newSource.Sha256.Equals(additions.GetProperty(CacheAdditionPaths[0]).GetString(), StringComparison.OrdinalIgnoreCase),
            "Added native-null source does not match the authenticated Test transition.");
        var intermediateIdentity = ParseInput(transition.RootElement.GetProperty("CurrentDescriptor"));
        Require(intermediateIdentity.Path == intermediatePath && intermediateIdentity.Sha256 == "77495a1bcb8b29eb57deec221325b1305fdf2f728515cb885198dc82df3bd64c" &&
            transition.RootElement.GetProperty("AddedPdbDocument").GetString() == CacheAdditionPaths[0] &&
            transition.RootElement.GetProperty("OldTests").GetInt32() == 416 && transition.RootElement.GetProperty("CurrentTests").GetInt32() == 419 &&
            transition.RootElement.GetProperty("AddedTests").EnumerateArray().Select(method => method.GetString()).SequenceEqual(new[]
            {
                "NativeNullArrayInputsAreAcquired", "CompilerNullArrayMembershipObservesReplacementWithoutCache",
                "CompilerNullArrayMembershipObservesReplacementWithCache"
            }), "Authenticated intermediate native-null Test transition differs.");
        var commandDescriptorPath = System.IO.Path.Combine(repository, "artifacts/command-initialization/coverage.gw5awfx_/collection/descriptor.json");
        const string commandDescriptorHash = "d70a5e3734b25302f7d700122c178f7604d084a819ed202fb9c8a824621989f8";
        var commandReceiptPath = System.IO.Path.Combine(repository, "artifacts/command-initialization/coverage-qualification._c6y3y6f/retention/transition.json");
        const string commandReceiptHash = "474402be9e5645471395d88895284afd6be4b2d42f87eb1294975d7bb0bcffe8";
        Require(PathInProfile(profile, "CommandDescriptor") == commandDescriptorPath &&
            ShaInProfile(profile, "CommandDescriptor").Equals(commandDescriptorHash, StringComparison.OrdinalIgnoreCase) &&
            PathInProfile(profile, "CommandReceipt") == commandReceiptPath &&
            ShaInProfile(profile, "CommandReceipt").Equals(commandReceiptHash, StringComparison.OrdinalIgnoreCase),
            "Qualified command descriptor or receipt origin differs.");
        using var commandDescriptor = Read(commandDescriptorPath, commandDescriptorHash);
        using var commandReceipt = Read(commandReceiptPath, commandReceiptHash);
        var commandSource = ParseInput(commandReceipt.RootElement.GetProperty("TestSource"));
        var dataSource = ParseInput(commandReceipt.RootElement.GetProperty("DataSource"));
        var oldDataSource = ParseInput(commandReceipt.RootElement.GetProperty("OldDataSource"));
        var commandBaseline = ParseInput(commandReceipt.RootElement.GetProperty("BaselineDescriptor"));
        var commandCurrent = ParseInput(commandReceipt.RootElement.GetProperty("CurrentDescriptor"));
        Require(commandReceipt.RootElement.GetProperty("Status").GetString() == "verified" &&
            commandBaseline == intermediateIdentity && commandCurrent.Path == commandDescriptorPath &&
            commandCurrent.Sha256.Equals(commandDescriptorHash, StringComparison.OrdinalIgnoreCase) &&
            commandSource.Path == CacheRepositoryPath("IQToolkitTest/RealProvider/NativeCommandInitializationTests.cs") &&
            commandSource.Sha256.Equals(additions.GetProperty("IQToolkitTest/RealProvider/NativeCommandInitializationTests.cs").GetString(), StringComparison.OrdinalIgnoreCase) &&
            dataSource.Path == CacheRepositoryPath("IQToolkit.Data/DbEntityProvider.cs") &&
            dataSource.Sha256.Equals(deltas.GetProperty("IQToolkit.Data/DbEntityProvider.cs").GetString(), StringComparison.OrdinalIgnoreCase) &&
            oldDataSource.Path == System.IO.Path.Combine(repository, "artifacts/command-initialization/red.bg8dit7i/DbEntityProvider.before.cs.snapshot") &&
            oldDataSource.Sha256.Equals(baseline["IQToolkit.Data/DbEntityProvider.cs"], StringComparison.OrdinalIgnoreCase),
            "Current source compatibility does not match the qualified command transition.");
        Capture(oldDataSource.Path, oldDataSource.Sha256);
        var mathDescriptorPath = System.IO.Path.Combine(repository, "artifacts/sqlite-math-culture/coverage.nx5khe0v/collection/descriptor.json");
        const string mathDescriptorHash = "596f7ea743ea586d779d3f22ac43d82d98d4c8e73978eac5e67fd20c00c849b3";
        var mathReceiptPath = System.IO.Path.Combine(repository, "artifacts/sqlite-math-culture/qualification.1p9l6zbh/retention/transition.json");
        const string mathReceiptHash = "ded76477fdc9f00ca49a4f4a36c2683cb081df6f2bccdc6433dfa41d88a62f4a";
        Require(PathInProfile(profile, "MathDescriptor") == mathDescriptorPath &&
            ShaInProfile(profile, "MathDescriptor").Equals(mathDescriptorHash, StringComparison.OrdinalIgnoreCase) &&
            PathInProfile(profile, "MathReceipt") == mathReceiptPath &&
            ShaInProfile(profile, "MathReceipt").Equals(mathReceiptHash, StringComparison.OrdinalIgnoreCase),
            "Qualified math descriptor or receipt origin differs.");
        using var mathDescriptor = Read(mathDescriptorPath, mathDescriptorHash);
        using var mathReceipt = Read(mathReceiptPath, mathReceiptHash);
        var mathSource = ParseInput(mathReceipt.RootElement.GetProperty("TestSource"));
        var sqliteSource = ParseInput(mathReceipt.RootElement.GetProperty("SQLiteSource"));
        var oldSQLiteSource = ParseInput(mathReceipt.RootElement.GetProperty("OldSQLiteSource"));
        var mathBaseline = ParseInput(mathReceipt.RootElement.GetProperty("BaselineDescriptor"));
        var mathCurrent = ParseInput(mathReceipt.RootElement.GetProperty("CurrentDescriptor"));
        Require(mathReceipt.RootElement.GetProperty("Status").GetString() == "verified" && mathBaseline == commandCurrent &&
            mathCurrent.Path == mathDescriptorPath && mathCurrent.Sha256.Equals(mathDescriptorHash, StringComparison.OrdinalIgnoreCase) &&
            mathSource.Path == CacheRepositoryPath("IQToolkitTest/RealProvider/NativeMathIdentifierCultureTests.cs") &&
            mathSource.Sha256.Equals(additions.GetProperty("IQToolkitTest/RealProvider/NativeMathIdentifierCultureTests.cs").GetString(), StringComparison.OrdinalIgnoreCase) &&
            mathSource.Sha256.Equals("c93ac9671eeaedf6d972078fa9b8f1dbf7285f358667987a59d58246e4286a49", StringComparison.OrdinalIgnoreCase) &&
            sqliteSource.Path == CacheRepositoryPath("IQToolkit.Data.SQLite/SQLiteFormatter.cs") &&
            sqliteSource.Sha256.Equals("7c694080670dd15ed8ba0327b92516f32619893a91b878b523ec7810de84a1c0", StringComparison.OrdinalIgnoreCase) &&
            oldSQLiteSource.Path == System.IO.Path.Combine(repository, "artifacts/sqlite-math-culture/red.b18i41v7/reviewed-inputs/0024-SQLiteFormatter.cs.snapshot") &&
            oldSQLiteSource.Sha256.Equals(baseline["IQToolkit.Data.SQLite/SQLiteFormatter.cs"], StringComparison.OrdinalIgnoreCase) &&
            mathReceipt.RootElement.GetProperty("OldTests").GetInt32() == 424 && mathReceipt.RootElement.GetProperty("CurrentTests").GetInt32() == 427 &&
            mathReceipt.RootElement.GetProperty("AddedPdbDocument").GetString() == "IQToolkitTest/RealProvider/NativeMathIdentifierCultureTests.cs" &&
            mathReceipt.RootElement.GetProperty("AddedTests").EnumerateArray().Select(method => method.GetString()).SequenceEqual(new[]
            {
                "NativeSQLiteMathSinInputsAreAcquired", "SQLiteMathSinPreservesNativeResultInInvariantCulture",
                "SQLiteMathSinPreservesNativeResultInTurkishCulture"
            }), "Current source compatibility does not match the fully requalified math transition.");
        Capture(oldSQLiteSource.Path, oldSQLiteSource.Sha256);
        CaptureHistoricalRecent(sqliteSource.Path, sqliteSource.Sha256);
        using var canonicalManifest = Read(RecentPath(CanonicalManifest), CanonicalManifest.Sha256);
        using var nullableManifest = Read(RecentPath(NullableManifest), NullableManifest.Sha256);
        var canonicalCurrent = ParseInput(canonicalManifest.RootElement.GetProperty("CurrentDescriptor"));
        var nullableBaseline = ParseInput(nullableManifest.RootElement.GetProperty("BaselineDescriptor"));
        var nullableCurrent = ParseInput(nullableManifest.RootElement.GetProperty("CurrentDescriptor"));
        Require(canonicalCurrent == new Input(RecentPath(CanonicalDescriptor), CanonicalDescriptor.Sha256) &&
            nullableBaseline == canonicalCurrent && nullableCurrent == new Input(RecentPath(NullableDescriptor), NullableDescriptor.Sha256),
            "Current cache descriptor chain differs from the recomputed recent transitions.");
        var qualifiedSources = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["IQToolkit.Data/Mapping/AttributeMapping.cs"] = "4dcaae30ecfa02e90cbc00da1a8412b1d68e4be12fec5102f907504f548088d1",
            ["IQToolkit.Data/EntityProvider.cs"] = "176306f6fd437bdfce693216bb57f4cde29cb692bb00b92230a6c843aa720b5a",
            ["IQToolkit.Data.SQLite/SQLiteFormatter.cs"] = "7f4180c30a777d6b10de21bf102c4b59d423b81e4735ac6c394788c81eaf6918"
        };
        // Recent continuity has already compared these exact source recipes with the compiled PDB documents.
        foreach (var source in qualifiedSources)
        {
            Require(source.Value.Equals(deltas.GetProperty(source.Key).GetString(), StringComparison.OrdinalIgnoreCase),
                "Current cache production source differs from its qualified compilation.");
            Capture(CacheRepositoryPath(source.Key), source.Value);
        }
        foreach (var binding in new[] { canonicalManifest.RootElement.GetProperty("TestSource"), nullableManifest.RootElement.GetProperty("TestSource") })
        {
            var source = ParseInput(binding); var relative = System.IO.Path.GetRelativePath(repository, source.Path).Replace('\\', '/');
            Require(source.Sha256.Equals(additions.GetProperty(relative).GetString(), StringComparison.OrdinalIgnoreCase),
                "Current cache test source differs from its recomputed recent transition.");
        }
        using var currentDescriptor = Read(RecentPath(NullableDescriptor), NullableDescriptor.Sha256);
        CacheValidateClassifierChange(baseline, deltas);
        var consumerCopies = CacheValidateConsumerCopies(baseline, deltas);
        var currentManifestInput = ParseInput(currentDescriptor.RootElement.GetProperty("Preservation")[0].GetProperty("Before"));
        using var currentManifest = Read(currentManifestInput.Path, currentManifestInput.Sha256); using var original = CacheRead("root-binaries-before.json");
        var oldBinaries = original.RootElement.EnumerateObject().ToArray();
        Require(oldBinaries.Length == 336 && currentManifest.RootElement.EnumerateObject().Count() == 336 &&
            oldBinaries.Select(binary => binary.Name).ToHashSet(StringComparer.Ordinal).SetEquals(
                currentManifest.RootElement.EnumerateObject().Select(binary => binary.Name)), "Ordinary binary profile changed.");
        var changedTest = new HashSet<string>(StringComparer.Ordinal)
        {
            "IQToolkitTest/bin/Release/net10.0/IQToolkitTest.dll", "IQToolkitTest/bin/Release/net10.0/IQToolkitTest.pdb",
            "IQToolkitTest/obj/Release/net10.0/IQToolkitTest.dll", "IQToolkitTest/obj/Release/net10.0/IQToolkitTest.pdb",
            "IQToolkitTest/obj/Release/net10.0/ref/IQToolkitTest.dll", "IQToolkitTest/obj/Release/net10.0/refint/IQToolkitTest.dll"
        };
        var changedData = new HashSet<string>(StringComparer.Ordinal)
        {
            "IQToolkit.Data/bin/Release/net10.0/IQToolkit.Data.dll", "IQToolkit.Data/bin/Release/net10.0/IQToolkit.Data.pdb",
            "IQToolkit.Data/obj/Release/net10.0/IQToolkit.Data.dll", "IQToolkit.Data/obj/Release/net10.0/IQToolkit.Data.pdb",
            "IQToolkit.Data.Access/bin/Release/net10.0/IQToolkit.Data.dll", "IQToolkit.Data.Access/bin/Release/net10.0/IQToolkit.Data.pdb",
            "IQToolkit.Data.MySqlClient/bin/Release/net10.0/IQToolkit.Data.dll", "IQToolkit.Data.MySqlClient/bin/Release/net10.0/IQToolkit.Data.pdb",
            "IQToolkit.Data.SQLite/bin/Release/net10.0/IQToolkit.Data.dll", "IQToolkit.Data.SQLite/bin/Release/net10.0/IQToolkit.Data.pdb",
            "IQToolkit.Data.SqlClient/bin/Release/net10.0/IQToolkit.Data.dll", "IQToolkit.Data.SqlClient/bin/Release/net10.0/IQToolkit.Data.pdb",
            "IQToolkitTest/bin/Release/net10.0/IQToolkit.Data.dll", "IQToolkitTest/bin/Release/net10.0/IQToolkit.Data.pdb",
            "Test/bin/Release/net10.0/IQToolkit.Data.dll", "Test/bin/Release/net10.0/IQToolkit.Data.pdb"
        };
        var changedSQLite = new HashSet<string>(StringComparer.Ordinal)
        {
            "IQToolkit.Data.SQLite/bin/Release/net10.0/IQToolkit.Data.SQLite.dll", "IQToolkit.Data.SQLite/bin/Release/net10.0/IQToolkit.Data.SQLite.pdb",
            "IQToolkit.Data.SQLite/obj/Release/net10.0/IQToolkit.Data.SQLite.dll", "IQToolkit.Data.SQLite/obj/Release/net10.0/IQToolkit.Data.SQLite.pdb",
            "IQToolkitTest/bin/Release/net10.0/IQToolkit.Data.SQLite.dll", "IQToolkitTest/bin/Release/net10.0/IQToolkit.Data.SQLite.pdb",
            "Test/bin/Release/net10.0/IQToolkit.Data.SQLite.dll", "Test/bin/Release/net10.0/IQToolkit.Data.SQLite.pdb"
        };
        Require(changedData.Count == 16 && changedTest.Count == 6 && changedSQLite.Count == 8 &&
            !changedData.Overlaps(changedTest) && !changedData.Overlaps(changedSQLite) && !changedTest.Overlaps(changedSQLite) &&
            changedData.Concat(changedTest).Concat(changedSQLite).All(path => original.RootElement.TryGetProperty(path, out _)),
            "Authenticated current binary delta paths differ.");
        foreach (var binary in oldBinaries)
        {
            var oldHash = binary.Value.GetString()!; var now = currentManifest.RootElement.GetProperty(binary.Name).GetString()!;
            if (changedTest.Contains(binary.Name))
            {
                Require(!oldHash.Equals(now, StringComparison.OrdinalIgnoreCase), "Expected authenticated Test binary change is absent.");
                Capture(System.IO.Path.Combine(repository, "artifacts/native-null-array/acquire.7ovl4p05/previous-binaries", binary.Name), oldHash);
            }
            else if (changedData.Contains(binary.Name))
            {
                Require(!oldHash.Equals(now, StringComparison.OrdinalIgnoreCase), "Expected authenticated Data binary change is absent.");
                Capture(System.IO.Path.Combine(repository, "artifacts/command-initialization/green.gl2ik3j8/previous-binaries", binary.Name), oldHash);
            }
            else if (changedSQLite.Contains(binary.Name))
            {
                Require(!oldHash.Equals(now, StringComparison.OrdinalIgnoreCase), "Expected authenticated SQLite binary change is absent.");
                Capture(System.IO.Path.Combine(repository, "artifacts/sqlite-math-culture/green.1_odmlyu/previous-binaries", binary.Name), oldHash);
            }
            else Require(oldHash.Equals(now, StringComparison.OrdinalIgnoreCase), "An ordinary binary changed outside the authenticated Data/Test/SQLite transitions.");
            CacheCaptureDuplicate(CacheRepositoryPath(binary.Name), now);
        }
        var mapping = ParseInput(currentDescriptor.RootElement.GetProperty("Mapping")); Capture(mapping.Path, mapping.Sha256);
        return new { CopiedHistoricalSources = 310, ReviewedDeltas = CacheDeltaPaths, ReviewedAdditions = CacheAdditionPaths,
            IntermediateDescriptor = intermediateIdentity, CommandDescriptor = new Input(commandDescriptorPath, commandDescriptorHash),
            CommandReceipt = new Input(commandReceiptPath, commandReceiptHash), MathDescriptor = new Input(mathDescriptorPath, mathDescriptorHash),
            MathReceipt = new Input(mathReceiptPath, mathReceiptHash), CanonicalDescriptor = canonicalCurrent,
            CurrentDescriptor = nullableCurrent, ConsumerCopies = consumerCopies, OrdinaryBinariesUnchanged = 306,
            AuthenticatedChangedDataBinaries = 16, AuthenticatedChangedTestBinaries = 6, AuthenticatedChangedSQLiteBinaries = 8,
            HistoricalAll336UnchangedCurrentClaim = false };
    }

    private static void CacheValidateClassifierChange(Dictionary<string, string> baseline, JsonElement deltas)
    {
        const string relative = "eng/FinalVerification/Classifiers.cs";
        var before = File.ReadAllBytes(Capture(System.IO.Path.Combine(repository, CacheArea, "environment/worktree", relative), baseline[relative]));
        var oldText = Encoding.UTF8.GetString(before);
        const string ledgerGuard = "ledger.GetProperty(\"RowCount\").GetInt32() == 32";
        const string rowsGuard = "rows.Length == 32";
        Require(oldText.Split(ledgerGuard, StringSplitOptions.None).Length == 2 && oldText.Split(rowsGuard, StringSplitOptions.None).Length == 2,
            "Historical classifier does not contain exactly the two reviewed count guards.");
        var mathText = oldText.Replace(ledgerGuard, "ledger.GetProperty(\"RowCount\").GetInt32() == 34", StringComparison.Ordinal)
            .Replace(rowsGuard, "rows.Length == 34", StringComparison.Ordinal);
        const string oldSignature = "private static bool ClassifyFacts(JsonElement descriptor)";
        const string newSignature = "private static bool ClassifyFacts(JsonElement descriptor, string consumerDirectory, string apiDirectory)";
        const string oldQualification = "qualification is \"scoped-current-tests\" or \"unqualified\" or \"not-due\"";
        const string newQualification = "qualification is \"scoped-current-tests\" or \"independent-package-producer\" or \"native-package-consumer-producer\" or \"unqualified\" or \"not-due\"";
        const string oldBranch = "            if (qualification != \"scoped-current-tests\")";
        const string producerBranch = "            if (qualification == \"independent-package-producer\")\n            {\n                Require(columns[1] == \"PACKAGE.INDEPENDENT.v1\" && due == \"M0\" && tests.Length == 0 &&\n                    !kindAndLifecycle[1].Contains(\"Proposed\", StringComparison.Ordinal), \"Unreviewed producer-backed fact scope.\");\n                var producerDependencies = fact.GetProperty(\"Dependencies\").EnumerateArray().ToArray();\n                Require(producerDependencies.Length == 1 && producerDependencies[0].GetProperty(\"Path\").GetString() == \"eng/FinalVerification/PackageIndependentEvidence.cs\" &&\n                    producerDependencies[0].GetProperty(\"Role\").GetString() == \"producer-verifier-source\", \"Unreviewed producer verifier dependency.\");\n                var source = RepositoryPath(producerDependencies[0].GetProperty(\"Path\").GetString()!);\n                var expected = producerDependencies[0].GetProperty(\"Sha256\").GetString()!;\n                Capture(source, expected);\n                var proof = ValidateIndependentPackageProducer(consumerDirectory, apiDirectory);\n                var boundSource = ParseInput(JsonSerializer.SerializeToElement(proof).GetProperty(\"VerifierSource\"));\n                Require(boundSource.Path == source && boundSource.Sha256.Equals(expected, StringComparison.OrdinalIgnoreCase),\n                    \"Producer verifier is not the reviewed compilation.\");\n                output.Add(new { FactId = columns[1], Lifecycle = kindAndLifecycle[1], DuePhase = due,\n                    State = \"executed-producer\", Reason = reason, ProducerEvidence = proof, Dependencies = producerDependencies });\n                continue;\n            }\n";
        const string nativeProducerBranch = "            if (qualification == \"native-package-consumer-producer\")\n            {\n                Require(columns[1] == \"PACKAGE.NATIVE.CONSUMER.v1\" && due == \"M0\" && tests.Length == 0 &&\n                    kindAndLifecycle[1] == \"Observed scoped; phase gate open\", \"Unreviewed native consumer fact scope.\");\n                var nativeDependencies = fact.GetProperty(\"Dependencies\").EnumerateArray().ToArray();\n                Require(nativeDependencies.Length == 2 &&\n                    nativeDependencies[0].GetProperty(\"Path\").GetString() == \"eng/FinalVerification/PackageNativeConsumerEvidence.cs\" &&\n                    nativeDependencies[0].GetProperty(\"Role\").GetString() == \"producer-verifier-source\" &&\n                    nativeDependencies[1].GetProperty(\"Path\").GetString() == \"eng/FinalVerification/PackageIndependentEvidence.cs\" &&\n                    nativeDependencies[1].GetProperty(\"Role\").GetString() == \"producer-shared-source\", \"Unreviewed native consumer source dependencies.\");\n                var nativeSource = RepositoryPath(nativeDependencies[0].GetProperty(\"Path\").GetString()!);\n                var nativeExpected = nativeDependencies[0].GetProperty(\"Sha256\").GetString()!;\n                var nativeSharedSource = RepositoryPath(nativeDependencies[1].GetProperty(\"Path\").GetString()!);\n                var nativeSharedExpected = nativeDependencies[1].GetProperty(\"Sha256\").GetString()!;\n                Capture(nativeSource, nativeExpected); Capture(nativeSharedSource, nativeSharedExpected);\n                var nativeProof = ValidateNativePackageConsumer(consumerDirectory, apiDirectory);\n                var nativeReceipt = JsonSerializer.SerializeToElement(nativeProof);\n                var nativeBoundSource = ParseInput(nativeReceipt.GetProperty(\"VerifierSource\"));\n                var nativeBoundShared = ParseInput(nativeReceipt.GetProperty(\"SharedVerifierSource\"));\n                Require(nativeReceipt.GetProperty(\"Status\").GetString() == \"verified\" &&\n                    nativeReceipt.GetProperty(\"Contract\").GetString() == \"PACKAGE-NATIVE-CONSUMER-BINDING-1\" &&\n                    nativeBoundSource.Path == nativeSource && nativeBoundSource.Sha256.Equals(nativeExpected, StringComparison.OrdinalIgnoreCase) &&\n                    nativeBoundShared.Path == nativeSharedSource && nativeBoundShared.Sha256.Equals(nativeSharedExpected, StringComparison.OrdinalIgnoreCase),\n                    \"Native consumer verifier is not the reviewed compilation.\");\n                output.Add(new { FactId = columns[1], Lifecycle = kindAndLifecycle[1], DuePhase = due,\n                    State = \"executed-producer\", Reason = reason, ProducerEvidence = nativeProof, Dependencies = nativeDependencies });\n                continue;\n            }\n";
        Require(mathText.Split(oldSignature, StringSplitOptions.None).Length == 2 &&
            mathText.Split(oldQualification, StringSplitOptions.None).Length == 2 &&
            mathText.Split(oldBranch, StringSplitOptions.None).Length == 2, "Historical classifier producer insertion points differ.");
        var expected = Encoding.UTF8.GetBytes(mathText.Replace(oldSignature, newSignature, StringComparison.Ordinal)
            .Replace(oldQualification, newQualification, StringComparison.Ordinal)
            .Replace(oldBranch, producerBranch + nativeProducerBranch + oldBranch, StringComparison.Ordinal));
        Require(expected.AsSpan().SequenceEqual(File.ReadAllBytes(Capture(CacheRepositoryPath(relative), deltas.GetProperty(relative).GetString()))),
            "Classifier change exceeds the reviewed count guards and two typed producer branches.");
    }

    private static object CacheValidateConsumerCopies(Dictionary<string, string> baseline, JsonElement deltas)
    {
        var nativeProof = CacheValidateRecentConsumerProducer();
        using var historicalReceipt = Read(System.IO.Path.Combine(repository, "artifacts/sqlite-math-culture/consumers.78w957rd/source-lock-refresh.json"),
            "d42aea33ca6a3b8b713c39a9ccf8fb41865ec48787d6ae489cccdec5d4dbbe5a");
        using var currentReceipt = Read(System.IO.Path.Combine(repository, "artifacts/recent-final-binding/source-lock-refresh.json"),
            "4508a942cb9eb446e063a5e4a2d48b9a19465e5c4e16c608430de1f1ed607ab5");
        var historical = historicalReceipt.RootElement;
        var proofs = new List<object>();
        foreach (var currentStep in new[] { false, true })
        {
            var observed = currentStep ? currentReceipt.RootElement : historical;
            Require(observed.GetProperty("Status").GetString() == "applied-exact-authentic-copy" && observed.GetProperty("GraphsUnchanged").GetBoolean() &&
                observed.GetProperty("BeforeBytesRetained").GetBoolean() && observed.GetProperty("ExactGeneratedCopiesVerified").GetBoolean() &&
                observed.GetProperty("ToolkitContentHashChanges").GetInt32() == 18 && observed.GetProperty("PreparedFeedHashChanges").GetInt32() == 6,
                "Authentic consumer source-copy receipt differs.");
            var expected = CacheDeltaPaths.Where(path => path.EndsWith("/packages.lock.json", StringComparison.Ordinal) ||
                path == "eng/IndependentConsumers/prepared-feed.sha256").Select(CacheRepositoryPath).ToHashSet(StringComparer.Ordinal);
            Require(expected.Count == 8 && expected.SetEquals(observed.GetProperty("Sources").EnumerateObject().Select(property => property.Name)),
                "Consumer source-copy scope differs.");
            var totalHashChanges = 0;
            foreach (var property in observed.GetProperty("Sources").EnumerateObject())
            {
                var relative = System.IO.Path.GetRelativePath(repository, property.Name).Replace('\\', '/');
                var copy = property.Value;
                var originalHash = copy.GetProperty("OriginalSha256").GetString()!;
                var generatedHash = copy.GetProperty("GeneratedSha256").GetString()!;
                var expectedBefore = currentStep ? historical.GetProperty("Sources").GetProperty(property.Name).GetProperty("GeneratedSha256").GetString()! : baseline[relative];
                Require(originalHash.Equals(expectedBefore, StringComparison.OrdinalIgnoreCase) &&
                    (!currentStep || generatedHash.Equals(deltas.GetProperty(relative).GetString(), StringComparison.OrdinalIgnoreCase)),
                    "Reviewed consumer hashes differ from the actual copied source origins.");
                var generated = copy.GetProperty("Generated").GetString()!;
                var feed = relative == "eng/IndependentConsumers/prepared-feed.sha256";
                var profileName = relative == "eng/PackageConsumer/packages.lock.json" ? "SQLite-runtime" :
                    System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(relative));
                var expectedGenerated = currentStep ? System.IO.Path.Combine(repository, "artifacts/ci-consumers/runs/run.nvlwAtiy/environment", relative) :
                    System.IO.Path.Combine(repository, "artifacts/ci-consumers/runs/run.EyuxrTHW",
                        feed ? "environment/eng/IndependentConsumers/prepared-feed.sha256" : "prepared-locks/" + profileName + ".json");
                Require(generated == expectedGenerated, "Copied consumer generated origin differs.");
                var beforePath = Capture(System.IO.Path.Combine(repository, currentStep ? "artifacts/ci-consumers/runs/run.nvlwAtiy/source" : CacheArea + "/environment/worktree", relative), originalHash);
                var currentPath = Capture(currentStep ? property.Name : generated, generatedHash);
                if (currentStep)
                {
                    var retainedBefore = System.IO.Path.Combine(repository, "artifacts/recent-final-binding/consumer-copies-before", relative);
                    Require(copy.GetProperty("BeforeRetained").GetString() == retainedBefore &&
                        File.ReadAllBytes(Capture(retainedBefore, originalHash)).AsSpan().SequenceEqual(File.ReadAllBytes(beforePath)),
                        "Current consumer before-byte retention differs from the native reviewed graph.");
                    if (!feed)
                        Require(File.ReadAllBytes(currentPath).AsSpan().SequenceEqual(File.ReadAllBytes(Capture(
                            System.IO.Path.Combine(repository, "artifacts/ci-consumers/runs/run.nvlwAtiy/prepared-locks", profileName + ".json"), generatedHash))),
                            "Current consumer generated environment differs from the native prepared lock.");
                }
                Require(File.ReadAllBytes(currentPath).AsSpan().SequenceEqual(File.ReadAllBytes(Capture(generated, generatedHash))),
                    "Current consumer source does not preserve the exact authentic generated copy.");
                int changes = 0;
                if (feed)
                {
                    var before = File.ReadAllLines(beforePath); var after = File.ReadAllLines(currentPath);
                    Require(before.Length == 6 && after.Length == 6, "Prepared feed entry count changed.");
                    for (var index = 0; index < before.Length; index++)
                    {
                        var oldEntry = Regex.Match(before[index], "^([a-f0-9]{64})  (.+\\.nupkg)$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
                        var newEntry = Regex.Match(after[index], "^([a-f0-9]{64})  (.+\\.nupkg)$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
                        Require(oldEntry.Success && newEntry.Success && oldEntry.Groups[2].Value == newEntry.Groups[2].Value &&
                            oldEntry.Groups[1].Value != newEntry.Groups[1].Value, "Prepared feed change exceeds the six observed archive hashes.");
                        changes++;
                    }
                }
                else
                {
                    using var oldLock = JsonDocument.Parse(File.ReadAllBytes(beforePath)); using var newLock = JsonDocument.Parse(File.ReadAllBytes(currentPath));
                    DuplicateCheck(oldLock.RootElement); DuplicateCheck(newLock.RootElement);
                    var before = oldLock.RootElement; var after = newLock.RootElement;
                    Require(before.EnumerateObject().Select(item => item.Name).ToHashSet(StringComparer.Ordinal).SetEquals(after.EnumerateObject().Select(item => item.Name)),
                        "Consumer lock root fields changed.");
                    foreach (var field in before.EnumerateObject().Where(item => item.Name != "dependencies"))
                        Require(JsonElement.DeepEquals(field.Value, after.GetProperty(field.Name)), "Consumer lock metadata changed.");
                    var frameworks = before.GetProperty("dependencies"); var currentFrameworks = after.GetProperty("dependencies");
                    Require(frameworks.EnumerateObject().Select(item => item.Name).ToHashSet(StringComparer.Ordinal).SetEquals(currentFrameworks.EnumerateObject().Select(item => item.Name)),
                        "Consumer lock framework targets changed.");
                    foreach (var framework in frameworks.EnumerateObject())
                    {
                        var nodes = currentFrameworks.GetProperty(framework.Name);
                        Require(framework.Value.EnumerateObject().Select(item => item.Name).ToHashSet(StringComparer.Ordinal).SetEquals(nodes.EnumerateObject().Select(item => item.Name)),
                            "Consumer lock package nodes changed.");
                        foreach (var node in framework.Value.EnumerateObject())
                        {
                            var current = nodes.GetProperty(node.Name);
                            Require(node.Value.EnumerateObject().Select(item => item.Name).ToHashSet(StringComparer.Ordinal).SetEquals(current.EnumerateObject().Select(item => item.Name)),
                                "Consumer lock package fields changed.");
                            foreach (var field in node.Value.EnumerateObject())
                            {
                                var now = current.GetProperty(field.Name);
                                if (field.Name == "contentHash" && Packages.Contains(node.Name, StringComparer.Ordinal) && !JsonElement.DeepEquals(field.Value, now)) changes++;
                                else Require(JsonElement.DeepEquals(field.Value, now), "Consumer lock changed beyond the observed toolkit content hashes.");
                            }
                        }
                    }
                    totalHashChanges += changes;
                }
                Require(changes == copy.GetProperty("HashChanges").GetInt32(), "Native consumer hash-change count differs.");
            }
            Require(totalHashChanges == 18, "Consumer toolkit content-hash refresh count differs.");
            proofs.Add(new { Step = currentStep ? "math-to-current" : "baseline-to-math", ToolkitContentHashChanges = totalHashChanges,
                PreparedFeedHashChanges = 6, LockGraphsUnchanged = true, ExactGeneratedCopiesVerified = true });
        }
        return new { Steps = proofs, NativeProducerAuthentication = nativeProof, NewDependencyGraphQualified = false };
    }

    private static object CacheValidateRecentConsumerProducer()
    {
        const string area = "artifacts/ci-consumers/runs/run.nvlwAtiy";
        const string outer = "artifacts/nullable-column/consumers.ck5z00n0";
        var pins = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["artifacts/nullable-column/consumers.ck5z00n0/result.json"] = "fcdbabb8074ab85df6a2e860173855ed1562eae7a49da5063c26670538a96999",
            ["artifacts/nullable-column/consumers.ck5z00n0/producer-evidence.json"] = "971d2426dc56cd41faa78c652c014b9e12e7c0d0f8975460eb25085f96dfb65c",
            ["artifacts/nullable-column/consumers.ck5z00n0/observed-oracle.json"] = "499e6a4237c15e65bbd3c780e7e976edb06872a9a461aa4a77b27e05c31707e1",
            ["artifacts/nullable-column/consumers.ck5z00n0/parent-retirement.json"] = "2415716f37c8b3c85ed85986704127a88a9c64ed8b3d387ecf61cb816a9e4b7d",
            ["artifacts/recent-final-binding/consumer-copy-preparation.json"] = "c2157ede7511516d70519072753acb82a473bf76aa22c217a744fe7eb8a49373",
            ["artifacts/recent-final-binding/consumer-copy-oracle.json"] = "8ff33383ec083ca6860c706f41ad4b0997a8e62e4045a83ff1f206eadcaf9b71",
            ["artifacts/ci-consumers/runs/run.nvlwAtiy/status.txt"] = "2700165975f68815c97d605c56eca8e90d497ade1264b6282401d13fee99ac27",
            ["artifacts/ci-consumers/runs/run.nvlwAtiy/inputs-before.sha256"] = "440612f3243e702b8ec22fd5fbbb798efe07252125fad1d7ab76520e96bce43d",
            ["artifacts/ci-consumers/runs/run.nvlwAtiy/inputs-after.sha256"] = "440612f3243e702b8ec22fd5fbbb798efe07252125fad1d7ab76520e96bce43d",
            ["artifacts/ci-consumers/runs/run.nvlwAtiy/copied-sources.sha256"] = "86db2079bd25e737e73996e11439cede739660e49335f65dc8fe069497708258",
            ["artifacts/ci-consumers/runs/run.nvlwAtiy/retained-path-mapping.txt"] = "42a069a9cbd0b5b737d929bc62b75cd727d35dd0927a6df4a09950d62eeb44ea",
            ["artifacts/ci-consumers/runs/run.nvlwAtiy/tested-packages.sha256"] = "7a8ffc9d98861b272da1e0cf2e7113718b7ad3f70b89b4974a7bc235b854ccc9",
            ["artifacts/ci-consumers/runs/run.nvlwAtiy/prepare-independent.log"] = "b34602a53bd74d7362d70ee471dbc7b4e20bdc2014b12907e539c9c5a3602aa4",
            ["artifacts/ci-consumers/runs/run.nvlwAtiy/verify-independent.log"] = "546dc1bb5b6f46e0213b34415c5f84ba705a0fb9f94a7d57e5eab945efadfaff",
            ["artifacts/ci-consumers/runs/run.nvlwAtiy/prepare-sqlite.log"] = "7dd53cd57dbdf2806689f6d84b18266e5a2b8bbb78dec04903258f12b00b5211",
            ["artifacts/ci-consumers/runs/run.nvlwAtiy/verify-sqlite.log"] = "e631059cf4924e8435a951129b8e63146f49d547e049e3fc22dcc168e13e5acb",
            ["artifacts/ci-consumers/runs/run.nvlwAtiy/IQToolkit.Data.Access.json.delta.log"] = "450d40843ad5aee87e4773a9d2df0c66cea3ef8be8fd65c16d3c7035487ad7da",
            ["artifacts/ci-consumers/runs/run.nvlwAtiy/IQToolkit.Data.MySqlClient.json.delta.log"] = "67617527997d479dfb8d2abc30a9732014a7d6da6ec308e912b6986e590a7920",
            ["artifacts/ci-consumers/runs/run.nvlwAtiy/IQToolkit.Data.SQLite.json.delta.log"] = "db04981d7fb1e7418599a7cce65d741e900dd83bf43fb6d2a314fda8547460fc",
            ["artifacts/ci-consumers/runs/run.nvlwAtiy/IQToolkit.Data.SqlClient.json.delta.log"] = "99ee9c86c4c3eead478dd5f8cc12f3843b3fd3aa6936f7218568cfb58f3ab0ae",
            ["artifacts/ci-consumers/runs/run.nvlwAtiy/IQToolkit.Data.json.delta.log"] = "6dbeaf5f74192e31efe4ca7dac6667983e90f44a8ede555e47b64581ec26293a",
            ["artifacts/ci-consumers/runs/run.nvlwAtiy/IQToolkit.json.delta.log"] = "7c61e14029f91aa3546b781ad49cc5dfc6a54c7e99a4d68d48c11bec23371b37",
            ["artifacts/ci-consumers/runs/run.nvlwAtiy/SQLite-runtime.json.delta.log"] = "db04981d7fb1e7418599a7cce65d741e900dd83bf43fb6d2a314fda8547460fc",
        };
        foreach (var pin in pins) Capture(CacheRepositoryPath(pin.Key), pin.Value);
        using var resultDocument = Read(CacheRepositoryPath(outer + "/result.json"), pins[outer + "/result.json"]);
        var result = resultDocument.RootElement;
        Require(result.GetProperty("Status").GetString() == "passed" && result.GetProperty("ProtectingGateVerified").GetBoolean() &&
            result.GetProperty("SourcesUnchanged").GetBoolean() && result.GetProperty("ReviewedInputsUnchanged").GetBoolean() &&
            result.GetProperty("IndexUnchanged").GetBoolean() && result.GetProperty("PostRetirementFencesVerified").GetBoolean() &&
            result.GetProperty("CleanupErrors").GetArrayLength() == 0 && result.GetProperty("StorageErrors").GetArrayLength() == 0 &&
            result.GetProperty("Commands").GetArrayLength() == 2 && result.GetProperty("Commands").EnumerateArray().All(command =>
                command.GetProperty("Exit").GetInt32() == 0 && command.GetProperty("RemainingLiveMembers").GetArrayLength() == 0) &&
            result.GetProperty("ProducerEvidence").GetString() == CacheRepositoryPath(area) && !result.GetProperty("FullGoalComplete").GetBoolean(),
            "Current consumer native producer did not qualify its declared finite scope.");
        using var retirementDocument = Read(CacheRepositoryPath(outer + "/parent-retirement.json"), pins[outer + "/parent-retirement.json"]);
        var retirement = retirementDocument.RootElement;
        const string parent = "/tmp/iqtoolkit-native-string-consumers.gbcplt4h";
        Require(retirement.GetProperty("OriginalParent").GetString() == parent && !Directory.Exists(parent) &&
            retirement.GetProperty("ParentAbsent").GetBoolean() && retirement.GetProperty("NoOriginalLiveSessions").GetBoolean() &&
            retirement.GetProperty("InputsPreserved").GetBoolean() &&
            JsonElement.DeepEquals(retirement.GetProperty("OriginalFiles"), retirement.GetProperty("RetainedFiles")),
            "Current consumer parent retirement differs.");
        var mapping = File.ReadAllText(Capture(CacheRepositoryPath(area + "/retained-path-mapping.txt"), pins[area + "/retained-path-mapping.txt"]));
        Require(mapping == parent + "/tmp/iqtoolkit-ci-consumers.u0rR0012\t" + CacheRepositoryPath(area + "/environment") + "\n" &&
            File.ReadAllText(Capture(CacheRepositoryPath(area + "/status.txt"), pins[area + "/status.txt"])) == "passed\n",
            "Current consumer native mapping or status differs.");
        var before = File.ReadAllLines(Capture(CacheRepositoryPath(area + "/inputs-before.sha256"), pins[area + "/inputs-before.sha256"]));
        Require(before.Length == 41 && before.SequenceEqual(File.ReadAllLines(Capture(CacheRepositoryPath(area + "/inputs-after.sha256"),
            pins[area + "/inputs-after.sha256"]))), "Current consumer native input preservation differs.");
        foreach (var line in before)
        {
            var parts = line.Split("  ", 2, StringSplitOptions.None);
            Require(parts.Length == 2 && Regex.IsMatch(parts[0], "^[a-f0-9]{64}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)),
                "Current consumer native input binding is malformed.");
            var path = System.IO.Path.IsPathFullyQualified(parts[1]) ? parts[1] : System.IO.Path.Combine(repository, area, "source", parts[1]);
            Capture(path, parts[0]);
        }
        var feed = File.ReadAllLines(Capture(CacheRepositoryPath(area + "/tested-packages.sha256"), pins[area + "/tested-packages.sha256"]));
        var packageHashes = new Dictionary<string, string>(StringComparer.Ordinal);
        Require(feed.Length == 6, "Current tested consumer feed scope differs.");
        foreach (var line in feed)
        {
            var match = Regex.Match(line, "^([a-f0-9]{64})  (.+)\\.1\\.1\\.2-preview\\.1\\.nupkg$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
            Require(match.Success && Packages.Contains(match.Groups[2].Value, StringComparer.Ordinal), "Current tested feed contains an unreviewed package.");
            var archive = Capture(CacheRepositoryPath(area + "/tested-packages/" + match.Groups[2].Value + ".1.1.2-preview.1.nupkg"), match.Groups[1].Value);
            packageHashes.Add(match.Groups[2].Value, Convert.ToBase64String(SHA512.HashData(File.ReadAllBytes(archive))));
        }
        Require(packageHashes.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(Packages), "Current tested feed package identities differ.");
        var generatedFeed = File.ReadAllLines(Capture(CacheRepositoryPath(area + "/environment/eng/IndependentConsumers/prepared-feed.sha256"),
            "78c30fc8d148970ccd6e781a64ab8910de31c030a849b4331e558ef853356092"));
        Require(generatedFeed.Length == 6 && generatedFeed.ToHashSet(StringComparer.Ordinal).SetEquals(feed), "Current generated feed does not bind the tested archives.");
        foreach (var profile in Packages.Concat(new[] { "SQLite-runtime" }))
        {
            using var reviewed = Read(CacheRepositoryPath(area + "/reviewed-locks/" + profile + ".json"));
            using var generated = Read(CacheRepositoryPath(area + "/prepared-locks/" + profile + ".json"));
            var changes = new List<string>();
            foreach (var framework in reviewed.RootElement.GetProperty("dependencies").EnumerateObject())
            foreach (var node in framework.Value.EnumerateObject())
            {
                var now = generated.RootElement.GetProperty("dependencies").GetProperty(framework.Name).GetProperty(node.Name);
                if (node.Value.TryGetProperty("contentHash", out var oldHash) && now.GetProperty("contentHash").GetString() != oldHash.GetString())
                {
                    Require(packageHashes.TryGetValue(node.Name, out var expectedHash) && now.GetProperty("contentHash").GetString() == expectedHash,
                        "Current generated lock contentHash is not bound to its tested archive.");
                    changes.Add(JsonSerializer.Serialize(new { Package = node.Name, PreviousContentHash = oldHash.GetString(), PreparedContentHash = expectedHash }));
                }
            }
            var log = File.ReadAllLines(Capture(CacheRepositoryPath(area + "/" + profile + ".json.delta.log"), pins[area + "/" + profile + ".json.delta.log"]));
            Require(log.Length == changes.Count + 1 && log[^1] == "Verified complete lock equality except feed-bound toolkit contentHash leaves." &&
                log.Take(changes.Count).Zip(changes).All(pair =>
                {
                    using var actual = JsonDocument.Parse(pair.First); using var expected = JsonDocument.Parse(pair.Second);
                    return JsonElement.DeepEquals(actual.RootElement, expected.RootElement);
                }), "Current consumer native graph review does not match its actual generated changes.");
        }
        var stages = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["artifacts/independent-consumers/prepare-runs/run.ZQfM4VPW"] = "prepared",
            ["artifacts/independent-consumers/verify-runs/run.1eywnLSn"] = "passed",
            ["artifacts/package-consumer/lock-preparation-runs/run.DIgyIFY2"] = "prepared",
            ["artifacts/package-consumer/runs/run.CnTkI9Hd"] = "passed"
        };
        foreach (var stage in stages)
            Require(File.ReadAllText(Capture(CacheRepositoryPath(area + "/environment/" + stage.Key + "/status.txt"))) == stage.Value + "\n",
                "Current consumer retained stage status differs.");
        return new { Status = "verified-finite-consumer-copy-chain", NativeProducer = CacheRepositoryPath(area),
            NativeInputs = 41, TestedArchives = 6, GraphsReviewed = 7, NewNativeExecution = false, NewDependencyGraphQualified = false };
    }

    private static IEnumerable<string> CacheSourceFiles(string root)
    {
        CacheNativeStage.NoLinkedPath(root);
        foreach (var file in Directory.EnumerateFiles(root)) { CacheNativeStage.NoLinkedPath(file); yield return file; }
        foreach (var directory in Directory.EnumerateDirectories(root))
            if (System.IO.Path.GetFileName(directory) is not "bin" and not "obj")
                foreach (var file in CacheSourceFiles(directory)) yield return file;
    }

    private static int CacheValidateRuntime()
    {
        using var initialDocument = Read(System.IO.Path.Combine(repository, CacheArea, "initial/compiled-inputs.json"), CacheCompiledPins["initial"]);
        var original = initialDocument.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.GetString()!, StringComparer.Ordinal);
        Require(original.Count == 219, "Retained compilation/runtime output scope differs.");
        var changing = new[] { "IQToolkit", "IQToolkit.Data", "IQToolkit.Data.SQLite", "IQToolkit.Data.SqlClient", "IQToolkitTest" }
            .SelectMany(name => new[] { name + ".dll", name + ".pdb" }).ToHashSet(StringComparer.Ordinal);
        changing.Add(".msCoverageSourceRootsMapping_IQToolkitTest");
        foreach (var stage in CacheCompiledPins)
        {
            using var document = Read(System.IO.Path.Combine(repository, CacheArea, stage.Key, "compiled-inputs.json"), stage.Value);
            var files = document.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.GetString()!, StringComparer.Ordinal);
            Require(files.Count == original.Count && original.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(files.Keys) &&
                original.All(pair => changing.Contains(pair.Key) || files[pair.Key] == pair.Value), "Native retained runtime dependency identities changed between stages.");
        }
        foreach (var file in original.Where(pair => !changing.Contains(pair.Key)))
        {
            Require(!System.IO.Path.IsPathRooted(file.Key) && file.Key.Split('/').All(part => part is not "." and not ".." && part.Length > 0), "Invalid native runtime relative path.");
            Capture(System.IO.Path.Combine(repository, CacheArea, "initial/compiled", file.Key), file.Value);
            CacheCaptureDuplicate(System.IO.Path.Combine(repository, "IQToolkitTest/bin/Release/net10.0", file.Key), file.Value);
        }
        return original.Count - changing.Count;
    }
}

internal static class CacheNativeStage
{
    private const long MaximumInputBytes = 16 * 1024 * 1024;
    private const long MaximumTotalBytes = 128 * 1024 * 1024;
    private const int MaximumTextCharacters = 64 * 1024;
    private static readonly XNamespace TrxNamespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
    private static readonly Guid Sha256Algorithm = new("8829d00f-11b8-4213-878b-770e8597ac16");
    private static readonly Dictionary<string, CapturedInput> Inputs = new(StringComparer.Ordinal);
    private static readonly List<NativeDiagnostic> Diagnostics = [];
    private static long totalBytes;

    private sealed record Input(string Path, string Sha256);
    private sealed record Module(string Name, Input Dll, Input Pdb);
    private sealed record SourceRoot(string Virtual, string Physical);
    private sealed record ExpectedTest(string Class, string Method, string Outcome, string Source,
        string Sha256, int FailureLine, string Assertion);
    private sealed record Descriptor(int Schema, string Worktree, Input Trx, Module TestAssembly,
        Module[] Modules, Dictionary<string, string> Sources, ExpectedTest[] ExpectedTests,
        Input Mapping, SourceRoot[]? SourceRoots);
    private sealed record CapturedInput(byte[] Bytes, string Sha256);
    private sealed record DocumentEvidence(string Module, string Name, string Path, string Sha256, string Kind);
    private sealed record MethodEvidence(string Class, string Method, string Source, string Sha256,
        string MetadataToken, string BodySha256, int[] SequenceLines);
    private sealed record NativeObservation(string Version, string Source, object[] Requests);
    private sealed record ExecutionEvidence(string Class, string Method, string TestId, string ExecutionId,
        string Outcome, string StdOut, string Message, string StackTrace, NativeObservation Native,
        MethodEvidence CompiledMethod);
    private sealed record NativeDiagnostic(string Class, string Method, string ExecutionId, string Outcome,
        string StdOut, string Message, string StackTrace);
    private sealed record Target(string Class, string Source, int Line, string Family);
    private static readonly Dictionary<string, Target> Targets = new(StringComparer.Ordinal)
    {
        ["TypedQueryCompilationPreservesCapturedNativeRows"] = CacheTarget(0, string.Empty),
        ["UntypedQueryExecutesCapturedNativeRows"] = CacheTarget(38, "IsInstanceOfType"),
        ["HeadHitReturnsTheMatchingNativeOption"] = CacheTarget(63, "AreSame"),
        ["NonaddingMissReturnsDefaultInsteadOfAnotherNativeOption"] = CacheTarget(51, "IsNull"),
        ["AddingHitPromotesItsNativeOptionBeforeEviction"] = CacheTarget(77, "IsTrue"),
        ["CapturedSelectionCanChangeBetweenObservedNativeOptions"] = CacheTarget(186, "AreEqual"),
        ["SharedCacheKeepsDistinctNativeProviderPoliciesSeparate"] = new(
            "IQToolkitTest.RealProvider.NativeProviderCacheTests",
            "IQToolkitTest/RealProvider/NativeProviderCacheTests.cs", 128, "CollectionAssert.AreEquivalent")
    };

    private static Target CacheTarget(int line, string family) => new(
        "IQToolkitTest.RealProvider.CacheMetadataTests", "IQToolkitTest/RealProvider/CacheMetadataTests.cs", line, family);

    private static Func<string, string> ResolveInput = null!;
    private static Func<string, string> ExpectedInputHash = null!;

    internal static JsonElement Validate(string descriptorPath, Func<string, string> resolve, Func<string, string> expected)
    {
        Inputs.Clear(); Diagnostics.Clear(); totalBytes = 0;
        ResolveInput = resolve; ExpectedInputHash = expected;
        var descriptor = ReadDescriptor(descriptorPath);
        var documents = new List<DocumentEvidence>();
        foreach (var module in descriptor.Modules.Append(descriptor.TestAssembly)) VerifySymbols(module, descriptor, documents);
        var executions = VerifyExecutions(descriptor, documents);
        VerifyUnchanged();
        return JsonSerializer.SerializeToElement(new
        {
            Status = "verified", descriptor.Worktree, Executions = executions, Documents = documents, ActualInputPaths = Inputs.Keys.Order(StringComparer.Ordinal).ToArray(),
            SourceQualified = true, BroadCacheContractVerified = false, SecurityVerified = false, ConcurrencyVerified = false
        });
    }

    internal static void NoLinkedPath(string path) => NoLinks(path);

    private static void Require([DoesNotReturnIf(false)] bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static void NoLinks(string path)
    {
        FileSystemInfo current = File.Exists(path) ? new FileInfo(path) : new DirectoryInfo(path);
        while (true)
        {
            Require(current.Exists && current.LinkTarget is null && (current.Attributes & FileAttributes.ReparsePoint) == 0,
                "Missing or linked input path: " + path);
            var parent = Path.GetDirectoryName(current.FullName);
            if (string.IsNullOrEmpty(parent) || parent == current.FullName) break;
            current = new DirectoryInfo(parent);
        }
    }

    private static byte[] Capture(string path, string? expected = null)
    {
        Require(Path.IsPathFullyQualified(path), "Historical native input path must be absolute.");
        path = Path.GetFullPath(path);
        var pinned = ExpectedInputHash(path);
        Require(expected is null || pinned.Equals(expected, StringComparison.OrdinalIgnoreCase), "Historical native input hash differs from its stage alias.");
        var physical = ResolveInput(path);
        NoLinks(physical);
        if (!Inputs.TryGetValue(path, out var captured))
        {
            var snapshot = Program.CaptureBound(physical, pinned);
            var bytes = File.ReadAllBytes(snapshot);
            Require(bytes.LongLength <= MaximumInputBytes && Inputs.Count < 4096 && totalBytes + bytes.LongLength <= MaximumTotalBytes,
                "Historical native stage input count/byte bound exceeded.");
            captured = new CapturedInput(bytes, Hash(bytes));
            Inputs.Add(path, captured); totalBytes += bytes.LongLength;
        }
        Require(captured.Sha256.Equals(pinned, StringComparison.OrdinalIgnoreCase), "Historical native snapshot hash differs.");
        return captured.Bytes;
    }

    private static byte[] Capture(Input input)
    {
        Require(input is not null && ValidHash(input.Sha256), "Invalid descriptor input hash.");
        return Capture(input.Path, input.Sha256);
    }

    private static bool ValidHash(string hash) => hash is not null && Match(hash, "^[0-9A-Fa-f]{64}$");
    private static bool Match(string text, string pattern) => Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private static void VerifyUnchanged()
    {
        foreach (var pair in Inputs)
        {
            var physical = ResolveInput(pair.Key); NoLinks(physical);
            var snapshot = Program.CaptureBound(physical, pair.Value.Sha256);
            Require(Hash(File.ReadAllBytes(snapshot)).Equals(pair.Value.Sha256, StringComparison.OrdinalIgnoreCase), "Historical native snapshot stability failed.");
        }
        // The coordinator checks every actual retained physical input again before publication.
    }

    private static void RejectDuplicateProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                Require(names.Add(property.Name), "Duplicate JSON property: " + property.Name);
                RejectDuplicateProperties(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) RejectDuplicateProperties(item);
    }

    private static Descriptor ReadDescriptor(string path)
    {
        using var json = JsonDocument.Parse(Capture(path), new JsonDocumentOptions { MaxDepth = 32 });
        RejectDuplicateProperties(json.RootElement);
        var options = new JsonSerializerOptions { UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow };
        var descriptor = json.Deserialize<Descriptor>(options) ?? throw new InvalidDataException("Missing descriptor.");
        Require(descriptor.Schema == 1 && Path.IsPathFullyQualified(descriptor.Worktree), "Unsupported schema or worktree.");
        Require(descriptor.Worktree == Path.GetFullPath(descriptor.Worktree).TrimEnd(Path.DirectorySeparatorChar), "Noncanonical worktree root.");
        Require(descriptor.Worktree == "/tmp/iqtoolkit-cache-seeds.wapfh628/worktree", "Historical native worktree differs.");
        // The authenticated disposition moved this genuine worktree; aliases preserve bytes,
        // while the unchanged descriptor/mapping retain their original logical paths.
        Require(descriptor.TestAssembly.Name == "IQToolkitTest" && descriptor.Modules.Length == 4 &&
            descriptor.Modules.Select(module => module.Name).ToHashSet(StringComparer.Ordinal).SetEquals(
                new[] { "IQToolkit", "IQToolkit.Data", "IQToolkit.Data.SQLite", "IQToolkit.Data.SqlClient" }), "Unexpected module set.");
        var binaries = new HashSet<string>(StringComparer.Ordinal);
        foreach (var module in descriptor.Modules.Append(descriptor.TestAssembly))
        {
            foreach (var input in new[] { module.Dll, module.Pdb })
            {
                Require(IsUnder(input.Path, descriptor.Worktree) && binaries.Add(Path.GetFullPath(input.Path)), "Binary is outside worktree or duplicated.");
                Capture(input);
            }
            Require(Path.GetFileName(module.Dll.Path) == module.Name + ".dll" &&
                Path.GetFileName(module.Pdb.Path) == module.Name + ".pdb", "Unexpected binary filename.");
        }
        Capture(descriptor.Trx);
        Require(descriptor.Sources.Count is > 0 and <= 4096, "Missing or excessive source manifest.");
        foreach (var source in descriptor.Sources)
        {
            Require(ValidHash(source.Value), "Invalid source manifest hash.");
            Capture(RepositoryPath(descriptor, source.Key), source.Value);
        }
        Require(descriptor.ExpectedTests.Length is > 0 and <= 7 &&
            descriptor.ExpectedTests.Select(test => test.Class + "." + test.Method).Distinct(StringComparer.Ordinal).Count() == descriptor.ExpectedTests.Length,
            "Empty, excessive or duplicate expected native tests.");
        foreach (var test in descriptor.ExpectedTests)
        {
            Require(Targets.TryGetValue(test.Method, out var target) && target.Class == test.Class && target.Source == test.Source,
                "Unexpected scoped native test identity or source.");
            Require(test.Outcome is "Passed" or "Failed" && ValidHash(test.Sha256) &&
                descriptor.Sources.TryGetValue(test.Source, out var hash) && hash.Equals(test.Sha256, StringComparison.OrdinalIgnoreCase),
                "Test source/outcome differs from manifest.");
            if (test.Outcome == "Passed") Require(test.FailureLine == 0 && test.Assertion == string.Empty, "Passing test has failure expectations.");
            else
            {
                Require(target.Line > 0 && test.FailureLine == target.Line && CanonicalFamily(test.Assertion) == target.Family,
                    "Failure expectation is not the reviewed assertion site.");
                var lines = Encoding.UTF8.GetString(Capture(RepositoryPath(descriptor, test.Source), test.Sha256)).Split('\n');
                Require(test.FailureLine <= lines.Length && Match(lines[test.FailureLine - 1],
                    Regex.Escape(QualifiedFamily(target.Family)) + @"(?:<[^;\r\n]+>)?\s*\("),
                    "Reviewed assertion is absent from the expected unchanged source line.");
            }
        }
        var failures = descriptor.ExpectedTests.Count(test => test.Outcome == "Failed");
        Require(failures == 0 || (failures == 1 && descriptor.ExpectedTests.Length == 2 && descriptor.ExpectedTests.Any(test =>
            test.Method == "TypedQueryCompilationPreservesCapturedNativeRows" && test.Outcome == "Passed")),
            "An intended assertion kill requires the unchanged typed native control to pass.");
        VerifyMapping(descriptor);
        return descriptor;
    }

    private static bool IsUnder(string path, string directory) => Path.IsPathFullyQualified(path) &&
        Path.GetFullPath(path).StartsWith(Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    private static string RepositoryPath(Descriptor descriptor, string relative)
    {
        Require(!string.IsNullOrEmpty(relative) && !Path.IsPathRooted(relative) && !relative.Contains('\\') &&
            relative.Split('/').All(part => part.Length > 0 && part is not "." and not ".."), "Noncanonical relative source path.");
        var path = Path.GetFullPath(Path.Combine(descriptor.Worktree, relative));
        Require(IsUnder(path, descriptor.Worktree), "Source escapes worktree.");
        return path;
    }

    private static Dictionary<string, string> Roots(Descriptor descriptor)
    {
        var roots = new Dictionary<string, string>(StringComparer.Ordinal) { ["/_/"] = descriptor.Worktree + Path.DirectorySeparatorChar };
        foreach (var root in descriptor.SourceRoots ?? [])
        {
            Require(root.Virtual == "/_1/" && Path.IsPathFullyQualified(root.Physical) &&
                root.Physical == Path.GetFullPath(root.Physical).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar &&
                roots.TryAdd(root.Virtual, root.Physical), "Unsupported, duplicated or noncanonical additional source root.");
            Require(root.Physical == "/home/firestrand/Projects/IQToolkit/artifacts/nuget/packages/", "Historical external compiler source root differs.");
        }
        return roots;
    }

    private static void VerifyMapping(Descriptor descriptor)
    {
        Require(descriptor.Mapping.Path == Path.Combine(Path.GetDirectoryName(descriptor.TestAssembly.Dll.Path)!,
            ".msCoverageSourceRootsMapping_IQToolkitTest"), "Mapping is not the native test-output mapping file.");
        var bytes = Capture(descriptor.Mapping);
        Require(bytes.Length >= 2 && bytes[0] == 0xff && bytes[1] == 0xfe && bytes.Length % 2 == 0, "Mapping is not native UTF16-LE.");
        var text = new UnicodeEncoding(false, true, true).GetString(bytes).TrimStart('\ufeff');
        var roots = Roots(descriptor);
        var observed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in text.Split('\n').Select(line => line.TrimEnd('\r')).Where(line => line.Length > 0))
        {
            var separator = line.IndexOf('|'); var equals = line.LastIndexOf('=');
            Require(separator > 0 && equals > separator && IsUnder(line[..separator], descriptor.Worktree), "Invalid native source-root mapping record.");
            var physical = line[(separator + 1)..equals]; var virtualRoot = line[(equals + 1)..];
            Require(roots.TryGetValue(virtualRoot, out var expected) && physical == expected, "Native source-root mapping disagrees with descriptor.");
            observed.Add(virtualRoot);
        }
        Require(observed.SetEquals(roots.Keys), "Declared source root lacks a native mapping record.");
    }

    private static string DocumentPath(Descriptor descriptor, string name)
    {
        if (Path.IsPathFullyQualified(name) && IsUnder(name, descriptor.Worktree)) return Path.GetFullPath(name);
        foreach (var root in Roots(descriptor))
        {
            if (!name.StartsWith(root.Key, StringComparison.Ordinal)) continue;
            var suffix = name[root.Key.Length..];
            Require(suffix.Length > 0 && !suffix.Contains('\\') && suffix.Split('/').All(part => part.Length > 0 && part is not "." and not ".."),
                "Noncanonical virtual PDB document.");
            var full = Path.GetFullPath(Path.Combine(root.Value, suffix));
            Require(IsUnder(full, root.Value), "PDB document escapes its authenticated source root.");
            return full;
        }
        throw new InvalidDataException("Unmapped portable-PDB document root: " + name);
    }

    private static void VerifySymbols(Module module, Descriptor descriptor, List<DocumentEvidence> documents)
    {
        using var pe = new PEReader(new MemoryStream(Capture(module.Dll), writable: false));
        var pdb = Capture(module.Pdb);
        using var provider = MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(pdb, writable: false));
        var symbols = provider.GetMetadataReader();
        var header = symbols.DebugMetadataHeader ?? throw new InvalidDataException("Missing portable PDB header.");
        var views = pe.ReadDebugDirectory().Where(entry => entry.Type == DebugDirectoryEntryType.CodeView).ToArray();
        Require(header.Id.Length == 20 && views.Length == 1 && views[0].IsPortableCodeView, "Unsupported DLL/PDB identity.");
        var view = pe.ReadCodeViewDebugDirectoryData(views[0]);
        Require(view.Age == 1 && new BlobContentId(header.Id) == new BlobContentId(view.Guid, views[0].Stamp), "DLL and portable PDB identities differ.");
        var checksums = pe.ReadDebugDirectory().Where(entry => entry.Type == DebugDirectoryEntryType.PdbChecksum).ToArray();
        Require(checksums.Length == 1 && header.IdStartOffset >= 0 && header.IdStartOffset <= pdb.Length - header.Id.Length, "Missing PDB checksum or invalid identity offset.");
        var checksum = pe.ReadPdbChecksumDebugDirectoryData(checksums[0]);
        var cleared = (byte[])pdb.Clone(); Array.Clear(cleared, header.IdStartOffset, header.Id.Length);
        Require(checksum.AlgorithmName == "SHA256" && checksum.Checksum.AsSpan().SequenceEqual(SHA256.HashData(cleared)), "DLL PDB checksum differs.");
        var metadata = pe.GetMetadataReader();
        Require(metadata.IsAssembly && metadata.GetString(metadata.GetAssemblyDefinition().Name) == module.Name, "Assembly identity differs from descriptor.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var handle in symbols.Documents)
        {
            var document = symbols.GetDocument(handle); var name = symbols.GetString(document.Name);
            Require(symbols.GetGuid(document.HashAlgorithm) == Sha256Algorithm, "Unsupported PDB source checksum algorithm.");
            var hash = Convert.ToHexString(symbols.GetBlobBytes(document.Hash)).ToLowerInvariant();
            Require(ValidHash(hash), "Invalid PDB source checksum.");
            var path = DocumentPath(descriptor, name);
            Require(seen.Add(path), "Duplicate portable-PDB source document.");
            string kind;
            if (IsUnder(path, descriptor.Worktree))
            {
                var relative = Path.GetRelativePath(descriptor.Worktree, path).Replace('\\', '/');
                if (descriptor.Sources.TryGetValue(relative, out var expected))
                {
                    Require(hash.Equals(expected, StringComparison.OrdinalIgnoreCase), "Compiled source differs from copied-source manifest.");
                    kind = "copied-source";
                }
                else
                {
                    Require(relative.StartsWith(module.Name + "/obj/", StringComparison.Ordinal) && relative.EndsWith(".cs", StringComparison.Ordinal),
                        "Compiled non-generated source is absent from copied-source manifest.");
                    kind = "retained-generated-obj-source";
                }
            }
            else
            {
                Require(module.Name == "IQToolkitTest" &&
                    name == "/_1/microsoft.net.test.sdk/18.10.1/build/net8.0/Microsoft.NET.Test.Sdk.Program.cs",
                    "Unsupported external compiler source.");
                kind = "retained-external-test-sdk-source";
            }
            Capture(path, hash);
            documents.Add(new DocumentEvidence(module.Name, name, path, hash, kind));
        }
        Require(seen.Count > 0, "Portable PDB has no authenticated documents.");
    }

    private static MethodEvidence BindMethod(ExpectedTest test, Descriptor descriptor, List<DocumentEvidence> documents)
    {
        using var pe = new PEReader(new MemoryStream(Capture(descriptor.TestAssembly.Dll), writable: false));
        using var provider = MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(Capture(descriptor.TestAssembly.Pdb), writable: false));
        var metadata = pe.GetMetadataReader(); var symbols = provider.GetMetadataReader();
        var methods = metadata.TypeDefinitions.Select(metadata.GetTypeDefinition)
            .Where(type => metadata.GetString(type.Namespace) + "." + metadata.GetString(type.Name) == test.Class)
            .SelectMany(type => type.GetMethods()).Where(handle => metadata.GetString(metadata.GetMethodDefinition(handle).Name) == test.Method).ToArray();
        Require(methods.Length == 1, "Missing or ambiguous compiled test method.");
        var method = metadata.GetMethodDefinition(methods[0]);
        Require(method.RelativeVirtualAddress > 0 && (method.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public,
            "Compiled test lacks a public executable body.");
        var body = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes();
        Require(body is { Length: > 0 }, "Compiled test body is empty.");
        var debug = symbols.GetMethodDebugInformation(methods[0]);
        var sourcePath = RepositoryPath(descriptor, test.Source);
        var lines = new List<int>();
        foreach (var point in debug.GetSequencePoints().Where(point => !point.IsHidden))
        {
            var handle = point.Document.IsNil ? debug.Document : point.Document;
            Require(!handle.IsNil, "Compiled method lacks a source document.");
            var name = symbols.GetString(symbols.GetDocument(handle).Name);
            if (DocumentPath(descriptor, name) != sourcePath) continue;
            Require(documents.Any(document => document.Module == "IQToolkitTest" && document.Path == sourcePath &&
                document.Sha256.Equals(test.Sha256, StringComparison.OrdinalIgnoreCase)), "Compiled test source checksum differs.");
            for (var line = point.StartLine; line <= point.EndLine && line - point.StartLine < 4096; line++) lines.Add(line);
        }
        Require(lines.Count > 0 && (test.Outcome == "Passed" || lines.Contains(test.FailureLine)), "Compiled body does not bind the reviewed test assertion/document.");
        return new MethodEvidence(test.Class, test.Method, test.Source, test.Sha256,
            "0x" + System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(methods[0]).ToString("X8", CultureInfo.InvariantCulture),
            Hash(body), lines.Distinct().Order().ToArray());
    }

    private static string Attribute(XElement element, string name) => (string?)element.Attribute(name) ?? throw new InvalidDataException("Missing native TRX attribute: " + name);
    private static XElement One(XElement element, string name)
    {
        var children = element.Elements(TrxNamespace + name).ToArray();
        Require(children.Length == 1, "Missing or duplicate native TRX element: " + name);
        return children[0];
    }
    private static int Count(XElement counters, string name)
    {
        Require(int.TryParse(Attribute(counters, name), NumberStyles.None, CultureInfo.InvariantCulture, out var count) && count >= 0,
            "Invalid native TRX counter.");
        return count;
    }
    private static string BoundedText(XElement element)
    {
        var text = element.Value; Require(text.Length <= MaximumTextCharacters, "Native diagnostic text exceeds bound."); return text;
    }

    private static ExecutionEvidence[] VerifyExecutions(Descriptor descriptor, List<DocumentEvidence> documents)
    {
        using var stream = new MemoryStream(Capture(descriptor.Trx), writable: false);
        using var xml = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null, MaxCharactersInDocument = MaximumInputBytes });
        var root = XDocument.Load(xml).Root ?? throw new InvalidDataException("Missing native TRX root.");
        Require(root.Name == TrxNamespace + "TestRun" && Guid.TryParse(Attribute(root, "id"), out _), "Unexpected native TRX root/run identity.");
        var definitions = One(root, "TestDefinitions").Elements(TrxNamespace + "UnitTest").ToArray();
        var results = One(root, "Results").Elements(TrxNamespace + "UnitTestResult").ToArray();
        Require(definitions.Length == descriptor.ExpectedTests.Length && results.Length == definitions.Length &&
            One(root, "TestDefinitions").Elements().Count() == definitions.Length && One(root, "Results").Elements().Count() == results.Length,
            "Native discovery/result set differs from exact expected tests.");
        var byId = new Dictionary<string, XElement>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            var id = Attribute(definition, "id");
            Require(Guid.TryParse(id, out _) && byId.TryAdd(id, definition), "Invalid or duplicate native test definition identity.");
        }
        var expected = descriptor.ExpectedTests.ToDictionary(test => test.Class + "." + test.Method, StringComparer.Ordinal);
        var observed = new HashSet<string>(StringComparer.Ordinal); var executionIds = new HashSet<string>(StringComparer.Ordinal);
        var resultIds = new HashSet<string>(StringComparer.Ordinal); var executions = new List<ExecutionEvidence>();
        foreach (var result in results)
        {
            var id = Attribute(result, "testId"); var executionId = Attribute(result, "executionId");
            Require(byId.TryGetValue(id, out var definition) && resultIds.Add(id) && Guid.TryParse(executionId, out _) && executionIds.Add(executionId),
                "Unlinked or duplicate native execution identity.");
            Require(Attribute(One(definition, "Execution"), "id") == executionId, "Native definition/result execution IDs differ.");
            var method = One(definition, "TestMethod");
            var className = Attribute(method, "className"); var methodName = Attribute(method, "name"); var key = className + "." + methodName;
            Require(expected.TryGetValue(key, out var test) && observed.Add(key) && Attribute(result, "testName") == methodName &&
                Attribute(definition, "name") == methodName && Attribute(method, "adapterTypeName") == "executor://mstestadapter/v4",
                "Unexpected native test method/name/adapter.");
            var codeBase = Attribute(method, "codeBase");
            Require((Path.IsPathFullyQualified(codeBase) && Path.GetFullPath(codeBase) == Path.GetFullPath(descriptor.TestAssembly.Dll.Path)) ||
                (!codeBase.Contains('/') && !codeBase.Contains('\\') && codeBase == Path.GetFileName(descriptor.TestAssembly.Dll.Path)),
                "Native codeBase does not bind the actual private test DLL.");
            var outcome = Attribute(result, "outcome");
            Require(outcome is "Passed" or "Failed" && outcome == test.Outcome, "Native outcome differs from exact expected outcome.");
            var output = One(result, "Output"); var stdout = BoundedText(One(output, "StdOut"));
            Require(!output.Elements(TrxNamespace + "StdErr").Any(element => element.Value.Length > 0), "Unexpected native test stderr.");
            var errors = output.Elements(TrxNamespace + "ErrorInfo").ToArray(); string message = string.Empty; string stack = string.Empty;
            if (errors.Length == 1)
            {
                message = BoundedText(One(errors[0], "Message")); stack = BoundedText(One(errors[0], "StackTrace"));
            }
            Diagnostics.Add(new NativeDiagnostic(className, methodName, executionId, outcome, stdout, message, stack));
            var native = VerifyNative(stdout, test);
            var compiled = BindMethod(test, descriptor, documents);
            if (outcome == "Passed") Require(errors.Length == 0, "Passing native test has failure fields.");
            else
            {
                Require(errors.Length == 1, "Failed native test lacks unique error information.");
                VerifyAssertion(message, stack, test, descriptor);
            }
            executions.Add(new ExecutionEvidence(className, methodName, id, executionId, outcome, stdout, message, stack, native, compiled));
        }
        Require(observed.SetEquals(expected.Keys), "Native expected test set is incomplete.");
        var entries = One(root, "TestEntries").Elements(TrxNamespace + "TestEntry").ToArray();
        Require(entries.Length == results.Length && One(root, "TestEntries").Elements().Count() == entries.Length &&
            entries.Select(entry => Attribute(entry, "testId")).Distinct(StringComparer.Ordinal).Count() == entries.Length &&
            entries.All(entry => executions.Any(execution => execution.TestId == Attribute(entry, "testId") &&
                execution.ExecutionId == Attribute(entry, "executionId"))), "Native test-entry links differ.");
        var summary = One(root, "ResultSummary"); var counters = One(summary, "Counters");
        var passed = executions.Count(execution => execution.Outcome == "Passed"); var failed = executions.Count - passed;
        Require(Count(counters, "total") == executions.Count && Count(counters, "executed") == executions.Count &&
            Count(counters, "passed") == passed && Count(counters, "failed") == failed &&
            Attribute(summary, "outcome") == (failed > 0 ? "Failed" : "Completed"), "Native summary/counts disagree with actual linked executions.");
        foreach (var name in new[] { "error", "timeout", "aborted", "inconclusive", "passedButRunAborted", "notRunnable", "notExecuted",
            "disconnected", "warning", "completed", "inProgress", "pending" }) Require(Count(counters, name) == 0, "Skipped or incomplete native test stage.");
        foreach (var info in summary.Elements(TrxNamespace + "RunInfos").Elements(TrxNamespace + "RunInfo"))
            Require(Attribute(info, "outcome") == "Passed", "Native run-level setup/discovery error.");
        Require(executions.Select(execution => execution.Native.Version).Distinct(StringComparer.Ordinal).Count() == 1 &&
            executions.Select(execution => execution.Native.Source).Distinct(StringComparer.Ordinal).Count() == 1, "Native SQLite provenance differs between stage results.");
        return executions.ToArray();
    }

    private static string CanonicalFamily(string family) => family.StartsWith("Assert.", StringComparison.Ordinal) ? family[7..] : family;
    private static string QualifiedFamily(string family) => family.StartsWith("CollectionAssert.", StringComparison.Ordinal) ? family : "Assert." + family;

    private static void VerifyAssertion(string message, string stack, ExpectedTest test, Descriptor descriptor)
    {
        var family = QualifiedFamily(CanonicalFamily(test.Assertion));
        Require(message.Contains(family, StringComparison.Ordinal) &&
            (message.StartsWith("Assertion failed.", StringComparison.Ordinal) || message.Contains("AssertFailedException", StringComparison.Ordinal) ||
                Match(message, Regex.Escape(family) + @"\b[^\r\n]*\bfailed\b")), "Failure is not the intended MSTest assertion family.");
        var exceptions = Regex.Matches(message, @"\b[A-Za-z_][A-Za-z0-9_.]*Exception\b", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        Require(exceptions.All(match => match.Value.EndsWith("AssertFailedException", StringComparison.Ordinal)), "Native failure reports a nonassertion exception.");
        var frames = stack.Split('\n').Select(line => line.Trim()).Where(line => line.StartsWith("at IQToolkitTest.", StringComparison.Ordinal)).ToArray();
        Require(frames.Length > 0, "Native assertion lacks a test-source stack frame.");
        var prefix = "at " + test.Class + "." + test.Method + "() in ";
        var suffix = ":line " + test.FailureLine.ToString(CultureInfo.InvariantCulture);
        Require(frames[0].StartsWith(prefix, StringComparison.Ordinal) && frames[0].EndsWith(suffix, StringComparison.Ordinal),
            "Native failure is at another test/method/assertion line.");
        var file = frames[0][prefix.Length..^suffix.Length];
        Require(DocumentPath(descriptor, file) == RepositoryPath(descriptor, test.Source), "Native assertion stack document differs from compiled source.");
    }

    private static NativeObservation VerifyNative(string stdout, ExpectedTest test)
    {
        var lines = stdout.Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
        var versions = lines.Where(line => line.StartsWith("Native SQLite version: ", StringComparison.Ordinal)).Select(line => line[23..]).ToArray();
        var sources = lines.Where(line => line.StartsWith("Native SQLite source: ", StringComparison.Ordinal)).Select(line => line[22..]).ToArray();
        Require(versions.Length > 0 && sources.Length == versions.Length && versions.Distinct(StringComparer.Ordinal).Count() == 1 &&
            sources.Distinct(StringComparer.Ordinal).Count() == 1 && Match(versions[0], @"^\d+\.\d+\.\d+$") &&
            Match(sources[0], @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} [0-9a-fA-F]{40,64}$"), "Missing or inconsistent observed native SQLite provenance.");
        var requests = new List<(string Request, string Parameter, string[] Rows, string Kind)>();
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (line.StartsWith("Native request: ", StringComparison.Ordinal))
            {
                Require(index + 1 < lines.Length && lines[index + 1].StartsWith("Native rows: ", StringComparison.Ordinal), "Native request lacks actual rows.");
                requests.Add((line[16..], string.Empty, NativeRows(lines[++index][13..]), "SQLiteMetadata"));
            }
            else if (line.StartsWith("Native oracle request: ", StringComparison.Ordinal))
            {
                Require(index + 2 < lines.Length && lines[index + 1].StartsWith("Native oracle parameter: ", StringComparison.Ordinal) &&
                    lines[index + 2].StartsWith("Native oracle rows: ", StringComparison.Ordinal), "Native parameter oracle lacks actual observations.");
                requests.Add((line[23..], lines[index + 1][25..], NativeRows(lines[index + 2][20..]), "native-parameter-oracle"));
                index += 2;
            }
        }
        if (test.Class == "IQToolkitTest.RealProvider.NativeProviderCacheTests")
        {
            Require(lines.Contains("Native schema request: SELECT * FROM pragma_compile_options LIMIT 0;", StringComparer.Ordinal) &&
                lines.Contains("Native column: compile_options System.String", StringComparer.Ordinal), "Native policy schema acquisition is missing.");
            var options = requests.Where(request => request.Request == "SELECT compile_options FROM pragma_compile_options;" && request.Kind == "native-parameter-oracle").ToArray();
            var parameters = requests.Where(request => request.Request == "SELECT compile_options FROM pragma_compile_options WHERE compile_options = @name;" &&
                request.Kind == "native-parameter-oracle").ToArray();
            Require(options.Length == 1 && options[0].Rows.Distinct(StringComparer.Ordinal).Count() >= 2 && parameters.Length == 2 &&
                parameters.Select(request => request.Parameter).Distinct(StringComparer.Ordinal).Count() == 2 &&
                parameters.All(request => request.Parameter.Length > 0 && options[0].Rows.Contains(request.Parameter, StringComparer.Ordinal) &&
                    request.Rows.Length > 0 && request.Rows.All(value => value == request.Parameter)) &&
                requests.Any(request => request.Request == "SELECT sqlite_version();" && request.Kind == "SQLiteMetadata" &&
                    request.Rows.Length == 1 && request.Rows[0] == versions[0]), "Native policy input or independent parameter-oracle prerequisite failed.");
        }
        else
        {
            Require(requests.Any(request => request.Request == "PRAGMA compile_options;" && request.Kind == "SQLiteMetadata" &&
                request.Rows.Length >= 3 && request.Rows.Distinct(StringComparer.Ordinal).Count() == request.Rows.Length),
                "Native cache case lacks three distinct unchanged actual PRAGMA options.");
        }
        return new NativeObservation(versions[0], sources[0], requests.Select(request => (object)new
            { request.Request, request.Parameter, request.Rows, request.Kind }).ToArray());
    }

    private static string[] NativeRows(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 8 });
        Require(document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() is > 0 and <= 4096 &&
            document.RootElement.EnumerateArray().All(value => value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 }),
            "Actual native rows are missing or not a bounded nonempty string array.");
        return document.RootElement.EnumerateArray().Select(value => value.GetString()!).ToArray();
    }
}
