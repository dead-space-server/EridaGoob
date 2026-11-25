// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Erida.RCD;
using Content.Shared.Atmos.Components;
using Content.Shared.RCD.Systems;

namespace Content.Shared.RCD.Components;

public sealed partial class RCDComponent
{
    /// <summary>
    /// The pipe layer selected by the placement ghost. Used only for prototypes with layer alternatives.
    /// </summary>
    [AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(RCDSystem), typeof(RCDPipeLayerSystem))]
    public AtmosPipeLayer PipeLayer;
}
