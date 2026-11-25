// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Erida.StationGoals;

/// <summary>
/// Cargo products that can be ordered only while this goal is active on some station.
/// </summary>
[RegisterComponent, Access(typeof(StationGoalCargoProductsSystem))]
public sealed partial class StationGoalCargoProductsComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<CargoProductPrototype>> Products = [];
}
