// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Trauma-Station/Trauma-Station/blob/6c975b102d5fa17299c98a678330eb933527f1f8/Content.Trauma.Client/MouseWheel/MouseWheelSystem.cs

using System.Numerics;
using Content.Shared._Erida.CCVar;
using Content.Shared.Input;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Input;

namespace Content.Client._Erida.MouseWheel;

/// <summary>
/// Changes camera zoom by mouse wheel over the game viewport.
/// If <see cref="ContentKeyFunctions.ZoomMod"/> is bound, zooms only while it is held.
/// </summary>
public sealed class MouseWheelZoomSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IInputManager _inputMan = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private InputSystem _input = default!;

    private const float ZoomStep = 0.1f;

    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, ECCVars.MouseWheelZoom, x => _enabled = x, true);
    }

    public void HandleMouseWheel(Vector2 delta)
    {
        if (!_enabled)
            return;

        if (_inputMan.TryGetKeyBinding(ContentKeyFunctions.ZoomMod, out _)
            && _input.CmdStates.GetState(ContentKeyFunctions.ZoomMod) != BoundKeyState.Down)
            return;

        if (_player.LocalEntity is not { } player || !TryComp(player, out ContentEyeComponent? eye))
            return;

        RaisePredictiveEvent(new SharedContentEyeSystem.RequestTargetZoomEvent
        {
            TargetZoom = eye.TargetZoom - new Vector2(delta.Y * ZoomStep) / SharedContentEyeSystem.ZoomMod,
        });
    }
}
