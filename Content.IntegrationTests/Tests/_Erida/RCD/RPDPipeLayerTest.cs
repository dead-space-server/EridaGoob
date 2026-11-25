// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared._Erida.RCD;
using Content.Shared.Atmos.Components;
using Content.Shared.RCD;
using Content.Shared.RCD.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Erida.RCD;

/// <summary>
/// The RPD must build pipes on the layer picked by the placement ghost.
/// </summary>
[TestOf(typeof(RCDPipeLayerSystem))]
public sealed class RPDPipeLayerTest : InteractionTest
{
    private static readonly EntProtoId RPDProtoId = "RPD";
    private static readonly ProtoId<RCDPrototype> PipeStraight = "PipeStraight";
    private static readonly EntProtoId PipePrimary = "GasPipeStraight";
    private static readonly EntProtoId PipeSecondary = "GasPipeStraightAlt1";
    private static readonly EntProtoId PipeTertiary = "GasPipeStraightAlt2";

    [Test]
    public async Task BuildsOnSelectedLayer()
    {
        var target = Transform.WithEntityId(new EntityCoordinates(SPlayer, new Vector2(0, 1)), MapData.Grid);
        await SetTile(PlatingRCD, SEntMan.GetNetCoordinates(target), MapData.Grid);

        var rpd = await PlaceInHands(RPDProtoId);
        await SetRpdProto(rpd, PipeStraight);

        await BuildOnLayer(rpd, AtmosPipeLayer.Primary, target);
        await AssertEntityLookup((PipePrimary, 1));

        await BuildOnLayer(rpd, AtmosPipeLayer.Secondary, target);
        await BuildOnLayer(rpd, AtmosPipeLayer.Tertiary, target);
        await AssertEntityLookup((PipePrimary, 1), (PipeSecondary, 1), (PipeTertiary, 1));

        // A pipe already on the selected layer blocks an identical one.
        await BuildOnLayer(rpd, AtmosPipeLayer.Secondary, target);
        await AssertEntityLookup((PipePrimary, 1), (PipeSecondary, 1), (PipeTertiary, 1));

        var secondary = await FindEntity(PipeSecondary);
        Assert.That(SEntMan.GetComponent<AtmosPipeLayersComponent>(secondary).CurrentPipeLayer, Is.EqualTo(AtmosPipeLayer.Secondary));
    }

    private async Task BuildOnLayer(NetEntity rpd, AtmosPipeLayer layer, EntityCoordinates target)
    {
        await Client.WaitPost(() => CEntMan.EntityNetManager.SendSystemNetworkMessage(new RCDConstructionGhostPipeLayerEvent(rpd, layer)));
        await RunTicks(5);
        Assert.That(SEntMan.GetComponent<RCDComponent>(ToServer(rpd)).PipeLayer, Is.EqualTo(layer));

        await Interact(null, target);
        await RunSeconds(2);
    }

    private async Task SetRpdProto(NetEntity rpd, ProtoId<RCDPrototype> protoId)
    {
        await UseInHand();
        await RunTicks(3);
        await SendBui(RcdUiKey.Key, new RCDSystemMessage(protoId), rpd);
        await CloseBui(RcdUiKey.Key, rpd);
    }
}
