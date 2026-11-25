// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Server.Lathe;
using Content.Server.Station.Systems;
using Content.Shared.Lathe;
using Content.Shared.Research.Systems;

namespace Content.Server._Erida.StationGoals;

/// <summary>
/// Adds the recipes approved by the station goal to the lathes of its station.
/// </summary>
public sealed class StationGoalLatheRecipesSystem : EntitySystem
{
    [Dependency] private LatheSystem _lathe = default!;
    [Dependency] private StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LatheComponent, LatheGetRecipesEvent>(OnGetRecipes, after: [typeof(LatheSystem), typeof(BlueprintSystem)]);
        SubscribeLocalEvent<StationGoalLatheRecipesComponent, StationGoalAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<StationGoalLatheRecipesComponent, ComponentShutdown>(OnShutdown);
    }

    #region Component events

    private void OnGetRecipes(Entity<LatheComponent> ent, ref LatheGetRecipesEvent args)
    {
        if (args.Lathe != ent.Owner || !TryGetApproval(ent, out var approval))
            return;

        foreach (var (recipe, required) in approval.Recipes)
        {
            if (args.Recipes.Contains(required))
                args.Recipes.Add(recipe);
        }
    }

    private void OnAssigned(Entity<StationGoalLatheRecipesComponent> ent, ref StationGoalAssignedEvent args)
    {
        RefreshLathes(args.Station);
    }

    private void OnShutdown(Entity<StationGoalLatheRecipesComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<StationGoalInfoComponent>(ent, out var info)
            || info.Station is not { } station
            || TerminatingOrDeleted(station))
            return;

        RefreshLathes(station);
    }

    #endregion

    #region Helpers

    private bool TryGetApproval(EntityUid lathe, [NotNullWhen(true)] out StationGoalLatheRecipesComponent? approval)
    {
        approval = null;
        return _station.GetOwningStation(lathe) is { } station
            && TryComp<StationGoalComponent>(station, out var goal)
            && TryComp(goal.Goal, out approval);
    }

    private void RefreshLathes(EntityUid station)
    {
        var query = EntityQueryEnumerator<LatheComponent>();
        while (query.MoveNext(out var uid, out var lathe))
        {
            if (_station.GetOwningStation(uid) == station)
                _lathe.UpdateUserInterfaceState(uid, lathe);
        }
    }

    #endregion
}
