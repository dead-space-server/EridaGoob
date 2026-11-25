// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Erida.RCD;
using Content.Shared.RCD.Components;
using Robust.Shared.Timing;

namespace Content.Client.RCD;

public sealed partial class RCDConstructionGhostSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private RCDPipeLayerSystem _rcdPipeLayer = default!;

    /// <summary>
    /// Syncs the pipe layer picked by the placement ghost and returns the prototype to show for it.
    /// </summary>
    private string? UpdatePipeLayer(Entity<RCDComponent> rcd, string? proto)
    {
        if (_placementManager.CurrentPermission?.MobUid != rcd.Owner
            || _placementManager.CurrentMode is not AlignRCDConstruction { PipeLayer: { } layer })
            return _rcdPipeLayer.GetLayerPrototype(proto, rcd.Comp.PipeLayer);

        if (_timing.IsFirstTimePredicted && rcd.Comp.PipeLayer != layer)
            RaisePredictiveEvent(new RCDConstructionGhostPipeLayerEvent(GetNetEntity(rcd), layer));

        return _rcdPipeLayer.GetLayerPrototype(proto, layer);
    }
}
