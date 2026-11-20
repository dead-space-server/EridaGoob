// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Erida.CCVar;

public sealed partial class ECCVars
{
    /// <summary>
    /// Last filter picked in the marking picker: <c>own</c>, <c>all</c>, <c>common</c> or a species id.
    /// </summary>
    public static readonly CVarDef<string> MarkingsFilter =
        CVarDef.Create("erida.ui.markings_filter", "own", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// How dressed the profile editor preview is: 0 all clothing, 1 underwear only, 2 naked.
    /// </summary>
    public static readonly CVarDef<int> PreviewClothing =
        CVarDef.Create("erida.ui.preview_clothing", 0, CVar.CLIENTONLY | CVar.ARCHIVE);
}
