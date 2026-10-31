namespace Content.Shared._Erida.ShieldPack.Components;

[RegisterComponent]
public sealed partial class EnergyShieldedComponent : Component
{
    [DataField]
    public EntityUid ShieldPackSource = default!;
}
