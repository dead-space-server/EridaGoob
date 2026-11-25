// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Server._Erida.StationGoals;

/// <summary>
/// Marks a station that receives a station goal at round start.
/// </summary>
[RegisterComponent, Access(typeof(StationGoalSystem))]
public sealed partial class StationGoalComponent : Component
{
    /// <summary>
    /// If not empty, the goal is picked only from these.
    /// </summary>
    [DataField]
    public List<EntProtoId> Whitelist = [];

    [DataField]
    public List<EntProtoId> Blacklist = [];

    [ViewVariables]
    public EntityUid? Goal;
}
