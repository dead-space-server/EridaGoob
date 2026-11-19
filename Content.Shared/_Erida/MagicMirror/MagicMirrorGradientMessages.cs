// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Erida.Humanoid.Markings;
using Content.Shared.DoAfter;
using Content.Shared.MagicMirror;
using Robust.Shared.Serialization;

namespace Content.Shared._Erida.MagicMirror;

/// <summary>
/// Changes the colors and gradient of a hair slot together, so a change to one never cancels the do-after of the other.
/// </summary>
[Serializable, NetSerializable]
public sealed class MagicMirrorChangeGradientMessage(
    MagicMirrorCategory category,
    List<Color> colors,
    MarkingGradient? gradient,
    int slot) : BoundUserInterfaceMessage
{
    public MagicMirrorCategory Category { get; } = category;
    public List<Color> Colors { get; } = colors;
    public MarkingGradient? Gradient { get; } = gradient;
    public int Slot { get; } = slot;
}

[Serializable, NetSerializable]
public sealed partial class MagicMirrorChangeGradientDoAfterEvent : DoAfterEvent
{
    public MagicMirrorCategory Category;
    public int Slot;
    public List<Color> Colors = [];
    public MarkingGradient? Gradient;

    public override DoAfterEvent Clone() => this;
}
