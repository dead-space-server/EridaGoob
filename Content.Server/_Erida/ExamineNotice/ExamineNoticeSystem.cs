// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared._Erida.ExamineNotice;
using Content.Shared.Bed.Sleep;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Erida.ExamineNotice;

public sealed class ExamineNoticeSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExamineNoticeComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<ExamineNoticeComponent> ent, ref ExaminedEvent args)
    {
        var examiner = args.Examiner;
        if (examiner == ent.Owner
            || !HasComp<ActorComponent>(ent)
            || !CanNotice(ent, examiner))
            return;

        var now = _timing.CurTime;
        if (ent.Comp.NextNotice.TryGetValue(examiner, out var next) && next > now)
            return;

        foreach (var (uid, time) in ent.Comp.NextNotice)
        {
            if (time <= now)
                ent.Comp.NextNotice.Remove(uid);
        }

        ent.Comp.NextNotice[examiner] = now + ent.Comp.Cooldown;
        _popup.PopupEntity(Loc.GetString("examine-notice-popup", ("examiner", Identity.Entity(examiner, EntityManager))),
            examiner,
            ent.Owner);
    }

    private bool CanNotice(Entity<ExamineNoticeComponent> target, EntityUid examiner)
    {
        if (!_mobState.IsAlive(examiner)
            || !_mobState.IsAlive(target)
            || HasComp<SleepingComponent>(target))
            return false;

        if (TryComp<BlindableComponent>(target, out var blindable) && blindable.IsBlind)
            return false;

        return !IsBehind(target, examiner)
            && _examine.InRangeUnOccluded(target, examiner, target.Comp.Range)
            && _examine.CanExamine(target, examiner);
    }

    private bool IsBehind(Entity<ExamineNoticeComponent> target, EntityUid examiner)
    {
        var (position, rotation) = _transform.GetWorldPositionRotation(target);
        var toExaminer = _transform.GetWorldPosition(examiner) - position;
        if (toExaminer.LengthSquared() < 0.01f)
            return false;

        // Mobs face south at zero rotation, so their back points a quarter turn further.
        var back = (rotation + MathHelper.PiOver2).ToVec();
        return Vector2.Dot(back, Vector2.Normalize(toExaminer)) > Math.Cos(target.Comp.BackArc.Theta / 2);
    }
}
