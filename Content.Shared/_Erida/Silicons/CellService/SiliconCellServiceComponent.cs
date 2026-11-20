// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Erida.Silicons.CellService;

/// <summary>
/// Power cell in <see cref="CellSlot"/> can only be serviced by someone else, and is shown on examine while the lock is open.
/// </summary>
[RegisterComponent]
public sealed partial class SiliconCellServiceComponent : Component
{
    [DataField]
    public string CellSlot = "cell_slot";
}
