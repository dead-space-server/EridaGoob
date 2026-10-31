using Content.Shared._Erida.ShieldPack.Components;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Timing;

namespace Content.Shared._Erida.ShieldPack;

public abstract class SharedShieldPackSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EnergyShieldedComponent, DamageModifyEvent>(OnDamageModifyEvent);
        SubscribeLocalEvent<EnergyShieldedComponent, ShotAttemptedEvent>(OnShotAttempt);
    }

    private void OnDamageModifyEvent(Entity<EnergyShieldedComponent> ent, ref DamageModifyEvent args)
    {
        var shieldPack = ent.Comp.ShieldPackSource;

        if (!TryComp<ShieldPackComponent>(shieldPack, out var eComp)
            || !eComp.Activated
            || args.Damage.GetTotal() <= 0)
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, DamageSpecifier.PenetrateArmor(eComp.Modifiers, args.Damage.ArmorPenetration), out var absorbed);

        var damageToShield = absorbed.GetTotal().Float() * eComp.EnergyConsuptionPerDamage * -1;

        var ev = new ChangeChargeEvent(damageToShield);
        RaiseLocalEvent(shieldPack, ref ev);
    }


    private void OnShotAttempt(Entity<EnergyShieldedComponent> ent, ref ShotAttemptedEvent args)
    {
        var shieldPack = ent.Comp.ShieldPackSource;

        if (!TryComp<ShieldPackComponent>(shieldPack, out var eComp)
            || !eComp.Activated)
            return;

        if (!eComp.BlockShooting)
            return;

        args.Cancel();

        if (!(_timing.CurTime > eComp.NextPopupTime))
            return;

        _popup.PopupClient(Loc.GetString("shieldpack-cant-shoot"), ent, ent);
        eComp.NextPopupTime = _timing.CurTime + eComp.PopupShotCooldown;
    }
}
