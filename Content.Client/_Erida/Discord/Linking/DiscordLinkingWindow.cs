// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Erida.Stylesheets;
using Content.Client._Erida.UserInterface.Controls;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Erida.Discord.Linking;

/// <summary>
/// Shows the one-time code the player sends to the Discord bot to link their account.
/// </summary>
public sealed class DiscordLinkingWindow : EridaWindow
{
    public readonly RichTextLabel Label;
    public readonly ShapedButton CopyButton;
    public readonly ShapedButton LinkButton;

    public DiscordLinkingWindow()
    {
        Title = Loc.GetString("rmc-ui-link-discord-account");
        MinWidth = 420;

        Label = new RichTextLabel { HorizontalExpand = true };

        var body = new ShapedPanel
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
        };
        body.AddStyleClass(EridaStyleClass.Section);
        body.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(14, 16),
            Children = { Label },
        });

        CopyButton = CreateButton("rmc-ui-link-discord-account-copy", null, EridaStyleClass.ButtonFilled);
        LinkButton = CreateButton("rmc-ui-link-discord-account-open-channel", null, EridaStyleClass.ButtonTonal);

        Contents.AddChild(body);
        Contents.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            Children = { CopyButton, LinkButton },
        });
    }
}
