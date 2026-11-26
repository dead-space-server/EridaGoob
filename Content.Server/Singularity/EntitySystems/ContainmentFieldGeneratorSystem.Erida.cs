// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Erida.Maps;
using Content.Shared.Singularity.Components;
using Robust.Server.Physics;

namespace Content.Server.Singularity.EntitySystems;

public sealed partial class ContainmentFieldGeneratorSystem
{
    [Dependency] private readonly GridSplitAnchorSystem _gridSplitAnchor = default!;

    /// <summary>
    /// Anchor changes during a split are skipped, so drop only the connections the split left across grids.
    /// </summary>
    private void OnGridSplit(ref GridSplitEvent args)
    {
        var query = EntityQueryEnumerator<ContainmentFieldGeneratorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var generator, out var xform))
        {
            if (xform.GridUid is not { } gridUid || gridUid != args.Grid && Array.IndexOf(args.NewGrids, gridUid) < 0)
                continue;

            foreach (var (other, _) in generator.Connections.Values)
            {
                if (Transform(other).GridUid == gridUid)
                    continue;

                RemoveConnections((uid, generator));
                break;
            }
        }
    }
}
