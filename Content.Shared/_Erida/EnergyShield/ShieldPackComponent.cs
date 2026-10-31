using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Erida.ShieldPack.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShieldPackComponent : Component
{
    [DataField(required: true)]
    public float EnergyConsuptionPerDamage = default!;

    [DataField(required: true)]
    public DamageModifierSet Modifiers = default!;

    [DataField(required: true)]
    public EntProtoId Shield = "BaseShieldPack";

    [DataField, AutoNetworkedField]
    public EntityUid? CurrentShield = null;

    [DataField, AutoNetworkedField]
    public bool Activated = false;

    [DataField, AutoNetworkedField]
    public bool BlockShooting = false;

    [DataField]
    public TimeSpan ActivateCooldown = TimeSpan.FromSeconds(15f);

    [DataField]
    public TimeSpan? CantActivateUntil = null;

    [DataField]
    public TimeSpan PopupShotCooldown = TimeSpan.FromSeconds(3f);

    [DataField]
    public TimeSpan NextPopupTime = TimeSpan.Zero;
}
