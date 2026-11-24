// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Lost-Paradise-Project/Lost-Light/blob/d67c4d747060511f0162c6b8ce679b1895ff16c2/Content.Client/_ES/Viewcone/ComponentTree/ESViewconeOccludableTreeSystem.cs

using System.Numerics;
using Content.Shared._Erida._ForEvents.Viewcone.Components;
using Robust.Client.GameObjects;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client._Erida._ForEvents.Viewcone.ComponentTree;

/// <summary>
/// Gathers occludable sprites for the viewcone alpha overlays.
/// </summary>
public sealed class ESViewconeOccludableTreeSystem : ComponentTreeSystem<ESViewconeOccludableTreeComponent, ESViewconeOccludableComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    protected override bool DoFrameUpdate => true;
    protected override bool DoTickUpdate => false;
    protected override bool Recursive => false;

    protected override Box2 ExtractAabb(in ComponentTreeEntry<ESViewconeOccludableComponent> entry, Vector2 pos, Angle rot)
    {
        if (!TryComp<SpriteComponent>(entry.Uid, out var sprite))
            return new Box2(pos, pos);

        return _sprite.CalculateBounds((entry.Uid, sprite), pos, rot, default).CalcBoundingBox();
    }
}
