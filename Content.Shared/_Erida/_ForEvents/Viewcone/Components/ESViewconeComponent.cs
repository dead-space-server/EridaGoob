// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Lost-Paradise-Project/Lost-Light/blob/d67c4d747060511f0162c6b8ce679b1895ff16c2/Content.Shared/_ES/Viewcone/Components/ESViewconeComponent.cs

using System.Numerics;
using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Shared._Erida._ForEvents.Viewcone.Components;

/// <summary>
/// Limits what the entity sees to a cone in front of it. Everything behind fades out clientside.
/// Every viewcone mechanic only applies to entities with this component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESViewconeComponent : Component
{
    /// <summary>
    /// Base cone angle in degrees, without modifiers. Use <see cref="ESViewconeAngleSystem.GetModifiedViewconeAngle"/> for the actual angle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BaseConeAngle = 210f;

    [DataField, AutoNetworkedField]
    public float ConeFeather = 10f;

    [DataField, AutoNetworkedField]
    public float ConeIgnoreRadius = 0.5f;

    /// <summary>
    /// Cone ignore radius while blind, e.g. inside a closed locker.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ConeIgnoreRadiusBlind;

    [DataField, AutoNetworkedField]
    public float ConeIgnoreFeather = 0.08f;

    /// <summary>
    /// Angle modifier in degrees per mob state. The lowest of these and clothing modifiers is applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<MobState, float> MobStateModifiers = new()
    {
        [MobState.Critical] = -360f,
    };

    /// <summary>
    /// Set while inside a closed entity storage.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsBlind;

    /// <summary>
    /// Kept negative so nothing is shown while blind.
    /// </summary>
    public const float BaseConeAngleBlind = -10f;

    // Clientside, used for lerping and keeping the angles consistent across overlays.
    public Angle ViewAngle;
    public Angle? DesiredViewAngle;
    public Angle LastMouseRotationAngle;
    public Vector2 LastWorldPos;
    public Angle LastWorldRotationAngle;
    public float CurrentConeAngle = 210f;
    public float DesiredConeAngle = 210f;
}
