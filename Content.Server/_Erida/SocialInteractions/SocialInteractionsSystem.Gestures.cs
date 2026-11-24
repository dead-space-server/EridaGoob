// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Erida.SocialInteractions;
using Content.Shared.IdentityManagement;
using Robust.Shared.Player;

namespace Content.Server._Erida.SocialInteractions;

public sealed partial class SocialInteractionsSystem
{
    private static readonly TimeSpan OfferTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan OfferCheckInterval = TimeSpan.FromSeconds(0.5);

    private TimeSpan _nextOfferCheck;

    private void InitializeGestures()
    {
        SubscribeLocalEvent<SocialGestureEvent>(OnSocialGesture);
    }

    private void OnSocialGesture(ref SocialGestureEvent args)
    {
        var key = "social-gesture-" + args.Gesture.Id.ToLowerInvariant();
        args.Handled = true;

        if (TryComp<SocialGestureOfferComponent>(args.Target, out var offer)
            && offer.Target == args.User
            && offer.Gesture == args.Gesture)
        {
            RemComp(args.Target, offer);
            PopupSides(key + "-complete", args.User, args.Target);
            _audio.PlayPvs(args.Sound, args.User);
            return;
        }

        var own = EnsureComp<SocialGestureOfferComponent>(args.User);
        own.Target = args.Target;
        own.Gesture = args.Gesture;
        own.ExpiresAt = _timing.CurTime + OfferTimeout;
        PopupSides(key + "-offer", args.User, args.Target);
    }

    private void UpdateGestures()
    {
        var now = _timing.CurTime;
        if (now < _nextOfferCheck)
            return;

        _nextOfferCheck = now + OfferCheckInterval;

        var query = EntityQueryEnumerator<SocialGestureOfferComponent>();
        while (query.MoveNext(out var uid, out var offer))
        {
            if (offer.ExpiresAt > now)
                continue;

            if (_mobState.IsAlive(uid) && !TerminatingOrDeleted(offer.Target))
                ExpireOffer(uid, offer);

            RemCompDeferred(uid, offer);
        }
    }

    private void ExpireOffer(EntityUid user, SocialGestureOfferComponent offer)
    {
        var key = "social-gesture-" + offer.Gesture.Id.ToLowerInvariant() + "-expire";
        _popup.PopupEntity(Loc.GetString(key + "-self"), user, user);

        var others = Filter.Pvs(user, entityManager: EntityManager)
            .RemoveWhereAttachedEntity(e => e == user);
        _popup.PopupEntity(Loc.GetString(key + "-others", ("user", Identity.Entity(user, EntityManager))), user, others, true);
    }
}
