// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Lost-Paradise-Project/Lost-Light/blob/d67c4d747060511f0162c6b8ce679b1895ff16c2/Content.Shared/_Funkystation/Viewcone/ViewconeStorageBlindSystem.cs

using Content.Shared._Erida._ForEvents.Viewcone.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.Containers;

namespace Content.Shared._Erida._ForEvents.Viewcone;

/// <summary>
/// Blinds viewcone holders inside a closed entity storage (lockers, crates).
/// </summary>
public sealed class ESViewconeStorageBlindSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private EntityQuery<ESViewconeComponent> _viewconeQuery;

    public override void Initialize()
    {
        base.Initialize();

        _viewconeQuery = GetEntityQuery<ESViewconeComponent>();

        SubscribeLocalEvent<ESViewconeComponent, ComponentStartup>(OnViewconeStartup);
        SubscribeLocalEvent<EntityStorageComponent, StorageAfterCloseEvent>(OnStorageClosed);
        SubscribeLocalEvent<EntityStorageComponent, StorageBeforeOpenEvent>(OnStorageOpened);
        SubscribeLocalEvent<EntityStorageComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<EntityStorageComponent, EntRemovedFromContainerMessage>(OnRemoved);
    }

    private void OnViewconeStartup(Entity<ESViewconeComponent> ent, ref ComponentStartup args)
    {
        if (!_container.TryGetContainingContainer(ent.Owner, out var container)
            || !TryComp<EntityStorageComponent>(container.Owner, out var storage)
            || storage.Open
            || storage.Contents != container)
            return;

        SetBlind(ent.Owner, true);
    }

    private void OnStorageClosed(Entity<EntityStorageComponent> ent, ref StorageAfterCloseEvent args)
    {
        SetBlindContents(ent, true);
    }

    private void OnStorageOpened(Entity<EntityStorageComponent> ent, ref StorageBeforeOpenEvent args)
    {
        SetBlindContents(ent, false);
    }

    private void OnInserted(Entity<EntityStorageComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (ent.Comp.Open || args.Container != ent.Comp.Contents)
            return;

        SetBlind(args.Entity, true);
    }

    private void OnRemoved(Entity<EntityStorageComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container != ent.Comp.Contents)
            return;

        SetBlind(args.Entity, false);
    }

    private void SetBlindContents(Entity<EntityStorageComponent> ent, bool blind)
    {
        foreach (var contained in ent.Comp.Contents.ContainedEntities)
        {
            SetBlind(contained, blind);
        }
    }

    private void SetBlind(EntityUid uid, bool blind)
    {
        if (!_viewconeQuery.TryComp(uid, out var viewcone) || viewcone.IsBlind == blind)
            return;

        viewcone.IsBlind = blind;
        Dirty(uid, viewcone);
    }
}
