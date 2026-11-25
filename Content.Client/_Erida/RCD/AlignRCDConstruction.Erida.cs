// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared._Erida.RCD;
using Content.Shared.Atmos.Components;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;

namespace Content.Client.RCD;

public sealed partial class AlignRCDConstruction
{
    [Dependency] private IEyeManager _eyeManager = default!;

    private const float MouseDeadzoneRadius = 0.25f;
    private const float GuideRadius = 0.1f;
    private const float GuideOffset = 0.21875f;
    private static readonly Color GuideColor = new(0, 0, 0.5785f);

    /// <summary>
    /// The pipe layer under the cursor, or null if the placed prototype has no layer alternatives.
    /// </summary>
    public AtmosPipeLayer? PipeLayer { get; private set; }

    private void UpdatePipeLayer(EntityUid gridUid)
    {
        if (!SupportsPipeLayers())
        {
            PipeLayer = null;
            return;
        }

        var mouseCoordsDiff = _unalignedMouseCoords.Position - MouseCoords.Position;

        if (mouseCoordsDiff.Length() <= MouseDeadzoneRadius)
        {
            PipeLayer = AtmosPipeLayer.Primary;
            return;
        }

        var gridRotation = _transformSystem.GetWorldRotation(gridUid);
        var direction = (new Angle(mouseCoordsDiff) + _eyeManager.CurrentEye.Rotation + gridRotation + Math.PI / 2).GetCardinalDir();

        PipeLayer = direction is Direction.North or Direction.East
            ? AtmosPipeLayer.Secondary
            : AtmosPipeLayer.Tertiary;
    }

    private void DrawPipeLayerGuides(in OverlayDrawArgs args)
    {
        if (PipeLayer == null
            || pManager.PlacementType != PlacementManager.PlacementTypes.None
            || _transformSystem.GetGrid(MouseCoords) is not { } gridUid
            || !_entityManager.TryGetComponent<MapGridComponent>(gridUid, out var grid))
            return;

        var gridRotation = _transformSystem.GetWorldRotation(gridUid);
        var worldPosition = _mapSystem.LocalToWorld(gridUid, grid, MouseCoords.Position);
        var direction = (_eyeManager.CurrentEye.Rotation + gridRotation + Math.PI / 2).GetCardinalDir();
        var multi = direction is Direction.North or Direction.South ? -1f : 1f;
        var offset = gridRotation.RotateVec(new Vector2(multi * GuideOffset, GuideOffset));

        args.WorldHandle.DrawCircle(worldPosition, GuideRadius, GuideColor);
        args.WorldHandle.DrawCircle(worldPosition + offset, GuideRadius, GuideColor);
        args.WorldHandle.DrawCircle(worldPosition - offset, GuideRadius, GuideColor);
    }

    private bool SupportsPipeLayers()
    {
        return pManager.CurrentPermission is { IsTile: false, EntityType: { } proto }
            && _entityManager.System<RCDPipeLayerSystem>().TryGetLayerPrototype(proto, AtmosPipeLayer.Primary, out _);
    }
}
