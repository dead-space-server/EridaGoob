// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;

namespace Content.Client.Light.EntitySystems;

public sealed class PlanetLightSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    /// <summary>
    /// Enables / disables the ambient occlusion overlay.
    /// </summary>
    public bool AmbientOcclusion
    {
        get => _ambientOcclusion;
        set
        {
            if (_ambientOcclusion == value)
                return;

            _ambientOcclusion = value;

            if (value)
            {
                _overlayMan.AddOverlay(new AmbientOcclusionOverlay());
            }
            else
            {
                _overlayMan.RemoveOverlay<AmbientOcclusionOverlay>();
            }
        }
    }

    private bool _ambientOcclusion;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetClearColorEvent>(OnClearColor);

        _cfgManager.OnValueChanged(CCVars.AmbientOcclusion, OnAmbientOcclusionChanged, true);

        _overlayMan.AddOverlay(new BeforeLightTargetOverlay());
        _overlayMan.AddOverlay(new RoofOverlay(EntityManager));
        _overlayMan.AddOverlay(new TileEmissionOverlay(EntityManager));
        _overlayMan.AddOverlay(new LightBlurOverlay());
        _overlayMan.AddOverlay(new SunShadowOverlay());
        _overlayMan.AddOverlay(new AfterLightTargetOverlay());
    }

    private void OnClearColor(ref GetClearColorEvent ev)
    {
        ev.Color = Color.Transparent;
    }

    private void OnAmbientOcclusionChanged(bool value)
    {
        AmbientOcclusion = value;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfgManager.UnsubValueChanged(CCVars.AmbientOcclusion, OnAmbientOcclusionChanged);
        _overlayMan.RemoveOverlay<BeforeLightTargetOverlay>();
        _overlayMan.RemoveOverlay<RoofOverlay>();
        _overlayMan.RemoveOverlay<TileEmissionOverlay>();
        _overlayMan.RemoveOverlay<LightBlurOverlay>();
        _overlayMan.RemoveOverlay<SunShadowOverlay>();
        _overlayMan.RemoveOverlay<AfterLightTargetOverlay>();

        if (_overlayMan.TryGetOverlay<AmbientOcclusionOverlay>(out var ambientOcclusion))
        {
            ambientOcclusion.Dispose();
            _overlayMan.RemoveOverlay<AmbientOcclusionOverlay>();
        }
    }
}