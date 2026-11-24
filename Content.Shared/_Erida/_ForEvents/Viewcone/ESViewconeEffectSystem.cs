// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Lost-Paradise-Project/Lost-Light/blob/d67c4d747060511f0162c6b8ce679b1895ff16c2/Content.Shared/_ES/Viewcone/ESViewconeEffectSystem.cs

using System.Numerics;
using Content.Shared._Erida._ForEvents.Viewcone.Components;
using Content.Shared._Erida.CCVar;
using Content.Shared.Throwing;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Erida._ForEvents.Viewcone;

/// <summary>
/// Sends markers (footsteps, landed throws) to nearby viewcone holders, who spawn them clientside.
/// They are only shown outside of the holder's view.
/// </summary>
public sealed class ESViewconeEffectSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private static readonly EntProtoId FootstepEffect = "ESViewconeEffectFootstep";
    private static readonly EntProtoId LandEffect = "ESViewconeEffectAttack";

    /// <summary>
    /// Effects are only sent to viewcone holders within this range of the source.
    /// </summary>
    private const float ViewerRange = 20f;

    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, ECCVars.ViewconeEnabled, value => _enabled = value, true);

        SubscribeLocalEvent<ThrownItemComponent, LandEvent>(OnLand);
    }

    private void OnLand(Entity<ThrownItemComponent> ent, ref LandEvent args)
    {
        if (args.User == null)
            return;

        SpawnEffect(ent.Owner, LandEffect);
    }

    /// <summary>
    /// Called by the mover controller whenever a footstep sound plays.
    /// </summary>
    public void SpawnFootstep(EntityUid uid, Vector2 wishDir)
    {
        SpawnEffect(uid, FootstepEffect, wishDir.ToWorldAngle());
    }

    /// <summary>
    /// Sends the effect to every viewcone holder near the source, except the source itself.
    /// </summary>
    /// <param name="angleOverride">World rotation of the effect instead of the source's rotation.</param>
    public void SpawnEffect(EntityUid source, EntProtoId effect, Angle? angleOverride = null)
    {
        if (_net.IsClient || !_enabled)
            return;

        var (sourcePos, sourceRot) = _xform.GetWorldPositionRotation(source);
        var mapId = Transform(source).MapID;

        Filter? viewers = null;
        var query = EntityQueryEnumerator<ESViewconeComponent, ActorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var actor, out var xform))
        {
            if (uid == source
                || xform.MapID != mapId
                || (_xform.GetWorldPosition(xform) - sourcePos).LengthSquared() > ViewerRange * ViewerRange)
                continue;

            viewers ??= Filter.Empty();
            viewers.AddPlayer(actor.PlayerSession);
        }

        if (viewers == null)
            return;

        var ev = new ESViewconeEffectEvent(effect, new MapCoordinates(sourcePos, mapId), angleOverride ?? sourceRot);
        RaiseNetworkEvent(ev, viewers);
    }
}

/// <summary>
/// Tells a viewcone holder to spawn a clientside effect.
/// </summary>
[Serializable, NetSerializable]
public sealed class ESViewconeEffectEvent(EntProtoId effect, MapCoordinates coordinates, Angle rotation) : EntityEventArgs
{
    public readonly EntProtoId Effect = effect;
    public readonly MapCoordinates Coordinates = coordinates;
    public readonly Angle Rotation = rotation;
}
