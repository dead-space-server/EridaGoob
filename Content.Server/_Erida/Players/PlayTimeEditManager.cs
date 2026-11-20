// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Erida.Players;

/// <summary>
/// Adds play time to a player whether they are online or not.
/// Online players go through <see cref="PlayTimeTrackingManager"/>, so the in-memory trackers do not overwrite the change.
/// Offline edits of one player run one after another, after the pending play time saves,
/// and <see cref="PlayTimeTrackingManager.LoadData"/> waits for them, so a connect or disconnect does not lose either side.
/// Must be called from the main thread.
/// </summary>
public sealed class PlayTimeEditManager
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _tracking = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    /// <summary>
    /// Last queued database operation per offline player.
    /// </summary>
    private readonly Dictionary<NetUserId, Task> _offline = [];

    public bool IsValidTracker(string tracker)
    {
        return _prototype.HasIndex<PlayTimeTrackerPrototype>(tracker);
    }

    /// <summary>
    /// Adds the given time to each tracker. A total never goes below zero.
    /// </summary>
    /// <returns>New totals and applied changes, or null if the player is online but their play time is still loading.</returns>
    public async Task<PlayTimeEdit?> AddTime(NetUserId userId, IReadOnlyDictionary<string, TimeSpan> times)
    {
        if (_playerManager.TryGetSessionById(userId, out var session))
            return AddTimeOnline(session, times);

        return await RunOffline(userId, () => AddTimeOffline(userId, times));
    }

    /// <summary>
    /// Current play time of all trackers, or null if the player is online but their play time is still loading.
    /// </summary>
    public async Task<Dictionary<string, TimeSpan>?> GetTimes(NetUserId userId)
    {
        if (!_playerManager.TryGetSessionById(userId, out var session))
            return await RunOffline(userId, () => GetStoredTimes(userId));

        if (!_tracking.TryGetTrackerTimes(session, out var times))
            return null;

        _tracking.FlushTracker(session);
        return new Dictionary<string, TimeSpan>(times);
    }

    /// <summary>
    /// Completes when the queued offline edits of the player are written. Never throws.
    /// </summary>
    public async Task WaitPendingEdit(NetUserId userId)
    {
        if (!_offline.TryGetValue(userId, out var pending))
            return;

        try
        {
            await pending;
        }
        catch
        {
            // The caller of the edit gets the error.
        }
    }

    private PlayTimeEdit? AddTimeOnline(ICommonSession session, IReadOnlyDictionary<string, TimeSpan> times)
    {
        if (!_tracking.TryGetTrackerTimes(session, out var current))
            return null;

        _tracking.FlushTracker(session);

        var edit = new PlayTimeEdit();
        foreach (var (tracker, time) in times)
        {
            var before = current.GetValueOrDefault(tracker);
            var total = Clamp(before, time);

            _tracking.AddTimeToTracker(session, tracker, total - before);
            edit.Totals[tracker] = total;
            edit.Changes[tracker] = total - before;
        }

        _tracking.SaveSession(session);
        _tracking.QueueSendTimers(session);

        return edit;
    }

    private async Task<PlayTimeEdit> AddTimeOffline(NetUserId userId, IReadOnlyDictionary<string, TimeSpan> times)
    {
        var stored = await GetStoredTimes(userId);

        var edit = new PlayTimeEdit();
        foreach (var (tracker, time) in times)
        {
            var before = stored.GetValueOrDefault(tracker);
            var total = Clamp(before, time);

            edit.Totals[tracker] = total;
            edit.Changes[tracker] = total - before;
        }

        await _db.UpdatePlayTimes(edit.Totals.Select(t => new PlayTimeUpdate(userId, t.Key, t.Value)).ToList());

        return edit;
    }

    private async Task<T> RunOffline<T>(NetUserId userId, Func<Task<T>> operation)
    {
        var task = RunAfter(_offline.GetValueOrDefault(userId), operation);
        _offline[userId] = task;

        try
        {
            return await task;
        }
        finally
        {
            if (_offline.TryGetValue(userId, out var last) && last == task)
                _offline.Remove(userId);
        }
    }

    private async Task<T> RunAfter<T>(Task? previous, Func<Task<T>> operation)
    {
        if (previous != null)
        {
            try
            {
                await previous;
            }
            catch
            {
                // The caller of the previous operation gets the error.
            }
        }

        // A disconnect saves the session in the background, read only after it lands.
        await _tracking.WaitPendingSaves();

        return await operation();
    }

    private async Task<Dictionary<string, TimeSpan>> GetStoredTimes(NetUserId userId)
    {
        var stored = await _db.GetPlayTimes(userId);
        return stored.ToDictionary(t => t.Tracker, t => t.TimeSpent);
    }

    private static TimeSpan Clamp(TimeSpan current, TimeSpan added)
    {
        var total = current + added;
        return total < TimeSpan.Zero ? TimeSpan.Zero : total;
    }
}

public sealed class PlayTimeEdit
{
    /// <summary>
    /// New totals of the changed trackers.
    /// </summary>
    public readonly Dictionary<string, TimeSpan> Totals = [];

    /// <summary>
    /// Time actually added per tracker, after clamping at zero.
    /// </summary>
    public readonly Dictionary<string, TimeSpan> Changes = [];
}
