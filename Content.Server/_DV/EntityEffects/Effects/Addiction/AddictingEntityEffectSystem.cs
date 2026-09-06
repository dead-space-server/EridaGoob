using Content.Shared._DV.Addictions;
using Content.Shared._DV.EntityEffects.Effects.Addiction;
using Content.Shared.EntityEffects;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Server._DV.EntityEffects.Effects.Addiction;

// TODO: When Addiction is moved to new Status, make this use StatusEffectsContainerComponent.
/// <summary>
/// Applies the addition status effect for an amount of time based on how much of the addicting reagent is in the system.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class AddictingEntityEffectSystem : EntityEffectSystem<StatusEffectsComponent, Addicting>
{
    [Dependency] private SharedAddictionSystem _addiction = default!;
    protected override void Effect(Entity<StatusEffectsComponent> entity, ref EntityEffectEvent<Addicting> args)
    {
        var addictionTime = args.Effect.Time * args.Scale;

        _addiction.TryApplyAddiction(entity, addictionTime);
    }
}
