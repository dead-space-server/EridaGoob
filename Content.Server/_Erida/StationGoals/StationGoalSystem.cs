// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Logs;
using Content.Server.Fax;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared._Erida.StationGoals;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Systems;
using Content.Shared.Paper;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Erida.StationGoals;

/// <summary>
/// Gives every station with <see cref="StationGoalComponent"/> one goal <see cref="CCVars.StationGoalDelay"/>
/// after round start, picked by its alive crew, and prints its result in the round end summary.
/// </summary>
public sealed class StationGoalSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private FaxSystem _fax = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private StationJobsSystem _stationJobs = default!;
    [Dependency] private IGameTiming _timing = default!;

    private bool _enabled;
    private TimeSpan _delay;
    private TimeSpan? _assignTime;
    private readonly List<EntProtoId> _goals = [];

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.StationGoal, value => _enabled = value, true);
        _cfg.OnValueChanged(CCVars.StationGoalDelay, value => _delay = TimeSpan.FromSeconds(value), true);

        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        CacheGoals();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_assignTime is not { } time
            || _timing.CurTime < time)
            return;

        _assignTime = null;
        AssignGoals();
    }

    #region Handle events

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        if (_enabled)
            _assignTime = _timing.CurTime + _delay;
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _assignTime = null;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            CacheGoals();
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        List<StationGoalResult> results = [];
        var query = EntityQueryEnumerator<StationGoalComponent>();
        while (query.MoveNext(out var uid, out var station))
        {
            if (!TryComp<StationGoalInfoComponent>(station.Goal, out var goal))
                continue;

            results.Add(new StationGoalResult
            {
                Station = MetaData(uid).EntityName,
                Goal = goal.Name,
                Progress = GetProgress((station.Goal.Value, goal)),
            });
        }

        RaiseNetworkEvent(new StationGoalResultsEvent(results));

        if (results.Count == 0)
            return;

        ev.AddLine(Loc.GetString("station-goal-round-end-header", ("count", results.Count)));
        foreach (var result in results)
        {
            ev.AddLine(result.Format(results.Count > 1));
        }
        ev.AddLine(string.Empty);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Non-abstract goal prototypes.
    /// </summary>
    public IReadOnlyList<EntProtoId> Goals => _goals;

    /// <summary>
    /// Replaces the station goal and sends it to the faxes.
    /// </summary>
    /// <param name="faxes">Faxes of the station that received the goal.</param>
    /// <param name="admin">Admin who assigned the goal, null for the automatic pick.</param>
    /// <returns>False if the station or the goal is invalid.</returns>
    public bool TryAssignGoal(Entity<StationGoalComponent?> station, EntProtoId goalId, out int faxes, ICommonSession? admin = null)
    {
        faxes = 0;
        if (!Resolve(station, ref station.Comp, false)
            || !_goals.Contains(goalId))
            return false;

        if (station.Comp.Goal is { } old)
            QueueDel(old);

        var goalUid = Spawn(goalId, MapCoordinates.Nullspace);
        var goal = Comp<StationGoalInfoComponent>(goalUid);
        goal.Station = station;
        station.Comp.Goal = goalUid;

        var ev = new StationGoalAssignedEvent(station);
        RaiseLocalEvent(goalUid, ref ev);

        if (admin == null)
            _adminLog.Add(LogType.EventStarted, LogImpact.Low,
                $"Station goal {goalId} was assigned to {ToPrettyString(station):station}");
        else
            LogCommand(admin, station, $"assigned goal {goalId}");

        faxes = SendFax((station, station.Comp), (goalUid, goal), true);
        return true;
    }

    /// <summary>
    /// Removes the station goal.
    /// </summary>
    public bool TryClearGoal(Entity<StationGoalComponent?> station, ICommonSession? admin = null)
    {
        if (!Resolve(station, ref station.Comp, false)
            || station.Comp.Goal is not { } goal)
            return false;

        station.Comp.Goal = null;
        QueueDel(goal);
        LogCommand(admin, station, "cleared the goal");
        return true;
    }

    /// <summary>
    /// Sends the current goal to the faxes again, without <see cref="StationGoalInfoComponent.Spawns"/>.
    /// </summary>
    /// <param name="faxes">Faxes of the station that received the goal.</param>
    public bool TryResendGoal(Entity<StationGoalComponent?> station, out int faxes, ICommonSession? admin = null)
    {
        faxes = 0;
        if (!Resolve(station, ref station.Comp, false)
            || !TryGetGoal(station, out var goal))
            return false;

        faxes = SendFax((station, station.Comp), goal.Value, false);
        LogCommand(admin, station, "resent the goal");
        return true;
    }

    /// <summary>
    /// Sets or clears the admin override of the station goal result.
    /// </summary>
    public bool TrySetForceCompleted(Entity<StationGoalComponent?> station, bool? completed, ICommonSession? admin = null)
    {
        if (!TryGetGoal(station, out var goal))
            return false;

        goal.Value.Comp.ForceCompleted = completed;
        LogCommand(admin, station, $"set the goal result to {completed?.ToString() ?? "auto"}");
        return true;
    }

    public bool TryGetGoal(Entity<StationGoalComponent?> station, [NotNullWhen(true)] out Entity<StationGoalInfoComponent>? goal)
    {
        goal = null;
        if (!Resolve(station, ref station.Comp, false)
            || station.Comp.Goal is not { } uid
            || !TryComp<StationGoalInfoComponent>(uid, out var info))
            return false;

        goal = (uid, info);
        return true;
    }

    /// <summary>
    /// Current progress from 0 to 1, or null for a goal without conditions and admin override.
    /// </summary>
    public float? GetProgress(Entity<StationGoalInfoComponent> goal)
    {
        if (goal.Comp.ForceCompleted is { } forced)
            return forced ? 1f : 0f;

        if (goal.Comp.Station is not { } station)
            return null;

        var ev = new StationGoalGetProgressEvent(station);
        RaiseLocalEvent(goal, ref ev);
        return ev.Progress;
    }

    /// <summary>
    /// Random goal that fits the station whitelist, blacklist and its alive crew.
    /// </summary>
    public bool TryPickGoal(Entity<StationGoalComponent> station, [NotNullWhen(true)] out EntProtoId? goal)
    {
        goal = null;
        var crew = CountCrew(station, out var departments);
        Dictionary<EntProtoId, float> weights = [];

        foreach (var id in _goals)
        {
            if (!_proto.Index(id).TryComp<StationGoalInfoComponent>(out var info, _factory))
                continue;

            var whitelisted = station.Comp.Whitelist.Contains(id);
            if (station.Comp.Whitelist.Count > 0 && !whitelisted
                || info.WhitelistOnly && !whitelisted
                || station.Comp.Blacklist.Contains(id)
                || crew < info.MinPlayers
                || crew > info.MaxPlayers
                || info.Weight <= 0f
                || !HasDepartments(info, departments))
                continue;

            weights[id] = info.Weight;
        }

        if (weights.Count == 0)
            return false;

        goal = _random.Pick(weights);
        return true;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Gives a goal to every station that has none yet, so a goal assigned by an admin is kept.
    /// </summary>
    private void AssignGoals()
    {
        var query = EntityQueryEnumerator<StationGoalComponent>();
        while (query.MoveNext(out var uid, out var station))
        {
            if (station.Goal != null)
                continue;

            if (!TryPickGoal((uid, station), out var goal))
            {
                Log.Warning($"No station goal fits station {ToPrettyString(uid)}");
                continue;
            }

            TryAssignGoal((uid, station), goal.Value, out _);
        }
    }

    /// <summary>
    /// Counts alive in-game players who took a job on the station, total and per department.
    /// </summary>
    private int CountCrew(EntityUid station, out Dictionary<ProtoId<DepartmentPrototype>, int> departments)
    {
        departments = [];
        var crew = 0;
        foreach (var session in _player.Sessions)
        {
            if (session.Status != SessionStatus.InGame
                || session.AttachedEntity is not { } mob
                || !_mobState.IsAlive(mob)
                || !_stationJobs.TryGetPlayerJobs(station, session.UserId, out var jobs))
                continue;

            crew++;
            foreach (var department in _proto.EnumeratePrototypes<DepartmentPrototype>())
            {
                foreach (var job in jobs)
                {
                    if (!department.Roles.Contains(job))
                        continue;

                    departments[department.ID] = departments.GetValueOrDefault(department.ID) + 1;
                    break;
                }
            }
        }

        return crew;
    }

    private static bool HasDepartments(StationGoalInfoComponent info, Dictionary<ProtoId<DepartmentPrototype>, int> departments)
    {
        foreach (var (department, count) in info.Departments)
        {
            if (departments.GetValueOrDefault(department) < count)
                return false;
        }

        return true;
    }

    private void LogCommand(ICommonSession? admin, EntityUid station, string action)
    {
        if (admin == null)
            _adminLog.Add(LogType.AdminCommands, LogImpact.Medium,
                $"Server console {action} for station {ToPrettyString(station):station}");
        else
            _adminLog.Add(LogType.AdminCommands, LogImpact.Medium,
                $"{admin:player} {action} for station {ToPrettyString(station):station}");
    }

    private void CacheGoals()
    {
        _goals.Clear();
        foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            if (!proto.Abstract && proto.HasComp<StationGoalInfoComponent>(_factory))
                _goals.Add(proto.ID);
        }
    }

    /// <returns>Number of faxes of the station that received the goal.</returns>
    private int SendFax(Entity<StationGoalComponent> station, Entity<StationGoalInfoComponent> goal, bool spawn)
    {
        var printout = new FaxPrintout(
            Loc.GetString(goal.Comp.Text, ("station", MetaData(station).EntityName)),
            Loc.GetString("station-goal-fax-paper-name"),
            null,
            null,
            "paper_stamp-centcom",
            [new StampDisplayInfo { StampedName = "stamp-component-stamped-name-centcom", StampedColor = Color.FromHex("#006600") }]);

        var sent = 0;
        var query = EntityQueryEnumerator<FaxMachineComponent, TransformComponent>();
        while (query.MoveNext(out var faxUid, out var fax, out var xform))
        {
            var own = fax.ReceiveStationGoal && _station.GetOwningStation(faxUid, xform) == station.Owner;
            if (!own && !fax.ReceiveAllStationGoals)
                continue;

            _fax.Receive(faxUid, printout, null, fax);

            if (!own)
                continue;

            sent++;
            if (!spawn)
                continue;

            foreach (var spawnId in goal.Comp.Spawns)
            {
                SpawnAtPosition(spawnId, xform.Coordinates);
            }
        }

        return sent;
    }

    #endregion
}
