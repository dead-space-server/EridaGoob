// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-only AND GPL-3.0-only

using System.Numerics;
using Content.Shared._ADT.Posing;
using Content.Shared._Erida.Posing;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._Erida.Posing;

public sealed partial class PosingSystem : SharedPosingSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private const string PosingContext = "posing";
    private const float LerpDuration = 0.15f;
    private const float ReturnDuration = 0.2f;

    private readonly Dictionary<EntityUid, PoseLerp> _interpolationState = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PosingComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
        SubscribeLocalEvent<PosingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PosingComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        var posing = _input.Contexts.New(PosingContext, "common");
        posing.AddFunction(ContentKeyFunctions.TogglePosing);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetUp);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetDown);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetLeft);
        posing.AddFunction(ContentKeyFunctions.PosingOffsetRight);
        posing.AddFunction(ContentKeyFunctions.PosingRotatePositive);
        posing.AddFunction(ContentKeyFunctions.PosingRotateNegative);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _input.Contexts.Remove(PosingContext);
        _interpolationState.Clear();
    }

    private void OnAfterHandleState(Entity<PosingComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.Posing && !_interpolationState.ContainsKey(ent))
            _interpolationState[ent] = new PoseLerp(ent.Comp.CurrentOffset, ent.Comp.CurrentAngle);
    }

    private void OnShutdown(Entity<PosingComponent> ent, ref ComponentShutdown args)
    {
        if (_interpolationState.Remove(ent) && !TerminatingOrDeleted(ent))
            ResetSprite(ent);

        if (_playerManager.LocalEntity == ent.Owner && _input.Contexts.ActiveContext.Name == PosingContext)
            _input.Contexts.SetActiveContext(ent.Comp.DefaultInputContext);
    }

    private void OnPlayerDetached(Entity<PosingComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        if (_input.Contexts.ActiveContext.Name == PosingContext)
            _input.Contexts.SetActiveContext(ent.Comp.DefaultInputContext);
    }

    protected override void ClientTogglePosing(EntityUid uid, PosingComponent posing)
    {
        base.ClientTogglePosing(uid, posing);

        // Reprediction replays the toggle until the server confirms it, which would restart the animation every tick.
        if (!_timing.IsFirstTimePredicted || !posing.Posing)
            return;

        _interpolationState[uid] = new PoseLerp(posing.CurrentOffset, posing.CurrentAngle);
        ApplySprite((uid, posing), posing.CurrentOffset, posing.CurrentAngle);
    }

    private void ApplySprite(Entity<PosingComponent> ent, Vector2 offset, Angle angle)
    {
        _sprite.SetOffset(ent.Owner, ent.Comp.DefaultOffset + offset);
        _sprite.SetRotation(ent.Owner, Angle.FromDegrees(ent.Comp.DefaultAngle) + angle);
    }

    private void ResetSprite(Entity<PosingComponent> ent)
    {
        ApplySprite(ent, Vector2.Zero, Angle.Zero);
    }

    // Other animations overwrite the sprite offset and rotation, so the pose is reapplied every frame.
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        UpdateInputContext();

        foreach (var (uid, state) in _interpolationState)
        {
            if (!TryComp<PosingComponent>(uid, out var posing))
            {
                _interpolationState.Remove(uid);
                continue;
            }

            if (posing.Posing)
                UpdatePosing((uid, posing), state, frameTime);
            else
                UpdateReturning((uid, posing), state, frameTime);
        }
    }

    /// <summary>
    /// Matches the input context to the predicted pose, which also undoes a toggle the server rejected.
    /// </summary>
    private void UpdateInputContext()
    {
        if (_playerManager.LocalEntity is not { } local
            || !TryComp<PosingComponent>(local, out var posing))
            return;

        var active = _input.Contexts.ActiveContext.Name;
        if (posing.Posing && active == posing.DefaultInputContext)
            _input.Contexts.SetActiveContext(PosingContext);
        else if (!posing.Posing && active == PosingContext)
            _input.Contexts.SetActiveContext(posing.DefaultInputContext);
    }

    private void UpdatePosing(Entity<PosingComponent> ent, PoseLerp state, float frameTime)
    {
        var posing = ent.Comp;
        if (state.IsReturning)
            state.Reset(posing.CurrentOffset, posing.CurrentAngle);

        if (state.TargetOffset != posing.CurrentOffset || state.TargetAngle != posing.CurrentAngle)
        {
            state.TargetOffset = posing.CurrentOffset;
            state.TargetAngle = posing.CurrentAngle;
            state.LerpTime = 0f;
        }

        state.LerpTime += frameTime;
        var t = Math.Min(1f, state.LerpTime / LerpDuration);

        if (t >= 1f)
        {
            state.Offset = state.TargetOffset;
            state.Angle = state.TargetAngle;
        }
        else
        {
            state.Offset = Vector2.Lerp(state.Offset, state.TargetOffset, t);
            state.Angle = Angle.FromDegrees(MathHelper.Lerp(state.Angle.Degrees, state.TargetAngle.Degrees, t));
        }

        ApplySprite(ent, state.Offset, state.Angle);
    }

    private void UpdateReturning(Entity<PosingComponent> ent, PoseLerp state, float frameTime)
    {
        if (!state.IsReturning)
        {
            state.IsReturning = true;
            state.LerpTime = 0f;
            state.TargetOffset = Vector2.Zero;
            state.TargetAngle = Angle.Zero;
        }

        state.LerpTime += frameTime;
        var t = Math.Min(1f, state.LerpTime / ReturnDuration);

        if (t >= 1f)
        {
            _interpolationState.Remove(ent);
            ResetSprite(ent);
            return;
        }

        state.Offset = Vector2.Lerp(state.Offset, state.TargetOffset, t);
        state.Angle = Angle.FromDegrees(MathHelper.Lerp(state.Angle.Degrees, state.TargetAngle.Degrees, t));
        ApplySprite(ent, state.Offset, state.Angle);
    }

    private sealed class PoseLerp(Vector2 offset, Angle angle)
    {
        public Vector2 Offset = offset;
        public Angle Angle = angle;
        public Vector2 TargetOffset = offset;
        public Angle TargetAngle = angle;
        public float LerpTime;
        public bool IsReturning;

        public void Reset(Vector2 newOffset, Angle newAngle)
        {
            Offset = TargetOffset = newOffset;
            Angle = TargetAngle = newAngle;
            LerpTime = 0f;
            IsReturning = false;
        }
    }
}
