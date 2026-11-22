// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Lost-Paradise-Project/Lost-Light/blob/d67c4d747060511f0162c6b8ce679b1895ff16c2/Content.Client/_ES/Viewcone/Overlays/ESViewconeResetAlphaOverlay.cs

using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._Erida._ForEvents.Viewcone.Overlays;

/// <summary>
/// After entities are drawn, restores the alpha that <see cref="ESViewconeSetAlphaOverlay"/> changed.
/// </summary>
public sealed class ESViewconeResetAlphaOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _ent = default!;

    private readonly ESViewconeOverlayManagementSystem _cone;
    private readonly SpriteSystem _sprite;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public ESViewconeResetAlphaOverlay()
    {
        IoCManager.InjectDependencies(this);

        _cone = _ent.System<ESViewconeOverlayManagementSystem>();
        _sprite = _ent.System<SpriteSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach (var (ent, baseAlpha) in _cone.CachedBaseAlphas)
        {
            if (_ent.Deleted(ent))
                continue;

            _sprite.SetColor((ent.Owner, ent.Comp), ent.Comp.Color.WithAlpha(baseAlpha));
            // Only visible sprites are cached.
            _sprite.SetVisible((ent.Owner, ent.Comp), true);
        }

        _cone.CachedBaseAlphas.Clear();
    }
}
