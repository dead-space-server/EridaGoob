// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Erida.Humanoid;

/// <summary>
/// Brings engine color sliders in line with the other Erida sliders.
/// </summary>
public static class EridaColorSliders
{
    /// <summary>
    /// Vertical padding of the gradient, so it draws as a thin track like a plain slider instead of filling the row.
    /// </summary>
    private static readonly Thickness Track = new(0, 5);

    /// <summary>
    /// Thins the gradient tracks and hides the color name of every color selector under <paramref name="root"/>.
    /// </summary>
    public static void Restyle(Control root)
    {
        foreach (var child in root.Children)
        {
            switch (child)
            {
                case ColorableSlider { BackgroundStyleBoxOverride: StyleBoxTexture box }:
                    FlattenTrack(box);
                    break;
                case ColorSelectorSliders sliders:
                    HideDescription(sliders);
                    break;
            }

            Restyle(child);
        }
    }

    /// <summary>
    /// The gradient is drawn by a shader over the Nano slider texture with slanted ends and 12px patch margins,
    /// which keep it thick. A plain white texture keeps the shader gradient and lets the padding thin it.
    /// </summary>
    private static void FlattenTrack(StyleBoxTexture box)
    {
        box.Texture = Texture.White;
        box.SetPatchMargin(StyleBox.Margin.All, 0);
        box.Padding = Track;
    }

    /// <summary>
    /// The color name sits next to the mode selector in the selector's header row.
    /// </summary>
    private static void HideDescription(ColorSelectorSliders sliders)
    {
        if (sliders.ChildCount == 0
            || sliders.GetChild(0) is not { ChildCount: > 0 } body
            || body.GetChild(0) is not { ChildCount: > 1 } header
            || header.GetChild(1) is not Label description)
        {
            return;
        }

        description.Visible = false;
    }
}
