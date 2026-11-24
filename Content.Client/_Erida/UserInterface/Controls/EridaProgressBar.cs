// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Erida.Stylesheets;
using Robust.Client.UserInterface;

namespace Content.Client._Erida.UserInterface.Controls;

/// <summary>
/// Thin pill-shaped progress bar: a track with a fill that grows from the left.
/// </summary>
public sealed class EridaProgressBar : Control
{
    private readonly ShapedPanel _fill;
    private float _value;

    /// <summary>
    /// Filled share of the bar, from 0 to 1.
    /// </summary>
    public float Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, 0f, 1f);
            InvalidateArrange();
        }
    }

    public EridaProgressBar()
    {
        MinHeight = 6;

        var track = new ShapedPanel();
        track.AddStyleClass(EridaStyleClass.ProgressTrack);

        _fill = new ShapedPanel();
        _fill.AddStyleClass(EridaStyleClass.ProgressFill);

        AddChild(track);
        AddChild(_fill);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        foreach (var child in Children)
        {
            var width = child == _fill ? finalSize.X * _value : finalSize.X;
            child.Arrange(UIBox2.FromDimensions(Vector2.Zero, new Vector2(width, finalSize.Y)));
        }

        return finalSize;
    }
}
