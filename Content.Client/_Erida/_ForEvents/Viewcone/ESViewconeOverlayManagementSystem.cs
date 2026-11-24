// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Lost-Paradise-Project/Lost-Light/blob/d67c4d747060511f0162c6b8ce679b1895ff16c2/Content.Client/_ES/Viewcone/ESViewconeOverlayManagementSystem.cs
// Ported from https://github.com/Lost-Paradise-Project/Lost-Light/blob/d67c4d747060511f0162c6b8ce679b1895ff16c2/Content.Client/_Funkystation/Overlays/Viewcone/ViewconeBlindSystem.cs

using Content.Client._Erida._ForEvents.Viewcone.Overlays;
using Content.Client.Eye;
using Content.Shared._Erida._ForEvents.Viewcone.Components;
using Content.Shared._Erida.CCVar;
using Content.Shared.MouseRotator;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client._Erida._ForEvents.Viewcone;

/// <summary>
/// Adds and removes the viewcone overlays for the local player and lerps the view and cone angles they use.
/// </summary>
public sealed class ESViewconeOverlayManagementSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private const float LerpHalfLife = 0.1f;

    private ESViewconeConeOverlay _coneOverlay = default!;
    private ESViewconeSetAlphaOverlay _setAlphaOverlay = default!;
    private ESViewconeResetAlphaOverlay _resetAlphaOverlay = default!;
    private EntityQuery<MouseRotatorComponent> _mouseRotatorQuery;

    /// <summary>
    /// Filled by the set alpha overlay and cleared by the reset alpha overlay within the same frame,
    /// overlays always draw sequentially. Only sprites that were visible get here.
    /// </summary>
    [Access(typeof(ESViewconeSetAlphaOverlay), typeof(ESViewconeResetAlphaOverlay))]
    public List<(Entity<SpriteComponent> Ent, float BaseAlpha)> CachedBaseAlphas = new(128);

    public override void Initialize()
    {
        base.Initialize();

        _mouseRotatorQuery = GetEntityQuery<MouseRotatorComponent>();

        SubscribeLocalEvent<ESViewconeComponent, ComponentInit>(OnViewconeInit);
        SubscribeLocalEvent<ESViewconeComponent, ComponentShutdown>(OnViewconeShutdown);
        SubscribeLocalEvent<ESViewconeComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ESViewconeComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _coneOverlay = new();
        _setAlphaOverlay = new();
        _resetAlphaOverlay = new();

        Subs.CVar(_cfg, ECCVars.ViewconeEnabled, OnViewconeEnabledChanged);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        RemoveOverlays();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var lerp = 1f - MathF.Pow(2f, -(frameTime / LerpHalfLife));

        // LerpingEye limits this to the eyes actually being rendered.
        var enumerator = AllEntityQuery<LerpingEyeComponent, EyeComponent, ESViewconeComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out _, out var eye, out var viewcone, out var xform))
        {
            if (!viewcone.DesiredConeAngle.Equals(viewcone.CurrentConeAngle))
                viewcone.CurrentConeAngle = MathHelper.Lerp(viewcone.CurrentConeAngle, viewcone.DesiredConeAngle, lerp);

            var (position, rotation) = _xform.GetWorldPositionRotation(xform);
            var playerAngle = rotation;
            var desiredWasNull = viewcone.DesiredViewAngle == null;

            if (_mouseRotatorQuery.HasComp(uid))
            {
                var mousePos = _eye.PixelToMap(_input.MouseScreenPosition);
                if (mousePos.MapId != MapId.Nullspace)
                    playerAngle = (mousePos.Position - position).ToAngle() + Angle.FromDegrees(90);

                viewcone.LastMouseRotationAngle = playerAngle;
            }
            else if (viewcone.LastMouseRotationAngle != 0f)
            {
                // Mouse rotation was just disabled: keep its angle until the entity turns or moves.
                if (MathHelper.CloseToPercent(viewcone.LastWorldRotationAngle, playerAngle, .001d)
                    && viewcone.LastWorldPos == position)
                    playerAngle = viewcone.LastMouseRotationAngle;
                else
                    viewcone.LastMouseRotationAngle = 0f;
            }

            viewcone.LastWorldPos = position;
            viewcone.LastWorldRotationAngle = rotation;
            viewcone.DesiredViewAngle = playerAngle + eye.Rotation;

            if (desiredWasNull)
            {
                viewcone.ViewAngle = viewcone.DesiredViewAngle.Value;
                continue;
            }

            viewcone.ViewAngle = Angle.Lerp(viewcone.ViewAngle, viewcone.DesiredViewAngle.Value, lerp);
        }
    }

    private void OnViewconeEnabledChanged(bool enabled)
    {
        if (!enabled)
        {
            RemoveOverlays();
            return;
        }

        if (HasComp<ESViewconeComponent>(_playerManager.LocalEntity))
            AddOverlays();
    }

    private void OnPlayerAttached(Entity<ESViewconeComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        AddOverlays();
    }

    private void OnPlayerDetached(Entity<ESViewconeComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveOverlays();
    }

    private void OnViewconeInit(Entity<ESViewconeComponent> ent, ref ComponentInit args)
    {
        if (_playerManager.LocalEntity == ent.Owner)
            AddOverlays();
    }

    private void OnViewconeShutdown(Entity<ESViewconeComponent> ent, ref ComponentShutdown args)
    {
        if (_playerManager.LocalEntity == ent.Owner)
            RemoveOverlays();
    }

    private void AddOverlays()
    {
        if (!_cfg.GetCVar(ECCVars.ViewconeEnabled))
            return;

        _overlayMan.AddOverlay(_coneOverlay);
        _overlayMan.AddOverlay(_setAlphaOverlay);
        _overlayMan.AddOverlay(_resetAlphaOverlay);
    }

    private void RemoveOverlays()
    {
        _overlayMan.RemoveOverlay(_coneOverlay);
        _overlayMan.RemoveOverlay(_setAlphaOverlay);
        _overlayMan.RemoveOverlay(_resetAlphaOverlay);
    }
}
