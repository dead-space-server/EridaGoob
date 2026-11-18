// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Erida.CCVar;
using Content.Shared.GameTicking.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Client._Erida.Lobby;

/// <summary>
/// Lobby backgrounds the player can pick instead of the one the server rolled.
/// </summary>
public static class EridaLobbyBackgrounds
{
    /// <summary>
    /// Same extensions the server picks random backgrounds from.
    /// </summary>
    private static readonly string[] Extensions = ["png", "jpg", "jpeg", "webp"];

    public static List<LobbyBackgroundPrototype> GetAvailable(IPrototypeManager prototypes)
    {
        return [.. prototypes.EnumeratePrototypes<LobbyBackgroundPrototype>()
            .Where(proto => Extensions.Contains(proto.Background.Extension))
            .OrderBy(proto => proto.ID, StringComparer.Ordinal)];
    }

    /// <summary>
    /// The picked background, or the first available one when the picked id is unknown.
    /// Null while random backgrounds are on, so the server's choice is used.
    /// </summary>
    public static ProtoId<LobbyBackgroundPrototype>? GetPicked(IConfigurationManager cfg, IPrototypeManager prototypes)
    {
        if (cfg.GetCVar(ECCVars.LobbyBackgroundRandom))
            return null;

        var picked = cfg.GetCVar(ECCVars.LobbyBackground);
        var available = GetAvailable(prototypes);

        if (available.Any(proto => proto.ID == picked))
            return picked;

        return available.Count > 0 ? available[0].ID : null;
    }

    /// <summary>
    /// Background names and artists are locale keys in some prototypes and plain text in others.
    /// </summary>
    public static string Localize(string? text, string fallback)
    {
        var loc = IoCManager.Resolve<ILocalizationManager>();
        if (string.IsNullOrEmpty(text))
            return loc.GetString(fallback);

        return loc.TryGetString(text, out var localized) ? localized : text;
    }
}
