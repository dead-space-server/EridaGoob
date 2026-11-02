// erida edit start
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameObjects;

namespace Content.Shared._RMC14.Vehicle;

public sealed partial class VehicleGunResolveSystem : EntitySystem
{
    [Dependency] private VehicleWeaponsSystem _weapons = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehiclePortGunOperatorComponent, VehicleGunRequest>(OnPortGun);
        SubscribeLocalEvent<VehicleWeaponsOperatorComponent, VehicleGunRequest>(OnWeapons);
    }

    private void OnPortGun(Entity<VehiclePortGunOperatorComponent> ent, ref VehicleGunRequest args)
    {
        if (ent.Comp.Gun is not { } portGun ||
            !TryComp(portGun, out VehiclePortGunComponent? portGunComp) ||
            portGunComp.Operator != ent.Owner ||
            !TryComp(portGun, out GunComponent? portGunGun))
            return;

        args.Gun = (portGun, portGunGun);
        args.Handled = true;
    }

    private void OnWeapons(Entity<VehicleWeaponsOperatorComponent> ent, ref VehicleGunRequest args)
    {
        if (ent.Comp.Vehicle is not { } vehicle ||
            !_weapons.TryGetSelectedWeaponForOperator(vehicle, ent.Owner, out var selected) ||
            !TryComp(selected, out GunComponent? selectedGun))
            return;

        args.Gun = (selected, selectedGun);
        args.Handled = true;
    }
}
// erida edit end
