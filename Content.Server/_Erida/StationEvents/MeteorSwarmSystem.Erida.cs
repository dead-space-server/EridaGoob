// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Meteor spawn geometry, shared with the meteor shield coverage. Distances are beyond the radius of the station largest grid.
/// </summary>
public sealed partial class MeteorSwarmSystem
{
    public const float SpawnMargin = 50f;

    /// <summary>
    /// Meteors spawn uniformly from <see cref="SpawnMargin"/> to <see cref="SpawnMargin"/> + this.
    /// </summary>
    public const float SpawnDepth = 100f;

    /// <summary>
    /// Maximum sideways shift of the meteor path as a share of the grid radius.
    /// </summary>
    public const float SpawnSideShift = 1f / 3f;
}
