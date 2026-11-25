// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Erida.StationGoals.AiShuttle;
using Content.Server.Cargo.Systems;
using Content.Server.Construction;
using Content.Server.GameTicking;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Mech.Components;
using Content.Shared.Station.Components;

namespace Content.Server._Erida.StationGoals.Conditions;

/// <summary>
/// Simple station goal conditions.
/// </summary>
public sealed class StationGoalConditionsSystem : EntitySystem
{
    [Dependency] private CargoSystem _cargo = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private GameTicker _ticker = default!;

    private const int AiShuttleSteps = 3;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CargoBalanceConditionComponent, StationGoalGetProgressEvent>(OnCargoBalanceProgress);
        SubscribeLocalEvent<RoundDurationConditionComponent, StationGoalGetProgressEvent>(OnRoundDurationProgress);
        SubscribeLocalEvent<MechDeliveryConditionComponent, StationGoalGetProgressEvent>(OnMechDeliveryProgress);
        SubscribeLocalEvent<AiShuttleConditionComponent, StationGoalGetProgressEvent>(OnAiShuttleProgress);
        SubscribeLocalEvent<ConstructionChangeEntityEvent>(OnConstructionChangeEntity);
    }

    private void OnConstructionChangeEntity(ConstructionChangeEntityEvent ev)
    {
        if (!HasComp<MechComponent>(ev.New))
            return;

        EnsureComp<StationGoalBuiltMechComponent>(ev.New).Station = _station.GetOwningStation(ev.New);
    }

    private void OnMechDeliveryProgress(Entity<MechDeliveryConditionComponent> ent, ref StationGoalGetProgressEvent args)
    {
        if (ent.Comp.Count <= 0)
        {
            args.Add(1f);
            return;
        }

        if (!TryComp<StationCentcommComponent>(args.Station, out var centcomm) || centcomm.MapEntity is not { } map)
        {
            args.Add(0f);
            return;
        }

        var count = 0;
        var query = EntityQueryEnumerator<StationGoalBuiltMechComponent, TransformComponent, MetaDataComponent>();
        while (query.MoveNext(out var mech, out var xform, out var meta))
        {
            if (mech.Station != args.Station
                || xform.MapUid != map
                || meta.EntityPrototype is not { } proto
                || !ent.Comp.Mechs.Contains(proto.ID))
                continue;

            count++;
        }

        args.Add((float) count / ent.Comp.Count);
    }

    private void OnCargoBalanceProgress(Entity<CargoBalanceConditionComponent> ent, ref StationGoalGetProgressEvent args)
    {
        if (!TryComp<StationBankAccountComponent>(args.Station, out var bank))
        {
            args.Add(0f);
            return;
        }

        var account = ent.Comp.Account ?? bank.PrimaryAccount;
        var balance = _cargo.GetBalanceFromAccount((args.Station, bank), account);
        args.Add((float) balance / Math.Max(ent.Comp.Amount, 1));
    }

    private void OnAiShuttleProgress(Entity<AiShuttleConditionComponent> ent, ref StationGoalGetProgressEvent args)
    {
        var best = 0;
        var query = EntityQueryEnumerator<AiShuttleCoreComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var core, out var xform))
        {
            if (core.Station != args.Station)
                continue;

            best = Math.Max(best, GetAiShuttleSteps(uid, xform));
            if (best == AiShuttleSteps)
                break;
        }

        args.Add((float) best / AiShuttleSteps);
    }

    private void OnRoundDurationProgress(Entity<RoundDurationConditionComponent> ent, ref StationGoalGetProgressEvent args)
    {
        if (ent.Comp.Duration <= TimeSpan.Zero)
        {
            args.Add(1f);
            return;
        }

        args.Add((float) (_ticker.RoundDuration() / ent.Comp.Duration));
    }

    /// <summary>
    /// Anchored on a grid outside the station that is not the evac or arrivals shuttle, powered, the grid can fly.
    /// </summary>
    private int GetAiShuttleSteps(EntityUid core, TransformComponent xform)
    {
        if (!xform.Anchored
            || xform.GridUid is not { } grid
            || HasComp<StationMemberComponent>(grid)
            || HasComp<EmergencyShuttleComponent>(grid)
            || HasComp<ArrivalsShuttleComponent>(grid))
            return 0;

        if (!this.IsPowered(core, EntityManager))
            return 1;

        return CanFly(grid) ? 3 : 2;
    }

    private bool CanFly(EntityUid grid)
    {
        if (!TryComp<ShuttleComponent>(grid, out var shuttle) || shuttle.AngularThrust <= 0f)
            return false;

        foreach (var thrust in shuttle.LinearThrust)
        {
            if (thrust > 0f)
                return true;
        }

        return false;
    }
}
