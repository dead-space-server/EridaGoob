// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Erida.CCVar;

public sealed partial class ECCVars
{
    /// <summary>
    /// Maximum length of directional emotes.
    /// </summary>
    public static readonly CVarDef<int> DirectionalEmoteMaxLength =
        CVarDef.Create("directional_emote.max_length", 8000, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Max distance for directional emotes between entities.
    /// </summary>
    public static readonly CVarDef<float> DirectionalEmoteMaxDistance =
        CVarDef.Create("directional_emote.max_distance", 2.3f, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    ///     Whether players can pick alternate job titles in the character editor.
    /// </summary>
    public static readonly CVarDef<bool> AlternateJobTitlesEnabled =
        CVarDef.Create("ic.alternate_job_titles_enabled", true, CVar.SERVER | CVar.REPLICATED);
}
