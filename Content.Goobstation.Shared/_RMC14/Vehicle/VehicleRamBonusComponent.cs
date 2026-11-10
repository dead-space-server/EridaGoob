using Robust.Shared.GameObjects;

namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent]
public sealed partial class VehicleRamBonusComponent : Component
{
    [DataField]
    public float RamDamageMultiplier = 1.5f;
}
