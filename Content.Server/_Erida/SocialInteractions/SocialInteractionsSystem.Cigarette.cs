// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Nutrition.Components;
using Content.Shared._Erida.SocialInteractions;
using Content.Shared.Nutrition.Components;
using Content.Shared.Smoking;
using Content.Shared.Temperature;

namespace Content.Server._Erida.SocialInteractions;

public sealed partial class SocialInteractionsSystem
{
    private void InitializeCigarette()
    {
        SubscribeLocalEvent<LightCigaretteEvent>(OnLightCigarette);
    }

    private void OnLightCigarette(ref LightCigaretteEvent args)
    {
        if (!_inventory.TryGetSlotEntity(args.Target, "mask", out var cigarette)
            || !TryComp<CigarComponent>(cigarette, out var cigar)
            || cigar.SelfIgniting
            || !TryComp<SmokableComponent>(cigarette, out var smokable)
            || smokable.State != SmokableState.Unlit
            || !HoldsHotItem(args.User))
            return;

        args.Handled = true;
        if (args.CheckOnly)
            return;

        _smoking.SetSmokableState(cigarette.Value, SmokableState.Lit, smokable);
    }

    private bool HoldsHotItem(EntityUid user)
    {
        if (_hands.GetActiveItem(user) is not { } item)
            return false;

        var ev = new IsHotEvent();
        RaiseLocalEvent(item, ev);
        return ev.IsHot;
    }
}
