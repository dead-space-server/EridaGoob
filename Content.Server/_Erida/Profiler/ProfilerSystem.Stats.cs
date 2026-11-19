// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Runtime.InteropServices;
using Content.Shared._Erida.Profiler;

namespace Content.Server._Erida.Profiler;

public sealed partial class ProfilerSystem
{
    private const int StatsBuckets = 10;
    private const int MaxStatRows = 100;
    private static readonly TimeSpan StatsInterval = TimeSpan.FromSeconds(1);

    private readonly StatsBucket[] _buckets = new StatsBucket[StatsBuckets];
    private readonly Dictionary<int, SystemAccumulator> _merged = [];
    private int _bucketIndex;
    private int _filledBuckets;
    private TimeSpan _nextStats;

    private ProfilerOverview? _lastOverview;
    private List<ProfilerSystemStat> _lastSystems = [];

    private void InitializeStats()
    {
        for (var i = 0; i < StatsBuckets; i++)
        {
            _buckets[i] = new StatsBucket();
        }
    }

    private void ResetStats()
    {
        foreach (var bucket in _buckets)
        {
            bucket.Reset(_timing.RealTime);
        }

        _bucketIndex = 0;
        _filledBuckets = 1;
        _nextStats = _timing.RealTime + StatsInterval;
        _lastOverview = null;
        _lastSystems = [];
    }

    private void Accumulate(bool slow)
    {
        var bucket = _buckets[_bucketIndex];
        if (slow)
            bucket.SlowTicks++;

        foreach (var entry in _entries)
        {
            if (!entry.HasTime || entry.StringId < 0)
                continue;

            var ms = entry.TimeMs;
            if (entry.IsGroup && _names(entry.StringId) == "Tick")
            {
                bucket.Ticks++;
                bucket.TickMsSum += ms;
                bucket.TickMsMax = Math.Max(bucket.TickMsMax, ms);
            }

            if (!ProfilerFrameParser.IsRankable(entry, _names))
                continue;

            ref var acc = ref CollectionsMarshal.GetValueRefOrAddDefault(bucket.Systems, entry.StringId, out _);
            acc.TotalMs += ms;
            acc.MaxMs = Math.Max(acc.MaxMs, ms);
            acc.Alloc += entry.Value.TimeAllocSample.Alloc;
            acc.Calls++;
        }
    }

    /// <summary>
    /// Once a second, sends the stats window, new slow ticks and new log groups to open windows and starts a new bucket.
    /// </summary>
    private void UpdateStats()
    {
        if (_subscribers.Count == 0 || _timing.RealTime < _nextStats)
            return;

        _nextStats = _timing.RealTime + StatsInterval;

        _lastOverview = BuildOverview();
        _lastSystems = BuildSystems();
        var logs = _logHandler.TakeChanged();
        FlushSlowTicks();

        foreach (var eui in _subscribers)
        {
            eui.SendMessage(new ProfilerStatsMessage(_lastOverview.Value, _lastSystems));
            if (logs.Count > 0)
                eui.SendMessage(new ProfilerLogsMessage(logs));
        }

        _bucketIndex = (_bucketIndex + 1) % StatsBuckets;
        _filledBuckets = Math.Min(_filledBuckets + 1, StatsBuckets);
        _buckets[_bucketIndex].Reset(_timing.RealTime);
    }

    private ProfilerOverview BuildOverview()
    {
        var ticks = 0;
        var slowTicks = 0;
        var tickMsSum = 0.0;
        var tickMsMax = 0f;
        var oldest = _buckets[_bucketIndex];

        for (var i = 0; i < _filledBuckets; i++)
        {
            var bucket = _buckets[(_bucketIndex - i + StatsBuckets) % StatsBuckets];
            ticks += bucket.Ticks;
            slowTicks += bucket.SlowTicks;
            tickMsSum += bucket.TickMsSum;
            tickMsMax = Math.Max(tickMsMax, bucket.TickMsMax);
            oldest = bucket;
        }

        return new ProfilerOverview
        {
            WindowSeconds = (float) (_timing.RealTime - oldest.StartTime).TotalSeconds,
            Ticks = ticks,
            AvgTickMs = ticks > 0 ? (float) (tickMsSum / ticks) : 0f,
            MaxTickMs = tickMsMax,
            SlowTicks = slowTicks,
            GcPauseMs = (float) (GC.GetTotalPauseDuration() - oldest.GcPauseStart).TotalMilliseconds,
            Gen0 = GC.CollectionCount(0) - oldest.Gen0Start,
            Gen1 = GC.CollectionCount(1) - oldest.Gen1Start,
            Gen2 = GC.CollectionCount(2) - oldest.Gen2Start,
            ManagedMemory = GC.GetTotalMemory(false),
            Entities = EntityManager.EntityCount,
            Players = _player.PlayerCount,
        };
    }

    private List<ProfilerSystemStat> BuildSystems()
    {
        _merged.Clear();
        for (var i = 0; i < _filledBuckets; i++)
        {
            var bucket = _buckets[(_bucketIndex - i + StatsBuckets) % StatsBuckets];
            foreach (var (id, acc) in bucket.Systems)
            {
                ref var merged = ref CollectionsMarshal.GetValueRefOrAddDefault(_merged, id, out _);
                merged.TotalMs += acc.TotalMs;
                merged.MaxMs = Math.Max(merged.MaxMs, acc.MaxMs);
                merged.Alloc += acc.Alloc;
                merged.Calls += acc.Calls;
            }
        }

        var systems = new List<ProfilerSystemStat>(_merged.Count);
        foreach (var (id, acc) in _merged)
        {
            systems.Add(new ProfilerSystemStat
            {
                Name = _names(id),
                TotalMs = (float) acc.TotalMs,
                AvgMs = (float) (acc.TotalMs / acc.Calls),
                MaxMs = acc.MaxMs,
                AvgAlloc = acc.Alloc / acc.Calls,
                Calls = acc.Calls,
            });
        }

        systems.Sort((a, b) => b.TotalMs.CompareTo(a.TotalMs));
        if (systems.Count > MaxStatRows)
            systems.RemoveRange(MaxStatRows, systems.Count - MaxStatRows);

        return systems;
    }

    private struct SystemAccumulator
    {
        public double TotalMs;
        public float MaxMs;
        public long Alloc;
        public int Calls;
    }

    private sealed class StatsBucket
    {
        public readonly Dictionary<int, SystemAccumulator> Systems = [];
        public int Ticks;
        public int SlowTicks;
        public double TickMsSum;
        public float TickMsMax;
        public TimeSpan StartTime;
        public TimeSpan GcPauseStart;
        public int Gen0Start;
        public int Gen1Start;
        public int Gen2Start;

        public void Reset(TimeSpan now)
        {
            Systems.Clear();
            Ticks = 0;
            SlowTicks = 0;
            TickMsSum = 0;
            TickMsMax = 0;
            StartTime = now;
            GcPauseStart = GC.GetTotalPauseDuration();
            Gen0Start = GC.CollectionCount(0);
            Gen1Start = GC.CollectionCount(1);
            Gen2Start = GC.CollectionCount(2);
        }
    }
}
