// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Erida.Lobby;
using Content.Shared._Erida.CCVar;
using Content.Shared.GameTicking.Prototypes;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby;

public sealed partial class LobbyState
{
    private LobbyBackgroundAnimation? _backgroundAnimation;
    private string? _shownBackground;

    private void StartupErida()
    {
        Lobby!.PreviousSongButton.OnPressed += OnPreviousSongPressed;
        Lobby.NextSongButton.OnPressed += OnNextSongPressed;
        Lobby.PauseSongButton.OnPressed += OnPauseSongPressed;
        UpdatePauseSongButton();
        Lobby.PreviousBackgroundButton.OnPressed += OnPreviousBackgroundPressed;
        Lobby.NextBackgroundButton.OnPressed += OnNextBackgroundPressed;

        _cfg.OnValueChanged(ECCVars.LobbyBackgroundRandom, OnLobbyBackgroundOptionChanged);
        _cfg.OnValueChanged(ECCVars.LobbyBackground, OnLobbyBackgroundOptionChanged);

        if (GetEridaLobbyBackground() != null)
            UpdateLobbyBackground();
    }

    private void ShutdownErida()
    {
        Lobby!.PreviousSongButton.OnPressed -= OnPreviousSongPressed;
        Lobby.NextSongButton.OnPressed -= OnNextSongPressed;
        Lobby.PauseSongButton.OnPressed -= OnPauseSongPressed;
        Lobby.PreviousBackgroundButton.OnPressed -= OnPreviousBackgroundPressed;
        Lobby.NextBackgroundButton.OnPressed -= OnNextBackgroundPressed;

        _cfg.UnsubValueChanged(ECCVars.LobbyBackgroundRandom, OnLobbyBackgroundOptionChanged);
        _cfg.UnsubValueChanged(ECCVars.LobbyBackground, OnLobbyBackgroundOptionChanged);
        _backgroundAnimation?.Stop();
        _shownBackground = null;
    }

    private void OnLobbyBackgroundOptionChanged<T>(T value)
    {
        UpdateLobbyBackground();
    }

    /// <summary>
    /// The background picked in the options, or the one the server rolled while random backgrounds are on.
    /// </summary>
    private ProtoId<LobbyBackgroundPrototype>? GetEridaLobbyBackground()
    {
        return EridaLobbyBackgrounds.GetPicked(_cfg, _protoMan) ?? _gameTicker.LobbyBackground;
    }

    /// <summary>
    /// Shows a static or animated lobby background, animated ones are ported from Monolith.
    /// </summary>
    private void SetEridaLobbyBackground(LobbyBackgroundPrototype? proto)
    {
        // Lobby status updates come on every ready toggle, restarting would jump the animation back to frame 0.
        if (Lobby != null && proto != null && proto.ID == _shownBackground)
            return;

        _backgroundAnimation ??= new LobbyBackgroundAnimation();
        _backgroundAnimation.Stop();
        _shownBackground = null;

        if (Lobby == null)
            return;

        if (proto == null)
        {
            Lobby.Background.Texture = null;
            Lobby.Background.BlurTexture = null;
            return;
        }

        _shownBackground = proto.ID;
        var firstFrame = _backgroundAnimation.Start(proto);
        Lobby.Background.Texture = firstFrame
            ?? (_resourceCache.TryGetResource<TextureResource>(proto.Background, out var texture) ? texture.Texture : null);
        Lobby.Background.BlurTexture = firstFrame;
    }

    private void UpdateEridaLobbyBackground(float frameTime)
    {
        if (Lobby == null || _backgroundAnimation is not { Playing: true })
            return;

        if (_backgroundAnimation.Update(frameTime) is { } frame)
            Lobby.Background.Texture = frame;

        Lobby.Background.BlurTexture ??= _backgroundAnimation.FirstFrame;
    }

    private void OnPreviousBackgroundPressed(BaseButton.ButtonEventArgs args) => CycleEridaLobbyBackground(-1);

    private void OnNextBackgroundPressed(BaseButton.ButtonEventArgs args) => CycleEridaLobbyBackground(1);

    /// <summary>
    /// Switches to the previous or next lobby background and remembers it, like the Monolith lobby does.
    /// Picking one by hand turns the random server background off, same as choosing it in the options.
    /// </summary>
    private void CycleEridaLobbyBackground(int delta)
    {
        var available = EridaLobbyBackgrounds.GetAvailable(_protoMan);
        if (available.Count == 0)
            return;

        var current = GetEridaLobbyBackground();
        var index = available.FindIndex(proto => proto.ID == current);
        index = index < 0
            ? (delta > 0 ? 0 : available.Count - 1)
            : ((index + delta) % available.Count + available.Count) % available.Count;

        _cfg.SetCVar(ECCVars.LobbyBackgroundRandom, false);
        _cfg.SetCVar(ECCVars.LobbyBackground, available[index].ID);
        _cfg.SaveToFile();
    }

    private void OnPreviousSongPressed(BaseButton.ButtonEventArgs args) => _contentAudioSystem.SkipLobbyTrack(-1);

    private void OnNextSongPressed(BaseButton.ButtonEventArgs args) => _contentAudioSystem.SkipLobbyTrack(1);

    private void OnPauseSongPressed(BaseButton.ButtonEventArgs args)
    {
        _contentAudioSystem.ToggleLobbyMusicPause();
        UpdatePauseSongButton();
    }

    /// <summary>
    /// Shows the pause or play icon on the lobby music pause button, ported from Monolith.
    /// </summary>
    private void UpdatePauseSongButton()
    {
        if (Lobby == null)
            return;

        var paused = _contentAudioSystem.LobbyMusicPaused;
        Lobby.PauseSongButton.Icon = _resourceCache.GetResource<TextureResource>(paused
            ? "/Textures/Interface/AdminActions/play.png"
            : "/Textures/Interface/AdminActions/pause.png").Texture;
        Lobby.PauseSongButton.ToolTip = Loc.GetString(paused ? "ui-lobby-song-resume" : "ui-lobby-song-pause");
    }
}
