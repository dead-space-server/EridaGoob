// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

using System.Linq;
using Content.Server._Erida.SplitPersonality;
using Content.Server.Ghost.Roles;
using Content.Shared._Erida.SplitPersonality;
using Content.Shared._Shitmed.Medical.Surgery;
using Content.Shared._Shitmed.Medical.Surgery.Conditions;
using Content.Shared._Shitmed.Medical.Surgery.Steps;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mind.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Erida;

[TestFixture]
public sealed class BrainPersonalityTest
{
    [TestCase(0f)]
    [TestCase(1f)]
    public async Task DamageRollsOnceUntilFullRecovery(float chance)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitAssertion(() =>
        {
            var em = server.EntMan;
            var config = server.ResolveDependency<IConfigurationManager>();
            config.SetCVar(CCVars.PersonalityTraumaChance, chance);
            var body = em.Spawn("MobHuman", map.MapCoords);
            var bodySystem = em.System<SharedBodySystem>();
            var brain = bodySystem.GetBodyOrgans(body).Single(o => em.HasComponent<BrainComponent>(o.Id)).Id;
            var trauma = em.System<TraumaSystem>();
            var personalities = em.System<BrainPersonalitySystem>();

            trauma.TryCreateOrganDamageModifier(brain, 174, body, "PersonalityTest");
            Assert.That(em.HasComponent<SplitPersonalityTraumaComponent>(brain), Is.False);
            trauma.TryChangeOrganDamageModifier(brain, 1, body, "PersonalityTest");
            Assert.That(em.GetComponent<BrainPersonalityComponent>(brain).Rolled, Is.True);
            Assert.That(em.HasComponent<SplitPersonalityTraumaComponent>(brain), Is.EqualTo(chance == 1));
            if (chance == 1)
                personalities.Cure(brain);

            config.SetCVar(CCVars.PersonalityTraumaChance, 1f);
            trauma.TryChangeOrganDamageModifier(brain, 5, body, "PersonalityTest");
            Assert.That(em.HasComponent<SplitPersonalityTraumaComponent>(brain), Is.False, "No repeat rolls or immediate relapse in the same damage episode");
            trauma.RestoreOrganIntegrity(brain);
            Assert.That(em.GetComponent<BrainPersonalityComponent>(brain).Rolled, Is.False);
            trauma.TryCreateOrganDamageModifier(brain, 175, body, "PersonalityTest");
            Assert.That(em.HasComponent<SplitPersonalityTraumaComponent>(brain), Is.True);
            personalities.Cure(brain);
        });
        await pair.CleanReturnAsync();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SwitchAndSurgeryRespectCurrentPrimarySlot(bool replaceWithWizard)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitAssertion(() =>
        {
            var em = server.EntMan;
            var bodySystem = em.System<SharedBodySystem>();
            var minds = em.System<SharedMindSystem>();
            var system = em.System<BrainPersonalitySystem>();
            var body = em.Spawn("MobHuman", map.MapCoords);
            var brain = bodySystem.GetBodyOrgans(body).Single(o => em.HasComponent<BrainComponent>(o.Id)).Id;
            var originalName = em.GetComponent<MetaDataComponent>(body).EntityName;
            var primary = minds.CreateMind(null).Owner;
            var secondary = minds.CreateMind(null).Owner;
            minds.TransferTo(primary, body);
            system.Acquire(brain);
            var state = em.GetComponent<BrainPersonalityComponent>(brain);
            var observer = state.Observer!.Value;
            Assert.That(em.System<GhostRoleSystem>().GhostRoles.Any(r => r.Owner == observer), Is.True);
            Assert.That(em.HasComponent<GhostComponent>(observer), Is.False);
            Assert.That(em.HasComponent<InputMoverComponent>(observer), Is.False);
            var ghostAttempt = new Content.Server.Ghost.GhostAttemptHandleEvent(em.GetComponent<MindComponent>(primary), true);
            em.EventBus.RaiseEvent(Robust.Shared.GameObjects.EventSource.Local, ghostAttempt);
            Assert.That(ghostAttempt.CanReturnGlobal, Is.True);
            Assert.That(em.System<ActionBlockerSystem>().CanInteract(observer, body), Is.False);
            Assert.That(em.System<ActionBlockerSystem>().CanConsciouslyPerformAction(observer), Is.False);
            var actionAttempt = new Content.Shared.Actions.Events.ActionAttemptEvent(observer);
            em.EventBus.RaiseLocalEvent(observer, ref actionAttempt);
            Assert.That(actionAttempt.Cancelled, Is.True, "Even mind-provided abilities cannot be used while inactive");
            minds.TransferTo(secondary, observer);
            state.Secondary = secondary;
            state.Remaining = 180;

            Assert.That(system.Switch(brain), Is.True);
            Assert.That(minds.GetMind(body), Is.EqualTo(secondary));
            Assert.That(minds.GetMind(observer), Is.EqualTo(primary));
            Assert.That(minds.IsCharacterDeadPhysically(em.GetComponent<MindComponent>(primary)), Is.False,
                "The inactive participant is alive while the shared body is alive");
            ghostAttempt = new Content.Server.Ghost.GhostAttemptHandleEvent(em.GetComponent<MindComponent>(primary), true);
            em.EventBus.RaiseEvent(Robust.Shared.GameObjects.EventSource.Local, ghostAttempt);
            Assert.That(ghostAttempt.CanReturnGlobal, Is.False, "An inactive participant cannot scout as a ghost and return");
            Assert.That(system.Switch(brain), Is.True);
            Assert.That(minds.GetMind(body), Is.EqualTo(primary));
            Assert.That(em.GetComponent<MetaDataComponent>(body).EntityName, Is.EqualTo(originalName));

            EntityUid? departedBody = null;
            if (replaceWithWizard)
            {
                departedBody = em.Spawn("MobHuman", map.MapCoords);
                var wizard = minds.CreateMind(null).Owner;
                minds.TransferTo(primary, departedBody);
                Assert.That(system.HasBothParticipants(brain), Is.False, "A departed mind must lose the private channel");
                minds.TransferTo(wizard, body);
                Assert.That(system.HasBothParticipants(brain), Is.True);
                Assert.That(state.Primary, Is.EqualTo(wizard));
                primary = wizard;
            }
            Assert.That(system.Switch(brain), Is.True);
            var part = em.GetComponent<TransformComponent>(brain).ParentUid;
            var surgery = em.Spawn("SurgeryMendBrainTissue", map.MapCoords);
            var valid = new SurgeryValidEvent(body, part);
            em.EventBus.RaiseLocalEvent(surgery, ref valid);
            Assert.That(valid.Cancelled, Is.False, "Trauma alone makes tissue restoration available");
            var step = em.Spawn("SurgeryStepMendBrainTissue", map.MapCoords);
            var check = new SurgeryStepCompleteCheckEvent(body, part, surgery);
            em.EventBus.RaiseLocalEvent(step, ref check);
            Assert.That(check.Cancelled, Is.True, "Trauma is work remaining for this step");
            var tool = em.Spawn("Hemostat", map.MapCoords);
            var perform = new SurgeryStepEvent(body, body, part, tool, surgery, step, true);
            em.EventBus.RaiseLocalEvent(step, ref perform);
            Assert.That(em.HasComponent<SplitPersonalityTraumaComponent>(brain), Is.False);
            Assert.That(minds.GetMind(body), Is.EqualTo(primary));
            Assert.That(em.HasComponent<GhostComponent>(em.GetComponent<MindComponent>(secondary).OwnedEntity), Is.True);
            Assert.That(em.HasComponent<PersonalityChannelComponent>(body), Is.False);
            if (departedBody is {} other)
                Assert.That(minds.GetMind(other), Is.Not.Null, "Cure must not pull the old primary back from another body");
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TimerPausesAndBrainDestructionReleasesBothMinds()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitAssertion(() =>
        {
            var em = server.EntMan;
            var body = em.Spawn("MobHuman", map.MapCoords);
            var brain = em.System<SharedBodySystem>().GetBodyOrgans(body).Single(o => em.HasComponent<BrainComponent>(o.Id)).Id;
            var minds = em.System<SharedMindSystem>();
            var system = em.System<BrainPersonalitySystem>();
            var primary = minds.CreateMind(null).Owner;
            var secondary = minds.CreateMind(null).Owner;
            minds.TransferTo(primary, body);
            system.Acquire(brain);
            var state = em.GetComponent<BrainPersonalityComponent>(brain);
            minds.TransferTo(secondary, state.Observer);
            state.Secondary = secondary;
            state.Remaining = 180;
            state.Remaining = 1;
            em.EnsureComponent<SleepingComponent>(body);
            system.Update(2);
            Assert.That(state.Remaining, Is.EqualTo(1));
            Assert.That(minds.GetMind(body), Is.EqualTo(primary));
            Assert.That(system.TrySendTelepathy(body, "sleeping", false), Is.True);
            em.RemoveComponent<SleepingComponent>(body);
            em.System<MobStateSystem>().ChangeMobState(body, MobState.Dead);
            system.Update(2);
            Assert.That(state.Remaining, Is.EqualTo(1));
            Assert.That(system.Switch(brain), Is.False);
            Assert.That(minds.IsCharacterDeadPhysically(em.GetComponent<MindComponent>(secondary)), Is.True);
            em.System<Content.Shared.Administration.Systems.RejuvenateSystem>().PerformRejuvenate(body);
            system.Update(2);
            Assert.That(minds.GetMind(body), Is.EqualTo(secondary));
            Assert.That(state.Remaining, Is.InRange(180, 600));
            // Both minds have no connected session. Ownership still changes only when the timer expires.
            var remaining = state.Remaining;
            system.Update(0.5f);
            Assert.That(state.Remaining, Is.EqualTo(remaining - 0.5f));
            Assert.That(minds.GetMind(body), Is.EqualTo(secondary));
            em.DeleteEntity(brain);
            Assert.That(em.HasComponent<GhostComponent>(em.GetComponent<MindComponent>(primary).OwnedEntity), Is.True);
            Assert.That(em.HasComponent<GhostComponent>(em.GetComponent<MindComponent>(secondary).OwnedEntity), Is.True);
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task GhostRoleCreatesAnInactivePlayerAndTreatmentReleasesThem()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true, Connected = true });
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        EntityUid brain = default, body = default, secondary = default;
        await server.WaitAssertion(() =>
        {
            var em = server.EntMan;
            var session = server.ResolveDependency<Robust.Server.Player.IPlayerManager>().Sessions.Single();
            var minds = em.System<SharedMindSystem>();
            minds.WipeMind(session);
            var ghost = em.Spawn(Content.Server.GameTicking.GameTicker.ObserverPrototypeName, map.MapCoords);
            var ghostMind = minds.CreateMind(session.UserId).Owner;
            minds.TransferTo(ghostMind, ghost);
            body = em.Spawn("MobHuman", map.MapCoords);
            brain = em.System<SharedBodySystem>().GetBodyOrgans(body).Single(o => em.HasComponent<BrainComponent>(o.Id)).Id;
            var primary = minds.CreateMind(null).Owner;
            minds.TransferTo(primary, body);
            em.System<BrainPersonalitySystem>().Acquire(brain);
            var state = em.GetComponent<BrainPersonalityComponent>(brain);
            var roles = em.System<GhostRoleSystem>();
            var role = roles.GhostRoles.Single(r => r.Owner == state.Observer);
            Assert.That(roles.Takeover(session, role.Comp.Identifier), Is.True);
            secondary = state.Secondary!.Value;
            Assert.That(session.AttachedEntity, Is.EqualTo(state.Observer));
            Assert.That(minds.GetMind(body), Is.EqualTo(primary));
            Assert.That(state.Remaining, Is.InRange(180, 600));
            Assert.That(roles.GhostRoles.Any(r => r.Owner == state.Observer), Is.False);
        });
        await pair.RunTicksSync(5);
        await server.WaitAssertion(() =>
        {
            var em = server.EntMan;
            var system = em.System<BrainPersonalitySystem>();
            Assert.That(system.Switch(brain), Is.True);
            Assert.That(em.System<SharedMindSystem>().GetMind(body), Is.EqualTo(secondary));
            system.Cure(brain);
            var ghost = em.GetComponent<MindComponent>(secondary).OwnedEntity;
            Assert.That(em.GetComponent<GhostComponent>(ghost!.Value).CanTakeGhostRoles, Is.True);
            Assert.That(em.HasComponent<InactivePersonalityComponent>(ghost), Is.False);
        });
        await pair.RunTicksSync(2);
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BrainTransplantCarriesBothParticipants()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        EntityUid brain = default, oldBody = default, newBody = default, primary = default, secondary = default;
        await server.WaitAssertion(() =>
        {
            var em = server.EntMan;
            var bodies = em.System<SharedBodySystem>();
            var minds = em.System<SharedMindSystem>();
            oldBody = em.Spawn("MobHuman", map.MapCoords);
            newBody = em.Spawn("MobHuman", map.MapCoords);
            brain = bodies.GetBodyOrgans(oldBody).Single(o => em.HasComponent<BrainComponent>(o.Id)).Id;
            var otherBrain = bodies.GetBodyOrgans(newBody).Single(o => em.HasComponent<BrainComponent>(o.Id)).Id;
            var newHead = em.GetComponent<TransformComponent>(otherBrain).ParentUid;
            primary = minds.CreateMind(null).Owner;
            secondary = minds.CreateMind(null).Owner;
            minds.TransferTo(primary, oldBody);
            em.System<BrainPersonalitySystem>().Acquire(brain);
            var state = em.GetComponent<BrainPersonalityComponent>(brain);
            minds.TransferTo(secondary, state.Observer);
            state.Secondary = secondary;
            state.Remaining = 180;
            Assert.That(bodies.RemoveOrgan(brain), Is.True);
            Assert.That(minds.GetMind(brain), Is.EqualTo(primary));
            Assert.That(bodies.RemoveOrgan(otherBrain), Is.True);
            Assert.That(bodies.AddOrganToFirstValidSlot(newHead, brain), Is.True);
            Assert.That(state.Host, Is.EqualTo(newBody), "The host must change synchronously with transplantation");
        });
        await pair.RunTicksSync(2);
        await server.WaitAssertion(() =>
        {
            var em = server.EntMan;
            var state = em.GetComponent<BrainPersonalityComponent>(brain);
            Assert.That(state.Host, Is.EqualTo(newBody));
            Assert.That(em.HasComponent<PersonalityChannelComponent>(oldBody), Is.False);
            Assert.That(em.GetComponent<InactivePersonalityComponent>(state.Observer!.Value).Body, Is.EqualTo(newBody));
            Assert.That(em.System<SharedMindSystem>().GetMind(newBody), Is.EqualTo(primary));
            Assert.That(em.System<SharedMindSystem>().GetMind(state.Observer.Value), Is.EqualTo(secondary));
            em.System<BrainPersonalitySystem>().Cure(brain);
        });
        await pair.CleanReturnAsync();
    }
}
