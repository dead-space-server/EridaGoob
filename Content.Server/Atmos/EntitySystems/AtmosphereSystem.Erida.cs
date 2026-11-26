// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    /// <summary>
    /// Offset from the split grid's tile indices to the same tiles on the grid it split from.
    /// The split moves the new grid origin next to its tiles, so the indices differ.
    /// </summary>
    private Vector2i GetSplitTileOffset(EntityUid oldGrid, EntityUid newGrid, MapGridComponent grid)
    {
        var (oldPos, oldRot) = _transformSystem.GetWorldPositionRotation(oldGrid);
        var offset = (-oldRot).RotateVec(_transformSystem.GetWorldPosition(newGrid) - oldPos) / grid.TileSize;
        return new Vector2i((int) MathF.Round(offset.X), (int) MathF.Round(offset.Y));
    }
}
