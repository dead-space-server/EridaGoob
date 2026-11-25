// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.AlmanacBlade;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Erida.StationGoals;

/// <summary>
/// Hides the cargo products of station goals until one of the goals that unlock them is active.
/// The cargo catalogue is not built per station, so an active goal on any station unlocks its products.
/// </summary>
public sealed class StationGoalCargoProductsSystem : EntitySystem
{
    [Dependency] private CargoSystem _cargo = default!;
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly HashSet<ProtoId<CargoProductPrototype>> _goalProducts = [];
    private readonly HashSet<ProtoId<CargoProductPrototype>> _activeProducts = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetHiddenCargoProductsEvent>(OnGetHiddenProducts);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<StationGoalCargoProductsComponent, StationGoalAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<StationGoalCargoProductsComponent, ComponentShutdown>(OnShutdown);

        CacheGoalProducts();
    }

    #region Handle events

    private void OnGetHiddenProducts(ref GetHiddenCargoProductsEvent args)
    {
        _activeProducts.Clear();
        var query = EntityQueryEnumerator<StationGoalCargoProductsComponent, StationGoalInfoComponent>();
        while (query.MoveNext(out var uid, out var products, out var info))
        {
            if (!IsActive(uid, info))
                continue;

            _activeProducts.UnionWith(products.Products);
        }

        foreach (var product in _goalProducts)
        {
            if (!_activeProducts.Contains(product))
                args.Hidden.Add(product);
        }
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            CacheGoalProducts();
    }

    #endregion

    #region Component events

    private void OnAssigned(Entity<StationGoalCargoProductsComponent> ent, ref StationGoalAssignedEvent args)
    {
        RefreshConsoles();
    }

    private void OnShutdown(Entity<StationGoalCargoProductsComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<StationGoalInfoComponent>(ent, out var info)
            || info.Station is not { } station
            || TerminatingOrDeleted(station))
            return;

        RefreshConsoles();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// The goal is still the current goal of its station, a replaced goal is only queued for deletion.
    /// </summary>
    private bool IsActive(EntityUid goal, StationGoalInfoComponent info)
    {
        return !TerminatingOrDeleted(goal)
            && info.Station is { } station
            && TryComp<StationGoalComponent>(station, out var stationGoal)
            && stationGoal.Goal == goal;
    }

    private void CacheGoalProducts()
    {
        _goalProducts.Clear();
        foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            if (!proto.Abstract && proto.TryComp<StationGoalCargoProductsComponent>(out var products, _factory))
                _goalProducts.UnionWith(products.Products);
        }
    }

    private void RefreshConsoles()
    {
        var query = EntityQueryEnumerator<StationCargoOrderDatabaseComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (!TerminatingOrDeleted(uid))
                _cargo.UpdateOrders(uid);
        }
    }

    #endregion
}
