// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Erida.CCVar;

public sealed partial class ECCVars
{
    public static readonly CVarDef<string> ApiToken =
        CVarDef.Create("eapi.key", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<string> ApiUrl =
        CVarDef.Create("eapi.url", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Seconds between attempts to reconnect to the API while it is inactive.
    /// </summary>
    public static readonly CVarDef<float> ApiHealthCheckInterval =
        CVarDef.Create("eapi.health_check_interval", 30f, CVar.SERVERONLY);
}
