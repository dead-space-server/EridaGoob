// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Erida.Stylesheets;
using Content.Client._Erida.UserInterface.Controls;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Erida.Lobby.UI;

/// <summary>
/// Asks whether to save the edited character before leaving the setup screen.
/// Replaces <see cref="Content.Client.Lobby.UI.CharacterSetupGuiSavePanel"/> with the same buttons.
/// </summary>
public sealed class EridaSavePanel : EridaWindow
{
    public ShapedButton SaveButton { get; }

    public ShapedButton NoSaveButton { get; }

    public ShapedButton CancelButton { get; }

    public EridaSavePanel()
    {
        Title = Loc.GetString("character-setup-gui-save-panel-title");
        SetWidth = 420;

        var text = new Label
        {
            Text = Loc.GetString("erida-save-panel-text"),
            Margin = new Thickness(6, 0, 6, 4),
        };
        text.AddStyleClass(EridaStyleClass.TextMuted);

        CancelButton = CreateButton("character-setup-gui-save-panel-cancel", null, EridaStyleClass.ButtonGhost);
        NoSaveButton = CreateButton("character-setup-gui-save-panel-nosave", null, EridaStyleClass.ButtonTonal);
        NoSaveButton.AddStyleClass(StyleClass.Negative);
        SaveButton = CreateButton("character-setup-gui-save-panel-save", "save", EridaStyleClass.ButtonFilled);

        CancelButton.OnPressed += _ => Close();

        Contents.AddChild(text);
        Contents.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            Children = { CancelButton, NoSaveButton, SaveButton },
        });
    }
}
