// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Lost-Paradise-Project/Lost-Light/blob/d67c4d747060511f0162c6b8ce679b1895ff16c2/Content.Shared/_ES/Viewcone/Components/ESViewconeOccludableComponent.cs

using Robust.Shared.ComponentTrees;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;

namespace Content.Shared._Erida._ForEvents.Viewcone.Components;

/// <summary>
/// Marks an entity that fades away clientside when it is out of the local player's viewcone.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESViewconeOccludableComponent : Component, IComponentTreeEntry<ESViewconeOccludableComponent>
{
    [DataField, AutoNetworkedField]
    public bool OccludeIfAnchored;

    /// <summary>
    /// The sprite is invisible while in view and visible outside of it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Inverted;

    /// <summary>
    /// Ignore the base alpha of the sprite when setting transparency. Used by viewcone effects.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OverrideBaseAlpha;

    /// <summary>
    /// For effects, the entity that caused it. Its own effects are never shown to it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Source;

    public EntityUid? TreeUid { get; set; }
    public DynamicTree<ComponentTreeEntry<ESViewconeOccludableComponent>>? Tree { get; set; }
    public bool AddToTree => true;
    public bool TreeUpdateQueued { get; set; }
}
