// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using Content.Server._Erida.Profiler;
using NUnit.Framework;
using Robust.Shared.Profiling;

namespace Content.Tests.Server._Erida;

/// <summary>
/// Parses hand-built frames laid out like the engine game loop writes them.
/// </summary>
[TestFixture, TestOf(typeof(ProfilerFrameParser))]
public sealed class ProfilerFrameParserTest
{
    private readonly List<string> _strings = [];

    private string Name(int id) => _strings[id];

    private int Id(string name)
    {
        var id = _strings.IndexOf(name);
        if (id >= 0)
            return id;

        _strings.Add(name);
        return _strings.Count - 1;
    }

    private static ProfValue Time(float ms, long alloc = 0)
    {
        return new ProfValue
        {
            Type = ProfValueType.TimeAllocSample,
            TimeAllocSample = new TimeAndAllocSample { Time = ms / 1000f, Alloc = alloc },
        };
    }

    private static ref ProfLog Write(ref ProfBuffer buffer)
    {
        return ref buffer.Log(buffer.LogWriteOffset++);
    }

    private void Value(ref ProfBuffer buffer, string name, ProfValue value)
    {
        ref var log = ref Write(ref buffer);
        log = default;
        log.Type = ProfLogType.Value;
        log.Value.StringId = Id(name);
        log.Value.Value = value;
    }

    private static long GroupStart(ref ProfBuffer buffer)
    {
        var pos = buffer.LogWriteOffset;
        ref var log = ref Write(ref buffer);
        log = default;
        log.Type = ProfLogType.GroupStart;
        return pos;
    }

    private void GroupEnd(ref ProfBuffer buffer, long start, string name, ProfValue value)
    {
        ref var log = ref Write(ref buffer);
        log = default;
        log.Type = ProfLogType.GroupEnd;
        log.GroupEnd.StartIndex = start;
        log.GroupEnd.StringId = Id(name);
        log.GroupEnd.Value = value;
    }

    /// <summary>
    /// Frame { Ticks { Tick { Tick=7, SlowSystem 80ms, FastSystem 0.01ms }, Tick count=1 }, GC Overview { Gen 2 Count=1 } }
    /// </summary>
    private ProfIndex WriteFrame(ref ProfBuffer buffer)
    {
        var frameStart = buffer.LogWriteOffset;
        Value(ref buffer, "Start Frame", new ProfValue { Type = ProfValueType.Int64, Int64 = 1 });

        var frame = GroupStart(ref buffer);
        var ticks = GroupStart(ref buffer);
        var tick = GroupStart(ref buffer);
        Value(ref buffer, "Tick", new ProfValue { Type = ProfValueType.Int64, Int64 = 7 });
        Value(ref buffer, "SlowSystem", Time(80f, 1024));
        Value(ref buffer, "FastSystem", Time(0.01f));
        GroupEnd(ref buffer, tick, "Tick", Time(81f));
        Value(ref buffer, "Tick count", new ProfValue { Type = ProfValueType.Int32, Int32 = 1 });
        GroupEnd(ref buffer, ticks, "Ticks", Time(81f));

        var gc = GroupStart(ref buffer);
        Value(ref buffer, "Gen 2 Count", new ProfValue { Type = ProfValueType.Int32, Int32 = 1 });
        GroupEnd(ref buffer, gc, "GC Overview", Time(0.001f));
        GroupEnd(ref buffer, frame, "Frame", Time(120f));

        return new ProfIndex
        {
            Type = ProfIndexType.Frame,
            StartPos = frameStart,
            EndPos = buffer.LogWriteOffset,
        };
    }

    private static ProfBuffer NewBuffer(int size = 64)
    {
        return new ProfBuffer
        {
            LogBuffer = new ProfLog[size],
            IndexBuffer = new ProfIndex[8],
        };
    }

    [Test]
    public void ReadsFrameTimeFromLastEntry()
    {
        var buffer = NewBuffer();
        var index = WriteFrame(ref buffer);

        Assert.That(ProfilerFrameParser.TryGetFrameTime(buffer, index, out var ms), Is.True);
        Assert.That(ms, Is.EqualTo(120f).Within(0.01f));
    }

    [Test]
    public void BuildsPrunedTree()
    {
        var buffer = NewBuffer();
        var index = WriteFrame(ref buffer);
        List<ProfilerEntry> entries = [];

        Assert.That(ProfilerFrameParser.TryParse(buffer, index, entries, []), Is.True);

        var nodes = ProfilerFrameParser.BuildTree(entries, Name, 0.1f, 100, out var truncated);
        var names = nodes.ConvertAll(n => $"{n.Depth}:{n.Name}");

        Assert.Multiple(() =>
        {
            Assert.That(truncated, Is.False);
            Assert.That(names, Is.EqualTo(new[]
            {
                "0:Start Frame",
                "0:Frame",
                "1:Ticks",
                "2:Tick",
                "3:Tick",
                "3:SlowSystem",
                "2:Tick count",
            }));
            Assert.That(nodes.Find(n => n.Name == "SlowSystem").Alloc, Is.EqualTo(1024));
            Assert.That(nodes.Find(n => n.Name == "Tick count").Value, Is.EqualTo(1));
        });
    }

    [Test]
    public void RanksSystemsNotContainers()
    {
        var buffer = NewBuffer();
        var index = WriteFrame(ref buffer);
        List<ProfilerEntry> entries = [];
        ProfilerFrameParser.TryParse(buffer, index, entries, []);

        var slowest = ProfilerFrameParser.GetSlowest(entries, Name, 1);

        Assert.That(slowest, Has.Count.EqualTo(1));
        Assert.That(slowest[0].Name, Is.EqualTo("SlowSystem"));
    }

    [Test]
    public void RejectsOverwrittenFrame()
    {
        var buffer = NewBuffer(16);
        var index = WriteFrame(ref buffer);
        WriteFrame(ref buffer);

        Assert.Multiple(() =>
        {
            Assert.That(ProfilerFrameParser.IsAvailable(buffer, index), Is.False);
            Assert.That(ProfilerFrameParser.TryGetFrameTime(buffer, index, out _), Is.False);
            Assert.That(ProfilerFrameParser.TryParse(buffer, index, [], []), Is.False);
        });
    }

    [Test]
    public void RejectsMismatchedGroups()
    {
        var buffer = NewBuffer();
        var start = buffer.LogWriteOffset;
        Value(ref buffer, "Start Frame", new ProfValue { Type = ProfValueType.Int64, Int64 = 1 });
        GroupStart(ref buffer);
        GroupEnd(ref buffer, start, "Frame", Time(1f));

        var index = new ProfIndex { Type = ProfIndexType.Frame, StartPos = start, EndPos = buffer.LogWriteOffset };

        Assert.That(ProfilerFrameParser.TryParse(buffer, index, [], []), Is.False);
    }
}
