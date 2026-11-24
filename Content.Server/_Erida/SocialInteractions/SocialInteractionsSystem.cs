// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Erida.SocialInteractions;

/// <summary>
/// Server side of Erida social interaction verbs: two-sided gestures, blown kisses and lighting cigarettes.
/// </summary>
public sealed partial class SocialInteractionsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SmokingSystem _smoking = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeGestures();
        InitializeKiss();
        InitializeCigarette();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateGestures();
    }

    /// <summary>
    /// Shows the self, target and others variants of a popup, each to its own audience.
    /// </summary>
    private void PopupSides(string prefix, EntityUid user, EntityUid target)
    {
        var userName = Identity.Entity(user, EntityManager);
        var targetName = Identity.Entity(target, EntityManager);
        _popup.PopupEntity(Loc.GetString(prefix + "-self", ("target", targetName)), user, user);
        _popup.PopupEntity(Loc.GetString(prefix + "-target", ("user", userName)), user, target);

        var others = Filter.Pvs(user, entityManager: EntityManager)
            .RemoveWhereAttachedEntity(e => e == user || e == target);
        _popup.PopupEntity(Loc.GetString(prefix + "-others", ("user", userName), ("target", targetName)), user, others, true);
    }
}
