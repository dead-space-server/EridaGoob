// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Lost-Paradise-Project/Lost-Light/blob/d67c4d747060511f0162c6b8ce679b1895ff16c2/Content.Shared/_Funkystation/CCVar/ViewconeCCVars.cs

using Robust.Shared.Configuration;

namespace Content.Shared._Erida.CCVar;

public sealed partial class ECCVars
{
    /// <summary>
    /// Toggles the whole viewcone mechanic.
    /// </summary>
    public static readonly CVarDef<bool> ViewconeEnabled =
        CVarDef.Create("erida.viewcone_enabled", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Disables the grain noise on the viewcone occlusion.
    /// </summary>
    public static readonly CVarDef<bool> ViewconeDisableGrain =
        CVarDef.Create("erida.viewcone_disable_grain", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// From 0 to 1, how much darker the viewcone occlusion gets.
    /// </summary>
    public static readonly CVarDef<float> ViewconeOcclusionOpacity =
        CVarDef.Create("erida.viewcone_occlusion_opacity", 0f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
