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
        // The owner-only component and the attach can arrive in either order.
        SubscribeLocalEvent<InactivePersonalityComponent, LocalPlayerAttachedEvent>((_, _, _) => AddEffects());
        SubscribeLocalEvent<InactivePersonalityComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<InactivePersonalityComponent, LocalPlayerDetachedEvent>((_, _, _) => RemoveEffects());
        SubscribeLocalEvent<InactivePersonalityComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(EntityUid uid, InactivePersonalityComponent component, ComponentStartup args)
    {
        if (uid == _player.LocalEntity)
            AddEffects();
    }

    private void OnShutdown(EntityUid uid, InactivePersonalityComponent component, ComponentShutdown args)
    {
        if (uid == _player.LocalEntity)
            RemoveEffects();
    }

    private void AddEffects()
    {
        if (_observing)
            return;
        _observing = true;
        _effects ??= [ new BlindOverlay(), new BlurryVisionOverlay(), new FlashOverlay(),
            new DrunkOverlay(), new RainbowOverlay(), new DrowsinessOverlay(), new PersonalityUnavailableOverlay() ];
        foreach (var effect in _effects)
            _overlays.AddOverlay(effect);
    }

    private void RemoveEffects()
    {
        _observing = false;
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
