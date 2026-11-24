// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client._Erida.Lobby;
using Content.Shared._Erida.CCVar;
using Content.Shared.GameTicking.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Options.UI.Tabs;

public sealed partial class MiscTab
{
    private void AddLobbyBackgroundOptions()
    {
        EridaTheme.AddOptions(Control);
        Control.AddOptionCheckBox(ECCVars.LobbyBackgroundRandom, LobbyBackgroundRandomCheckBox);

        var backgrounds = EridaLobbyBackgrounds.GetAvailable(_prototypeManager);
        LobbyBackgroundDropDown.Visible = backgrounds.Count > 0;

        if (backgrounds.Count == 0)
            return;

        var options = backgrounds
            .Select(proto => new OptionDropDownCVar<string>.ValueOption(proto.ID, GetBackgroundOptionName(proto)))
            .ToList();

        Control.AddOptionDropDown(ECCVars.LobbyBackground, LobbyBackgroundDropDown, options);
    }

    /// <summary>
    /// Option buttons show plain text, so the color markup of unknown titles and artists is dropped.
    /// </summary>
    private static string GetBackgroundOptionName(LobbyBackgroundPrototype proto)
    {
        var name = Loc.GetString(proto.Animated ? "ui-options-lobby-background-item-animated" : "ui-options-lobby-background-item",
            ("title", EridaLobbyBackgrounds.Localize(proto.Name, "lobby-state-background-unknown-title")),
            ("artist", EridaLobbyBackgrounds.Localize(proto.Artist, "lobby-state-background-unknown-artist")));

        return FormattedMessage.RemoveMarkupPermissive(name);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        // Reset, default and reload set the checkbox without raising OnToggled.
        LobbyBackgroundDropDown.Button.Disabled = LobbyBackgroundRandomCheckBox.Pressed;
    }
}
