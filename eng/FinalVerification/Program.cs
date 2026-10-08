using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace IQToolkit.FinalVerification;

internal static partial class Program
{
    private const long Limit = 16 * 1024 * 1024;
    private const int MaximumInputs = 6144;
    private const long MaximumRetainedBytes = 640L * 1024 * 1024;
    private static readonly string[] Packages = ["IQToolkit", "IQToolkit.Data", "IQToolkit.Data.Access", "IQToolkit.Data.MySqlClient", "IQToolkit.Data.SQLite", "IQToolkit.Data.SqlClient"];
    private static readonly Dictionary<string, string> Inputs = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> Snapshots = new(StringComparer.Ordinal);
    private static string repository = string.Empty;
    private static string retained = string.Empty;
    private static long bytes;
    private static string activeDescriptor = string.Empty;
    private static bool nullableCompilationQualified;
    private static bool recentCompilationQualified;
    private static readonly Dictionary<(string Path, string Sha256), Input> HistoricalRecentAliases = new();
    private static readonly Input CanonicalDescriptor = new("artifacts/canonical-publication/coverage.3w2as6e8/collection/descriptor.json",
        "66ef50d8b67c6bbf71a6dcb82f0479133ee292c05e0ddef7c3ce9a2b8583c415");
    private static readonly Input NullableDescriptor = new("artifacts/nullable-column/coverage.843ik22m/collection/descriptor.json",
        "378b60b0e4dd4e189583fbd23e64a8b0d62fa59951ae763e92253784288613bf");
    private static readonly Input CanonicalManifest = new("artifacts/canonical-publication/coverage.3w2as6e8/transition.json",
        "48dcf621aab069c222402e84dc948d62dfcad8fb729cfdb2364492967fa12b6f");
    private static readonly Input NullableManifest = new("artifacts/nullable-column/coverage.843ik22m/transition.json",
        "9a9e42a4ebcb84b250d0be826e1b6162d6be7f03efe7ceed063547b4fb60e23f");
    private static bool mathCompilationQualified;
    private static bool commandCompilationQualified;
    private static readonly Dictionary<string, Input> HistoricalTestAliases = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Input> HistoricalCommandAliases = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Input> HistoricalMathAliases = new(StringComparer.Ordinal);
    private sealed record Input(string Path, string Sha256);

    private static int Main(string[] args)
    {
        int exit = 1;
        bool retentionReady = false;
        object outcome;
        try
        {
            Require(args.Length >= 4, "Expected operation, repository, profile and empty retention directory.");
            repository = System.IO.Path.GetFullPath(args[1]); retained = System.IO.Path.GetFullPath(args[3]);
            Require(Directory.Exists(retained) && !Directory.EnumerateFileSystemEntries(retained).Any(), "Retention must exist and be empty.");
            retentionReady = true;
            using var profile = Read(args[2]);
            Require(profile.RootElement.GetProperty("Schema").GetInt32() == 1 && !profile.RootElement.GetProperty("FinalSuccessSupported").GetBoolean(), "Unsupported precursor profile.");
            if (args[0] == "nullable-continuity-control")
            {
                Require(args.Length == 7, "Expected baseline/current descriptors and nullable transition manifest.");
                outcome = NullableColumnContinuity.Validate(repository, args[4], args[5], args[6]);
                exit = 0;
            }
            else if (args[0] == "recent-context-rejection")
            {
                Require(args.Length == 4, "Unexpected recent-context-rejection arguments.");
                ValidateRecentCompilation();
                CaptureHistoricalRecent(System.IO.Path.Combine(repository, "IQToolkitTest/bin/Release/net10.0/IQToolkitTest.dll"),
                    "5412519419aab722196937753ea63c058145f5b030947149bca61d6142f69871");
                throw new InvalidDataException("Authentic wrong-context input unexpectedly qualified.");
            }
            else if (args[0] == "command-continuity-control")
            {
                Require(args.Length == 4, "Unexpected command-continuity-control arguments.");
                ValidateRecentCompilation();
                ValidateMathCompilation(profile.RootElement);
                outcome = ValidateCommandCompilation(profile.RootElement);
                exit = 0;
            }
            else
            {
                ValidateAnchor(profile.RootElement);
                if (args[0] == "preflight")
                {
                    Require(args.Length == 4, "Unexpected preflight arguments.");
                    outcome = new { Status = "verified", Operation = "transition-continuity", ActiveDescriptor = activeDescriptor, FinalReadinessVerified = false };
                    exit = 0;
                }
                else if (args[0] == "cache-control")
                {
                    Require(args.Length == 4, "Unexpected cache-control arguments.");
                    outcome = ValidateCacheSeeds(profile.RootElement);
                    exit = 0;
                }
                else if (args[0] == "independent-package-control")
                {
                    Require(args.Length == 6, "Expected consumer and API producer directories.");
                    outcome = ValidateIndependentPackageProducer(args[4], args[5]);
                    exit = 0;
                }
                else if (args[0] == "native-package-control")
                {
                    Require(args.Length == 6, "Expected consumer and API producer directories.");
                    outcome = ValidateNativePackageConsumer(args[4], args[5]);
                    exit = 0;
                }
                else
                {
                    Require(args[0] == "assess" && args.Length == 12, "Expected API, symbols, consumers, strict and coverage directories.");
                    outcome = Assess(profile.RootElement, args.Skip(4).Take(5).ToArray(), args[9], args[10], args[11]);
                    // Readiness remains rejected until every due obligation has evidence.
                }
            }
        }
        catch (Exception error)
        {
            outcome = new { Status = "rejected", Operation = "evidence-integrity", FailureType = error.GetType().Name, Reason = error is InvalidDataException ? error.Message : "Unable to bind final evidence." };
        }
        if (!retentionReady)
        {
            Console.WriteLine(JsonSerializer.Serialize(outcome));
            return 1;
        }
        try
        {
            var after = Inputs.ToDictionary(pair => pair.Key, pair => Hash(pair.Key), StringComparer.Ordinal);
            Write("inputs-before.json", Inputs); Write("inputs-after.json", after);
            Require(Inputs.All(pair => pair.Value == after[pair.Key]), "Final verification inputs changed.");
            if (exit == 0 && args[0] == "preflight")
            {
                using var selected = new StreamWriter(new FileStream(System.IO.Path.Combine(retained, "active-descriptor.txt"), FileMode.CreateNew));
                selected.WriteLine(activeDescriptor);
            }
            Write("outcome.json", outcome);
        }
        catch (Exception error)
        {
            exit = 1;
            outcome = new { Status = "rejected", Operation = "terminal-integrity", FailureType = error.GetType().Name };
        }
        Console.WriteLine(JsonSerializer.Serialize(outcome));
        return exit;
    }

    private static void Require([DoesNotReturnIf(false)] bool condition, string reason)
    {
        if (!condition) throw new InvalidDataException(reason);
    }

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static string Capture(string path, string? expected = null)
    {
        path = System.IO.Path.GetFullPath(path);
        if (!Inputs.TryGetValue(path, out var hash))
        {
            var file = new FileInfo(path);
            Require(file.Exists && file.LinkTarget is null && file.Length is > 0 and <= Limit && Inputs.Count < MaximumInputs && bytes + file.Length <= MaximumRetainedBytes, "Missing, linked or oversized final input.");
            hash = Hash(path);
            var copy = System.IO.Path.Combine(retained, $"{Inputs.Count:D4}-{file.Name}.snapshot");
            File.Copy(path, copy, overwrite: false);
            Require(Hash(copy) == hash && Hash(path) == hash, "Input changed while retained.");
            Inputs.Add(path, hash); Snapshots.Add(path, copy); bytes += file.Length;
        }
        Require(expected is null || hash.Equals(expected, StringComparison.OrdinalIgnoreCase), "Final input identity differs.");
        return Snapshots[path];
    }

    internal static string CaptureBound(string path, string? expected = null) => Capture(path, expected);

    // Historical transition reads alone may resolve authenticated overwritten inputs.
    internal static string CaptureHistoricalCompilation(string path, string? expected = null) =>
        CaptureHistoricalInput(path, expected, useV1yTest: false);

    private static string CaptureHistoricalInput(string path, string? expected, bool useV1yTest)
    {
        Require(commandCompilationQualified, "Command compilation must qualify before historical input resolution.");
        path = System.IO.Path.GetFullPath(path);
        Input? alias;
        if (useV1yTest && HistoricalTestAliases.TryGetValue(path, out var test)) alias = test;
        else alias = HistoricalCommandAliases.TryGetValue(path, out var command) ? command : null;
        if (alias is null) return CaptureHistoricalCommand(path, expected);
        Require(expected is null || alias.Sha256.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "Historical input identity differs from its declared transition context.");
        return Capture(alias.Path, alias.Sha256);
    }

    // Applied only by older transition validators, after their own aliases resolve.
    internal static string CaptureHistoricalCommand(string path, string? expected = null)
    {
        Require(mathCompilationQualified, "Math compilation must qualify before historical command input resolution.");
        path = System.IO.Path.GetFullPath(path);
        if (!HistoricalMathAliases.TryGetValue(path, out var alias)) return CaptureHistoricalRecent(path, expected);
        Require(expected is null || alias.Sha256.Equals(expected, StringComparison.OrdinalIgnoreCase),
            "Historical math input identity differs from its declared transition context.");
        return Capture(alias.Path, alias.Sha256);
    }

    // Recent aliases require both the logical path and its expected historical identity.
    internal static string CaptureHistoricalRecent(string path, string? expected = null)
    {
        Require(nullableCompilationQualified, "Nullable compilation must qualify before recent input resolution.");
        path = System.IO.Path.GetFullPath(path);
        if (expected is not null && HistoricalRecentAliases.TryGetValue((path, expected.ToUpperInvariant()), out var alias))
            return Capture(alias.Path, alias.Sha256);
        return Capture(path, expected);
    }

    private static string RecentPath(Input input) => System.IO.Path.Combine(repository, input.Path);

    private static void RegisterRecentAliases(JsonElement manifest, int expectedCount)
    {
        var aliases = manifest.GetProperty("BaselineBindings").EnumerateObject().ToArray();
        Require(aliases.Length == expectedCount, "Qualified recent alias count differs.");
        foreach (var property in aliases)
        {
            var binding = ParseInput(property.Value);
            Require(System.IO.Path.IsPathFullyQualified(property.Name) && property.Name == System.IO.Path.GetFullPath(property.Name),
                "Qualified recent alias logical path is not normalized.");
            Capture(binding.Path, binding.Sha256);
            Require(HistoricalRecentAliases.TryAdd((property.Name, binding.Sha256.ToUpperInvariant()), binding),
                "Qualified recent alias context is duplicate.");
        }
    }

    private static void ValidateRecentCompilation()
    {
        Require(!nullableCompilationQualified && !recentCompilationQualified && HistoricalRecentAliases.Count == 0,
            "Recent compilation context was already initialized.");
        foreach (var input in new[] { CanonicalDescriptor, NullableDescriptor, CanonicalManifest, NullableManifest })
            Capture(RecentPath(input), input.Sha256);
        var nullable = JsonSerializer.SerializeToElement(NullableColumnContinuity.Validate(repository, RecentPath(CanonicalDescriptor),
            RecentPath(NullableDescriptor), RecentPath(NullableManifest)));
        Require(nullable.GetProperty("Status").GetString() == "verified" && nullable.GetProperty("NativeProducerAuthenticated").GetBoolean() &&
            nullable.GetProperty("CurrentComparisonVerified").GetBoolean(), "Recent nullable comparison did not qualify.");
        using var nullableManifest = Read(RecentPath(NullableManifest), NullableManifest.Sha256);
        nullableCompilationQualified = true;
        RegisterRecentAliases(nullableManifest.RootElement, 5);
        var baseline = new Input("artifacts/coverage/runs/run.TFv2whz3/descriptor.json",
            "9bc0eb52ce5d50ba9cb9e89744851ed4e75c4d7aa1e589fc2772f6a6b8567d35");
        Capture(RecentPath(baseline), baseline.Sha256);
        var canonical = JsonSerializer.SerializeToElement(CanonicalPublicationContinuity.Validate(repository, RecentPath(baseline),
            RecentPath(CanonicalDescriptor), RecentPath(CanonicalManifest)));
        Require(canonical.GetProperty("Status").GetString() == "verified" && canonical.GetProperty("NativeProducerAuthenticated").GetBoolean() &&
            canonical.GetProperty("CurrentComparisonVerified").GetBoolean(), "Recent canonical comparison did not qualify.");
        foreach (var comparison in new[] { nullable, canonical })
            foreach (var flag in new[] { "CurrentFinalAccepted", "FullGoalComplete", "CoverageTargetsVerified", "IndividualBranchArmsVerified" })
                Require(!comparison.GetProperty(flag).GetBoolean(), "Recent comparison broad qualification differs.");
        using var canonicalManifest = Read(RecentPath(CanonicalManifest), CanonicalManifest.Sha256);
        RegisterRecentAliases(canonicalManifest.RootElement, 6);
        Require(HistoricalRecentAliases.Count == 11 && HistoricalRecentAliases.Keys.Select(key => key.Path).Distinct(StringComparer.Ordinal).Count() == 9,
            "Qualified recent alias identities or logical paths differ.");
        recentCompilationQualified = true;
    }

    private static JsonDocument Read(string path, string? expected = null)
    {
        var document = JsonDocument.Parse(File.ReadAllBytes(Capture(path, expected)));
        DuplicateCheck(document.RootElement);
        return document;
    }

    private static void DuplicateCheck(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var properties = element.EnumerateObject().ToArray();
            Require(properties.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == properties.Length, "Duplicate JSON property.");
            foreach (var property in properties) DuplicateCheck(property.Value);
        }
        else if (element.ValueKind == JsonValueKind.Array) foreach (var value in element.EnumerateArray()) DuplicateCheck(value);
    }

    private static Input ParseInput(JsonElement element) => element.Deserialize<Input>() ?? throw new InvalidDataException("Missing input.");
    private static string PathInProfile(JsonElement profile, string name) => System.IO.Path.GetFullPath(System.IO.Path.Combine(repository, profile.GetProperty(name).GetProperty("Path").GetString()!));
    private static string ShaInProfile(JsonElement profile, string name) => profile.GetProperty(name).GetProperty("Sha256").GetString()!;

    private static void ValidateAnchor(JsonElement profile)
    {
        ValidateRecentCompilation();
        ValidateMathCompilation(profile);
        ValidateCommandCompilation(profile);
        ValidateCompilation(profile);
        var originalPath = PathInProfile(profile, "OriginalDescriptor"); var anchorPath = PathInProfile(profile, "AnchorDescriptor");
        using var original = Read(originalPath, ShaInProfile(profile, "OriginalDescriptor"));
        using var anchor = Read(anchorPath, ShaInProfile(profile, "AnchorDescriptor"));
        using var transition = Read(PathInProfile(profile, "TransitionReceipt"), ShaInProfile(profile, "TransitionReceipt"));
        var receipt = transition.RootElement;
        Require(receipt.GetProperty("Status").GetString() == "verified" && receipt.GetProperty("SourceFiles").GetInt32() == 97 && receipt.GetProperty("ComparedLeaves").GetInt32() == 16716 &&
            receipt.GetProperty("CoverageTargetsVerified").GetBoolean() == false && receipt.GetProperty("ChangedLine").GetInt32() == 781, "Unsupported transition qualification.");
        foreach (var pair in new[] { (Name: "Baseline", Path: originalPath, Sha: ShaInProfile(profile, "OriginalDescriptor")), (Name: "Current", Path: anchorPath, Sha: ShaInProfile(profile, "AnchorDescriptor")) })
        {
            var input = ParseInput(receipt.GetProperty(pair.Name));
            Require(input.Path == pair.Path && input.Sha256.Equals(pair.Sha, StringComparison.OrdinalIgnoreCase), "Transition does not bind the selected descriptors.");
        }
        Require(original.RootElement.GetProperty("RepositoryRoot").GetString() == repository && anchor.RootElement.GetProperty("RepositoryRoot").GetString() == repository, "Descriptor repository differs.");
        var aliases = receipt.GetProperty("BaselineBindings"); Require(aliases.EnumerateObject().Count() == 11, "Original alias scope differs.");
        var expected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var module in original.RootElement.GetProperty("Modules").EnumerateArray().Append(original.RootElement.GetProperty("TestAssembly")))
            foreach (var kind in new[] { "Dll", "Pdb" })
            {
                var logical = ParseInput(module.GetProperty(kind)); var physical = ParseInput(aliases.GetProperty(logical.Path));
                expected.Add(logical.Path);
                var target = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(originalPath)!, "environment", "shadow", System.IO.Path.GetFileName(logical.Path));
                Require(physical.Path == target && physical.Sha256.Equals(logical.Sha256, StringComparison.OrdinalIgnoreCase), "Retained original compilation binding differs.");
                Capture(physical.Path, physical.Sha256);
            }
        var formatter = System.IO.Path.Combine(repository, "IQToolkit.Data/Common/Language/SqlFormatter.cs"); expected.Add(formatter);
        var oldFormatter = ParseInput(aliases.GetProperty(formatter));
        Require(oldFormatter.Sha256.Equals("cafa877e77e6eba75722dd6cdc0f77d0af819d623db8ae69e24f33359e222da7", StringComparison.OrdinalIgnoreCase) &&
            oldFormatter.Path == System.IO.Path.Combine(repository, "artifacts/sql-string-literal/acquire.fizx6eza/prospective-source/IQToolkit.Data/Common/Language/SqlFormatter.cs"), "Original formatter binding differs.");
        Capture(oldFormatter.Path, oldFormatter.Sha256);
        Require(expected.SetEquals(aliases.EnumerateObject().Select(property => property.Name)), "Unreviewed original binding.");
        using var executed = Read(PathInProfile(profile, "TransitionInputsBefore"), ShaInProfile(profile, "TransitionInputsBefore"));
        using var after = Read(PathInProfile(profile, "TransitionInputsAfter"), ShaInProfile(profile, "TransitionInputsAfter"));
        foreach (var property in executed.RootElement.EnumerateObject())
        {
            var hash = property.Value.GetProperty("Sha256").GetString()!;
            Require(after.RootElement.GetProperty(property.Name).GetString() == hash, "Executed transition input preservation differs.");
            CaptureHistoricalInput(property.Name, hash, useV1yTest: true);
            Capture(property.Value.GetProperty("Snapshot").GetString()!, hash);
        }
        Require(executed.RootElement.EnumerateObject().Count() == 198 && after.RootElement.EnumerateObject().Count() == 198, "Executed transition input scope differs.");
        Require(recentCompilationQualified, "Complete recent continuity must qualify before current selection.");
        activeDescriptor = RecentPath(NullableDescriptor);
    }

    private static void ValidateCompilation(JsonElement profile)
    {
        var candidate = PathInProfile(profile, "CurrentDescriptor");
        Capture(candidate, ShaInProfile(profile, "CurrentDescriptor"));
        var manifest = PathInProfile(profile, "CompilationManifest");
        Capture(manifest, ShaInProfile(profile, "CompilationManifest"));
        using var receipt = Read(PathInProfile(profile, "CompilationReceipt"), ShaInProfile(profile, "CompilationReceipt"));
        var actual = JsonSerializer.SerializeToElement(CompilationContinuity.Validate(PathInProfile(profile, "AnchorDescriptor"), candidate, manifest));
        Require(JsonElement.DeepEquals(actual, receipt.RootElement), "Requalified compilation differs from the executed receipt.");
        var aliases = receipt.RootElement.GetProperty("BaselineBindings");
        Require(aliases.EnumerateObject().Count() == 2, "Expected exactly two historical Test aliases.");
        foreach (var property in aliases.EnumerateObject())
        {
            var alias = ParseInput(property.Value);
            Capture(alias.Path, alias.Sha256);
            HistoricalTestAliases.Add(property.Name, alias);
        }
        using var before = Read(PathInProfile(profile, "CompilationInputsBefore"), ShaInProfile(profile, "CompilationInputsBefore"));
        using var after = Read(PathInProfile(profile, "CompilationInputsAfter"), ShaInProfile(profile, "CompilationInputsAfter"));
        Require(before.RootElement.EnumerateObject().Count() == 188 && after.RootElement.EnumerateObject().Count() == 188,
            "Executed compilation transition scope differs.");
        foreach (var property in before.RootElement.EnumerateObject())
        {
            var hash = property.Value.GetProperty("Sha256").GetString()!;
            Require(after.RootElement.GetProperty(property.Name).GetString() == hash, "Executed compilation transition preservation differs.");
            CaptureHistoricalCompilation(property.Name, hash);
            Capture(property.Value.GetProperty("Snapshot").GetString()!, hash);
        }
    }

    // Qualifies the newest transition and its complete executed capture before aliases become available.
    private static object ValidateMathCompilation(JsonElement profile)
    {
        var pins = new[]
        {
            (Name: "MathDescriptor", Path: "artifacts/sqlite-math-culture/coverage.nx5khe0v/collection/descriptor.json", Sha256: "596f7ea743ea586d779d3f22ac43d82d98d4c8e73978eac5e67fd20c00c849b3"),
            (Name: "MathManifest", Path: "artifacts/sqlite-math-culture/coverage.nx5khe0v/transition.json", Sha256: "ec07776a112c5248299c6b2363735355c07c1e63ae24ab6b5b534b19ead46293"),
            (Name: "MathReceipt", Path: "artifacts/sqlite-math-culture/qualification.1p9l6zbh/retention/transition.json", Sha256: "ded76477fdc9f00ca49a4f4a36c2683cb081df6f2bccdc6433dfa41d88a62f4a"),
            (Name: "MathInputsBefore", Path: "artifacts/sqlite-math-culture/qualification.1p9l6zbh/retention/inputs-before.json", Sha256: "529766e910c183fff8498e9cffb74e796901a2c34434235173704fb0618ec9c0"),
            (Name: "MathInputsAfter", Path: "artifacts/sqlite-math-culture/qualification.1p9l6zbh/retention/inputs-after.json", Sha256: "395621b885c280ec6aef90ac9a7a9a0ee0b25298dca4d1684d4fc1af49f31208"),
            (Name: "MathProducerSource", Path: "artifacts/sqlite-math-culture/coverage-helper/Program.cs", Sha256: "9b7dccb3e75ac2b19e260fbb6158930330d401323149af676ca0b0664804f407")
        };
        foreach (var pin in pins)
        {
            var input = profile.GetProperty(pin.Name);
            Require(input.ValueKind == JsonValueKind.Object && input.EnumerateObject().Select(value => value.Name)
                .Order(StringComparer.Ordinal).SequenceEqual(new[] { "Path", "Sha256" }) &&
                PathInProfile(profile, pin.Name) == System.IO.Path.Combine(repository, pin.Path) &&
                ShaInProfile(profile, pin.Name).Equals(pin.Sha256, StringComparison.OrdinalIgnoreCase),
                "Math profile pin differs from its authentic executed origin.");
            Capture(PathInProfile(profile, pin.Name), pin.Sha256);
        }
        var baseline = PathInProfile(profile, "CommandDescriptor");
        Capture(baseline, ShaInProfile(profile, "CommandDescriptor"));
        var current = PathInProfile(profile, "MathDescriptor");
        using var receipt = Read(PathInProfile(profile, "MathReceipt"), ShaInProfile(profile, "MathReceipt"));
        var actual = JsonSerializer.SerializeToElement(MathIdentifierContinuity.Validate(baseline, current, PathInProfile(profile, "MathManifest")));
        Require(JsonElement.DeepEquals(actual, receipt.RootElement), "Requalified math compilation differs from the executed receipt.");
        using var before = Read(PathInProfile(profile, "MathInputsBefore"), ShaInProfile(profile, "MathInputsBefore"));
        using var after = Read(PathInProfile(profile, "MathInputsAfter"), ShaInProfile(profile, "MathInputsAfter"));
        var inputs = before.RootElement.EnumerateObject().ToArray();
        Require(inputs.Length == 1181 && after.RootElement.EnumerateObject().Count() == 1181,
            "Executed math compilation input scope differs.");
        var origin = System.IO.Path.GetDirectoryName(PathInProfile(profile, "MathInputsBefore"));
        foreach (var property in inputs)
        {
            var hash = property.Value.GetProperty("Sha256").GetString()!;
            Require(after.RootElement.TryGetProperty(property.Name, out var final) && final.GetString() == hash,
                "Executed math compilation preservation differs.");
            var snapshot = property.Value.GetProperty("Snapshot").GetString()!;
            Require(System.IO.Path.IsPathFullyQualified(property.Name) && System.IO.Path.GetFullPath(property.Name) == property.Name &&
                System.IO.Path.IsPathFullyQualified(snapshot) && System.IO.Path.GetFullPath(snapshot) == snapshot &&
                System.IO.Path.GetDirectoryName(snapshot) == origin, "Executed math input origin differs.");
            CaptureHistoricalRecent(property.Name, hash);
            Capture(snapshot, hash);
        }
        var aliases = receipt.RootElement.GetProperty("BaselineBindings");
        var expectedAliases = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [System.IO.Path.Combine(repository, "IQToolkit.Data.SQLite", "SQLiteFormatter.cs")] = "artifacts/sqlite-math-culture/red.b18i41v7/reviewed-inputs/0024-SQLiteFormatter.cs.snapshot",
            [System.IO.Path.Combine(repository, "IQToolkitTest", "bin", "Release", "net10.0", "IQToolkit.Data.SQLite.dll")] = "artifacts/sqlite-math-culture/green.1_odmlyu/previous-binaries/IQToolkitTest/bin/Release/net10.0/IQToolkit.Data.SQLite.dll",
            [System.IO.Path.Combine(repository, "IQToolkitTest", "bin", "Release", "net10.0", "IQToolkit.Data.SQLite.pdb")] = "artifacts/sqlite-math-culture/green.1_odmlyu/previous-binaries/IQToolkitTest/bin/Release/net10.0/IQToolkit.Data.SQLite.pdb",
            [System.IO.Path.Combine(repository, "IQToolkitTest", "bin", "Release", "net10.0", "IQToolkitTest.dll")] = "artifacts/sqlite-math-culture/acquire.meb_1d_2/previous-binaries/IQToolkitTest/bin/Release/net10.0/IQToolkitTest.dll",
            [System.IO.Path.Combine(repository, "IQToolkitTest", "bin", "Release", "net10.0", "IQToolkitTest.pdb")] = "artifacts/sqlite-math-culture/acquire.meb_1d_2/previous-binaries/IQToolkitTest/bin/Release/net10.0/IQToolkitTest.pdb"
        };
        Require(expectedAliases.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(aliases.EnumerateObject().Select(property => property.Name)),
            "Historical math alias scope differs from the exact SQLite/source/Test transition.");
        foreach (var property in aliases.EnumerateObject())
        {
            var alias = ParseInput(property.Value);
            Require(alias.Path == System.IO.Path.Combine(repository, expectedAliases[property.Name]), "Historical math alias origin differs.");
            Capture(alias.Path, alias.Sha256);
            HistoricalMathAliases.Add(property.Name, alias);
        }
        mathCompilationQualified = true;
        return new
        {
            Status = "verified", Operation = "math-compilation-continuity", SourceFiles = receipt.RootElement.GetProperty("SourceFiles").GetInt32(),
            ComparedLeaves = receipt.RootElement.GetProperty("ComparedLeaves").GetInt32(), RetainedInputs = inputs.Length,
            FinalReadinessVerified = false, DurableFinalProfileAccepted = false, FullGoalComplete = false
        };
    }

    // Replays the command transition after the newest math transition has qualified.
    private static object ValidateCommandCompilation(JsonElement profile)
    {
        var baseline = PathInProfile(profile, "CurrentDescriptor");
        Capture(baseline, ShaInProfile(profile, "CurrentDescriptor"));
        var current = PathInProfile(profile, "CommandDescriptor");
        Capture(current, ShaInProfile(profile, "CommandDescriptor"));
        var manifest = PathInProfile(profile, "CommandManifest");
        Capture(manifest, ShaInProfile(profile, "CommandManifest"));
        using var receipt = Read(PathInProfile(profile, "CommandReceipt"), ShaInProfile(profile, "CommandReceipt"));
        var actual = JsonSerializer.SerializeToElement(CommandInitializationContinuity.Validate(baseline, current, manifest));
        Require(JsonElement.DeepEquals(actual, receipt.RootElement), "Requalified command compilation differs from the executed receipt.");
        using var before = Read(PathInProfile(profile, "CommandInputsBefore"), ShaInProfile(profile, "CommandInputsBefore"));
        using var after = Read(PathInProfile(profile, "CommandInputsAfter"), ShaInProfile(profile, "CommandInputsAfter"));
        var inputs = before.RootElement.EnumerateObject().ToArray();
        Require(inputs.Length == 587 && after.RootElement.EnumerateObject().Count() == 587,
            "Executed command compilation input scope differs.");
        var origin = System.IO.Path.GetDirectoryName(PathInProfile(profile, "CommandInputsBefore"));
        foreach (var property in inputs)
        {
            var hash = property.Value.GetProperty("Sha256").GetString()!;
            Require(after.RootElement.TryGetProperty(property.Name, out var final) && final.GetString() == hash,
                "Executed command compilation preservation differs.");
            var snapshot = property.Value.GetProperty("Snapshot").GetString()!;
            Require(System.IO.Path.IsPathFullyQualified(property.Name) && System.IO.Path.GetFullPath(property.Name) == property.Name &&
                System.IO.Path.IsPathFullyQualified(snapshot) && System.IO.Path.GetFullPath(snapshot) == snapshot &&
                System.IO.Path.GetDirectoryName(snapshot) == origin, "Executed command input origin differs.");
            CaptureHistoricalCommand(property.Name, hash);
            Capture(snapshot, hash);
        }
        var aliases = receipt.RootElement.GetProperty("BaselineBindings");
        var expectedAliases = new HashSet<string>(StringComparer.Ordinal)
        {
            System.IO.Path.Combine(repository, "IQToolkit.Data", "DbEntityProvider.cs"),
            System.IO.Path.Combine(repository, "IQToolkitTest", "bin", "Release", "net10.0", "IQToolkit.Data.dll"),
            System.IO.Path.Combine(repository, "IQToolkitTest", "bin", "Release", "net10.0", "IQToolkit.Data.pdb"),
            System.IO.Path.Combine(repository, "IQToolkitTest", "bin", "Release", "net10.0", "IQToolkitTest.dll"),
            System.IO.Path.Combine(repository, "IQToolkitTest", "bin", "Release", "net10.0", "IQToolkitTest.pdb")
        };
        Require(expectedAliases.SetEquals(aliases.EnumerateObject().Select(property => property.Name)),
            "Historical command alias scope differs from the exact Data/source/Test transition.");
        foreach (var property in aliases.EnumerateObject())
        {
            var alias = ParseInput(property.Value);
            Capture(alias.Path, alias.Sha256);
            HistoricalCommandAliases.Add(property.Name, alias);
        }
        commandCompilationQualified = true;
        return new
        {
            Status = "verified", Operation = "command-compilation-continuity",
            SourceFiles = receipt.RootElement.GetProperty("SourceFiles").GetInt32(),
            ComparedLeaves = receipt.RootElement.GetProperty("ComparedLeaves").GetInt32(),
            ComparedInheritedLeaves = receipt.RootElement.GetProperty("ComparedInheritedLeaves").GetInt32(),
            AddedLeaves = receipt.RootElement.GetProperty("AddedLeafCount").GetInt32(), RetainedInputs = inputs.Length,
            FinalReadinessVerified = false, DurableFinalProfileAccepted = false, FullGoalComplete = false
        };
    }

    private static object Assess(JsonElement profile, string[] paths, string baselineInventory, string currentInventory, string baselineCommitObject)
    {
        var kinds = new[] { "api-compatibility", "package-symbols", "ci-consumers", "verification", "coverage" };
        for (var i = 0; i < paths.Length; i++)
        {
            Require(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(paths[i])) == System.IO.Path.Combine(repository, "artifacts", kinds[i], "runs") &&
                File.ReadAllText(Capture(System.IO.Path.Combine(paths[i], "status.txt"))).Trim() == "passed", "Expected passing producer in its owned evidence area.");
        }
        var feed = System.IO.Path.Combine(paths[0], "packages");
        Require(File.ReadAllText(Capture(System.IO.Path.Combine(paths[1], "package-directory.txt"))).Trim() == feed &&
            File.ReadAllText(Capture(System.IO.Path.Combine(paths[2], "package-directory.txt"))).Trim() == feed, "Symbol/consumer checks did not use the same fresh feed.");
        Require(Directory.GetFiles(feed, "*.nupkg").Length == 6 && Directory.GetFiles(feed, "*.snupkg").Length == 6, "Fresh feed scope differs.");
        foreach (var package in Packages)
            foreach (var kind in new[] { (Suffix: "nupkg", Payload: "dll"), (Suffix: "snupkg", Payload: "pdb") })
            {
                var filename = $"{package}.1.1.2-preview.1.{kind.Suffix}";
                var archivePath = System.IO.Path.Combine(feed, filename);
                var capturedArchive = Capture(archivePath);
                var archiveHash = Hash(capturedArchive);
                Capture(System.IO.Path.Combine(paths[1], "tested-packages", filename), archiveHash);
                if (kind.Suffix == "nupkg")
                {
                    Capture(System.IO.Path.Combine(paths[0], "tested-packages", filename), archiveHash);
                    Capture(System.IO.Path.Combine(paths[2], "tested-packages", filename), archiveHash);
                }
                using var archive = ZipFile.OpenRead(capturedArchive);
                var entry = archive.GetEntry($"lib/net10.0/{package}.{kind.Payload}") ?? throw new InvalidDataException("Missing package payload.");
                Require(entry.Length is > 0 and <= Limit, "Oversized package payload.");
                using var input = entry.Open();
                Require(Convert.ToHexString(SHA256.HashData(input)) == Hash(Capture(System.IO.Path.Combine(repository, package, "bin/Release/net10.0", $"{package}.{kind.Payload}"))), "Tested package differs from final ordinary output.");
            }
        Require(File.ReadAllBytes(Capture(System.IO.Path.Combine(paths[3], "snapshot.txt"))).AsSpan().SequenceEqual(File.ReadAllBytes(Capture(System.IO.Path.Combine(paths[3], "snapshot-after.txt")))), "Strict source snapshot changed.");
        using var descriptor = Read(System.IO.Path.Combine(paths[4], "descriptor.json"));
        var strictTrx = ParseInput(descriptor.RootElement.GetProperty("StrictTrx"));
        Require(strictTrx.Path == System.IO.Path.Combine(paths[3], "tests.trx"), "Coverage did not use this strict run.");
        Capture(strictTrx.Path, strictTrx.Sha256);
        var reports = descriptor.RootElement.GetProperty("Reports").EnumerateArray().Select(ParseInput).ToArray();
        Require(reports.Length > 0 && reports.Select(report => report.Sha256.ToUpperInvariant()).Distinct().Count() == 1, "Coverage report copies differ.");
        foreach (var report in reports) Capture(report.Path, report.Sha256);
        using var stream = File.OpenRead(Capture(reports[0].Path, reports[0].Sha256));
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = Limit });
        var root = XDocument.Load(reader).Root ?? throw new InvalidDataException("Missing native coverage.");
        Require(root.Name == "coverage", "Unexpected coverage root.");
        int Number(string name) => int.Parse(root.Attribute(name)?.Value ?? string.Empty, NumberStyles.None, CultureInfo.InvariantCulture);
        int lines = Number("lines-valid"), covered = Number("lines-covered"), branches = Number("branches-valid"), coveredBranches = Number("branches-covered");
        Require(lines > 0 && branches > 0 && covered <= lines && coveredBranches <= branches, "Empty or inconsistent coverage.");
        var measured = root.Element("packages")?.Elements("package").Select(package => (string?)package.Attribute("name") ?? string.Empty).Order(StringComparer.Ordinal).ToArray() ?? [];
        var missing = Packages.Except(measured, StringComparer.Ordinal).ToArray();
        var gaps = new List<object>();
        if (missing.Length > 0) gaps.Add(new { Requirement = "full-project-coverage", State = "unqualified", MissingLibraries = missing });
        if ((decimal)covered / lines < .90m) gaps.Add(new { Requirement = "default-final-line-target", State = "insufficient-measured-scope", RequiredPercent = 90 });
        if (!ClassifyTouched(root, baselineInventory, currentInventory, baselineCommitObject)) gaps.Add(new { Requirement = "touched-file-layer-targets", State = "classified-incomplete", Receipt = "touched-classification.json" });
        if (!ClassifyFacts(descriptor.RootElement, paths[2], paths[0])) gaps.Add(new { Requirement = "mandatory-fact-execution-bindings", State = "classified-incomplete", Receipt = "fact-classification.json" });
        var cacheSeeds = ValidateCacheSeeds(profile);
        gaps.Add(new { Requirement = "optimized-cache-seed-qualification", State = "unqualified", DiagnosticProfile = "verified-diagnostic-profile", DefaultOptimizedArtifactsUnqualified = true, Task = "V0.6" });
        return new { Status = "rejected", Operation = "final-readiness", Scope = "V0.6-precursor", EvidenceIntegrity = "verified", ProducerDirectories = paths,
            MeasuredLibraries = measured, CoveredLines = covered, Lines = lines, CoveredBranches = coveredBranches, Branches = branches,
            CacheSeedEvidence = cacheSeeds, RequiredEvidence = gaps, FinalReadinessVerified = false, FullRoadmapComplete = false };
    }

    private static void Write(string name, object value)
    {
        using var stream = new FileStream(System.IO.Path.Combine(retained, name), FileMode.CreateNew);
        JsonSerializer.Serialize(stream, value);
    }
}
