// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Erida.ReadyManifest;
using Content.Client._Erida.UserInterface;
using Content.Client.CrewManifest;
using Content.Shared.CCVar;

namespace Content.Client._Erida.Lobby.UI;

public sealed partial class EridaLobby
{
    private void InitManifest()
    {
        ManifestButton.Icon = EridaIcons.Get("assignment");
        ManifestButton.OnPressed += _ => OpenManifest();
    }

    private void OnManifestCVarChanged(bool _)
    {
        UpdateManifestButton();
    }

    private void UpdateManifestButton()
    {
        var crew = _gameTicker?.IsGameStarted ?? false;

        ManifestButton.Visible = _cfg.GetCVar(CCVars.CrewManifestWithoutEntity);
        ManifestButton.ToolTip = Loc.GetString(crew ? "crew-manifest-window-title" : "erida-ready-manifest-title");
    }

    private void OpenManifest()
    {
        if (_gameTicker == null)
            return;

        if (!_gameTicker.IsGameStarted)
        {
            _entitySystems.GetEntitySystem<ReadyManifestSystem>().RequestReadyManifest();
            return;
        }

        foreach (var station in _gameTicker.StationNames.Keys)
        {
            _entitySystems.GetEntitySystem<CrewManifestSystem>().RequestCrewManifest(station);
            return;
        }
    }
}
