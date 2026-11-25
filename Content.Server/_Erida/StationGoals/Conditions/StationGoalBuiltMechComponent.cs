// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Erida.StationGoals.Conditions;

/// <summary>
/// Mech built through construction, so mechs spawned by CentComm or ERT do not count for goals.
/// </summary>
[RegisterComponent, Access(typeof(StationGoalConditionsSystem))]
public sealed partial class StationGoalBuiltMechComponent : Component
{
    /// <summary>
    /// Station the mech was built on, null if built outside any station. Only mechs of the goal station count.
    /// </summary>
    [ViewVariables]
    public EntityUid? Station;
}
