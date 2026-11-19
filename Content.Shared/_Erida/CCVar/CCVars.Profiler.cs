// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Erida.CCVar;

public sealed partial class ECCVars
{
    /// <summary>
    /// Keeps the engine profiler running and collects slow ticks and log errors for the host profiler window.
    /// </summary>
    public static readonly CVarDef<bool> ProfilerEnabled =
        CVarDef.Create("erida.profiler.enabled", true, CVar.SERVERONLY);

    /// <summary>
    /// A server frame longer than this many milliseconds is recorded as a slow tick.
    /// </summary>
    public static readonly CVarDef<float> ProfilerSlowTickMs =
        CVarDef.Create("erida.profiler.slow_tick_ms", 100f, CVar.SERVERONLY);
}
