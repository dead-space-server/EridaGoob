using System.ComponentModel.DataAnnotations;

namespace Content.Shared._Erida.EnergyShield.Components;

[RegisterComponent]
public sealed partial class EnergyShieldedComponent : Component
{
    [DataField]
    public EntityUid EnergyShield = default;
}
