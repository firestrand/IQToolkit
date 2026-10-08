using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace IQToolkit.TestResultsVerifier;

internal static class Program
{
    private const long MaximumBytes = 16 * 1024 * 1024;
    private static readonly XNamespace Trx = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    private static int Main(string[] args)
    {
        try
        {
            Require(args.Length == 2, "Expected a TRX report and translation output.");
            var trx = Path.GetFullPath(args[0]);
            var translation = Path.GetFullPath(args[1]);
            ValidateSize(trx);
            ValidateSize(translation);
            var trxHash = Hash(trx);
            var translationHash = Hash(translation);
            var tests = ValidateTests(trx);
            var translations = ValidateTranslations(translation);
            Require(trxHash == Hash(trx) && translationHash == Hash(translation), "Input reports changed during validation.");
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Status = "verified", Tests = tests, Translations = translations,
                TrxSha256 = trxHash, TranslationSha256 = translationHash
            }));
            return 0;
        }
        catch (InvalidDataException error)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { Status = "rejected", Reason = error.Message }));
            return 1;
        }
        catch (Exception error)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { Status = "rejected", Reason = "Unable to validate input reports.", FailureType = error.GetType().Name }));
            return 1;
        }
    }

    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidDataException(reason);
    }

    private static void ValidateSize(string path) =>
        Require(new FileInfo(path).Length is > 0 and <= MaximumBytes, "Missing, empty or oversized report.");

    private static string Hash(string path)
    {
        using var input = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(input));
    }

    private static int Number(string? value)
    {
        Require(int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number >= 0, "Invalid execution count.");
        return number;
    }

    private static object ValidateTests(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
            MaxCharactersInDocument = MaximumBytes
        });
        var root = XDocument.Load(reader).Root ?? throw new InvalidDataException("Missing test report root.");
        Require(root.Name == Trx + "TestRun", "Unexpected test report format.");
        var results = root.Element(Trx + "Results")?.Elements(Trx + "UnitTestResult").ToArray() ?? [];
        Require(results.Length > 0, "MSTest discovered no test results.");
        var definitions = root.Element(Trx + "TestDefinitions")?.Elements(Trx + "UnitTest").ToArray() ?? [];
        var byId = new Dictionary<string, XElement>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            var id = (string?)definition.Attribute("id");
            Require(!string.IsNullOrEmpty(id) && byId.TryAdd(id, definition), "Missing or duplicate test definition identity.");
        }
        var executionIds = new HashSet<string>(StringComparer.Ordinal);
        var testIds = new HashSet<string>(StringComparer.Ordinal);
        var passed = 0;
        var skipped = 0;
        var sqlitePassed = 0;
        var nativePassed = 0;
        foreach (var result in results)
        {
            var id = (string?)result.Attribute("testId");
            var execution = (string?)result.Attribute("executionId");
            Require(id is not null && testIds.Add(id) && byId.ContainsKey(id), "Missing, duplicate or unlinked test result identity.");
            Require(!string.IsNullOrEmpty(execution) && executionIds.Add(execution), "Missing or duplicate test execution identity.");
            var definition = byId[id!];
            Require((string?)definition.Element(Trx + "Execution")?.Attribute("id") == execution, "Test execution identity mismatch.");
            var method = definition.Element(Trx + "TestMethod");
            var type = (string?)method?.Attribute("className");
            var assembly = ((string?)method?.Attribute("codeBase"))?.Replace('\\', '/');
            Require(type?.StartsWith("IQToolkitTest.", StringComparison.Ordinal) == true &&
                    Path.GetFileName(assembly) == "IQToolkitTest.dll" &&
                    (string?)method?.Attribute("adapterTypeName") == "executor://mstestadapter/v4", "Unexpected test assembly, namespace or adapter.");
            var outcome = (string?)result.Attribute("outcome");
            Require(outcome is "Passed" or "NotExecuted", "MSTest contains a failure or unsupported outcome.");
            if (outcome == "NotExecuted")
            {
                skipped++;
                Require(definition.Element(Trx + "TestCategory")?.Elements(Trx + "TestCategoryItem")
                    .Any(category => (string?)category.Attribute("TestCategory") == "SqlServerIntegration") == true,
                    "An offline test was unexpectedly skipped.");
            }
            else
            {
                passed++;
                if (type == "IQToolkitTest.SQLiteTests") sqlitePassed++;
                if (type!.StartsWith("IQToolkitTest.RealProvider.", StringComparison.Ordinal)) nativePassed++;
            }
        }
        Require(passed > 0 && sqlitePassed > 0 && nativePassed > 0, "Expected offline suites did not execute.");
        var summary = root.Element(Trx + "ResultSummary");
        Require((string?)summary?.Attribute("outcome") == "Completed", "MSTest run did not complete.");
        var counters = summary?.Element(Trx + "Counters");
        Require(counters is not null && Number((string?)counters.Attribute("total")) == results.Length &&
                Number((string?)counters.Attribute("passed")) == passed &&
                Number((string?)counters.Attribute("executed")) == passed &&
                Number((string?)counters.Attribute("failed")) == 0 &&
                Number((string?)counters.Attribute("error")) == 0, "MSTest summary disagrees with observed executions.");
        foreach (var name in new[] { "timeout", "aborted", "inconclusive", "passedButRunAborted", "notRunnable", "disconnected", "warning", "completed", "inProgress", "pending" })
            Require(Number((string?)counters!.Attribute(name)) == 0, "MSTest has an incomplete or unsupported run outcome.");
        foreach (var info in summary!.Element(Trx + "RunInfos")?.Elements(Trx + "RunInfo") ?? [])
            Require((string?)info.Attribute("outcome") == "Passed", "MSTest reported a run-level problem.");
        var reportedSkipped = Number((string?)counters!.Attribute("notExecuted"));
        Require(reportedSkipped == 0 || reportedSkipped == skipped, "MSTest skip counter has an unsupported discrepancy.");
        // The installed adapter reports notExecuted=0 for actual NotExecuted
        // results. Individual results remain authoritative for skipped counts.
        return new { Discovered = results.Length, Passed = passed, Skipped = skipped, SQLitePassed = sqlitePassed,
            NativePassed = nativePassed, ReportedNotExecuted = reportedSkipped };
    }

    private static int ValidateTranslations(string path)
    {
        var text = File.ReadAllText(path);
        var headers = Regex.Matches(text, "^Running tests: NorthwindTranslationTests\\r?$", RegexOptions.Multiline);
        var summaries = Regex.Matches(text, "^SUMMARY: NorthwindTranslationTests\\r?$", RegexOptions.Multiline);
        Require(headers.Count == 1 && summaries.Count == 1,
                "Unexpected translation suite identity.");
        var cases = Regex.Matches(text, "^Test ([0-9]+): (Test\\S+) - (PASSED|FAILED)\\r?$", RegexOptions.Multiline);
        Require(cases.Count > 0, "Translation suite executed no tests.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < cases.Count; index++)
        {
            var test = cases[index];
            Require(Number(test.Groups[1].Value) == index + 1 && names.Add(test.Groups[2].Value) &&
                    test.Groups[3].Value == "PASSED" && test.Index > headers[0].Index && test.Index < summaries[0].Index,
                    "Translation execution is duplicated, incomplete or failed.");
        }
        var totals = Regex.Matches(text, "^Total tests run: ([0-9]+)\\r?$", RegexOptions.Multiline);
        var completions = Regex.Matches(text, "^ALL tests passed!\\r?$", RegexOptions.Multiline);
        Require(totals.Count == 1 && Number(totals[0].Groups[1].Value) == cases.Count &&
                totals[0].Index > summaries[0].Index && completions.Count == 1 &&
                completions[0].Index > totals[0].Index &&
                string.IsNullOrWhiteSpace(text[(completions[0].Index + completions[0].Length)..]) &&
                !text.Contains("Total tests failed:", StringComparison.Ordinal), "Translation summary disagrees with observed executions.");
        return cases.Count;
    }
}
