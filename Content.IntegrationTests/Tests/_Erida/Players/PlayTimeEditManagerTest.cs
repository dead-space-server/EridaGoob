// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Pair;
using Content.Server._Erida.Players;
using Content.Server.Database;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests._Erida.Players;

[TestFixture]
[TestOf(typeof(PlayTimeEditManager))]
public sealed class PlayTimeEditManagerTest
{
    private static readonly string Overall = PlayTimeTrackingShared.TrackerOverall;
    private const string Job = "JobPassenger";

    [Test]
    public async Task AddsTimeOnlineAndOffline()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, Dirty = true });
        var server = pair.Server;

        var edit = server.ResolveDependency<PlayTimeEditManager>();
        var tracking = server.ResolveDependency<PlayTimeTrackingManager>();
        var players = server.ResolveDependency<IPlayerManager>();
        var db = server.ResolveDependency<IServerDbManager>();

        var session = players.Sessions.Single();
        var userId = session.UserId;

        Assert.Multiple(() =>
        {
            Assert.That(edit.IsValidTracker(Overall));
            Assert.That(edit.IsValidTracker(Job));
            Assert.That(edit.IsValidTracker("NotATracker"), Is.False);
        });

        // Online: goes through the tracker, so the in-memory value changes too.
        var before = TimeSpan.Zero;
        await server.WaitPost(() => before = tracking.GetPlayTimeForTracker(session, Job));

        var online = await Run(pair, () => edit.AddTime(userId, new Dictionary<string, TimeSpan>
        {
            [Job] = TimeSpan.FromHours(2),
        }));

        Assert.That(online, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(online!.Totals[Job], Is.EqualTo(before + TimeSpan.FromHours(2)));
            Assert.That(online.Changes[Job], Is.EqualTo(TimeSpan.FromHours(2)));
        });
        await server.WaitPost(() =>
            Assert.That(tracking.GetPlayTimeForTracker(session, Job), Is.EqualTo(before + TimeSpan.FromHours(2))));

        // Disconnect saves the session in the background, an offline read waits for that save.
        var clientNet = pair.Client.ResolveDependency<IClientNetManager>();
        await pair.Client.WaitPost(() => clientNet.ClientDisconnect("test"));
        await PoolManager.WaitUntil(server, () => !players.TryGetSessionById(userId, out _));

        var afterDisconnect = await Run(pair, () => edit.GetTimes(userId));
        var stored = afterDisconnect![Job];
        var overall = afterDisconnect.GetValueOrDefault(Overall);
        Assert.That(stored, Is.GreaterThanOrEqualTo(before + TimeSpan.FromHours(2)));

        // Offline: two edits at once both land, and removal clamps at zero.
        Task<PlayTimeEdit?> firstTask = null!;
        Task<PlayTimeEdit?> secondTask = null!;
        await server.WaitPost(() =>
        {
            firstTask = edit.AddTime(userId, new Dictionary<string, TimeSpan> { [Job] = TimeSpan.FromMinutes(30) });
            secondTask = edit.AddTime(userId, new Dictionary<string, TimeSpan>
            {
                [Job] = TimeSpan.FromMinutes(30),
                [Overall] = -overall - TimeSpan.FromHours(1),
            });
        });
        await PoolManager.WaitUntil(server, () => firstTask.IsCompleted && secondTask.IsCompleted);
        var first = await firstTask;
        var second = await secondTask;

        Assert.Multiple(() =>
        {
            Assert.That(first!.Totals[Job], Is.EqualTo(stored + TimeSpan.FromMinutes(30)));
            Assert.That(second!.Totals[Job], Is.EqualTo(stored + TimeSpan.FromHours(1)));
            Assert.That(second.Totals[Overall], Is.EqualTo(TimeSpan.Zero));
            Assert.That(second.Changes[Overall], Is.EqualTo(-overall));
        });
        Assert.That(await StoredTime(db, userId, Job), Is.EqualTo(stored + TimeSpan.FromHours(1)));

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Starts the task on the server main thread, as the API does, and ticks until it finishes.
    /// </summary>
    private static async Task<T> Run<T>(TestPair pair, Func<Task<T>> start)
    {
        Task<T> task = null!;
        await pair.Server.WaitPost(() => task = start());
        await PoolManager.WaitUntil(pair.Server, () => task.IsCompleted);
        return await task;
    }

    private static async Task<TimeSpan> StoredTime(IServerDbManager db, NetUserId userId, string tracker)
    {
        var times = await db.GetPlayTimes(userId.UserId);
        return times.FirstOrDefault(t => t.Tracker == tracker)?.TimeSpent ?? TimeSpan.Zero;
    }
}
