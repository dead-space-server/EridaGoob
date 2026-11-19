// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Erida.CCVar;
using Content.Shared._Erida.Profiler;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Profiling;
using Robust.Shared.Timing;

namespace Content.Server._Erida.Profiler;

/// <summary>
/// Reads the engine profiler buffer to find slow ticks and per-system timings, and collects log errors,
/// for the host-only profiler window.
/// </summary>
/// <remarks>
/// History is kept for the whole server lifetime on purpose, so it is not cleared on round restart:
/// the freezes worth looking at often happen around round start and end.
/// </remarks>
public sealed partial class ProfilerSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private ProfManager _prof = default!;

    public const int MaxSlowTicks = 50;
    public const string ProfilerSawmill = "erida.profiler";
    private const int MinBufferSize = 32768;
    private const int MaxTreeNodes = 500;
    private const float TreeMinMs = 0.1f;
    private const int MaxSlowTickTreesPerSecond = 2;
    private static readonly TimeSpan SlowTickLogInterval = TimeSpan.FromSeconds(5);

    private readonly List<ProfilerEntry> _entries = [];
    private readonly List<(int Entry, long Pos)> _stack = [];
    private readonly List<ProfilerSlowTick> _slowTicks = [];
    private readonly HashSet<ProfilerEui> _subscribers = [];
    private List<ProfilerSlowTick> _pendingSlowTicks = [];

    private bool _enabled;
    private bool _ownsProf;
    private float _slowTickMs;
    private long _lastIndex;
    private TimeSpan _gcPausePrev;
    private TimeSpan _gcPausePrevPrev;
    private TimeSpan _nextSlowTickLog;
    private int _suppressedSlowTicks;
    private TimeSpan _slowTickBudgetReset;
    private int _slowTickTrees;

    private Func<int, string> _names = default!;
    private ProfilerLogHandler _logHandler = default!;
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill(ProfilerSawmill);
        _names = _prof.GetString;
        _logHandler = new ProfilerLogHandler(ProfilerSawmill);

        Subs.CVar(_cfg, ECCVars.ProfilerSlowTickMs, value => _slowTickMs = value, true);
        Subs.CVar(_cfg, ECCVars.ProfilerEnabled, SetEnabled, true);

        InitializeStats();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _logManager.RootSawmill.RemoveHandler(_logHandler);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled)
            return;

        // Someone switched the engine profiler off by hand: the cvar of this system owns it.
        if (!_prof.IsEnabled)
        {
            _cfg.SetCVar(CVars.ProfEnabled, true);
            _ownsProf = true;
            return;
        }

        ProcessFrames();
        UpdateStats();
    }

    #region Subscribers

    public int SubscriberCount => _subscribers.Count;

    public void AddSubscriber(ProfilerEui eui)
    {
        // The new window gets pending slow ticks in its full state, so it must not get them again.
        FlushSlowTicks();

        if (_subscribers.Add(eui) && _subscribers.Count == 1)
            ResetStats();
    }

    public void RemoveSubscriber(ProfilerEui eui)
    {
        _subscribers.Remove(eui);
        if (_subscribers.Count == 0)
            _pendingSlowTicks.Clear();
    }

    public ProfilerEuiState GetState()
    {
        return new ProfilerEuiState(
            _lastOverview,
            _lastSystems,
            [.. _slowTicks],
            _logHandler.GetAll(),
            _slowTickMs,
            _enabled);
    }

    /// <summary>
    /// Clears slow ticks, log groups and stats, and resends the empty state to every open window.
    /// </summary>
    public void Reset()
    {
        _slowTicks.Clear();
        _pendingSlowTicks.Clear();
        _logHandler.Clear();
        _suppressedSlowTicks = 0;
        ResetStats();

        foreach (var eui in _subscribers)
        {
            eui.StateDirty();
        }
    }

    #endregion

    #region Frames

    private void ProcessFrames()
    {
        ref readonly var buffer = ref _prof.Buffer;
        var end = buffer.IndexWriteOffset;

        // The index buffer was recreated after prof.index_size changed.
        if (end < _lastIndex)
            _lastIndex = end;

        var start = Math.Max(_lastIndex, end - buffer.IndexBuffer.LongLength);
        if (start >= end)
            return;

        _lastIndex = end;

        // Frames are read one frame late, so the window of two batches covers the whole slow frame.
        var gcPause = GC.GetTotalPauseDuration();
        var gcWindow = gcPause - _gcPausePrevPrev;
        _gcPausePrevPrev = _gcPausePrev;
        _gcPausePrev = gcPause;

        var collect = _subscribers.Count > 0;
        for (var i = start; i < end; i++)
        {
            ref readonly var index = ref buffer.Index(i);
            if (!ProfilerFrameParser.TryGetFrameTime(buffer, index, out var frameMs))
                continue;

            var slow = frameMs >= _slowTickMs;
            if (!slow && !collect)
                continue;

            if (!ProfilerFrameParser.TryParse(buffer, index, _entries, _stack))
                continue;

            if (collect)
                Accumulate(slow);

            if (slow)
                RecordSlowTick(frameMs, (float) gcWindow.TotalMilliseconds);
        }
    }

    private void RecordSlowTick(float frameMs, float gcPauseMs)
    {
        var (tick, tickCount) = GetTickInfo();

        // When every frame is slow, building a tree for each one only adds garbage and traffic to the lag.
        if (TryTakeSlowTickTree())
        {
            var nodes = ProfilerFrameParser.BuildTree(_entries, _names, TreeMinMs, MaxTreeNodes, out var truncated);
            var slowTick = new ProfilerSlowTick
            {
                Time = DateTime.UtcNow,
                Tick = tick,
                FrameMs = frameMs,
                TickCount = tickCount,
                GcPauseMs = gcPauseMs,
                Truncated = truncated,
                Nodes = nodes,
            };

            _slowTicks.Add(slowTick);
            if (_slowTicks.Count > MaxSlowTicks)
                _slowTicks.RemoveAt(0);

            if (_subscribers.Count > 0)
                _pendingSlowTicks.Add(slowTick);
        }

        LogSlowTick(tick, frameMs, gcPauseMs);
    }

    private bool TryTakeSlowTickTree()
    {
        if (_timing.RealTime >= _slowTickBudgetReset)
        {
            _slowTickBudgetReset = _timing.RealTime + TimeSpan.FromSeconds(1);
            _slowTickTrees = 0;
        }

        if (_slowTickTrees >= MaxSlowTickTreesPerSecond)
            return false;

        _slowTickTrees++;
        return true;
    }

    /// <summary>
    /// Sends slow ticks recorded since the last flush to every open window.
    /// </summary>
    private void FlushSlowTicks()
    {
        if (_pendingSlowTicks.Count == 0)
            return;

        var message = new ProfilerSlowTicksMessage(_pendingSlowTicks);
        _pendingSlowTicks = [];

        foreach (var eui in _subscribers)
        {
            eui.SendMessage(message);
        }
    }

    private (uint Tick, int Count) GetTickInfo()
    {
        uint? tick = null;
        var count = 0;
        foreach (var entry in _entries)
        {
            if (entry.IsGroup || entry.StringId < 0)
                continue;

            switch (_names(entry.StringId))
            {
                case "Tick" when tick == null && entry.Value.Type == ProfValueType.Int64:
                    tick = (uint) entry.Value.Int64;
                    break;
                case "Tick count" when entry.Value.Type == ProfValueType.Int32:
                    count = entry.Value.Int32;
                    break;
            }
        }

        return (tick ?? _timing.CurTick.Value, count);
    }

    private void LogSlowTick(uint tick, float frameMs, float gcPauseMs)
    {
        if (_timing.RealTime < _nextSlowTickLog)
        {
            _suppressedSlowTicks++;
            return;
        }

        _nextSlowTickLog = _timing.RealTime + SlowTickLogInterval;

        var slowest = ProfilerFrameParser.GetSlowest(_entries, _names, 3);
        var slowestText = string.Join(", ", slowest.ConvertAll(s => $"{s.Name} {s.Ms:F1}ms"));
        var suppressedText = _suppressedSlowTicks > 0
            ? $", +{_suppressedSlowTicks} slow ticks not logged"
            : string.Empty;

        _sawmill.Warning(
            $"Slow tick {tick}: {frameMs:F0}ms, GC pause {gcPauseMs:F0}ms, slowest: {slowestText}{suppressedText}");
        _suppressedSlowTicks = 0;
    }

    #endregion

    #region Helpers

    private void SetEnabled(bool enabled)
    {
        _enabled = enabled;

        if (!enabled)
        {
            _logManager.RootSawmill.RemoveHandler(_logHandler);
            if (_ownsProf)
                _cfg.SetCVar(CVars.ProfEnabled, false);

            _ownsProf = false;
            return;
        }

        if (_cfg.GetCVar(CVars.ProfBufferSize) < MinBufferSize)
            _cfg.SetCVar(CVars.ProfBufferSize, MinBufferSize);

        if (!_cfg.GetCVar(CVars.ProfEnabled))
        {
            _cfg.SetCVar(CVars.ProfEnabled, true);
            _ownsProf = true;
        }

        _lastIndex = _prof.Buffer.IndexWriteOffset;
        _gcPausePrev = _gcPausePrevPrev = GC.GetTotalPauseDuration();
        _logManager.RootSawmill.RemoveHandler(_logHandler);
        _logManager.RootSawmill.AddHandler(_logHandler);
    }

    #endregion
}
