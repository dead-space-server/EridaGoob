// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

using Content.Shared._Erida.SplitPersonality;
using Content.Shared._Shitmed.Medical.Surgery.Consciousness.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client._Erida.SplitPersonality;

/// <summary>No free viewpoint while the shared senses are unavailable.</summary>
public sealed class PersonalityUnavailableOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public PersonalityUnavailableOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = int.MaxValue;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var player = _player.LocalEntity;
        if (!_entities.TryGetComponent<EyeComponent>(player, out var eye) || args.Viewport.Eye != eye.Eye
            || !_entities.TryGetComponent<InactivePersonalityComponent>(player, out var observer))
            return false;
        return observer.Body is not {} body
            || !_entities.TryGetComponent<MobStateComponent>(body, out var mob) || mob.CurrentState != MobState.Alive
            || _entities.HasComponent<SleepingComponent>(body)
            || _entities.TryGetComponent<ConsciousnessComponent>(body, out var consciousness) && !consciousness.IsConscious;
    }

    protected override void Draw(in OverlayDrawArgs args) => args.WorldHandle.DrawRect(args.WorldBounds, Color.Black);
}
