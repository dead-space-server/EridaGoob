// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Erida.StationGoals;

/// <summary>
/// CentComm approval: while the goal is active, lathes of its station can print extra recipes.
/// </summary>
[RegisterComponent, Access(typeof(StationGoalLatheRecipesSystem))]
public sealed partial class StationGoalLatheRecipesComponent : Component
{
    /// <summary>
    /// Unlocked recipe and the recipe a lathe must already have for it,
    /// so research and the lathe type still decide who can print it.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<LatheRecipePrototype>, ProtoId<LatheRecipePrototype>> Recipes = [];
}
