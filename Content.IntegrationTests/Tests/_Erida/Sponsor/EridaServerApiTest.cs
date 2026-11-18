// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable

using Content.IntegrationTests.Pair;
using Content.Server._Erida.Administration;
using Content.Server._Erida.Discord.Linking;
using Content.Server._Erida.Sponsor;
using Content.Shared._Erida.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests._Erida.Sponsor;

[TestFixture]
[TestOf(typeof(EridaServerApi))]
public sealed class EridaServerApiTest
{
    private static PoolSettings Settings => new() { Dirty = true };

    private static async Task Configure(TestPair pair, FakeApiBot bot, float healthCheckInterval = 0f)
    {
        var cfg = pair.Server.ResolveDependency<IConfigurationManager>();
        await pair.Server.WaitPost(() =>
        {
            cfg.SetCVar(ECCVars.ApiHealthCheckInterval, healthCheckInterval);
            cfg.SetCVar(ECCVars.ApiToken, "test-key");
            cfg.SetCVar(ECCVars.ApiUrl, bot.Url);
        });
    }

    private static async Task Finish(TestPair pair)
    {
        var cfg = pair.Server.ResolveDependency<IConfigurationManager>();
        await pair.Server.WaitPost(() =>
        {
            cfg.SetCVar(ECCVars.ApiToken, string.Empty);
            cfg.SetCVar(ECCVars.ApiUrl, string.Empty);
            cfg.SetCVar(ECCVars.ApiHealthCheckInterval, ECCVars.ApiHealthCheckInterval.DefaultValue);
        });
        await pair.CleanReturnAsync();
    }

    private static async Task WaitFor(TestPair pair, Func<bool> condition, string what)
    {
        for (var i = 0; i < 400; i++)
        {
            if (condition())
                return;

            await pair.RunTicksSync(3);
            await Task.Delay(10);
        }

        Assert.Fail($"Timed out waiting for {what}");
    }

    private static async Task<T> Call<T>(TestPair pair, Func<Task<T>> call)
    {
        Task<T>? task = null;
        await pair.Server.WaitPost(() => task = call());
        await WaitFor(pair, () => task is { IsCompleted: true }, "the API call");
        return await task!;
    }

    [Test]
    public async Task RecoversWhenBotWasDownAtStart()
    {
        using var bot = new FakeApiBot(start: false);
        await using var pair = await PoolManager.GetServerClient(Settings);
        var api = pair.Server.ResolveDependency<EridaServerApi>();
        var user = Guid.NewGuid();

        bot.SponsorRank = 3;
        bot.Linked = true;

        await Configure(pair, bot);

        Assert.That(await Call(pair, () => api.GetSponsorUserDataOrNull(user)), Is.Null);
        Assert.That(await Call(pair, () => api.GetLinkedUserDataOrNull(user)), Is.Null);

        bot.Start();

        var sponsor = await Call(pair, () => api.GetSponsorUserDataOrNull(user));
        Assert.That(sponsor?.Rank, Is.EqualTo(3), "sponsor status stayed lost after the bot came back");

        var link = await Call(pair, () => api.GetLinkedUserDataOrNull(user));
        Assert.That(link?.Linked, Is.True, "link status stayed lost after the bot came back");

        await Finish(pair);
    }

    [Test]
    public async Task BotIsNotCheckedAgainDuringInterval()
    {
        using var bot = new FakeApiBot(start: false);
        await using var pair = await PoolManager.GetServerClient(Settings);
        var api = pair.Server.ResolveDependency<EridaServerApi>();
        var user = Guid.NewGuid();

        bot.SponsorRank = 3;

        await Configure(pair, bot, healthCheckInterval: 3600f);

        Assert.That(await Call(pair, () => api.GetSponsorUserDataOrNull(user)), Is.Null);

        bot.Start();

        Assert.That(await Call(pair, () => api.GetSponsorUserDataOrNull(user)), Is.Null);
        Assert.That(bot.LinkRequestsStarted, Is.Zero, "the health check ran again before the interval ended");
        Assert.That(bot.SponsorRequestsStarted, Is.Zero);

        await Finish(pair);
    }

    [Test]
    public async Task DisconnectDuringRequestDoesNotCacheSponsor()
    {
        using var bot = new FakeApiBot();
        await using var pair = await PoolManager.GetServerClient(Settings);
        var server = pair.Server;
        var api = server.ResolveDependency<EridaServerApi>();
        var sponsors = server.System<SponsorManagerSystem>();

        bot.SponsorRank = 3;
        await Configure(pair, bot);

        // Makes the API active, so the request below really reaches the bot.
        await Call(pair, () => api.GetLinkedUserDataOrNull(Guid.NewGuid()));

        bot.SponsorDelay = TimeSpan.FromSeconds(2);
        var started = bot.SponsorRequestsStarted;
        var answered = bot.SponsorRequestsAnswered;

        var dummy = await server.AddDummySession();
        await WaitFor(pair, () => bot.SponsorRequestsStarted > started, "the sponsor request");

        // Lets the rest of the server finish connecting the dummy, as it happens for a real player.
        await pair.RunTicksSync(30);

        Assert.That(bot.SponsorRequestsAnswered, Is.EqualTo(answered), "the request must still be running");
        await server.RemoveDummySession(dummy);
        await WaitFor(pair, () => bot.SponsorRequestsAnswered > answered, "the sponsor answer");
        await pair.RunTicksSync(30);
        await Task.Delay(100);
        await pair.RunTicksSync(30);

        Assert.That(sponsors.IsSponsor(dummy.UserId), Is.False, "the cache kept a player that already left");

        await Finish(pair);
    }

    [Test]
    public async Task DisconnectDuringRequestDoesNotCacheLink()
    {
        using var bot = new FakeApiBot();
        await using var pair = await PoolManager.GetServerClient(Settings);
        var server = pair.Server;
        var api = server.ResolveDependency<EridaServerApi>();
        var links = server.System<DiscordLinkingSystem>();

        bot.Linked = true;
        await Configure(pair, bot);

        // Makes the API active, so the request below really reaches the bot.
        await Call(pair, () => api.GetLinkedUserDataOrNull(Guid.NewGuid()));

        bot.LinkDelay = TimeSpan.FromSeconds(2);
        var started = bot.LinkRequestsStarted;
        var answered = bot.LinkRequestsAnswered;

        var dummy = await server.AddDummySession();
        await WaitFor(pair, () => bot.LinkRequestsStarted > started, "the link request");

        // Lets the rest of the server finish connecting the dummy, as it happens for a real player.
        await pair.RunTicksSync(30);

        Assert.That(bot.LinkRequestsAnswered, Is.EqualTo(answered), "the request must still be running");
        await server.RemoveDummySession(dummy);

        await WaitFor(pair, () => bot.LinkRequestsAnswered > answered, "the link answer");
        await pair.RunTicksSync(30);
        await Task.Delay(100);
        await pair.RunTicksSync(30);

        Assert.That(links.IsUserVerified(dummy.UserId), Is.False, "the cache kept a player that already left");

        bot.LinkDelay = TimeSpan.Zero;
        await Finish(pair);
    }

    [Test]
    public async Task ConnectedPlayerIsCachedAsLinked()
    {
        using var bot = new FakeApiBot();
        await using var pair = await PoolManager.GetServerClient(Settings);
        var server = pair.Server;
        var api = server.ResolveDependency<EridaServerApi>();
        var links = server.System<DiscordLinkingSystem>();

        bot.Linked = true;
        await Configure(pair, bot);
        await Call(pair, () => api.GetLinkedUserDataOrNull(Guid.NewGuid()));

        ICommonSession dummy = await server.AddDummySession();
        await WaitFor(pair, () => links.IsUserVerified(dummy.UserId), "the link status");

        await Finish(pair);
    }

    [Test]
    public async Task ConnectedPlayerIsCachedAsSponsor()
    {
        using var bot = new FakeApiBot();
        await using var pair = await PoolManager.GetServerClient(Settings);
        var server = pair.Server;
        var api = server.ResolveDependency<EridaServerApi>();
        var sponsors = server.System<SponsorManagerSystem>();

        bot.SponsorRank = 3;
        await Configure(pair, bot);
        await Call(pair, () => api.GetLinkedUserDataOrNull(Guid.NewGuid()));

        ICommonSession dummy = await server.AddDummySession();
        await WaitFor(pair, () => sponsors.IsSponsor(dummy.UserId), "the sponsor status");

        await Finish(pair);
    }
}
