// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.RCD.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Erida.RCD;

/// <summary>
/// Lets the RCD place pipes on the layer picked by the placement ghost,
/// using the layer alternatives of the built prototype.
/// </summary>
public sealed class RCDPipeLayerSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedAtmosPipeLayersSystem _pipeLayers = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<RCDConstructionGhostPipeLayerEvent>(OnGhostPipeLayer);
    }

    private void OnGhostPipeLayer(RCDConstructionGhostPipeLayerEvent ev, EntitySessionEventArgs args)
    {
        var uid = GetEntity(ev.NetEntity);

        if (args.SenderSession.AttachedEntity is not { } player
            || _hands.GetActiveItem(player) != uid
            || !TryComp<RCDComponent>(uid, out var rcd)
            || !Enum.IsDefined(ev.PipeLayer)
            || rcd.PipeLayer == ev.PipeLayer)
            return;

        rcd.PipeLayer = ev.PipeLayer;
        Dirty(uid, rcd);
    }

    /// <summary>
    /// Finds the alternative of an entity prototype for the given pipe layer.
    /// </summary>
    public bool TryGetLayerPrototype(string? proto, AtmosPipeLayer layer, out EntProtoId result)
    {
        result = default;

        if (proto == null
            || !_proto.TryIndex<EntityPrototype>(proto, out var entProto)
            || !entProto.TryComp<AtmosPipeLayersComponent>(out var layers, EntityManager.ComponentFactory))
            return false;

        return _pipeLayers.TryGetAlternativePrototype(layers, layer, out result);
    }

    /// <summary>
    /// Returns the prototype to build on the given layer, or the prototype itself if it has no layer alternatives.
    /// </summary>
    public string? GetLayerPrototype(string? proto, AtmosPipeLayer layer)
    {
        return TryGetLayerPrototype(proto, layer, out var alt) ? alt.Id : proto;
    }

    /// <summary>
    /// Returns the layer the RCD will build the prototype on. Prototypes without layer alternatives always use the primary layer.
    /// </summary>
    public AtmosPipeLayer GetPipeLayer(RCDComponent rcd, string? proto)
    {
        return TryGetLayerPrototype(proto, rcd.PipeLayer, out _) ? rcd.PipeLayer : AtmosPipeLayer.Primary;
    }

    /// <summary>
    /// Checks whether the entity is the prototype or one of its layer alternatives.
    /// </summary>
    public bool IsSamePrototype(EntityUid ent, string proto)
    {
        if (MetaData(ent).EntityPrototype?.ID is not { } id)
            return false;

        if (id == proto)
            return true;

        foreach (var layer in Enum.GetValues<AtmosPipeLayer>())
        {
            if (TryGetLayerPrototype(proto, layer, out var alt) && alt == id)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks whether the entity is on the layer the RCD will build the prototype on.
    /// </summary>
    public bool IsOnBuildLayer(EntityUid ent, RCDComponent rcd, string proto)
    {
        return !TryComp<AtmosPipeLayersComponent>(ent, out var layers)
            || layers.CurrentPipeLayer == GetPipeLayer(rcd, proto);
    }
}
