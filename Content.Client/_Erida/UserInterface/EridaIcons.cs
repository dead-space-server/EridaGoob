// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;

namespace Content.Client._Erida.UserInterface;

/// <summary>
/// Material Symbols icons made by Tools/_Erida/svg_icons.py, by glyph name like <c>"close"</c> or <c>"Departments/work"</c>.
/// </summary>
public static class EridaIcons
{
    private const string IconDirectory = "/Textures/_Erida/Interface/Lobby/";

    /// <summary>
    /// The resource cache already keeps loaded textures, a static cache would share them between client instances in tests.
    /// </summary>
    public static Texture Get(string name)
    {
        return IoCManager.Resolve<IResourceCache>().GetTexture($"{IconDirectory}{name}.svg.192dpi.png");
    }
}
