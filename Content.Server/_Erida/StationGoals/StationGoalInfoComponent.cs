// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Erida.StationGoals;

/// <summary>
/// A station goal. Conditions are separate components on the same entity,
/// they answer <see cref="StationGoalGetProgressEvent"/>. The result is checked at round end.
/// </summary>
[RegisterComponent, Access(typeof(StationGoalSystem))]
public sealed partial class StationGoalInfoComponent : Component
{
    /// <summary>
    /// Fax text, gets the station name as <c>$station</c>.
    /// </summary>
    [DataField(required: true)]
    public LocId Text;

    /// <summary>
    /// Short name for the round end summary.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Minimum alive crew of the station to pick this goal.
    /// </summary>
    [DataField(readOnly: true)]
    public int? MinPlayers;

    /// <summary>
    /// Maximum alive crew of the station to pick this goal.
    /// </summary>
    [DataField(readOnly: true)]
    public int? MaxPlayers;

    /// <summary>
    /// Minimum alive crew of the station in each department to pick this goal.
    /// </summary>
    [DataField(readOnly: true)]
    public Dictionary<ProtoId<DepartmentPrototype>, int> Departments = [];

    [DataField(readOnly: true)]
    public float Weight = 1f;

    /// <summary>
    /// Picked only if the station whitelist contains this goal.
    /// </summary>
    [DataField(readOnly: true)]
    public bool WhitelistOnly;

    /// <summary>
    /// Entities spawned next to every fax of the station that receives the goal.
    /// </summary>
    [DataField(readOnly: true)]
    public List<EntProtoId> Spawns = [];

    [ViewVariables]
    public EntityUid? Station;

    /// <summary>
    /// Admin override of the result.
    /// </summary>
    [ViewVariables]
    public bool? ForceCompleted;
}
