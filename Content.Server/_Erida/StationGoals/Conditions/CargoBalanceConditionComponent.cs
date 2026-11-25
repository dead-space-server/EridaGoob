// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Erida.StationGoals.Conditions;

/// <summary>
/// The station cargo account has at least <see cref="Amount"/> spesos.
/// </summary>
[RegisterComponent, Access(typeof(StationGoalConditionsSystem))]
public sealed partial class CargoBalanceConditionComponent : Component
{
    [DataField(required: true)]
    public int Amount;

    /// <summary>
    /// Account to check, the station primary account if null.
    /// </summary>
    [DataField(readOnly: true)]
    public ProtoId<CargoAccountPrototype>? Account;
}
