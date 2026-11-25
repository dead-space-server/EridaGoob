// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Goobstation.Common.AlmanacBlade;
using Content.Server._Erida.StationGoals;
using Content.Server._Erida.StationGoals.AiShuttle;
using Content.Server._Erida.StationGoals.MeteorShield;
using Content.IntegrationTests.Pair;
using Content.Server.Construction;
using Content.Server.GameTicking;
using Content.Server.Lathe;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Systems;
using Content.Shared._Erida.Silicons.StationAi;
using Content.Shared._Erida.StationGoals;
using Content.Shared.Interaction;
using Content.Shared.Lathe;
using Content.Shared.Movement.Systems;
using Content.Shared.Research.Systems;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Station.Components;
using Content.Shared.StationAi;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.UnitTesting;
using StationGoalSummarySystem = Content.Client._Erida.StationGoals.StationGoalSummarySystem;

namespace Content.IntegrationTests.Tests._Erida;

/// <summary>
/// Station goals: prototypes, assignment, admin override, meteor shield, AI shuttle and mech delivery conditions.
/// </summary>
[TestFixture]
[TestOf(typeof(StationGoalSystem))]
[TestOf(typeof(MeteorShieldSystem))]
[TestOf(typeof(AiShuttleCoreSystem))]
public sealed class StationGoalTest
{
    private const string Satellite = "MeteorShieldSatellite";
    private const string Wall = "WallSolid";
    private const string Meteor = "MeteorSmall";
    private const string GoalA = "StationGoalKeepStation";
    private const string GoalB = "StationGoalMeteorShield";
    private const string GoalAiShuttle = "StationGoalAiShuttle";
    private const string GoalMechs = "StationGoalCombatMechs";
    private const string AiShuttleCore = "AiShuttleCore";
    private const string CombatMech = "MechGygax";
    private const string OtherMech = "MechRipley2";
    private const string MechLathe = "SecurityTechFab";
    private const string OtherLathe = "Autolathe";
    private const string PeripheralsRecipe = "GygaxPeripheralsElectronics";
    private const string CentralRecipe = "StationGoalGygaxCentralElectronics";
    private const string SatelliteProduct = "EngineeringMeteorShieldSatellite";
    private const int FarTile = 16;

    [Test]
    public async Task GoalsAreValid()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var loc = server.ResolveDependency<ILocalizationManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var goalSystem = entMan.System<StationGoalSystem>();
        List<string> errors = [];

        await server.WaitAssertion(() =>
        {
            var station = CreateStation(entMan, map.Grid);
            var goals = goalSystem.Goals;
            if (goals.Count == 0)
                errors.Add("No station goals found");

            foreach (var id in goals)
            {
                goalSystem.TryAssignGoal(station, id, out _);
                if (GetGoal(entMan, station) is not { } goal)
                {
                    errors.Add($"{id}: not assigned");
                    continue;
                }

                if (!loc.HasString(goal.Comp.Text))
                    errors.Add($"{id}: missing text {goal.Comp.Text}");
                else if (!loc.GetString(goal.Comp.Text, ("station", "test")).EndsWith(loc.GetString("station-goal-end")))
                    errors.Add($"{id}: text {goal.Comp.Text} is cut off, check the ftl for parse errors");

                if (!loc.HasString(goal.Comp.Name))
                    errors.Add($"{id}: missing name {goal.Comp.Name}");

                if (goal.Comp.Station != station)
                    errors.Add($"{id}: goal is not linked to the station");

                if (goalSystem.GetProgress(goal) is < 0f or > 1f)
                    errors.Add($"{id}: progress out of range");

                foreach (var department in goal.Comp.Departments.Keys)
                {
                    if (!protoMan.HasIndex(department))
                        errors.Add($"{id}: unknown department {department}");
                }
            }

            entMan.DeleteEntity(station);
        });

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ForceCompletedOverrides()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var goalSystem = entMan.System<StationGoalSystem>();
        List<string> errors = [];

        await server.WaitAssertion(() =>
        {
            var station = CreateStation(entMan, map.Grid);
            goalSystem.TryAssignGoal(station, GoalA, out _);
            var goal = GetGoal(entMan, station)!.Value;
            var auto = goalSystem.GetProgress(goal);

            goalSystem.TrySetForceCompleted(station, true);
            if (goalSystem.GetProgress(goal) != 1f)
                errors.Add("Forced completion does not give progress 1");

            goalSystem.TrySetForceCompleted(station, false);
            if (goalSystem.GetProgress(goal) != 0f)
                errors.Add("Forced failure does not give progress 0");

            goalSystem.TrySetForceCompleted(station, null);
            if (goalSystem.GetProgress(goal) != auto)
                errors.Add("Clearing the override does not restore automatic progress");

            entMan.DeleteEntity(station);
        });

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ReassignDeletesOldGoal()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var goalSystem = entMan.System<StationGoalSystem>();
        List<string> errors = [];
        EntityUid station = default;
        EntityUid oldGoal = default;

        await server.WaitPost(() =>
        {
            station = CreateStation(entMan, map.Grid);
            goalSystem.TryAssignGoal(station, GoalA, out _);
            oldGoal = GetGoal(entMan, station)!.Value;
            goalSystem.TryAssignGoal(station, GoalB, out _);
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            if (!entMan.Deleted(oldGoal))
                errors.Add("Old goal entity was not deleted");

            if (GetGoal(entMan, station) is not { } goal || entMan.GetComponent<MetaDataComponent>(goal).EntityPrototype?.ID != GoalB)
                errors.Add("Station does not have the new goal");
            else if (goal.Comp.Station != station)
                errors.Add("New goal is not linked to the station");

            entMan.DeleteEntity(station);
        });

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ClearAndResend()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var goalSystem = entMan.System<StationGoalSystem>();
        List<string> errors = [];
        EntityUid station = default;
        EntityUid oldGoal = default;

        await server.WaitPost(() =>
        {
            station = CreateStation(entMan, map.Grid);
            if (!goalSystem.TryAssignGoal(station, GoalA, out _))
                errors.Add("Valid goal was not assigned");

            if (goalSystem.TryAssignGoal(station, "BaseStationGoal", out _))
                errors.Add("Abstract goal was assigned");

            if (!goalSystem.TryResendGoal(station, out _))
                errors.Add("Resend failed for a station with a goal");

            oldGoal = GetGoal(entMan, station)!.Value;
            if (!goalSystem.TryClearGoal(station))
                errors.Add("Clear failed for a station with a goal");

            if (goalSystem.TryResendGoal(station, out _))
                errors.Add("Resend succeeded for a station without a goal");

            if (goalSystem.TryClearGoal(station))
                errors.Add("Clear succeeded for a station without a goal");
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            if (!entMan.Deleted(oldGoal))
                errors.Add("Cleared goal entity was not deleted");

            if (GetGoal(entMan, station) != null)
                errors.Add("Station still has a goal after clear");

            entMan.DeleteEntity(station);
        });

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CoverageMath()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var shieldSystem = entMan.System<MeteorShieldSystem>();
        List<string> errors = [];
        List<EntityUid> spawned = [];
        EntityUid station = default;
        var center = new Vector2(0.5f, 0.5f);

        await server.WaitPost(() => station = CreateStation(entMan, map.Grid));

        async Task<float> Coverage(params (Vector2 Offset, bool Active, bool Walled)[] satellites)
        {
            var coverage = 0f;
            await server.WaitPost(() =>
            {
                foreach (var (offset, active, walled) in satellites)
                {
                    SpawnSatellite(server, map, center + offset, active, walled, spawned);
                }
            });

            await server.WaitRunTicks(5);

            await server.WaitPost(() =>
            {
                coverage = shieldSystem.GetCoverage(station);
                foreach (var uid in spawned)
                {
                    entMan.DeleteEntity(uid);
                }
                spawned.Clear();
            });

            return coverage;
        }

        async Task Check(string name, float expected, float tolerance, params (Vector2, bool, bool)[] satellites)
        {
            var coverage = await Coverage(satellites);
            if (MathF.Abs(coverage - expected) > tolerance)
                errors.Add($"{name}: coverage {coverage}, expected {expected}");
        }

        // One satellite at distance 40 with range 30 covers directions within asin(0.75) of it.
        var single = 2f * MathF.Asin(0.75f) / MathF.Tau;
        var east = new Vector2(40f, 0f);
        var west = new Vector2(-40f, 0f);

        await Check("No satellites", 0f, 0.001f);
        await Check("Inactive satellite", 0f, 0.001f, (east, false, false));
        await Check("Single satellite", single, 0.03f, (east, true, false));
        await Check("Overlapping satellites", single, 0.03f, (east, true, false), (east, true, false));
        await Check("Opposite satellites", 2f * single, 0.05f, (east, true, false), (west, true, false));
        await Check("Satellite next to the station", 1f, 0.001f, (new Vector2(5f, 0f), true, false));
        await Check("Walled satellite", 0f, 0.001f, (east, true, true));

        // Meteors spawn from 50 to 150 beyond the station radius, so a satellite at 120 catches only those spawned behind it.
        var deep = await Coverage((new Vector2(120f, 0f), true, false));
        var deepMax = 2f * MathF.Asin(0.25f) / MathF.Tau;
        if (deep <= 0.001f || deep >= deepMax)
            errors.Add($"Satellite inside the meteor spawn zone: coverage {deep}, expected between 0 and {deepMax}");

        await Check("Satellite beyond meteor spawns", 0f, 0.001f, (new Vector2(500f, 0f), true, false));

        await server.WaitPost(() => entMan.DeleteEntity(station));

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ShieldDeletesMeteor()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var shieldSystem = entMan.System<MeteorShieldSystem>();
        var transform = entMan.System<SharedTransformSystem>();
        List<string> errors = [];
        List<EntityUid> spawned = [];
        EntityUid openMeteor = default;
        EntityUid walledMeteor = default;
        EntityUid unpoweredMeteor = default;
        EntityUid open = default;

        await server.WaitPost(() =>
        {
            open = SpawnSatellite(server, map, new Vector2(200.5f, 0.5f), true, false, spawned);
            openMeteor = entMan.Spawn(Meteor, new MapCoordinates(new Vector2(210f, 0.5f), map.MapId));

            SpawnSatellite(server, map, new Vector2(400.5f, 0.5f), true, true, spawned);
            walledMeteor = entMan.Spawn(Meteor, new MapCoordinates(new Vector2(410f, 0.5f), map.MapId));

            var unpowered = SpawnSatellite(server, map, new Vector2(600.5f, 0.5f), true, false, spawned);
            entMan.System<PowerReceiverSystem>().SetNeedsPower(unpowered, true);
            unpoweredMeteor = entMan.Spawn(Meteor, new MapCoordinates(new Vector2(610f, 0.5f), map.MapId));

            spawned.Add(walledMeteor);
            spawned.Add(unpoweredMeteor);
        });

        await server.WaitRunTicks(30);

        await server.WaitAssertion(() =>
        {
            if (!entMan.Deleted(openMeteor))
                errors.Add("Working satellite did not destroy a meteor in line of sight");

            if (entMan.Deleted(walledMeteor))
                errors.Add("Satellite destroyed a meteor through a wall");

            if (entMan.Deleted(unpoweredMeteor))
                errors.Add("Unpowered satellite destroyed a meteor");

            transform.Unanchor(open);
            if (entMan.GetComponent<MeteorShieldComponent>(open).Active)
                errors.Add("Satellite stays switched on after it is unanchored");

            foreach (var uid in spawned)
            {
                entMan.DeleteEntity(uid);
            }
        });

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AiShuttleProgress()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var stationMap = await pair.CreateTestMap();
        var shuttleMap = await pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var goalSystem = entMan.System<StationGoalSystem>();
        var power = entMan.System<PowerReceiverSystem>();
        var transform = entMan.System<SharedTransformSystem>();
        List<string> errors = [];
        EntityUid station = default;
        EntityUid core = default;
        Entity<StationGoalInfoComponent> goal = default;

        await server.WaitPost(() =>
        {
            station = CreateStation(entMan, stationMap.Grid);
            goalSystem.TryAssignGoal(station, GoalAiShuttle, out _);
            goal = GetGoal(entMan, station)!.Value;

            core = entMan.SpawnAtPosition(AiShuttleCore, stationMap.GridCoords);
            if (goalSystem.GetProgress(goal) != 0f)
                errors.Add("Core on the station grid counts");

            var stray = entMan.SpawnAtPosition(AiShuttleCore, shuttleMap.GridCoords);
            if (goalSystem.GetProgress(goal) != 0f)
                errors.Add("Core not delivered to the station counts");

            entMan.DeleteEntity(stray);
            if (!transform.AnchorEntity(core, shuttleMap.GridCoords))
                errors.Add("Could not anchor the core on the shuttle");

            if (goalSystem.GetProgress(goal) is not { } unpowered || MathF.Abs(unpowered - 1f / 3f) > 0.001f)
                errors.Add($"Unpowered core on a shuttle: progress {goalSystem.GetProgress(goal)}, expected 1/3");

            if (entMan.HasComponent<AiRemoteGridComponent>(shuttleMap.Grid))
                errors.Add("Grid of an unpowered core is remote");

            power.SetNeedsPower(core, false);
        });

        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            if (goalSystem.GetProgress(goal) is not { } powered || MathF.Abs(powered - 2f / 3f) > 0.001f)
                errors.Add($"Powered core on a shuttle without thrusters: progress {goalSystem.GetProgress(goal)}, expected 2/3");

            if (!entMan.HasComponent<AiRemoteGridComponent>(shuttleMap.Grid))
                errors.Add("Grid of an anchored powered core is not remote");

            transform.Unanchor(core);
            if (entMan.HasComponent<AiRemoteGridComponent>(shuttleMap.Grid))
                errors.Add("Grid stays remote after the core is unanchored");

            transform.AnchorEntity(core, shuttleMap.GridCoords);

            entMan.AddComponent<EmergencyShuttleComponent>(shuttleMap.Grid);
            if (goalSystem.GetProgress(goal) != 0f)
                errors.Add("Core on the evac shuttle counts");

            entMan.RemoveComponent<EmergencyShuttleComponent>(shuttleMap.Grid);
            entMan.AddComponent<ArrivalsShuttleComponent>(shuttleMap.Grid);
            if (goalSystem.GetProgress(goal) != 0f)
                errors.Add("Core on the arrivals shuttle counts");

            entMan.RemoveComponent<ArrivalsShuttleComponent>(shuttleMap.Grid);
            entMan.DeleteEntity(core);
            if (entMan.HasComponent<AiRemoteGridComponent>(shuttleMap.Grid))
                errors.Add("Grid stays remote after the core is deleted");

            entMan.DeleteEntity(station);
        });

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MechDeliveryProgress()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var centcommMap = await pair.CreateTestMap();
        var stationMap = await pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var goalSystem = entMan.System<StationGoalSystem>();
        var transform = entMan.System<SharedTransformSystem>();
        List<string> errors = [];

        await server.WaitAssertion(() =>
        {
            var station = CreateStation(entMan, stationMap.Grid);
            entMan.AddComponent<StationCentcommComponent>(station).MapEntity = centcommMap.MapUid;
            goalSystem.TryAssignGoal(station, GoalMechs, out _);
            var goal = GetGoal(entMan, station)!.Value;
            List<EntityUid> spawned = [];

            void SpawnMech(string proto, bool built, EntityCoordinates buildAt, EntityCoordinates deliverTo)
            {
                var uid = entMan.SpawnAtPosition(proto, buildAt);
                if (built)
                    entMan.EventBus.RaiseLocalEvent(uid, new ConstructionChangeEntityEvent(uid, uid), true);

                transform.SetCoordinates(uid, deliverTo);
                spawned.Add(uid);
            }

            void Check(string name, float expected)
            {
                if (goalSystem.GetProgress(goal) is not { } progress || MathF.Abs(progress - expected) > 0.001f)
                    errors.Add($"{name}: progress {goalSystem.GetProgress(goal)}, expected {expected}");
            }

            SpawnMech(CombatMech, true, stationMap.GridCoords, centcommMap.GridCoords);
            Check("One built mech at CentComm", 1f / 3f);

            SpawnMech(CombatMech, false, stationMap.GridCoords, centcommMap.GridCoords);
            Check("Spawned mech does not count", 1f / 3f);

            SpawnMech(CombatMech, true, stationMap.GridCoords, stationMap.GridCoords);
            Check("Built mech still on the station does not count", 1f / 3f);

            SpawnMech(OtherMech, true, stationMap.GridCoords, centcommMap.GridCoords);
            Check("Non-combat mech does not count", 1f / 3f);

            SpawnMech(CombatMech, true, centcommMap.GridCoords, centcommMap.GridCoords);
            Check("Mech built outside the station does not count", 1f / 3f);

            SpawnMech(CombatMech, true, stationMap.GridCoords, centcommMap.GridCoords);
            SpawnMech(CombatMech, true, stationMap.GridCoords, centcommMap.GridCoords);
            Check("Three built mechs at CentComm", 1f);

            foreach (var uid in spawned)
            {
                entMan.DeleteEntity(uid);
            }

            entMan.DeleteEntity(station);
        });

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task GoalApprovesLatheRecipes()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var goalSystem = entMan.System<StationGoalSystem>();
        var latheSystem = entMan.System<LatheSystem>();
        var research = entMan.System<SharedResearchSystem>();
        List<string> errors = [];

        await server.WaitAssertion(() =>
        {
            var station = CreateStation(entMan, map.Grid);
            var lathe = entMan.SpawnAtPosition(MechLathe, map.GridCoords);
            var otherLathe = entMan.SpawnAtPosition(OtherLathe, map.GridCoords);

            bool HasCentral()
            {
                return latheSystem.TryGetAvailableRecipes(lathe, out var recipes) && recipes.Contains(CentralRecipe);
            }

            goalSystem.TryAssignGoal(station, GoalMechs, out _);
            if (HasCentral())
                errors.Add("Central board is available before the peripherals board is researched");

            research.AddLatheRecipe(lathe, PeripheralsRecipe);
            if (!HasCentral())
                errors.Add("Central board is not available with the goal and researched peripherals board");

            var otherRecipes = latheSystem.GetAvailableRecipes(otherLathe, entMan.GetComponent<LatheComponent>(otherLathe), true);
            if (otherRecipes.Contains(CentralRecipe))
                errors.Add($"Central board is listed for {OtherLathe}, which cannot print the peripherals board");

            goalSystem.TryAssignGoal(station, GoalA, out _);
            if (HasCentral())
                errors.Add("Central board is still available after the goal was replaced");

            entMan.DeleteEntity(lathe);
            entMan.DeleteEntity(otherLathe);
            entMan.DeleteEntity(station);
        });

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task GoalUnlocksCargoProducts()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var goalSystem = entMan.System<StationGoalSystem>();
        List<string> errors = [];

        await server.WaitAssertion(() =>
        {
            var station = CreateStation(entMan, map.Grid);

            bool Hidden()
            {
                var ev = new GetHiddenCargoProductsEvent([]);
                entMan.EventBus.RaiseEvent(EventSource.Local, ref ev);
                return ev.Hidden.Contains(SatelliteProduct);
            }

            if (!Hidden())
                errors.Add("Satellite can be ordered without a goal");

            goalSystem.TryAssignGoal(station, GoalA, out _);
            if (!Hidden())
                errors.Add("Satellite can be ordered with a goal that does not unlock it");

            goalSystem.TryAssignGoal(station, GoalB, out _);
            if (Hidden())
                errors.Add("Satellite cannot be ordered with the meteor shield goal");

            goalSystem.TryAssignGoal(station, GoalA, out _);
            if (!Hidden())
                errors.Add("Satellite can still be ordered after the meteor shield goal was replaced");

            entMan.DeleteEntity(station);
        });

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ResultsReachClient()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var goalSystem = entMan.System<StationGoalSystem>();
        var summary = pair.Client.EntMan.System<StationGoalSummarySystem>();
        List<string> errors = [];
        const string stationName = "station goal test";
        EntityUid station = default;
        float? expected = null;

        await server.WaitPost(() =>
        {
            station = CreateStation(entMan, map.Grid);
            entMan.System<MetaDataSystem>().SetEntityName(station, stationName);
            goalSystem.TryAssignGoal(station, GoalA, out _);
            goalSystem.TrySetForceCompleted(station, true);
            expected = goalSystem.GetProgress(GetGoal(entMan, station)!.Value);
            entMan.EventBus.RaiseEvent(EventSource.Local, new RoundEndTextAppendEvent());
        });

        await pair.RunTicksSync(5);

        await pair.Client.WaitAssertion(() =>
        {
            var results = summary.Results.Where(r => r.Station == stationName).ToList();
            if (results.Count != 1)
                errors.Add($"Client got {results.Count} results for the test station, expected 1");
            else if (results[0].Progress != expected)
                errors.Add($"Client progress {results[0].Progress}, server progress {expected}");
        });

        await server.WaitPost(() => entMan.DeleteEntity(station));

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AiInteractionNeedsVisionOnCoreGrid()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var aiMap = await pair.CreateTestMap();
        var shuttleMap = await pair.CreateTestMap();
        var otherMap = await pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var interaction = entMan.System<SharedInteractionSystem>();
        var mapSystem = entMan.System<SharedMapSystem>();
        List<string> errors = [];
        List<EntityUid> spawned = [];

        await server.WaitAssertion(() =>
        {
            for (var x = 0; x <= FarTile; x++)
            {
                mapSystem.SetTile(shuttleMap.Grid, new Vector2i(x, 0), shuttleMap.Tile.Tile);
                mapSystem.SetTile(otherMap.Grid, new Vector2i(x, 0), otherMap.Tile.Tile);
            }

            EntityUid SpawnAt(EntityUid grid, int x)
            {
                var uid = entMan.SpawnAtPosition(null, new EntityCoordinates(grid, x + 0.5f, 0.5f));
                spawned.Add(uid);
                return uid;
            }

            entMan.AddComponent<StationAiVisionComponent>(SpawnAt(shuttleMap.Grid, 0));
            entMan.AddComponent<StationAiVisionComponent>(SpawnAt(otherMap.Grid, 0));

            var ai = SpawnAt(aiMap.Grid, 0);
            entMan.AddComponent<StationAiOverlayComponent>(ai);
            var eye = SpawnAt(shuttleMap.Grid, 0);
            entMan.System<SharedMoverController>().SetRelay(ai, eye);
            var seen = SpawnAt(shuttleMap.Grid, 1);
            var unseen = SpawnAt(shuttleMap.Grid, FarTile);
            var otherGrid = SpawnAt(otherMap.Grid, 1);
            entMan.EnsureComponent<AiRemoteGridComponent>(otherMap.Grid);

            if (interaction.InRangeUnobstructed(ai, seen))
                errors.Add("AI reaches another grid without a remote grid");

            entMan.EnsureComponent<AiRemoteGridComponent>(shuttleMap.Grid);
            if (!interaction.InRangeUnobstructed(ai, seen))
                errors.Add("AI does not reach a visible target on the remote grid");

            if (interaction.InRangeUnobstructed(ai, unseen))
                errors.Add("AI reaches a target out of camera vision on the remote grid");

            if (interaction.InRangeUnobstructed(ai, otherGrid))
                errors.Add("AI reaches a visible target on a remote grid without its eye");

            foreach (var uid in spawned)
            {
                entMan.DeleteEntity(uid);
            }
        });

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    private static EntityUid SpawnSatellite(
        RobustIntegrationTest.ServerIntegrationInstance server,
        TestMapData map,
        Vector2 position,
        bool active,
        bool walled,
        List<EntityUid> spawned)
    {
        var entMan = server.EntMan;
        var mapSystem = entMan.System<SharedMapSystem>();
        var transform = entMan.System<SharedTransformSystem>();

        var grid = mapSystem.CreateGridEntity(map.MapId);
        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                mapSystem.SetTile(grid, new Vector2i(x, y), map.Tile.Tile);
                if (walled && (x != 0 || y != 0))
                    entMan.SpawnAtPosition(Wall, new EntityCoordinates(grid, x + 0.5f, y + 0.5f));
            }
        }

        transform.SetWorldPosition(grid, position - new Vector2(0.5f, 0.5f));
        spawned.Add(grid);

        var coords = new EntityCoordinates(grid, 0.5f, 0.5f);
        var uid = entMan.SpawnAtPosition(Satellite, coords);
        transform.AnchorEntity(uid, coords);
        entMan.System<PowerReceiverSystem>().SetNeedsPower(uid, false);
        entMan.System<MeteorShieldSystem>().SetActive((uid, entMan.GetComponent<MeteorShieldComponent>(uid)), active);
        return uid;
    }

    private static EntityUid CreateStation(IEntityManager entMan, EntityUid grid)
    {
        var station = entMan.Spawn(null, MapCoordinates.Nullspace);
        entMan.AddComponent<StationDataComponent>(station);
        entMan.AddComponent<StationGoalComponent>(station);
        entMan.System<StationSystem>().AddGridToStation(station, grid);
        return station;
    }

    private static Entity<StationGoalInfoComponent>? GetGoal(IEntityManager entMan, EntityUid station)
    {
        if (entMan.GetComponent<StationGoalComponent>(station).Goal is not { } goal
            || !entMan.TryGetComponent<StationGoalInfoComponent>(goal, out var info))
            return null;

        return (goal, info);
    }
}
