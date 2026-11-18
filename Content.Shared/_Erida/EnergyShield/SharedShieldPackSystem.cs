using Content.Shared._Erida.ShieldPack.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.PowerCell;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Timing;

namespace Content.Shared._Erida.ShieldPack;

public abstract partial class SharedShieldPackSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    [SubscribeLocalEvent]
    private void OnDamageModifyEvent(Entity<EnergyShieldedComponent> ent, ref DamageModifyEvent args)
    {
        var shieldPack = ent.Comp.ShieldPackSource;

        if (!TryComp<ShieldPackComponent>(shieldPack, out var eComp)
            || !_toggle.IsActivated(shieldPack)
            || args.Damage.GetTotal() <= 0)
            return;

        var charge = new GetChargeEvent();
        RaiseLocalEvent(shieldPack, ref charge);

        var reduced = DamageSpecifier.ApplyModifierSet(args.Damage, DamageSpecifier.PenetrateArmor(eComp.Modifiers, args.Damage.ArmorPenetration));
        var absorbed = args.Damage - reduced;

        var cost = absorbed.GetTotal().Float() * eComp.EnergyConsuptionPerDamage;
        if (cost <= 0 || charge.CurrentCharge <= 0)
            return;

        var paid = MathF.Min(1f, charge.CurrentCharge / cost);
        if (paid < 1f)
            reduced += absorbed * (1f - paid);

        args.Damage = reduced;

        var damageToShield = cost * paid * -1;

        var ev = new ChangeChargeEvent(damageToShield);
        RaiseLocalEvent(shieldPack, ref ev);
    }

    [SubscribeLocalEvent]
    private void OnShotAttempt(Entity<EnergyShieldedComponent> ent, ref ShotAttemptedEvent args)
    {
        var shieldPack = ent.Comp.ShieldPackSource;

        if (!TryComp<ShieldPackComponent>(shieldPack, out var eComp)
            || !_toggle.IsActivated(shieldPack))
            return;

        if (!eComp.BlockShooting)
            return;

        args.Cancel();

        if (!(_timing.CurTime > eComp.NextPopupTime))
            return;

        _popup.PopupClient(Loc.GetString("shieldpack-cant-shoot"), ent, ent);
        eComp.NextPopupTime = _timing.CurTime + eComp.PopupShotCooldown;
    }

    [SubscribeLocalEvent]
    private void OnAttempt(Entity<ShieldPackComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (ent.Comp.Wearer == null)
        {
            args.Cancelled = true;
            return;
        }

        if (ent.Comp.CantActivateUntil > _timing.CurTime)
        {
            args.Cancelled = true;
            args.Popup = Loc.GetString("shieldpack-on-cooldown");
        }
    }

    [SubscribeLocalEvent]
    private void OnToggled(Entity<ShieldPackComponent> ent, ref ItemToggledEvent args)
    {
        if (args.Activated)
            CreateShield(ent);
        else
            DestroyShield(ent);
    }

    [SubscribeLocalEvent]
    private void OnItemEquipped(Entity<ShieldPackComponent> ent, ref GotEquippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        ent.Comp.Wearer = args.Equipee;
        Dirty(ent);

        if (_toggle.IsActivated(ent.Owner))
            CreateShield(ent);
    }

    [SubscribeLocalEvent]
    private void OnItemUneqipped(Entity<ShieldPackComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        DestroyShield(ent);
        ent.Comp.Wearer = null;
        Dirty(ent);

        _toggle.TryDeactivate(ent.Owner, args.Equipee, showPopup: false);
    }

    [SubscribeLocalEvent]
    private void OnPowerCellSlotEmpty(Entity<ShieldPackComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        ent.Comp.CantActivateUntil = _timing.CurTime + ent.Comp.ActivateCooldown;
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<ShieldPackComponent> ent, ref ComponentShutdown args)
    {
        DestroyShield(ent);
    }

    private void CreateShield(Entity<ShieldPackComponent> ent)
    {
        if (ent.Comp.Wearer is not { } wearer)
            return;

        var esedComp = EnsureComp<EnergyShieldedComponent>(wearer);
        esedComp.ShieldPackSource = ent;
        Dirty(wearer, esedComp);

        SpawnShieldEffect(ent, wearer);
    }

    private void DestroyShield(Entity<ShieldPackComponent> ent)
    {
        DeleteShieldEffect(ent);

        if (ent.Comp.Wearer is not { } wearer
            || TerminatingOrDeleted(wearer)
            || !TryComp<EnergyShieldedComponent>(wearer, out var esedComp)
            || esedComp.ShieldPackSource != ent.Owner)
            return;

        RemComp(wearer, esedComp);
    }

    protected virtual void SpawnShieldEffect(Entity<ShieldPackComponent> ent, EntityUid wearer)
    {
    }

    protected virtual void DeleteShieldEffect(Entity<ShieldPackComponent> ent)
    {
    }
}
