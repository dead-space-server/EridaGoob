// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Erida.Profiler;

/// <summary>
/// Aggregated timing of one profiled entry (usually an entity system update) over the stats window.
/// </summary>
[Serializable, NetSerializable]
public struct ProfilerSystemStat
{
    public required string Name;
    public required float TotalMs;
    public required float AvgMs;
    public required float MaxMs;
    public required long AvgAlloc;
    public required int Calls;
}

/// <summary>
/// Server-wide numbers over the stats window.
/// </summary>
[Serializable, NetSerializable]
public struct ProfilerOverview
{
    public required float WindowSeconds;
    public required int Ticks;
    public required float AvgTickMs;
    public required float MaxTickMs;
    public required int SlowTicks;
    public required float GcPauseMs;
    public required int Gen0;
    public required int Gen1;
    public required int Gen2;
    public required long ManagedMemory;
    public required int Entities;
    public required int Players;
}

/// <summary>
/// One node of a profiled frame tree, stored in pre-order with its depth.
/// </summary>
[Serializable, NetSerializable]
public struct ProfilerNode
{
    public required string Name;
    public required int Depth;
    public required bool IsGroup;
    public float TimeMs;
    public long Alloc;
    public long? Value;
}

/// <summary>
/// A server frame that took longer than the slow tick threshold.
/// </summary>
[Serializable, NetSerializable]
public struct ProfilerSlowTick
{
    public required DateTime Time;
    public required uint Tick;
    public required float FrameMs;
    public required int TickCount;
    public required float GcPauseMs;
    public required bool Truncated;
    public required List<ProfilerNode> Nodes;
}

/// <summary>
/// Repeated log messages of one kind.
/// </summary>
[Serializable, NetSerializable]
public struct ProfilerLogGroup
{
    public required string Key;
    public required string Sawmill;
    public required LogLevel Level;
    public required string Message;
    public string? Exception;
    public required int Count;
    public required DateTime FirstSeen;
    public required DateTime LastSeen;
}

[Serializable, NetSerializable]
public sealed class ProfilerEuiState(
    ProfilerOverview? overview,
    List<ProfilerSystemStat> systems,
    List<ProfilerSlowTick> slowTicks,
    List<ProfilerLogGroup> logs,
    float slowTickMs,
    bool enabled) : EuiStateBase
{
    public ProfilerOverview? Overview { get; } = overview;
    public List<ProfilerSystemStat> Systems { get; } = systems;
    public List<ProfilerSlowTick> SlowTicks { get; } = slowTicks;
    public List<ProfilerLogGroup> Logs { get; } = logs;
    public float SlowTickMs { get; } = slowTickMs;
    public bool Enabled { get; } = enabled;
}

[Serializable, NetSerializable]
public sealed class ProfilerStatsMessage(ProfilerOverview overview, List<ProfilerSystemStat> systems) : EuiMessageBase
{
    public ProfilerOverview Overview { get; } = overview;
    public List<ProfilerSystemStat> Systems { get; } = systems;
}

/// <summary>
/// Slow ticks recorded since the previous batch, oldest first.
/// </summary>
[Serializable, NetSerializable]
public sealed class ProfilerSlowTicksMessage(List<ProfilerSlowTick> slowTicks) : EuiMessageBase
{
    public List<ProfilerSlowTick> SlowTicks { get; } = slowTicks;
}

/// <summary>
/// New or updated log groups, matched on the client by <see cref="ProfilerLogGroup.Key"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class ProfilerLogsMessage(List<ProfilerLogGroup> logs) : EuiMessageBase
{
    public List<ProfilerLogGroup> Logs { get; } = logs;
}

[Serializable, NetSerializable]
public sealed class ProfilerResetMessage : EuiMessageBase;
