// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Erida._ForEvents.Viewcone;

namespace Content.Client._Erida._ForEvents.Viewcone;

/// <summary>
/// Spawns the clientside viewcone effects the server sends.
/// </summary>
public sealed class ESViewconeEffectSpawnSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ESViewconeEffectEvent>(OnEffect);
    }

    private void OnEffect(ESViewconeEffectEvent ev)
    {
        var ent = Spawn(ev.Effect, ev.Coordinates);
        _xform.SetWorldRotation(ent, ev.Rotation);
    }
}
