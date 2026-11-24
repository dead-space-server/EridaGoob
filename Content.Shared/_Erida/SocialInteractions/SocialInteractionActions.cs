// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.InteractionVerbs;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Erida.SocialInteractions;

/// <summary>
/// Offers a two-sided gesture to the target, or completes it if the target already offered the same one.
/// The verb prototype id is the gesture id.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SocialGestureAction : InteractionAction
{
    [DataField]
    public SoundSpecifier? Sound;

    public override bool CanPerform(InteractionArgs args, InteractionVerbPrototype proto, bool beforeDelay, VerbDependencies deps) => true;

    public override bool Perform(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        var ev = new SocialGestureEvent(args.User, args.Target, proto.ID, Sound);
        deps.EntMan.EventBus.RaiseEvent(EventSource.Local, ref ev);
        return ev.Handled;
    }
}

/// <summary>
/// Throws a harmless projectile at the target that shows a popup to whoever it hits.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BlowKissAction : InteractionAction
{
    [DataField(required: true)]
    public EntProtoId Projectile;

    [DataField]
    public float Speed = 8f;

    public override bool CanPerform(InteractionArgs args, InteractionVerbPrototype proto, bool beforeDelay, VerbDependencies deps) => true;

    public override bool Perform(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps)
    {
        var ev = new BlowKissEvent(args.User, args.Target, Projectile, Speed);
        deps.EntMan.EventBus.RaiseEvent(EventSource.Local, ref ev);
        return ev.Handled;
    }
}

/// <summary>
/// Lights an unlit cigarette in the target's mouth with a hot item from the user's hands.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class LightCigaretteAction : InteractionAction
{
    public override bool IsAllowed(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps)
        => Raise(args, deps, true);

    public override bool CanPerform(InteractionArgs args, InteractionVerbPrototype proto, bool beforeDelay, VerbDependencies deps)
        => Raise(args, deps, true);

    public override bool Perform(InteractionArgs args, InteractionVerbPrototype proto, VerbDependencies deps)
        => Raise(args, deps, false);

    private static bool Raise(InteractionArgs args, VerbDependencies deps, bool checkOnly)
    {
        var ev = new LightCigaretteEvent(args.User, args.Target, checkOnly);
        deps.EntMan.EventBus.RaiseEvent(EventSource.Local, ref ev);
        return ev.Handled;
    }
}

/// <summary>
/// Raised by <see cref="SocialGestureAction"/> on the server.
/// </summary>
[ByRefEvent]
public record struct SocialGestureEvent(EntityUid User, EntityUid Target, ProtoId<InteractionVerbPrototype> Gesture, SoundSpecifier? Sound, bool Handled = false);

/// <summary>
/// Raised by <see cref="BlowKissAction"/> on the server.
/// </summary>
[ByRefEvent]
public record struct BlowKissEvent(EntityUid User, EntityUid Target, EntProtoId Projectile, float Speed, bool Handled = false);

/// <summary>
/// Raised by <see cref="LightCigaretteAction"/> on the server. With <see cref="CheckOnly"/> only reports whether it can be done.
/// </summary>
[ByRefEvent]
public record struct LightCigaretteEvent(EntityUid User, EntityUid Target, bool CheckOnly, bool Handled = false);
