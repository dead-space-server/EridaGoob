// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client._Shitcode.Wizard.MagicMirror;

public sealed partial class WizardMirrorWindow
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
