using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IQToolkit;
using IQToolkit.Data.Common;
using IQToolkit.Data.Mapping;
using IQToolkit.Data.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NativeContext = IQToolkitTest.RealProvider.NativeProviderCacheTests.CompileOptionsContext;
using NativeEntry = IQToolkitTest.RealProvider.NativeProviderCacheTests.NativeMetadataEntry;

namespace IQToolkitTest.RealProvider;

[TestClass]
public class CanonicalPublicationTests
{
    private const string NativeRequest = "SELECT compile_options FROM pragma_compile_options;";
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    [TestMethod]
    public void MappingCreationReturnsThePublishedNativeEntity()
    {
        var expected = AcquireNativeRows();
        using var connection = new SqliteConnection("Data Source=:memory:");
        using var pause = new CreationPause();
        var mapping = new PausedMapping(pause);
        var provider = new SQLiteQueryProvider(connection, mapping, QueryPolicy.Default);
        var calls = Compete(() => mapping.GetEntity(typeof(NativeEntry), nameof(NativeContext.Entries)), pause);
        Assert.AreEqual(ConnectionState.Closed, connection.State, "Prerequisite: actors must perform no driver I/O.");
        var retained = mapping.GetEntity(typeof(NativeEntry), nameof(NativeContext.Entries));

        VerifyNativeTable(provider.GetTable(calls.First.Value), expected, "first mapping return");
        VerifyNativeTable(provider.GetTable(calls.Competing.Value), expected, "competing mapping return");
        VerifyNativeTable(provider.GetTable(retained), expected, "retained mapping lookup");
        RecordIdentities("mapping", calls, retained, pause);
        Assert.AreSame(calls.Competing.Value, retained, "Prerequisite: the completed competing mapping must be retained.");
        Assert.AreSame(retained, calls.First.Value, "Canonical mapping publication must return the retained native entity.");
    }

    [TestMethod]
    public void TableCreationReturnsThePublishedNativeTable()
    {
        var expected = AcquireNativeRows();
        using var connection = new SqliteConnection("Data Source=:memory:");
        using var pause = new CreationPause();
        var mapping = new AttributeMapping(typeof(NativeContext));
        var entity = mapping.GetEntity(typeof(NativeEntry), nameof(NativeContext.Entries));
        var provider = new PausedProvider(connection, mapping, pause);
        var calls = Compete(() => provider.GetTable(entity), pause);
        Assert.AreEqual(ConnectionState.Closed, connection.State, "Prerequisite: actors must perform no driver I/O.");
        var retained = provider.GetTable(entity);

        VerifyNativeTable(calls.First.Value, expected, "first table return");
        VerifyNativeTable(calls.Competing.Value, expected, "competing table return");
        VerifyNativeTable(retained, expected, "retained table lookup");
        RecordIdentities("table", calls, retained, pause);
        Assert.AreSame(calls.Competing.Value, retained, "Prerequisite: the completed competing table must be retained.");
        Assert.AreSame(retained, calls.First.Value, "Canonical table publication must return the retained native table.");
    }

    [TestMethod]
    public void SequentialMappingRetainsItsNativeEntityAndResults()
    {
        var expected = AcquireNativeRows();
        using var connection = new SqliteConnection("Data Source=:memory:");
        var mapping = new AttributeMapping(typeof(NativeContext));
        var provider = new SQLiteQueryProvider(connection, mapping, QueryPolicy.Default);
        var first = mapping.GetEntity(typeof(NativeEntry), nameof(NativeContext.Entries));
        var retained = mapping.GetEntity(typeof(NativeEntry), nameof(NativeContext.Entries));
        VerifyNativeTable(provider.GetTable(first), expected, "sequential first mapping");
        VerifyNativeTable(provider.GetTable(retained), expected, "sequential retained mapping");
        Assert.AreSame(first, retained, "Sequential native mapping lookup must retain its entity.");
    }

    [TestMethod]
    public void SequentialTableRetainsItsNativeRootAndResults()
    {
        var expected = AcquireNativeRows();
        using var connection = new SqliteConnection("Data Source=:memory:");
        var mapping = new AttributeMapping(typeof(NativeContext));
        var entity = mapping.GetEntity(typeof(NativeEntry), nameof(NativeContext.Entries));
        var provider = new SQLiteQueryProvider(connection, mapping, QueryPolicy.Default);
        var first = provider.GetTable(entity);
        var retained = provider.GetTable(entity);
        VerifyNativeTable(first, expected, "sequential first table");
        VerifyNativeTable(retained, expected, "sequential retained table");
        Assert.AreSame(first, retained, "Sequential native table lookup must retain its root.");
    }

    private static List<string> AcquireNativeRows()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM pragma_compile_options LIMIT 0;";
            using var reader = command.ExecuteReader();
            var columns = reader.GetColumnSchema();
            Assert.AreEqual(1, columns.Count, "Prerequisite: native compile-option schema must have one column.");
            Assert.AreEqual("compile_options", columns[0].ColumnName);
            Assert.AreEqual(typeof(string), columns[0].DataType);
            Console.WriteLine("Native schema: " + JsonSerializer.Serialize(new
            {
                columns[0].ColumnName,
                DataType = columns[0].DataType.FullName,
                columns[0].DataTypeName
            }));
        }
        var rows = SQLiteMetadata.ReadValues(NativeRequest);
        Assert.IsTrue(rows.Count > 0, "Prerequisite: actual native compile options are required.");
        return rows;
    }

    private static void VerifyNativeTable(IEntityTable table, List<string> expected, string label)
    {
        Assert.IsInstanceOfType<IEntityTable<NativeEntry>>(table);
        var provider = (SQLiteQueryProvider)table.Provider;
        using var log = new StringWriter();
        provider.Log = log;
        try
        {
            var actual = ((IEntityTable<NativeEntry>)table).Select(row => row.Name).ToList();
            Console.WriteLine("Native canonical projection: " + JsonSerializer.Serialize(new
            {
                Label = label, Request = NativeRequest, Native = expected, Toolkit = actual, Sql = log.ToString()
            }));
            // The query has no ORDER BY; compare the complete unchanged native multiset.
            CollectionAssert.AreEquivalent(expected, actual, "Prerequisite: each actual root must preserve native rows.");
        }
        finally
        {
            provider.Log = null;
        }
    }

    private static (Call<T> First, Call<T> Competing) Compete<T>(Func<T> acquire, CreationPause pause)
        where T : class
    {
        using var cancellation = new CancellationTokenSource();
        Task<Call<T>> first = null;
        Task<Call<T>> competing = null;
        pause.ActorsSettled = false;
        try
        {
            first = Task.Factory.StartNew(() => Observe(acquire), cancellation.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Assert.IsTrue(pause.Entered.Wait(WaitLimit), "Prerequisite: first actual factory must reach its pause.");
            competing = Task.Factory.StartNew(() => Observe(acquire), cancellation.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Assert.IsTrue(competing.Wait(WaitLimit), "Prerequisite: competing acquisition must complete while first is held.");
            var second = competing.GetAwaiter().GetResult();
            Assert.IsFalse(first.IsCompleted, "Prerequisite: first acquisition must remain held before release.");
            Assert.IsTrue(pause.Observations.Any(value => value.Ordinal >= 2),
                "Prerequisite: a competing actual factory must have returned.");
            pause.ReleaseFirst();
            Assert.IsTrue(first.Wait(WaitLimit), "Prerequisite: released first acquisition must settle.");
            var initial = first.GetAwaiter().GetResult();
            Assert.AreNotEqual(initial.ThreadId, second.ThreadId, "Prerequisite: acquisitions need distinct actual actors.");
            Assert.IsTrue(second.EndTicks <= pause.ReleaseTicks && initial.EndTicks >= pause.ReleaseTicks,
                "Prerequisite: competing completion must precede release and first completion.");
            return (initial, second);
        }
        finally
        {
            cancellation.Cancel();
            pause.ReleaseFirst();
            var actors = new Task[] { first, competing }.Where(task => task != null).ToArray();
            bool settled;
            try
            {
                settled = Task.WaitAll(actors, WaitLimit);
            }
            catch (AggregateException error)
            {
                // A fault is never a canonical-publication RED. Preserve its actual diagnostic.
                Console.WriteLine("Actual actor settlement error: " + error);
                settled = actors.All(task => task.IsCompleted);
            }
            pause.ActorsSettled = settled;
            Console.WriteLine("Canonical actor cleanup: " + JsonSerializer.Serialize(new
            {
                Actors = actors.Length, Settled = settled,
                Statuses = actors.Select(task => task.Status.ToString()).ToArray()
            }));
            Assert.IsTrue(settled, "Prerequisite: all actual acquisition actors must settle before native I/O.");
        }
    }

    private static Call<T> Observe<T>(Func<T> acquire)
    {
        int thread = Environment.CurrentManagedThreadId;
        long begin = Stopwatch.GetTimestamp();
        var value = acquire();
        return new Call<T>(value, thread, begin, Stopwatch.GetTimestamp());
    }

    private static void RecordIdentities<T>(string target, (Call<T> First, Call<T> Competing) calls,
        T retained, CreationPause pause)
    {
        Console.WriteLine("Canonical publication observation: " + JsonSerializer.Serialize(new
        {
            Target = target, StopwatchFrequency = Stopwatch.Frequency,
            FirstThread = calls.First.ThreadId, FirstBegin = calls.First.BeginTicks, FirstEnd = calls.First.EndTicks,
            CompetingThread = calls.Competing.ThreadId, CompetingBegin = calls.Competing.BeginTicks,
            CompetingEnd = calls.Competing.EndTicks, pause.ReleaseTicks, pause.ActorsSettled,
            FirstMatchesRetained = ReferenceEquals(calls.First.Value, retained),
            CompetingMatchesRetained = ReferenceEquals(calls.Competing.Value, retained),
            FirstFactoryReturnType = pause.FirstValue?.GetType().FullName,
            FirstTableMatchesActualFactoryReturn = pause.FirstValue is IEntityTable
                ? (bool?)ReferenceEquals(calls.First.Value, pause.FirstValue) : null,
            Callbacks = pause.Observations.OrderBy(value => value.Ordinal).ToArray()
        }));
    }

    private sealed record Call<T>(T Value, int ThreadId, long BeginTicks, long EndTicks);
    private sealed record Callback(int Ordinal, int ThreadId, long EnterTicks, long ReturnTicks, bool Released);

    private sealed class CreationPause : IDisposable
    {
        private readonly ManualResetEventSlim _release = new(false);
        private int _ordinal;
        private long _releaseTicks;

        internal ManualResetEventSlim Entered { get; } = new(false);
        internal ConcurrentQueue<Callback> Observations { get; } = new();
        internal object FirstValue { get; private set; }
        internal bool ActorsSettled { get; set; } = true;
        internal long ReleaseTicks => Interlocked.Read(ref _releaseTicks);

        internal T ActualReturn<T>(T value)
        {
            int ordinal = Interlocked.Increment(ref _ordinal);
            int thread = Environment.CurrentManagedThreadId;
            long enter = Stopwatch.GetTimestamp();
            bool released = true;
            if (ordinal == 1)
            {
                FirstValue = value;
                Entered.Set();
                released = _release.Wait(WaitLimit);
            }
            Observations.Enqueue(new Callback(ordinal, thread, enter, Stopwatch.GetTimestamp(), released));
            if (!released)
                throw new TimeoutException("Prerequisite: first actual factory was not released within its bound.");
            return value;
        }

        internal void ReleaseFirst()
        {
            Interlocked.CompareExchange(ref _releaseTicks, Stopwatch.GetTimestamp(), 0);
            _release.Set();
        }

        public void Dispose()
        {
            // Never dispose synchronization objects underneath an unsettled actor.
            // Incomplete settlement fails the test and the outer bounded runner.
            if (ActorsSettled)
            {
                Entered.Dispose();
                _release.Dispose();
            }
        }
    }

    private sealed class PausedMapping : AttributeMapping
    {
        private readonly CreationPause _pause;

        internal PausedMapping(CreationPause pause) : base(typeof(NativeContext)) => _pause = pause;

        protected override IEnumerable<MappingAttribute> GetMappingAttributes(string rootEntityId)
        {
            // Materialize only attributes actually returned by the existing reflection seam.
            var attributes = base.GetMappingAttributes(rootEntityId).ToArray();
            return _pause.ActualReturn(attributes);
        }
    }

    private sealed class PausedProvider : SQLiteQueryProvider
    {
        private readonly CreationPause _pause;

        internal PausedProvider(SqliteConnection connection, QueryMapping mapping, CreationPause pause)
            : base(connection, mapping, QueryPolicy.Default) => _pause = pause;

        protected override IEntityTable CreateTable(MappingEntity entity)
            => _pause.ActualReturn(base.CreateTable(entity));
    }
}
