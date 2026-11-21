// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

using Content.Shared._Erida.SplitPersonality;
using Content.Shared.Humanoid.Markings;

namespace Content.Shared.Humanoid;

public abstract partial class SharedHumanoidAppearanceSystem
{
    public void ApplyPersonalityColors(EntityUid uid, HumanoidCharacterAppearance appearance, MarkingSet palette,
        HumanoidAppearanceComponent humanoid)
    {
        SetSkinColor(uid, appearance.SkinColor, false, humanoid: humanoid);
        humanoid.EyeColor = appearance.EyeColor;
        PersonalityColors.Apply(humanoid.MarkingSet, palette);
        Dirty(uid, humanoid);
        RaiseMarkingsChanged(uid);
    }
}
