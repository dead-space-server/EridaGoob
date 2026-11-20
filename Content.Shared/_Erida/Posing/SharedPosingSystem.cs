// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-only AND GPL-3.0-only

using System.Numerics;
using Content.Shared._ADT.Posing;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage.Components;
using Content.Shared.Input;
using Content.Shared.Mobs;
using Content.Shared.Movement.Events;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._Erida.Posing;

public abstract partial class SharedPosingSystem : EntitySystem
{
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private IGameTiming _timing = default!;

    private const float OffsetSpeed = 0.2f;
    private const float AngleSpeed = 20f;

    private readonly Dictionary<(NetUserId User, BoundKeyFunction Function), (EntityUid Uid, float Angle, Vector2 Offset, GameTick Start, GameTick End)> _continuousInput = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PosingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<PosingComponent, DownedEvent>(OnDowned);
        SubscribeLocalEvent<PosingComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<PosingComponent, PlayerDetachedEvent>(OnPlayerDetached);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.TogglePosing, InputCmdHandler.FromDelegate(OnTogglePosingKey, handle: false))
            .Bind(ContentKeyFunctions.PosingOffsetRight, ContinuousHandler(ContentKeyFunctions.PosingOffsetRight, offset: new Vector2(OffsetSpeed, 0f)))
            .Bind(ContentKeyFunctions.PosingOffsetLeft, ContinuousHandler(ContentKeyFunctions.PosingOffsetLeft, offset: new Vector2(-OffsetSpeed, 0f)))
            .Bind(ContentKeyFunctions.PosingOffsetUp, ContinuousHandler(ContentKeyFunctions.PosingOffsetUp, offset: new Vector2(0f, OffsetSpeed)))
            .Bind(ContentKeyFunctions.PosingOffsetDown, ContinuousHandler(ContentKeyFunctions.PosingOffsetDown, offset: new Vector2(0f, -OffsetSpeed)))
            .Bind(ContentKeyFunctions.PosingRotatePositive, ContinuousHandler(ContentKeyFunctions.PosingRotatePositive, angle: -AngleSpeed))
            .Bind(ContentKeyFunctions.PosingRotateNegative, ContinuousHandler(ContentKeyFunctions.PosingRotateNegative, angle: AngleSpeed))
            .Register<SharedPosingSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<SharedPosingSystem>();
        _continuousInput.Clear();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_continuousInput.Count == 0)
            return;

        var tick = _timing.CurTick;
        var keepTicks = (uint) _timing.TickRate;
        foreach (var (key, input) in _continuousInput)
        {
            if (!TryComp<PosingComponent>(input.Uid, out var posing) || !posing.Posing)
            {
                _continuousInput.Remove(key);
                continue;
            }

            if (tick < input.Start)
                continue;

            if (tick >= input.End)
            {
                if (tick.Value - input.End.Value > keepTicks)
                    _continuousInput.Remove(key);

                continue;
            }

            if (input.Angle != 0f)
                TryAdjustPosingAngle(input.Uid, input.Angle * frameTime, posing);

            if (input.Offset != Vector2.Zero)
                TryAdjustPosingOffset(input.Uid, input.Offset * frameTime, posing);
        }
    }

    private InputCmdHandler ContinuousHandler(BoundKeyFunction function, float angle = 0f, Vector2 offset = default)
    {
        return InputCmdHandler.FromDelegate(
            session =>
            {
                if (session?.AttachedEntity is { } userUid && HasComp<PosingComponent>(userUid))
                    _continuousInput[(session.UserId, function)] = (userUid, angle, offset, _timing.CurTick, GameTick.MaxValue);
            },
            session =>
            {
                if (session == null || !_continuousInput.TryGetValue((session.UserId, function), out var input))
                    return;

                input.End = _timing.CurTick;
                _continuousInput[(session.UserId, function)] = input;
            },
            handle: false);
    }

    private void RemoveContinuousInput(EntityUid uid)
    {
        foreach (var (key, input) in _continuousInput)
        {
            if (input.Uid == uid)
                _continuousInput.Remove(key);
        }
    }

    private void OnPlayerDetached(Entity<PosingComponent> ent, ref PlayerDetachedEvent args)
    {
        RemoveContinuousInput(ent);
    }

    private void OnTogglePosingKey(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { } userUid || !TryComp<PosingComponent>(userUid, out var posing))
            return;

        if (posing.Posing || CanStartPosing(userUid))
            TogglePosing(userUid, posing);
    }

    private void OnUpdateCanMove(Entity<PosingComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.Posing)
            args.Cancel();
    }

    private void OnDowned(Entity<PosingComponent> ent, ref DownedEvent args)
    {
        if (ent.Comp.Posing)
            TogglePosing(ent, ent.Comp);
    }

    private void OnMobStateChanged(Entity<PosingComponent> ent, ref MobStateChangedEvent args)
    {
        if (ent.Comp.Posing)
            TogglePosing(ent, ent.Comp);
    }

    private void TogglePosing(EntityUid uid, PosingComponent posingComp)
    {
        posingComp.Posing = !posingComp.Posing;
        _actionBlocker.UpdateCanMove(uid);

        posingComp.CurrentAngle = Angle.Zero;
        posingComp.CurrentOffset = Vector2.Zero;

        if (!posingComp.Posing)
            RemoveContinuousInput(uid);

        ClientTogglePosing(uid, posingComp);
        Dirty(uid, posingComp);
    }

    private void TryAdjustPosingOffset(EntityUid uid, Vector2 offset, PosingComponent? posingComp = null)
    {
        if (!Resolve(uid, ref posingComp, false) || !posingComp.Posing)
            return;

        var previousOffset = posingComp.CurrentOffset;

        posingComp.CurrentOffset += offset;
        posingComp.CurrentOffset = Vector2.Clamp(posingComp.CurrentOffset, -posingComp.OffsetLimits, posingComp.OffsetLimits);

        if (posingComp.CurrentOffset.Equals(previousOffset))
            return;

        Dirty(uid, posingComp);
    }

    private void TryAdjustPosingAngle(EntityUid uid, float angle, PosingComponent? posingComp = null)
    {
        if (!Resolve(uid, ref posingComp, false) || !posingComp.Posing)
            return;

        var previousAngle = posingComp.CurrentAngle;

        var newAngle = posingComp.CurrentAngle.Degrees + angle;
        posingComp.CurrentAngle = Angle.FromDegrees(Math.Clamp(newAngle, -posingComp.AngleLimits, posingComp.AngleLimits));

        if (posingComp.CurrentAngle.Equals(previousAngle))
            return;

        Dirty(uid, posingComp);
    }

    protected virtual void ClientTogglePosing(EntityUid uid, PosingComponent posing)
    {
    }

    private bool CanStartPosing(EntityUid uid)
    {
        if (!_actionBlocker.CanConsciouslyPerformAction(uid))
            return false;

        if (TryComp<StaminaComponent>(uid, out var stamina) && stamina.Critical)
            return false;

        if (HasComp<StunnedComponent>(uid))
            return false;

        return !_standing.IsDown(uid);
    }
}
