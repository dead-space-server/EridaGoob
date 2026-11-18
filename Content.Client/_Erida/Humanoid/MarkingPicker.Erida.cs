// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

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
}
