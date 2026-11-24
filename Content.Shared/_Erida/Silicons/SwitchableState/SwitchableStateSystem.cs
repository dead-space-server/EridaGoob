// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Silicons.Borgs;
using Robust.Shared.Utility;

namespace Content.Shared._Erida.Silicons.SwitchableState;

public sealed partial class ChangeSpriteSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly SharedHandheldLightSystem _lightSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgSwitchableStateComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BorgSwitchableStateComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BorgSwitchableStateComponent, SwitchStateActionEvent>(OnSwitchStateActionEvent);
        SubscribeLocalEvent<BorgSwitchableStateComponent, SwitchStateMessage>(OnSwitchStateMessage);
        SubscribeLocalEvent<BorgSwitchableStateComponent, SwitchStateDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<BorgSwitchableStateComponent, LightToggleEvent>(OnLightToggle);
        SubscribeLocalEvent<BorgSwitchableStateComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<BorgSwitchableStateComponent, MobStateChangedEvent>(OnMobstateChanged);
    }

    #region Base events

    private void OnMapInit(Entity<BorgSwitchableStateComponent> ent, ref MapInitEvent args)
    {
        if (!_action.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.ActionId))
            return;

        _action.SetEntityIcon((ent.Comp.ActionEntity.Value, null), ent.Owner);
        Dirty(ent);

        var userInterfaceComp = EnsureComp<UserInterfaceComponent>(ent);
        _ui.SetUi((ent, userInterfaceComp), SwitchStateUiKey.Key, new InterfaceData("SwitchableStateBoundUserInterface"));
    }

    private void OnShutdown(Entity<BorgSwitchableStateComponent> ent, ref ComponentShutdown args)
    {
        _action.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
        _ui.CloseUi(ent.Owner, SwitchStateUiKey.Key);
    }

    #endregion

    #region Events

    private void OnSwitchStateActionEvent(Entity<BorgSwitchableStateComponent> ent, ref SwitchStateActionEvent args)
    {
        if (args.Handled || !TryComp<UserInterfaceComponent>(ent, out var userInterfaceComp))
            return;

        args.Handled = true;

        if (!_ui.IsUiOpen((ent, userInterfaceComp), SwitchStateUiKey.Key, args.Performer))
            _ui.OpenUi((ent, userInterfaceComp), SwitchStateUiKey.Key, args.Performer);
        else
            _ui.CloseUi((ent, userInterfaceComp), SwitchStateUiKey.Key, args.Performer);
    }

    private void OnSwitchStateMessage(Entity<BorgSwitchableStateComponent> ent, ref SwitchStateMessage args)
    {
        _ui.CloseUi(ent.Owner, SwitchStateUiKey.Key, args.Actor);

        if (!CanSwitch(ent, args.Type))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            ent.Owner,
            ent.Comp.DoAfterDuration,
            new SwitchStateDoAfterEvent(args.Type),
            ent.Owner)
        {
            BreakOnDamage = false,
            BlockDuplicate = true,
            BreakOnHandChange = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(Entity<BorgSwitchableStateComponent> ent, ref SwitchStateDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!CanSwitch(ent, args.Type))
            return;

        args.Handled = true;

        ChangeType(ent, args.Type);
    }

    private void OnLightToggle(Entity<BorgSwitchableStateComponent> ent, ref LightToggleEvent args)
    {
        if (ent.Comp.CurrentType != BorgStateType.Base && TryComp<HandheldLightComponent>(ent.Owner, out var comp))
            _lightSystem.TurnOff((ent.Owner, comp), false);
    }

    private void OnRefreshSpeed(Entity<BorgSwitchableStateComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!ent.Comp.StatesWhiteList.TryGetValue(ent.Comp.CurrentType, out var vector))
            return;

        args.ModifySpeed(vector.X, vector.Y);
    }

    private void OnMobstateChanged(Entity<BorgSwitchableStateComponent> ent, ref MobStateChangedEvent args)
    {
        switch (args.NewMobState)
        {
            case MobState.Critical:
            case MobState.Dead:
                if (ent.Comp.StatesWhiteList.ContainsKey(BorgStateType.Wreck))
                    ChangeType(ent, BorgStateType.Wreck);
                break;
            case MobState.Alive when ent.Comp.CurrentType == BorgStateType.Wreck:
                ChangeType(ent, BorgStateType.Base);
                break;
        }
    }

    #endregion

    #region Functions

    /// <summary>
    /// Sets up the states of the borg subtype, or removes the component if the subtype has none.
    /// </summary>
    public void SetupStates(EntityUid uid, BorgTypePrototype type, BorgSubtypePrototype? subtype)
    {
        if (TryComp<BorgSwitchableStateComponent>(uid, out var existing) && existing.CurrentType != BorgStateType.Base)
            ChangeType((uid, existing), BorgStateType.Base);

        if (subtype is not { StatesWhiteList.Count: > 0 })
        {
            RemComp<BorgSwitchableStateComponent>(uid);
            return;
        }

        var comp = EnsureComp<BorgSwitchableStateComponent>(uid);
        comp.DoAfterDuration = subtype.DoAfterDuration;
        comp.StatesWhiteList = new Dictionary<BorgStateType, Vector2>(subtype.StatesWhiteList);
        comp.StatesWhiteList.TryAdd(BorgStateType.Base, Vector2.One);
        comp.BaseSprite = new SpriteSpecifier.Rsi(subtype.SpritePath, type.SpriteBodyState);

        Dirty(uid, comp);
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
    }

    private bool CanSwitch(Entity<BorgSwitchableStateComponent> ent, BorgStateType type)
    {
        return type != ent.Comp.CurrentType
            && ent.Comp.StatesWhiteList.ContainsKey(type)
            && _mobState.IsAlive(ent);
    }

    private void ChangeType(Entity<BorgSwitchableStateComponent> ent, BorgStateType newType)
    {
        SetRest(ent, newType != BorgStateType.Base);

        ent.Comp.CurrentType = newType;

        _appearance.SetData(ent, SwitchStateVisuals.Key, newType);

        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(ent.Owner);
        Dirty(ent);
    }

    private void SetRest(Entity<BorgSwitchableStateComponent> ent, bool isActive)
    {
        if (TryComp<SpriteMovementComponent>(ent.Owner, out var spriteMovement))
        {
            spriteMovement.IsEnabled = !isActive;
            Dirty(ent.Owner, spriteMovement);
        }

        if (isActive && TryComp<HandheldLightComponent>(ent.Owner, out var comp))
            _lightSystem.TurnOff((ent.Owner, comp));
    }

    #endregion
}
