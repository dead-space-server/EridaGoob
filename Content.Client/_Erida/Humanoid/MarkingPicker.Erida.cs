// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Erida.Humanoid;
using Content.Shared._Erida.Humanoid.Markings;
using Content.Shared.Humanoid.Markings;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Humanoid;

public sealed partial class MarkingPicker
{
    private bool _populateQueued;

    /// <summary>
    /// Loading a profile changes the species, sex and markings one by one, so the tabs are rebuilt once on the next frame.
    /// </summary>
    private void QueuePopulate()
    {
        _populateQueued = true;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_populateQueued)
            Populate("");
    }

    private void BuildTab(int index)
    {
        if (index >= 0 && index < _categoryTabs.Count)
            _categoryTabs[index].EnsureBuilt();
    }

    private void MarkingGradientChange(MarkingCategories category, MarkingPrototype prototype, MarkingGradient? gradient)
    {
        var index = _currentMarkings.FindIndexOf(category, prototype.ID);
        if (index < 0)
            return;

        var marking = new Marking(_currentMarkings.Markings[category][index]) { Gradient = gradient };
        _currentMarkings.Replace(category, index, marking);
        OnMarkingColorChange?.Invoke(_currentMarkings);
    }

    /// <summary>
    /// Swaps the first color of the marking with the second color of its gradient.
    /// </summary>
    private void MarkingGradientSwap(MarkingCategories category, MarkingPrototype prototype)
    {
        var index = _currentMarkings.FindIndexOf(category, prototype.ID);
        if (index < 0)
            return;

        var marking = new Marking(_currentMarkings.Markings[category][index]);
        if (!MarkingGradientControl.TrySwap(marking))
            return;

        _currentMarkings.Replace(category, index, marking);
        OnMarkingColorChange?.Invoke(_currentMarkings);

        foreach (var tab in _categoryTabs)
        {
            tab.RefreshItems();
        }
    }

    private sealed partial class CategoryTab
    {
        public void MarkingGradientChange(MarkingPrototype prototype, MarkingGradient? gradient)
        {
            _picker.MarkingGradientChange(_category, prototype, gradient);
        }

        public void MarkingGradientSwap(MarkingPrototype prototype)
        {
            _picker.MarkingGradientSwap(_category, prototype);
        }
    }

    private sealed partial class MarkingItem
    {
        private MarkingGradientControl? _gradientControl;

        private void AddGradientControl(Marking marking)
        {
            _gradientControl = new MarkingGradientControl { Margin = new Thickness(8, 2, 0, 2) };
            _gradientControl.SetGradient(marking.Gradient, MarkingGradientControl.Roots(marking));
            _gradientControl.GradientChanged += gradient => _tab.MarkingGradientChange(_prototype, gradient);
            _gradientControl.SwapPressed += () => _tab.MarkingGradientSwap(_prototype);
            _colorsContainer.AddChild(_gradientControl);
        }

        private void UpdateGradientControl(Marking marking)
        {
            _gradientControl?.SetGradient(marking.Gradient, MarkingGradientControl.Roots(marking));
        }
    }
}
