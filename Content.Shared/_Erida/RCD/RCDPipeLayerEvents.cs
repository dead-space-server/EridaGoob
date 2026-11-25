// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._Erida.RCD;

/// <summary>
/// Raised predictively by the client when the RCD placement ghost moves to another pipe layer.
/// </summary>
[Serializable, NetSerializable]
public sealed class RCDConstructionGhostPipeLayerEvent(NetEntity netEntity, AtmosPipeLayer pipeLayer) : EntityEventArgs
{
    public NetEntity NetEntity { get; } = netEntity;
    public AtmosPipeLayer PipeLayer { get; } = pipeLayer;
}
