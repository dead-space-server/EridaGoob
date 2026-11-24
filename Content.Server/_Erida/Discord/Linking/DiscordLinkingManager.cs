// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.Tasks;
using Content.Server._Erida.Administration;
using Content.Shared._Erida.Discord.Linking;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server._Erida.Discord.Linking;

public sealed partial class DiscordLinkingManager : IPostInjectInit
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EridaServerApi _serverApi = default!;

    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(3);

    /// <summary>
    /// How long a failed answer of the bot is reused, so pressing the button again doesn't query the bot each time.
    /// </summary>
    private static readonly TimeSpan FailureLifetime = TimeSpan.FromSeconds(15);

    private readonly Dictionary<NetUserId, (DiscordLinkingCodeStatus Status, Guid Code, TimeSpan Expires)> _codes = [];
    private readonly Dictionary<NetUserId, Task<(DiscordLinkingCodeStatus Status, Guid Code)>> _pending = [];

    void IPostInjectInit.PostInject()
    {
        _net.RegisterNetMessage<DiscordLinkingRequestMsg>(OnRequest);
        _net.RegisterNetMessage<DiscordLinkingCodeMsg>();
    }

    private async void OnRequest(DiscordLinkingRequestMsg message)
    {
        var channel = message.MsgChannel;
        var (status, code) = await GetOrCreateCode(channel.UserId);

        if (channel.IsConnected)
            _net.ServerSendMessage(new DiscordLinkingCodeMsg { Status = status, Code = code }, channel);
    }

    /// <summary>
    /// Returns the player's unexpired code or failure, or registers a new code with the bot. Requests made while
    /// the bot is answering share the same task.
    /// </summary>
    public Task<(DiscordLinkingCodeStatus Status, Guid Code)> GetOrCreateCode(NetUserId user)
    {
        if (_codes.TryGetValue(user, out var cached) && cached.Expires > _timing.RealTime)
            return Task.FromResult((cached.Status, cached.Code));

        if (_pending.TryGetValue(user, out var pending))
            return pending;

        RemoveExpiredCodes();

        var task = CreateCode(user);
        if (!task.IsCompleted)
            _pending[user] = task;

        return task;
    }

    private async Task<(DiscordLinkingCodeStatus Status, Guid Code)> CreateCode(NetUserId user)
    {
        try
        {
            var code = Guid.NewGuid();
            var status = await _serverApi.RegisterLinkingCode(user.UserId, code);

            if (status != DiscordLinkingCodeStatus.Ok)
                code = default;

            var lifetime = status == DiscordLinkingCodeStatus.Ok ? CodeLifetime : FailureLifetime;
            _codes[user] = (status, code, _timing.RealTime + lifetime);
            return (status, code);
        }
        finally
        {
            _pending.Remove(user);
        }
    }

    /// <summary>
    /// Drops codes and failures that have expired, so players who never come back don't stay in memory.
    /// </summary>
    private void RemoveExpiredCodes()
    {
        var now = _timing.RealTime;
        foreach (var (user, entry) in _codes)
        {
            if (entry.Expires <= now)
                _codes.Remove(user);
        }
    }
}
