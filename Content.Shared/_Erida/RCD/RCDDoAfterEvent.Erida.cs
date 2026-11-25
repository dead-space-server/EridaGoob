// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos.Components;

namespace Content.Shared.RCD.Systems;

public sealed partial class RCDDoAfterEvent
{
    [DataField]
    public AtmosPipeLayer PipeLayer { get; set; }
}
