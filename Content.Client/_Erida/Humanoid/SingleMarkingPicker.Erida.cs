// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Erida.Humanoid;
using Content.Shared.Humanoid.Markings;

namespace Content.Client.Humanoid;

public sealed partial class SingleMarkingPicker
{
    /// <summary>
    ///     What happens if a marking's gradient is changed or swapped with its first color.
    ///     Sends a 'slot' number, and the marking with both its colors and its gradient.
    /// </summary>
    public Action<(int slot, Marking marking)>? OnGradientChanged;

    private MarkingGradientControl? _gradientControl;

    /// <summary>
    /// Adds the gradient editor under the color sliders. Windows that don't handle
    /// <see cref="OnGradientChanged"/>, such as the wizard's mirror, don't get it.
    /// </summary>
    private void AddGradientControl(Marking marking, MarkingPrototype proto)
    {
        _gradientControl = null;
        if (proto.ForcedColoring || OnGradientChanged == null)
            return;

        var control = new MarkingGradientControl();
        control.SetGradient(marking.Gradient, MarkingGradientControl.Roots(marking));
        control.GradientChanged += gradient =>
        {
            marking.Gradient = gradient;
            OnGradientChanged?.Invoke((_slot, marking));
        };
        control.SwapPressed += () =>
        {
            if (!MarkingGradientControl.TrySwap(marking))
                return;

            OnGradientChanged?.Invoke((_slot, marking));
            PopulateColors();
        };

        ColorSelectorContainer.AddChild(control);
        _gradientControl = control;
    }

    /// <summary>
    /// Keeps the gradient's color suggestions in step with the color sliders, which don't rebuild the editor.
    /// </summary>
    private void UpdateGradientRoots(Marking marking)
    {
        _gradientControl?.SetRoots(MarkingGradientControl.Roots(marking));
    }
}
