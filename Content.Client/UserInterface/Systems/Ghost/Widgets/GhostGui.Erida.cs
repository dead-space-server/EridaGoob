// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client.UserInterface.Systems.Ghost.Widgets;

public sealed partial class GhostGui
{
    private int _shownSeconds = -1;

    private void UpdateRespawnButton()
    {
        var seconds = 0;
        if (_respawnTime is { } respawnTime && _gameTiming.CurTime < respawnTime)
            seconds = (int) Math.Ceiling((respawnTime - _gameTiming.CurTime).TotalSeconds);

        if (seconds == _shownSeconds)
            return;

        _shownSeconds = seconds;
        GhostRespawnButton.Disabled = seconds > 0;
        GhostRespawnButton.Text = seconds > 0
            ? Loc.GetString("ghost-gui-respawn-button-denied", ("time", $"{seconds / 60}:{seconds % 60:D2}"))
            : Loc.GetString("ghost-gui-respawn-button-allowed");
    }
}
