// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Erida.Lobby.UI;
using Content.Client._Erida.UserInterface.Controls;
using Content.Client.Info;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Lobby.UI;

public sealed partial class LobbyGui
{
    public ShapedTextureRect Background => EridaLobby.Background;
    public Label ServerName => EridaLobby.ServerName;

    /// <summary>
    /// Both Erida side panels share one fixed width, so the width vanilla applies from
    /// server.lobby_right_panel_width goes to a control that isn't shown.
    /// </summary>
    public Control RightSide { get; } = new();

    public EridaCharacterPanel CharacterPreview => EridaLobby.CharacterPreview;
    public Control CharacterSetupState => EridaLobby.CharacterSetupState;

    /// <summary>
    /// Vanilla vote popups go here and are never shown, <see cref="EridaVoteList"/> draws the votes instead.
    /// </summary>
    public BoxContainer VoteContainer { get; } = new();

    public ChatBox Chat => EridaLobby.Chat;

    public ShapedButton ReadyButton => EridaLobby.ReadyButton;
    public ShapedButton ObserveButton => EridaLobby.ObserveButton;
    public ShapedButton AHelpButton => EridaLobby.AHelpButton;
    public ShapedButton OptionsButton => EridaLobby.OptionsButton;
    public ShapedButton LeaveButton => EridaLobby.LeaveButton;

    public Label StartTime => EridaLobby.StartTime;
    public Label StationTime => EridaLobby.StationTime;

    /// <summary>
    /// Server currency is not shown in the Erida lobby, Goob still writes the balance here.
    /// </summary>
    public Label Balance { get; } = new();

    public ServerInfo ServerInfo => EridaLobby.ServerInfo;
    public RichTextLabel PlaytimeComment => EridaLobby.PlaytimeComment;
    public RichTextLabel LobbySong => EridaLobby.LobbySong;
    public ShapedButton PreviousSongButton => EridaLobby.PreviousSongButton;
    public ShapedButton NextSongButton => EridaLobby.NextSongButton;
    public ShapedButton PauseSongButton => EridaLobby.PauseSongButton;
    public ShapedButton PreviousBackgroundButton => EridaLobby.PreviousBackgroundButton;
    public ShapedButton NextBackgroundButton => EridaLobby.NextBackgroundButton;
    public RichTextLabel LobbyBackground => EridaLobby.LobbyBackground;
    public RichTextLabel LobbyMessageLabel => EridaLobby.LobbyMessageLabel;
}
