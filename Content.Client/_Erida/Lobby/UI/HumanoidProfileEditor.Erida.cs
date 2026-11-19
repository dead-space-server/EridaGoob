// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void RemoveForeignMarkings()
    {
        if (Profile == null)
            return;

        Profile = Profile.WithCharacterAppearance(_markingManager.WithoutForeignMarkings(Profile.Appearance, Profile.Species));
        UpdateMarkings();
        ReloadPreview();
    }
}
