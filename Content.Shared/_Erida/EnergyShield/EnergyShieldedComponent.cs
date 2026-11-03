using Robust.Shared.GameStates;

namespace Content.Shared._Erida.ShieldPack.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EnergyShieldedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid ShieldPackSource = default!;
}
