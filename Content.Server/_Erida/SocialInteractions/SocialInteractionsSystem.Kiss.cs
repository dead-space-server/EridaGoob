// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Erida.SocialInteractions;
using Content.Shared.IdentityManagement;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Server._Erida.SocialInteractions;

public sealed partial class SocialInteractionsSystem
{
    private void InitializeKiss()
    {
        SubscribeLocalEvent<BlowKissEvent>(OnBlowKiss);
        SubscribeLocalEvent<BlownKissComponent, StartCollideEvent>(OnKissCollide);
    }

    private void OnBlowKiss(ref BlowKissEvent args)
    {
        var from = _transform.GetMapCoordinates(args.User);
        var direction = _transform.GetMapCoordinates(args.Target).Position - from.Position;
        if (direction.LengthSquared() < 0.01f)
            return;

        var kiss = Spawn(args.Projectile, from);
        var kissComp = EnsureComp<BlownKissComponent>(kiss);
        kissComp.Shooter = args.User;

        var body = EnsureComp<PhysicsComponent>(kiss);
        _physics.SetBodyStatus(kiss, body, BodyStatus.InAir);
        var velocity = direction.Normalized() * args.Speed;
        _physics.SetLinearVelocity(kiss, body.LinearVelocity + velocity - _physics.GetMapLinearVelocity(kiss, body), body: body);
        args.Handled = true;
    }

    private void OnKissCollide(Entity<BlownKissComponent> ent, ref StartCollideEvent args)
    {
        var target = args.OtherEntity;
        if (target == ent.Comp.Shooter
            || !args.OtherFixture.Hard
            || EntityManager.IsQueuedForDeletion(ent))
            return;

        QueueDel(ent);
        if (!HasComp<ActorComponent>(target)
            || ent.Comp.Shooter is not { } shooter
            || TerminatingOrDeleted(shooter))
            return;

        if (ent.Comp.HitEffect is { } effect)
            SpawnAttachedTo(effect, Transform(target).Coordinates);

        _popup.PopupEntity(Loc.GetString("social-kiss-hit", ("user", Identity.Entity(shooter, EntityManager))),
            target,
            target);
    }
}
