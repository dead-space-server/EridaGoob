// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Erida.Profiler;
using Robust.Shared.Profiling;

namespace Content.Server._Erida.Profiler;

/// <summary>
/// One log entry of a profiled frame, with its group nesting resolved.
/// </summary>
public struct ProfilerEntry
{
    public int StringId;
    public int Depth;
    public int Parent;
    public bool IsGroup;
    public ProfValue Value;

    public readonly bool HasTime => Value.Type == ProfValueType.TimeAllocSample;

    public readonly float TimeMs => HasTime ? Value.TimeAllocSample.Time * 1000f : 0f;
}

/// <summary>
/// Reads frames that the engine game loop writes into the <see cref="ProfManager"/> ring buffer.
/// </summary>
/// <remarks>
/// Layout of a frame: a "Start Frame" value, then a "Frame" group with Input, Ticks (Tick groups with
/// per-system values), Update and GC Overview inside it. The index of a frame points just past the
/// "Frame" group end, so the last entry of a frame holds its total time.
/// </remarks>
public static class ProfilerFrameParser
{
    /// <summary>
    /// Groups that only wrap other timed entries, so their own time says nothing about who is slow.
    /// </summary>
    public static readonly HashSet<string> ContainerGroups = ["Frame", "Ticks", "Tick", "Systems"];

    /// <summary>
    /// Whether the frame is still fully present in the ring buffer.
    /// </summary>
    public static bool IsAvailable(in ProfBuffer buffer, in ProfIndex index)
    {
        return index.Type == ProfIndexType.Frame
               && index.EndPos > index.StartPos
               && index.EndPos <= buffer.LogWriteOffset
               && index.StartPos >= buffer.LogWriteOffset - buffer.LogBuffer.LongLength;
    }

    public static bool TryGetFrameTime(in ProfBuffer buffer, in ProfIndex index, out float ms)
    {
        ms = 0f;
        if (!IsAvailable(buffer, index))
            return false;

        ref var last = ref buffer.Log(index.EndPos - 1);
        if (last.Type != ProfLogType.GroupEnd
            || last.GroupEnd.Value.Type != ProfValueType.TimeAllocSample)
            return false;

        ms = last.GroupEnd.Value.TimeAllocSample.Time * 1000f;
        return true;
    }

    /// <summary>
    /// Flattens a frame into <paramref name="entries"/> in pre-order.
    /// </summary>
    /// <returns>False if the frame was overwritten or does not have the expected layout.</returns>
    public static bool TryParse(
        in ProfBuffer buffer,
        in ProfIndex index,
        List<ProfilerEntry> entries,
        List<(int Entry, long Pos)> stack)
    {
        entries.Clear();
        stack.Clear();

        if (!IsAvailable(buffer, index))
            return false;

        for (var pos = index.StartPos; pos < index.EndPos; pos++)
        {
            ref var log = ref buffer.Log(pos);
            var parent = stack.Count > 0 ? stack[^1].Entry : -1;

            switch (log.Type)
            {
                case ProfLogType.GroupStart:
                    stack.Add((entries.Count, pos));
                    entries.Add(new ProfilerEntry
                    {
                        StringId = -1,
                        Depth = stack.Count - 1,
                        Parent = parent,
                        IsGroup = true,
                    });
                    break;
                case ProfLogType.Value:
                    entries.Add(new ProfilerEntry
                    {
                        StringId = log.Value.StringId,
                        Depth = stack.Count,
                        Parent = parent,
                        Value = log.Value.Value,
                    });
                    break;
                case ProfLogType.GroupEnd:
                    if (stack.Count == 0 || stack[^1].Pos != log.GroupEnd.StartIndex)
                        return false;

                    var (entryIdx, _) = stack[^1];
                    stack.RemoveAt(stack.Count - 1);

                    var entry = entries[entryIdx];
                    entry.StringId = log.GroupEnd.StringId;
                    entry.Value = log.GroupEnd.Value;
                    entries[entryIdx] = entry;
                    break;
                default:
                    return false;
            }
        }

        return stack.Count == 0;
    }

    /// <summary>
    /// Builds a network tree, dropping timed nodes faster than <paramref name="minMs"/>.
    /// Untimed values (counters) are kept when their group is kept.
    /// </summary>
    public static List<ProfilerNode> BuildTree(
        List<ProfilerEntry> entries,
        Func<int, string> names,
        float minMs,
        int maxNodes,
        out bool truncated)
    {
        truncated = false;
        List<ProfilerNode> nodes = [];
        var keep = new bool[entries.Count];

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var parentKept = entry.Parent < 0 || keep[entry.Parent];
            keep[i] = parentKept && (!entry.HasTime || entry.TimeMs >= minMs);

            if (!keep[i])
                continue;

            if (nodes.Count >= maxNodes)
            {
                truncated = true;
                break;
            }

            nodes.Add(new ProfilerNode
            {
                Name = entry.StringId >= 0 ? names(entry.StringId) : "?",
                Depth = entry.Depth,
                IsGroup = entry.IsGroup,
                TimeMs = entry.TimeMs,
                Alloc = entry.HasTime ? entry.Value.TimeAllocSample.Alloc : 0,
                Value = entry.Value.Type switch
                {
                    ProfValueType.Int32 => entry.Value.Int32,
                    ProfValueType.Int64 => entry.Value.Int64,
                    _ => null,
                },
            });
        }

        return nodes;
    }

    /// <summary>
    /// Whether the entry is a timed bucket worth ranking: a system update, a command or a non-container group.
    /// </summary>
    public static bool IsRankable(in ProfilerEntry entry, Func<int, string> names)
    {
        return entry.HasTime
               && entry.StringId >= 0
               && (!entry.IsGroup || !ContainerGroups.Contains(names(entry.StringId)));
    }

    /// <summary>
    /// The slowest rankable entries of the frame.
    /// </summary>
    public static List<(string Name, float Ms)> GetSlowest(
        List<ProfilerEntry> entries,
        Func<int, string> names,
        int count)
    {
        List<(string Name, float Ms)> result = [];
        foreach (var entry in entries)
        {
            if (!IsRankable(entry, names))
                continue;

            result.Add((names(entry.StringId), entry.TimeMs));
        }

        result.Sort((a, b) => b.Ms.CompareTo(a.Ms));
        if (result.Count > count)
            result.RemoveRange(count, result.Count - count);

        return result;
    }
}
