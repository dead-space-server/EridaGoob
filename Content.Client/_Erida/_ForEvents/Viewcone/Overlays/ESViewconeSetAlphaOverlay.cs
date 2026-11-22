// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Lost-Paradise-Project/Lost-Light/blob/d67c4d747060511f0162c6b8ce679b1895ff16c2/Content.Client/_ES/Viewcone/Overlays/ESViewconeSetAlphaOverlay.cs

using Content.Client._Erida._ForEvents.Viewcone.ComponentTree;
using Content.Client.Eye;
using Content.Shared._Erida._ForEvents.Viewcone;
using Content.Shared._Erida._ForEvents.Viewcone.Components;
using Content.Shared.Movement.Pulling.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client._Erida._ForEvents.Viewcone.Overlays;

/// <summary>
/// Before entities are drawn, sets the alpha of every <see cref="ESViewconeOccludableComponent"/> in view
/// depending on whether it is inside the cone. <see cref="ESViewconeResetAlphaOverlay"/> restores it afterwards.
/// </summary>
public sealed class ESViewconeSetAlphaOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly ESViewconeOverlayManagementSystem _cone;
    private readonly ESViewconeAngleSystem _angle;
    private readonly ESViewconeOccludableTreeSystem _tree;
    private readonly TransformSystem _xform;
    private readonly SpriteSystem _sprite;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    private Entity<EyeComponent, ESViewconeComponent>? _nextEye;

    public ESViewconeSetAlphaOverlay()
    {
        IoCManager.InjectDependencies(this);

        _cone = _ent.System<ESViewconeOverlayManagementSystem>();
        _angle = _ent.System<ESViewconeAngleSystem>();
        _tree = _ent.System<ESViewconeOccludableTreeSystem>();
        _xform = _ent.System<TransformSystem>();
        _sprite = _ent.System<SpriteSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _nextEye = null;

        if (args.Viewport.Eye == null)
            return false;

        var enumerator = _ent.AllEntityQueryEnumerator<LerpingEyeComponent, EyeComponent, ESViewconeComponent>();
        while (enumerator.MoveNext(out var uid, out _, out var eye, out var viewcone))
        {
            if (args.Viewport.Eye != eye.Eye)
                continue;

            _nextEye = (uid, eye, viewcone);
            break;
        }

        return _nextEye != null;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_nextEye is not { } next)
            return;

        var (ent, eye, cone) = next;

        var eyePos = _xform.GetWorldPosition(ent);
        // ViewAngle includes the eye rotation, the cone itself doesn't.
        var eyeRot = cone.ViewAngle - eye.Rotation;

        // Whatever we pull stays visible.
        var pulled = _ent.GetComponentOrNull<PullerComponent>(_player.LocalEntity)?.Pulling;

        var radConeAngle = MathHelper.DegreesToRadians(_angle.GetModifiedViewconeAngle((ent, cone)));
        var radConeFeather = MathHelper.DegreesToRadians(cone.ConeFeather);
        var ignoreRadius = _angle.GetModifiedConeIgnoreRadius((ent, cone));

        _cone.CachedBaseAlphas.Clear();
        foreach (var entry in _tree.QueryAabb(args.MapId, args.WorldBounds))
        {
            var (comp, xform) = entry;
            var uid = entry.Uid;

            if (uid == pulled
                || comp.Source == ent
                || !comp.OccludeIfAnchored && xform.Anchored
                || !_ent.TryGetComponent<SpriteComponent>(uid, out var sprite)
                || !sprite.Visible) // hidden by another system, e.g. equipped items
                continue;

            var dist = _xform.GetWorldPosition(xform) - eyePos;
            var angleDist = Angle.ShortestDistance(dist.ToWorldAngle(), eyeRot);

            var baseAlpha = sprite.Color.A;
            var angleAlpha = (float) Math.Clamp(Math.Abs(angleDist.Theta) - radConeAngle * 0.5f + radConeFeather * 0.5f, 0f, radConeFeather) / radConeFeather;
            var distAlpha = Math.Clamp(dist.Length() - ignoreRadius + cone.ConeIgnoreFeather * 0.5f, 0f, cone.ConeIgnoreFeather) / cone.ConeIgnoreFeather;
            var targetAlpha = Math.Max(1f - angleAlpha, 1f - distAlpha);

            _cone.CachedBaseAlphas.Add(((uid, sprite), baseAlpha));

            // Visible is toggled too, overlays like status icons skip invisible sprites.
            var alpha = (comp.Inverted ? 1f - targetAlpha : targetAlpha) * (comp.OverrideBaseAlpha ? 1f : baseAlpha);
            _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(alpha));
            _sprite.SetVisible((uid, sprite), alpha > 0f);
        }
    }
}
