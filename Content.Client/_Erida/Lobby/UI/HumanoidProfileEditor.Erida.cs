// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid.Markings;
using Robust.Shared.Timing;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private bool _profilePreviewQueued;

    /// <summary>
    /// Reloads the preview once on the next frame. Color sliders fire on every mouse move, often several times
    /// a frame, and each reload rebuilds every marking layer of the dummy.
    /// </summary>
    private void QueueProfilePreview()
    {
        _profilePreviewQueued = true;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_profilePreviewQueued)
            return;

        _profilePreviewQueued = false;
        ReloadProfilePreview();
    }

    private void InitializeMarkingGradients()
    {
        HairStylePicker.OnGradientChanged += args =>
        {
            if (Profile is null || args.marking.MarkingColors.Count == 0)
                return;

            Profile = Profile.WithCharacterAppearance(Profile.Appearance
                .WithHairColor(args.marking.MarkingColors[0])
                .WithHairGradient(args.marking.Gradient));
            UpdateCMarkingsHair();
            QueueProfilePreview();
        };

        FacialHairPicker.OnGradientChanged += args =>
        {
            if (Profile is null || args.marking.MarkingColors.Count == 0)
                return;

            Profile = Profile.WithCharacterAppearance(Profile.Appearance
                .WithFacialHairColor(args.marking.MarkingColors[0])
                .WithFacialHairGradient(args.marking.Gradient));
            UpdateCMarkingsFacialHair();
            QueueProfilePreview();
        };

        CopySkinColorButton.OnPressed += _ => CopySkinColorToMarkings();
    }

    /// <summary>
    /// Recolors every sprite of every marking into the skin color, keeping each sprite's alpha.
    /// Hair and facial hair aren't markings in the profile, so they keep their colors.
    /// </summary>
    private void CopySkinColorToMarkings()
    {
        if (Profile is null)
            return;

        var skin = Profile.Appearance.SkinColor;
        var markings = new List<Marking>(Profile.Appearance.Markings.Count);
        foreach (var marking in Profile.Appearance.Markings)
        {
            var copy = new Marking(marking);
            for (var i = 0; i < copy.MarkingColors.Count; i++)
            {
                copy.SetColor(i, skin.WithAlpha(copy.MarkingColors[i].A));
            }

            markings.Add(copy);
        }

        Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithMarkings(markings));
        UpdateMarkings();
        QueueProfilePreview();
    }
}
