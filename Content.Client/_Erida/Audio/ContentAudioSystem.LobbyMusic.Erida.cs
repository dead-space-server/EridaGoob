// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.CCVar;
using Robust.Shared.Audio.Components;

namespace Content.Client.Audio;

public sealed partial class ContentAudioSystem
{
    private TimeSpan? _lobbyPausedAt;

    /// <summary>
    /// Whether the lobby music is paused with <see cref="ToggleLobbyMusicPause"/>.
    /// </summary>
    public bool LobbyMusicPaused => _lobbyPausedAt != null;

    /// <summary>
    /// Switches the lobby music to the next or previous track of the playlist.
    /// Skips tracks that fail to load, so one broken file doesn't stop the music.
    /// The stream is deleted right away, _audio.Stop only queues it and skips non first-time predicted calls.
    /// </summary>
    public void SkipLobbyTrack(int delta)
    {
        if (_lobbyPlaylist is not { Length: > 0 } playlist || !_configManager.GetCVar(CCVars.LobbyMusicEnabled))
            return;

        var index = _lobbySoundtrackInfo == null ? -1 : Array.IndexOf(playlist, _lobbySoundtrackInfo.Filename);
        if (index < 0 && delta < 0)
            index = 0;

        if (_lobbySoundtrackInfo != null && Exists(_lobbySoundtrackInfo.MusicStreamEntityUid))
            Del(_lobbySoundtrackInfo.MusicStreamEntityUid);
        _lobbySoundtrackInfo = null;
        _lobbyPausedAt = null;

        for (var i = 0; i < playlist.Length && _lobbySoundtrackInfo == null; i++)
        {
            index = ((index + delta) % playlist.Length + playlist.Length) % playlist.Length;
            PlaySoundtrack(playlist[index]);
        }
    }

    /// <summary>
    /// Pauses or resumes the current lobby track.
    /// </summary>
    public void ToggleLobbyMusicPause()
    {
        if (_lobbySoundtrackInfo is not { } info || !Exists(info.MusicStreamEntityUid))
            return;

        if (_lobbyPausedAt is { } pausedAt)
        {
            _audio.SetState(info.MusicStreamEntityUid, AudioState.Playing);
            _lobbySoundtrackInfo = info with { NextTrackOn = info.NextTrackOn + (_timing.CurTime - pausedAt) };
            _lobbyPausedAt = null;
            return;
        }

        _audio.SetState(info.MusicStreamEntityUid, AudioState.Paused);
        _lobbyPausedAt = _timing.CurTime;
    }
}
