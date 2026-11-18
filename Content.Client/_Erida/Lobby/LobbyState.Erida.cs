// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Erida.Lobby;
using Content.Shared._Erida.CCVar;
using Content.Shared.GameTicking.Prototypes;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby;

public sealed partial class LobbyState
{
    private LobbyBackgroundAnimation? _backgroundAnimation;
    private string? _shownBackground;

    private void StartupErida()
    {
        _cfg.OnValueChanged(ECCVars.LobbyBackgroundRandom, OnLobbyBackgroundOptionChanged);
        _cfg.OnValueChanged(ECCVars.LobbyBackground, OnLobbyBackgroundOptionChanged, true);
    }

    private void ShutdownErida()
    {
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
            return;
        }

        _shownBackground = proto.ID;
        Lobby.Background.Texture = _backgroundAnimation.Start(proto)
            ?? (_resourceCache.TryGetResource<TextureResource>(proto.Background, out var texture) ? texture.Texture : null);
    }

    private void UpdateEridaLobbyBackground(float frameTime)
    {
        if (Lobby == null || _backgroundAnimation is not { Playing: true })
            return;

        if (_backgroundAnimation.Update(frameTime) is { } frame)
            Lobby.Background.Texture = frame;
    }
}
