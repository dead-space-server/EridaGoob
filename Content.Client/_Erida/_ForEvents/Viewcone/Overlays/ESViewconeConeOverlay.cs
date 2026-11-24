// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Lost-Paradise-Project/Lost-Light/blob/d67c4d747060511f0162c6b8ce679b1895ff16c2/Content.Client/_ES/Viewcone/Overlays/ESViewconeConeOverlay.cs

using Content.Client.Eye;
using Content.Shared._Erida._ForEvents.Viewcone;
using Content.Shared._Erida._ForEvents.Viewcone.Components;
using Content.Shared._Erida.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Erida._ForEvents.Viewcone.Overlays;

/// <summary>
/// Renders the darkened area outside of the cone, without alpha modulation.
/// </summary>
public sealed class ESViewconeConeOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly ESViewconeAngleSystem _angle;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private static readonly ProtoId<ShaderPrototype> ShaderPrototype = "Viewcone";
    private readonly ShaderInstance _viewconeShader;

    private const float BaselineGrainMult = 0.35f;
    private const float MaxDarkenAmount = 0.5f;

    private float _grainMult = BaselineGrainMult;
    private float _darkenAmount;

    private Entity<EyeComponent, ESViewconeComponent>? _eyeEntity;
    private float _coneAngle;
    private float _coneFeather;
    private float _coneIgnoreRadius;
    private float _coneIgnoreFeather;

    public ESViewconeConeOverlay()
    {
        IoCManager.InjectDependencies(this);

        _angle = _ent.System<ESViewconeAngleSystem>();

        _viewconeShader = _proto.Index(ShaderPrototype).InstanceUnique();
        ZIndex = -6;

        _cfg.OnValueChanged(ECCVars.ViewconeDisableGrain, disabled => _grainMult = disabled ? 0f : BaselineGrainMult, true);
        _cfg.OnValueChanged(ECCVars.ViewconeOcclusionOpacity, opacity => _darkenAmount = opacity * MaxDarkenAmount, true);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _eyeEntity = null;

        // There is no way to get the entity back from an IEye. LerpingEye limits this to the eyes being rendered.
        var enumerator = _ent.AllEntityQueryEnumerator<LerpingEyeComponent, EyeComponent, ESViewconeComponent>();
        while (enumerator.MoveNext(out var uid, out _, out var eye, out var viewcone))
        {
            if (args.Viewport.Eye != eye.Eye)
                continue;

            _coneAngle = _angle.GetModifiedViewconeAngle((uid, viewcone));
            // A zero feather breaks the shader for a zero-angle cone.
            _coneFeather = _coneAngle <= 0f ? 0.01f : viewcone.ConeFeather;
            _coneIgnoreRadius = (_angle.GetModifiedConeIgnoreRadius((uid, viewcone)) - viewcone.ConeIgnoreFeather) * 50f;
            _coneIgnoreFeather = Math.Max(viewcone.ConeIgnoreFeather * 200f, 8f);
            _eyeEntity = (uid, eye, viewcone);
            break;
        }

        return _eyeEntity != null;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || _eyeEntity is not { } eyeEntity)
            return;

        var worldHandle = args.WorldHandle;

        _viewconeShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _viewconeShader.SetParameter("Zoom", eyeEntity.Comp1.Zoom.X);
        _viewconeShader.SetParameter("ViewAngle", (float) eyeEntity.Comp2.ViewAngle.Theta);
        _viewconeShader.SetParameter("ConeAngle", _coneAngle);
        _viewconeShader.SetParameter("ConeFeather", _coneFeather);
        _viewconeShader.SetParameter("ConeIgnoreRadius", _coneIgnoreRadius);
        _viewconeShader.SetParameter("ConeIgnoreFeather", _coneIgnoreFeather);
        _viewconeShader.SetParameter("GrainMult", _grainMult);
        _viewconeShader.SetParameter("DarkenAmount", _darkenAmount);

        worldHandle.UseShader(_viewconeShader);
        worldHandle.DrawRect(args.WorldBounds, Color.White);
        worldHandle.UseShader(null);
        _eyeEntity = null;
    }
}
