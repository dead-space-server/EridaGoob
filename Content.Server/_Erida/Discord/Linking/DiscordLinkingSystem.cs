// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.Tasks;
using Content.Server._Erida.Administration;
using Content.Shared._Erida.Discord.Linking;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Erida.Discord.Linking;

public sealed class DiscordLinkingSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly EridaServerApi _eridaServerApi = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private Dictionary<NetUserId, LinkStatus> _cachedPlayers = new();
    private readonly Dictionary<NetUserId, TimeSpan> _lastCheck = new();
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromSeconds(10);

    public override void Initialize()
    {
        SubscribeNetworkEvent<VerifyStatusRequest>(OnVerifyStatusRequest);
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        base.Initialize();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private async void OnVerifyStatusRequest(VerifyStatusRequest msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;

        if (!_cachedPlayers.TryGetValue(session.UserId, out var status) || !status.Verified)
        {
            var now = _timing.RealTime;
            if (!_lastCheck.TryGetValue(session.UserId, out var last) || last + RecheckDelay <= now)
            {
                _lastCheck[session.UserId] = now;
                await RefreshPlayer(session);
                _cachedPlayers.TryGetValue(session.UserId, out status);
            }
        }

        if (IsConnected(session))
            RaiseNetworkEvent(new VerifyStatusResponse { Verified = status?.Verified ?? false }, session);
    }

    /// <summary>
    /// Retrieves the player linked user data from API and updates the local cache.
    /// </summary>
    /// <param name="session"></param>
    private async Task RefreshPlayer(ICommonSession session)
    {
        var status = await _eridaServerApi.GetLinkedUserDataOrNull(session.UserId);

        // The player may have left while the bot was answering, Disconnected has already cleared the cache.
        if (status != null && IsConnected(session))
            _cachedPlayers[session.UserId] = status;
    }

    private static bool IsConnected(ICommonSession session)
    {
        return session.Status is SessionStatus.Connected or SessionStatus.InGame;
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        switch (args.NewStatus)
        {
            case SessionStatus.Connected:
                await RefreshPlayer(args.Session);
                break;
            case SessionStatus.Disconnected:
                _cachedPlayers.Remove(args.Session.UserId);
                _lastCheck.Remove(args.Session.UserId);
                break;
        }
    }

    /// <summary>
    /// Forces the cached data for the specified player to be invalidated and refreshed.
    /// </summary>
    /// <param name="userId"></param>
    public void ForceUpdateUserData(NetUserId userId)
    {
        _cachedPlayers.Remove(userId);
        if (_playerManager.TryGetSessionById(userId, out var session))
            _ = RefreshPlayer(session);
    }

    /// <summary>
    /// If player was cached by system returns his LinkStatus
    /// </summary>
    /// <param name="userId"></param>
    /// <returns><see cref="LinkStatus"/></returns>
    private LinkStatus GetCachedUserInfo(NetUserId userId)
    {
        if (_cachedPlayers.ContainsKey(userId))
            return _cachedPlayers[userId];

        return new LinkStatus();
    }

    /// <summary>
    /// Retrieves linked user information from the server.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>
    /// The user's linked status, or an empty <see cref="LinkStatus"/> if no data is found.
    /// </returns>
    private async Task<LinkStatus> GetUserInfo(NetUserId userId)
    {
        return await _eridaServerApi.GetLinkedUserDataOrNull(userId)
            ?? new LinkStatus();
    }

    /// <summary>
    /// Determines whether the specified player has been verified.
    /// Checks only cached players.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>
    /// <c>true</c> if the user is verified otherwise <c>false</c>.
    /// </returns>
    public bool IsUserVerified(NetUserId userId)
    {
        return GetCachedUserInfo(userId).Verified;
    }

}
