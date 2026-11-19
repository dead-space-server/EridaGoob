// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

namespace Content.Shared._Erida.Humanoid.Markings;

/// <summary>
/// How the second color of a <see cref="MarkingGradient"/> is laid over the sprite.
/// The order must match the <c>gradientStyle</c> branches in <c>marking_gradient.swsl</c> and is stored in the database.
/// </summary>
[Serializable, NetSerializable]
public enum MarkingGradientStyle : byte
{
    Ombre,
    Split,
    Underdye,
    Streaks,
    DipDye,
    FrontStrands,
}

/// <summary>
/// Blends a marking from its own color into <see cref="Color"/>.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial record struct MarkingGradient
{
    /// <summary>
    /// Separates the gradient from the colors in a database string. Colors are written as hex starting with '#'.
    /// </summary>
    public const char DbSeparator = '|';

    [DataField]
    public MarkingGradientStyle Style;

    /// <summary>
    /// The second color. The first one is the color of the marking's sprite.
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// Style-specific share of the accent from 0 to 1, such as the height of the transition or the density of strands.
    /// </summary>
    [DataField]
    public float Offset = 0.5f;

    public MarkingGradient()
    {
    }

    /// <summary>
    /// Returns a copy that is safe to render, since the values can come straight from a client.
    /// </summary>
    public MarkingGradient Sanitized()
    {
        return this with
        {
            Style = Enum.IsDefined(Style) ? Style : MarkingGradientStyle.Ombre,
            Color = HumanoidCharacterAppearance.ClampColor(Color),
            Offset = float.IsFinite(Offset) ? Math.Clamp(Offset, 0f, 1f) : 0.5f,
        };
    }

    /// <summary>
    /// Appends the gradient to a database string of colors, or returns the string as is if there is no gradient.
    /// </summary>
    public static string AppendToDb(string colors, MarkingGradient? gradient)
    {
        if (gradient is not { } value)
            return colors;

        var offset = value.Offset.ToString("0.###", CultureInfo.InvariantCulture);
        return $"{colors}{DbSeparator}{(byte) value.Style};{value.Color.ToHex()};{offset}";
    }

    /// <summary>
    /// Splits a database string into the colors before <see cref="DbSeparator"/> and the gradient after it.
    /// A missing or broken gradient gives null, so strings saved before gradients existed still load.
    /// </summary>
    public static string SplitFromDb(string input, out MarkingGradient? gradient)
    {
        gradient = null;

        var separator = input.IndexOf(DbSeparator);
        if (separator < 0)
            return input;

        var parts = input[(separator + 1)..].Split(';');
        if (parts.Length == 3
            && byte.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var style)
            && Color.TryFromHex(parts[1], out var color)
            && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var offset))
        {
            gradient = new MarkingGradient
            {
                Style = (MarkingGradientStyle) style,
                Color = color,
                Offset = offset,
            }.Sanitized();
        }

        return input[..separator];
    }
}
