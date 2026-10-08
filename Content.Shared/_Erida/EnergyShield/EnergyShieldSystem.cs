using System.Runtime.CompilerServices;
using Content.Shared._Erida.EnergyShield.Components;
using Content.Shared.Armor;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power;
using Content.Shared.Weapons.Melee.Events;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Shared._Erida.EnergyShield;

public sealed partial class EnergyShieldSystem : EntitySystem
{
    [Dependency] private EntityManager _entityManager = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnergyShieldedComponent, BeforeDamageChangedEvent>(OnBeforeDamageChangedEventEvent);
        SubscribeLocalEvent<EnergyShieldComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnBeforeDamageChangedEventEvent(Entity<EnergyShieldedComponent> ent, ref BeforeDamageChangedEvent args)
    {
        var energyShield = ent.Comp.EnergyShield;

        if (!TryComp<EnergyShieldComponent>(energyShield, out var eComp)
            || !eComp.Activated)
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, DamageSpecifier.PenetrateArmor(eComp.Modifiers, args.Damage.ArmorPenetration), out var absorbed);

        var damageToShield = absorbed.GetTotal();

        var ev = new ChangeChargeEvent(eComp.EnergyConsuptionPerDamage * damageToShield.Float());
        RaiseLocalEvent(energyShield, ref ev);
    }

    private void OnToggled(Entity<EnergyShieldComponent> ent, ref ItemToggledEvent args)
    {
        ent.Comp.Activated = !ent.Comp.Activated;

        if (ent.Comp.Activated)
        {
            var shield = SpawnAttachedTo(ent.Comp.Shield, _transformSystem.GetParentUid(ent.Owner).ToCoordinates());
            ent.Comp.CurrentShield = shield;
        }
        else
        {
            _entityManager.DeleteEntity(ent.Comp.CurrentShield);
        }
    }
}
