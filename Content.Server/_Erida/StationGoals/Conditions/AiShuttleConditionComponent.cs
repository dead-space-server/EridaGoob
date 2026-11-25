// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Erida.StationGoals.AiShuttle;

namespace Content.Server._Erida.StationGoals.Conditions;

/// <summary>
/// A powered <see cref="AiShuttleCoreComponent"/> delivered to the goal station is anchored on a shuttle that is not part of a station,
/// is not the evac or arrivals shuttle and can fly: it has a gyroscope and working thrusters.
/// </summary>
[RegisterComponent, Access(typeof(StationGoalConditionsSystem))]
public sealed partial class AiShuttleConditionComponent : Component;
