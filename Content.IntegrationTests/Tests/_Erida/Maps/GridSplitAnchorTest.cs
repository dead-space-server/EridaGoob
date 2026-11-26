// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Pair;
using Content.Server._Erida.Maps;
using Content.Server.Singularity.EntitySystems;
using Content.Shared.Singularity.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._Erida.Maps;

/// <summary>
/// Checks that entities moved by a grid split are not treated as unanchored by a destroyed tile.
/// </summary>
[TestFixture, TestOf(typeof(GridSplitAnchorSystem))]
public sealed class GridSplitAnchorTest
{
    private const string Cable = "CableHV";
    private const string CableStack = "CableHVStack1";
    private const string Generator = "ContainmentFieldGenerator";

    // The cut leaves 7 tiles on the old grid and moves 5 onto the new one.
    private const int Length = 13;
    private const int CutX = 7;

    [Test]
    public async Task SplitKeepsMovedEntities()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await CreateRow(pair);
        var entMan = pair.Server.EntMan;
        var generators = entMan.System<ContainmentFieldGeneratorSystem>();
        List<string> errors = [];
        EntityUid cable = default;
        Entity<ContainmentFieldGeneratorComponent> gen1 = default, gen2 = default;

        await pair.Server.WaitPost(() =>
        {
            cable = SpawnAt(entMan, testMap, Cable, 12);
            gen1 = SpawnGenerator(entMan, testMap, 8);
            gen2 = SpawnGenerator(entMan, testMap, 11);
        });
        await pair.RunTicksSync(5);

        await pair.Server.WaitPost(() =>
        {
            generators.ReceivePower(gen1.Comp.PowerMinimum, gen1);
            if (!gen1.Comp.IsConnected)
                errors.Add("Generators did not connect before the split");

            Cut(entMan, testMap, CutX);
        });
        await pair.RunTicksSync(5);

        await pair.Server.WaitPost(() =>
        {
            if (!entMan.EntityExists(cable))
                errors.Add("Cable on the split grid was deleted");
            else if (entMan.GetComponent<TransformComponent>(cable).GridUid == testMap.Grid.Owner)
                errors.Add("Cable did not move onto the split grid");

            if (CountProto(entMan, CableStack) > 0)
                errors.Add("Split dropped a cable piece");

            if (!gen1.Comp.IsConnected || !gen2.Comp.IsConnected)
                errors.Add("Generators on the split grid lost their connection");
        });

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SplitBetweenGeneratorsDisconnects()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await CreateRow(pair);
        var entMan = pair.Server.EntMan;
        var generators = entMan.System<ContainmentFieldGeneratorSystem>();
        List<string> errors = [];
        Entity<ContainmentFieldGeneratorComponent> gen1 = default, gen2 = default;

        await pair.Server.WaitPost(() =>
        {
            gen1 = SpawnGenerator(entMan, testMap, 5);
            gen2 = SpawnGenerator(entMan, testMap, 9);
        });
        await pair.RunTicksSync(5);

        await pair.Server.WaitPost(() =>
        {
            generators.ReceivePower(gen1.Comp.PowerMinimum, gen1);
            if (!gen1.Comp.IsConnected)
                errors.Add("Generators did not connect before the split");

            Cut(entMan, testMap, CutX);
        });
        await pair.RunTicksSync(5);

        await pair.Server.WaitPost(() =>
        {
            if (gen1.Comp.IsConnected || gen2.Comp.IsConnected)
                errors.Add("Generators split onto different grids stayed connected");
        });

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TileRemovalDropsCable()
    {
        await using var pair = await PoolManager.GetServerClient();
        var testMap = await CreateRow(pair);
        var entMan = pair.Server.EntMan;
        List<string> errors = [];
        EntityUid cable = default;

        await pair.Server.WaitPost(() =>
        {
            cable = SpawnAt(entMan, testMap, Cable, 0);
            Cut(entMan, testMap, 0);
        });
        await pair.RunTicksSync(5);

        await pair.Server.WaitPost(() =>
        {
            if (entMan.EntityExists(cable))
                errors.Add("Cable on a removed tile was not deleted");

            if (CountProto(entMan, CableStack) != 1)
                errors.Add("Cable on a removed tile did not drop a piece");
        });

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        await pair.CleanReturnAsync();
    }

    private static async Task<TestMapData> CreateRow(TestPair pair)
    {
        var testMap = await pair.CreateTestMap();
        var mapSys = pair.Server.System<SharedMapSystem>();

        await pair.Server.WaitPost(() =>
        {
            for (var x = 1; x < Length; x++)
            {
                mapSys.SetTile(testMap.Grid, new Vector2i(x, 0), testMap.Tile.Tile);
            }
        });

        return testMap;
    }

    private static EntityUid SpawnAt(IEntityManager entMan, TestMapData testMap, string proto, int x)
    {
        return entMan.SpawnEntity(proto, new EntityCoordinates(testMap.Grid, x + 0.5f, 0.5f));
    }

    private static Entity<ContainmentFieldGeneratorComponent> SpawnGenerator(IEntityManager entMan, TestMapData testMap, int x)
    {
        var uid = SpawnAt(entMan, testMap, Generator, x);
        entMan.System<SharedTransformSystem>().AnchorEntity(uid);
        var generator = entMan.GetComponent<ContainmentFieldGeneratorComponent>(uid);
        generator.Enabled = true;
        return (uid, generator);
    }

    private static void Cut(IEntityManager entMan, TestMapData testMap, int x)
    {
        entMan.System<SharedMapSystem>().SetTile(testMap.Grid, new Vector2i(x, 0), Tile.Empty);
    }

    private static int CountProto(IEntityManager entMan, string proto)
    {
        return entMan.AllEntities<MetaDataComponent>().Count(ent => ent.Comp.EntityPrototype?.ID == proto);
    }
}
