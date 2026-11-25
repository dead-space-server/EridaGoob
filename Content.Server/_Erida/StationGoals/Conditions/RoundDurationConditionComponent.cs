// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Erida.StationGoals.Conditions;

/// <summary>
/// The round has lasted at least <see cref="Duration"/>.
/// </summary>
[RegisterComponent, Access(typeof(StationGoalConditionsSystem))]
public sealed partial class RoundDurationConditionComponent : Component
{
    [DataField(required: true)]
    public TimeSpan Duration;
}
