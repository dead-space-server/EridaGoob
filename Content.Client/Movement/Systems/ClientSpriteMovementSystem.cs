// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.Movement.Systems;

/// <summary>
/// Controls the switching of motion and standing still animation
/// </summary>
public sealed class ClientSpriteMovementSystem : SharedSpriteMovementSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private EntityQuery<SpriteComponent> _spriteQuery;

    public override void Initialize()
    {
        base.Initialize();

        _spriteQuery = GetEntityQuery<SpriteComponent>();

        SubscribeLocalEvent<SpriteMovementComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<SpriteMovementComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!_spriteQuery.TryGetComponent(ent, out var sprite) || !ent.Comp.IsEnabled) // Erida edit
            return;

        if (ent.Comp.IsMoving)
        {
            foreach (var (layer, state) in ent.Comp.MovementLayers)
            {
                if (HasState((ent.Owner, sprite), layer, state)) // Erida edit
                    continue;

                _sprite.LayerSetData((ent.Owner, sprite), layer, state);
            }
        }
        else
        {
            foreach (var (layer, state) in ent.Comp.NoMovementLayers)
            {
                if (HasState((ent.Owner, sprite), layer, state)) // Erida edit
                    continue;

                _sprite.LayerSetData((ent.Owner, sprite), layer, state);
            }
        }

        // Erida start
        var ev = new AfterSpriteMovementUpdateEvent(ent.Comp.IsMoving);
        RaiseLocalEvent(ent, ref ev);
    }

    private bool HasState(Entity<SpriteComponent> sprite, string key, PrototypeLayerData data)
    {
        if (data.State == null || data.RsiPath != null || data.TexturePath != null)
            return false;

        return _sprite.TryGetLayer(sprite.AsNullable(), key, out var layer, false) && layer.State == data.State;
    }
    // Erida end
}
