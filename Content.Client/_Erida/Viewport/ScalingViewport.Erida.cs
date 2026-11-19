// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Trauma-Station/Trauma-Station/blob/6c975b102d5fa17299c98a678330eb933527f1f8/Content.Client/Viewport/ScalingViewport.Trauma.cs

using Content.Client._Erida.MouseWheel;
using Robust.Client.UserInterface;

namespace Content.Client.Viewport;

public sealed partial class ScalingViewport
{
    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);

        if (args.Handled || MathHelper.CloseToPercent(0f, args.Delta.Y))
            return;

        _entityManager.SystemOrNull<MouseWheelZoomSystem>()?.HandleMouseWheel(args.Delta);
    }
}
