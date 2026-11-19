// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text;
using System.Threading;
using Content.Shared._Erida.Profiler;
using Serilog.Events;

namespace Content.Server._Erida.Profiler;

/// <summary>
/// Collects warnings and errors from every sawmill, grouped by kind.
/// </summary>
/// <remarks>
/// Called from any thread, so all state is behind a lock. Must never log itself.
/// Repeats only bump a counter, so a log storm stays cheap.
/// </remarks>
public sealed class ProfilerLogHandler(string ownSawmill) : ILogHandler
{
    public const int MaxGroups = 200;
    private const int MaxMessageLength = 1000;
    private const int MaxExceptionLength = 6000;

    private readonly Lock _lock = new();
    private readonly Dictionary<string, ProfilerLogGroup> _groups = [];
    private readonly HashSet<string> _changed = [];

    public void Log(string sawmillName, LogEvent message)
    {
        if (message.Level < LogEventLevel.Warning || sawmillName == ownSawmill)
            return;

        // A throwing handler would break logging for the whole server.
        try
        {
            Collect(sawmillName, message);
        }
        catch
        {
            // ignored
        }
    }

    private void Collect(string sawmillName, LogEvent message)
    {
        var key = BuildKey(sawmillName, message);
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            if (_groups.TryGetValue(key, out var group))
            {
                group.Count++;
                group.LastSeen = now;
                _groups[key] = group;
                _changed.Add(key);
                return;
            }

            if (_groups.Count >= MaxGroups)
                EvictOldest();

            _groups[key] = new ProfilerLogGroup
            {
                Key = key,
                Sawmill = sawmillName,
                Level = ToLogLevel(message.Level),
                Message = Truncate(message.RenderMessage(), MaxMessageLength),
                Exception = message.Exception is { } exception
                    ? Truncate(exception.ToString(), MaxExceptionLength)
                    : null,
                Count = 1,
                FirstSeen = now,
                LastSeen = now,
            };
            _changed.Add(key);
        }
    }

    public List<ProfilerLogGroup> GetAll()
    {
        lock (_lock)
        {
            return [.. _groups.Values];
        }
    }

    /// <summary>
    /// Groups added or updated since the last call.
    /// </summary>
    public List<ProfilerLogGroup> TakeChanged()
    {
        lock (_lock)
        {
            var result = new List<ProfilerLogGroup>(_changed.Count);
            foreach (var key in _changed)
            {
                if (_groups.TryGetValue(key, out var group))
                    result.Add(group);
            }

            _changed.Clear();
            return result;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _groups.Clear();
            _changed.Clear();
        }
    }

    private void EvictOldest()
    {
        string? oldest = null;
        var oldestTime = DateTime.MaxValue;
        foreach (var (key, group) in _groups)
        {
            if (group.LastSeen >= oldestTime)
                continue;

            oldest = key;
            oldestTime = group.LastSeen;
        }

        if (oldest == null)
            return;

        _groups.Remove(oldest);
        _changed.Remove(oldest);
    }

    /// <summary>
    /// Exceptions group by type and throwing frame, plain messages by text with numbers masked,
    /// so "Entity 123 has no X" and "Entity 456 has no X" land in one group.
    /// </summary>
    public static string BuildKey(string sawmill, LogEvent message)
    {
        var builder = new StringBuilder();
        builder.Append(sawmill).Append('|').Append((int) message.Level).Append('|');

        if (message.Exception is { } exception)
        {
            builder.Append(exception.GetType().FullName).Append('|');
            builder.Append(GetTopFrame(exception));
            return builder.ToString();
        }

        AppendMasked(builder, message.MessageTemplate.Text);
        return builder.ToString();
    }

    private static string GetTopFrame(Exception exception)
    {
        var trace = exception.StackTrace;
        if (string.IsNullOrEmpty(trace))
            return exception.Message;

        var end = trace.IndexOf('\n');
        return (end < 0 ? trace : trace[..end]).Trim();
    }

    private static void AppendMasked(StringBuilder builder, string text)
    {
        var inNumber = false;
        foreach (var c in text)
        {
            if (char.IsAsciiDigit(c))
            {
                if (!inNumber)
                    builder.Append('#');

                inNumber = true;
                continue;
            }

            inNumber = false;
            builder.Append(c);
        }
    }

    private static string Truncate(string text, int length)
    {
        return text.Length <= length ? text : text[..length] + "…";
    }

    private static LogLevel ToLogLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Fatal => LogLevel.Fatal,
            LogEventLevel.Error => LogLevel.Error,
            _ => LogLevel.Warning,
        };
    }
}
