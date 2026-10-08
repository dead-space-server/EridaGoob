using System.ComponentModel.DataAnnotations;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Erida.EnergyShield.Components;

[RegisterComponent]
public sealed partial class EnergyShieldComponent : Component
{
    [DataField(required: true)]
    public float EnergyConsuptionPerDamage = default!;

    [DataField(required: true)]
    public DamageModifierSet Modifiers = default!;

    [DataField(required: true)]
    public EntProtoId Shield = "BaseEnergyShield";

    [DataField]
    public EntityUid? CurrentShield = null;

    [DataField]
    public bool Activated = false;
}

[Serializable, NetSerializable]
public enum ChangeSpriteVisuals : byte
{
    SpriteId
}
