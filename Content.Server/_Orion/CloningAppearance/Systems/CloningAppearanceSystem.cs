using Content.Server._Orion.CloningAppearance.Components;
using Content.Server._Orion.CloningAppearance.Events;
using Content.Server._EinsteinEngines.Language; // erida edit
using Content.Server.Clothing.Systems;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Station.Systems;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Preferences;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager;

namespace Content.Server._Orion.CloningAppearance.Systems;

// erida edit - port InteQ
// SPDX-License-Identifier: AGPL-3.0-or-later

public sealed class CloningAppearanceSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly StationSystem _stations = default!;
    [Dependency] private readonly StationSpawningSystem _spawning = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly OutfitSystem _outfitSystem = default!;
    [Dependency] private readonly LanguageSystem _language = default!; // erida edit

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CloningAppearanceComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CloningAppearanceEvent>(OnPlayerSpawn);
    }

    public EntityUid SpawnProfileEntity(EntityCoordinates coordinates, HumanoidCharacterProfile profile, EntityUid? stationUid = null)
    {
        return _spawning.SpawnPlayerMob(coordinates, null, profile, stationUid);
    }

    public EntityUid SpawnProfileEntity(EntityCoordinates coordinates, ICommonSession player, EntityUid? stationUid = null)
    {
        var profile = _ticker.GetPlayerProfile(player);
        return SpawnProfileEntity(coordinates, profile, stationUid);
    }

    private void OnPlayerSpawn(CloningAppearanceEvent ev)
    {
        var profile = _ticker.GetPlayerProfile(ev.Player);
        var mobUid = SpawnProfileEntity(ev.Coords, profile, ev.StationUid);
        var targetMind = ev.MindId != null && TryComp<MindComponent>(ev.MindId, out var transferredMind)
            ? (ev.MindId.Value, transferredMind)
            : _mindSystem.GetOrCreateMind(ev.Player.UserId);

        foreach (var entry in ev.Component.Components.Values)
        {
            var comp = (Component) _serialization.CreateCopy(entry.Component, notNullableOverride: true);
            EntityManager.AddComponent(mobUid, comp, true);
        }

        if (ev.Component.StartingGear != null)
            _outfitSystem.SetOutfit(mobUid, ev.Component.StartingGear);

        // erida edit: traits only get applied by TraitSystem on PlayerSpawnCompleteEvent, no direct call here;
        // languages still need a refresh because the copied components may carry language knowledge
        _language.UpdateEntityLanguages(mobUid);

        foreach (var nearbyEntity in _entityLookupSystem.GetEntitiesInRange(mobUid, 1f))
        {
            if (!TryComp<CryostorageComponent>(nearbyEntity, out var cryostorageComponent))
                continue;

            if (!_container.TryGetContainer(nearbyEntity, cryostorageComponent.ContainerId, out var container))
                continue;

            if (!_container.CanInsert(mobUid, container, true))
                continue;

            _container.Insert(mobUid, container);
            break;
        }

        targetMind.Comp.CharacterName = MetaData(mobUid).EntityName;
        targetMind.Comp.OriginalOwnedEntity = GetNetEntity(mobUid);
        Dirty(targetMind);
        _mindSystem.TransferTo(targetMind, mobUid);
    }

    private void OnPlayerAttached(Entity<CloningAppearanceComponent> ent, ref PlayerAttachedEvent args)
    {
        if(TerminatingOrDeleted(ent))
            return;

        QueueLocalEvent(new CloningAppearanceEvent
        {
            Player = args.Player,
            Component = ent.Comp,
            StationUid = _stations.GetOwningStation(ent),
            Coords = Transform(ent).Coordinates,
            MindId = TryComp<MindContainerComponent>(ent, out var mindContainer) ? mindContainer.Mind : null,
        });

        QueueDel(ent);
    }
}
