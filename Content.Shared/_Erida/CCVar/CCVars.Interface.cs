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
    ///     Hex seed color the Erida UI palette is generated from.
    /// </summary>
    public static readonly CVarDef<string> InterfaceAccent =
        CVarDef.Create("erida.ui.accent", "#303AA8", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Whether Erida UI panels let the blurred background show through.
    /// </summary>
    public static readonly CVarDef<bool> InterfaceTransparency =
        CVarDef.Create("erida.ui.transparency", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     How transparent Erida UI panels are when <see cref="InterfaceTransparency"/> is on.
    /// </summary>
    public static readonly CVarDef<float> InterfaceTransparencyAmount =
        CVarDef.Create("erida.ui.transparency_amount", 0.11f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Whether the lobby shows the chat panel on the left and the character panel on the right.
    /// </summary>
    public static readonly CVarDef<bool> LobbyChatLeft =
        CVarDef.Create("erida.ui.lobby_chat_left", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// How dressed the profile editor preview is: 0 all clothing, 1 underwear only, 2 naked.
    /// </summary>
    public static readonly CVarDef<int> PreviewClothing =
        CVarDef.Create("erida.ui.preview_clothing", 0, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Whether interactions show the hand or item flying from the performer to the target.
    /// </summary>
    public static readonly CVarDef<bool> InteractionParticles =
        CVarDef.Create("erida.ui.interaction_particles", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Whether using or picking up an item shows it popping up above the performer.
    /// </summary>
    public static readonly CVarDef<bool> InHandInteractionParticles =
        CVarDef.Create("erida.ui.inhand_interaction_particles", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Which <see cref="RoundJoinAnimationType"/> is shown when joining a round.
    /// </summary>
    public static readonly CVarDef<int> RoundJoinAnimation =
        CVarDef.Create("erida.ui.round_join_animation", (int) RoundJoinAnimationType.StationCredits, CVar.CLIENTONLY | CVar.ARCHIVE);
}
