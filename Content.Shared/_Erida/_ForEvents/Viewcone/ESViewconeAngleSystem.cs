// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Lost-Paradise-Project/Lost-Light/blob/d67c4d747060511f0162c6b8ce679b1895ff16c2/Content.Shared/_ES/Viewcone/ESViewconeAngleSystem.cs
// Ported from https://github.com/Lost-Paradise-Project/Lost-Light/blob/d67c4d747060511f0162c6b8ce679b1895ff16c2/Content.Shared/_LP/Viewcone/ViewconeMobStateModifierSystem.cs

using Content.Shared._Erida._ForEvents.Viewcone.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;

namespace Content.Shared._Erida._ForEvents.Viewcone;

/// <summary>
/// Public API for the actual viewcone angle, including clothing and mob state, rather than just the base angle.
/// </summary>
public sealed class ESViewconeAngleSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    private const SlotFlags ModifierSlots = SlotFlags.HEAD | SlotFlags.EYES | SlotFlags.MASK;

    private EntityQuery<ESViewconeModifierComponent> _modifierQuery;
    private EntityQuery<MaskComponent> _maskQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;

    public override void Initialize()
    {
        base.Initialize();

        _modifierQuery = GetEntityQuery<ESViewconeModifierComponent>();
        _maskQuery = GetEntityQuery<MaskComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();

        SubscribeLocalEvent<ESViewconeModifierComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<ESViewconeModifierComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<ESViewconeComponent>(args.Examiner))
            return;

        var loc = ent.Comp.AngleModifier < 0
            ? "es-viewcone-modifier-examine-decrease"
            : "es-viewcone-modifier-examine-increase";

        var degrees = (int) MathF.Abs(ent.Comp.AngleModifier);
        args.PushMarkup(Loc.GetString(loc, ("degrees", degrees)));
    }

    /// <summary>
    /// Returns the current (lerped) cone angle and updates the angle it lerps to.
    /// </summary>
    public float GetModifiedViewconeAngle(Entity<ESViewconeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return 0f;

        ent.Comp.DesiredConeAngle = ent.Comp.IsBlind
            ? ESViewconeComponent.BaseConeAngleBlind
            : ent.Comp.BaseConeAngle + GetAngleModifier((ent.Owner, ent.Comp));

        return ent.Comp.CurrentConeAngle;
    }

    public float GetModifiedConeIgnoreRadius(Entity<ESViewconeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return 0f;

        return ent.Comp.IsBlind ? ent.Comp.ConeIgnoreRadiusBlind : ent.Comp.ConeIgnoreRadius;
    }

    private float GetAngleModifier(Entity<ESViewconeComponent> ent)
    {
        float? modifier = null;

        if (_modifierQuery.TryComp(ent, out var own))
            modifier = own.AngleModifier;

        if (_mobStateQuery.TryComp(ent, out var mobState)
            && ent.Comp.MobStateModifiers.TryGetValue(mobState.CurrentState, out var stateModifier))
            modifier = Lowest(modifier, stateModifier);

        if (!_inventory.TryGetContainerSlotEnumerator(ent.Owner, out var slots, ModifierSlots))
            return modifier ?? 0f;

        while (slots.NextItem(out var item))
        {
            if (!_modifierQuery.TryComp(item, out var clothing)
                || _maskQuery.TryComp(item, out var mask) && mask.IsToggled)
                continue;

            modifier = Lowest(modifier, clothing.AngleModifier);
        }

        return modifier ?? 0f;
    }

    private static float Lowest(float? current, float value)
    {
        return current is { } c ? MathF.Min(c, value) : value;
    }
}
