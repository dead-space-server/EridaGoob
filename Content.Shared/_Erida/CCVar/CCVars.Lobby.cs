// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Erida.CCVar;

public sealed partial class ECCVars
{
    /// <summary>
    ///     Whether the lobby shows the background picked by the server for the round.
    /// </summary>
    public static readonly CVarDef<bool> LobbyBackgroundRandom =
        CVarDef.Create("erida.ui.lobby_background_random", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Lobby background prototype shown while <see cref="LobbyBackgroundRandom"/> is off.
    /// </summary>
    public static readonly CVarDef<string> LobbyBackground =
        CVarDef.Create("erida.ui.lobby_background", "LuaMSky2", CVar.CLIENTONLY | CVar.ARCHIVE);
}
