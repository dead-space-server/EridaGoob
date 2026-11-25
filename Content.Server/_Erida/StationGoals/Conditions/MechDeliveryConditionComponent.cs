// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Server._Erida.StationGoals.Conditions;

/// <summary>
/// At least <see cref="Count"/> mechs of <see cref="Mechs"/> built by the station crew are on the CentComm map.
/// </summary>
[RegisterComponent, Access(typeof(StationGoalConditionsSystem))]
public sealed partial class MechDeliveryConditionComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> Mechs = [];

    [DataField(required: true)]
    public int Count;
}
