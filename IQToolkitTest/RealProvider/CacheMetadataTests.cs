using System;
using System.Collections.Generic;
using System.Linq;
using IQToolkit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IQToolkitTest.RealProvider;

[TestClass]
public class CacheMetadataTests
{
    [TestMethod]
    public void TypedQueryCompilationPreservesCapturedNativeRows()
    {
        var rows = ReadDistinctOptions();
        var cache = new QueryCache(2);
        CollectionAssert.AreEqual(rows.ToArray(), cache.Execute(rows.AsQueryable()).ToArray());
        Assert.AreEqual(1, cache.Count);
    }

    [TestMethod]
    public void ExpressionQueryCompilationPreservesCapturedNativeRows()
    {
        var rows = ReadDistinctOptions();
        var cache = new QueryCache(2);
        var result = cache.Execute(rows.AsQueryable().Expression);
        Assert.IsInstanceOfType<IEnumerable<string>>(result);
        CollectionAssert.AreEqual(rows.ToArray(), ((IEnumerable<string>)result).ToArray());
    }

    [TestMethod]
    public void UntypedQueryExecutesCapturedNativeRows()
    {
        var rows = ReadDistinctOptions();
        var cache = new QueryCache(2);
        IQueryable query = rows.AsQueryable();
        var result = cache.Execute(query);
        Assert.IsInstanceOfType<IEnumerable<string>>(result);
        CollectionAssert.AreEqual(rows.ToArray(), ((IEnumerable<string>)result).ToArray());
        Assert.AreEqual(1, cache.Count);
    }

    [TestMethod]
    public void NonaddingMissReturnsDefaultInsteadOfAnotherNativeOption()
    {
        var rows = ReadDistinctOptions();
        var cache = new MostRecentlyUsedCache<string>(2);
        Assert.IsFalse(cache.Lookup(rows[0], true, out var inserted));
        Assert.AreSame(rows[0], inserted);
        Assert.IsFalse(cache.Lookup(rows[1], false, out var missed));
        Assert.IsNull(missed);
        Assert.AreEqual(1, cache.Count);
    }

    [TestMethod]
    public void HeadHitReturnsTheMatchingNativeOption()
    {
        var rows = ReadDistinctOptions();
        var cache = new MostRecentlyUsedCache<string>(2);
        cache.Lookup(rows[0], true, out _);
        cache.Lookup(rows[1], true, out _);
        Assert.IsTrue(cache.Lookup(rows[1], false, out var found));
        Assert.AreSame(rows[1], found);
    }

    [TestMethod]
    public void AddingHitPromotesItsNativeOptionBeforeEviction()
    {
        var rows = ReadDistinctOptions();
        var cache = new MostRecentlyUsedCache<string>(2);
        cache.Lookup(rows[0], true, out _);
        cache.Lookup(rows[1], true, out _);
        Assert.IsTrue(cache.Lookup(rows[0], true, out var accessed));
        Assert.AreSame(rows[0], accessed);
        Assert.IsFalse(cache.Lookup(rows[2], true, out var inserted));
        Assert.AreSame(rows[2], inserted);
        Assert.IsTrue(cache.Lookup(rows[0], false, out var retained));
        Assert.AreSame(rows[0], retained);
        Assert.IsFalse(cache.Lookup(rows[1], false, out var evicted));
        Assert.IsNull(evicted);
        Assert.AreEqual(2, cache.Count);
    }

    [TestMethod]
    public void NonaddingHitPreservesRecencyOfNativeOptions()
    {
        var rows = ReadDistinctOptions();
        var cache = new MostRecentlyUsedCache<string>(2);
        cache.Lookup(rows[0], true, out _);
        cache.Lookup(rows[1], true, out _);
        Assert.IsTrue(cache.Lookup(rows[0], false, out var accessed));
        Assert.AreSame(rows[0], accessed);
        cache.Lookup(rows[2], true, out _);
        Assert.IsFalse(cache.Lookup(rows[0], false, out var evicted));
        Assert.IsNull(evicted);
        Assert.IsTrue(cache.Lookup(rows[1], false, out var retained));
        Assert.AreSame(rows[1], retained);
    }

    [TestMethod]
    public void ClearReleasesEntriesAndAllowsNativeInsertion()
    {
        var rows = ReadDistinctOptions();
        var cache = new MostRecentlyUsedCache<string>(2);
        cache.Lookup(rows[0], true, out _);
        cache.Clear();
        Assert.AreEqual(0, cache.Count);
        Assert.IsFalse(cache.Lookup(rows[1], true, out var inserted));
        Assert.AreSame(rows[1], inserted);
        Assert.AreEqual(1, cache.Count);
        Assert.IsFalse(cache.Lookup(rows[0], false, out var missed));
        Assert.IsNull(missed);
    }

    [TestMethod]
    public void EqualNativeFunctionNameHitRetainsTheCachedObject()
    {
        var names = SQLiteMetadata.ReadValues("PRAGMA function_list;");
        var duplicate = names.GroupBy(name => name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        Assert.IsNotNull(duplicate, "Native function metadata must supply duplicate names; no padding is allowed.");
        var observed = duplicate.ToArray();
        Assert.AreNotSame(observed[0], observed[1], "Native capture must supply distinct equal string objects.");
        var options = ReadDistinctOptions();
        var cache = new MostRecentlyUsedCache<string>(2);
        cache.Lookup(observed[0], true, out _);
        cache.Lookup(options[0], true, out _);
        Assert.IsTrue(cache.Lookup(observed[1], true, out var found));
        Assert.AreSame(observed[0], found);
        cache.Lookup(options[1], true, out _);
        Assert.IsTrue(cache.Lookup(observed[1], false, out var retained));
        Assert.AreSame(observed[0], retained);
    }

    [TestMethod]
    public void DistinctNativeRootsPreserveVersionThenOptionsResults()
    {
        var versions = SQLiteMetadata.ReadValues("SELECT sqlite_version();");
        var options = ReadDistinctOptions();
        Assert.IsTrue(versions.Count > 0, "Native version metadata is required.");
        var versionQuery = versions.AsQueryable();
        var optionsQuery = options.AsQueryable();
        var cache = new QueryCache(2);
        CollectionAssert.AreEqual(versions.ToArray(), cache.Execute(versionQuery).ToArray());
        CollectionAssert.AreEqual(options.ToArray(), cache.Execute(optionsQuery).ToArray());
        CollectionAssert.AreEqual(versions.ToArray(), cache.Execute(versionQuery).ToArray());
        Assert.AreEqual(2, cache.Count);
    }

    [TestMethod]
    public void DistinctNativeRootsPreserveOptionsThenVersionResults()
    {
        var options = ReadDistinctOptions();
        var versions = SQLiteMetadata.ReadValues("SELECT sqlite_version();");
        Assert.IsTrue(versions.Count > 0, "Native version metadata is required.");
        var optionsQuery = options.AsQueryable();
        var versionQuery = versions.AsQueryable();
        var cache = new QueryCache(2);
        CollectionAssert.AreEqual(options.ToArray(), cache.Execute(optionsQuery).ToArray());
        CollectionAssert.AreEqual(versions.ToArray(), cache.Execute(versionQuery).ToArray());
        CollectionAssert.AreEqual(options.ToArray(), cache.Execute(optionsQuery).ToArray());
        Assert.AreEqual(2, cache.Count);
    }

    [TestMethod]
    public void RepeatedNativeRootReusesItsCompiledEntry()
    {
        var rows = ReadDistinctOptions();
        var query = rows.AsQueryable();
        var cache = new QueryCache(2);
        CollectionAssert.AreEqual(rows.ToArray(), cache.Execute(query).ToArray());
        CollectionAssert.AreEqual(rows.ToArray(), cache.Execute(query).ToArray());
        Assert.IsTrue(cache.Contains(query));
        Assert.AreEqual(1, cache.Count);
    }

    [TestMethod]
    public void CapturedSelectionCanChangeBetweenObservedNativeOptions()
    {
        var rows = ReadDistinctOptions();
        var selected = rows[0];
        var query = rows.AsQueryable().Where(option => option == selected);
        var cache = new QueryCache(2);
        Assert.AreEqual(rows[0], cache.Execute(query).Single());
        selected = rows[1];
        Assert.AreEqual(rows[1], cache.Execute(query).Single());
        Assert.AreEqual(1, cache.Count);
    }

    private static List<string> ReadDistinctOptions()
    {
        var rows = SQLiteMetadata.ReadValues("PRAGMA compile_options;");
        Assert.IsTrue(rows.Count >= 3, "Native metadata must supply enough entries; no padding is allowed.");
        Assert.AreEqual(rows.Count, rows.Distinct(StringComparer.Ordinal).Count(),
            "Native compile options must be distinct for these sequential traces.");
        return rows;
    }
}
