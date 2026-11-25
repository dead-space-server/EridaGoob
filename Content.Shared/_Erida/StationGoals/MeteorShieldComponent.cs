// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Erida.StationGoals;

/// <summary>
/// Satellite that destroys meteors in range. Works only when switched on in open space.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedMeteorShieldSystem))]
public sealed partial class MeteorShieldComponent : Component
{
    [DataField(readOnly: true)]
    public float Range = 30f;

    [DataField(readOnly: true)]
    public TimeSpan ToggleDelay = TimeSpan.FromSeconds(2);

    [DataField(readOnly: true)]
    public EntProtoId BeamPrototype = "MeteorShieldBeam";

    [DataField, AutoNetworkedField]
    public bool Active;
}
