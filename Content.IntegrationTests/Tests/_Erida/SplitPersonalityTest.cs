// SPDX-FileCopyrightText: 2026 Kiwuru / Kitty <olosa15f@gmail.com>
// SPDX-License-Identifier: MIT

using System.Linq;
using Content.Server._Erida.SplitPersonality;
using Content.Server.Actions;
using Content.Server.Access.Systems;
using Content.Shared._Erida.SplitPersonality;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Actions.Components;
using Content.Shared.DetailExaminable;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Server.PDA;
using Content.Server.StationRecords.Systems;
using Content.Shared.StationRecords;
using Content.Shared.Preferences;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._Erida;

[TestFixture]
public sealed class SplitPersonalityTest
{
    [Test]
    public async Task AlternatePersonalityValidationWorksOnClientAndChecksServerLimit()
    {
        await using var pair = await PoolManager.GetServerClient();
        await pair.Client.WaitAssertion(() =>
        {
            var config = IoCManager.Resolve<IConfigurationManager>();
            Assert.That(config.IsCVarRegistered(CCVars.GameMaxCharacterSlots.Name), Is.False);
            var profile = HumanoidCharacterProfile.DefaultWithSpecies("Human");
            profile.AlternatePersonalitySlot = 1;
            Assert.DoesNotThrow(() => profile.EnsureValid(null!, IoCManager.Instance!));
            Assert.That(profile.AlternatePersonalitySlot, Is.EqualTo(1));
            profile.AlternatePersonalitySlot = -1;
            profile.EnsureValid(null!, IoCManager.Instance!);
            Assert.That(profile.AlternatePersonalitySlot, Is.Null);
        });
        await pair.Server.WaitAssertion(() =>
        {
            var config = IoCManager.Resolve<IConfigurationManager>();
            var profile = HumanoidCharacterProfile.DefaultWithSpecies("Human");
            profile.AlternatePersonalitySlot = config.GetCVar(CCVars.GameMaxCharacterSlots);
            profile.EnsureValid(null!, IoCManager.Instance!);
            Assert.That(profile.AlternatePersonalitySlot, Is.Null);
            profile.AlternatePersonalitySlot = 1;
            profile.EnsureValid(null!, IoCManager.Instance!);
            Assert.That(profile.AlternatePersonalitySlot, Is.EqualTo(1));
        });
        await pair.CleanReturnAsync();
    }

    [TestCase("Human", "MobHuman", false)]
    [TestCase("Demon", "MobDemon", false)]
    [TestCase("Human", "MobHuman", true)]
    [TestCase("Demon", "MobDemon", true)]
    public async Task SwitchKeepsBodyAndJobAndRestoresPrimary(string species, string prototype, bool withPda)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entities = server.EntMan;
        await server.WaitAssertion(() =>
        {
            var body = entities.SpawnEntity(prototype, map.GridCoords);
            var humanoids = entities.System<SharedHumanoidAppearanceSystem>();
            var profile = HumanoidCharacterProfile.DefaultWithSpecies(species).WithName("Primary");
            profile.Appearance.HairStyleId = "HumanHairDoublebunLong";
            profile.Appearance.Markings.Add(new Content.Shared.Humanoid.Markings.Marking(
                "HumanHairDoublebunLong", new[] { Color.Black }));
            profile.Appearance.Markings.Add(new Content.Shared.Humanoid.Markings.Marking(
                "HumanHairDoublebunLong", new[] { Color.Black }));
            profile.FlavorText = "First description";
            humanoids.LoadProfile(body, profile);
            var appearance = entities.GetComponent<HumanoidAppearanceComponent>(body);
            var originalSkin = appearance.SkinColor;
            var originalEyes = appearance.EyeColor;
            var originalShape = appearance.MarkingSet.Markings.SelectMany(p => p.Value).Select(m => m.MarkingId).ToArray();
            var originalColors = appearance.MarkingSet.Markings.SelectMany(p => p.Value).SelectMany(m => m.MarkingColors).ToArray();
            var originalHeight = appearance.Height;
            var originalSex = appearance.Sex;
            var equipment = entities.SpawnEntity(withPda ? "PassengerPDA" : "PassengerIDCard", map.GridCoords);
            var pda = withPda ? entities.GetComponent<PdaComponent>(equipment) : null;
            var card = pda?.ContainedId ?? equipment;
            var cards = entities.System<IdCardSystem>();
            cards.TryChangeFullName(card, "Registered owner");
            cards.TryChangeJobTitle(card, "Assistant");
            Assert.That(entities.System<InventorySystem>().TryEquip(body, equipment, "id", force: true), Is.True);
            if (pda != null)
                entities.System<PdaSystem>().SetOwner(equipment, pda, body, "Registered owner");
            var originalAccess = entities.GetComponent<AccessComponent>(card).Tags.ToArray();
            var station = entities.SpawnEntity(null, map.GridCoords);
            entities.AddComponent<StationRecordsComponent>(station);
            var records = entities.System<StationRecordsSystem>();
            var registration = new GeneralStationRecord { Name = "Registered owner", JobTitle = "Assistant" };
            var registrationKey = records.AddRecordEntry(station, registration);
            Assert.That(registrationKey.IsValid(), Is.True);
            records.SetIdKey(equipment, registrationKey);
            var component = entities.EnsureComponent<SplitPersonalityComponent>(body);
            component.Primary = profile.Clone();
            component.PrimaryColors = PersonalityColors.Copy(appearance.MarkingSet);
            component.Alternate = profile.WithName("Alternate");
            component.Alternate.FlavorText = "Second description";
            component.Alternate.Appearance.EyeColor = Color.Red;
            component.Alternate.Appearance.SkinColor = Color.Red;
            component.AlternateColors = PersonalityColors.Copy(appearance.MarkingSet);
            foreach (var marking in component.AlternateColors.Markings.SelectMany(p => p.Value))
                marking.SetColor(Color.Red);
            component.AlternateColors.Markings[Content.Shared.Humanoid.Markings.MarkingCategories.Hair] = new()
            {
                new Content.Shared.Humanoid.Markings.Marking("WFMeridianHairCurtains", new[] { Color.Red }),
            };
            var actions = entities.System<ActionsSystem>();
            var actionId = actions.AddAction(body, "ActionSwitchPersonality");
            Assert.That(actionId, Is.Not.Null);
            Entity<ActionComponent> action = (actionId!.Value, entities.GetComponent<ActionComponent>(actionId.Value));
            actions.SetUseDelay(action, null);
            actions.PerformAction(body, action);
            Assert.Multiple(() =>
            {
                Assert.That(component.AlternateActive, Is.True);
                Assert.That(entities.GetComponent<MetaDataComponent>(body).EntityName, Is.EqualTo("Alternate"));
                Assert.That(entities.GetComponent<DetailExaminableComponent>(body).Content, Is.EqualTo("Second description"));
                Assert.That(appearance.Species.Id, Is.EqualTo(species));
                Assert.That(appearance.Sex, Is.EqualTo(originalSex));
                Assert.That(appearance.Height, Is.EqualTo(originalHeight));
                Assert.That(appearance.MarkingSet.Markings.SelectMany(p => p.Value).Select(m => m.MarkingId), Is.EqualTo(originalShape));
                Assert.That(appearance.EyeColor, Is.EqualTo(species == "Demon" ? Color.Red : originalEyes));
                foreach (var hair in appearance.MarkingSet.Markings[Content.Shared.Humanoid.Markings.MarkingCategories.Hair])
                    Assert.That(hair.MarkingColors, Is.All.EqualTo(species == "Demon" ? Color.Red : Color.Black));
                Assert.That(entities.GetComponent<IdCardComponent>(card).FullName, Is.EqualTo("Registered owner"));
                Assert.That(entities.GetComponent<AccessComponent>(card).Tags, Is.EquivalentTo(originalAccess));
                Assert.That(registration.Name, Is.EqualTo("Registered owner"));
                Assert.That(registration.JobTitle, Is.EqualTo("Assistant"));
                if (pda != null)
                    Assert.That(pda.OwnerName, Is.EqualTo("Registered owner"));
                Assert.That(entities.GetComponent<IdCardComponent>(card).LocalizedJobTitle, Is.EqualTo("Assistant"));
            });
            actions.PerformAction(body, action);
            Assert.Multiple(() =>
            {
                Assert.That(component.AlternateActive, Is.False);
                Assert.That(component.SwitchingPersonality, Is.False);
                Assert.That(entities.GetComponent<IdCardComponent>(card).FullName, Is.EqualTo("Registered owner"));
                Assert.That(entities.GetComponent<IdCardComponent>(card).LocalizedJobTitle, Is.EqualTo("Assistant"));
                Assert.That(entities.GetComponent<AccessComponent>(card).Tags, Is.EquivalentTo(originalAccess));
                Assert.That(registration.Name, Is.EqualTo("Registered owner"));
                Assert.That(registration.JobTitle, Is.EqualTo("Assistant"));
                if (pda != null)
                    Assert.That(pda.OwnerName, Is.EqualTo("Registered owner"));
                Assert.That(entities.GetComponent<MetaDataComponent>(body).EntityName, Is.EqualTo("Primary"));
                Assert.That(appearance.EyeColor, Is.EqualTo(originalEyes));
                Assert.That(appearance.SkinColor, Is.EqualTo(originalSkin));
                Assert.That(appearance.MarkingSet.Markings.SelectMany(p => p.Value).SelectMany(m => m.MarkingColors), Is.EqualTo(originalColors));
            });
            // Ordinary renames still update registered equipment outside the personality switch.
            entities.System<MetaDataSystem>().SetEntityName(body, "Administrative rename");
            Assert.That(entities.GetComponent<IdCardComponent>(card).FullName, Is.EqualTo("Administrative rename"));
            Assert.That(registration.Name, Is.EqualTo("Administrative rename"));
            if (pda != null)
                Assert.That(pda.OwnerName, Is.EqualTo("Administrative rename"));
            entities.DeleteEntity(body);
            entities.DeleteEntity(station);
        });
        await pair.CleanReturnAsync();
    }
}
