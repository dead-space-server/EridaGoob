// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._Erida.Atmos;

/// <summary>
/// Checks that splitting a grid moves the gas of the detached tiles onto the new grid.
/// </summary>
[TestFixture, TestOf(typeof(AtmosphereSystem))]
public sealed class GridSplitAtmosTest
{
    private const int Length = 5;
    private const int CutX = 2;
    private const float MolesPerTile = 100f;

    [Test]
    public async Task SplitKeepsAtmosphere()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var mapSys = entMan.System<SharedMapSystem>();
        var atmosSys = entMan.System<AtmosphereSystem>();

        var testMap = await pair.CreateTestMap();
        var grid = testMap.Grid;
        List<string> errors = [];

        await server.WaitPost(() =>
        {
            for (var x = 1; x < Length; x++)
            {
                mapSys.SetTile(grid, new Vector2i(x, 0), testMap.Tile.Tile);
            }

            var atmos = entMan.AddComponent<GridAtmosphereComponent>(grid);
            var overlay = entMan.GetComponent<GasTileOverlayComponent>(grid);
            var xform = entMan.GetComponent<TransformComponent>(grid);
            atmosSys.RunProcessingFull((grid, atmos, overlay, grid.Comp, xform), testMap.MapUid, atmosSys.AtmosTickRate);

            // Distinct amounts per tile, so a wrong index mapping changes the total.
            for (var x = 0; x < Length; x++)
            {
                if (atmosSys.GetTileMixture((grid, atmos, overlay), null, new Vector2i(x, 0)) is not { } air)
                {
                    errors.Add($"Tile {x} has no air after revalidation");
                    continue;
                }

                air.Clear();
                air.AdjustMoles(Gas.Oxygen, MolesPerTile * (x + 1));
            }

            mapSys.SetTile(grid, new Vector2i(CutX, 0), Tile.Empty);

            var newGrids = entMan.AllEntities<MapGridComponent>()
                .Where(ent => ent.Owner != grid.Owner && entMan.GetComponent<TransformComponent>(ent).MapID == testMap.MapId)
                .ToList();
            if (newGrids.Count != 1)
            {
                errors.Add($"Expected 1 split grid, got {newGrids.Count}");
                return;
            }

            var newGrid = newGrids[0];
            var newAtmos = entMan.GetComponent<GridAtmosphereComponent>(newGrid);
            var newMoles = newAtmos.Tiles.Values.Sum(tile => tile.Air?.TotalMoles ?? 0f);
            var oldMoles = mapSys.GetAllTiles(grid, grid.Comp)
                .Sum(tile => atmosSys.GetTileMixture((grid, atmos, overlay), null, tile.GridIndices)?.TotalMoles ?? 0f);

            var expected = MolesPerTile * (Length * (Length + 1) / 2 - (CutX + 1));
            if (!MathHelper.CloseTo(newMoles + oldMoles, expected, 0.01f))
                errors.Add($"Expected {expected} moles after split, got {newMoles} on the new grid and {oldMoles} on the old one");
        });

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }
}
