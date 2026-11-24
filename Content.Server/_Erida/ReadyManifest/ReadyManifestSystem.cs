// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Preferences.Managers;
using Content.Shared._Erida.ReadyManifest;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Erida.ReadyManifest;

/// <summary>
/// Shows lobby players which jobs the ready players want: every high priority job, and any wanted head job.
/// Character names are shown to admins only.
/// </summary>
public sealed class ReadyManifestSystem : EntitySystem
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private EuiManager _eui = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IServerPreferencesManager _preferences = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    /// <summary>
    /// Jobs with at least this weight are listed at any priority, so heads show up even when not high.
    /// </summary>
    private const int HeadJobWeight = 10;

    private readonly Dictionary<ICommonSession, ReadyManifestEui> _openEuis = [];
    private ReadyManifestEuiState? _state;
    private ReadyManifestEuiState? _adminState;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestReadyManifestEvent>(OnRequest);
        SubscribeLocalEvent<PlayerToggleReadyEvent>(OnPlayerToggleReady);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        Subs.CVar(_cfg, CCVars.GameLobbyEnabled, _ => InvalidateState());

        _preferences.PreferencesChanged += OnPreferencesChanged;
        _player.PlayerStatusChanged += OnPlayerStatusChanged;
        _admin.OnPermsChanged += OnPermsChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _preferences.PreferencesChanged -= OnPreferencesChanged;
        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
        _admin.OnPermsChanged -= OnPermsChanged;
    }

    private void OnRequest(RequestReadyManifestEvent ev, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;

        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby
            || !_cfg.GetCVar(CCVars.CrewManifestWithoutEntity)
            || _openEuis.ContainsKey(session))
        {
            return;
        }

        var eui = new ReadyManifestEui(this);
        _openEuis.Add(session, eui);
        _eui.OpenEui(eui, session);
        eui.StateDirty();
    }

    private void OnPlayerToggleReady(PlayerToggleReadyEvent ev)
    {
        InvalidateState();
    }

    /// <summary>
    /// Joining the lobby resets the ready status without raising <see cref="PlayerToggleReadyEvent"/>.
    /// </summary>
    private void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        InvalidateState();
    }

    private void OnPreferencesChanged(NetUserId userId)
    {
        if (IsReady(userId))
            InvalidateState();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Disconnected && IsReady(args.Session.UserId))
            InvalidateState();
    }

    /// <summary>
    /// Character names depend on the admin status, so an open manifest gets the other state after deadmin or readmin.
    /// </summary>
    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (_openEuis.TryGetValue(args.Player, out var eui))
            eui.StateDirty();
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        CloseAll();
    }

    /// <summary>
    /// A restart from the lobby resets ready statuses without raising <see cref="PlayerToggleReadyEvent"/>.
    /// </summary>
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        CloseAll();
    }

    private void CloseAll()
    {
        foreach (var eui in new List<ReadyManifestEui>(_openEuis.Values))
        {
            eui.Close();
        }

        _openEuis.Clear();
        _state = null;
        _adminState = null;
    }

    private bool IsReady(NetUserId userId)
    {
        return _gameTicker.PlayerGameStatuses.TryGetValue(userId, out var status)
            && status == PlayerGameStatus.ReadyToPlay;
    }

    private void InvalidateState()
    {
        _state = null;
        _adminState = null;

        foreach (var eui in _openEuis.Values)
        {
            eui.StateDirty();
        }
    }

    public void OnEuiClosed(ICommonSession session)
    {
        _openEuis.Remove(session);
    }

    /// <summary>
    /// State shared by every open manifest of non-admins, and another one with character names for admins.
    /// </summary>
    public ReadyManifestEuiState GetState(ICommonSession session)
    {
        if (_admin.IsAdmin(session))
            return _adminState ??= BuildState(true);

        return _state ??= BuildState(false);
    }

    private ReadyManifestEuiState BuildState(bool withNames)
    {
        List<ReadyManifestEntry> entries = [];
        var readyCount = 0;

        foreach (var session in _player.Sessions)
        {
            if (!IsReady(session.UserId))
                continue;

            readyCount++;

            if (!_preferences.TryGetCachedPreferences(session.UserId, out var preferences)
                || preferences.SelectedCharacter is not HumanoidCharacterProfile profile)
            {
                continue;
            }

            foreach (var (jobId, priority) in profile.JobPriorities)
            {
                if (priority == JobPriority.Never || !_prototypes.TryIndex(jobId, out var job))
                    continue;

                if (priority != JobPriority.High && job.Weight < HeadJobWeight)
                    continue;

                entries.Add(new ReadyManifestEntry(jobId, withNames ? profile.Name : null));
            }
        }

        return new ReadyManifestEuiState(entries, readyCount);
    }
}
