// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Lost-Paradise-Project/Lost-Light/blob/d67c4d747060511f0162c6b8ce679b1895ff16c2/Content.Shared/_ES/Viewcone/Components/ESViewconeModifierComponent.cs

using Robust.Shared.GameStates;

namespace Content.Shared._Erida._ForEvents.Viewcone.Components;

/// <summary>
/// Modifies the viewcone angle of the wearer (head, eyes or mask slot) or of the entity itself.
/// Modifiers don't stack, the lowest one is applied.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESViewconeModifierComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public float AngleModifier;
}
