// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Trauma-Station/Trauma-Station/blob/6c975b102d5fa17299c98a678330eb933527f1f8/Content.Trauma.Common/CCVar/TraumaCVars.MouseWheel.cs

using Robust.Shared.Configuration;

namespace Content.Shared._Erida.CCVar;

public sealed partial class ECCVars
{
    /// <summary>
    /// Whether camera zoom can be changed by mouse wheel.
    /// </summary>
    public static readonly CVarDef<bool> MouseWheelZoom =
        CVarDef.Create("erida.mouse_wheel_zoom", true, CVar.ARCHIVE | CVar.CLIENTONLY);
}
