using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.EntityEffects.Effects.Addiction;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Addicting : EntityEffectBase<Addicting>
{
    /// <summary>
    /// How long should the pain be per 1u of the reagent.
    /// </summary>
    [DataField]
    public float Time = 5f;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-addicted", ("chance", Probability));
}
