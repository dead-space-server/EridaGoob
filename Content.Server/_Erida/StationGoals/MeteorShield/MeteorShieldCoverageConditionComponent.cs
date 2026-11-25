// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Erida.StationGoals.MeteorShield;

/// <summary>
/// Active meteor shield satellites cover at least <see cref="Coverage"/> of the directions meteors can come from.
/// </summary>
[RegisterComponent, Access(typeof(MeteorShieldSystem))]
public sealed partial class MeteorShieldCoverageConditionComponent : Component
{
    /// <summary>
    /// Required share of the full circle around the station, from 0 to 1.
    /// </summary>
    [DataField(required: true)]
    public float Coverage;
}
