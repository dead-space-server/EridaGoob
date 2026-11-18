// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Erida.PdaPainter;
using Content.Shared.Access.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client._Erida.PdaPainter;

public sealed class PaintedIdCardVisualizerSystem : VisualizerSystem<IdCardComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, IdCardComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<string>(uid, PdaPainterVisuals.IdCardPrototype, out var protoId, args.Component))
            return;

        var template = Spawn(protoId, MapCoordinates.Nullspace);
        SpriteSystem.CopySprite(template, uid);
        Del(template);
    }
}
