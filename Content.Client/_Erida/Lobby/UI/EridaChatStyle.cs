// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Erida.Stylesheets;
using Content.Client._Erida.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Chat.Controls;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Robust.Client.Graphics;

namespace Content.Client._Erida.Lobby.UI;

/// <summary>
/// Swaps the style classes of a vanilla <see cref="ChatBox"/> for Erida ones and puts its input into a pill.
/// </summary>
public static class EridaChatStyle
{
    public static void Apply(ChatBox chat)
    {
        chat.ChatWindowPanel.PanelOverride = new StyleBoxEmpty();

        var input = chat.ChatInput;
        input.RemoveStyleClass(ChatInputBox.StyleClassChatPanel);
        input.Margin = new Thickness(4, 2);

        input.Input.RemoveStyleClass(ChatInputBox.StyleClassChatLineEdit);
        input.Input.AddStyleClass(EridaStyleClass.ChatLineEdit);

        input.ChannelSelector.RemoveStyleClass(ChannelSelectorItemButton.StyleClassChatSelectorOptionButton);
        input.ChannelSelector.AddStyleClass(EridaStyleClass.ChatChip);

        input.FilterButton.RemoveStyleClass(ChatInputBox.StyleClassChatFilterOptionButton);
        input.FilterButton.AddStyleClass(EridaStyleClass.ChatIconButton);

        if (input.Parent is not { } parent)
            return;

        var index = input.GetPositionInParent();
        var pill = new ShapedPanel { HorizontalExpand = true };
        pill.AddStyleClass(EridaStyleClass.ChatInput);

        parent.RemoveChild(input);
        pill.AddChild(input);
        parent.AddChild(pill);
        pill.SetPositionInParent(index);
    }
}
