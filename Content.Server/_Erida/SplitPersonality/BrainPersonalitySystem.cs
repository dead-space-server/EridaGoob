// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Shared._Erida.SplitPersonality;
using Content.Shared._Shitmed.Medical.Surgery;
using Content.Shared._Shitmed.Medical.Surgery.Traumas;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared._Shitmed.Medical.Surgery.Consciousness.Components;
using Content.Shared.Ghost;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Erida.SplitPersonality;

public sealed partial class BrainPersonalitySystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly GhostRoleSystem _roles = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly GhostSystem _ghosts = default!;

    private static readonly EntProtoId ObserverProto = "InactivePersonality";
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.2);

    private TimeSpan _lastUpdate;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BrainComponent, OrganIntegrityChangedEvent>(OnIntegrity);
        SubscribeLocalEvent<PersonalityRoleComponent, TakeGhostRoleEvent>(OnTakeRole);
        SubscribeLocalEvent<BrainPersonalityComponent, EntityTerminatingEvent>(OnBrainDestroyed);
        SubscribeLocalEvent<BrainPersonalityComponent, OrganAddedToBodyEvent>(OnBrainInserted, after: [typeof(BrainSystem)]);
        SubscribeLocalEvent<BrainPersonalityComponent, OrganRemovedFromBodyEvent>(OnBrainRemoved, after: [typeof(BrainSystem)]);
        SubscribeLocalEvent<PersonalityChannelComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<MendPersonalityStepComponent, SurgeryStepEvent>(OnMend);
        SubscribeLocalEvent<GhostAttemptHandleEvent>(OnGhostAttempt);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => _lastUpdate = _timing.CurTime);
        InitializeChat();
    }

    private void OnGhostAttempt(GhostAttemptHandleEvent args)
    {
        // Leaving an inactive seat is permanent: it must never grant a free scouting trip and return.
        if (HasComp<InactivePersonalityComponent>(args.Mind.OwnedEntity))
            args.CanReturnGlobal = false;
    }

    private void OnIntegrity(Entity<BrainComponent> brain, ref OrganIntegrityChangedEvent args)
    {
        if (!TryComp<OrganComponent>(brain, out var organ))
            return;
        var state = EnsureComp<BrainPersonalityComponent>(brain);
        if (args.NewIntegrity >= organ.IntegrityCap)
            state.Rolled = false;
        var threshold = Math.Max(0, _config.GetCVar(CCVars.PersonalityBrainDamage));
        if (state.Rolled || HasComp<SplitPersonalityTraumaComponent>(brain)
            || organ.IntegrityCap - args.OldIntegrity >= threshold
            || organ.IntegrityCap - args.NewIntegrity < threshold)
            return;
        state.Rolled = true;
        var success = _random.Prob(Math.Clamp(_config.GetCVar(CCVars.PersonalityTraumaChance), 0, 1));
        _adminLog.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(brain)} crossed the split-personality brain-damage threshold; trauma roll succeeded: {success}");
        if (success)
            Acquire(brain, state, organ);
    }

    public void Acquire(EntityUid brain, BrainPersonalityComponent? state = null, OrganComponent? organ = null)
    {
        if (!Resolve(brain, ref organ) || HasComp<SplitPersonalityTraumaComponent>(brain))
            return;
        state ??= EnsureComp<BrainPersonalityComponent>(brain);
        EnsureComp<SplitPersonalityTraumaComponent>(brain);
        state.Host = organ.Body ?? brain;
        if (_mind.TryGetMind(state.Host.Value, out var primary, out _))
            state.Primary = primary;
        Link(state.Host.Value, brain);
        var observer = Spawn(ObserverProto, _transform.GetMapCoordinates(brain));
        state.Observer = observer;
        Link(observer, brain);
        UpdateView(brain, state, organ);
        // The observer itself is the waiting ghost role. Once occupied, it remains the inactive seat.
        Comp<PersonalityRoleComponent>(observer).Brain = brain;
    }

    private void Link(EntityUid entity, EntityUid brain) =>
        EnsureComp<PersonalityChannelComponent>(entity).Brain = brain;

    private void OnTakeRole(Entity<PersonalityRoleComponent> role, ref TakeGhostRoleEvent args)
    {
        if (!TryComp<BrainPersonalityComponent>(role.Comp.Brain, out var state)
            || !HasComp<SplitPersonalityTraumaComponent>(role.Comp.Brain)
            || state.Secondary != null || !TryComp<GhostRoleComponent>(role, out var ghostRole)
            || state.Primary == _mind.GetMind(args.Player.UserId)
            || !TryComp<GhostComponent>(args.Player.AttachedEntity, out var ghost) || !ghost.CanTakeGhostRoles)
            return;
        _roles.GhostRoleInternalCreateMindAndTransfer(args.Player, role, role, ghostRole);
        if (!_mind.TryGetMind(role, out var secondary, out _))
            return;
        state.Secondary = secondary;
        state.Remaining = NextInterval();
        _roles.UnregisterGhostRole((role, ghostRole));
        RemComp<GhostRoleComponent>(role);
        args.TookRole = true;
        _adminLog.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(secondary)} became the second personality of {ToPrettyString(role.Comp.Brain)}");
    }

    private TimeSpan NextInterval()
    {
        var min = Math.Max(1, _config.GetCVar(CCVars.PersonalityMinimumInterval));
        var max = Math.Max(min, _config.GetCVar(CCVars.PersonalityMaximumInterval));
        return TimeSpan.FromSeconds(_random.NextFloat(min, max));
    }

    private void OnMindAdded(EntityUid host, PersonalityChannelComponent link, MindAddedMessage args)
    {
        if (!TryComp<BrainPersonalityComponent>(link.Brain, out var state)
            || state.Transferring || host != state.Host)
            return;
        // External mind swaps replace only the active seat. Never retrieve a departed mind on cure.
        if (state.SecondaryActive)
            state.Secondary = args.Mind.Owner;
        else
            state.Primary = args.Mind.Owner;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;
        var elapsed = curTime - _lastUpdate;
        if (elapsed < UpdateInterval)
            return;
        _lastUpdate = curTime;

        var query = EntityQueryEnumerator<SplitPersonalityTraumaComponent, BrainPersonalityComponent, OrganComponent>();
        while (query.MoveNext(out var brain, out _, out var state, out var organ))
        {
            Advance((brain, state, organ), elapsed);
        }
    }

    /// <summary>Refreshes the inactive view and runs the switch timer for the given amount of time.</summary>
    public void Advance(Entity<BrainPersonalityComponent, OrganComponent> brain, TimeSpan elapsed)
    {
        var (uid, state, organ) = brain;
        UpdateView(uid, state, organ);
        if (organ.Body is not {} body
            || !IsConscious(body)
            || state.Secondary == null
            || state.Primary == null)
            return;

        state.Remaining -= elapsed;
        if (state.Remaining > TimeSpan.Zero)
            return;
        Switch(uid, state);
        state.Remaining = NextInterval();
    }

    private bool IsConscious(EntityUid body) =>
        TryComp<MobStateComponent>(body, out var mob) && mob.CurrentState == MobState.Alive
        && !HasComp<SleepingComponent>(body)
        && (!TryComp<ConsciousnessComponent>(body, out var consciousness) || consciousness.IsConscious);

    private void OnBrainInserted(Entity<BrainPersonalityComponent> brain, ref OrganAddedToBodyEvent args)
    {
        if (HasComp<SplitPersonalityTraumaComponent>(brain) && TryComp<OrganComponent>(brain, out var organ))
            UpdateView(brain, brain.Comp, organ);
    }

    private void OnBrainRemoved(Entity<BrainPersonalityComponent> brain, ref OrganRemovedFromBodyEvent args)
    {
        if (!HasComp<SplitPersonalityTraumaComponent>(brain) || TerminatingOrDeleted(brain))
            return;
        MoveHost(brain, brain.Comp, brain);
        if (brain.Comp.Observer is {} observer && !TerminatingOrDeleted(observer))
        {
            var view = Comp<InactivePersonalityComponent>(observer);
            view.Body = null;
            _mob.ChangeMobState(observer, MobState.Dead);
            Dirty(observer, view);
            _eye.SetTarget(observer, brain);
        }
    }

    private void MoveHost(EntityUid brain, BrainPersonalityComponent state, EntityUid host)
    {
        if (state.Host == host)
            return;
        if (state.Host is {} old && !TerminatingOrDeleted(old))
            RemComp<PersonalityChannelComponent>(old);
        state.Host = host;
        Link(host, brain);
    }

    private void UpdateView(EntityUid brain, BrainPersonalityComponent state, OrganComponent organ)
    {
        var host = organ.Body ?? brain;
        MoveHost(brain, state, host);
        if (state.Observer is not {} observer || TerminatingOrDeleted(observer))
            return;
        // Track the physical body without parenting: deleting a body or its container must not delete the inactive mind.
        var hostPosition = _transform.GetMapCoordinates(host);
        if (!hostPosition.Position.EqualsApprox(_transform.GetMapCoordinates(observer).Position)
            || hostPosition.MapId != Transform(observer).MapID)
            _transform.SetMapCoordinates(observer, hostPosition);
        var view = Comp<InactivePersonalityComponent>(observer);
        if (view.Body != organ.Body)
        {
            view.Body = organ.Body;
            Dirty(observer, view);
        }
        _eye.SetTarget(observer, host);
        // Shared mind/death checks must agree for both participants, even though only one controls the body.
        var lifeState = organ.Body is {} body && TryComp<MobStateComponent>(body, out var bodyState)
            ? bodyState.CurrentState : MobState.Dead;
        _mob.ChangeMobState(observer, lifeState);
        if (organ.Body != null && Name(observer) != Name(host))
            _metadata.SetEntityName(observer, Name(host));
        if (TryComp<EyeComponent>(host, out var eye))
        {
            _eye.SetDrawFov(observer, eye.DrawFov);
            _eye.SetDrawLight((observer, null), eye.DrawLight);
            _eye.SetZoom(observer, eye.Zoom);
            _eye.SetVisibilityMask(observer, eye.VisibilityMask);
            _eye.SetOffset(observer, eye.Offset);
            _eye.SetRotation(observer, eye.Rotation);
        }
    }

    public bool Switch(EntityUid brain, BrainPersonalityComponent? state = null)
    {
        if (!Resolve(brain, ref state) || state.Host is not {} host || state.Observer is not {} observer
            || !IsConscious(host) || state.Primary is not {} primary || state.Secondary is not {} secondary)
            return false;
        var outgoing = state.SecondaryActive ? secondary : primary;
        var incoming = state.SecondaryActive ? primary : secondary;
        // A departed, freely ghosting or visiting participant cannot be pulled back by the timer.
        if (!TryComp<MindComponent>(outgoing, out var outgoingMind)
            || outgoingMind.OwnedEntity != host
            || outgoingMind.VisitingEntity != null
            || !TryComp<MindComponent>(incoming, out var incomingMind)
            || incomingMind.OwnedEntity != observer
            || incomingMind.VisitingEntity != null)
            return false;
        state.Transferring = true;
        try
        {
            _mind.TransferTo(incoming, null, ghostCheckOverride: true, createGhost: false);
            _mind.TransferTo(outgoing, observer, ghostCheckOverride: true);
            _mind.TransferTo(incoming, host, ghostCheckOverride: true);
            state.SecondaryActive = !state.SecondaryActive;
        }
        finally { state.Transferring = false; }
        _adminLog.Add(LogType.Mind, LogImpact.Low, $"{ToPrettyString(brain)} switched controlling personality to {ToPrettyString(incoming)}");
        return true;
    }

    private void OnMend(Entity<MendPersonalityStepComponent> ent, ref SurgeryStepEvent args)
    {
        foreach (var organ in _body.GetPartOrgans(args.Part).ToArray())
        {
            if (HasComp<SplitPersonalityTraumaComponent>(organ.Id))
                Cure(organ.Id);
        }
    }

    public void Cure(EntityUid brain)
    {
        if (!TryComp<BrainPersonalityComponent>(brain, out var state))
            return;
        Cleanup(brain, state, restorePrimary: true);
        RemComp<SplitPersonalityTraumaComponent>(brain);
        _adminLog.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(brain)} was cured of split-personality trauma");
    }

    private void OnBrainDestroyed(Entity<BrainPersonalityComponent> brain, ref EntityTerminatingEvent args) =>
        Cleanup(brain, brain.Comp, restorePrimary: false);

    private void Cleanup(EntityUid brain, BrainPersonalityComponent state, bool restorePrimary)
    {
        state.Transferring = true;
        try
        {
            if (state.Observer is {} observer && !TerminatingOrDeleted(observer))
            {
                if (state.Host is {} host)
                {
                    if (state.SecondaryActive || !restorePrimary)
                    {
                        var active = state.SecondaryActive ? state.Secondary : state.Primary;
                        if (active is {} activeMind && TryComp<MindComponent>(activeMind, out var activeComp)
                            && activeComp.OwnedEntity == host)
                            Release((activeMind, activeComp), host);
                    }
                    if (restorePrimary && state.SecondaryActive && !TerminatingOrDeleted(host)
                        && state.Primary is {} primary && TryComp<MindComponent>(primary, out var mind)
                        && mind.OwnedEntity == observer)
                        _mind.TransferTo(primary, host, ghostCheckOverride: true);
                }
                // Transfer explicitly before deleting the observer; no return-to-body privilege survives treatment.
                if (_mind.TryGetMind(observer, out var inactive, out var inactiveMind))
                    Release((inactive, inactiveMind), observer);
                QueueDel(observer);
            }
            if (state.Host is {} old && !TerminatingOrDeleted(old))
                RemComp<PersonalityChannelComponent>(old);
            state.Observer = null;
            state.Primary = null;
            state.Secondary = null;
            state.SecondaryActive = false;
        }
        finally { state.Transferring = false; }
    }

    /// <summary>
    /// Ghosts a participant without a way back. Like a deleted mind container, no ghost is made during round cleanup.
    /// </summary>
    private void Release(Entity<MindComponent> mind, EntityUid from)
    {
        _mind.TransferTo(mind, null, ghostCheckOverride: true, createGhost: false, mind: mind.Comp);
        // SpawnGhost falls back to the observer spawn point when this position is on a terminating map.
        if (_ticker.RunLevel != GameRunLevel.PreRoundLobby)
            _ghosts.SpawnGhost((mind.Owner, mind.Comp), from);
    }
}
