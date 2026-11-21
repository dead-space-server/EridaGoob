// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Silicons.Borgs;
using Content.Shared._Erida.Silicons.SwitchableState;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._Erida.Silicons.SwitchableState;

public sealed class SwitchableStateVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    private const string MovementLayer = "movement";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgSwitchableStateComponent, AppearanceChangeEvent>(OnAppearanceChange, after: [typeof(BorgSystem)]);
        SubscribeLocalEvent<BorgSwitchableStateComponent, AfterSpriteMovementUpdateEvent>(OnMovementUpdate);
    }

    private void OnAppearanceChange(Entity<BorgSwitchableStateComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateSprite((ent, ent.Comp, args.Sprite));
    }

    private void OnMovementUpdate(Entity<BorgSwitchableStateComponent> ent, ref AfterSpriteMovementUpdateEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        UpdateSprite((ent, ent.Comp, sprite));
    }

    private void UpdateSprite(Entity<BorgSwitchableStateComponent, SpriteComponent> ent)
    {
        if (ent.Comp1.BaseSprite is not SpriteSpecifier.Rsi rsi)
            return;

        var type = ent.Comp1.CurrentType;
        if (type != BorgStateType.Base)
        {
            _sprite.LayerSetRsiState((ent, ent.Comp2), BorgVisualLayers.Body, $"{rsi.RsiState}_{type.ToString().ToLowerInvariant()}");
            return;
        }

        var isMoving = TryComp<SpriteMovementComponent>(ent, out var movement) && movement.IsEnabled && movement.IsMoving;

        var bodyState = rsi.RsiState;
        if (isMoving && movement!.MovementLayers.TryGetValue(MovementLayer, out var data) && data.State != null)
            bodyState = data.State;

        _sprite.LayerSetRsiState((ent, ent.Comp2), BorgVisualLayers.Body, bodyState);

        UpdateLight(ent, isMoving, rsi.RsiState + "_moving_e");
    }

    private void UpdateLight(Entity<BorgSwitchableStateComponent, SpriteComponent> ent, bool isMoving, string movingState)
    {
        if (!TryComp<BorgChassisComponent>(ent, out var chassis))
            return;

        if (!_appearance.TryGetData<bool>(ent, BorgVisuals.HasPlayer, out var hasPlayer) || !hasPlayer)
            return;

        if (!_sprite.TryGetLayer((ent, ent.Comp2), BorgVisualLayers.Light, out var layer, false))
            return;

        var state = isMoving && layer.ActualRsi?.TryGetState(movingState, out _) == true
            ? movingState
            : chassis.HasMindState;

        _sprite.LayerSetRsiState((ent, ent.Comp2), BorgVisualLayers.Light, state);
    }
}
