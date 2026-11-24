// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.InteractionVerbs;
using Robust.Shared.Prototypes;

namespace Content.Server._Erida.SocialInteractions;

/// <summary>
/// A gesture this entity offered and is waiting for the target to answer.
/// </summary>
[RegisterComponent]
public sealed partial class SocialGestureOfferComponent : Component
{
    [ViewVariables]
    public EntityUid Target;

    [ViewVariables]
    public ProtoId<InteractionVerbPrototype> Gesture;

    [ViewVariables]
    public TimeSpan ExpiresAt;
}
