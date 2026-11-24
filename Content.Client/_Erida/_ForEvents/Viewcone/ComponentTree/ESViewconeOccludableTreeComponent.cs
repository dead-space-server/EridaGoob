// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Lost-Paradise-Project/Lost-Light/blob/d67c4d747060511f0162c6b8ce679b1895ff16c2/Content.Client/_ES/Viewcone/ComponentTree/ESViewconeOccludableTreeComponent.cs

using Content.Shared._Erida._ForEvents.Viewcone.Components;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client._Erida._ForEvents.Viewcone.ComponentTree;

[RegisterComponent]
public sealed partial class ESViewconeOccludableTreeComponent : Component, IComponentTreeComponent<ESViewconeOccludableComponent>
{
    public DynamicTree<ComponentTreeEntry<ESViewconeOccludableComponent>> Tree { get; set; } = default!;
}
