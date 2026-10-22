using Content.Shared._Erida.EnergyShield.Components;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Shared._Erida.EnergyShield;

public sealed partial class EnergyShieldSystem : EntitySystem
{
    [Dependency] private EntityManager _entityManager = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergyShieldedComponent, DamageModifyEvent>(OnDamageModifyEvent);
        SubscribeLocalEvent<EnergyShieldComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<EnergyShieldComponent, DidUnequipEvent>(OnItemStripped);
    }

    private void OnDamageModifyEvent(Entity<EnergyShieldedComponent> ent, ref DamageModifyEvent args)
    {
        var energyShield = ent.Comp.EnergyShieldSource;

        if (!TryComp<EnergyShieldComponent>(energyShield, out var eComp)
            || !eComp.Activated)
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, DamageSpecifier.PenetrateArmor(eComp.Modifiers, args.Damage.ArmorPenetration), out var absorbed);

        var damageToShield = absorbed.GetTotal().Float() * eComp.EnergyConsuptionPerDamage;

        Logger.Debug("damageToShield: " + damageToShield.ToString());

        var ev = new ChangeChargeEvent(damageToShield);
        RaiseLocalEvent(energyShield, ref ev);
    }

    private void OnToggled(Entity<EnergyShieldComponent> ent, ref ItemToggledEvent args)
    {
        if (args.User == null)
            return;

        ent.Comp.Activated = !ent.Comp.Activated;

        ToggleShield(ent.Owner, args.User.Value, ent.Comp.Activated);
    }

    private void OnItemStripped(Entity<EnergyShieldComponent> ent, ref DidUnequipEvent args)
    {
        ent.Comp.Activated = false;
        ToggleShield(ent.Owner, args.Equipee, ent.Comp.Activated);
    }

    public bool ToggleShield(EntityUid uid, EntityUid user, bool state, EnergyShieldComponent? esComp = null)
    {
        if (!Resolve(uid, ref esComp))
            return false;

        return state ? CreateShield(uid, user, esComp) : DestroyShield(uid, user, esComp);
    }

    public bool CreateShield(EntityUid uid, EntityUid user, EnergyShieldComponent? esComp = null)
    {
        if (!Resolve(uid, ref esComp))
            return false;

        var shield = SpawnAttachedTo(esComp.Shield, _transformSystem.GetParentUid(user).ToCoordinates());
        esComp.CurrentShield = shield;
        var esedComp = EnsureComp<EnergyShieldedComponent>(user);
        esedComp.EnergyShieldSource = uid;
        return true;
    }

    public bool DestroyShield(EntityUid uid, EntityUid user, EnergyShieldComponent? esComp = null)
    {
        if (!Resolve(uid, ref esComp))
            return false;

        _entityManager.DeleteEntity(esComp.CurrentShield);
        _entityManager.RemoveComponent<EnergyShieldedComponent>(user);
        return true;
    }
}
