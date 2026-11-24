// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared._Erida.SocialInteractions;

/// <summary>
/// A flying blown kiss: shows a popup and an effect to whoever it hits, then disappears.
/// Not a projectile, so the hit does not shake the target's camera.
/// </summary>
[RegisterComponent]
public sealed partial class BlownKissComponent : Component
{
    [DataField(readOnly: true)]
    public EntProtoId? HitEffect = "EffectHearts";

    [ViewVariables]
    public EntityUid? Shooter;
}
