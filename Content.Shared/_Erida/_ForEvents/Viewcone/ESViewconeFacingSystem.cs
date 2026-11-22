// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Lost-Paradise-Project/Lost-Light/blob/d67c4d747060511f0162c6b8ce679b1895ff16c2/Content.Shared/Interaction/SharedInteractionSystem.cs
// Ported from https://github.com/Lost-Paradise-Project/Lost-Light/blob/d67c4d747060511f0162c6b8ce679b1895ff16c2/Content.Shared/Throwing/ThrowingSystem.cs

using Content.Shared._Erida._ForEvents.Viewcone.Components;
using Content.Shared._Erida.CCVar;
using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Shared._Erida._ForEvents.Viewcone;

/// <summary>
/// Turns viewcone holders towards what they throw and towards where they open the context menu,
/// so they can look behind themselves.
/// </summary>
public sealed class ESViewconeFacingSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly RotateToFaceSystem _rotate = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private EntityQuery<ESViewconeComponent> _viewconeQuery;
    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();

        _viewconeQuery = GetEntityQuery<ESViewconeComponent>();

        Subs.CVar(_cfg, ECCVars.ViewconeEnabled, value => _enabled = value, true);

        SubscribeLocalEvent<ThrowEvent>(OnThrow);

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.UseSecondary, new PointerInputCmdHandler(HandleUseSecondary))
            .Register<ESViewconeFacingSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<ESViewconeFacingSystem>();
    }

    /// <summary>
    /// Whether the entity turns towards the cursor on the context menu key instead of opening the menu.
    /// </summary>
    public bool FacesOnUseSecondary(EntityUid? user)
    {
        return _enabled && user is { } uid && _viewconeQuery.HasComp(uid);
    }

    private void OnThrow(ref ThrowEvent args)
    {
        if (!_enabled
            || args.User is not { } user
            || !_viewconeQuery.HasComp(user)
            || !TryComp<PhysicsComponent>(args.Thrown, out var physics)
            || physics.LinearVelocity.LengthSquared() < 0.01f)
            return;

        _rotate.TryFaceAngle(user, physics.LinearVelocity.ToWorldAngle());
    }

    /// <summary>
    /// The client opens the context menu on the same key and lets the input through only for viewcone holders.
    /// Never handles the input, so other binds still run.
    /// </summary>
    private bool HandleUseSecondary(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (session?.AttachedEntity is not { } user
            || !FacesOnUseSecondary(user)
            || _combat.IsInCombatMode(user)
            || !coords.IsValid(EntityManager)
            || !_actionBlocker.CanInteract(user, null))
            return false;

        var mapCoords = _xform.ToMapCoordinates(coords);
        if (mapCoords.MapId != Transform(user).MapID)
            return false;

        _rotate.TryFaceCoordinates(user, mapCoords.Position);
        return false;
    }
}
