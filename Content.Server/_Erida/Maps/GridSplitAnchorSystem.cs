// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Shared.Map.Components;

namespace Content.Server._Erida.Maps;

/// <summary>
/// Tells the anchor changes made by a grid split apart from real ones.
/// The split moves every anchored entity by unanchoring it and anchoring it again on the new grid,
/// which raises regular <see cref="AnchorStateChangedEvent"/>s.
/// </summary>
public sealed class GridSplitAnchorSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private List<Entity<MapGridComponent>> _grids = [];

    /// <summary>
    /// Whether the entity's anchor state is changing only because a grid split is moving it.
    /// While the split moves an entity, its tile exists on both the old and the new grid.
    /// </summary>
    public bool IsSplitMove(TransformComponent xform)
    {
        if (xform.GridUid is not { } gridUid || xform.MapUid is not { } mapUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        if (!_map.TryGetTileRef(gridUid, grid, xform.Coordinates, out var tile) || tile.Tile.IsEmpty)
            return false;

        var worldPos = _transform.ToMapCoordinates(xform.Coordinates).Position;
        _grids.Clear();
        _map.FindGridsIntersecting(mapUid, Box2.CenteredAround(worldPos, new Vector2(0.1f)), ref _grids, includeMap: false);

        foreach (var other in _grids)
        {
            if (other.Owner != gridUid && _map.TryGetTileRef(other, other.Comp, worldPos, out var otherTile) && !otherTile.Tile.IsEmpty)
                return true;
        }

        return false;
    }
}
