// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Stylesheets;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Funkystation.VendingMachines.UI;

public sealed partial class VendingMachineKeypadMenu
{
    [Dependency] private IResourceCache _resCache = default!;

    private const float SelectedNameWidth = 150f;

    private readonly Label _selectedNameLabel = new()
    {
        ClipText = true,
        Align = Label.AlignMode.Center,
        FontColorOverride = Color.FromHex("#33ff33"),
    };

    private void InitializeErida()
    {
        _selectedNameLabel.FontOverride = _resCache.NotoStack(size: 9);
        _selectedNameLabel.SetWidth = SelectedNameWidth;
        BufferLabel.Parent?.AddChild(_selectedNameLabel);
    }

    private void SubscribeSlot(VendingMachineGridSlot slot, int index)
    {
        slot.Pressed += () => OnSlotPressed(index);
    }

    private void OnSlotPressed(int index)
    {
        if (_showingFeedback)
            return;

        _bufferLetter = (char) ('A' + index / 10);
        OnNumberPressed(index % 10, 1f);
    }

    private void UpdateSelectedName()
    {
        var name = string.Empty;
        if (_bufferLetter is { } letter && _bufferNumber is { } number)
        {
            var index = (letter - 'A') * 10 + number;
            if (index >= 0 && index < _slots.Count)
                name = _slots[index].ShortName;
        }

        _selectedNameLabel.Text = name;
    }
}
