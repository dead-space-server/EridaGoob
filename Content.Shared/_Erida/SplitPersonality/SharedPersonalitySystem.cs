// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

using System.Linq;
using Content.Shared._Erida.Actions;
using Content.Shared._Shitmed.Medical.Surgery;
using Content.Shared._Shitmed.Medical.Surgery.Conditions;
using Content.Shared.Body.Organ;
using Content.Shared._Shitmed.Medical.Surgery.Steps;
using Content.Shared.Body.Systems;
using Content.Shared.Emoting;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Pointing;
using Content.Shared.Speech;

namespace Content.Shared._Erida.SplitPersonality;

public sealed class SharedPersonalitySystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    private const string BrainSlot = "brain";

    // Post-lobotomy markers that the mend surgery already treated before this trauma existed.
    private static readonly string[] LobotomyKeys = ["OhioAccent", "RatvarianLanguage", "Clumsy"];

    private EntityQuery<InactivePersonalityComponent> _inactiveQuery;

    public override void Initialize()
    {
        base.Initialize();
        _inactiveQuery = GetEntityQuery<InactivePersonalityComponent>();
        SubscribeLocalEvent<InactivePersonalityComponent, InteractionAttemptEvent>((Entity<InactivePersonalityComponent> _, ref InteractionAttemptEvent ev) => ev.Cancelled = true);
        SubscribeLocalEvent<InactivePersonalityComponent, ConsciousAttemptEvent>((Entity<InactivePersonalityComponent> _, ref ConsciousAttemptEvent ev) => ev.Cancelled = true);
        SubscribeLocalEvent<InactivePersonalityComponent, UserActionAttemptEvent>(OnActionAttempt);
        SubscribeLocalEvent<InactivePersonalityComponent, UseAttemptEvent>(Block);
        SubscribeLocalEvent<InactivePersonalityComponent, PickupAttemptEvent>(Block);
        SubscribeLocalEvent<InactivePersonalityComponent, DropAttemptEvent>(Block);
        SubscribeLocalEvent<InactivePersonalityComponent, EmoteAttemptEvent>(Block);
        SubscribeLocalEvent<InactivePersonalityComponent, SpeakAttemptEvent>(Block);
        SubscribeLocalEvent<InactivePersonalityComponent, PointAttemptEvent>(Block);
        SubscribeLocalEvent<MendPersonalityConditionComponent, SurgeryValidEvent>(OnSurgeryValid);
        SubscribeLocalEvent<MendPersonalityStepComponent, SurgeryStepCompleteCheckEvent>(OnStepCheck);
    }

    private void OnActionAttempt(Entity<InactivePersonalityComponent> observer, ref UserActionAttemptEvent args)
    {
        // Also cover abilities supplied by a mind role, including those that skip consciousness checks.
        args.Cancelled = true;
    }

    private void Block(EntityUid uid, InactivePersonalityComponent component, CancellableEntityEventArgs args) => args.Cancel();

    public EntityUid? PerceptionSource(EntityUid? entity)
    {
        return _inactiveQuery.TryComp(entity, out var observer) ? observer.Body : entity;
    }

    private IEnumerable<Entity<OrganComponent>> Brains(EntityUid part) =>
        _body.GetPartOrgans(part).Where(o => o.Component.SlotId == BrainSlot).Select(o => new Entity<OrganComponent>(o.Id, o.Component));

    private void OnSurgeryValid(Entity<MendPersonalityConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (!Brains(args.Part).Any(b => HasComp<SplitPersonalityTraumaComponent>(b.Owner)
            || b.Comp.AddedKeys.Overlaps(LobotomyKeys)))
            args.Cancelled = true;
    }

    private void OnStepCheck(Entity<MendPersonalityStepComponent> ent, ref SurgeryStepCompleteCheckEvent args)
    {
        // Cancelled here means the step still has work left to perform.
        if (Brains(args.Part).Any(b => HasComp<SplitPersonalityTraumaComponent>(b.Owner)))
            args.Cancelled = true;
    }
}
