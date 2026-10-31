using Content.Server.Popups;
using Content.Shared._Erida.ShieldPack;
using Content.Shared._Erida.ShieldPack.Components;
using Content.Shared.Coordinates;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.PowerCell;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Erida.ShieldPack;

public sealed class ShieldPackSystem : SharedShieldPackSystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShieldPackComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<ShieldPackComponent, ItemToggleActivateAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<ShieldPackComponent, GotUnequippedEvent>(OnItemUneqipped);
        SubscribeLocalEvent<ShieldPackComponent, GotEquippedEvent>(OnItemEquipped);
        SubscribeLocalEvent<ShieldPackComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
    }
    private void OnToggled(Entity<ShieldPackComponent> ent, ref ItemToggledEvent args)
    {
        ToggleShield(ent.Owner, args.User);
    }

    private void OnAttempt(Entity<ShieldPackComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (ent.Comp.CantActivateUntil > _timing.CurTime)
        {
            args.Cancelled = true;

            if (args.User == null)
                return;

            _popup.PopupEntity(Loc.GetString("shieldpack-on-cooldown"), args.User.Value, args.User.Value);
        }
    }

    private void OnItemUneqipped(Entity<ShieldPackComponent> ent, ref GotUnequippedEvent args)
    {
        DestroyShield(ent.Owner, args.Equipee);
    }

    private void OnItemEquipped(Entity<ShieldPackComponent> ent, ref GotEquippedEvent args)
    {
        if (ent.Comp.Activated)
            CreateShield(ent.Owner, args.Equipee);
    }

    private void OnPowerCellSlotEmpty(Entity<ShieldPackComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        ent.Comp.CantActivateUntil = _timing.CurTime + ent.Comp.ActivateCooldown;
    }

    public bool ToggleShield(EntityUid uid, EntityUid? user = null, bool? state = null, ShieldPackComponent? esComp = null)
    {
        if (!Resolve(uid, ref esComp))
            return false;

        if (state == null)
            esComp.Activated = !esComp.Activated;
        else
            esComp.Activated = state.Value;

        if (!GetUser(uid, ref user)
            || user == null)
            return false;

        return esComp.Activated ? CreateShield(uid, user.Value, esComp) : DestroyShield(uid, user.Value, esComp);
    }

    public bool CreateShield(EntityUid uid, EntityUid? user, ShieldPackComponent? esComp = null)
    {
        if (!Resolve(uid, ref esComp))
            return false;

        if (!GetUser(uid, ref user)
            || user == null)
            return false;

        var shield = SpawnAttachedTo(esComp.Shield, user.Value.ToCoordinates());
        esComp.CurrentShield = shield;
        var esedComp = EnsureComp<EnergyShieldedComponent>(user.Value);
        esedComp.ShieldPackSource = uid;
        //Dirty(uid, esComp);
        //Dirty(user.Value, esedComp);
        return true;
    }

    public bool DestroyShield(EntityUid uid, EntityUid? user, ShieldPackComponent? esComp = null, bool deleteESComp = true)
    {
        if (!Resolve(uid, ref esComp))
            return false;

        if (!GetUser(uid, ref user, true)
            || user == null)
            return false;

        _entityManager.QueueDeleteEntity(esComp.CurrentShield);
        if (deleteESComp)
            _entityManager.RemoveComponent<EnergyShieldedComponent>(user.Value);
        //Dirty(uid, esComp);
        return true;
    }

    private bool GetUser(EntityUid shieldPack, ref EntityUid? user, bool ignoreUser = false)
    {
        if (!ignoreUser && !_inventory.TryGetContainingSlot(shieldPack, out var _))
            return false;

        if (user != null)
        {
            return true;
        }

        if (user == null && _container.TryGetContainingContainer((shieldPack, null, null), out var container))
        {
            user = container.Owner;
            return true;
        }

        return false;
    }
}
