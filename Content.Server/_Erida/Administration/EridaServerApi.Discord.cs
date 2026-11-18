// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Content.Shared._Erida.Discord.Linking;

namespace Content.Server._Erida.Administration;

public sealed partial class EridaServerApi
{
    public async Task<bool> RegisterLinkingCode(Guid user, Guid code)
    {
        if (!await EnsureActive())
            return false;

        try
        {
            using var req = CreateRequest(HttpMethod.Post, "/api/link/codes");
            req.Content = JsonContent.Create(new { code = code.ToString(), userId = user });

            using var resp = await _http.SendAsync(req);

            if (!resp.IsSuccessStatusCode)
                _sawmill.Error($"Link code register failed: {(int) resp.StatusCode}");

            return resp.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Link code register failed: {e.Message}");
            return false;
        }
    }

    public async Task<LinkStatus?> GetLinkedUserDataOrNull(Guid user)
    {
        if (!await EnsureActive())
            return null;

        try
        {
            using var req = CreateRequest(HttpMethod.Get, $"/api/link/{user}");
            using var resp = await _http.SendAsync(req);

            if (!resp.IsSuccessStatusCode)
                return null;

            var result = await resp.Content.ReadFromJsonAsync<LinkStatus>();

            return result;
        }
        catch (Exception e)
        {
            _sawmill.Warning($"Get link status failed: {e.Message}");
            return null;
        }
    }
}
