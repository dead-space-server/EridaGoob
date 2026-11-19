// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Erida.Humanoid.Markings;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidCharacterAppearance
{
    [DataField]
    public MarkingGradient? HairGradient { get; set; }

    [DataField]
    public MarkingGradient? FacialHairGradient { get; set; }

    public HumanoidCharacterAppearance WithHairGradient(MarkingGradient? gradient)
    {
        return new(this) { HairGradient = gradient };
    }

    public HumanoidCharacterAppearance WithFacialHairGradient(MarkingGradient? gradient)
    {
        return new(this) { FacialHairGradient = gradient };
    }

    /// <summary>
    /// Copies the gradients onto an appearance built by the constructor, which doesn't take them.
    /// </summary>
    private HumanoidCharacterAppearance KeepGradients(HumanoidCharacterAppearance appearance)
    {
        appearance.HairGradient = HairGradient;
        appearance.FacialHairGradient = FacialHairGradient;
        return appearance;
    }

    private bool GradientsEqual(HumanoidCharacterAppearance other)
    {
        return HairGradient.Equals(other.HairGradient) && FacialHairGradient.Equals(other.FacialHairGradient);
    }
}
