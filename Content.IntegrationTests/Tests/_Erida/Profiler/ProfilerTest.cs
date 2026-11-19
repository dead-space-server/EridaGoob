// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Content.Server._Erida.Profiler;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._Erida.CCVar;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Profiling;

namespace Content.IntegrationTests.Tests._Erida.Profiler;

/// <summary>
/// The host profiler must stay closed to everyone but hosts, and must record frames over the threshold.
/// </summary>
[TestFixture, TestOf(typeof(ProfilerSystem))]
public sealed class ProfilerTest
{
    private const string SlowEntry = "EridaProfilerTestSlowEntry";

    [Test]
    public async Task NonHostCannotOpen()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, Dirty = true });
        var server = pair.Server;

        var players = server.ResolveDependency<IPlayerManager>();
        var admins = server.ResolveDependency<IAdminManager>();
        var euis = server.ResolveDependency<EuiManager>();
        var profiler = server.System<ProfilerSystem>();
        List<string> errors = [];

        var attribute = typeof(ProfilerCommand).GetCustomAttribute<AdminCommandAttribute>();
        if (attribute?.Flags != AdminFlags.Host)
            errors.Add($"{nameof(ProfilerCommand)} must require {AdminFlags.Host}, got {attribute?.Flags}");

        await server.WaitAssertion(() =>
        {
            var session = players.Sessions.Single();
            if (admins.IsAdmin(session))
                admins.DeAdmin(session);

            var eui = new ProfilerEui();
            euis.OpenEui(eui, session);

            if (!eui.IsShutDown)
                errors.Add("Profiler window stayed open for a non-admin player");

            if (profiler.SubscriberCount != 0)
                errors.Add($"Non-admin player was subscribed to the profiler ({profiler.SubscriberCount} subscribers)");
        });

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SlowFrameIsRecorded()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var server = pair.Server;

        var cfg = server.ResolveDependency<IConfigurationManager>();
        var prof = server.ResolveDependency<ProfManager>();
        var profiler = server.System<ProfilerSystem>();
        List<string> errors = [];

        await server.WaitPost(() =>
        {
            cfg.SetCVar(ECCVars.ProfilerSlowTickMs, 20f);
        });
        await server.WaitRunTicks(1);

        // The test game loop does not write frames, so write one the way the engine game loop does.
        await server.WaitPost(() =>
        {
            if (!prof.IsEnabled)
                errors.Add("Profiler system did not enable the engine profiler");

            var frameStart = prof.WriteValue("Start Frame", ProfData.Int64(0));
            var frameGroup = prof.WriteGroupStart();
            var frameSampler = ProfSampler.StartNew();

            using (prof.Group("Ticks"))
            using (prof.Group("Tick"))
            {
                prof.WriteValue("Tick", ProfData.Int64(4242));
                using (prof.Value(SlowEntry))
                {
                    Thread.Sleep(60);
                }
            }

            prof.WriteGroupEnd(frameGroup, "Frame", frameSampler);
            prof.MarkIndex(frameStart, ProfIndexType.Frame);
        });
        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            var slowTicks = profiler.GetState().SlowTicks;
            var recorded = slowTicks.Where(t => t.Tick == 4242).ToList();

            if (recorded.Count != 1)
            {
                errors.Add($"Expected one slow tick #4242, got {recorded.Count} of {slowTicks.Count}");
                return;
            }

            if (recorded[0].FrameMs < 50f)
                errors.Add($"Slow tick time is {recorded[0].FrameMs} ms, expected at least 50 ms");

            if (!recorded[0].Nodes.Any(n => n.Name == SlowEntry && n.TimeMs >= 50f))
                errors.Add($"Slow tick tree has no {SlowEntry} node");
        });

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }
}
