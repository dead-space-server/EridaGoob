// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

using System.Linq;
using Content.Shared.Preferences;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void InitializeSplitPersonality()
    {
        AlternatePersonalityButton.OnItemSelected += args =>
        {
            if (Profile == null)
                return;

            Profile = new HumanoidCharacterProfile(Profile)
            {
                AlternatePersonalitySlot = args.Id < 0 ? null : args.Id,
            };
            AlternatePersonalityButton.SelectId(args.Id);
            SetDirty();
        };
    }

    private void UpdateSplitPersonality()
    {
        AlternatePersonalityButton.Clear();
        AlternatePersonalityButton.AddItem(Loc.GetString("split-personality-disabled"), -1);
        AlternatePersonalityButton.Disabled = Profile == null;
        var selected = -1;
        if (Profile != null && _preferencesManager.Preferences is { } preferences)
        {
            foreach (var (slot, character) in preferences.Characters.OrderBy(p => p.Key))
            {
                if (slot == CharacterSlot || character is not HumanoidCharacterProfile other || other.Species != Profile.Species)
                    continue;

                AlternatePersonalityButton.AddItem($"{slot + 1}: {other.Name}", slot);
                if (Profile.AlternatePersonalitySlot == slot)
                    selected = slot;
            }
        }

        if (selected == -1 && Profile?.AlternatePersonalitySlot is { } missing)
        {
            AlternatePersonalityButton.AddItem(Loc.GetString("split-personality-unavailable", ("slot", missing + 1)), missing);
            selected = missing;
        }
        AlternatePersonalityButton.SelectId(selected);
    }
}
