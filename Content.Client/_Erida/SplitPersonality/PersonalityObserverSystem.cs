// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

using Content.Client.Drowsiness;
using Content.Client.Drugs;
using Content.Client.Drunk;
using Content.Client.Eye.Blinding;
using Content.Client.Flash;
using Content.Shared._Erida.SplitPersonality;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Erida.SplitPersonality;

/// <summary>Reuses normal body effect renderers for a passive participant, without granting body input.</summary>
public sealed class PersonalityObserverSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlays = default!;
    [Dependency] private readonly ILightManager _lights = default!;
    private bool _observing;
    private Overlay[]? _effects;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InactivePersonalityComponent, LocalPlayerDetachedEvent>((_, _, _) =>
        {
            RemoveEffects();
            _observing = false;
        });
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        var observing = HasComp<InactivePersonalityComponent>(_player.LocalEntity);
        if (_observing == observing)
            return;
        _observing = observing;
        if (observing)
        {
            _effects ??= new Overlay[] { new BlindOverlay(), new BlurryVisionOverlay(), new FlashOverlay(),
                new DrunkOverlay(), new RainbowOverlay(), new DrowsinessOverlay(), new PersonalityUnavailableOverlay() };
            foreach (var effect in _effects)
                _overlays.AddOverlay(effect);
        }
        else
            RemoveEffects();
    }

    private void RemoveEffects()
    {
        if (_effects == null)
            return;
        _lights.Enabled = true;
        foreach (var effect in _effects)
        {
            if (_overlays.TryGetOverlay(effect.GetType(), out var registered) && ReferenceEquals(effect, registered))
                _overlays.RemoveOverlay(effect);
        }
    }

    public override void Shutdown()
    {
        RemoveEffects();
        base.Shutdown();
    }
}
