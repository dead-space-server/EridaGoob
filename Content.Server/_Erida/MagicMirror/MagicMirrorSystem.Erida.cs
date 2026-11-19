// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Erida.MagicMirror;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid.Markings;
using Content.Shared.IdentityManagement;
using Content.Shared.MagicMirror;
using Content.Shared.Popups;

namespace Content.Server.MagicMirror;

public sealed partial class MagicMirrorSystem
{
    /// <summary>
    /// Starts the same do-after as a color change, since dyeing hair with a gradient takes as long.
    /// The mirror sends colors through this message too, with the gradient they go with.
    /// </summary>
    private void OnTryMagicMirrorChangeGradient(Entity<MagicMirrorComponent> ent, ref MagicMirrorChangeGradientMessage message)
    {
        if (ent.Comp.Target is not { } target)
            return;

        if (CheckHeadSlotOrClothes(message.Actor, target))
        {
            _popup.PopupEntity(
                target == message.Actor
                    ? Loc.GetString("magic-mirror-blocked-by-hat-self")
                    : Loc.GetString("magic-mirror-blocked-by-hat-self-target", ("target", Identity.Entity(target, EntityManager))),
                message.Actor,
                message.Actor,
                PopupType.Medium);
            return;
        }

        _doAfterSystem.Cancel(ent.Comp.DoAfter);
        ent.Comp.DoAfter = null;

        var doAfterTime = ent.Comp.ChangeSlotTime;
        if (target == message.Actor)
            doAfterTime /= 3;

        var doAfter = new MagicMirrorChangeGradientDoAfterEvent
        {
            Category = message.Category,
            Slot = message.Slot,
            Colors = message.Colors,
            Gradient = message.Gradient,
        };

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, message.Actor, doAfterTime, doAfter, ent, target: target, used: ent)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                NeedHand = true,
            },
            out var doAfterId);

        _popup.PopupEntity(
            target == message.Actor
                ? Loc.GetString("magic-mirror-change-color-self")
                : Loc.GetString("magic-mirror-change-color-target", ("user", Identity.Entity(message.Actor, EntityManager))),
            target,
            target,
            PopupType.Medium);

        ent.Comp.DoAfter = doAfterId;
    }

    private void OnChangeGradientDoAfter(Entity<MagicMirrorComponent> ent, ref MagicMirrorChangeGradientDoAfterEvent args)
    {
        ent.Comp.DoAfter = null;

        if (args.Handled || args.Cancelled || args.Target is not { } target || ent.Comp.Target != target)
            return;

        MarkingCategories category;
        switch (args.Category)
        {
            case MagicMirrorCategory.Hair:
                category = MarkingCategories.Hair;
                break;
            case MagicMirrorCategory.FacialHair:
                category = MarkingCategories.FacialHair;
                break;
            default:
                return;
        }

        _humanoid.SetMarkingColor(target, category, args.Slot, args.Colors);
        _humanoid.SetMarkingGradient(target, category, args.Slot, args.Gradient);
    }
}
