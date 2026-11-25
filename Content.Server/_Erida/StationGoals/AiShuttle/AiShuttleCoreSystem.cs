// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Erida.Silicons.StationAi;
using Content.Shared._Erida.StationGoals;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Shuttles.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.UserInterface;
using Robust.Shared.Timing;

namespace Content.Server._Erida.StationGoals.AiShuttle;

/// <summary>
/// AI shuttle cores: piloting the shuttle by the station AI eye and marking the core grid with <see cref="AiRemoteGridComponent"/>.
/// </summary>
public sealed class AiShuttleCoreSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private AiRemoteGridSystem _remoteGrid = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ShuttleConsoleSystem _shuttleConsole = default!;
    [Dependency] private SharedStationAiSystem _stationAi = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly TimeSpan AppearanceInterval = TimeSpan.FromSeconds(0.5);

    private TimeSpan _nextAppearance;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AiShuttleCoreComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AiShuttleCoreComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<AiShuttleCoreComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<AiShuttleCoreComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AiShuttleCoreComponent, AfterActivatableUIOpenEvent>(OnUiOpened);
        SubscribeLocalEvent<FTLStartedEvent>(OnFtlStarted);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    /// <summary>
    /// Pilots leave without an event, so the controlled light is refreshed by polling the few cores.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextAppearance)
            return;

        _nextAppearance = _timing.CurTime + AppearanceInterval;

        var query = EntityQueryEnumerator<AiShuttleCoreComponent, ShuttleConsoleComponent>();
        while (query.MoveNext(out var uid, out _, out var console))
        {
            var controlled = false;
            foreach (var pilot in console.SubscribedPilots)
            {
                if (HasComp<StationAiHeldComponent>(pilot))
                {
                    controlled = true;
                    break;
                }
            }

            _appearance.SetData(uid, AiShuttleCoreVisuals.Controlled, controlled);
        }
    }

    #region Component events

    private void OnMapInit(Entity<AiShuttleCoreComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Station = _station.GetOwningStation(ent);
        SetActive(ent, Transform(ent).Anchored && this.IsPowered(ent, EntityManager));
    }

    private void OnPowerChanged(Entity<AiShuttleCoreComponent> ent, ref PowerChangedEvent args)
    {
        SetActive(ent, args.Powered && Transform(ent).Anchored);
    }

    private void OnAnchorChanged(Entity<AiShuttleCoreComponent> ent, ref AnchorStateChangedEvent args)
    {
        SetActive(ent, args.Anchored && this.IsPowered(ent, EntityManager));
    }

    private void OnShutdown(Entity<AiShuttleCoreComponent> ent, ref ComponentShutdown args)
    {
        SetActive(ent, false);
    }

    private void OnUiOpened(Entity<AiShuttleCoreComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        if (!HasComp<StationAiHeldComponent>(args.User)
            || !TryComp<ShuttleConsoleComponent>(ent, out var console))
            return;

        if (!ent.Comp.Active)
        {
            _popup.PopupEntity(Loc.GetString("ai-shuttle-core-popup-unavailable"), ent, args.User);
            return;
        }

        if (TryComp<PilotComponent>(args.User, out var pilot))
        {
            if (pilot.Console == ent.Owner)
                return;

            _shuttleConsole.RemovePilot(args.User, pilot);
        }

        EnsureComp<PilotComponent>(args.User);
        _shuttleConsole.AddPilot(ent, args.User, console);
        _appearance.SetData(ent, AiShuttleCoreVisuals.Controlled, true);
    }

    #endregion

    #region Handle events

    /// <summary>
    /// The AI eye has <see cref="NoFTLComponent"/> and is left behind, put the eye of a piloting AI back on the core.
    /// </summary>
    private void OnFtlStarted(ref FTLStartedEvent args)
    {
        var query = EntityQueryEnumerator<AiShuttleCoreComponent, ShuttleConsoleComponent, TransformComponent>();
        while (query.MoveNext(out _, out _, out var console, out var xform))
        {
            if (xform.GridUid != args.Entity)
                continue;

            foreach (var pilot in console.SubscribedPilots)
            {
                if (!_stationAi.TryGetCore(pilot, out var aiCore)
                    || aiCore.Comp?.RemoteEntity is not { } eye)
                    continue;

                _transform.SetCoordinates(eye, xform.Coordinates);
            }
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _nextAppearance = TimeSpan.Zero;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Updates the core state and the remote mark of its grid, the grid stays marked while any core on it is active.
    /// </summary>
    private void SetActive(Entity<AiShuttleCoreComponent> ent, bool active)
    {
        ent.Comp.Active = active;
        if (Transform(ent).GridUid is not { } grid || TerminatingOrDeleted(grid))
            return;

        var remote = active;
        var query = EntityQueryEnumerator<AiShuttleCoreComponent, TransformComponent>();
        while (!remote && query.MoveNext(out var uid, out var core, out var xform))
        {
            remote = uid != ent.Owner && core.Active && xform.GridUid == grid;
        }

        _remoteGrid.SetRemote(grid, remote);
    }

    #endregion
}
