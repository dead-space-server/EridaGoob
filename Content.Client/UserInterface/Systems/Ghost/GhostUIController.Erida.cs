// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.GameTicking;

namespace Content.Client.UserInterface.Systems.Ghost;

public sealed partial class GhostUIController
{
    private void OnRoundRestart(RoundRestartCleanupEvent ev, EntitySessionEventArgs args)
    {
        DeathTime = null;
        UpdateRespawn();
    }
}
