using System;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameObjects;

namespace Content.Shared._RMC14.Vehicle;

public sealed partial class VehicleRamBonusSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;

    public float GetRamDamageMultiplier(EntityUid vehicle)
    {
        var multiplier = 1f;

        if (TryComp(vehicle, out VehicleRamBonusComponent? vehicleBonus))
            multiplier *= MathF.Max(vehicleBonus.RamDamageMultiplier, 0f);

        if (!TryComp(vehicle, out HardpointSlotsComponent? hardpoints) ||
            !TryComp(vehicle, out ItemSlotsComponent? itemSlots))
        {
            return multiplier;
        }

        foreach (var slot in hardpoints.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Id))
                continue;

            if (!_itemSlots.TryGetSlot(vehicle, slot.Id, out var itemSlot, itemSlots) ||
                itemSlot.Item is not { } item)
            {
                continue;
            }

            if (TryComp(item, out HardpointIntegrityComponent? integrity) && integrity.Integrity <= 0f)
                continue;

            if (TryComp(item, out VehicleRamBonusComponent? hardpointBonus))
                multiplier *= MathF.Max(hardpointBonus.RamDamageMultiplier, 0f);
        }

        return multiplier;
    }
}